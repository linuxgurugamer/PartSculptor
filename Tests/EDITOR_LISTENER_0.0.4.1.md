# 0.0.4.1 editor listener regression tests

1. Launch KSP, open a fresh VAB or SPH craft, place a StretchScale-enabled part.
   Confirm no NullReferenceException when the first part is created.
2. Add more StretchScale parts, duplicate with symmetry, and change a scale preset.
   Confirm changes are applied, capacities update, and ship-modified callbacks
   are still batched (not one notification for every descendant).
3. Delete the part that was added first while leaving other StretchScale parts.
   Confirm resource switching and updates still work on remaining parts.
4. Remove all StretchScale parts and add one again. Confirm no duplicate listener
   or NullReferenceException.
5. Switch VAB -> Space Center -> SPH, load a saved craft, and exercise presets.
   Confirm the listener is registered again exactly once.
6. Try a craft with a tank switcher which modifies resource capacities silently;
   the configured fallback scan should still reconcile its capacity.
7. Inspect KSP.log for the absence of exceptions in
   `RegisterEditorListener`, `EnsureEditorListener`, and `OnDestroy`.

These are manual in-game checks; this patch was not compiled or run inside KSP.
