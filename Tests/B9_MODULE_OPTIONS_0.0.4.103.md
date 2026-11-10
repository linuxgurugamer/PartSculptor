# B9PartSwitch module options — manual verification (0.0.4.103)

In the editor, install B9PartSwitch and load a part that has a `ModuleB9PartSwitch`
with `affectFARVoxels = true` and/or `advancedTweakablesOnly = true` at the
**MODULE** level, and otherwise morph-safe SUBTYPE nodes.

1. Select the Morph tool and confirm the part's handles are available.
2. Morph the part and change between resource-only or TEXTURE subtypes;
   confirm geometry and textures remain correct.
3. Repeat with each option set to `false`, with either option absent, and
   with both options present.
4. Confirm real geometry switchers (TRANSFORM, NODE, MATERIAL, etc.) are still
   rejected for face morphing, and unknown MODULE fields still show a reason.
5. Save/reload a craft, and test an Alt-duplicate and a symmetry counterpart.
6. If FAR is installed, check drag/voxel updates in game; allowing a config
   field alone does not verify FAR integration or aerodynamic behavior.

Offline tests are source-level checks, not a substitute for KSP runtime testing.
