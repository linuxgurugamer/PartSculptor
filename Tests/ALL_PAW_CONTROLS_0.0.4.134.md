# PartSculptor 0.0.4.134 — All-PAW-controls window (KSP1 VAB/SPH)

1. In VAB, place one FL-T400 and open its PAW. Click **Show PartSculptor controls window**.
   Verify a scrollable and draggable **PartSculptor - All Controls** window opens,
   that moving the mouse over it does not select or zoom the ship, and that the
   PAW button changes to **Hide PartSculptor controls window**. Close and reopen.
2. Confirm Scale X/Y/Z, Lock proportions, Stretch children, Match node sizes,
   Match attached faces, Copy attached morph steps, Lock X/Z, and the other
   PAW fields all appear. Enter a new X scale but do not click Set: it must
   leave the part unchanged. Click Set, verify mesh/nodes/PAW/symmetry and Ctrl+Z.
   With Lock proportions checked, use Set X to verify all three axes update.
3. Change each global toggle using the window and verify the corresponding PAW
   control and another part's controls reflect the same session-wide setting.
   Change the toggle in the PAW and verify the open window updates too.
   Change Lock proportions and Lock X/Z, verifying symmetry counterparts.
4. With the same tank, select Bottom, enter a numeric tilt X=20, Z=0 and click
   **Apply face tilt**. Check that the Bottom face PAW readout, deformation,
   reset/undo and opposite-face preservation match the original PAW Apply.
   Switch to Top and confirm it loads Top's current angle without importing
   the un-applied Bottom text. Enter an invalid/too-large value: it must not
   deform the part. Verify existing Morph gizmo still works.
5. Use the window's Scale preset chooser to apply a built-in absolute preset;
   test Browse presets, Repeat preset, Reset scale, Swap X/Z, Show scaling
   gizmo, Show face morph handles, Reset face morph, Show scaled values, and
   Match face to parent now. All actions should match PAW behavior.
6. Confirm calculated Volume multiplier, Dry mass/cost, Tank capacity, Scale
   safety, Career scale limits and both face tilt readouts update live when
   editing through either window, PAW or gizmos.
7. With two stacked T400s, enable Copy attached morph steps from the window,
   morph an attached face by numeric tilt, and verify the preexisting mirrored
   child morph/rotation path. Then test Match attached faces independently.
8. Open the window from a second part: it should switch ownership to the new
   part without showing a second window. Delete the selected part, exit VAB,
   and reload: no ghost windows or input locks should remain.
9. Repeat with a symmetric part pair, a restricted/procedural part, and with
   the alternate window skin enabled. Restricted scaling actions should not
   circumvent existing restrictions. Also test on a small-screen resolution.

These are in-game checks. Python regressions validate wiring/source behavior
but cannot exercise Unity IMGUI, KSP Editor undo, or input locks.
