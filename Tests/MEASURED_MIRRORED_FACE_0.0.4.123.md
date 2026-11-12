# PartSculptor 0.0.4.123 — FL-T400 measured mirrored face test

1. Open the VAB. Start a fresh craft with two unmodified, identical FL-T400 tanks stacked vertically. Enable **Copy attached morph steps** and turn off **Match attached faces**. Enable PartSculptor debug logging if available.
2. Tilt the bottom tank's upper-right face edge downward by a small amount, then by a larger amount (e.g., ~0.2). Verify the upper tank's touching edge is mirrored on the same visible side, its *actual tilted edge length* matches the bottom face, and the body moves into contact.
3. Repeat with the opposite direction, the opposite side, and X/Z mixed tilts. Return to the initial shape to test drag cancellation/undo.
4. Roll the upper tank 90° before the tilt; verify the matching visible side still responds and the X/Z native spans are interchanged correctly.
5. Try two intentionally differently shaped touching faces; Copy must retain their pre-existing differences rather than overwrite them with absolute Match behavior.
6. Add a third descendant part; the adjustment should affect only the directly connected face and rigidly move the descendant. Check Match mode separately; it must remain unchanged.
7. Watch `KSP.log` for `mirrored face` measurements/corrections when debug logging is enabled. Any correction skipped by morph/Career/volume limits should leave the original incremental Copy result, not replace it with a clamped shape.

**Limits:** These are test instructions, not proof of game behavior. Offline geometry/unit tests cannot verify KSP model scaling, attachment event ordering, or renderer state.
