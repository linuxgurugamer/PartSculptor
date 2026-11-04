# PartSculptor 0.0.4.85 — RealFuels 15.16.1 compatibility test

1. Install RealFuels v15.16.1.0, PartSculptor and its dependencies. Check that RealFuels is the provider of `ModuleFuelTanks`.
2. Use a tank whose RealFuels **usable volume** reads 4,000 L before scaling. Resize using PartSculptor so the desired usable volume is 5,000 L.
3. Confirm RealFuels' **own UI** now reports 5,000 L, its free/allocated volumes are consistent, and the old 4,000 L warning is absent.
4. With utilization set to 80%, the underlying `totalVolume` must become 6,250 L for a 5,000 L usable request. Check non-100% utilization and press the reset scale control.
5. Save/load, copy and duplicate the resized part and verify no accumulation. Try another scale, then return to the first to test immutable baseline.
6. In flight, check fuel quantity is not refilled and remains constant if capacity is reconciled; verify the RealFuels UI and stock resource UI.
7. Repeat a stock tank, a B9 tank and a basic Modular Fuel Tanks (non-RF) tank to detect regressions.

The Python regression checks do not verify RealFuels' runtime state. Inspect KSP.log on failure.
