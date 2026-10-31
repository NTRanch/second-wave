// SecondWave.Core — Prima's Continuation Rig as a deterministic
// fixed-step C# core (Tiger Style C#: explicit contracts, bounded work,
// nullable discipline). No Unity dependency: the same semantics ship as
// the JS realization in web/game.mjs, gated by conformance vectors
// dumped from THIS core. All arithmetic is integer milli-units
// (0..1000) so the C# and JS traces match exactly.
//
// Model: three phases per scene — Build, Peak (a decision window), and
// Continuation (one only, past the peak, at the Rig's reduced curve).
// STOP at the peak ends the scene at a recorded cost; continuing is a
// skill: ride the target band while the sensitivity meter climbs.
// Scene 2 opens with the Rig remembering scene 1 (per-channel residue
// plus a raised floor on the continuation path).

using System;
using System.Collections.Generic;
using System.Globalization;

namespace NTRanch.SecondWave
{
    /// <summary>Input bits shared by the C# core and the JS realization.</summary>
    public static class Input
    {
        public const int Tap = 1;
        public const int Confirm = 2;
        public const int Back = 4;
        public const int Pause = 8;
        public const int Up = 16;
        public const int Down = 32;
        public const int Stop = 64;
    }

    /// <summary>Level tuning. Arrays are indexed by level (0..2).</summary>
    public static class Levels
    {
        public static readonly int[] Unlocked = { 1, 2, 3 };
        public static readonly int[] PeakWindowSteps = { 313, 250, 188 };
        public static readonly int[] TargetDepth = { 650, 720, 780 };
        public static readonly int[] HoldNeedMs = { 4000, 5000, 6000 };
        public static readonly int[] TimeLimitSteps = { 18750, 20625, 22500 };
        public static readonly string[] Names =
            { "First Swell", "The Reduced Curve", "Second Wave" };
    }

    /// <summary>The deterministic Continuation Rig core.</summary>
    public sealed class Core
    {
        public const int MaxValue = 1000;
        public const int PeakThreshold = 850;
        public const int PeakCollapseDepth = 700;
        public const int LowDepth = 200;
        public const int LowStepsMax = 188;
        public const int SceneEndSteps = 90;
        public const int StopCost = 150;
        public const int SuccessBonus = 500;
        public const int SensitivityCap = 1000;
        public const int SensitivityCapAssist = 1250;

        private static readonly int[] Rise = { 9, 6, 13 };
        private static readonly int[] Decay = { 5, 2, 8 };

        private readonly bool _demo;
        private readonly uint _seed;
        private readonly int[] _v = new int[3];
        private int _active;
        private bool _assist;
        private bool _calmWeather;
        private int _continuationsScene;
        private int _continuationsTotal;
        private int _demoPeakWait;
        private int _demoSwitchTimer;
        private int _endKind; // 0 none, 1 success, 2 stop, 3 settle, 4 faded
        private int _endTimer;
        private int _eventCode;
        private int _frame;
        private int _holdMs;
        private int _level;
        private int _lowSteps;
        private int _menuCursor;
        private int _peakTimer;
        private int _phase; // 0 build, 1 peak, 2 continuation
        private int _recordContinuations;
        private int _recordScenes;
        private int _recordStops;
        private int _scene;
        private int _score;
        private int _screen; // 0 gate 1 title 2 howto 3 play 4 pause 5 settings 6 victory 7 gameover
        private bool _sceneEnding;
        private int _sensitivity;
        private int _settingsCursor;
        private int _stopsTotal;
        private int _stopCostTotal;
        private int _timeLeftSteps;

        private Core(uint seed, int level, bool demo)
        {
            _seed = seed;
            _demo = demo;
            _level = level;
            _screen = 0;
            _scene = 1;
            _timeLeftSteps = Levels.TimeLimitSteps[level];
        }

        /// <summary>Create a core. Level must be 0..2.</summary>
        public static Core Create(uint seed, int level, bool demo)
        {
            if (level < 0 || level > 2)
                throw new ArgumentOutOfRangeException(nameof(level), "level must be 0..2");
            return new Core(seed, level, demo);
        }

        public int Screen => _screen;
        public int LevelIdx => _level;
        public int Scene => _scene;
        public int Phase => _phase;
        public int Active => _active;
        public int Sensitivity => _sensitivity;
        public int ContinuationsTotal => _continuationsTotal;
        public int StopsTotal => _stopsTotal;
        public int StopCostTotal => _stopCostTotal;
        public int Score => _score;
        public int HoldMs => _holdMs;
        public int EventCode => _eventCode;
        public int EndKind => _endKind;

        public int Channel(int index)
        {
            if (index < 0 || index > 2)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _v[index];
        }

        /// <summary>Depth: mean of the unlocked channels, integer milli.</summary>
        public int Depth
        {
            get
            {
                int n = Levels.Unlocked[_level];
                int sum = 0;
                for (int i = 0; i < n; i++) sum += _v[i];
                return sum / n;
            }
        }

        public int SettingsBits => (_calmWeather ? 1 : 0) | (_assist ? 2 : 0);

        public void ApplySettings(bool calmWeather, bool assist)
        {
            _calmWeather = calmWeather;
            _assist = assist;
        }

        public int RecordBits =>
            (_recordContinuations & 0xFF) | ((_recordStops & 0xFF) << 8)
            | ((_recordScenes & 0xFF) << 16);

        public void ApplyRecord(int continuations, int stops, int scenes)
        {
            _recordContinuations = Math.Clamp(continuations, 0, 99);
            _recordStops = Math.Clamp(stops, 0, 99);
            _recordScenes = Math.Clamp(scenes, 0, 99);
        }

        /// <summary>Reduced-curve rise for a channel (continuation phase).</summary>
        public static int ReducedRise(int channel) => Rise[channel] * 55 / 100;

        /// <summary>Reduced-curve decay for a channel (continuation phase).</summary>
        public static int ReducedDecay(int channel) => Decay[channel] * 60 / 100;

        /// <summary>Trance-Deep weather as a pure function of depth+phase.</summary>
        public static (int Rain, int Stars, int Mist) Weather(int depth, int phase, bool calm)
        {
            int d = Math.Clamp(depth, 0, MaxValue);
            int rain = d / 25;
            int stars = 24 - d / 50;
            int mist = phase == 2 ? 3 : d / 300;
            if (calm) { rain /= 2; mist /= 2; }
            return (rain, stars, mist);
        }

        public void StartLevel(int level)
        {
            if (level < 0 || level > 2)
                throw new ArgumentOutOfRangeException(nameof(level), "level must be 0..2");
            _level = level;
            _scene = 1;
            _score = 0;
            _continuationsTotal = 0;
            _stopsTotal = 0;
            _stopCostTotal = 0;
            _timeLeftSteps = Levels.TimeLimitSteps[level];
            ResetScene(0, 0, 0, 0);
            _screen = 3;
        }

        private void ResetScene(int r0, int r1, int r2, int floor)
        {
            _v[0] = Math.Clamp(r0 + floor, 0, 600);
            _v[1] = Math.Clamp(r1 + floor, 0, 600);
            _v[2] = Math.Clamp(r2 + floor, 0, 600);
            _active = 0;
            _phase = 0;
            _continuationsScene = 0;
            _holdMs = 0;
            _sensitivity = 0;
            _lowSteps = 0;
            _peakTimer = 0;
            _sceneEnding = false;
            _endKind = 0;
            _endTimer = 0;
            _demoPeakWait = 0;
            _demoSwitchTimer = 0;
        }

        /// <summary>Advance one fixed 16 ms step. Unknown bits are ignored.</summary>
        public void Step(int inputBits)
        {
            _eventCode = 0;
            _frame++;
            int bits = _demo ? DemoBits(inputBits) : inputBits;
            switch (_screen)
            {
                case 0:
                    if ((bits & Input.Confirm) != 0) _screen = 1;
                    break;
                case 1: StepTitle(bits); break;
                case 2:
                    if ((bits & (Input.Confirm | Input.Back)) != 0) _screen = 1;
                    break;
                case 3: StepPlay(bits); break;
                case 4:
                    if ((bits & Input.Confirm) != 0) _screen = 3;
                    else if ((bits & Input.Back) != 0) _screen = 1;
                    break;
                case 5: StepSettings(bits); break;
                case 6:
                case 7:
                    if ((bits & Input.Confirm) != 0) _screen = 1;
                    break;
            }
        }

        private void StepTitle(int bits)
        {
            if ((bits & Input.Up) != 0) _menuCursor = (_menuCursor + 2) % 3;
            if ((bits & Input.Down) != 0) _menuCursor = (_menuCursor + 1) % 3;
            if ((bits & Input.Confirm) == 0) return;
            if (_menuCursor == 0) StartLevel(_level);
            else if (_menuCursor == 1) _screen = 2;
            else _screen = 5;
        }

        private void StepSettings(int bits)
        {
            if ((bits & Input.Up) != 0) _settingsCursor = (_settingsCursor + 1) % 2;
            if ((bits & Input.Down) != 0) _settingsCursor = (_settingsCursor + 1) % 2;
            if ((bits & Input.Confirm) != 0)
            {
                if (_settingsCursor == 0) _calmWeather = !_calmWeather;
                else _assist = !_assist;
            }
            if ((bits & Input.Back) != 0) _screen = 1;
        }

        private int DemoBits(int _)
        {
            if (_screen == 0) return Input.Confirm;
            if (_screen == 1) return Input.Confirm; // cursor starts on PLAY
            if (_screen != 3) return Input.Confirm;
            if (_sceneEnding) return 0;
            if (_phase == 1)
            {
                _demoPeakWait++;
                return _demoPeakWait >= 30 ? Input.Confirm : 0;
            }
            // Steer the lowest unlocked channel; hold unless depth or
            // sensitivity says breathe.
            _demoSwitchTimer++;
            if (_demoSwitchTimer >= 30)
            {
                _demoSwitchTimer = 0;
                int n = Levels.Unlocked[_level];
                int low = 0;
                for (int i = 1; i < n; i++) if (_v[i] < _v[low]) low = i;
                _active = low;
            }
            int d = Depth;
            if (_phase == 2)
            {
                int target = Levels.TargetDepth[_level];
                if (_sensitivity > 700) return 0;
                if (d > target + 180) return 0;
                return Input.Tap;
            }
            return d > 985 ? 0 : Input.Tap;
        }

        private void StepPlay(int bits)
        {
            if ((bits & Input.Pause) != 0 && !_sceneEnding) { _screen = 4; return; }
            if (_sceneEnding)
            {
                _endTimer--;
                if (_endTimer <= 0) AdvanceScene();
                return;
            }
            _timeLeftSteps--;
            if (_timeLeftSteps <= 0) { _screen = 7; _eventCode = 8; return; }
            int n = Levels.Unlocked[_level];
            if ((bits & Input.Down) != 0) { _active = (_active + 1) % n; _eventCode = 7; }
            if ((bits & Input.Up) != 0) { _active = (_active + n - 1) % n; _eventCode = 7; }
            bool held = (bits & Input.Tap) != 0;
            _score += Depth / 100;
            if (_phase == 0) StepBuild(held);
            else if (_phase == 1) StepPeak(bits);
            else StepContinuation(held);
        }

        private void ApplyDynamics(bool held, bool reduced)
        {
            int n = Levels.Unlocked[_level];
            for (int i = 0; i < n; i++)
            {
                int rise = reduced ? ReducedRise(i) : Rise[i];
                int decay = reduced ? ReducedDecay(i) : Decay[i];
                if (held && i == _active) _v[i] = Math.Min(MaxValue, _v[i] + rise);
                else _v[i] = Math.Max(0, _v[i] - (held ? decay / 2 : decay));
            }
        }

        private void StepBuild(bool held)
        {
            ApplyDynamics(held, false);
            if (Depth >= PeakThreshold && _continuationsScene == 0)
            {
                _phase = 1;
                _peakTimer = Levels.PeakWindowSteps[_level];
                _demoPeakWait = 0;
                _eventCode = 1;
            }
        }

        private void StepPeak(int bits)
        {
            // The peak is a held moment: values freeze; the window
            // timer is the pressure to choose.
            if ((bits & Input.Confirm) != 0)
            {
                _phase = 2;
                _continuationsScene = 1;
                _continuationsTotal++;
                _sensitivity = 300; // the second wave starts already tender
                _eventCode = 2;
                return;
            }
            if ((bits & (Input.Stop | Input.Back)) != 0) { EndScene(2); return; }
            _peakTimer--;
            if (_peakTimer <= 0) { EndScene(2); return; }
            if (Depth < PeakCollapseDepth) _phase = 0;
        }

        private void StepContinuation(bool held)
        {
            ApplyDynamics(held, true);
            int cap = _assist ? SensitivityCapAssist : SensitivityCap;
            _sensitivity = Math.Clamp(_sensitivity + (held ? 5 : -3), 0, cap);
            if (Depth >= Levels.TargetDepth[_level]) _holdMs += 16;
            if (_sensitivity >= cap) { EndScene(3); return; }
            if (_holdMs >= Levels.HoldNeedMs[_level]) { EndScene(1); return; }
            if (Depth < LowDepth) { _lowSteps++; if (_lowSteps > LowStepsMax) EndScene(4); }
            else _lowSteps = 0;
        }

        private void EndScene(int kind)
        {
            _endKind = kind;
            _sceneEnding = true;
            _endTimer = SceneEndSteps;
            if (kind == 1) { _score += SuccessBonus; _eventCode = 4; }
            else if (kind == 2)
            {
                _stopsTotal++;
                _stopCostTotal += StopCost;
                _score = Math.Max(0, _score - StopCost);
                _eventCode = 3;
            }
            else if (kind == 3) _eventCode = 6;
            else _eventCode = 4;
        }

        private void AdvanceScene()
        {
            if (_scene == 2)
            {
                _recordScenes++;
                _screen = 6;
                _eventCode = 5;
                return;
            }
            int pct = _endKind == 1 ? 40 : _endKind == 3 ? 25 : _endKind == 2 ? 15 : 10;
            int floor = _endKind == 1 ? 90 : _endKind == 3 ? 40 : 0;
            int r0 = _v[0] * pct / 100, r1 = _v[1] * pct / 100, r2 = _v[2] * pct / 100;
            _scene = 2;
            ResetScene(r0, r1, r2, floor);
        }

        // ---- draw list (960x960, palette indices, r/c/l/p/t ops) ----

        private static string F(string format, params object[] args) =>
            string.Format(CultureInfo.InvariantCulture, format, args);

        /// <summary>The frame as draw-list text lines.</summary>
        public string DrawText()
        {
            var ops = new List<string>();
            ops.Add("r 0 0 960 960 0");
            DrawWeather(ops);
            switch (_screen)
            {
                case 0: DrawGate(ops); break;
                case 1: DrawTitle(ops); break;
                case 2: DrawHowto(ops); break;
                case 3:
                case 4: DrawPlay(ops); if (_screen == 4) DrawPause(ops); break;
                case 5: DrawSettings(ops); break;
                case 6: DrawEnd(ops, true); break;
                case 7: DrawEnd(ops, false); break;
            }
            return string.Join("\n", ops);
        }

        private void DrawWeather(List<string> ops)
        {
            var (rain, stars, mist) = Weather(Depth, _phase, _calmWeather);
            for (int i = 0; i < stars; i++)
            {
                int x = (int)((_seed * 31u + (uint)(i * 173) + (uint)(_frame / 8)) % 940u) + 10;
                int y = (int)((_seed * 17u + (uint)(i * 269)) % 300u) + 10;
                ops.Add(F("c {0} {1} 2 7", x, y));
            }
            for (int i = 0; i < rain; i++)
            {
                int x = (int)((_seed * 13u + (uint)(i * 211) + (uint)(_frame * 3)) % 940u) + 10;
                int y = (int)((_seed * 7u + (uint)(i * 97) + (uint)(_frame * 11)) % 880u) + 40;
                ops.Add(F("l {0} {1} {2} {3} 2 4", x, y, x - 6, y + 22));
            }
            for (int m = 0; m < mist; m++)
                ops.Add(F("r 0 {0} 960 26 9", 640 + m * 70));
        }

        private void DrawGate(List<string> ops)
        {
            ops.Add("t 300 300 40 1 SECOND WAVE");
            ops.Add("t 330 400 20 2 18+ ONLY. ADULTS ONLY.");
            ops.Add("t 240 450 20 3 A CONTINUATION RIG, PLAYED BY HOLDING ON");
            ops.Add("t 240 500 20 3 PAST THE FIRST PEAK. PRIMA, WAVE C.");
            ops.Add("t 330 580 20 1 PRESS ENTER TO CONFIRM");
        }

        private void DrawTitle(List<string> ops)
        {
            ops.Add("t 300 180 40 1 SECOND WAVE");
            ops.Add("t 300 260 20 3 PRIMA AND THE CONTINUATION RIG");
            string[] items = { "PLAY", "HOW TO PLAY", "SETTINGS" };
            for (int i = 0; i < 3; i++)
                ops.Add(F("t 380 {0} 20 {1} {2}{3}", 380 + i * 60,
                    i == _menuCursor ? 1 : 2, i == _menuCursor ? "+ " : "  ", items[i]));
            ops.Add(F("t 240 640 20 3 RECORD: {0} CONTINUATIONS - {1} STOPS - {2} LEVELS DONE",
                _recordContinuations, _recordStops, _recordScenes));
            ops.Add("t 240 700 20 3 UP/DOWN CHOOSE - ENTER CONFIRM");
        }

        private void DrawHowto(List<string> ops)
        {
            ops.Add("t 330 120 40 1 HOW TO PLAY");
            string[] lines =
            {
                "HOLD (SPACE / POINTER) TO DRIVE THE ACTIVE",
                "CHANNEL UP. RELEASE AND EVERY CHANNEL DECAYS.",
                "UP/DOWN SWITCHES THE ACTIVE CHANNEL.",
                "AT THE PEAK (DEPTH 850) CHOOSE, QUICKLY:",
                "ENTER - CONTINUE PAST THE PEAK, ON THE",
                "REDUCED CURVE. B - STOP, END THE SCENE,",
                "AND PAY THE RECORDED COST.",
                "IN CONTINUATION, RIDE THE TARGET BAND WHILE",
                "THE SENSITIVITY METER CLIMBS. AT ITS CAP",
                "THE SCENE SETTLES. ONE CONTINUATION ONLY.",
                "SCENE 2 OPENS WITH THE RIG REMEMBERING",
                "SCENE 1: RESIDUE, AND A RAISED FLOOR IF",
                "YOU CONTINUED. TWO SCENES PER LEVEL.",
            };
            for (int i = 0; i < lines.Length; i++)
                ops.Add(F("t 130 {0} 20 2 {1}", 200 + i * 48, lines[i]));
            ops.Add("t 300 840 20 1 ENTER - BACK TO TITLE");
        }

        private void DrawSettings(List<string> ops)
        {
            ops.Add("t 360 180 40 1 SETTINGS");
            ops.Add(F("t 240 {0} 20 {1} {2}CALM WEATHER: {3}", 340,
                _settingsCursor == 0 ? 1 : 2, _settingsCursor == 0 ? "+ " : "  ",
                _calmWeather ? "ON" : "OFF"));
            ops.Add(F("t 240 {0} 20 {1} {2}SENSITIVITY ASSIST: {3}", 420,
                _settingsCursor == 1 ? 1 : 2, _settingsCursor == 1 ? "+ " : "  ",
                _assist ? "ON" : "OFF"));
            ops.Add("t 200 560 20 3 ASSIST RAISES THE SENSITIVITY CAP.");
            ops.Add("t 200 610 20 3 SETTINGS ARE NEVER GATED BY PROGRESS.");
            ops.Add("t 260 700 20 3 ENTER TOGGLE - BACKSPACE BACK");
        }

        private void DrawPlay(List<string> ops)
        {
            int d = Depth;
            // The figure: concentric rings answering depth.
            ops.Add(F("c 480 400 {0} 4", 60 + d / 6));
            ops.Add(F("c 480 400 {0} 1", 30 + d / 10));
            ops.Add(F("c 480 400 12 2"));
            string phaseName = _phase == 0 ? "BUILD" : _phase == 1 ? "PEAK" : "CONTINUATION";
            ops.Add(F("t 60 50 20 1 {0} - SCENE {1}/2 - {2}", Levels.Names[_level], _scene, phaseName));
            ops.Add(F("t 60 90 20 2 DEPTH {0} - TARGET {1}", d,
                _phase == 2 ? Levels.TargetDepth[_level] : PeakThreshold));
            ops.Add(F("t 700 50 20 2 TIME {0}S", _timeLeftSteps * 16 / 1000));
            ops.Add(F("t 700 90 20 2 SCORE {0}", _score));
            // Depth gauge.
            ops.Add("r 60 140 840 18 9");
            ops.Add(F("r 60 140 {0} 18 1", d * 840 / 1000));
            ops.Add(F("l {0} 132 {1} 166 3 8", 60 + PeakThreshold * 840 / 1000, 60 + PeakThreshold * 840 / 1000));
            if (_phase == 1)
            {
                ops.Add("t 200 200 20 8 THE PEAK. ENTER - CONTINUE. B - STOP.");
                ops.Add(F("t 330 240 20 2 DECIDE IN {0}S", _peakTimer * 16 / 1000 + 1));
            }
            if (_phase == 2)
            {
                ops.Add(F("t 60 200 20 5 SENSITIVITY {0} / {1}", _sensitivity,
                    _assist ? SensitivityCapAssist : SensitivityCap));
                ops.Add(F("r 60 235 840 12 9"));
                ops.Add(F("r 60 235 {0} 12 5", _sensitivity * 840
                    / (_assist ? SensitivityCapAssist : SensitivityCap)));
                ops.Add(F("t 60 265 20 2 HELD IN BAND {0} / {1} MS", _holdMs, Levels.HoldNeedMs[_level]));
            }
            if (_sceneEnding)
            {
                string msg = _endKind == 1 ? "THE SECOND WAVE, RIDDEN."
                    : _endKind == 2 ? "STOPPED. THE COST IS RECORDED."
                    : _endKind == 3 ? "SETTLED AT THE CAP."
                    : "FADED BELOW THE FLOOR.";
                ops.Add(F("t 240 500 20 1 {0}", msg));
            }
            // Channel bars.
            string[] names = { "PULSE", "SUSTAIN", "SURGE" };
            int n = Levels.Unlocked[_level];
            for (int i = 0; i < 3; i++)
            {
                int x = 180 + i * 240;
                bool live = i < n;
                ops.Add(F("r {0} 780 160 14 9", x));
                if (live) ops.Add(F("r {0} 780 {1} 14 {2}", x, _v[i] * 160 / 1000, i == _active ? 1 : 4));
                ops.Add(F("t {0} 810 20 {1} {2}{3}", x, live ? (i == _active ? 1 : 2) : 7,
                    i == _active && live ? "+ " : "  ", live ? names[i] : "LOCKED"));
                if (live) ops.Add(F("t {0} 845 20 3 {1}", x, _v[i]));
            }
            ops.Add("t 130 910 20 3 HOLD TO DRIVE - UP/DOWN CHANNEL - P PAUSE");
        }

        private void DrawPause(List<string> ops)
        {
            ops.Add("r 240 340 480 220 9");
            ops.Add("t 400 380 40 1 PAUSED");
            ops.Add("t 300 470 20 2 ENTER RESUME - BACKSPACE TITLE");
        }

        private void DrawEnd(List<string> ops, bool victory)
        {
            ops.Add(F("t {0} 260 44 1 {1}", victory ? 330 : 320, victory ? "VICTORY" : "TIME OUT"));
            ops.Add(F("t 240 380 20 2 {0} - BOTH SCENES CLOSED", Levels.Names[_level]));
            ops.Add(F("t 240 440 20 2 CONTINUATIONS {0} - STOPS {1} (COST {2})",
                _continuationsTotal, _stopsTotal, _stopCostTotal));
            ops.Add(F("t 240 500 20 2 SCORE {0}", _score));
            ops.Add("t 300 620 20 1 ENTER - BACK TO TITLE");
        }
    }
}
