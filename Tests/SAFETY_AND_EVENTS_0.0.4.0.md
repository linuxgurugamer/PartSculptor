# PartSculptor 0.0.4.0 — editor performance and scaling safety tests

Test with KSP 1.12.x. This is a manual regression checklist, not proof of an in-game test. **Back up crafts and saves first.**

## Baseline and persistence

1. With default `Settings.cfg`, place a new FL-T400. Check that Scale preset initially says `1x uniform`.
2. Set X=1, Y=2, Z=1. The tank's capacity and stored amount should double while retaining its fill percentage.
3. Set X=1, Y=1, Z=1. Verify original capacity is restored; repeat 3–4 times without drift.
4. Save and reload the craft; the chosen dimensions, resources, and saved baselines should remain consistent.
5. Use Reset scale, Swap X/Z, Double Y, Half Y, Repeat preset, and locked proportions.
6. Verify mass, cost, volume, and Scale safety displays update without needing to close/reopen the PAW.

## Event-driven checks

1. Change a tank's dimensions once. Watch `KSP.log`: it should not emit a continuous stream of `OnPartResourceChanged`-related messages.
2. Use a tank switcher that issues `OnPartResourceChanged`. Switching fuel/type should update its capacity on the next editor update.
3. With a *silent* tank switcher, wait up to `resourceFallbackSeconds` (plus 1.55 seconds of staggering) after the switch. Verify the capacity is corrected.
4. Repeat with several dozen stock tanks. Ordinary unmodified tanks should not receive periodic full capacity scans (unless `checkAllResourcesPeriodically=true`).
5. For an unrecognized silent switcher, turn `checkAllResourcesPeriodically=true`, restart KSP, and verify it is picked up by the fallback.

## Batched editor notifications

1. Make a craft with ten or more PartSculptor descendants and a symmetry set; enable Stretch children.
2. Resize the root. Verify each child and symmetry counterpart moves/scales as expected.
3. Use a debugger or temporarily add logging inside `LateUpdate` at the `GameEvents.onEditorShipModified.Fire` call. Verify that an edit generates one PartSculptor-originated ship notification instead of a notification per part.
4. Verify the editor vessel's mass/cost UI and staging data eventually refresh following the deferred (end-of-frame) notification.

## Limits and warnings

1. Configure `maxMassMultiplier=4` and `maxVolumeMultiplier=4`; restart KSP.
2. For a stock tank, set X=2 and Y=2. The combined multiplier of 4 is allowed.
3. Attempt Z=2 (8x requested). It should be clamped to stay within 4x; `Scale safety` should say that a limit was reached.
4. Select `4x uniform` (64x requested). All three axes should be reduced proportionally; no axis should drop below `minScale`.
5. Configure `warnMassMultiplier=2`, `warnVolumeMultiplier=2`, and generous maximums. Check for visible warnings at 2x and at 0.5x; verify clearing after resetting to 1x.
6. Repeat with optional TweakScale installed: the **total** volume multiplier includes TweakScale^3, while the PartSculptor mass factor is X*Y*Z. A TweakScale-only size change should not unexpectedly reset X/Y/Z.
7. Load a craft that was saved beyond the newly configured caps. Loading should preserve the dimensions (the caps apply only to new edits).
8. If existing TweakScale size makes the minimum allowed dimensions exceed the cap, editing should be refused without invalid geometry.

## Restrictions

1. Set all `restrict*` options to true and restart KSP.
2. Place a wheel, docking port, and animated/deployable part. Check that stretch controls/presets are unavailable and `Scale safety` reports the module causing the restriction.
3. Place a stock fuel tank without those modules. It should remain fully resizable.
4. Load an older saved craft containing a stretched wheel/docking port. Its saved geometry should remain intact; new editing should be restricted.
5. With Stretch children enabled, verify that a restricted descendant is skipped and propagation stops at that part.

## Important limitations

- No KSP assemblies were provided here, so a compile against `Assembly-CSharp.dll` and the installed Unity assemblies is required.
- The module restriction lists are name-based and not exhaustive. A specialized wheel/animation module may need its prefix added.
- Silent third-party module changes not accompanied by a part resource event need either a recognized dynamic provider or `checkAllResourcesPeriodically=true`.
- With unusual rotations, non-uniform child stretching is an approximation and cannot represent arbitrary shear.
