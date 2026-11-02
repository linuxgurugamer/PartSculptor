# 0.0.4.54 verification: linked face movement and configurable snap

1. In VAB, attach a stock FL-T400, select sixth Morph tool, then the tank.
2. Drag one orange corner normally: the orange face X/Z dimensions change (taper). Cyan face unaffected.
3. Alt-drag the orange corner: tilt the orange face.
4. Hold Shift+Alt **before clicking**, then drag an orange corner: all four orange handles translate by exactly the same X/Z displacement; the face width/depth and tilt stay fixed; cyan remains fixed. The tank should become an oblique/sheared tank with parallel face corners. The vertical Y height stays fixed.
5. Open Vessel Inspector > Settings: choose 0.01, click Save, and repeat linked drag; then try 0.05, 0.10 and 0.25. Small movements should round to the chosen step, measured from the drag start.
6. Shift-drag without Alt: normal taper with current snap increments. No modifier: smooth taper. Alt alone: smooth face tilt.
7. Shift+Alt drag left and right across the start position should not progressively add offset; it should return exactly to initial shape at zero drag.
8. Save craft, reload, and verify offsets and current geometry; check flight scene restoration. Verify symmetry and any attached nodes move with morphed face.
9. During Shift+Alt drag press Esc or right mouse button: original face translation must be restored.
10. Click Reset face morph in PAW and verify offsets/tilt/dimensions return to native geometry.
11. Check old crafts without offset fields load as before. UserSettings.cfg without morphSnapIncrement defaults to 0.05.

Note: Offline tests cannot establish KSP in-game rendering or collider behavior.
