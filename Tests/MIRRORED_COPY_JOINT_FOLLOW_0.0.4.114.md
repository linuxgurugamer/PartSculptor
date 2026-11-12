# KSP editor validation — mirrored Copy mode joint following (0.0.4.114)

Use KSP 1.12.x with ModuleManager, PartSculptor, its required UI dependencies, and two ordinary mesh-readable cylindrical tanks. Do not use procedural/robotic parts for the first pass.

1. **Parent → child:** Stack tank B above tank A, enable `Copy attached morph steps` and disable `Match attached faces`. On A's top connected face, move the right edge downward, then back upward. B's connecting face should mirror the edge movement while both stack-node centers stay coincident. The far end of B must not morph.
2. **Child → parent:** Morph B's bottom connected face by the same right-edge gesture. Tank A's touching top face should mirror it. The *parent's node is allowed to move*; B must follow that updated node (the previous failure left B behind). Confirm the two node centers stay together during continuous dragging.
3. **Opposite facing ends and roll:** Repeat with the child rotated by 90°, 180°, and 270° about its stack axis. The same visible side should change, and connected nodes must remain coincident.
4. **Mixed adjustments:** Stretch X/depth and move a face sideways, then tilt. Confirm both node anchors and normals stay paired without the child drifting away, and confirm that no grandchildren are independently re-morphed.
5. **Intentional editor offset:** Before morphing, translate B slightly off its parent node with the stock offset tool. Record this initial anchor offset. Change the parent face angle: the offset should follow the rotated joint, not snap to zero.
6. **Symmetry:** Place two identical children in symmetry, then morph the parent touching face. Each physical connection should remain paired. Verify other siblings are unaffected unless their own connection is involved.
7. **Undo / cancel / reset:** During a drag, press Esc or right-click to cancel, and try Ctrl+Z after completing a drag; both ends must return to their original positions without accumulating drift. Test `Reset face morph` from each part.
8. **Toggle isolation:** With Copy off / Match on, the existing geometry-matching mode should be unchanged. With both off, face edits should remain independent; with both on, Copy takes priority for live edits and Match remains available for a new placement.
9. **Save / reload:** Save a craft with connected morphed faces and reload it; the nodes and geometry must not migrate, and no automatic extra live propagation should occur until another manual edit.

Note: mirrored incremental face controls are not a guarantee that the two *mesh surfaces* become identical; the purpose of the new correction is to keep their **attachment-node poses and child placement** joined after both faces change. Verify visually and inspect `KSP.log` for skipped unsupported joints.
