# PartSculptor 0.0.4.72 — Morph edge midpoint manual test

1. In the VAB place a stock tank and activate Morph. Expect **16** foreground handles: four corners and four smaller edge midpoints on each face (orange bottom, cyan top).
2. Rotate the view and confirm all midpoint handles remain at the midpoint of their edge, including after taper, tilt, and whole-face translation.
3. Turn **Lock X/Z** off. Drag X-/X+ edge midpoint: only face width changes. Drag Z-/Z+ edge midpoint: only face depth changes. Corner handles still change both dimensions.
4. Hold **Alt** and drag each midpoint: X-edge midpoint changes only X tilt, Z-edge midpoint only Z tilt. Test on an already tilted face.
5. Enable **Lock X/Z**. Midpoint drag should preserve face width/depth ratio. Test Shift snap and verify it preserves the ratio too.
6. Test **Shift+Alt** translate and **Ctrl+Alt** radial resize using midpoint handles; ensure the two faces remain independently editable.
7. Attach a second tank above the part, and confirm midpoint markers remain visible and selectable through the other part using the foreground GUI.
8. Save, reload, clone (Alt-click), and reset the tank. Morph parameters should persist or reset normally; no new serialized fields should appear.
9. Switch between Stretch and Morph, deselect the part, exit the editor: no gizmo remnants or stuck mouse lock.

> Source-level tests are not a substitute for KSP editor/runtime validation.
