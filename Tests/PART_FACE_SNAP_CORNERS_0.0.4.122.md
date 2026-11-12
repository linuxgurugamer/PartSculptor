# PartSculptor 0.0.4.122 — mirrored tilt and four-corner snap

## Two identical FL-T400 tanks

1. Stack two stock FL-T400 tanks in the VAB, with no offset and default rolls. Enable **Copy attached morph steps**; disable Match.
2. Alt-drag the lower tank's upper-right side downward. Confirm the **local** tilt value on the upper tank's bottom face is the same magnitude with the mirrored sign.
3. Inspect both the dragged edge and its opposite edge. With congruent starting faces, the four visible end-face corners should coincide: no lip, shortened edge, reduced visible edge slope, or gap. The upper tank body should pivot as needed, and the far end should retain its prior *local* morph values.
4. Reverse the drag through zero and repeat for both tilt signs, with large and small changes. Check that reversing returns the child to its original pose with no progressive roll.
5. Repeat for Z-side tilt and a mixed X+Z tilt, then roll the child 90°, 180° and 270° before dragging. Check that the same physical side stays paired.
6. Edit the CHILD's touching face instead of the parent's; the physical child body should still be the only rigidly corrected subtree (never the parent). Add a grandchild part and confirm it follows exactly once.
7. Try reset, Esc/right-click drag cancellation, Ctrl+Z, save/load, and other non-morph editor operations. No automatic morph or snap should happen just from loading.

## Different initial sizes

1. Attach parts of different widths and enable Copy. The new rigid fit should be skipped if the four corners cannot be matched within tolerance; the pre-existing plane alignment remains the fallback.
2. The mod should not change a recipient's width/depth/tilt values beyond the v113 mirrored incremental step. Use **Match attached faces** when actual geometry should be resized to coincide.

These are **manual KSP/Unity tests**; offline Python checks cannot exercise the editor's part transforms.
