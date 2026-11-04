# PartSculptor 0.0.4.34 — Presets regression checklist

Offline: run `python Tests/run_regressions.py` (C# production-math tests also run with the .NET 8 SDK).

1. In VAB add stock part with PartSculptor, confirm default PAW presets read from `Presets.cfg`, including Double Y and Half Y.
2. With axes 1/1/1 choose Double Y twice (using Repeat preset): expect Y=4, X=Z=1. Reset then choose 1.5x: expect all axes=1.5.
3. Click Edit scale presets. Create a preset named `Long tank` from the current axes, set absolute X=1, Y=2.5, Z=1, save; confirm it appears immediately in every part's PAW dropdown.
4. Apply it to part, verify usual symmetry and Career Mode volume limits still govern actual scale.
5. Duplicate the stock `Double Y` rule, name it `Triple Y`, make relative X=1, Y=3, Z=1; save and repeat to confirm each click multiplies Y by 3 (no mass compounding beyond the deliberate dimension multiplier).
6. Close KSP and reopen, confirm custom presets persist under PluginData/CustomPresets.cfg and remain available.
7. Edit/rename an existing custom preset, delete it, and confirm PAW options refresh and built-in defaults cannot be deleted or overwritten.
8. Supply invalid inputs (blank or duplicate name, zero, NaN, Infinity, malformed axis) and verify save is rejected, previous custom file remains valid.
9. With preset window open move mouse/drag over editor parts, check ClickThroughBlocker prevents craft click-through and the window can move from unused areas.
10. Edit default Presets.cfg on disk then restart KSP; verify the new/changed built-in preset is loaded.
11. Verify craft reload saves actual X/Y/Z values, not a dependency on the preset name, and existing B9, docking, gizmo and inspector behavior is unchanged.

Not verified in this environment: KSP/.NET 4.8 compile, UI_ChooseOption dynamic choice refresh, in-game save/load.
