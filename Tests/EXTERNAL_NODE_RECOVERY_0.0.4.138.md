# PartSculptor 0.0.4.138 — external attachment-node recovery test plan

This change ports and adapts the external attachment-node history in the supplied
StretchScale 1.3 to PartSculptor, including its separate face-morph node positions.
These tests **must be run in the KSP 1.12.x editor and flight**: Python source
checks cannot validate Unity transform parenting or foreign-mod update timing.

1. **Existing saved craft** — Load a v0.0.4.137 craft with a stretched and morphed
   FL-T400, attach another tank and save/reload twice. Confirm node positions,
   face angles and intentional gaps remain unchanged. No repeated drift.
2. **Morph-first stability** — Morph a tank face, wait > 30 frames, then change
   its ordinary stretch and morph again. Verify no false foreign rebase, no
   doubling of node deformation, and the exact same visible result after reload.
3. **External switch** — Attach a child to a B9 part with supported static
   geometry and a subtype that changes stack-node positions. Switch A→B→A
   after the first 30 editor frames, verify movement of parent-connected
   parts and node coincidence, then save/reload. Verify resource capacities
   and B9 subtype state remain stable. Unsupported B9 dynamic mesh changes
   should remain excluded from face morphing.
4. **Stock variant** — Change a stock part variant that redefines node positions,
   and confirm PartSculptor restores the stretched/morphed node positions
   instead of resetting to the prefab node. Repeat without changing model scale.
5. **TweakScale** — Stretch, TweakScale, change TweakScale back; verify expected
   node coordinates and no double scaling, and that already-moved descendants
   are not moved twice.
6. **Parent/child edits** — With two FL-T400 tanks, test copy and match attached
   faces separately, in both driver directions; verify the 0.0.4.137 morph and
   child-rotation behavior remains unchanged.
7. **Surface attachments** — Change scale and variants on parts with radial
   children; inspect attachment node anchor, offset and saved craft stability.
8. **Flight startup** — Launch a stretched/morphed craft with a variant switcher
   that may overwrite nodes on start. Observe first 120 frames and verify that
   repair does not move the vessel or alter resources/fuel amounts.
9. **Undo and symmetry** — Undo an editor stretch/morph, redo, then save/reload.
   Test radial and mirror symmetry. No compounding native baseline.
10. **Unscaled ownership** — On a part with PartSculptor scale 1 and no face morph,
    change the stock variant; ensure the native node is not overwritten merely
    because PartSculptor is installed.

Use `debugLogChanges = true` in `GameData/PartSculptor/PluginData/Settings.cfg`
and look for `node` / `foreign native node` messages in KSP.log when a variant
replaces the node. The external-node history is saved in each module as
`PARTSCULPTOR_NODE_HISTORY`; do not hand-edit these records.
