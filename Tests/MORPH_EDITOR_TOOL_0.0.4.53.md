# Dedicated Morph editor gizmo — 0.0.4.53

Two independent custom buttons are placed after the stock editor's four tools:

- **Fifth: Stretch** — original X/Y/Z gizmo; stretches the whole selected part.
- **Sixth: Morph** — eight corner handles (orange = lower end face, cyan = upper end face) and twelve cage edges; changes face width/depth and tilts planar end faces using the existing face-morph calculation and save/load pipeline.

Select a mode by clicking its button, then click a StretchScale part. The buttons are mutually exclusive. Clicking the other button switches the current selected part in place without moving or detaching it. Clicking the active button again exits. **M** still switches tools as a shortcut; the old IMGUI mode-selection button is no longer needed. Stock tool buttons or hotkeys 1–4, Esc (when not dragging), scene change, and UI teardown exit the custom tool. PAW **Show face morph handles** remains available independent of either toolbar selection.

Morph controls: drag a face corner for taper (X/Z face width/depth); Alt-drag for planar end-face tilt; Shift to snap to 0.05; Esc/right-click during a drag to cancel. Morphing is limited to supported static mesh/primitive-collider parts, as in 0.0.4.52. An on-screen reason is shown when a part cannot morph.

## KSP in-game checks (not available offline)

1. Open VAB and SPH; confirm **six** tools: stock four, green Stretch icon, distinct turquoise/orange cage Morph icon; buttons must not overlap at different editor UI scales.
2. Click Morph then an FL-T400 stock tank with ModuleStretchScale: eight face handles and cage lines should appear, and one morph drag should change geometry without detaching the tank.
3. Switch Morph → Stretch → Morph on the same tank; current part remains selected and only one set of gizmo handles appears at once.
4. Click Morph again; both handles and pickup lock disappear. Repeat with Escape, native tools, and hotkeys 1–4.
5. Press M while a custom tool is active; expect highlighted toolbar button and gizmo to switch. Pressing M while dragging must not change modes.
6. Save/clear/reload a morphed tank and verify shape, colliders, resource volume and symmetry persist.
7. Test unusable/unsupported mesh and collider types; expect a rejection message rather than silent no-op.
8. Confirm original fifth Stretch and both Part Action Window gizmo commands continue to work.

Offline source-structure tests validate wiring, cleanup and versioning but are not proof of KSP runtime behavior. KSP DLLs are not included with the project.
