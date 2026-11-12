# KSP editor validation: mirrored face plus moving attached tank (0.0.4.118)

This is an **in-game** checklist; passing offline source and numeric tests does
not establish that the KSP editor updates its physical part graph correctly.

## Two stock FL-T400s

1. Open the VAB with two FL-T400 tanks stacked vertically, at native size. Leave
   both morphs at identity. Enable **Copy attached morph steps**, disable **Match
   attached faces** so only Copy runs.
2. Use Morph to tilt the **top face of the bottom (parent) T400** by dragging its
   right-side handle downward. Watch the top (child) tank. Its *bottom face*
   should receive the same physical-distance step on the same visible side in
   the opposite LOCAL direction, while the **whole child tank pivots** to keep
   the touching end planes in contact. The stock stack-node centers should
   coincide. The parent body must not rotate.
3. Gradually reverse that drag, moving back through zero and to the opposite
   tilt. Check that the child body pivots back without accumulated twisting or
   drifting. Return to the starting morph. It should return to its starting pose.
4. Repeat from the front/back side and with a two-axis corner tilt. Repeat with
   the child rotated by 90 degrees before attachment.
5. Now drive the **bottom face of the top (child) T400** instead. The parent
   stays fixed; the child assembly must move to keep its joint connected while
   the parent's touching face is morphed with the mirrored local increment.

## Descendants, undo and reset

6. Add a third stock tank (or a small probe/core) to the CHILD tank's other end.
   During both parent-driven and child-driven tilts, the third part should move
   with the second tank as one assembly, not remain floating or double-rotate.
7. Try an intentionally offset attachment. The original offset between nodes
   should be preserved; Copy must not silently snap a deliberate editor offset.
8. Test **Esc/right-click cancel**, **Ctrl+Z/redo**, and **Reset face morph**.
   Confirm no accumulating joint gaps and that all attached parts return to
   their original pose on cancel/reset. Undo/redo can also rely on KSP restoring
   the entire craft snapshot.
9. Switch Copy off and enable Match. Confirm that Match remains the separate
   absolute four-corner face fit, not this incremental mirrored mode.

## Interpretation

- The child's local end-face morph controls still have opposite mirrored
  deltas. The CHILD BODY must now rotate; observing equal and opposite WORLD
  face slopes *and* flush contact is geometrically impossible for two identical
  initially flat faces under opposing local tilts without a compensating body
  rotation. Verify the faces touch, not that the child remains upright.
- If the connected faces were originally different sizes or had different
  physical shape, Copy maintains those differences. A rigid pose alone cannot
  make noncongruent faces identical; use **Match face to parent now** to start
  from compatible geometry.
- If a test fails, record which tank/face/handle was edited, whether the child
  was 90-degree rolled or offset, whether the entire child body moved, and
  whether the node centers or face planes separated. Include `KSP.log`.
