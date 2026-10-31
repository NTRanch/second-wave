// SecondWave.Tests — console test runner (exit-code gated).
// Half validation, half adversarial, per the estate test split.
using System;
using NTRanch.SecondWave;

internal static class Program
{
    private static int _failures;
    private static int _count;

    private static void Check(bool condition, string name)
    {
        _count++;
        if (!condition) { _failures++; Console.WriteLine($"FAIL {name}"); }
        else Console.WriteLine($"pass {name}");
    }

    private static Core Play(int level, uint seed = 42)
    {
        var core = Core.Create(seed, level, false);
        core.Step(Input.Confirm); // gate -> title
        core.Step(Input.Confirm); // title PLAY -> play
        return core;
    }

    private static void HoldToPeak(Core core)
    {
        for (int i = 0; i < 20000 && core.Phase == 0; i++) core.Step(Input.Tap);
    }

    private static int Main()
    {
        // ---------- validation ----------
        var boot = Core.Create(1, 0, false);
        Check(boot.Screen == 0, "boot screen is the 18+ gate");
        bool threw = false;
        try { Core.Create(1, 3, false); } catch (ArgumentOutOfRangeException) { threw = true; }
        Check(threw, "create rejects level 3");

        var g = Play(0);
        Check(g.Screen == 3 && g.Scene == 1 && g.Phase == 0, "play starts scene 1 in build");
        int before = g.Channel(0);
        for (int i = 0; i < 10; i++) g.Step(Input.Tap);
        Check(g.Channel(0) == before + 90, "pulse rises 9/step while held");
        for (int i = 0; i < 10; i++) g.Step(0);
        Check(g.Channel(0) == before + 90 - 50, "pulse decays 5/step released");

        var pk = Play(0);
        HoldToPeak(pk);
        Check(pk.Phase == 1 && pk.Depth >= Core.PeakThreshold, "peak entered at threshold 850");
        pk.Step(Input.Confirm);
        Check(pk.Phase == 2 && pk.ContinuationsTotal == 1 && pk.Sensitivity == 300,
            "confirm at peak enters continuation, sensitivity seeded 300");

        Check(Core.ReducedRise(0) == 4 && Core.ReducedRise(2) == 7, "reduced rise is 55% floored");
        Check(Core.ReducedDecay(0) == 3 && Core.ReducedDecay(1) == 1, "reduced decay is 60% floored");

        // Continuation success -> scene 2 remembers (residue + raised floor).
        var demo = Core.Create(7, 0, true);
        demo.StartLevel(0);
        int guard = 0;
        while (demo.Scene == 1 && guard++ < 100000) demo.Step(0);
        Check(demo.Scene == 2, "demo reaches scene 2");
        Check(demo.Channel(0) >= 90, "scene 2 opens with residue + raised floor after continuation");

        // Full demo victories on all levels (the playtest contract).
        for (int level = 0; level < 3; level++)
        {
            var d = Core.Create(9000 + (uint)level, level, true);
            d.StartLevel(level);
            int steps = 0;
            while (d.Screen != 6 && steps < 30000) { d.Step(0); steps++; }
            Check(d.Screen == 6 && d.ContinuationsTotal == 2,
                $"demo wins level {level + 1} via two continuations (steps={steps})");
        }

        var w1 = Core.Weather(0, 0, false);
        var w2 = Core.Weather(1000, 2, false);
        Check(w1.Rain == 0 && w1.Stars == 24 && w2.Rain == 40 && w2.Mist == 3 && w2.Stars == 4,
            "weather is a pure monotone function of depth and phase");
        Check(Core.Weather(500, 0, true).Rain == Core.Weather(500, 0, false).Rain / 2,
            "calm weather halves rain");

        var s = Core.Create(3, 0, false);
        s.ApplySettings(true, false);
        Check(s.SettingsBits == 1, "settings bridge round-trips calm");
        s.ApplySettings(false, true);
        Check(s.SettingsBits == 2, "settings bridge round-trips assist");
        s.ApplyRecord(2, 1, 3);
        Check(s.RecordBits == (2 | (1 << 8) | (3 << 16)), "record bridge round-trips");

        // ---------- adversarial ----------
        var adv = Play(2);
        for (int i = 0; i < 500; i++) adv.Step(int.MaxValue); // every bit set
        Check(adv.Channel(0) <= 1000 && adv.Channel(1) <= 1000 && adv.Channel(2) <= 1000,
            "hostile input bits never push a channel past the cap");
        Check(adv.ContinuationsTotal <= 2, "no input flood manufactures continuations");

        var stop = Play(0);
        HoldToPeak(stop);
        int scoreAtPeak = stop.Score;
        int depthAtPeak = stop.Depth;
        stop.Step(Input.Stop);
        Check(stop.StopsTotal == 1 && stop.StopCostTotal == Core.StopCost
            && stop.Score == Math.Max(0, scoreAtPeak + depthAtPeak / 100 - Core.StopCost),
            "STOP at peak records the cost exactly once");

        var expire = Play(0);
        HoldToPeak(expire);
        for (int i = 0; i < 400 && expire.StopsTotal == 0; i++) expire.Step(0);
        Check(expire.StopsTotal == 1, "peak window expiry auto-stops (a choice by default)");

        // Exactly-one-continuation law: after continuing, depth past the
        // threshold again must NOT re-enter the peak in the same scene.
        var once = Play(0);
        HoldToPeak(once);
        once.Step(Input.Confirm);
        bool rePeaked = false;
        for (int i = 0; i < 3000; i++)
        {
            once.Step(Input.Tap);
            if (once.Phase == 1) rePeaked = true;
            if (once.Scene == 2 || once.Screen != 3) break;
        }
        Check(!rePeaked && once.ContinuationsTotal == 1, "exactly one continuation per scene");

        var det1 = Core.Create(99, 1, true); det1.StartLevel(1);
        var det2 = Core.Create(99, 1, true); det2.StartLevel(1);
        bool same = true;
        for (int i = 0; i < 5000; i++)
        {
            det1.Step(0); det2.Step(0);
            if (det1.Depth != det2.Depth || det1.Phase != det2.Phase
                || det1.DrawText() != det2.DrawText()) same = false;
        }
        Check(same, "identical seed and inputs give identical traces and frames");

        var clampRec = Core.Create(1, 0, false);
        clampRec.ApplyRecord(5000, -3, 12);
        Check(clampRec.RecordBits == (99 | (0 << 8) | (12 << 16)), "record bridge clamps hostile values");

        bool threw2 = false;
        try { Play(0).StartLevel(-1); } catch (ArgumentOutOfRangeException) { threw2 = true; }
        Check(threw2, "start_level rejects -1");

        // Sensitivity cap settles the scene (overstimulation ceiling).
        var cap = Play(0);
        HoldToPeak(cap);
        cap.Step(Input.Confirm);
        int guardSteps = 0;
        while (cap.EndKind == 0 && guardSteps++ < 20000) cap.Step(Input.Tap);
        Check(cap.EndKind == 3 || cap.EndKind == 1,
            "held continuation ends at the sensitivity cap or the band goal, never runs away");

        Console.WriteLine($"tests: {_count - _failures}/{_count} passed");
        return _failures == 0 ? 0 : 1;
    }
}
