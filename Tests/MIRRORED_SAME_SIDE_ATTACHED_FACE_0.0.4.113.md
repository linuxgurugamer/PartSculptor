# 0.0.4.113 — Same-side mirror: KSP editor validation

Begin from a fresh craft in the VAB. Use two supported, stack-connected parts with parallel Y axes, and enable **Copy attached morph steps**. Turn off **Match attached faces** for clarity.

1. On the parent upper end connected to the child lower end, drag the **right (+X) edge DOWN** to decrease the parent tilt X. Check that the child's **same visible right edge** is driven UP relative to the left edge (its local tilt X changes by the opposite amount). The child's far end must not change morph parameters. Do not expect Copy to fit the two touching quadrilaterals absolutely: that is Match mode.
2. Reverse the drag and cancel using Esc/right-click. Both parts should restore their initial morph parameters. Reset must similarly mirror the incremental reset.
3. Repeat with right edge UP, left edge DOWN, front (+Z) DOWN, back (-Z) DOWN. Check opposite longitudinal Y tilt on the **same visible** side. Try both a parent-driven and child-driven edit.
4. Roll the child 90°, 180°, and 270° when connecting. Confirm the mirrored adjustment follows the corresponding *world side*, not the same local X/Z index. Reject unsupported arbitrary 45° rolls rather than editing the wrong side.
5. Give the child a different width and initial tilt BEFORE dragging the parent. Copy should ADD mapped deltas to that existing face, not overwrite it with the parent's final shape. Compare to Match mode, which should fit the touching face geometrically instead.
6. With Copy ON and Match ON simultaneously, dragging uses Copy; initial new part placement still uses Match. Check symmetry, two or more attached children, Career/volume restrictions, no propagation past one connection, and no changes on craft load.
7. Inspect positions/colliders after a large mirrored tilt. Mirrored motions are intentionally not a flush-fit operation, so joint shape may no longer coincide; use Match if the priority is a strictly flush interface.

**Caution:** Offline math and source checks cannot validate Unity transform movement, KSP editor undo or colliders. These cases require in-game testing.
