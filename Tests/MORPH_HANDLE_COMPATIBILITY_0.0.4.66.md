# Morph gizmo compatibility and camera selection test (0.0.4.66)

1. In VAB, activate Morph and select a stock FL-T400. Verify all eight handles.
2. Select a long/offset static part, zoom so that some corner handles remain
   visible while the overall visual bounding-box center is beyond the viewport.
   Verify visible corners remain drawn and draggable. Orbit and repeat.
3. Select a part whose renderer uses a different layer, if available; verify
   editor-camera projection and 2D markers still work.
4. Select a part without `ModuleStretchScale`: verify explicit missing-module
   message in the gizmo help, upper screen message, and KSP.log.
5. Try a part with an animated or switchable mesh, a non-readable mesh, and
   a non-supported collider: verify a specific, part-named rejection reason.
   Do NOT enable morphing of unsafe colliders or animated mesh instances.
6. Orbit so that the selected part is entirely behind the camera and confirm
   the tool gives a camera-position hint.
7. Test ordinary Stretch handles, original face tilt, cloning, snap and
   attachment behavior to ensure no regression.

KSP and .NET Framework assemblies are required for the runtime tests.
