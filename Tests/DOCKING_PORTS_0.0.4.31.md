# Specialized docking-port checks — 0.0.4.31

Run `python Tests/run_regressions.py` for the offline structural suite and, with
.NET 8, compiled production math suite. For KSP in-game validation:

1. In VAB attach a stock standard (`size1`) port at 1×. Confirm original
   acquisition/capture distances, no geometry change and `size1` compatibility.
2. Scale uniformly to 2×. The face should be 2.5 m, compatibility `size2`;
   verify docking to an unscaled Senior in flight.
3. Scale back to 1×. Confirm `size1` and **exactly** original capture distances.
   Repeat 1×→1.25×→1.5×→2×→1× at least ten times; no compounding.
4. Make a standard port 1.5× (1.875 m). It should have type
   `StretchScaleDock_1875mm`, and match another 1.875 m StretchScale port,
   not a stock Senior. Validate 0.625→1.25→2.5 conversions too.
5. Stretch X and Z of a Y-facing port while keeping Y=1; then change Y alone.
   Confirm that Y-only stretch does not change capture distance or type.
6. Verify `nodeTypes` singleton stays synchronized with `nodeType` for stock
   ports, including a craft save/reload and editor undo/redo.
7. Dock in flight, switch focus/load and undock. Check that no state reset or
   unexpected relabeling occurs while docked, undocking or acquiring.
8. Test protected custom nodeType, multi-type, gendered, shielded/deployable,
   and B9-modified ports. None of these may be forcibly relabeled. B9 parts also retain their
   own docking distance fields.
9. Disable each settings flag separately. No docking type changes when disabled;
   no distance modifications when distance scaling is disabled.
10. Test port geometry with simultaneous TweakScale (if installed), orientation
    variations and symmetry parts. Capture distance follows actual face size.

This does not replace real on-orbit docking tests. Ensure `KSP.log` has no
`ModuleDockingNode` errors, and inspect port state after docking and undocking.
