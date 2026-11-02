# Behavior selection and inheritance regression checks — 0.0.4.5

Requires KSP 1.12.x and the GameData exponent/behavior configurations described in README. These are manual in-game checks; no local KSP compile/runtime was available when authoring this release.

1. **Selection without TweakScale**: Install the TWEAKSCALEBEHAVIOR definitions but no TweakScale DLL or PartModule. Apply `MODULE { name = ModuleStretchScale; behavior = SRB }` to a stock solid booster (ordinary KSP multiline ConfigNode syntax). Verify log contains `uses behavior(s): SRB` and maxThrust/maxFuelFlow use exponent 3 instead of global 2.5.
2. **Engine preset**: Select `behavior = Engine`; at axes (1,2,1) dry mass should use exponent 2.5 instead of 3. Confirm resetting axes restores original mass without compounding.
3. **Precedence**: Select `behavior = SRB`; add local `TWEAKSCALEEXPONENTS { name = ModuleEngines; maxThrust = 2 }`. Verify maxThrust exponent 2 and flow stays 3.
4. **Ordered selection**: With `behavior = Engine, SRB`, verify both Part.mass (2.5) and engine thrust (3) rules apply. Swap ordering with two presets overriding the same field to confirm last one wins.
5. **Explicit parent**: Add `TWEAKSCALEBEHAVIOR { name = Child; inherits = Engine; MODULE { name = TweakScale; TWEAKSCALEEXPONENTS { name = ModuleEngines; maxThrust = 3 } } }`, select `behavior = Child`. Verify both rules apply.
6. **Module-level parent**: In a custom behavior set `MODULE { name = TweakScale; inherits = Engine; ... }`. Verify it works like root `inherits`.
7. **Circular definitions**: A inherits B, B inherits A. Ensure no hang/overflow; expect a logged circular-inheritance warning.
8. **Missing behavior**: Select an unknown behavior and confirm a warning in KSP.log and otherwise normal scaling.
9. **Crewed square type**: On a suitable uncrewed test part, select `behavior = Crewed` and load the stock `SCALETYPE stack_square` definition; dry mass exponent should be 2. Remove SCALETYPE defs and confirm fallback remains 2. (Do not test by changing occupied crew seats.)
10. **Science growth limit**: Select `behavior = Science` and attempt X=1.5. It should be capped at 1x. Shrinking should work. Previously saved crafts at >1x should load without automatically changing size.
11. **Legacy behavior**: With both TweakScale and StretchScale modules and no StretchScale behavior selected, a local exponent override in the TweakScale module should continue working. Once `ModuleStretchScale.behavior` is set, only explicitly chosen and local StretchScale overrides should determine the exponents.
12. **Craft reload and symmetry**: Save a craft with inherited behavior, reload it, clone it, and exercise symmetry, resetting axes and child stretching; verify no drift and that each instance resolves its own ModuleStretchScale config.

**Known scope:** behavior/SCALETYPE exponent definitions are inherited, and `allowGrow=false` is enforced for new edits; TweakScale's scalar UI (`scaleFactors`, `freeScale`, `defaultScale`, `suffix`) does not replace StretchScale's three independent axis controls.
