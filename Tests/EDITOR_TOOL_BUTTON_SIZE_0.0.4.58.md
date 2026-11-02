# 0.0.4.58: 28 × 28 Stretch/Morph editor buttons

1. Enter the VAB or SPH with the standard editor tool strip visible.
2. Confirm that the fifth Stretch button and sixth Morph button each have
   28 × 28 logical UI dimensions compared with the stock 32 × 32 buttons.
3. Confirm that neither custom button has icon padding or a gap beside the
   fourth stock button and between Stretch and Morph.
4. Click the edge of each new button to verify that its full 28 × 28 area
   is selectable and that active green/cyan highlighting works.
5. Change the KSP UI scale, resize the window, and switch VAB/SPH scenes.
   Confirm the alignment and dimensions are retained.
6. Activate Stretch and Morph modes to verify that the gizmos still work.

These steps require an actual KSP runtime; offline tests verify the source
layout math, not the game's rendering.
