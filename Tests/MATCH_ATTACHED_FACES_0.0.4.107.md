# Face matching when placing a new stack part — 0.0.4.107

In the VAB/SPH, enable **Match attached faces** in any PartSculptor PAW (independently of **Match node sizes**). For new sessions, use Vessel Inspector → Settings to choose a saved default.

1. Morph the **top** face of a stock cylinder-shaped tank to 1.5× width, 0.75× depth. Stack-attach a different/new tank onto that top node. Only the **new tank's bottom** face should take the parent's width and depth, while its top face stays unchanged. The parent must not move.
2. Repeat with the parent's **bottom** face, a non-uniformly stretched parent, and a new tank with different native width/depth. The four touching corners should align, not simply copy morph multipliers.
3. Rotate the new part 90° about its stack axis before attaching. Width and depth must be interchanged to preserve the parent's physical face shape. Repeat at 180° and 270°.
4. Morph a parent end with **Alt tilt** and **Shift+Alt face offsets**, then attach a new part. If all four corners and stack pivot are representable, match width/depth/tilt. If they cannot be represented (including arbitrary/45° rotation or mismatched node offset), do not alter the child.
5. Turn **Match attached faces** off. A newly attached child must retain its original face. Toggle **Match node sizes** independently and confirm its prior behavior, alone and together with face matching.
6. Save and reload a ship with matching disabled or enabled, and enter the VAB with existing stack joints: no existing part should be reshaped merely by loading. Detach and reattach a child to trigger a new match; manually morphing a previously connected part must not cause new auto-fitting.
7. Try a surface-attached part, side node, recessed stack connector, non-morphable mesh, robotic/B9 incompatible variant, and face geometry requiring width/depth outside 0.25–4×. Confirm no incorrect morph or exception.
8. Verify Ctrl+Z / Ctrl+Y around placing a new part, symmetry clones, existing attached descendants, mass/resources, collision meshes, save/reload, and launch. Test Career limits to confirm forbidden matches do not apply.
9. Save, Apply, Cancel in settings. Confirm the face-match default is stored in `PluginData/UserSettings.cfg`, session PAW toggles update, and Cancel restores both the opening values and any prior preferences file.

`Tests/run_regressions.py` checks source wiring and (when .NET is available) pure geometry fitting, but cannot validate the live KSP editor lifecycle, mesh visuals or collider behavior.
