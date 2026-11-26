# Scale preset persistence (0.0.5.23)

Manual KSP verification:

1. Place a stock tank in Sandbox, choose 2x uniform in its PAW, and check all three axes are 2.000. Save the craft and reload it. Open the PAW: the selector should still say 2x uniform and dimensions, resources, and attachment positions should be unchanged. Repeat with a nonuniform absolute preset and a named custom absolute preset.
2. Use an older craft saved before this release with all axes at 2.000 and no saved preset name. The selector should infer 2x uniform. Unmatched axes should display Custom. Do not modify the geometry to fit a preset.
3. Manually edit an axis, save and reload. Custom should stay Custom even if the axes happen to match an absolute preset. Apply a relative preset, save and reload: it should remain Custom. Repeat preset should run the previous relative command only when explicitly clicked.
4. Delete or alter the definition of a custom preset after saving a craft using it. On load, show Custom when the saved dimensions no longer match the named preset; preserve the saved axes.
5. Copy a preset-scaled part using Alt-copy and symmetry, save and reload. Check each part's label and dimensions. Reset scale and verify 1x uniform when the reset succeeds.
6. Reopen PAWs, switch B9 subtypes where supported, and check that restoring model scale without changing the axes does not clear the preset label.

The Python tests check source integration, and the .NET regression runner includes display-resolution reference cases. Neither substitutes for in-game PAW and craft-save testing.
