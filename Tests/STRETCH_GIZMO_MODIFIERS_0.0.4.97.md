# Manual KSP editor test — Stretch gizmo Ctrl X/Z and help (0.0.4.97)

Use KSP 1.12.x in VAB and SPH with a stock tank. Begin with Lock proportions **off**, X=1.0, Y=2.0 and Z=1.5 (or any nonuniform configuration) so aspect-ratio changes are obvious.

1. Select the fifth, **Stretch**, editor tool and click the tank. Verify the new Stretch help panel appears directly beneath the six editor-tool buttons, with one line per command and the same skin/opacity treatment as the Morph panel. Check at different UI scales and resolutions, and when the gizmo is activated from the part PAW.
2. Drag X, Y and Z individually without modifiers. Only the selected axis changes, subject to saved PAW Lock proportions settings.
3. Hold **Shift** while dragging X (repeat with Y and Z): changes snap in **0.05** steps relative to the drag start. Repeat using left/right Shift.
4. Hold **Alt** while dragging X, Y or Z: X/Y/Z change by the SAME factor. Releasing Alt stops linking future edits; it must not change saved PAW Lock proportions. Test left/right Alt.
5. Hold **Shift+Alt** while dragging: 0.05 snap applies to the selected axis and all three dimensions remain proportional; test axis limits.
6. Hold **Ctrl** and drag X: X and Z change by the SAME factor; **Y remains unchanged**. Repeat dragging Z. Starting at (1.0, 2.0, 1.5), X=1.25 should result in (1.25, 2.0, 1.875). Repeat with right Ctrl.
7. Hold **Ctrl** and drag Y: Y changes alone; X and Z remain unchanged. This is intentional: Ctrl links X to Z, not Y to either.
8. Hold **Shift+Ctrl** while dragging X and Z: the dragged axis snaps in 0.05 increments and X/Z stay proportional. Repeat with right Shift and right Ctrl.
9. Enable PAW **Lock proportions**, then test Alt (all three) and Ctrl (X/Z only). Ctrl should temporarily override PAW all-axis lock; releasing Ctrl restores its original behavior. Alt wins if both are held. PAW checkbox must remain unchanged.
10. Test min/max per-axis limits on a nonuniform X/Z pair: the ratio must remain intact after the FIRST linked axis hits its limit; do not clamp X and Z independently.
11. Press Esc or right-click during each mode: verify the whole drag returns to its initial X/Y/Z and editor input lock is released. Finish a gesture and test Ctrl+Z and Ctrl+Y: the whole drag remains one KSP undo checkpoint and the custom tool deactivates safely.
12. Confirm symmetry, attached children, reloading a craft, resource capacity changes, and toggling to Morph continue to function. Morph help and all 16 handles must be unchanged.

Offline Python regression tests and optional .NET math checks cover code paths, not Unity mouse/keyboard/UI interaction. In-game validation remains necessary.
