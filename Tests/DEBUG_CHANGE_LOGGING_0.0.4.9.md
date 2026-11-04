# PartSculptor 0.0.4.9 – debug change logging regression

## Enable / disable

1. Debug build, no explicit `debugLogChanges`: enabled by default.
2. Release build, no explicit `debugLogChanges`: disabled by default.
3. Add `debugLogChanges = true` in `PARTSCULPTOR_SETTINGS`: enabled in Release too.
4. Add `debugLogChanges = false`: disabled in Debug too.
5. Check `KSP.log` for `[PartSculptor][DEBUG]` and part name plus Unity instance ID.

## Part, resource and mass baseline stability

1. Add one fresh stock tank in the VAB, enable locked proportions.
2. Change to 1.25, then 1.50, then 1.25, then 1.00.
3. In `KSP.log`, inspect `[apply]`, `[geometry]`, `[mass]`, `[cost]`, `[resource]` and `[exponent]` lines.
4. Verify mass and stock resource multipliers are 1.953125, 3.375, 1.953125, 1 relative to the same unscaled baseline.
5. Verify raw resource `amount`/`maxAmount` before -> after, fixed capacity baseline, calculated mass/cost modifier deltas, and individual contributions from other enabled mass/cost modifiers (where applicable).
6. With the scale unchanged, leave editor open for a minute; no recurring identical exponent, mass/cost or resource change lines should appear.
7. Save and reload; verify original baselines remain stable and logging identifies the reloaded part.

## Modules, attach nodes, curves and callbacks

1. Scale a command pod and a part with ModuleReactionWheel; verify each changed bound field includes old -> new, native, exponent and factor.
2. Scale a part with a FloatCurve (e.g. ModuleWheelMotor torque curve); verify changed key values/tangents are shown and identical curves do not re-log.
3. Scale a part with stack and surface nodes; verify positions and integer node size tiers log only when changed.
4. Attach child parts, select Stretch children and resize; verify descendant movements identify the affected child and parent.
5. Use symmetry counterparts; verify each actual part has its own distinct part instance ID in the trace.
6. Exercise presets, Reset, Swap X/Z, lock proportions and Career limit rejection/clamping; check action/accepted/rejected lines.
7. With logging disabled, confirm none of these emit `[PartSculptor][DEBUG]` lines and scaling continues normally.

## Notes

The debugger logs values PartSculptor writes and the mass/cost deltas it returns to KSP. It does **not** claim to trace all unrelated writes performed by other mods or engine internals. In-game verification is required.
