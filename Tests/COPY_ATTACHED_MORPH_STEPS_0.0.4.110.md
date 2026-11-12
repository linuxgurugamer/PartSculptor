> Historical plan for 0.0.4.110. Its local-tilt expectations are superseded by the same-side mirrored behavior in 0.0.4.113.

# 0.0.4.110 — Copy attached morph steps (KSP editor manual tests)

1. Start with 0.0.4.110. In Vessel Inspector → Settings, turn on **Copy attached morph steps** by default; Save, re-open and confirm persistence. Apply then Cancel should restore both saved and live session mode. Verify the shared PAW toggle on a second part. Toggle back off.
2. Stack two ordinary PartSculptor tanks, with **Match attached faces = Off**, **Copy attached morph steps = On**. Morph only the parent's *connected* face +0.20 X width. Confirm child connecting face receives +0.20 width adjustment, not the parent's absolute width. The opposite face remains unchanged.
3. Give the child a different pre-existing width first (e.g. 1.35). Then change parent connected face from width 1.10 to 1.30. Confirm child increases 1.35 → 1.55, retaining its offset rather than being fitted to the parent.
4. Drag the child's connected face; verify the parent receives the delta. Drag the parent's non-connected face; verify there is no change in child.
5. Test Z resize, Alt tilt, Shift+Alt translation, Ctrl+Alt radial and combinations. Rotating a stack connection by 90° should swap X/Z changes; opposing face orientations should correctly map signs of tilt/offset.
6. Test Esc and right-click cancellation: both original and receiving faces should return to their pre-drag shape for fully accepted steps. Test Reset face morph: receiver receives the reverse *delta*, not a hard reset.
7. Set **Match attached faces = On**, **Copy attached morph steps = Off**: physical geometric matching should work exactly as in 0.0.4.109. Turn Copy On too: live editing must COPY changes, not match shape, while automatic new-part placement still matches geometry.
8. Ensure normal editor Ctrl+Z undo, symmetry parts, attached grandchildren, parent and child topology, and craft save/reload work correctly. No changes should be made merely because a saved craft is opened.
9. Deliberately exceed recipient face bound (e.g. 3.95 +0.10) or Career/volume limit; receiver must remain unchanged for that rejected step. Test side/surface nodes, unsupported colliders, non-PartSculptor neighbors and arbitrary 45° rotations: copy must be skipped safely, without preventing the driver edit.

Offline tests cover the mapping and wiring, but live KSP behavior requires in-game verification.
