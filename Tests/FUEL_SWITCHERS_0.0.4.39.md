# MFT / Firespitter / InterstellarFuelSwitch regression checklist (0.0.4.39)

1. Confirm StretchScale starts when none of the three optional DLLs is present.
2. MFT: switch between tank types; edit tank fuel allocations; change 1x -> 1.5x -> 1x, check fuel amounts, mass and cost, and check the native MFT tank-volume UI independently.
3. FSfuelSwitch: A -> B -> A -> B at 1.5x, including two setups using the same resource name with different capacity; observe no multiplying growth.
4. IFS: switch between two resource setups, save/reload, switch back; check resource quantities and subtype names.
5. Do the same with two switcher modules on one part and with a B9PartSwitch module installed; make sure variants are tracked separately.
6. Compare dry mass + KSP module modifier deltas before and after switching. Verify no unexpected modifiers.
7. Verify in flight with partly depleted tank that a switch does not refill fuel unexpectedly.
8. Undo/redo and editor symmetry; confirm no compounded capacities.
9. Without KSP installed run `python Tests/run_regressions.py`; with .NET 8 installed run `python Tests/run_regressions.py --require-dotnet`.

Known limitation: MFT internal tank-volume accounting is not updated by this initial adapter; capacity consistency must be tested against its native UI before using the mod in important craft.
