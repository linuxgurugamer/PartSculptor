# StretchScale 0.0.4.42 validation checklist

1. In VAB, open Vessel Inspector > Settings. Verify Match node sizes,
   Stretch children, and Lock proportions all default OFF when no user file exists.
2. Turn them ON, Save, quit and restart; verify choices persist. Turn them OFF
   and verify the session node/children controls update.
3. With Lock proportions default ON, add a new part; verify it is locked.
   Save a craft after manually unlocking a part; reload the craft and verify
   the saved per-part choice is retained even with the global default ON.
4. Open a StretchScale PAW. Verify the old preset slider has been replaced by
   a Presets button. Click it to open the dedicated presets window.
5. Verify its quick slider lists only absolute presets. Define a custom
   relative preset in the Vessel Inspector's Edit presets window; verify the
   full Presets list includes it while the quick slider does NOT.
6. Apply an absolute preset twice; scale must remain constant. Apply a
   relative preset twice; it should multiply the CURRENT scale twice by design.
7. Verify Repeat preset uses the last applied preset, and Reset scale returns
   1x along with the last-used preset label.
8. Close a window, change parts, leave/reenter editor; confirm windows close
   cleanly and ClickThroughBlocker works. Check no IMGUI Layout/Repaint errors.

Automated source checks: python Tests/run_regressions.py
C# mathematical runner (requires .NET 8): python Tests/run_regressions.py --require-dotnet
