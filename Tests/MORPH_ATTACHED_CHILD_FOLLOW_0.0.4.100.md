# 0.0.4.100 — Face tilt and connected-part movement: in-game checks

Test in KSP 1.12.x in the VAB **and** SPH using the user's `PartSculptor(3).zip` rebase.

1. Place a static tank as the root and attach a small engine or pod to its **top** stack node. Select Morph and Alt-drag a top face corner/edge to tilt the face. The attached part should **rotate** to align with the new top node normal, not remain vertical. Its attachment contact must not open a gap or penetrate the tank.
2. Attach a second engine/tank/pod below that child. Tilt and reset the root's top face: **all descendants** must follow the child as one assembly. Check with an off-center secondary payload where the pivot is visibly different from the part's origin.
3. Use Shift+Alt (translate face), resize/taper with a corner/midpoint and Ctrl+Alt (radial resize); connected parts must still **translate** with their node, and tilt combines translation with rotation when applicable.
4. Alt-drag the **bottom** face when the morphed part itself is attached to another part above or below. Verify the parent stays where it was; the morphed part remains connected at the parent node. (This update preserves parent-attachment position handling; the parent does not follow the child face's tilt.)
5. Move back and forth between tilt angles, then Reset face morph; verify the child returns to its original position and orientation within floating-point tolerance. Test **Esc / right-click cancel**, **Ctrl+Z** and **Ctrl+Y** and craft save/reload.
6. Repeat with symmetry counterparts (including attached children), a surface-attachment craft, and a mixed stack/surface setup. Confirm child orientation/positions and no unintended movements. Note that surface-attached children without an owner-side attach node may not follow this stack-node path.
7. Confirm there is no movement in flight, no accumulating transforms during repeated editor drags, and no unwanted side effects on normal Stretch axes or fuel-volume calculations.

This change is not compiled or executed against the KSP/Unity runtime in the offline test environment.
