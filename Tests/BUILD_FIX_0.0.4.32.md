# Build fix — 0.0.4.32

- Fixes CS0103 in `PartSculptor.cs`, `OnStartFinished()`, by fetching the part prefab in that method before calling `DockingPortScaling.Create(part, prefab)`.
- The previous local variable named `prefab` in `OnStart()` cannot be used from another method.
- No docking-port logic or scaling formulas were changed.

## Regression checks

1. Build the PartSculptor solution with the usual KSP 1.12.x references.
2. Verify CS0103 no longer occurs at the docking scaler initialization.
3. Add a stock docking port and confirm the docking values still appear in the Scaled Values inspector.
4. Run `python Tests/run_regressions.py` for automated source integration checks (plus C# tests when .NET 8 is available).
