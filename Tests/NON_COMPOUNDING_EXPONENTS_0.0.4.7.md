# Exponent baseline / non-compounding regression (0.0.4.7)

The source regression corrected in this release was `ExponentBinding.Apply()`
replacing the immutable `Native` with the field's current (already scaled)
value if it differed from `LastWritten`. That could apply `s^e` repeatedly.

## Core checks in KSP editor

1. Use an ordinary stock tank with `ModuleStretchScale`, global `Part` and `TweakScale` rules, and NO other scaling mods. Verify the *dry* mass (not fuel mass) and capacity start at their native values.
2. Disable Career Mode scale limits if needed. Set X, Y and Z all to 2.0. For cubic mass and capacity exponents expect exactly 8x native mass and capacity.
3. Click the 2.0 control several more times, open/close the PAW, wait for several Update() geometry-check periods and click Refresh in the editor: dry mass, capacity and module values MUST NOT grow.
4. Change X/Y/Z to 1/2/1. Native mass and capacity must be 2x (not 8x); change back to 2/2/2 and expect 8x, then reset to 1/1/1 and expect 1x.
5. With a global ModuleEngines rule `maxThrust = 2.5`, an engine at 2/2/2 must report `nativeThrust * 2^2.5`, independent of how often that scale was revisited. Check an SRB behavior overriding it to exponent 3.
6. With a converter rule `inputList = 3`, verify the Ratio of every entry stays at `nativeRatio * (X*Y*Z)`, not a cumulative multiplication at each edit.
7. On a scaled craft save and reopen: each ModuleStretchScale MODULE must include `STRETCHSCALE_EXPONENT_BASE` entries with original numeric/vector/curve values; scale change and revert after loading must produce the same values. This is the primary test for fields that KSP serializes already stretched.
8. Duplicate the scaled part, scale the duplicate back to 1x and verify its values restore to prefab-or-original values without affecting the source.
9. With `ModuleFuelTanks` or another fuel switcher, change tank types and repeat with + without TweakScale. If external switchers mutate fields without replacing the module, special integration may still be required; values must NEVER grow on repeated unchanged scale operations.
10. Check KSP.log for `StretchScale` exceptions, NaN, Infinity; repeat in VAB, SPH, and on craft reload from flight.

## What is now enforced

- Bindings capture an immutable native number, vector, or FloatCurve at initialization.
- For each edit, `new = native * pow(cuberoot(X*Y*Z), exponent)`; part offsets use native component * matching axis.
- `Apply()` cannot adopt the previous scaled value as the new native value.
- Baselines are persisted in the craft's `ModuleStretchScale` ConfigNode and restored at startup.
- Legacy craft (without baseline records) attempt prefab matching and, when required, divide loaded values by the persisted stretch factor.
- Ordinary module value changes by other mods are not adopted automatically anymore; third-party live switchers may require targeted integration to rebind to their new unscaled values safely.

Not compiled or tested against KSP assemblies in this environment. These are manual in-game verification steps, not claims of completed in-game testing.
