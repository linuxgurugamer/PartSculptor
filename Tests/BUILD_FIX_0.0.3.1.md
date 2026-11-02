# StretchScale 0.0.3.1 — cost API fix

- Fix compilation: `Part` has no `cost` field. The previous call to `part.partInfo.partPrefab.cost` was invalid.
- `GetModuleCost()` now uses KSP's provided `defaultCost` value, so fuel price is not charged twice by the modifier.
- The PAW Dry cost display derives catalog dry cost from `part.partInfo.cost` minus the original resource amounts times their per-unit cost.
- All other resource, mass, presets, and geometry behavior is preserved.

**Validation:** Source-level checks completed. No KSP assemblies were available here for an actual game build.
