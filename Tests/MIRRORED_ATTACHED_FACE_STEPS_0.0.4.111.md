> Historical plan for 0.0.4.111. World-corner coincidence fitting in Copy is superseded by mirrored incremental tilt steps in 0.0.4.113.

# 0.0.4.111 — Verify flush mirrored copy across attached stack faces

1. Use two identical stock PartSculptor cylinders/tanks, connected by their normal opposing Y stack nodes. In the PAW set **Copy attached morph steps = On**, **Match attached faces = Off**. Confirm no gaps before beginning.
2. Morph the parent's touching top face using X width, Z depth, Alt tilt around X and Z, and Shift+Alt offset, separately and combined. Inspect the joint from the side. The child's face must stay flush; child tilt must not be applied twice after its part transform rotates to the parent's tilted node.
3. Repeat editing the CHILD's touching bottom face, and verify the parent receives the mirrored result while the child does **not** rotate again when the parent changes its stack-node normal.
4. Test a 90°, 180°, and 270° roll of the attached part; mirrored face corners must follow the correct opposite/rotated physical corners, not the raw same local-axis parameters.
5. Resize parent and child to different original scales and repeat. The mirror duplicates physical corner displacement, so normalized face multipliers may differ between parts. If the two faces are not flush initially, use **Match face to parent now** first, then test Copy.
6. Morph a face's opposite, nonconnected end and confirm it does not alter its neighbor. Attach grandchildren and ensure the morph does not recursively propagate. Verify radial (Ctrl+Alt), edge handles, reset, drag cancellation (Esc/right click), and editor undo/redo restore the attached boundary consistently.
7. With symmetry, each mirrored joint must fit its own attached neighbor. With both Copy and Match enabled, Copy governs live morphs and Match still governs newly attached parts.
8. Try incompatible colliders, recessed or surface attachment, strong arbitrary roll/skew, and extreme face bounds/Career limits. Unsafe joints should be skipped without visible partial faces or crashes. In Settings, confirm the existing saved `copyAttachedMorphStepsDefault` toggle still works.

These tests require the actual KSP editor; offline math tests alone cannot validate renderer or attachment updates.
