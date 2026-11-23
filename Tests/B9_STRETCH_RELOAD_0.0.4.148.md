# B9PartSwitch stretched stack-node craft reload — 0.0.4.148

1. In the VAB, select the ORANGES `oranges_NLSUS_interstage` using B9PartSwitch. Use `NLSUS-B` (native `top` Y = 1.208) or `NLSUS-A` (native `top` Y = 2.4885).
2. Set PartSculptor Y stretch to 1.5 with the XYZ editor gizmo. For B the final top-node Y is 1.812; for A it is 3.73275. Leave morphing off initially.
3. Attach a command pod or other stack part at `top`. Save the craft, exit the editor or start a fresh editor scene, then load the saved craft. Confirm the attached part remains at the top of the *stretched* selected mesh, not the stock/B9-native node.
4. Wait at least 150 editor frames after load. Verify no second shift or drift as B9 finishes subtype setup. Save and reload again; verify no accumulating displacement. Inspect `PARTSCULPTOR_STACK_GAP` records in `.craft` when diagnosing offsets.
5. Switch A→B→A, reattach the top part if necessary, save, reload and verify the correct selected subtype is respected.
6. Repeat with 2+ parts above the adapter, a radially attached part, a rotated stack, and optional TweakScale; child subtrees must move together without shifting surface attachments.
7. Make a deliberate small nonzero gap between connected node locations (where supported), save/reload, and verify the saved gap remains intact.
8. Test a craft saved before v0.0.4.148 without `PARTSCULPTOR_STACK_GAP` records; legacy stack connections should still default to zero gap.
9. While editing a loaded craft, move/resize with the XYZ gizmo normally. The bounded load repair must not keep snapping parts after startup or after part-count changes.

Offline regression tests cover source integration and expected numeric correction, not runtime KSP transform/link restoration; that requires the above in-game tests.
