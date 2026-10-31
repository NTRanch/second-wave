// playtest.mjs — automated playtest: the demo policy rides every
// level to victory through the continuation path on BOTH scenes,
// against the shipped JS realization. The seahawk pass on primo is the
// follow-up named in docs/BREAKAGE-LOG.md.
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { createGame } from "../web/game.mjs";

const SCREENS = ["gate", "title", "howto", "play", "pause", "settings", "victory", "gameover"];
let failures = 0;
for (let level = 0; level < 3; level++) {
  const game = createGame({ seed: 9000 + level, level, demo: true });
  game.startLevel(level);
  let steps = 0;
  while (game.screen() !== 6 && steps < 30000) { game.step(0); steps++; }
  const won = game.screen() === 6;
  const contPath = game.continuations() === 2;
  console.log(`level ${level + 1}: screen=${SCREENS[game.screen()]} steps=${steps} continuations=${game.continuations()} stops=${game.stops()} ${won && contPath ? "VICTORY" : "FAIL"}`);
  if (!won || !contPath) failures++;
}
if (failures) { console.error(`playtest: ${failures} level(s) not completed via continuation`); process.exit(1); }
console.log("playtest: all 3 levels completed via the continuation path, both scenes");
