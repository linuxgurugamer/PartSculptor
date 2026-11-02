> **Historical test plan (superseded as of v0.0.4.69).** PatchManager support was removed; StretchScale is now applied to all parts by the single ModuleManager patch. Do not follow PatchManager installation steps below.

# StretchScale 0.0.4.33 — PatchManager crewed-part support

## Automated checks

Run `python Tests/run_regressions.py` from the repository root. The Python tests
verify the metadata/patch filename pairing, default-disabled state, location
inside PluginData, positive-crew MM filter, duplicate-module guard, and absence
of an always-active all-parts rule.

## KSP 1.12.x manual checks

1. Start KSP with ModuleManager and StretchScale, but without PatchManager.
   Confirm uncrewed parts have StretchScale and crewed pods do not (unless another
   patch provided it).
2. Install KSP1 PatchManager; find `Enable Crewed Parts` under StretchScale.
   Confirm the entry starts inactive and inspect at least two crewed parts.
3. Enable the entry, click **Apply All**, and restart KSP. Verify a Mk1
   Command Pod and a Mk1 Cockpit have exactly one ModuleStretchScale and an
   uncrewed tank still has exactly one.
4. Check a third-party crewed part with CrewCapacity > 0, plus a part that
   already defines ModuleStretchScale. Ensure no duplicates.
5. Disable the entry in PatchManager, click Apply All, restart KSP and verify
   crewed parts no longer receive StretchScale from this optional patch.
6. Verify the optional `CrewedParts.cfg` is not applied while it remains under
   `PatchManager/PluginData` and is moved/activated only by PatchManager.
7. Back up any craft used to test crewed scaling. Test hatches/EVA/IVA when
   enabling the feature: matching model/IVA motion is not guaranteed by the patch.

> Offline checks do not replace in-game ModuleManager / PatchManager tests.
