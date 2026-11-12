# PartSculptor 0.0.4.112 — Copy-through versus Match

Manual in-game test (KSP 1.12.x; start a fresh editor session):

1. Attach two compatible, equal-size stack tanks (bottom of the upper tank to top of the lower tank). Turn **Match attached faces** on and **Copy attached morph steps** off. Morph the lower tank's connected top face from width 1 to 1.3. Expect the upper tank's **touching bottom** to grow flush, but its **far top** to stay at width 1; the attached tank visibly tapers.
2. Reset both tanks, then turn **Copy attached morph steps** on (Match may be left off or on). Repeat the same change. Expect the touching bottom to remain flush AND the upper tank's **far top** to grow by the same incremental width step to 1.3, producing a non-tapered upper tank. The two modes must look different.
3. Repeat using different preexisting far-end widths (e.g., upper tank's top already at 1.4). After a driver width change of +0.2, expect the upper far end to become 1.6, NOT 1.2. Copy is incremental, not absolute matching.
4. Tilt the driver's connected face, then reverse the drag or press Esc. The joined corners must remain flush throughout; the recipient far end should show the reflected tilt step and return to its prior state after cancellation.
5. Repeat with the attachment reversed (morph upper/child first), and with 90°, 180°, and 270° rolls. Test offset X/Z and depth changes, plus settings disabled/enabled and symmetry counterparts.
6. Verify unconnected ends don't propagate, only immediate neighbors receive shape changes, other grandchildren follow moved attachment nodes without being morphed, and new-part placement still uses geometric Match rather than Copy.
7. Test a receiver far end near 4x width and an over-limit driver step. The *entire* receiver update must be skipped (not clamped/half-applied); check KSP.log for the reason.
8. Save and reload the craft. The copied geometry must persist; loading must not cause a second copy or matching pass.

No full KSP editor test or compilation is available in the offline harness. The pure math tests only cover the axis-reflection and delta behavior; editor transform/collider/node interactions require manual verification.
