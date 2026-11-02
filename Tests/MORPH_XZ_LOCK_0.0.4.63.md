# 0.0.4.63 Morph Lock X/Z manual verification

1. In VAB put a simple FL-T400, select sixth Morph tool, open the part PAW and confirm **Lock X/Z = On** for a new part. Resize a corner with no modifiers: both face width and depth change by a single factor.
2. With Shift held before dragging, check that both axes still preserve their ratio through each snapped step. Check rectangular faces with independent X/Z scaling already applied.
3. Turn **Lock X/Z = Off** in the PAW and drag diagonally/along X only. Confirm X and Z can change independently.
4. Confirm Alt tilts, Shift+Alt moves the entire face, and Ctrl+Alt performs a radial resize independent of the toggle.
5. Save craft with Lock X/Z Off, reload, and confirm it remains Off. Clone and symmetry copy: check toggle follows the part.
6. Vessel Inspector → Settings: default should be On; change to Off, Save, place another tank; the new part should be Off while existing parts retain their own choices. Reset to On and repeat.
7. Settings dialog must not display the file path line.

The C# runner verifies shared-factor math; the Python suite checks wiring and persistent settings. A real KSP runtime test is still required.
