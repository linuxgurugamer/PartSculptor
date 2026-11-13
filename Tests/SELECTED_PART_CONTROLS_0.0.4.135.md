# PartSculptor 0.0.4.135 — Selected Part / Compact PAW validation

1. In VAB/SPH attach two FL-T400 tanks. Right-click the lower one; open its PartSculptor Controls window. Set a unique numeric scale for the lower tank, leaving it attached.
2. Click/select the upper tank via its PAW, the editor selection, and the Vessel Inspector list. Without closing the window, verify it now shows the **upper tank**'s own scales, tilt values and presets. Switch back; both parts must retain distinct values.
3. Type an X scale or numeric tilt without Apply; change selected parts; verify the typed-but-not-applied input is discarded instead of being applied to the wrong part.
4. Select a part without ModulePartSculptor, if available. Verify that the window remains open but does **not** show an unrelated previous part's controls.
5. In Vessel Inspector → Settings, check **Hide PartSculptor PAW controls (keep window button)** and Save. Verify every PartSculptor PAW contains only the opener while other modules' PAW fields remain unaffected. The separate window must still expose all controls.
6. Uncheck via the All Controls window; verify the previous PAW fields and buttons return, preserving any restricted part options and the career-only readout.
7. Reenable the option, restart KSP, and verify it persists via UserSettings.cfg. Confirm Settings.cfg supplies false when no user preference exists. Use Save/Apply/Cancel in Settings and verify Cancel restores the initial visibility state and preferences file.
8. Change selection, remove a currently selected part, close/reopen the editor, load a saved craft, and check there are no stale pointers or duplicate windows. Confirm numeric tilt Apply, Scale Set, Copy/Match and undo remain functional.

In-game tests remain necessary; these operations cannot be exercised by the offline Python regression runner.
