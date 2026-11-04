# Flight resource maximum display — PartSculptor 0.0.4.79

This test verifies that KSP's **stock Part Action Window** displays the live
resource `maxAmount` rather than its original prefab limit.

1. In the VAB, add a stock LiquidFuel/Oxidizer tank. Record both capacities.
2. Stretch it to 2x in one axis. Confirm the editor shows doubled capacity.
3. Launch and right-click that same tank. Check that the PAW **maximum** for both
   resources matches the enlarged VAB capacity; the bar should show the correct
   fill percentage.
4. Close and reopen the PAW. Check the limit again. Pin/unpin the PAW and repeat.
5. Run the engine until some fuel is consumed. The **amount** should decrease,
   but the **maximum** should stay at its enlarged value. Check the bar fraction.
6. Repeat after saving and reloading in flight, and after switching vessels.
7. Repeat with an unstretched tank (stock label/formatting must remain unchanged).
8. Repeat with a resource-only B9PartSwitch tank, if installed. The current
   subtype's actual PartResource capacity must be displayed and no fuel refilled.
9. If the displayed upper limit is still incorrect, compare the stock **part
   right-click PAW** with the **stock resource toolbar app** and report which
   one is wrong. They are distinct UI controls.
10. Enable PartSculptor scale-debug logging if available and check for
    `[PartSculptor]` / `resource PAW` corrections in KSP.log.

This is an in-game test; offline source checks cannot confirm Unity UI timing.
