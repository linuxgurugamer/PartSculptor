# PartSculptor 0.0.4.44 — preset catalog regression checklist

1. In GameData/PartSculptor/PluginData, confirm `Presets.cfg` contains 8 absolute and 8 relative PRESET nodes. `GameData/PartSculptor/Presets.cfg` must no longer exist.
2. Open a new craft in VAB. Check the PAW **Scale preset** selector shows the eight absolute entries only; the eight same-name relative entries must not appear in this control.
3. Open **Browse presets**. Confirm that all 16 entries appear, each with `[absolute]` or `[relative]` and the correct X/Y/Z values.
4. Set a part to 1/1/1, choose `Double Y [absolute]` in the browser and Apply. Expect 1/2/1. Apply again: still 1/2/1.
5. Choose `Double Y [relative]` and Apply. Expect 1/4/1. Apply again: expect 1/8/1. **Repeat preset** must repeat the relative, not the absolute, command.
6. Reset scale, then select `Double Y` from PAW. Expect 1/2/1; this PAW option must remain absolute.
7. In Vessel Inspector open **Edit presets**. Confirm both same-name entries are selectable separately and show different modes. Duplicate one, save under a unique name, and delete the copy; the shipped entries must remain intact.
8. In Editor, create one custom absolute and one custom relative preset with the same name. Both must be visible in the full browser, and only the absolute version in PAW. Deleting one must not delete its counterpart.
9. Click **Reload files** in the preset editor to re-read `PluginData/Presets.cfg` and custom presets without restarting KSP.
10. Verify the Settings window height is 339px, and the Browse presets window is 410x554px before positioning adjustments; both remain draggable and work with ClickThroughBlocker.
11. Save/reload a craft with an absolute preset selected and confirm the part's stored scale factors remain correct.

These are manual KSP checks. The Python structural tests are executable via `python Tests/run_regressions.py`; the optional C# runner requires .NET 8.
