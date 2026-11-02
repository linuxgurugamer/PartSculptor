# Face morph handle visibility - 0.0.4.52

## What was changed
- Use `EditorLogic.fetch.editorCamera` first, not `Camera.main`, and validate that the camera sees the model's **actual render layer** within the editor viewport.
- Put the 3D corner cubes/cage lines on that model render layer; refresh after a layer change.
- Render an additional IMGUI **screen-space cage and eight handles** based on exactly the screen points used for picking. This is independent of layer culling, depth occlusion and 3D shaders.
- Draw from the editor addon `OnGUI`, not from the selected `PartModule`, so face handles can appear whether opened with the fifth tool or the PAW command.
- Show a visible message if no matching editor camera can be located.

## In-game checks (required)
1. Put one simple stock fuel tank in VAB, enable the sixth Morph tool, click tank. Expect eight GUI overlay handles (bottom four orange, top four cyan) and 12 cage lines. Rotate the editor camera and verify corners track the part.
2. Drag any cyan handle, then orange; verify the correct end face changes and mouse-up completes. Alt-drag and Shift-drag should function, with Esc cancelling a drag.
3. Activate **Show face morph handles** from a tank's PAW *without* entering the fifth tool; the overlay should still appear.
4. Use the stock tool buttons, exit editor, and unload the part. Verify cleanup and editor picking are not obstructed.
5. Check GameData/KSP.log and the face-status line if no handles show: collision/mesh eligibility errors are reported separately from camera errors.

## Limitations
No in-game KSP build or gameplay validation was available in the generation environment. The additional GUI cage is drawn on top of the geometry by design, including back-face handles. Existing conservative collision/mesh restrictions are unchanged.
