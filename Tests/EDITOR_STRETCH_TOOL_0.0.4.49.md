# Stretch tool regression checklist (0.0.4.49)

- Enter VAB and SPH: a fifth Stretch button appears directly to the right of Re-root.
- Click Stretch, then click an FL-T800 with ModulePartSculptor: 3 axis handles appear.
- Click an engine with ModulePartSculptor: gizmo moves to the engine; tank stays attached.
- Click a part without ModulePartSculptor: gizmo hides; craft remains unchanged.
- Click gizmo handle and drag; handles track cursor and existing scaling rules apply.
- Hold Shift to snap; Esc during drag restores starting scale without exiting mode.
- Press Esc again (without dragging) to exit; lock is released.
- Click Place, Offset, Rotate and Re-root in turn; Stretch tool deactivates, no stale lock.
- Press stock tool keys 1-4, including 1 while Place is already active: exits Stretch mode.
- Click Stretch a second time to exit; reselect and then clear/load craft.
- Switch VAB->SPH and leave editor; widget, gizmo and input locks are destroyed.
- Test UI scaling / resolution changes: button stays beside stock tools, not clipped.
- Confirm existing PAW 'Show scaling gizmo' remains operational.
- Test crewed and uncrewed parts under Career safety restrictions.
- Ensure tool icon loads from PluginData without relying on GameDatabase.
