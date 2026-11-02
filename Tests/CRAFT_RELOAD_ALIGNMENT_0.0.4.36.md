# Craft-load attachment regression — 0.0.4.36

1. In VAB, build Mk1 pod -> FL-T800 -> LV-T45 engine. Leave pod as root.
2. Lock proportions on tank; set 1.5x. Verify pod and engine remain flush with tank nodes.
3. Save to a NEW craft name; clear editor; load same craft. Confirm no pod/tank intersection or detached engine.
4. Repeat clear/load twice more; verify there is no position drift.
5. Scale tank 1.5x -> 2x -> 1x and repeat save/reload.
6. Build reversed-root variant (tank as root, pod as child), save/load.
7. Move the pod slightly using editor offset gizmo, save a new craft and reload; check offset preserved.
8. Test surface-mounted batteries, radial boosters and rotated stack nodes; positions unchanged.
9. Test prefabs B9 subtype changes after loading; re-scaling must not re-run whole-ship snap.
10. Test a flight launch and revert to editor. The post-load alignment must not run in flight.
11. Confirm no extra `onEditorShipModified` storms and that PAW/Gizmo edits remain unaffected.

C# unit assertions in RegressionRunner test absolute post-load correction and a saved offset.
Offline Python smoke tests enforce startup-only scheduling, stack references, and persistence.
KSP in-game validation is still required.
