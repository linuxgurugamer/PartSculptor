# KSP 1.12.x editor checks: equal physical mirrored face movements (0.0.4.115)

Enable **Copy attached morph steps** and disable **Match attached faces** to isolate Copy. These must be ordinary mesh-readable stack parts; both touching ends need PartSculptor.

1. Stack a 1.25 m tank above another 1.25 m tank. Tilt the lower part's right edge **down** by a visible amount. The upper part's **same visible right** edge should move **up** by the same distance relative to its original shape. Repeat left/front/back and do a reverse drag.
2. Repeat with a 1.25 m parent and 2.5 m child. The recipient's raw morph `TiltX` increment should be approximately **half** the source's when its native half width is doubled, but the physical vertical edge movement should be equal. Also test width and offset adjustments.
3. Attach a rectangular or 90-degree-rotated child; the same world-visible side should move, with X/Z source dimensions mapped to the target's actual dimensions.
4. Repeat with the *child* as the edited source and its parent as the recipient. Confirm that node anchors remain connected and any editor placement offset follows the rotating joint as in v114.
5. Set a different initial morph on the child, then drag the source. Copy must add the same physical increment, not replace the child's existing values with the source's final shape.
6. Cancel a drag (Esc/right-click), reset, undo, and reload the craft. Neither face should accumulate a residual delta or move the joint away from its recorded location.
7. With only **Match attached faces** on, the absolute geometry fitting should work as before; with both toggles off, there should be no propagation. Try a change that would exceed the child's morph limits and verify that Copy is skipped, not silently clamped.

Note: Equal *incremental* physical movement is different from absolute face fitting. Native mesh bounds, rolled joints and nonuniform transform scale determine the recipient's raw control values. KSP in-game verification is essential, because no Unity/editor runtime is available in the offline test environment.
