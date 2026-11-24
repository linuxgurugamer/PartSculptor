# PartSculptor 0.0.4.149 — Procedural Geometry validation

1. In VAB place a stock tank and select the **PG** (seventh) widget, then the tank. Add a ring. Check that the added visual vertex count becomes nonzero, and the mesh looks unchanged while X/Z are 1.
2. Change ring height to 50%, X to 1.5, Z to 0.75, and apply. Check that a visible bulge/constriction forms, while the two end faces and attachment nodes remain stationary.
   Observe that a convex collider may not reproduce a deep inward constriction (it uses a convex hull).
3. Add another ring at a different height and edit it. Verify independent deformation, correct textures/UVs and no visible cracks. Remove a ring, then Clear all; the original topology and collider should return.
4. Undo/redo an Add, Apply, Remove and Clear operation; make sure symmetry counterparts remain identical and no duplicated meshes remain.
5. Save, reload in the editor, launch to flight, quicksave/load and return to the VAB; verify shape, collider, resource capacities, end nodes and attached parts.
6. Repeat with a B9PartSwitch length-switched part, including switching before and after edits and reloading a craft. Every subtype should keep its own mesh selection and correct attachment nodes.
7. Check a primitive-collider stock part, a detailed convex-collider part (should refuse operations exceeding 255 triangles), a part with multiple submesh materials, and a part with unreadable/skinned geometry (should refuse safely).
8. Verify that Stretch, Morph and PG widgets are mutually exclusive and activating a native editor tool exits PG. Test right-click/PAW and Ctrl+Z shortcuts while the PG panel is open.

No in-game testing was possible in the source packaging environment.
