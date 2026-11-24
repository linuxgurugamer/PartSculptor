# PG vertex drag regression — 0.0.5.12

The original failing parts were not supplied, so these steps must be repeated in KSP with the affected cubes and cylindrical tank.

1. In a Sandbox VAB, select PG, choose Vertices, insert a vertex on a cube face, and drag the outward axis a small distance. Release. Check that the visual handle stays at the edited position, the collider follows, and the calculated volume increases. Undo and repeat inward. A concave indentation can remain outside the collision/volume change because Unity uses convex collision.
2. Repeat on the cylindrical tank, including a triangle interior and a shared edge/ring edge. Repeat with an existing ring. Ring-only controls should remain unchanged.
3. On a part with a dense native convex collider, test several individual support points. The submitted collision hull must have at most 252 triangles, even if source topology is denser. Large hulls may be approximated by a common directional sample of original/edited points.
4. Check group dragging, soft selection, stack-node following, symmetry, save/reload, undo, and deletion. Verify resources, mass and cost restore after clearing all vertex edits. Test flight physics and contact with neighboring parts.
5. If an edit still snaps back, copy the exact screen message and the matching `KSP.log` entries containing `ProceduralGeometryTool.CommitVertices` or `ProceduralGeometryTool.DragCancelled`. Include the affected part's internal name/config path. A genuinely invalid topology, degenerate hull, symmetry failure or configured volume/mass limit should still reject the edit rather than bypass safety checks.

Offline tests cover reference convex volumes for a cube and a cylinder, and source integration. They do not run Unity mesh cooking or actual KSP editor interactions.
