# PartSculptor 0.0.4.125 — editor-integrated child rotation

1. In KSP 1.12.x VAB, attach an FL-T400 tank above another identical FL-T400. Enable **Copy attached morph steps** and leave Match off.
2. Select the lower tank Morph tool, Alt-drag the top-face right edge down and up (repeat and reverse the drag); verify the upper tank **body**, not only its lower face, rotates around the attachment joint.
3. Check that the child face's local **tilt degrees** still shows the correctly mirrored magnitude; the top of the child should rotate as a rigid body, with node anchors still near one another. Verify the in-game face lip separately (numeric node-normal alignment does not guarantee the physical model edge lengths match).
4. Repeat with the child as the source, and with the upper tank rotated 90 degrees before attaching. Confirm the edited parent is not rigidly rotated, and downstream parts move with the child.
5. Reset and cancel a Morph drag; confirm child rotation reverses. Press Ctrl+Z once to undo an entire drag, and Ctrl+Y to redo it. There should be no flood of undo states.
6. Test mirror and radial symmetry. Each connected physical pair should rotate once, and there should be no extra rotation on an unrelated counterpart.
7. Confirm **Match attached faces** does not perform Copy's extra rigid rotation. Check KSP.log for errors from onEditorPartEvent and transform updates.

This cannot be verified by the offline math/source tests alone.
