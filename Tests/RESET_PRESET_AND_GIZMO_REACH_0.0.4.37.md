# Reset preset and gizmo reach — 0.0.4.37

## Editor regression checklist

1. Place a tank with `ModuleStretchScale`, pick `2x uniform` or `Double Y` in its PAW and click **Reset scale**. Verify X/Y/Z = 1 and **Scale preset** = **1x uniform**.
2. Use the preset arrows immediately after reset. Verify the next click advances from **1x uniform**, not the previously selected item, and **Repeat preset** repeats 1x uniform.
3. Reset a part with symmetry counterparts. Verify the mirrored parts have X/Y/Z = 1 and their preset selector reads **1x uniform** if their PAWs are opened.
4. Open the scaling gizmo for a small part and a long tank. Verify all three handles extend farther away from the part; drag each handle and check normal scaling, Shift-snapping and Esc cancellation.
5. Save/reload the craft, verify scaling geometry and preset reset results remain unchanged.

## Offline smoke check

`python -m unittest discover -s Tests/python_regressions`

These checks do not replace KSP in-game testing.
