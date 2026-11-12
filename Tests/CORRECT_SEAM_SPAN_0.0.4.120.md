# v0.0.4.120: FL-T400 mirrored Copy seam alignment

Start with **two new stock FL-T400 tanks** joined by their stack nodes. Enable **Copy attached morph steps**, disable **Match attached faces** and ordinary node-size matching. Use the stock VAB and the PartSculptor Morph tool.

1. Tilt the lower tank's upper **right** face edge downward using Alt-drag. Confirm the child's corresponding lower face gets the opposite **local** tilt and its whole body receives the additional rotation from v119.
2. Inspect the entire circumference/long X dimension of the joint from a side view. The parent should not protrude beyond the child's touching face at either the dragged edge or the opposite edge. Rotate the camera: check front/back edges too.
3. Drag the same edge upward through flat and toward the opposite tilt, then back down. The seam must not accumulate a lip, a gap, translation, or roll. Test Esc cancel, undo, and Reset face morph.
4. Try the child's bottom face as the driver. The parent geometry must not be rotated by the joint follow. Repeat for an opposite-side Z-axis tilt and for a child rolled by 90 degrees.
5. Verify the child's **opposite/far face** morph values are unchanged. Add a third tank on top of the child: it must follow the child's rigid body pose, but its face must not be morphed transitively.
6. Start with a deliberately different-sized or tapered child connected face. Copy should still apply the incremental mirrored step, **not** force an absolute face match. For an intentionally mismatched pair, use the separate Match command.
7. Save/reload the craft: loading must not trigger any fresh morph or seam correction; Copy should only act on new editor drags.

**Note:** The Python regressions verify source wiring and independent geometry arithmetic; an actual KSP/Unity editor build is necessary to verify the FL-T400 model and the transform hierarchy.
