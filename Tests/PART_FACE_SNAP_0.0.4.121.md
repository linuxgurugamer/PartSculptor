# PartSculptor 0.0.4.121 — live Copy with PartFaceSnap

This source is **rebased directly on v0.0.4.119**, which was itself rebased on
v0.0.4.113. Version 0.0.4.120 changes are not included.

## Two stock FL-T400 tanks

1. In the VAB connect two *unmorphed* stock FL-T400 tanks vertically, without offsets.
2. Enable **Copy attached morph steps**; disable **Match attached faces**.
3. Drag the lower tank's top-right morph handle downward (Alt tilt). The
   upper tank's connected right side must morph upward in its **local frame**.
4. After both face edits, its whole body must pivot around the physical **end
   face**, not merely around the attach-node normal; the face planes and centers
   should coincide. Look for light leaking through or overlap at all four edges.
5. Reverse the drag, repeat both + and - tilt and Ctrl+Z / Esc cancellation.
   Check for any accumulation in rotation or position after reversal.
6. Repeat on the child's lower face to drive the joint from the other side.
7. Repeat with an intermediate 90° roll and with an extra tank/pod above the
   child. All descendants must move with the child rigidly, not double-rotate.

## Unsupported or different joints

- With different-size face ends, the **planes and centers** align but Copy does
  not equalize the lengths of their edges; use Match for an absolute geometry fit.
- Test offset/recessed stack nodes: PartFaceSnap intentionally aligns actual
  mesh end planes instead of preserving a prior visible gap at the node.
- Reopen a saved craft: there should be no automatic snap on load.
- Turn off Copy and use Match: geometry fitting should be unchanged.
- Test a side/surface attachment: no PartFaceSnap correction should occur.

No executable KSP/Unity editor tests are available in the offline Python suite;
these steps must be verified inside the game.
