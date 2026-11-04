# PartSculptor 0.0.4.27 — B9PartSwitch regression checklist

Requires KSP1 1.12.x, ModuleManager and B9PartSwitch 2.20.x in addition to
PartSculptor's standard dependencies. B9 itself is **not** mandatory.

## Minimal tank (two RESOURCE presets)

Use a test part with ModuleB9PartSwitch `moduleID=fuelSwitch`, baseVolume 100,
with Structural, LF/Ox and MonoProp tank types. Confirm the native per-subtype
resource capacities in a fresh editor session before stretching.

1. At axes 1,1,1, switch Structural -> LF/Ox -> MonoProp -> Structural.
   Resources should be created/removed and native capacities should match B9.
2. Set axes 1.5,1.5,1.5 (volume 3.375). Switch LF/Ox -> MonoProp -> LF/Ox.
   Every resource capacity should match that subtype's **native** capacity ×
   3.375; no doubling with repeated switches. Fill fraction is preserved when
   PartSculptor changes a capacity; subtype switching may reset resources per B9.
3. Change 1.5 -> 2 -> 1.5 -> 1.0 and verify every capacity and amount remains
   finite, regardless of switch history.
4. With LF/Ox selected, save craft, exit editor, reload and repeat steps 2-3.
5. Use two B9 switchers on one part (e.g., independent tank and paint moduleIDs),
   change both and ensure switching paint does not compound resource capacities.
6. Verify total **dry** mass and cost: B9's addedMass/addedCost/tankMass/tankCost
   each scale with the configured PartSculptor exponent, but are never added
   twice. Resources' own mass/cost are still calculated by KSP from PartResources.
7. Check B9 `NODE` position/size changes after scaling; save/reload, switch back.
8. Check B9 subtype `MODULE/DATA` changes (engine thrust/field changes): switch
   at 2×, verify the new subtype native value multiplied by the exponent once.
9. Verify multiple symmetry counterparts follow native B9's subtype behavior.
10. In flight, test a B9 part with `switchInFlight=true` (safe, nonstructural
    subtype) and ensure module values/resources update without exceptions.
11. Start without B9 installed; ordinary stock resources and mass/cost must
    behave exactly as in 0.0.4.26.
12. Switch B9 subtypes on an **unscaled** craft, compare B9 Part Switch alone,
    and ensure PartSculptor's presence makes no numerical difference.

## Known limitations / observations

- B9's own subtype restrictions and switched model transforms remain B9-owned.
- No B9 DLL reference is bundled. No test here substitutes for an actual KSP
  compile and in-game run.
- Test third-party mods that also manage tank resources separately.
- Check specialized B9 `NODE` + `TRANSFORM` arrangements and flight physics.
