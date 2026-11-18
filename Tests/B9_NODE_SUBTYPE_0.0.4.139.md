# B9 subtype stack node restoration — 0.0.4.139

1. Install ORANGES and B9PartSwitch and load `oranges_NLSUS_interstage` in VAB. Disable Copy attached morph steps and Match attached faces to isolate native stack-node positioning.
2. Select `NLSUS-B` (short mesh), which specifies `NODE { name = top; position = 0, 1.208, 0 }`. Stretch only Y to 1.5. The top node should be at Y=1.812 (not 1.208) relative to the part.
3. Change to `NLSUS-A` (long mesh), which has no NODE override and defaults to Y=2.4885. The top node should be at Y=3.73275, visible above the longer mesh, not buried in its center.
4. Toggle B→A→B repeatedly. Verify stable positions and sizes without progressive drift, despite any previously saved node geometry history.
5. Repeat with a part connected to the top node, check that the attachment remains linked and that no duplicate movement or jump occurs.
6. Repeat with an editor save/reload, stock part symmetry, and a face morph on the switched part. Verify that B9 still independently controls active model meshes.
7. Verify that a subtype that explicitly toggles node visibility remains under B9's control; PartSculptor must never create a node that B9 removed.

This plan requires actual KSP/Unity testing. Offline tests only check source paths and expected native coordinate selection.
