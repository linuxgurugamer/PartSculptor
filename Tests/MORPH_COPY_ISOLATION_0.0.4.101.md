# Morph independence on Alt-copy and symmetry — 0.0.4.101

Test in KSP 1.12.x editor (both VAB and SPH). Test with both a mesh-collider tank and a stock tank using a primitive collider.

1. Place a part, select Morph, but do not change its face dimensions. Alt-click to duplicate it, and attach the copy elsewhere on the craft. Morph the original. **Expected:** only the original geometry/collision shape changes; the standalone copy stays unchanged.
2. Morph a part noticeably, then Alt-copy it. **Expected:** the duplicate initially has the same saved shape. Morph or reset **only the original**. **Expected:** the copy retains its own shape and collider and does not change position/attach-node orientation.
3. Morph the copy independently, then the original again. **Expected:** both morph states remain independent; no shared-mesh deformation or double-deformation.
4. Copy a part with several child parts (Alt-copy subtree). Tilt a face on the original. **Expected:** only the original attachment subtree follows the morph; the copied subtree stays put.
5. Create a **genuine** symmetry group of two or more parts, then morph one. **Expected:** face values propagate intentionally to the group's symmetry counterparts, but their meshes/colliders remain independent instances. Duplicating the whole symmetry group should not link that new group to the old one.
6. Save and reload a craft containing both original and Alt-copied morphed parts. **Expected:** both keep their separate shapes and nodes, and future morphs stay independent. Undo/Redo a morph on one part.
7. Repeat with a part using primitive collision (proxy MeshCollider), especially one copied while the Morph handles were shown but no morph had yet been performed. **Expected:** no double morph, missing collider or physics warning.

Offline source checks cover callbacks, cloned visual/collision mesh ownership, startup fallback and intentional symmetry. They cannot exercise Unity/KSP's actual clone lifecycle; validate all cases in game before release.
