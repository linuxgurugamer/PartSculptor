# StretchScale 0.0.4.70 — restored scale exponents

1. Install a clean StretchScale 0.0.4.70 and ModuleManager without TweakScale. Verify the bundled `ScaleExponents.cfg` produces stock-part/module `TWEAKSCALEEXPONENTS` and `TWEAKSCALEBEHAVIOR` nodes. Compare scaled resource capacities and other module values with 0.0.4.68.
2. Verify all parts (including crewed parts) get exactly one `ModuleStretchScale` from `StretchScale.cfg`.
3. Install TweakScale and verify the restored rules do not duplicate those supplied by TweakScale (`:NEEDS[!TweakScale]`).
4. Verify `GameData/PartSculptor/PatchManager` remains absent, and no PatchManager installation is needed.
5. Regression check: `python Tests/run_regressions.py`. KSP in-game tests are still required.
