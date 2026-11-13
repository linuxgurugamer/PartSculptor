# PartSculptor 0.0.4.132 — Rotated part Morph handle drag test

1. In the VAB, place two FL-T400 tanks vertically. Enable **Copy attached morph steps**.
2. Alt-drag the lower tank's upper face enough to rotate the upper tank noticeably; release the mouse.
3. Select the upper tank, and start a **new** Alt-drag on either its connecting face or its free face. Move the pointer along the visible direction in which the selected handle should travel. The handle should respond along the tank's **current tilted local axis**, not screen-vertical as if it were unrotated.
4. Repeat on each corner and an X/Z midpoint; reverse the pointer direction within the same drag. Verify the handle never jumps or reverses as the tank rotates further.
5. On the rotated upper tank, test normal corner resize, edge-midpoint resize, Shift+Alt face translation, and Ctrl+Alt radial resize. Their drag reference must be the rotated pose when the gesture began and must not drift when Copy rotates the part again during the gesture.
6. Turn Copy off, retry a free face, then try a 90° roll or nonuniformly stretched test part. Observe both end-face degree readouts and ensure Cancel (Esc) and Ctrl+Z still restore the drag as before.

C# compilation and KSP editor rendering/drag behavior must be verified in the actual game; offline tests cannot confirm visual tracking.
