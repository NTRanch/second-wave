# Breakage log — Second Wave

Every breakage found in playtest or build lands here with: what broke,
the evidence, the bloch attempt id, and the fix or the filed finding.
Copied to bloch at the week's close (the log is the input, bloch is
the record).

## 2026-10-30 — v1.0 build (sandbox)

- **B-1 (held, by environment): Unity WebGL packaging not run.** Unity
  lives on primo and the sandbox→primo tunnel has been down since
  03:15 PDT. Per the program contract's engine-week rule, the shipped
  artefact is the tested native C# core (`core/SecondWave.Core`, plain
  classlib, no Unity dependency) plus the JS realization of the same
  core in `web/index.html`, gated by conformance vectors dumped from
  the C# core (903 trace points, exact). The Unity WebGL leg — and the
  seahawk playtest pass — are primo follow-ups; R-904-class live
  checks and the seahawk half of R-905 stay open for that reason.
  Nothing about Unity was faked: `Assets/Scripts/Game.cs` remains the
  scaffold entry point awaiting the primo leg.
- **B-2: the peak collapsed before its window could expire.** First
  draft decayed channel values during the Peak phase; depth fell
  under the 700 collapse line in ~78 steps, so the 313-step decision
  window (and its auto-stop) was unreachable — the expiry test failed
  against a core that could never expire. Fix (design ruling): the
  peak is a held moment — values freeze during Peak; the timer alone
  is the pressure. Mirrored in the JS realization; C# suite 28/28.
- **B-3: continuation decay ×1.40 was unsustainable.** First draft's
  reduced curve cut rise to ×0.55 but RAISED decay to ×1.40; under
  the demo policy's hold/release duty cycle at the sensitivity
  ceiling, depth bled monotonically and scenes faded below the floor
  instead of closing in band (test: scene 2 opened without its raised
  floor). Fix: reduced decay is ×0.60 — the reduced curve is slower
  in both directions; sensitivity, not decay, is the continuation's
  pressure. Finding filed to the port-back with numbers.
- **B-4: raster font has no ">" glyph.** The D-221 frame inspection of
  the final MP4s showed the menu/channel cursor rendering as "?"
  (the shared rasteriser's fallback glyph). The browser's real font
  renders ">" fine; the preview videos are the deliverable that
  showed it. Fix: cursor prefix is "+" in both emitters; previews
  re-captured and re-inspected PASS.
- **B-5 (held, by design of the jam): GJ-07 live itch checks open.**
  Verified live at entry time; no entry is being submitted tonight.
  R-904 stays open. The game's own 18+ gate ships first regardless.
- **B-6 (design ruling): persistence scope.** What persists (shell
  localStorage): settings and the lifetime record (continuations,
  stops, levels closed). The Rig's scene-to-scene memory is the
  core's own state inside a level — that IS the mechanic, and it is
  gated by the C# suite + conformance, not by storage. Recorded here
  rather than silently narrowing R-202/R-203.
