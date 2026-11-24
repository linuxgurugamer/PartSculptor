# Individual vertex physics — manual KSP validation

1. In VAB, place a stock tank, open PG / Vertices, insert a vertex in the middle of a triangle on the outside of the tank. Insertion without movement should not change the collider, node poses, or calculated volume.
2. Drag the vertex outward; release. Inspect the collider (with a collider visualization mod or by contacting another part) and check volume, mass, cost and available fuel. Drag inward; check the reduction in geometric volume. Convex collision cannot reproduce an inward dent exactly.
3. Insert a vertex on a cap triangle close to a stack node, then drag with Alt. The node position and orientation should follow its local triangle. Attach a child and repeat; check that the connected node stays joined. Edit the side near a surface-attach node and check that node pose too. Distant/unaffected nodes should remain unchanged.
4. Add a ring, resize/tilt it, and repeat with individual vertices. Edit ordinary XYZ scale and face morph with the vertex present. Repeated no-op applies should not compound deformation.
5. Remove or clear vertices. Check restoration of the ring-only mesh, collider, node poses and volume. Check Ctrl+Z, symmetry, save/reload, Alt-copy, launch and quickload; fuel should not be reset by geometry restoration.
6. Test primitive colliders, low-detail mesh colliders, B9 subtype switching and an over-budget convex collider. A rejected edit must not alter any symmetry target. Test stock and MFT/B9 tanks. Watch KSP.log for cooking warnings.
7. Confirm the original PG ring controls and Alt tilt still work.

Offline tests exercise independent signed-volume math and source contracts only; Unity collider cooking, in-game node motion and resource accounting require this manual test.
