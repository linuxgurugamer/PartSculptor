# PartSculptor 0.0.4.24 Vessel Inspector regression checklist

1. In VAB/SPH and flight, open Vessel Inspector: **Scaled parts only** begins checked.
2. With all parts at scale 1.0, the list says no parts have been scaled.
3. Scale two different parts, reopen; verify only those parts appear.
4. Hover each entry: only the corresponding part on the craft/vessel is highlighted; data selection does not change.
5. Move mouse to empty list space, checkbox, scrollbar, right pane or outside window; the highlight goes away.
6. Scroll the list: the hover target follows the visible row, not an offscreen item.
7. Change the filter so the hovered part disappears; the highlight is removed immediately.
8. Close the inspector while hovering, change scenes, undo/delete a hovered part, or change active vessel; no stale highlight or exception.
9. Check a part already highlighted by KSP/another mod; after leaving the row its prior highlight color, mode and active state return.
10. Clicking a left-pane row still selects its report; clicking a part on the vessel still follows existing selection behavior.
11. Toolbar button is registered for VAB, SPH, FLIGHT and MAPVIEW; Editor-only inspect toggle stays hidden in flight.
12. No Layout/Repaint control-count errors while hovering or moving the resizable window.

Manual KSP 1.12.x testing required.
