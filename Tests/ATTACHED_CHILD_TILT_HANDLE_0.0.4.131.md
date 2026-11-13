# PartSculptor 0.0.4.131 — Attached child face tilt-handle direction

Baseline: version 0.0.4.130. This update changes *input mapping* only, not the saved face geometry or mirrored attachment math.

1. In VAB, stack two stock FL-T400 tanks. The lower tank is the parent, upper tank is the child. Enable **Copy attached morph steps** and select the **Morph** tool.
2. Select the **bottom face of the upper child** (the end attached to the lower tank). Hold **Alt**, drag its right edge *up* on screen. The selected handle should move up, not down; dragging down should also move down. Repeat on the left edge and Z-edge midpoints, and reverse direction within a single drag.
3. Tilt beyond 30 degrees if the selected `maxFaceTilt` allows. Ensure the handle direction does not suddenly reverse while the child body rotates. Verify the mirrored connecting face and child body rotation still occur, and the degree readouts still report the local morph angles.
4. Reset both faces. Tilt the **top face of the lower parent**; that handle should still move with the mouse and the child should follow. Also tilt the **top free face of the upper child**: drag behavior should remain normal.
5. Turn **Copy attached morph steps OFF** and repeat the upper child's attached bottom-face drag. Also test with **Match attached faces** only and with both modes off. Drag behavior should remain unchanged from the previous version in those modes.
6. Try the attached upper tank rolled 90 and 180 degrees and tilt both X and Z directions. Test Esc/right-click cancellation and one Ctrl+Z per completed drag.
7. Check that normal corner/midpoint resizing and Shift+Alt face translation are unaffected.

Actual KSP editor results must be verified in-game. Offline checks validate the selected conditional sign and code paths, not the Unity camera or editor rotation.
