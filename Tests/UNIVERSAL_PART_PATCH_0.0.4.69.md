# StretchScale 0.0.4.69 — all-parts ModuleManager patch

1. Remove any previous `GameData/StretchScale` PatchManager folder and bundled `Patches/ScaleExponents.cfg` from the KSP installation before updating.
2. With ModuleManager installed, launch KSP and verify `ModuleStretchScale` appears exactly once on a stock uncrewed fuel tank.
3. Check a crewed command pod and a modded cockpit: each should have exactly one `ModuleStretchScale` without installing or enabling PatchManager.
4. Check a part that already declares `ModuleStretchScale`; it must not receive a duplicate.
5. Verify save/reload, editor scale and IVA seat capacity safeguards for crewed parts.
6. With and without TweakScale installed, test any configurations relying on global exponent rules. StretchScale no longer ships its own `TWEAKSCALEEXPONENTS` definitions.
7. Run `python Tests/run_regressions.py`; if .NET 8 SDK is available, run `python Tests/run_regressions.py --require-dotnet`.

Offline source checks cannot validate the final ModuleManager patch application inside KSP.
