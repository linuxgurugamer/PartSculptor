# B9PartSwitch TEXTURE subtype + face morph: manual KSP tests (0.0.4.102)

Run in KSP 1.12.x with B9PartSwitch installed. A simple test part should contain
`ModulePartSculptor`, a readable static model, and a B9 switcher with at least
2 subtypes that only differ by `TEXTURE` nodes. Example (adapt texture paths to
ones actually installed):

```cfg
MODULE
{
    name = ModuleB9PartSwitch
    moduleID = materialVariant
    switcherDescription = Appearance
    SUBTYPE
    {
        name = Original
        title = Original
    }
    SUBTYPE
    {
        name = Painted
        title = Painted
        TEXTURE
        {
            texture = MyPack/Parts/TestPart/PaintedTexture
            currentTexture = OriginalTexture
            transform = BodyMesh
        }
    }
}
```

1. In VAB/SPH, add the part; select Morph. Handles should appear (not a B9
   compatibility error), assuming a supported collider/mesh.
2. Morph it to an obvious taper. Change B9 subtype Original -> Painted -> Original.
   Confirm renderer texture changes and *taper stays intact*. Verify matching
   collider and attachment placement; switch again after morphing a second time.
3. Repeat with multiple TEXTURE nodes; `baseTransform` targets; and a
   RESOURCE + TEXTURE subtype. Ensure B9 resource capacities still behave.
4. Save craft, reload, switch variants and morph again. Alt-copy the part and
   separately switch/copy/morph each; verify the other part's shape/texture is
   unchanged. Also test symmetry counterparts.
5. Negative tests: introduce a subtype with `TRANSFORM`, mesh `transform`,
   `NODE`, `MATERIAL`, or unknown nested node, and verify Morph is refused with
   a diagnostic. Introduce missing `texture` path in a `TEXTURE` node and verify
   it is refused; malformed B9 itself may also refuse to load.
6. Validate when the skin is changed through B9 with the Morph tool already
   active, while drag handles are visible, and after Ctrl+Z/Ctrl+Y.

`TEXTURE` is a renderer-material switch, not a true mesh/geometry switch.
This update does **not** enable morphing of geometry-changing B9 subtypes.

Offline checks validate the code paths but cannot prove B9/KSP runtime
integration, material assignment order, drag cubes, or collider behavior.
