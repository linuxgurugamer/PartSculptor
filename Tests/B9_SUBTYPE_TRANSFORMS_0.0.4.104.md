# B9PartSwitch SUBTYPE transform toggles — KSP 1.12.x manual test (0.0.4.104)

This update permits `parentID` on `ModuleB9PartSwitch` and `transform = ...` (repeated if necessary) directly inside a `SUBTYPE`, where B9 enables/disables existing GameObjects. Nested `TRANSFORM { positionOffset, rotationOffset, scaleOffset }` blocks are **still unsupported**. This is not a full geometry replacement or model rebuilding system.

## Suggested test part

Use a part with two B9 subtypes that toggle two distinct pre-existing, static, readable mesh groups and their colliders, for example:

```cfg
MODULE
{
    name = ModuleB9PartSwitch
    moduleID = meshSwitch
    affectDragCubes = true
    SUBTYPE
    {
        name = VariantA
        transform = modelA
    }
    SUBTYPE
    {
        name = VariantB
        transform = modelB
    }
}
```

For `parentID`, use a separate known-good B9 parent/child switcher configuration with that field on the **MODULE**, not the SUBTYPE.

## Checklist

1. Start in VAB/SPH with VariantA visible. Open Morph and verify handles appear; Morph must not silently turn on the hidden VariantB mesh.
2. Morph VariantA (resize, offset, tilt). Switch B9 to VariantB. Confirm the newly visible model is also morphed and VariantA is hidden.
3. Switch back to VariantA. Confirm its morph remains and no duplicate transformations accumulate after repeated subtype switching.
4. Verify colliders in both variants agree with visible geometry; attach additional parts and tilt the morph faces. Confirm attached parts follow their nodes.
5. Alt-duplicate the morphed part, change the subtype of one copy, and morph it again. Verify the other copy remains unchanged.
6. Repeat with symmetry counterparts, save/reload the craft, and launch to flight. Verify both the active variant and collider are correct in flight.
7. Combine `transform = ...` with `RESOURCE` and `TEXTURE` in the same subtype, and verify resource volume/capacity as well as textures across switches.
8. Check that `SUBTYPE { TRANSFORM { ... } }`, `NODE`, `MATERIAL` and `MODULE` nested blocks are still rejected, with a clear morph compatibility reason.
9. Confirm `parentID` no longer causes otherwise safe B9 modules to fail the morph safety check.
10. Verify malformed empty `transform =` fields are rejected. Also test multiple values, a wildcard selector and a regex selector when present in your modded game.

## Notes

- Meshes from **inactive** variant GameObjects are captured when morph support is created; they must exist at that time. This does **not** support dynamic mesh creation/replacement, animated/skinned geometry, or moved B9 attachment nodes.
- For variants with very different bounding envelopes, the morph cage is based on the union of all static variant meshes. Check the resulting morph against each model's intended shape.
- Actual in-game behavior requires validation with your B9PartSwitch version; offline source tests alone cannot verify Unity transform activation and collider behavior.
