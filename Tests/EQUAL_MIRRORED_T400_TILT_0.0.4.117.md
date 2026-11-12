# KSP editor test: equal mirrored tilt on two FL-T400 tanks (v0.0.4.117)

In the VAB, build two stock FL-T400 tanks in a vertical stack. Enable **Copy attached morph steps**, disable **Match attached faces** for clarity. Leave both tanks at default size/rotation and move the camera to the side where the join is visible.

1. Use Morph + Alt to drag the lower tank's TOP right edge DOWN. The UPPER tank's BOTTOM same visible right edge must move UP by the same physical amount (relative to its tank body); the child tank's entire body must NOT gain an extra rotation. The two node centers must remain connected. Because these slopes are intentionally opposite, a wedge between the faces is allowed; this is Copy, not Match.
2. Reverse direction, across zero tilt, and continue upward. Confirm same magnitude/opposite direction, with no accumulated child-body rotation or node-center drift.
3. Repeat with left edge and front/back edges; try a two-axis corner tilt and a 90°-rolled child.
4. Repeat driving from the UPPER tank's BOTTOM face. Parent may not rotate. Child node center follows the parent node. Check mirrored tilt on the same visible side.
5. Change the child width/height, repeat a small tilt, and verify physical edge-displacement mapping, not blindly equal numerical parameters.
6. Cancel a drag with Escape / right click, undo with Ctrl+Z, and reset the face. Verify no drift and no changes to the child far/top face morph settings.
7. Turn Copy off, enable Match, and confirm Match still tries to make the faces flush rather than imposing equal and opposite visible tilt.

If the visual result differs, capture the KSP log, tell which tank was edited, whether the joint was already tilted, the face/edge and direction, and whether the child was pre-rotated by the editor.
