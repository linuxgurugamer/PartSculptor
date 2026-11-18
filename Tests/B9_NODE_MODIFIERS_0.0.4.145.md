# B9PartSwitch NODE modifier support — v0.0.4.145

## ORANGES NLSUS interstage (required)
1. Install ORANGES and B9PartSwitch. In the VAB load `oranges_NLSUS_interstage` with its two variants: `NLSUS-A` has the top node at Y=2.4885, `NLSUS-B` has `NODE { name = top; position = 0, 1.208, 0 }`.
2. At 1x Y stretch, switch A→B→A several times. Top node should alternate between Y=2.4885 and Y=1.208 and not disappear.
3. Set Y stretch to 1.5 on B. Expected B top Y=1.812; on switching to A expected Y=3.73275; repeating should not accumulate errors. Try X/Z stretching as well.
4. Enable Morph tool (previous versions rejected this B9 NODE stanza). Confirm static meshes/colliders qualify and face handles appear. Morph the top end, switch B→A→B, then reset the morph; verify node positions/morph normals are not compounded.
5. Repeat with a part attached at the top, with symmetry, across save/load, after undo, and in flight. B9 still determines which model mesh is visible and whether a node is enabled.

## Additional NODE configurations
- `NODE { name = top; positionOffset = 0, 0.2, 0 }`: offset should use original prefab location plus 0.2, then current PartSculptor stretch.
- `NODE { name = top; size = 3 }`: node-size tier should be based on 3 when this subtype is active; returning to an unmodified subtype restores the PART's original size tier. Test with and without TweakScale to detect double scaling.
- `NODE { name = top*; position = 0, 2, 0 }` and `NODE { name = /^top[0-9]+$/; size = 2 }`: check matching multiple pre-existing stack nodes, and that other nodes remain untouched.
- `SUBTYPE { node = top }` (visibility toggle) remains managed by B9. PartSculptor must never resurrect a node that B9 disabled.
- Malformed/unknown fields inside NODE, a nonexistent selector, `SUBTYPE { TRANSFORM { ... } }`, or other unsupported model geometry changes must still be rejected for face morphing with a reason.

KSP and B9 are not available for automatic integration testing; this plan must be executed in-game.
