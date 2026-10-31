// settings-check.mjs — gates R-202/R-203/R-403: settings AND record
// bridges round-trip, and the settings screen is reachable from boot
// with no progression (safety/accessibility options never gated).
import { createGame, INPUT } from "../web/game.mjs";

const game = createGame({ seed: 7, level: 0, demo: false });
function steps(n, bits = 0) { for (let i = 0; i < n; i++) game.step(bits); }
function fail(msg) { console.error(`settings-check: ${msg}`); process.exit(1); }

if (game.screen() !== 0) fail("boot screen is not the 18+ gate");
game.applySettings(true, false);
if (game.settingsBits() !== 1) fail("applySettings(calm) did not round-trip");
game.applySettings(false, true);
if (game.settingsBits() !== 2) fail("applySettings(assist) did not round-trip");
game.applySettings(true, true);
game.applyRecord(2, 1, 3);
if (game.recordBits() !== (2 | (1 << 8) | (3 << 16))) fail("applyRecord did not round-trip");
game.applyRecord(5000, -3, 12);
if (game.recordBits() !== (99 | (0 << 8) | (12 << 16))) fail("applyRecord did not clamp");

steps(1, INPUT.confirm);
if (game.screen() !== 1) fail("confirm did not leave the gate");
steps(1, INPUT.down); steps(1, INPUT.down);
steps(1, INPUT.confirm);
if (game.screen() !== 5) fail(`settings not reachable from title (screen=${game.screen()})`);
steps(2);
if (!game.frameText().includes("CALM WEATHER: ON")) fail("settings screen does not show restored CALM WEATHER");
if (!game.frameText().includes("SENSITIVITY ASSIST: ON")) fail("settings screen does not show restored ASSIST");
console.log("settings-check: settings + record bridges round-trip; settings reachable ungated; restored values render");
