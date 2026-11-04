# Modular Fuel Tanks / RealFuels volume integration — PartSculptor 0.0.4.83

Test with an actual ModuleFuelTanks tank installed; these operations are not
covered by the offline Python/C# math checks.

1. Place a tank with `MODULE { name = ModuleFuelTanks; volume = 1000; }` in
   the VAB, select a supported tank type, and allocate LF/O using MFT's UI.
2. With PartSculptor, scale Y to 2.0 (X=Z=1). MFT's **total volume** should
   change to about 2000; MFT's own allocated and available volumes should use
   that capacity, as should the stock PartResource maxima.
3. Repeat the scale to 2.0 without changing it: volume must stay 2000, not 4000.
4. Return to 1.0, then change X and Z independently; check volume changes by
   X×Y×Z and resource fill fractions stay constant in the VAB.
5. Change MFT tank type and adjust manually allocated fuels; check no double
   scaling, no spurious reload/reset and correct behavior under symmetry.
6. Save and reload the craft and duplicate a stretched tank (Alt-click); MFT
   volume and fuel allocation should remain consistent, and both can reset
   to native independently.
7. Launch with a partially filled tank: MFT volume should be the saved stretched
   volume, with no refill at load or when PartSculptor's periodic check runs.
   Fuel should burn normally with stable stage delta-v.
8. Verify a normal stock tank and B9PartSwitch tank with the same test remain
   correct and unchanged from 0.0.4.82.
9. If TweakScale is present, test both its uniform 1.25× scale and
   PartSculptor's 2× Y stretch; confirm the intended volumes multiply once.
10. Check KSP.log for `MFT ChangeVolume failed` and compare with MFT version;
    if the callback differs, record its method signature.

This source has no hard dependency on the MFT/RealFuels DLL. The in-game
`ChangeVolume` implementation must be tested with the installed MFT version.
