# 0.0.4.65 — Always-visible Morph and Stretch gizmo regression

In the VAB, place two stock FL-T400 tanks vertically, attached end-to-end.

1. Select the lower tank with the sixth **Morph** tool. Confirm that both
   the four top cyan and bottom orange corner markers and the cage lines
   remain visible even where the upper tank overlaps the lower tank's face.
2. Move the camera and zoom. Confirm the foreground squares stay aligned with
   the selected tank's eight corners and do not lag behind the 3D scene.
3. Drag one of the previously obscured top corners; confirm the selected face
   morphs and the attached tank is not inadvertently selected.
4. Switch to the fifth **Stretch** tool and select the same lower tank. Confirm
   the red, green and blue axis drag squares and their stems remain visible in
   front of either tank wherever the gizmo would otherwise be occluded.
5. Drag an axis marker previously obscured by the adjacent tank. Confirm only
   its corresponding PartSculptor axis changes; Shift snapping and Esc cancel
   still work.
6. Switch tools repeatedly, deselect the part, and leave the editor. Confirm
   gizmo markers and temporary input locks disappear.
7. Also check an isolated tank, a tilted or translated morph, UI scale changes,
   and the SPH. Verify markers don't intercept toolbar/PAW input.

If either gizmo has no markers, check KSP.log for exceptions from
`EditorStretchTool.OnGUI`, `FaceMorphGizmo.DrawLabels` or `ScaleGizmo.DrawLabels`.

Offline source checks: `python Tests/run_regressions.py`.
No KSP game or Unity runtime is available for automated in-game visibility checks.
