# 0.0.4.127 — In-game attached-face tilt validation

1. Place two **stock FL-T400** tanks with their default sizes/scales and
   flat opposing stack faces. Toggle **Copy attached morph steps** on; turn
   **Match attached faces** off. Leave both starting face angles at 0°.
2. Alt-drag a corner of the lower tank's top face to a **39.6° total tilt**.
   The upper tank's *bottom face tilt* PAW field must read **39.6°** too
   (within approximately 0.1°, with mirrored X/Z component signs). The
   whole upper tank should still rotate to follow the attachment.
3. Reverse drag and release; verify the upper face follows back to 0°,
   without stalling near 22° or accumulating tilt on repeated drags.
4. Tilt only one component using an edge midpoint, try both directions,
   and then perform mixed X/Z tilt and both parent-to-child and
   child-to-parent edits. Repeat with a 90° roll, with symmetry and with
   an attachment grandchild. Verify only the neighbor touching face changes.
5. Change the receiver part's stretch axes or face size. The local signed
   tilt angles should track the *incremental* physical slope change, but
   the old face angle difference must remain if their starting morphs differ.
   If the required raw Tilt would exceed allowed limits, no partial unsafe
   update should occur.
6. Try reattaching a different part to the same node. The cached side
   correspondence must be replaced for the new neighbor. When an existing
   craft is loaded with an already extreme joint angle, test whether Copy
   recognizes the connection; it may need reattachment to seed its map.
7. For debugging, set `debugLogChanges = true` in
   `GameData/PartSculptor/PluginData/Settings.cfg`, restart KSP, and inspect
   `[PartSculptor]` / `mirrored face inclination` log entries. They show
   total source/receiver tilt before/after rigid rotation, scale factors,
   and face dimensions. Record a failing step with the corresponding log.

Actual KSP editor behavior cannot be validated by the offline math tests.
