# PartSculptor 0.0.4.35 - Preset editor entry-point regression

Run `python Tests/run_regressions.py` for offline checks.

1. Open the Vessel Inspector using ToolbarController in VAB/SPH; select a part containing `ModulePartSculptor`. The top row has an **Edit presets** button next to Close.
2. Click **Edit presets**; verify the existing preset editor opens and offers the unchanged New, Duplicate, Reload, Save, Delete, and Apply functions. The floating editor remains draggable and uses ClickThroughBlocker.
3. Close the preset editor, click **Edit presets** again, then click the button again to toggle it closed. Check no IMGUI Layout/Repaint control-count errors occur.
4. Select a different PartSculptor part, open the editor, and apply a preset. Confirm the preset applies to that selected part and uses its original `CommitChange()`/safety path.
5. Disable **Scaled parts only**, select a part without `ModulePartSculptor`; the **Edit presets** button is disabled.
6. In flight, confirm there is no **Edit presets** button (preset editing is editor-only).
7. Open a part's PAW: the **Edit scale presets** action is absent, while PAW preset dropdown and Repeat preset remain.
8. Save, reload, and delete a custom preset; confirm `PluginData/CustomPresets.cfg` persists and PAW preset choices refresh.
9. Confirm inspector resizing, dragging, scaled-only filter, part highlighting, and PAW **Show scaled values** still behave as before.

Not verified here: KSP compilation or in-game UI; the offline source checks do not replace those tests.
