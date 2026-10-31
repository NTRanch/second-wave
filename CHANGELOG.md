# Changelog — Second Wave

All notable changes, newest first. Format: Keep a Changelog.

## [1.0.0] — 2026-10-30

- The full Continuation mechanism group, playable in the single-file
  `web/index.html`: the three-phase model (Build / Peak / Continuation)
  driven by a hold/release stimulus rig with three per-channel presets
  (Pulse, Sustain, Surge — distinct rise/decay curves).
- STOP at the peak ends the scene at a recorded cost (150 points, and
  the Rig remembers less); continuing is the skill path: one
  continuation only, on the reduced curve (rise ×0.55, decay ×0.60),
  riding the target band while the sensitivity meter climbs to its
  soft cap (settle outcome; assist setting raises the cap).
- Two scenes per level; scene 2 opens with the Rig remembering scene 1
  (per-channel residue + a raised floor on the continuation path),
  persisted across sessions as a lifetime record via the core bridges.
- Trance-Deep weather as a pure function of depth and phase.
- Three levels: First Swell (1 channel), The Reduced Curve (2 channels,
  tighter peak window), Second Wave (3 channels, tightest window,
  highest target). Eight screens: 18+ gate, title, how-to, gameplay,
  pause, settings, victory, game over.
- Native core: deterministic fixed-step C# (`core/SecondWave.Core`,
  dotnet 8, no Unity dependency) with a 28-check console test suite
  (half validation / half adversarial); the shipped JS realization is
  gated exact against conformance vectors dumped from the C# core
  (903 trace points). Unity WebGL packaging is the primo follow-up
  (BREAKAGE-LOG B-1) — not faked.
- Per-level preview videos in `docs/previews/`, rendered from the
  shipped build's demo mode.

## [Unreleased]

- Repo scaffolded 2026-10-30: bloch roadmap (kind=game, lane=restricted),
  planning, per-game skill, baseline C# / Unity config, NTRanch-themed mdBook,
  brand-asset slots.
