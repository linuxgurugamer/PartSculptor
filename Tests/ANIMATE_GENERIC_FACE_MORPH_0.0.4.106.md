# ModuleAnimateGeneric face-morph compatibility (0.0.4.106)

1. In VAB/SPH, select a part that contains `ModuleAnimateGeneric` and has readable static visual meshes and supported colliders (for example, an animated cargo bay with mesh-based doors). Select the Morph tool and check that it is no longer rejected solely because of the animation module.
2. Morph the part with the animation retracted. Inspect the deformed mesh and any collider proxy behavior. Deploy and retract the animation repeatedly; check whether the animated transforms remain aligned and collision geometry behaves acceptably. Animation transforms may not remain safe after severe morphs.
3. Reset the morph, save and reload the craft, then repeat the animation cycle. Confirm that saved geometry and collider state recover properly and that Ctrl+Z/redo in the editor has no unexpected interaction with animation.
4. Confirm a part with a `Robotic` module-name or `B9DisableTransform` module-name is still rejected for face morphing. Verify that an unsupported B9 subtype operation still produces its B9-specific rejection reason.
5. Verify `Settings.RestrictAnimatedParts` still independently controls ordinary editor stretch on supported `ModuleAnimate*` / `ModuleDeployable*` parts, and defaults to disabled.
6. Verify unreadable, skinned, or otherwise unsupported meshes and colliders still receive the existing specific failure reason. Test symmetry copies and flight load with a compatible animated part.

This is a source-level compatibility change. Full runtime behavior requires validation against actual KSP 1.12.x animated parts and mods.
