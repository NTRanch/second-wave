// conformance.mjs — the JS realization must reproduce the C# core's
// dumped state traces EXACTLY for the stateless scripted input pattern
// (the same pure function of step index as SecondWave.Vectors).
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { createGame, INPUT } from "../web/game.mjs";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const vectors = JSON.parse(readFileSync(join(root, "conformance/vectors.json"), "utf8"));

function scriptBits(step) {
  if (step === 0 || step === 1) return INPUT.confirm;
  const cycle = step % 240;
  let bits = 0;
  if (cycle < 150) bits |= INPUT.tap;
  if (cycle === 150) bits |= INPUT.down;
  if (step % 50 === 0) bits |= INPUT.confirm;
  return bits;
}

let failures = 0;
for (const { level, trace } of vectors.levels) {
  const game = createGame({ seed: vectors.seed, level, demo: false });
  const wanted = new Map(trace.map((row) => [row[0], row]));
  let checked = 0;
  for (let step = 0; step < 6000; step++) {
    game.step(scriptBits(step));
    const row = wanted.get(step);
    if (!row) continue;
    checked++;
    const got = [step, game.screen(), game.scene(), game.phase(), game.depth(),
      game.channel(0), game.channel(1), game.channel(2), game.sensitivity(),
      game.continuations(), game.holdMs(), game.score()];
    if (JSON.stringify(got) !== JSON.stringify(row)) {
      console.error(`level ${level + 1} step ${step}: JS ${JSON.stringify(got)} != C# ${JSON.stringify(row)}`);
      failures++;
      if (failures > 4) break;
    }
  }
  console.log(`level ${level + 1}: ${checked} trace points checked`);
}
if (failures) { console.error(`conformance: ${failures} mismatch(es)`); process.exit(1); }
console.log("conformance: JS realization matches the C# core exactly on all levels");
