# Editor Stretch/Morph Undo and Redo — Manual KSP Regression, 0.0.4.95

**KSP 1.12.x, VAB and SPH.** Repeat with and without KSP Community Fixes'
BetterEditorUndoRedo when possible; the native undo capture implementation differs.
Use an ordinary static stock fuel tank with ModulePartSculptor.

1. Enable **Stretch**, select the tank, drag its X handle to a different scale, then release. Press **Ctrl+Z**. Expected: the original dimensions, mass/resources/nodes and child attachments return; the Stretch tool button returns to inactive grey, handles disappear, stock part picking is unlocked. Press **Ctrl+Y**; the new dimensions return. Repeat for Y and Z, uniform lock, symmetry and Stretch children.
2. Make two separate Stretch drags. Undo once and ensure only the second gesture is reverted; undo again restores the first; redo in order. A long single drag should occupy *one* undo checkpoint, not dozens.
3. Enable **Morph**, select the tank, drag a corner, release, press **Ctrl+Z**. Expected: original face geometry, mass/resources, collider and attachment geometry return, morph gizmo and button deactivate. **Ctrl+Y** restores the morphed shape. Repeat for all four edge-midpoint handles and Alt tilt, Shift snap, Shift+Alt translation, Ctrl+Alt radial growth, and Ctrl+Alt+Shift snapping. Test both faces and symmetry counterparts.
4. Start a new drag, move the handle then press **Esc** or right click: original shape should be restored immediately, with no stuck input lock. Click without moving a handle, then undo: no extra undo checkpoint should be made.
5. Enable a gizmo directly through the part PAW rather than the custom toolbar. Press Ctrl+Z or Ctrl+Y. The gizmo must disappear and must not retain a dead part reference.
6. Open a text input in the editor and use Ctrl+Z in that field. PartSculptor must not dismiss its tool while a text field has focus.
7. Test Undo/Redo after removing or reloading the previously selected part, after switching to a stock editor tool, with a large craft, and when the selected part has attached children or symmetry clones.
8. Save the craft, reload it, undo and redo morph/scale edits. Ensure the results are stable (no double scaling, spurious resource refill or geometry drift), and no new exceptions appear in `KSP.log`.

The offline Python suite checks source-level integration only; it does not verify KSP's undo stack or Unity runtime.
