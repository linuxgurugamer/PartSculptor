# Manual KSP editor checks — PartSculptor 0.0.4.96

KSP 1.12.x; test in both VAB and SPH, starting with a stock tank and its **Lock proportions** PAW field OFF.

1. **Stretch X without modifiers:** drag the red gizmo handle. Only X should change. Repeat for green Y and blue Z. Other axes must be unchanged.
2. **Shift snap:** drag an axis while holding Shift. Confirm it changes in 0.05 increments relative to the start of that drag. Releasing Shift resumes continuous scaling.
3. **Alt proportions:** from a nonuniform scale (e.g. X=1, Y=2, Z=1.5), hold Alt and drag one axis. All three dimensions should change by the same multiplier. The PAW's Lock proportions setting must still be OFF afterward. Repeat using LeftAlt and RightAlt.
4. **Shift+Alt:** verify both the axis increment (0.05) and the linked scaling. The X:Y:Z ratio should be preserved while Alt is held; ensure all axis limits still apply.
5. **Existing PAW lock:** with Lock proportions ON, drag without Alt. All dimensions should still scale together. Holding/releasing Alt must not turn the PAW checkbox off.
6. **Cancel and undo:** start Shift+Alt drag, press Esc or right-click. The original scale must be restored. Finish a drag and press Ctrl+Z then Ctrl+Y; verify the intended prior/current craft values and gizmo deactivation.
7. **Symmetry and children:** test scaling a symmetry counterpart and optional Stretch children, ensuring proportions, nodes and limits remain safe.
8. **Morph help panel:** select Morph tool and a supported part; confirm all instructions are displayed on separate lines, the panel expands to the longest line, tip backgrounds respect opacity, and all 16 handles still operate as before.
9. **Other resolution / UI scale:** test Morph info panel with higher KSP UI scale, small screens, and alternate skin for clipping or off-screen text.

Offline Python tests do not validate actual KSP or Unity drag input, layout, symmetry or Undo/Redo. Record in-game findings separately.
