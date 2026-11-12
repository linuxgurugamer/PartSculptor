# Face matching regression tests — 0.0.4.108

1. In VAB/SPH enable **Match attached faces** on any PartSculptor PAW. Morph a tank's top end to width 1.5/depth 0.75, then place a NEW tank by its bottom stack node. Check that the child end fits, and its top is unchanged. Repeat with the opposite ends and different stock part sizes.
2. Confirm the placement still matches when the child already has a parent assigned before its first editor Update. Test attaching from the parts catalog, Alt-copying, undo/redo, picking and reattaching, and placing a subassembly. A new attachment should be modified at most once.
3. Repeat with nonzero parent face tilt (around 0.15–0.3) and optional face offsets. Mildly rotated child frames should get a close fit, with geometry that is visibly near the parent rim. Large offsets or 45° roll must still be rejected when beyond the 7.5% corner-error tolerance.
4. Disable auto-matching and attach an otherwise compatible child: it should remain unchanged. Right-click the **child** and select **Match face to parent now**; it should match even with auto-matching off. Change the parent geometry and retry manually to refresh the child fit.
5. Test the manual action on a root part, surface attachment, side or inset node, child with unsupported mesh/collider, and a part whose needed shape exceeds 0.25–4x or Career/volume restrictions. Confirm a readable on-screen reason and no unintended deformation.
6. Save and reload an existing morphed vessel, with auto-matching on. Existing joins must not be changed merely because the file was loaded. Detach/re-attach a child and confirm it can be matched as a new placement. Repeat in flight (no editor matching).
7. Repeat with **Match node sizes** enabled and disabled; verify that size matching still takes place first. Test 90°, 180°, and 270° rolls, symmetry, child parts with further children, moving/copying parts, Undo/Redo, save/load and launch.
8. Watch `KSP.log` with PartSculptor debug logging enabled if a join is unexpectedly skipped. Log messages distinguish placement, invalid nodes, incompatible shapes, and rejected size limits.

The offline tests check matching math and event wiring but cannot replace in-game KSP physics/editor validation.
