// perf.mjs — gates R-401: one step of simulation + draw-list emission
// must stay far inside the 16 ms step budget. Budget of record: 10,000
// steps in under 2 s on this machine; shipped page under 512 KB.
import { statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { createGame } from "../web/game.mjs";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const game = createGame({ seed: 31, level: 2, demo: true });
game.startLevel(2);
const t0 = performance.now();
for (let i = 0; i < 10000; i++) { game.step(0); game.frameText(); }
const ms = performance.now() - t0;
const size = statSync(join(root, "web/index.html")).size;
console.log(`perf: 10000 steps+draw in ${ms.toFixed(1)} ms (${(ms / 10000).toFixed(3)} ms/step); page ${size} bytes`);
if (ms >= 2000) { console.error("perf: step budget exceeded"); process.exit(1); }
if (size >= 512 * 1024) { console.error("perf: page size budget exceeded"); process.exit(1); }
console.log("perf: within budget");
