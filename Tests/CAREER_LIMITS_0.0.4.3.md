# Career progression regression tests — 0.0.4.3

1. Create a **new Career save**. In the level-1 VAB, open a tank PAW. Confirm `Career scale limits` displays `VAB level 1`, with axes 0.75–1.5 and volume <= 3.
2. Set X = 2: it should clamp to 1.5. Set X = Y = Z = 1.5: the volume cap should clamp the combined scale to <= 3; verify mass and fuel remain consistent.
3. Select `4x uniform`: the dimensions must respect both axis and volume caps. `Double Y` and `Repeat preset` must also respect them.
4. Upgrade the **VAB** to level 2. Reenter the VAB; max axis should become 3, max volume 27. Verify level 3 similarly.
5. Upgrade VAB only; enter SPH with SPH still at level 1. SPH should still enforce its own level-1 limits.
6. Test Science and Sandbox: the Career PAW line should be hidden and the Career axis/volume caps must not apply.
7. Open a craft saved with axes outside the current career tier. Dimensions must not change on load or in flight. Reducing such a scale towards the allowable range should work; increasing it further must not.
8. Test `Lock proportions`, multiple symmetry parts, `Stretch children`, and the scale presets when a descendant reaches its tier cap.
9. Set `enableCareerLimits = false` and relaunch KSP. Confirm that only global mass/volume safety limits apply.
10. Set an intentionally unusual limit (e.g. level-1 max volume 1.2) and verify caps are still enforced after restarting KSP.
11. If building upgrades are disabled for a Career save, verify the highest configured Career tier is used.
12. Verify no unexpected editor callbacks, ship-modified notification loops, or changes to resource capacity/amounts when the scale is rejected.

Not compiled against KSP or tested in game.
