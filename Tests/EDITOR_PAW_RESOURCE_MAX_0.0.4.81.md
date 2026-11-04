# Editor and flight resource PAW maximum — 0.0.4.81

1. In the VAB place an ordinary stock tank, right-click it and note the stock fuel limit.
2. Stretch Y to 2x. Verify the PartSculptor **Capacity** summary displays twice the original maximum, and the **stock resource PAW control** also shows the new maximum.
3. Drag the fuel bar below full. Stretch Y again. Verify the live fill fraction is preserved and the displayed upper limit follows the new capacity.
4. Reset the scale to 1x and verify that the stock limit returns rather than retaining the previously stretched value.
5. Close/reopen the PAW, then clone the tank, load a saved craft, and repeat in the SPH.
6. Launch and verify the flight PAW retains the same maximum as the editor, without changing the fuel amount or refilling when fuel is consumed.
7. Repeat with a B9 resource-only fuel switcher. Verify the selected subtype and its actual capacity; a failed PartSculptor Capacity summary is a separate scaling issue from a stale PAW label.

These are manual KSP tests; source-level tests cannot verify KSP UI rendering.
