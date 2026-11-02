### 0.0.4.65 — Both editor gizmos stay visible in front of attached parts

See `Tests/BOTH_GIZMO_VISIBILITY_0.0.4.65.md` for a two-tank visual and drag test.
The `test_face_morph_occlusion_464.py` suite now verifies 0.0.4.65 behavior: an always-painted morph cage and on-top Stretch axis markers using the editor addon callback.

### 0.0.4.64 — Face-morph handle occlusion

Run `python Tests/run_regressions.py` and see
`Tests/FACE_MORPH_OCCLUSION_0.0.4.64.md`. This checks the above-3D
screen-space overlay, 8 hit-aligned corners, 12 cage edges, visual priorities,
raycast passthrough, cleanup and fallback behavior.

# StretchScale regression tests (KSP 1.12.x)

These are **manual in-game tests**. They have not been run in this environment.

## Basic tank scaling
1. Add a fresh FL-T400 (180 LiquidFuel / 220 Oxidizer), scale Y from 1 to 2.
2. Confirm capacity 360 LF / 440 Ox and the reported volume multiplier is 2x.
3. Set the tank to 50% full and scale Y from 2 to 4. Confirm 720 LF / 880 Ox capacity, 50% fill.
4. Set Y back to 1. Confirm stock capacities (no cumulative rounding).
5. Save/reload craft with Y=2. Confirm capacity remains doubled, not quadrupled.

## Presets and part stats
1. From X=Y=Z=1, choose `2x uniform`: all axes should be 2 and volume 8x.
2. Choose `1x uniform` to reset; choose `Double Y`, then `Half Y`. Confirm reversible values.
3. Starting at `Custom`, click the preset **right arrow repeatedly**: confirm it advances to each of `0.5x uniform`, `1x uniform`, `2x uniform`, `4x uniform`, `Double Y`, and `Half Y`, and shows each selected name (not `Custom`). Click the **left arrow repeatedly** and confirm it moves back across the choices.
4. Select `Double Y` from an initial scale Y=1 and click **Repeat preset** twice: Y should become 2, then 4, then 8. Select `Half Y` and repeat twice to return Y to 1.
5. Edit an axis manually after selecting a preset. Confirm the selector still shows the **last chosen preset** and its arrows remain functional; the current axis values remain authoritative.
6. Verify dry mass/cost estimate increases with volume; no direct writes to `part.mass` or `part.cost`.
7. In Career Mode check reported editor total includes dry-cost and fuel-cost adjustments.

## Symmetry / children / editor performance
1. With symmetric tanks, resize one and confirm both storage values/geometry change identically.
2. Turn on Stretch children; resize a parent with multiple descendants and check scales and attachment locations.
3. Confirm Undo, craft save/reload and switching between VAB and SPH do not duplicate resource multipliers.
4. Monitor log for repeated OnPartResourceChanged and editor rebuild events when idle; stock parts should not continuously refresh resources.

## Optional mod compatibility
1. With TweakScale present, scale uniformly then stretch Y; compare displayed mass/cost and resources.
2. For each installed tank switcher, switch fuel types after stretching, then re-open the part window and compare capacities.
3. Save/reload a switched, stretched tank; verify neither old resource types nor old capacities come back.
4. Try a tank switch that changes an existing `PartResource.maxAmount` in place, not just swapping resources.

If a specialized switcher changes the capacity *after* StretchScale applies it, a dedicated switcher integration may be required.

### 0.0.4.63 — Face-morph Lock X/Z

Run `python Tests/run_regressions.py` and see `Tests/MORPH_XZ_LOCK_0.0.4.63.md` for the in-game test checklist. Regression coverage checks per-part persistence, default-on user configuration, proportional/snap math, Alt modifier priority, and removal of the settings-path label.
