// game.mjs — Second Wave JS realization of the C# Continuation Rig
// core (core/SecondWave.Core/Core.cs). Shared verbatim by the shipped
// page (inlined by tools/assemble.mjs) and the node harnesses.
// Integer milli arithmetic mirrors the C# core exactly; conformance
// vectors dumped FROM the C# core gate this file (tools/conformance.mjs).

export const PALETTE = [
  "#0d0208", "#ff2e88", "#f8f0ed", "#ac9393", "#540e46",
  "#ec8584", "#eaeaea", "#68636c", "#ffd166", "#1a0510",
];

export const INPUT = { tap: 1, confirm: 2, back: 4, pause: 8, up: 16, down: 32, stop: 64 };

export const LEVELS = [
  { unlocked: 1, peakWindowSteps: 313, targetDepth: 650, holdNeedMs: 4000, timeLimitSteps: 18750, name: "FIRST SWELL" },
  { unlocked: 2, peakWindowSteps: 250, targetDepth: 720, holdNeedMs: 5000, timeLimitSteps: 20625, name: "THE REDUCED CURVE" },
  { unlocked: 3, peakWindowSteps: 188, targetDepth: 780, holdNeedMs: 6000, timeLimitSteps: 22500, name: "SECOND WAVE" },
];

const RISE = [9, 6, 13];
const DECAY = [5, 2, 8];
const PEAK_THRESHOLD = 850;
const PEAK_COLLAPSE = 700;
const STOP_COST = 150;
const SUCCESS_BONUS = 500;
const clamp = (v, lo, hi) => Math.max(lo, Math.min(hi, v));

export function reducedRise(ch) { return Math.trunc(RISE[ch] * 55 / 100); }
export function reducedDecay(ch) { return Math.trunc(DECAY[ch] * 60 / 100); }
export function weather(depth, phase, calm) {
  const d = clamp(depth, 0, 1000);
  let rain = Math.trunc(d / 25);
  const stars = 24 - Math.trunc(d / 50);
  let mist = phase === 2 ? 3 : Math.trunc(d / 300);
  if (calm) { rain = Math.trunc(rain / 2); mist = Math.trunc(mist / 2); }
  return [rain, stars, mist];
}

export function createGame({ seed = 1, level = 0, demo = false } = {}) {
  if (level < 0 || level > 2) throw new Error(`core init rejected level ${level}`);
  const S = {
    seed: seed >>> 0, demo, level, screen: 0, scene: 1, phase: 0,
    v: [0, 0, 0], active: 0, calmWeather: false, assist: false,
    continuationsScene: 0, continuationsTotal: 0, demoPeakWait: 0,
    demoSwitchTimer: 0, endKind: 0, endTimer: 0, eventCode: 0, frame: 0,
    holdMs: 0, lowSteps: 0, menuCursor: 0, peakTimer: 0,
    recordContinuations: 0, recordScenes: 0, recordStops: 0,
    sceneEnding: false, score: 0, sensitivity: 0, settingsCursor: 0,
    stopsTotal: 0, stopCostTotal: 0, timeLeftSteps: LEVELS[level].timeLimitSteps,
  };
  const L = () => LEVELS[S.level];
  const depth = () => {
    const n = L().unlocked; let sum = 0;
    for (let i = 0; i < n; i++) sum += S.v[i];
    return Math.trunc(sum / n);
  };

  function resetScene(r0, r1, r2, floor) {
    S.v = [clamp(r0 + floor, 0, 600), clamp(r1 + floor, 0, 600), clamp(r2 + floor, 0, 600)];
    S.active = 0; S.phase = 0; S.continuationsScene = 0; S.holdMs = 0;
    S.sensitivity = 0; S.lowSteps = 0; S.peakTimer = 0; S.sceneEnding = false;
    S.endKind = 0; S.endTimer = 0; S.demoPeakWait = 0; S.demoSwitchTimer = 0;
  }
  function startLevel(l) {
    if (l < 0 || l > 2) throw new Error(`core start_level rejected ${l}`);
    S.level = l; S.scene = 1; S.score = 0; S.continuationsTotal = 0;
    S.stopsTotal = 0; S.stopCostTotal = 0; S.timeLeftSteps = L().timeLimitSteps;
    resetScene(0, 0, 0, 0); S.screen = 3;
  }
  function endScene(kind) {
    S.endKind = kind; S.sceneEnding = true; S.endTimer = 90;
    if (kind === 1) { S.score += SUCCESS_BONUS; S.eventCode = 4; }
    else if (kind === 2) {
      S.stopsTotal++; S.stopCostTotal += STOP_COST;
      S.score = Math.max(0, S.score - STOP_COST); S.eventCode = 3;
    } else if (kind === 3) S.eventCode = 6;
    else S.eventCode = 4;
  }
  function advanceScene() {
    if (S.scene === 2) { S.recordScenes++; S.screen = 6; S.eventCode = 5; return; }
    const pct = S.endKind === 1 ? 40 : S.endKind === 3 ? 25 : S.endKind === 2 ? 15 : 10;
    const floor = S.endKind === 1 ? 90 : S.endKind === 3 ? 40 : 0;
    const r = S.v.map((x) => Math.trunc(x * pct / 100));
    S.scene = 2; resetScene(r[0], r[1], r[2], floor);
  }
  function applyDynamics(held, reduced) {
    const n = L().unlocked;
    for (let i = 0; i < n; i++) {
      const rise = reduced ? reducedRise(i) : RISE[i];
      const decay = reduced ? reducedDecay(i) : DECAY[i];
      if (held && i === S.active) S.v[i] = Math.min(1000, S.v[i] + rise);
      else S.v[i] = Math.max(0, S.v[i] - (held ? Math.trunc(decay / 2) : decay));
    }
  }
  function demoBits() {
    if (S.screen === 0) return INPUT.confirm;
    if (S.screen === 1) return INPUT.confirm;
    if (S.screen !== 3) return INPUT.confirm;
    if (S.sceneEnding) return 0;
    if (S.phase === 1) { S.demoPeakWait++; return S.demoPeakWait >= 30 ? INPUT.confirm : 0; }
    S.demoSwitchTimer++;
    if (S.demoSwitchTimer >= 30) {
      S.demoSwitchTimer = 0;
      const n = L().unlocked; let low = 0;
      for (let i = 1; i < n; i++) if (S.v[i] < S.v[low]) low = i;
      S.active = low;
    }
    const d = depth();
    if (S.phase === 2) {
      if (S.sensitivity > 700) return 0;
      if (d > L().targetDepth + 180) return 0;
      return INPUT.tap;
    }
    return d > 985 ? 0 : INPUT.tap;
  }
  function stepPlay(bits) {
    if ((bits & INPUT.pause) !== 0 && !S.sceneEnding) { S.screen = 4; return; }
    if (S.sceneEnding) { S.endTimer--; if (S.endTimer <= 0) advanceScene(); return; }
    S.timeLeftSteps--;
    if (S.timeLeftSteps <= 0) { S.screen = 7; S.eventCode = 8; return; }
    const n = L().unlocked;
    if ((bits & INPUT.down) !== 0) { S.active = (S.active + 1) % n; S.eventCode = 7; }
    if ((bits & INPUT.up) !== 0) { S.active = (S.active + n - 1) % n; S.eventCode = 7; }
    const held = (bits & INPUT.tap) !== 0;
    S.score += Math.trunc(depth() / 100);
    if (S.phase === 0) {
      applyDynamics(held, false);
      if (depth() >= PEAK_THRESHOLD && S.continuationsScene === 0) {
        S.phase = 1; S.peakTimer = L().peakWindowSteps; S.demoPeakWait = 0; S.eventCode = 1;
      }
    } else if (S.phase === 1) {
      if ((bits & INPUT.confirm) !== 0) {
        S.phase = 2; S.continuationsScene = 1; S.continuationsTotal++;
        S.sensitivity = 300; S.eventCode = 2; return;
      }
      if ((bits & (INPUT.stop | INPUT.back)) !== 0) { endScene(2); return; }
      S.peakTimer--;
      if (S.peakTimer <= 0) { endScene(2); return; }
      if (depth() < PEAK_COLLAPSE) S.phase = 0;
    } else {
      applyDynamics(held, true);
      const cap = S.assist ? 1250 : 1000;
      S.sensitivity = clamp(S.sensitivity + (held ? 5 : -3), 0, cap);
      if (depth() >= L().targetDepth) S.holdMs += 16;
      if (S.sensitivity >= cap) { endScene(3); return; }
      if (S.holdMs >= L().holdNeedMs) { endScene(1); return; }
      if (depth() < 200) { S.lowSteps++; if (S.lowSteps > 188) endScene(4); }
      else S.lowSteps = 0;
    }
  }
  function step(bitsIn) {
    S.eventCode = 0; S.frame++;
    const bits = S.demo ? demoBits() : (bitsIn >>> 0);
    switch (S.screen) {
      case 0: if ((bits & INPUT.confirm) !== 0) S.screen = 1; break;
      case 1:
        if ((bits & INPUT.up) !== 0) S.menuCursor = (S.menuCursor + 2) % 3;
        if ((bits & INPUT.down) !== 0) S.menuCursor = (S.menuCursor + 1) % 3;
        if ((bits & INPUT.confirm) !== 0) {
          if (S.menuCursor === 0) startLevel(S.level);
          else if (S.menuCursor === 1) S.screen = 2;
          else S.screen = 5;
        }
        break;
      case 2: if ((bits & (INPUT.confirm | INPUT.back)) !== 0) S.screen = 1; break;
      case 3: stepPlay(bits); break;
      case 4:
        if ((bits & INPUT.confirm) !== 0) S.screen = 3;
        else if ((bits & INPUT.back) !== 0) S.screen = 1;
        break;
      case 5:
        if ((bits & INPUT.up) !== 0) S.settingsCursor = (S.settingsCursor + 1) % 2;
        if ((bits & INPUT.down) !== 0) S.settingsCursor = (S.settingsCursor + 1) % 2;
        if ((bits & INPUT.confirm) !== 0) {
          if (S.settingsCursor === 0) S.calmWeather = !S.calmWeather;
          else S.assist = !S.assist;
        }
        if ((bits & INPUT.back) !== 0) S.screen = 1;
        break;
      case 6: case 7: if ((bits & INPUT.confirm) !== 0) S.screen = 1; break;
    }
  }

  // ---- draw list (mirrors the C# emitter) ----
  function drawText() {
    const ops = ["r 0 0 960 960 0"];
    const [rain, stars, mist] = weather(depth(), S.phase, S.calmWeather);
    for (let i = 0; i < stars; i++) {
      const x = ((S.seed * 31 + i * 173 + Math.trunc(S.frame / 8)) % 940) + 10;
      const y = ((S.seed * 17 + i * 269) % 300) + 10;
      ops.push(`c ${x} ${y} 2 7`);
    }
    for (let i = 0; i < rain; i++) {
      const x = ((S.seed * 13 + i * 211 + S.frame * 3) % 940) + 10;
      const y = ((S.seed * 7 + i * 97 + S.frame * 11) % 880) + 40;
      ops.push(`l ${x} ${y} ${x - 6} ${y + 22} 2 4`);
    }
    for (let m = 0; m < mist; m++) ops.push(`r 0 ${640 + m * 70} 960 26 9`);
    const d = depth();
    if (S.screen === 0) {
      ops.push("t 300 300 40 1 SECOND WAVE");
      ops.push("t 330 400 20 2 18+ ONLY. ADULTS ONLY.");
      ops.push("t 240 450 20 3 A CONTINUATION RIG, PLAYED BY HOLDING ON");
      ops.push("t 240 500 20 3 PAST THE FIRST PEAK. PRIMA, WAVE C.");
      ops.push("t 330 580 20 1 PRESS ENTER TO CONFIRM");
    } else if (S.screen === 1) {
      ops.push("t 300 180 40 1 SECOND WAVE");
      ops.push("t 300 260 20 3 PRIMA AND THE CONTINUATION RIG");
      const items = ["PLAY", "HOW TO PLAY", "SETTINGS"];
      items.forEach((it, i) => ops.push(
        `t 380 ${380 + i * 60} 20 ${i === S.menuCursor ? 1 : 2} ${i === S.menuCursor ? "+ " : "  "}${it}`));
      ops.push(`t 240 640 20 3 RECORD: ${S.recordContinuations} CONTINUATIONS - ${S.recordStops} STOPS - ${S.recordScenes} LEVELS DONE`);
      ops.push("t 240 700 20 3 UP/DOWN CHOOSE - ENTER CONFIRM");
    } else if (S.screen === 2) {
      ops.push("t 330 120 40 1 HOW TO PLAY");
      const lines = [
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
      ];
      lines.forEach((ln, i) => ops.push(`t 130 ${200 + i * 48} 20 2 ${ln}`));
      ops.push("t 300 840 20 1 ENTER - BACK TO TITLE");
    } else if (S.screen === 5) {
      ops.push("t 360 180 40 1 SETTINGS");
      ops.push(`t 240 340 20 ${S.settingsCursor === 0 ? 1 : 2} ${S.settingsCursor === 0 ? "+ " : "  "}CALM WEATHER: ${S.calmWeather ? "ON" : "OFF"}`);
      ops.push(`t 240 420 20 ${S.settingsCursor === 1 ? 1 : 2} ${S.settingsCursor === 1 ? "+ " : "  "}SENSITIVITY ASSIST: ${S.assist ? "ON" : "OFF"}`);
      ops.push("t 200 560 20 3 ASSIST RAISES THE SENSITIVITY CAP.");
      ops.push("t 200 610 20 3 SETTINGS ARE NEVER GATED BY PROGRESS.");
      ops.push("t 260 700 20 3 ENTER TOGGLE - BACKSPACE BACK");
    } else if (S.screen === 3 || S.screen === 4) {
      ops.push(`c 480 400 ${60 + Math.trunc(d / 6)} 4`);
      ops.push(`c 480 400 ${30 + Math.trunc(d / 10)} 1`);
      ops.push("c 480 400 12 2");
      const phaseName = S.phase === 0 ? "BUILD" : S.phase === 1 ? "PEAK" : "CONTINUATION";
      ops.push(`t 60 50 20 1 ${L().name} - SCENE ${S.scene}/2 - ${phaseName}`);
      ops.push(`t 60 90 20 2 DEPTH ${d} - TARGET ${S.phase === 2 ? L().targetDepth : PEAK_THRESHOLD}`);
      ops.push(`t 700 50 20 2 TIME ${Math.trunc(S.timeLeftSteps * 16 / 1000)}S`);
      ops.push(`t 700 90 20 2 SCORE ${S.score}`);
      ops.push("r 60 140 840 18 9");
      ops.push(`r 60 140 ${Math.trunc(d * 840 / 1000)} 18 1`);
      const px = 60 + Math.trunc(PEAK_THRESHOLD * 840 / 1000);
      ops.push(`l ${px} 132 ${px} 166 3 8`);
      if (S.phase === 1) {
        ops.push("t 200 200 20 8 THE PEAK. ENTER - CONTINUE. B - STOP.");
        ops.push(`t 330 240 20 2 DECIDE IN ${Math.trunc(S.peakTimer * 16 / 1000) + 1}S`);
      }
      if (S.phase === 2) {
        const cap = S.assist ? 1250 : 1000;
        ops.push(`t 60 200 20 5 SENSITIVITY ${S.sensitivity} / ${cap}`);
        ops.push("r 60 235 840 12 9");
        ops.push(`r 60 235 ${Math.trunc(S.sensitivity * 840 / cap)} 12 5`);
        ops.push(`t 60 265 20 2 HELD IN BAND ${S.holdMs} / ${L().holdNeedMs} MS`);
      }
      if (S.sceneEnding) {
        const msg = S.endKind === 1 ? "THE SECOND WAVE, RIDDEN."
          : S.endKind === 2 ? "STOPPED. THE COST IS RECORDED."
          : S.endKind === 3 ? "SETTLED AT THE CAP." : "FADED BELOW THE FLOOR.";
        ops.push(`t 240 500 20 1 ${msg}`);
      }
      const names = ["PULSE", "SUSTAIN", "SURGE"];
      const n = L().unlocked;
      for (let i = 0; i < 3; i++) {
        const x = 180 + i * 240; const live = i < n;
        ops.push(`r ${x} 780 160 14 9`);
        if (live) ops.push(`r ${x} 780 ${Math.trunc(S.v[i] * 160 / 1000)} 14 ${i === S.active ? 1 : 4}`);
        ops.push(`t ${x} 810 20 ${live ? (i === S.active ? 1 : 2) : 7} ${i === S.active && live ? "+ " : "  "}${live ? names[i] : "LOCKED"}`);
        if (live) ops.push(`t ${x} 845 20 3 ${S.v[i]}`);
      }
      ops.push("t 130 910 20 3 HOLD TO DRIVE - UP/DOWN CHANNEL - P PAUSE");
      if (S.screen === 4) {
        ops.push("r 240 340 480 220 9");
        ops.push("t 400 380 40 1 PAUSED");
        ops.push("t 300 470 20 2 ENTER RESUME - BACKSPACE TITLE");
      }
    } else {
      const victory = S.screen === 6;
      ops.push(`t ${victory ? 330 : 320} 260 44 1 ${victory ? "VICTORY" : "TIME OUT"}`);
      ops.push(`t 240 380 20 2 ${L().name} - BOTH SCENES CLOSED`);
      ops.push(`t 240 440 20 2 CONTINUATIONS ${S.continuationsTotal} - STOPS ${S.stopsTotal} (COST ${S.stopCostTotal})`);
      ops.push(`t 240 500 20 2 SCORE ${S.score}`);
      ops.push("t 300 620 20 1 ENTER - BACK TO TITLE");
    }
    return ops.join("\n");
  }

  return {
    startLevel,
    step,
    screen: () => S.screen,
    levelIdx: () => S.level,
    scene: () => S.scene,
    phase: () => S.phase,
    depth,
    channel: (i) => S.v[i],
    sensitivity: () => S.sensitivity,
    holdMs: () => S.holdMs,
    score: () => S.score,
    continuations: () => S.continuationsTotal,
    stops: () => S.stopsTotal,
    stopCost: () => S.stopCostTotal,
    endKind: () => S.endKind,
    event: () => S.eventCode,
    applySettings(calm, assist) { S.calmWeather = !!calm; S.assist = !!assist; },
    settingsBits: () => (S.calmWeather ? 1 : 0) | (S.assist ? 2 : 0),
    applyRecord(c, st, sc) {
      S.recordContinuations = clamp(Math.trunc(c) || 0, 0, 99);
      S.recordStops = clamp(Math.trunc(st) || 0, 0, 99);
      S.recordScenes = clamp(Math.trunc(sc) || 0, 0, 99);
    },
    recordBits: () => (S.recordContinuations & 0xFF) | ((S.recordStops & 0xFF) << 8) | ((S.recordScenes & 0xFF) << 16),
    frameText: drawText,
    frame() { return { palette: PALETTE, ops: parseDrawList(drawText()) }; },
  };
}

/// Parse the draw-list line protocol into raster/canvas ops.
export function parseDrawList(text) {
  const ops = [];
  for (const line of text.split("\n")) {
    if (!line) continue;
    const kind = line[0];
    if (kind === "t") {
      const parts = line.split(" ");
      ops.push(["t", +parts[1], +parts[2], +parts[3], +parts[4], parts.slice(5).join(" ")]);
    } else {
      const parts = line.split(" ").map(Number);
      ops.push([kind, ...parts.slice(1)]);
    }
  }
  return ops;
}
