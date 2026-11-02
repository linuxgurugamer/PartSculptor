# Morph help panel positioning (0.0.4.60)

## In-game VAB/SPH acceptance checks

1. Enter the VAB and add a stock FL-T400 tank.
2. Select Morph (sixth editor tool), then click the tank.
3. Confirm **one** wrapped information panel appears immediately below the
   row of six tools, its left edge aligned with the first (Place) tool and its
   right edge aligned with the Morph tool.
4. Verify text wraps within the panel rather than continuing to the right.
5. Scale the KSP UI and/or resize the game window: the panel should remain
   aligned beneath the tool strip, and must not move left of Place.
6. Choose an unsupported part: the error message should appear in the SAME
   anchored area, not at the old top-left fixed position.
7. Test activating via the part-action-window Morph option.
8. Confirm drag/tilt/translate/radial handle modes still work, and dragging
   displays the existing small tooltip near the cursor.
9. Switch tools and editor scenes: no orphaned help panels or input locks.

Offline: `python -m unittest discover -s Tests/python_regressions`.
