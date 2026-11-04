# Face morph validation (0.0.4.50)

## Usage
1. In VAB/SPH click the **sixth Morph** editor tool (immediately right of Stretch) and select a supported static mesh-collider part.
2. Eight corner handles (four orange on the bottom face, four cyan on the top) should appear. Press **M** to switch between fifth Stretch and sixth Morph tools if desired.
3. Drag a TOP corner outward/inward: only the top face expands/narrows in X/Z, yielding taper. Drag a BOTTOM corner independently to make a reverse taper.
4. Hold **Left/Right Alt** while dragging a corner: its entire end face should tilt without bending the face into four noncoplanar corners. Shift rounds to steps of 0.05; Esc/right-click cancels.
5. Save craft, clear editor, reload twice, and launch: both visuals and collision geometry should be retained; no repeated growth on reload.
6. With attached parts and symmetry, test that nodes and children stay at their corresponding faces and that save/load restores relative positions. Use disposable test craft.
7. Change X/Y/Z while a morph exists: taper/tilt must be recalculated from native vertices. Set morph to identity to return to base shape.
8. Switch away using the fifth Stretch button, M, a native tool, Esc, or scene change: all handles and input locks must be released.
9. Attempt morphing a stock tank with BoxCollider, SphereCollider or CapsuleCollider. Handles should appear; after dragging, collision proxies should become active and originals disabled. On Reset face morph, originals should be restored.
10. Attempt morphing a part with unsupported colliders, unreadable/skinned geometry, or B9-switched geometry. An on-screen rejection reason should appear.

## Known limitations
- Supports **Y-axis end faces** of static, readable visual meshes and ordinary MeshColliders or primitive Box/Sphere/Capsule collision shapes. Primitive shapes are approximated by convex mesh proxies during a morph. Not for procedural/animated or variant-changing geometry.
- Tilted attachment nodes get new positions/normals, but existing attachments may require manual adjustment of orientation; rotation of the entire attached subassembly is not automated.
- Volume is estimated from linearly interpolated end-face areas; large combined tilt+taper may have slightly different true mesh volume, and aerodynamic voxelization needs separate in-game validation.
- Shared mesh instances are never modified. Morph data are KSPField floats in the ModulePartSculptor craft node, not a separate file.
- KSP, shader tangent generation, collider cooking/performance and save/load must be validated in game; offline checks are not proof of gameplay compatibility.
