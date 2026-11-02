# StretchScale 0.0.4.11: Scaled Values window test plan

## Build/static

- Build Debug and Release using KSP 1.12.x managed references.
- Confirm the PAW event `Show scaled values` appears in the Stretch Scale group in both builds.
- With `debugLogChanges = false`, verify the live inspector still displays data.
- Verify the project includes ScaledValuesWindow.cs and the two .version files and assembly agree on 0.0.4.11.

## Runtime behavior

1. In VAB/SPH place a stock liquid-fuel tank. Open the PAW and click **Show scaled values**.
2. At scale 1, only unchanged or no rows should appear with the default filter. Tick **Show unchanged values** to inspect all bound properties.
3. With proportions locked, set 1.25 and then 1.5. The dry-mass target should show factors 1.953125 and 3.375 of the native value for mass exponent 3; repeated clicks at 1.5 must not grow the value further.
4. The report should show original/current axes, model transform, attach-node values, and resource capacities. The current values must update without closing the inspector.
5. Add a part with engine, reaction wheel, RCS, or generator and matching TWEAKSCALEEXPONENTS definitions. The changed module fields should appear by module path with original/current values and exponent.
6. Inspect a module with a scaled FloatCurve. The curve data must remain readable via tooltip or Copy report.
7. Test fuel tank half-full and empty. "Amount" uses equivalent original at the current fill, not the initially filled resource amount.
8. Click Copy report, paste into text editor, and verify all displayed rows are included as tab-separated data.
9. Drag and scroll the window and verify clicking behind it does not pick up parts. Close it, and verify editor interactions work normally.
10. Open one inspector on each of two parts, close one and verify the other retains its input lock. Delete a part with an open inspector; verify no exceptions or stuck locks.
11. Leave the editor with the window open; verify no lingering input lock in flight or the next editor session.
12. Test `Show unchanged values` at scale 1 and `Show scaled values` button toggling the window open and shut.

## Limitations

- This is a live per-part snapshot, not a record of every historical click.
- Attached child world positions are intentionally not shown with an invented original value.
- Full runtime testing requires KSP assemblies and the game environment.
