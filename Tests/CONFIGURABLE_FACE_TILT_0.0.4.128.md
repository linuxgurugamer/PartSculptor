# KSP1 manual check — configurable face tilt (0.0.4.128)

1. Start with `maxFaceTilt = 0.75` in `GameData/PartSculptor/PluginData/Settings.cfg` and restart KSP.
2. Place an FL-T400 in the VAB, select Morph, and Alt-drag a face edge. Confirm the single-axis slope stops at the historical limit; the exact degree readout depends on the face dimensions and stretching.
3. Change `maxFaceTilt` to `1.5`, restart KSP, then repeat the drag on a new craft. Confirm the face can tilt noticeably farther, and the angle readout continues updating.
4. Enable **Match attached faces**, attach a second FL-T400 at a compatible stack node, and check that its touching face can match when the fitted tilt parameter exceeds 0.75. This is distinct from the separately controlled **Copy attached morph steps** behavior.
5. Verify the same configuration works for both the bottom and top faces and for negative X and Z tilt, reset, undo and redo, and symmetry.
6. Restore the setting to `0.75` and restart. Confirm new edits are limited again. Saved craft geometry should not change merely because the configuration was lowered; take care when *editing* previously steeper saved faces, since edited values are clamped to the newly configured limit.
7. Try `maxFaceTilt = -1`, `maxFaceTilt = 0`, and a nonnumeric value, restarting between tests. Confirm the mod remains usable with its default limit. A value above 4.0 is also rejected.

**Cautions:** Extreme face tilt can produce badly shaped models, colliders, or awkward attachment behavior; back up craft before testing. These tests must be performed in KSP because offline source checks cannot verify visual results.
