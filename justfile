# Second Wave — task recipes (wired at the v1.0 build, 2026-10-30).
# dotnet 8.0.425 lives at ~/workspace/tools/dotnet in the sandbox.
# The Unity WebGL packaging leg is a primo follow-up (see
# docs/BREAKAGE-LOG.md B-1); the shipped artefact is the tested C#
# core + its conformance-gated JS realization in web/index.html.

DOTNET := env_var_or_default("DOTNET", join(home_dir(), "workspace/tools/dotnet/dotnet"))

# Assemble the single-file jam entry from the JS realization + shell
build:
    node tools/assemble.mjs

# Run every gate: C# tests, vectors-vs-JS conformance, bridges, perf, playtest
check:
    {{DOTNET}} run --project core/SecondWave.Tests -c Release
    node tools/assemble.mjs
    node tools/conformance.mjs
    node tools/settings-check.mjs
    node tools/perf.mjs
    node tools/playtest.mjs

# Regenerate conformance vectors FROM the C# core
vectors:
    {{DOTNET}} run --project core/SecondWave.Vectors -c Release -- conformance/vectors.json

# Render the per-level preview videos from the shipped build (demo mode)
previews:
    node tools/capture.mjs 0 20264
    node tools/capture.mjs 1 20264
    node tools/capture.mjs 2 20264

# Build the planning book
book:
    mdbook build book
