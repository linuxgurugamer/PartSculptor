# Face morph gizmo occlusion — 0.0.4.64

## In-game checks (VAB/SPH)

1. Place a stock FL-T400 and put another tank directly on its **top node**.
   Select the lower tank with the sixth **Morph** tool and orbit the camera to
   look toward the shared joint. All four cyan top-corner handles and cage
   edges should remain clearly visible **in front of** the upper tank.
2. Drag the cyan handle that appears inside the silhouette of the attached
   upper tank. Confirm that the lower tank changes shape and the upper tank is
   not accidentally selected. Repeat with ordinary drag, Alt, Shift+Alt and
   Ctrl+Alt modes.
3. Place a tank below the selected tank; repeat for all four orange handles.
4. Rotate and zoom the camera, including a view where 3D handles are occluded
   by the selected tank. The overlay stays aligned with the same mouse hit
   positions. Hover and dragging should make a handle larger.
5. Open the PAW and settings window, move the camera and switch between Morph,
   Stretch and a stock tool. Confirm overlay handles don't block UI input and
   disappear upon exiting Morph. Test **Show face morph handles** from the PAW.
6. Delete the selected part and leave the editor. Confirm no remaining overlay
   or input locks; look for exceptions in KSP.log.

## Offline checks

`python Tests/run_regressions.py`

No game runtime or KSP-dependent build available in this environment.
