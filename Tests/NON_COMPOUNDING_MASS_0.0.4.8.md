# Locked-proportions mass/capacity regression — StretchScale 0.0.4.8

## Reproduction

1. Use an unmodified stock fuel tank without TweakScale or a tank switcher
   (except ModuleStretchScale); note its 1x **dry** mass, resource capacity,
   resource amount, and vessel wet mass separately.
2. Enable **Lock proportions**. From 1x, increase one axis to 1.25 and let
   the other two follow. Then increase it to 1.50, then 1.75, then 2.00.
3. Compare normalized dry mass and capacity against the expected values:

   | X=Y=Z | X*Y*Z | native dry mass x | native capacity x |
   |------:|------:|------------------:|------------------:|
   | 1.00 | 1.000000 | 1.000000 | 1.000000 |
   | 1.25 | 1.953125 | 1.953125 | 1.953125 |
   | 1.50 | 3.375000 | 3.375000 | 3.375000 |
   | 1.75 | 5.359375 | 5.359375 | 5.359375 |
   | 2.00 | 8.000000 | 8.000000 | 8.000000 |

4. Return to 1.25, 1.50, and 1.00; repeat each step several times. Dry mass,
   fuel capacity, and amount at a fixed fill percentage must be identical each
   time (allowing for floating-point/GUI rounding).
5. Wait 30+ frames between steps; re-open the PAW, save/reopen the craft,
   duplicate the tank, and perform the same sequence again. Neither cached
   mass nor native capacity may drift between updates or craft loads.
6. Repeat with a stock tank using a mass-changing variant: variant mass and
   other mass-modifier deltas remain **additive**, not multiplied again by
   StretchScale. Repeat with an unrelated mass-modifying module if available.
7. Repeat with TweakScale at a fixed non-1x size; the TweakScale-adjusted base
   is stretched once, and TweakScale-only changes continue to work.
8. Repeat with a tank-switching module and check that selecting a new tank
   configuration still updates native capacity but merely resizing does not
   increase the baseline. If it fails, record the specific switcher/version.
9. Build DEBUG and check KSP.log for `[StretchScale] ... native mass ... live
   Part.mass ... stretch target ... tank capacity multiplier ...` immediately
   after each axis edit. The stretch target must depend on the *current axes*,
   never on the previous click or previous vessel mass.

## Source changes

- `GetModuleMass` now returns the delta between an absolute prefab-anchored
  target and the mass KSP already has, instead of scaling all module deltas.
- `GetModuleCost` uses the same non-compounding additive-delta approach.
- The dry-mass/dry-cost PAW readouts follow those calculations.
- Existing stock resource types keep prefab/saved native capacities even if
  PartResources are recreated or other code rewrites the expanded capacity.
- `ExponentBinding.Native` remains fixed as in 0.0.4.7.

**Environment caveat:** Source tests and archive integrity checks are static;
run the steps above in KSP 1.12 for actual runtime verification.
