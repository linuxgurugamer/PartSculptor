# Procedural Parts exclusion — 0.0.4.92

1. Install Procedural Parts and PartSculptor with ModuleManager, and start KSP.
2. Open an ordinary stock fuel tank in the VAB; verify `ModulePartSculptor` is present.
3. Open a Procedural Parts tank and procedural SRB; verify the PartSculptor PAW group and module are absent.
4. Confirm the procedural controls still resize and update tank volume.
5. Verify that non-procedural crewed parts still have PartSculptor.
6. Inspect `GameData/ModuleManager.ConfigCache` for a `PART` containing `MODULE { name = ProceduralPart }`: it must not contain a module named `ModulePartSculptor`, unless another patch explicitly adds it.
7. Confirm existing `ScaleExponents.cfg` is unchanged.

Only the ModuleManager module-injection patch is modified. Previously saved procedural craft that already contain a PartSculptor module will not have it removed automatically.
