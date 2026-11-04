# Manual test — midpoint drag sensitivity (0.0.4.73)

In the VAB/SPH, place a simple tank and select the Morph tool.

1. Set **Lock X/Z = Off**, view the tank at a noticeable diagonal angle, and drag the right/left midpoints along their projected direction. The selected handle should stay with the cursor along the X motion, without the previous lag. Repeat for the front/back midpoints in Z.
2. Repeat with **Lock X/Z = On**. The handle should track the cursor along its allowed path while both X and Z scale together.
3. Repeat with **Ctrl+Alt** radial scaling using a midpoint handle; both dimensions should continue to change together.
4. Hold **Shift** to verify configured snap steps; stair-step movement is expected only while snapping.
5. Check a tilted/offset face and a non-uniformly stretched tank; verify no loss of handle movement or jumping at drag start.
6. Drag perpendicular to the selected handle's allowed direction: one-axis handles need not follow the cursor across that direction.
7. Check boundary limits (0.25–4), a face at nearly edge-on camera view, corner-handle dragging, face translation, face tilt, Esc cancellation and craft reload.

Screen-space behavior must be validated inside KSP; offline tests do not render the editor camera.
