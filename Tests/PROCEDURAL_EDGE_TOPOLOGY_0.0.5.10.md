# PG edge/ring insertion and deletion — manual KSP checks

1. In the VAB select a compatible static part, choose PG > Vertices > Add vertex, insert on a surface, select its yellow handle. Confirm X/Y/Z arrows extend 84 screen pixels and drag accurately along each local axis. Orbit and zoom; check the arrows remain the same screen size.
2. Choose Add edge/ring vertex and click on a visible triangle edge between endpoints. Verify adjacent triangles subdivide, the new handle is yellow, and the shape is unchanged before dragging. Try an edge across a material/UV seam on a single mesh; check for gaps after hard and soft drags. Click far from any edge and confirm no insertion.
3. Create a ring at 50%, resize/tilt it, then insert at its visible cut edge using Add edge/ring vertex. Drag along X/Y/Z; verify ring controls are unchanged. Move/remove the ring; retained vertex anchors should replay, or the edit should be safely rejected if ambiguous.
4. Insert a second vertex on an edge created by an earlier edit. Delete the first using Delete selected, confirm the remaining anchor persists and topology is rebuilt without unused split points. Its position may shift when a supporting edit is removed. Undo and redo. Clear vertices and confirm ring-only topology is restored; clear rings as well to recover the original native topology (retaining any ordinary Stretch/Morph edits).
5. Test a tank with a stack-attached child. Drag/deletes with zero and positive radius; check collider, attachment pose, resource capacity, dry mass, cost, calculated volume and symmetry. Save/reload and launch; test a B9 variant and the convex collider budget. Existing convex collision approximation and same-mesh seam limitations still apply.
6. Verify the native Delete key retains normal editor behavior; use the panel's Delete selected button for individual added vertices.

Offline tests cover reference geometry and source contracts, not KSP rendering, Unity physics, or C# compilation.
