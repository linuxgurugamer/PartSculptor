# StretchScale 0.0.4.45 in-game regression checklist

1. Start the VAB with PluginData/Settings.cfg and verify other settings work, including Career limits and editor steps. Delete or rename the old GameData/PartSculptor/Settings.cfg if upgrading in-place.
2. Open Browse presets. Verify Show relative starts checked and Show absolute starts unchecked. Only eight relative built-ins should be visible initially.
3. Check Show absolute. Verify sixteen built-in presets are visible. Uncheck Show relative: eight absolute entries remain. Uncheck both: no entries and Apply selected disabled. Restore relative.
4. Apply Double Y [relative] to a tank at 1/1/1. Check Y=2, PAW selector shows Custom. Repeat preset: Y=4 and selector remains Custom.
5. Apply 1.5x uniform [absolute]; confirm axes 1.5/1.5/1.5 and selector shows 1.5x uniform. With Career limits active and preventing full application, verify selector reads Custom instead.
6. Directly edit X/Y/Z in PAW, drag gizmo, and use Swap X/Z; confirm selector shows Custom even if the scale happens to equal a preset.
7. Click Reset scale; axes go to 1/1/1 and selector returns to 1x uniform. Check symmetry counterparts show the same selector values after scaling/reset.
8. Save and reload a craft; verify scale values and presets still load from PluginData. Test custom absolute presets in PAW and relative presets in browser after reopening.
9. Inspect KSP.log for IMGUI ArgumentException during filter toggle changes or rapid preset selection; none should occur.

Offline Python smoke tests can be run via `python Tests/run_regressions.py`. They are not a substitute for KSP in-game behavior testing.
