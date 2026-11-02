# Gizmo cursor tracking (0.0.4.38)

1. In the VAB, place an FL-T800 tank and open **Show scaling gizmo**.
2. Verify all three handles are farther from the part (stem factor 3).
3. Click near the edge (not center) of an X, Y or Z handle. It should not
   jump to the mouse center when the drag begins.
4. Slowly drag outward, to 1.5x and 2x, then back. Observe that the rendered
   handle follows the mouse instead of lagging behind due to the long stem.
5. Try with locked proportions on and off; no cumulative mass growth.
6. Hold Shift; the handle should snap in 0.05 increments and may deviate
   slightly from the cursor between snap steps.
7. Drag beyond configured maximum and minimum sizes: the handle stays at the
   limit; it must not pretend the scaling succeeded.
8. Press Escape or right-click while dragging; previous axes are restored.
9. Release mouse: normal editor picking must work again, and no lock should
   remain after closing the gizmo or leaving the scene.
10. Switch camera angle and repeat; the drag direction and sensitivity should
    reflect the new screen projection.

Offline: `python Tests/run_regressions.py`; executable C# checks require .NET 8.
