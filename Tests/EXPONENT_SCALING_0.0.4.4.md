# Exponent scaling checks — 0.0.4.4

Requires a KSP 1.12.x installation with StretchScale and a `.cfg` defining the rules in the prompt. The source is not game-built in this environment.

1. **Database:** Start KSP and check `KSP.log` for the count of TWEAKSCALEEXPONENTS and TWEAKSCALEBEHAVIOR nodes. Confirm there are nonzero rules when the file is in GameData.
2. **Equivalent linear scale:** For a `ModuleEngines` engine at X=1, Y=2, Z=1, `maxThrust` and `maxFuelFlow` should multiply by `2^(2.5/3)` (about 1.7818); reset to 1,1,1 and confirm original numbers. Repeat several times to rule out drift.
3. **Uniform:** X=2, Y=2, Z=2 must multiply thrust by `2^2.5` (about 5.6569), and a 3-exponent resource capacity by 8.
4. **SRB behavior:** On a part with the SRB behavior's module patch copied by ModuleManager, its engine `maxThrust`/flow exponent should be 3, not 2.5; at X=1, Y=2, Z=1 the factor is 2.
5. **Engine behavior:** When applied to a part, `Part.mass = 2.5` gives dry mass `2^(2.5/3)` at X=1, Y=2, Z=1. On ordinary parts it remains 2x.
6. **Decoupler/Science/HeatShield behaviors:** When the behavior's per-part TweakScale exponent node is present, check that DryCost uses 0.5, -1.5, or 0.5 respectively; unsupported `type`/`freeScale` fields control TweakScale alone.
7. **Part stats:** Check breakingForce/Torque at area exponent 2; CoM/CoP/CoL offsets must scale independently by their matching axis.
8. **Nested resources:** Inspect a reaction wheel's ElectricCharge `rate` at exponent 3; a solar panel's producer output rate and chargeRate at exponent 2; a wheel motor's ElectricCharge rate at exponent 2 without changing unrelated resource entries.
9. **Converter lists:** `inputList = 3`, `outputList = 3`, `reqList = 3` should adjust each populated `ResourceRatio.Ratio`. Verify custom `inputList { Ratio = 3 }` works.
10. **FloatCurve:** Check wheel motor torqueCurve and solar temperatureEfficCurve values at their declared exponents, then restore scale. Key times must remain unmodified.
11. **Ignore:** A part with ModuleFuelTanks must not receive additional stock `Part.Resources` scaling; a part with ModuleEngineConfigs must not receive generic ModuleEngines/ModuleRCS exponent scaling.
12. **Reload and save:** Save a craft at nontrivial axes, reload, enter flight, return to editor, and verify no multiplying on successive loads. Test alongside TweakScale if installed.
13. **Other mods:** Trigger a fuel or engine switch; check the refreshed native baseline and verify changing axes only applies one stretch multiplier.
14. **No file:** Remove the exponent `.cfg`, restart, and verify original StretchScale mass/cost/resource/geometry behavior remains available while module-specific numeric properties remain unchanged.

**Limitations:** Some KSP and third-party modules cache values during OnLoad or OnStart. Those need dedicated adapters for the actual runtime effect; crew seat UI changes are not guaranteed live. Verify performance/behavior in game, particularly with engines, wheel suspension and solar panels.
