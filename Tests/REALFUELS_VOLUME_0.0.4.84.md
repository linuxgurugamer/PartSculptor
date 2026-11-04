# RealFuels volume integration — PartSculptor 0.0.4.84

These cases require an actual KSP 1.12 game with RealFuels installed. They cannot be validated by source-level regression tests alone.

1. Select an ordinary RealFuels tank with `ModuleFuelTanks` and check its native `volume` (e.g. 1000 L) in the editor.
2. Double PartSculptor's Y scale: the RealFuels total volume should read 2000 L. Check fuel capacity and available volume in RealFuels' tank GUI, not just the stock PAW.
3. Repeatedly resize 1x -> 2x -> 1x -> 2x; there must be no compound growth. Include different utilization settings if your tank supports them.
4. Configure a partially filled tank. Confirm the editor fill percentage remains stable when resizing and that manually chosen fuel proportions remain under RealFuels' control.
5. Save and reopen the craft; duplicate the tank. Each should restore its own correct volume and selected fuels.
6. Launch the craft and confirm the flight volume and fuel amount persist. Burn fuel: PartSculptor must not refill it when checking volume.
7. Test an otherwise identical tank with Modular Fuel Tanks instead of RealFuels, and another with B9PartSwitch. Their behavior must remain unchanged.
8. If volume does not change, inspect `KSP.log` for `ModuleFuelTanks volume update did not take effect`. Record the class, assembly, requested and current volumes; attach those lines and the tank PART configuration to the report.
