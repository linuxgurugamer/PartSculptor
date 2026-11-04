# PartSculptor 0.0.4.82 — B9 managed tank capacity test plan

Use the *same craft* that showed an incorrect upper capacity in flight with B9PartSwitch.

1. Confirm a non-B9 stock tank still has correct PAW capacity after resizing (0.0.4.78 behavior).
2. With B9PartSwitch installed, place the Streamline Engines Tanks DTE OX-50-61 mini Oxidizer Tank. Note its base capacity and selected tank subtype.
3. Stretch Y to 2×, leaving X/Z at 1×. Verify actual LiquidFuel/Oxidizer capacities are 2× original (or as prescribed by the active subtype's tank type).
4. Choose each B9 tank subtype in turn; verify current fuel types and maximum volumes and no volume multiplication compounding when going back.
5. Part-fill a tank in the editor, then launch. On the launch pad open the stock right-click PAW. The displayed maximum must equal the actual stretched capacity, and the fuel amount must still be present.
6. Burn fuel, stop thrust, close/reopen PAW. The maximum must stay fixed and the *amount* must fall; no refills or unexplained zeroing.
7. Save and reload the flight; confirm the capacity and remaining fuel amount are unchanged.
8. Repeat with a resized tank using both Stretch and TweakScale where installed. Test an empty/structural B9 subtype and a part with two B9 modules.

If a discrepancy remains, enable PartSculptor debug logging and compare B9 `baseVolume`, active subtype, `GetTotalVolume`, and the live `PartResource.maxAmount` at launch. The code does not attempt to change an external resource toolbar UI.
