# Second Wave

**Nice Thoughts Ranch** · Torogao Jam Program week 12 (Wave C — Mini-games: the Crescendo set) · Prima · **C# / Unity**

Continuation — her step-4 game, the three-phase model as a core loop.

An 18+ game built for the weekly Gameotron jam on itch.io (NTRanch), as one
rung of the 14-week Torogao system-validation program (D-333/D-334/D-335/D-336).
The full planning book lives in [`book/`](book/) (mdBook, NTRanch theme);
this repo's bloch roadmap is [`.ai/roadmap.md`](.ai/roadmap.md).

## Play

Open `web/index.html` in a browser (one self-contained file, runs
offline). **Hold** SPACE or the pointer to drive the active channel;
**↑/↓** switches channels (Pulse / Sustain / Surge, each with its own
rise/decay curve). Drive depth to the peak (850), then choose inside
the window: **Enter** continues past the peak on the Rig's reduced
curve — ride the target band while the sensitivity meter climbs, one
continuation only — or **B** stops, ends the scene, and pays the
recorded cost. Every level is two scenes, and scene 2 opens with the
Rig remembering scene 1: residue in every channel, and a raised floor
if you continued. **P** pauses. Demo mode: `web/index.html?demo=<0..2>&seed=<n>`.

## Levels

1. **First Swell** — one channel (Pulse), a wide 5 s peak window, target 650.
2. **The Reduced Curve** — two channels, a 4 s window, target 720.
3. **Second Wave** — all three channels, a 3 s window, target 780.

## Previews (rendered from the shipped build's demo mode)

- [Level 1 — First Swell](docs/previews/level-1-second-wave.mp4) (12 s)
- [Level 2 — The Reduced Curve](docs/previews/level-2-second-wave.mp4) (12 s)
- [Level 3 — Second Wave](docs/previews/level-3-second-wave.mp4) (12 s)

## Verify

`just check` — the C# suite (28 checks, validation + adversarial),
conformance of the shipped JS realization against vectors dumped from
the C# core (903 trace points, exact), settings/record bridges,
performance budget, and the scripted playtest. `just previews`
re-renders the videos; `just vectors` regenerates the vectors from
the C# core.

- **Delivery:** one self-contained `index.html`. The native artefact is
  the tested C# core (`core/`); the Unity WebGL packaging leg is a
  primo follow-up (Unity lives there; see `docs/BREAKAGE-LOG.md` B-1).
- **Cast/art law (D-292):** existing keeper art only; staging here is
  abstract-symbolic (drawn shapes, light, typography) by design.
- **Audio:** generated WebAudio answering input only; nothing is ever
  played aloud by an agent; the Owner presses play.

Status: **v1.0.0** (2026-10-30) — full mechanism group playable, all
sandbox gates green; seahawk playtest + GJ-07 live checks + Unity
packaging ride primo's return.
