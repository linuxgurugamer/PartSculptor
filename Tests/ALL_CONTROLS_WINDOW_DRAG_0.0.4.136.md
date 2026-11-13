# PartSculptor 0.0.4.136 — All Controls window drag and part-name validation

1. In the VAB/SPH, place two attached FL-T400 tanks and open **Show PartSculptor controls window** from either part's PAW.
2. Confirm the selected part name is **bold**, while **Part:** remains normal weight. Select another part: confirm the bold name changes with selection.
3. Drag the window using the title bar, then the **Part:** label, a section heading, a read-only field label, and blank space outside the scrolling pane. The entire window should follow the pointer without changing any morph values.
4. Click the scale input, type a value, use **Set**, toggle **Match attached faces**, switch Top/Bottom numeric tilt, use **Apply face tilt**, and scroll with the wheel and scrollbar. Controls should respond without unintentionally dragging the window.
5. Select a part with no PartSculptor module; drag the placeholder from its message. Confirm the **Close** button still works.
6. Reopen the window with compact PAW enabled. Confirm dragging works and selecting another part retains the same window.

Requires in-game VAB/SPH testing: offline tests cannot simulate Unity IMGUI mouse event ownership.
