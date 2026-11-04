# Face-morph attachment node orientation verification (0.0.4.61)

1. In the VAB, choose an FL-T400 and show the Morph gizmo.
2. Tilt only the upper face via Alt+drag. Verify `node_stack_top` points
   perpendicular to the angled face, and the bottom node remains unchanged.
3. Tilt both faces independently and inspect both end-node directions.
4. Move a face with Shift+Alt: a parallel face translation must not rotate
   its node. Resize with Ctrl+Alt: normal follows any tilt without drift.
5. Reset morph: both nodes return to native orientation. Repeat 50 edits.
6. Stretch the tank along X/Z/Y with an active tilt: node normals must
   stay correct after PartSculptor recalculates the geometry.
7. Save/reload the craft and verify restored tilt/node orientation.
8. Try an ordinary surface-attachment node on a part with supported mesh.
9. Regression tests: `python Tests/run_regressions.py`. C# runner tests
   execute only if the .NET SDK is available.

The fix updates the AttachNode orientation. It does not automatically reorient
parts already connected to the node during a morph edit.
