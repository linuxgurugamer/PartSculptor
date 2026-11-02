# StretchScale 0.0.4.43 – PAW preset selector / compact browser

1. In the VAB, open a part PAW. Confirm **Scale preset** is a slider/choose option
   and offers only absolute presets (all eight shipped ones). Select Double Y
   twice and ensure it always produces X=1, Y=2, Z=1.
2. Click **Browse presets** in the same PAW. Confirm the popup is about 410 px
   wide, has a scrollable full list and Apply selected, but **no slider**.
3. Create an absolute custom preset in Vessel Inspector > Edit presets. Confirm
   it appears in BOTH the PAW slider and full browser.
4. Create a relative custom preset. Confirm it appears ONLY in the full browser.
   Apply once and then click Repeat preset: both applications should multiply
   current scale as requested, while the PAW selector remains on an absolute item.
5. Delete or rename the chosen absolute custom preset. Confirm the PAW selector
   reverts to 1x uniform (or first available absolute entry) without error.
6. Reset scale after a relative preset and confirm the PAW selector returns
   to 1x uniform and Repeat preset uses 1x uniform. Check symmetry counterparts.
7. Close/reopen the browser, switch parts, reload a craft, and verify no GUI
   Layout/Repaint exceptions or unwanted reapplication during load.

Run `python Tests/run_regressions.py` for source-level regression tests.
KSP/Unity editor integration must still be verified in-game.
