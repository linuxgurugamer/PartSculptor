> **Historical test plan (superseded as of v0.0.4.69).** PatchManager support was removed; PartSculptor is now applied to all parts by the single ModuleManager patch. Do not follow PatchManager installation steps below.

# Crewed-parts test plan for 0.0.4.48

- Enable the optional crewed PatchManager patch; start a fresh Career/Sandbox craft.
- Mk1 command pod (one stock IVA seat): scale 1 -> 1.5 -> 2 -> 1. No phantom crew slots; inspector should say requested 2/4 vs actual 1.
- Custom pod with more authored `InternalSeat` modules than its original CrewCapacity: expand, check editor crew tab count and IVA seated positions. Save, clear editor, reload, launch, return to editor. Verify no drift.
- Pod without discoverable IVA: keep native capacity and show fallback reason.
- With crew aboard, attempt to shrink / increase. Existing occupant must remain assigned, and capacity must not be modified.
- Empty the pod, repeat scale edit. Capacity should update using original prefab capacity, not the previous output.
- Repeat 1->2->1->2 and save/reload; capacity should never grow cumulatively.
- Check single editor ship-modified notification and no stock PAW exceptions.
- Enter flight and reload the vessel: no live CrewCapacity changes, no EVA/IVA corruption.
- Run `python Tests/run_regressions.py --require-dotnet` for executable pure calculation cases.
