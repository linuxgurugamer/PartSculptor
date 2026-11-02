# Cloned face-morph node alignment (0.0.4.62)

## In-game editor test (VAB and SPH)

1. Place a stock FL-T400 fuel tank. Activate Morph and Alt-drag the top
   corners until the upper face is visibly sloped.
2. Inspect its top stack node from the side; verify its normal is perpendicular
   to the sloping face.
3. Alt-click to duplicate the morphed tank in the editor. Leave the clone
   unattached for visual comparison and inspect its upper face and node.
4. Confirm the clone's face angle exactly matches the source and the clone's
   attach node points outward at 90 degrees to its upper face.
5. Tilt the clone again, duplicate the clone, and compare all three parts.
   No new tilt may accumulate from one copy to the next.
6. Repeat with bottom-face tilt, face width/depth taper, face translation,
   and Ctrl+Alt radial resize. Verify attachments still have correct normals.
7. Attach a small tank or docking port to the tilted node, duplicate the craft
   branch, and check attachment position and direction. Save and reload.
8. Repeat with a primitive-collider tank and with a readable MeshCollider part.
   Observe that cloned physics meshes do not retain doubled deformation.
9. Reset the clone's face morph and ensure the mesh, collider, and nodes all
   return to their unmodified baseline without affecting the original part.

Offline checks: `python Tests/run_regressions.py` (C# math runner requires the
.NET SDK; live scene cloning requires KSP).
