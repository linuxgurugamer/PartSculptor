# PartSculptor 0.0.4.77 — Vessel Inspector Show Unchanged Values

1. In the VAB/SPH, create a craft with at least two scaled parts. Open the Vessel Inspector.
2. Select part A. Turn **Show unchanged values** on; verify unchanged rows are included.
3. Select part B from the left part list. Verify the toggle is still on and unchanged rows appear for part B.
4. Turn the toggle off and select part A again. Verify it stays off and only changed rows appear.
5. Click another part directly on the craft while the inspector is open; confirm the state persists.
6. Toggle while changing the selection rapidly; the last choice should carry forward without Unity GUILayout errors.
7. Test in flight with at least two scaled parts and repeat steps 2–5.
8. Open a separate scaled-values window from a part's PAW; confirm its toggle remains independent of the Inspector's state.
9. Verify changing parts still resets the report's scroll position and tooltip state.
