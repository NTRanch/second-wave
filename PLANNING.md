# Planning — Second Wave

- **Program:** Torogao Jam Program (D-333), week 12 of 14, Wave C — Mini-games: the Crescendo set
- **Cast:** Prima
- **Language:** C# / Unity (tiger-style-csharp)
- **Canon core:** Continuation — her step-4 game, the three-phase model as a core loop.

## Mechanism group (D-334: primary + coupled + persistence/revelation + feel)

- The three-phase orgasm model with continuation (8473238 floor) — stopping is a choice with a cost, continuing is a skill
- The Continuation Rig core + per-channel presets
- Overstimulation staging in the Don't Stop shape — one continuation past the first peak, at the Rig's reduced curve
- Multi-scene persistence in miniature — the second scene opens with the Rig remembering the first
- Trance-Deep weather at depth

## Validates

The continuation floor as a loop, rig persistence as something a player can notice, and the Rig's semantics surviving re-expression in Unity (read against W4/W11's Rust findings).

## Port-back

Verdict filed against Prima's ladder record; continuation-curve findings; C#/Unity verdict, second pass.

## Delivery

Unity WebGL build packed into the single file (loader + wasm inlined). Named risk: build size; if the packed file breaches itch limits, ship the WebGL build as the jam's downloadable twin and a reduced-core shell as the page build — decided at this week's preflight, never silently. Toolchain home: primo (Unity lives there).

## Open preflight items

- GJ-06-class start gate for this week's toolchain (later weeks gate their
  own preflight; only W1's gate covers the program start).
- GJ-07: 18+ flag, content warnings listed, itch ToS screen — verified live
  at entry (the NSFW allowance clause is Owner-reported; re-verify on page).
- Engine-research chapter in the book: confirm the pinned library versions
  before the first build commit.
