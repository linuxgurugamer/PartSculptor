# Scaling gizmo regression checklist — 0.0.4.30

## VAB / SPH input
1. Start a fresh craft with a stock tank that has ModuleStretchScale.
2. Open the tank PAW and click **Show scaling gizmo**; verify three axes and
   that colors match X red, Y green, Z blue and labels match scale values.
3. Move the camera, orbit it and zoom; handles must follow the part and remain
   aligned with part-local axes. Test a rotated tank.
4. Drag X from 1 to 1.5 and back: Y/Z should stay at 1 when unlocked. Repeat
   the identical drag and verify no additional mass/fuel scaling occurs.
5. Enable **Lock proportions** and drag Y 1 to 1.5; all three axes should
   reach 1.5, and a native 1t tank's dry mass should become 3.375t.
6. Repeatedly move 1 -> 1.25 -> 1.5 -> 1 and check the baseline is stable.
7. Hold Shift during a drag: scale should change in 0.05 increments from the
   starting value, without jumping when the mouse hasn't moved.
8. Press Escape mid-drag: all scale axes should return to pre-drag values;
   repeat with right-click. Release the mouse after cancelling.
9. Try picking/detaching a vessel part while the cursor is *away* from gizmo
   handles: ordinary editor actions must work. Hover and drag a handle: stock
   part pickup should NOT happen. After closing the gizmo, no lock remains.
10. Confirm **Stretch children** and symmetry counterparts use the same rules
    as PAW scaling. Test with attached engines/resources and rotated children.
11. Check career tier limits and allowGrow=false rules: gizmo cannot bypass
    the restrictions. Reload a saved craft and verify gizmo changes aren't saved.
12. Open a different part's gizmo, hide the gizmo, delete its part, change to
    flight, return to VAB: no orphaned meshes or input locks may remain.
13. Verify original PAW scaling presets, auto node matching, FX scaling,
    B9PartSwitch subtype changes and inspector tooltips remain functional.

## Automated tests

Run `python Tests/run_regressions.py --require-dotnet` in a .NET 8 SDK
installation. The C# runner tests actual production ScaleMath.GizmoScaleValue
and ScaleMath.GizmoAxes, including no compounding, locking, bounds and snapping.
The Python smoke checks verify the gizmo's project integration and lifecycle.

These do not replace an actual KSP1/Unity editor-input test.
