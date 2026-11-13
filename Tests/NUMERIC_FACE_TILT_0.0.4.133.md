# PartSculptor v0.0.4.133 — Numeric face tilt manual test

1. Install in KSP 1.12.x, place one stock FL-T400, open its PAW.
2. In **Tilt face**, choose **Top**. Enter X = 39.6 degrees and Z = 0; click **Apply face tilt**. Confirm the Top face tilt readout shows ~39.6°, Bottom remains flat.
3. Change **Tilt face** to Bottom. Confirm the inputs reset to 0/0. Set X = -12.5°, Z = +7.5° and Apply. Check signed bottom X/Z readouts and total. Switch back to Top; confirm the original 39.6° values are displayed.
4. Edit X or Z without clicking Apply. Confirm the actual face geometry and displayed face-angle readout do **not** change. Change faces; confirm un-applied values are discarded and replaced with the selected face's current angles.
5. Set a value that exceeds `maxFaceTilt` (e.g. 80° at default 2.5 slope); click Apply. Verify an error appears and the actual face does not change. Existing values must not be silently clamped.
6. Morph a face numerically; Ctrl+Z should undo the entire change. Ctrl+Y should redo it. Check face collider and node positions.
7. With two FL-T400s, enable **Copy attached morph steps**; numerically tilt the lower tank's connected Top face and confirm the upper Bottom face receives the same mirrored tilt step and child body movement that gizmo dragging performs. Repeat with **Match attached faces** instead.
8. Apply numeric tilts to symmetric tanks. Their faces should follow the same symmetry behavior as the original gizmo. Test undo.
9. Change scaleX, scaleY and scaleZ, then Apply 15° to a face. The displayed angle should remain 15°; the stored tilt slope should reflect scale and face width/depth.
10. Reopen and reload a craft; saved face geometry should be restored, while the numerical PAW controls initialize from the selected face's saved shape.

Numeric degree inputs are local face inclinations, not the part's world orientation. In-game execution and compilation are not covered by offline checks.
