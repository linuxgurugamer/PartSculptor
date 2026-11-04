# PartSculptor — all-parts ModuleManager patch regression

1. Use a clean `GameData/PartSculptor` installation with ModuleManager.
2. Verify a stock uncrewed tank gets one `ModulePartSculptor`.
3. Verify a crewed pod and cockpit each get one `ModulePartSculptor`; no PatchManager configuration is required.
4. Verify a part already declaring `ModulePartSculptor` is not given another instance.
5. Confirm `Patches/PartSculptor.cfg` is the sole module-injection patch and `Patches/ScaleExponents.cfg` is bundled.
6. With and without TweakScale installed, verify the fallback exponent rules apply only when TweakScale is absent.
7. Confirm that part-mass, resource-capacity and stage Δv readouts remain stable when the vessel is idle.
