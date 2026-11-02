# Automatic exponent matching — 0.0.4.6

These are manual KSP 1.12.x regression checks. The implementation has not been compiled or run in KSP here.

## Precondition

Place a `.cfg` containing top-level `TWEAKSCALEEXPONENTS` definitions in GameData. Ensure the part being tested contains `ModuleStretchScale`, but **do not set** `behavior`, `inheritBehavior`, or any per-part overrides. TweakScale DLL/PartModule need not be installed.

1. **Database load**: Check the KSP log for `Loaded N exponent rules`. `N` must be > 0 when the `.cfg` is present. Definitions with the same `name` merge by individual field, rather than being discarded.
2. **Part rule automatically**: For a part with `breakingForce = 2` and `breakingTorque = 2` in `TWEAKSCALEEXPONENTS { name = Part }`, scale (1,2,1). Expect both values to multiply by `2^(2/3)` without setting `behavior`.
3. **Module rule automatically**: For a liquid engine containing `ModuleEngines` and a global exponent `maxThrust = 2.5`, scale (1,2,1). Expect the engine's maxThrust to multiply by `2^(2.5/3)` (~1.7818). Reset to (1,1,1) and repeat to check for drift.
4. **Subclass matching**: Test a `ModuleEnginesFX` subclass with a global `ModuleEngines` definition. Confirm inherited exponents are honored; a more-specific `TWEAKSCALEEXPONENTS { name = ModuleEnginesFX; maxThrust = 3 }` wins over the base default.
5. **Registered moduleName alias**: For a custom PartModule whose registered `moduleName` differs from its runtime C# class name, define an exponent rule with that `moduleName`. Confirm the rule is applied without behaviors, while class-name and ancestor rules continue to work.
6. **Other stock modules**: Confirm reaction-wheel torque (`PitchTorque`, etc.), parachute drags, RCS power, converter resource ratios, and solar chargeRate scale only on parts carrying those modules.
7. **Explicit override precedence**: Assign `TWEAKSCALEEXPONENTS { name = ModuleEngines; maxThrust = 2 }` inside `ModuleStretchScale` on one part. That part uses 2, while another with no override uses the global 2.5. A more specific local `name = ModuleEnginesFX` rule wins over a local `name = ModuleEngines` rule on a derived engine.
8. **Behavior is still optional**: Set `behavior = SRB` on an SRB and confirm its exponent-3 thrust override wins. Remove the behavior and verify global exponent-2.5 thrust still applies automatically.
9. **Exclusion**: When a part contains `ModuleEngineConfigs`, the global rule with `-ignore = ModuleEngineConfigs` does not scale that engine. `Part.Resources` scaling excludes parts with `ModuleFuelTanks` when specified.
10. **Without rules**: Remove the `.cfg` from GameData and restart KSP. Geometry, mass, cost, and resource defaults still work; unconfigured module fields must remain unchanged.

**Reminder:** These tests assess rule discovery and numeric properties, not every KSP module's runtime caches. Some modules may need custom after-scale refreshes to update their actual in-flight effects.
