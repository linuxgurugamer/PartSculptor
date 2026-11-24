# Manual KSP editor checks — group vertex selection

1. Add several face/edge vertices on a supported tank with PG. Click one and Ctrl-click others (test both Ctrl keys). Verify white borders and one shared 84-pixel gizmo centered on the selected handles. Ctrl-click a selected handle to remove it. Click an unselected handle without Ctrl to replace the selection.
2. Drag each shared axis, the group center and a selected yellow handle (normal X/Z, Alt Y). Check equal world displacement of all selected vertices, including differently oriented mesh objects. Mesh changes only on release. Cancel a drag with Esc/right-click; verify no geometry change. Esc outside a drag clears selection.
3. Add a vertex inside a triangle created by an earlier vertex. Select both with zero influence radius and translate. Verify the dependent vertex does not receive extra displacement. Repeat with overlapping nonzero influence radii; selected handles should move equally, other vertices should follow the soft field. Very ill-conditioned groups may reject movement.
4. Apply a radius to the group, including a mixed-radius group; verify all selected radii change. Disable soft selection and apply to set all to zero. Verify unselected radii stay intact.
5. Select all visible, deselect all, select a subset and Delete selected. Verify only those edits are deleted and the native topology plus remaining rings/edits are rebuilt. Ctrl+Z should undo an entire group deletion, radius edit or completed drag at once.
6. Verify selection resets on part/topology changes; B9 inactive handles are excluded. Save/reload the craft and check vertex offsets, colliders, moved stack nodes, volume, resources, mass/cost and symmetry. Selection itself is not persisted. Verify ring editing still behaves as before.

Offline Python tests check source contracts and reference math; full Unity/KSP testing is required.
