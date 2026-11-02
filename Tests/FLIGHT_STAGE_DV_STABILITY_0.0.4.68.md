# Stage delta-v stability in flight — 0.0.4.68

1. In the VAB create an otherwise stock rocket with one tank stretched along
   Y, a command pod, and an engine. Note the editor's stage delta-v.
2. Launch to the pad; keep throttle at zero, engine inactive, and wait at least
   30 seconds. The stage delta-v should settle instead of alternating between
   multiple fixed values roughly every 1.5 seconds.
3. Launch normally, then coast with engine off and stable atmospheric pressure.
   Check that stage delta-v remains stable aside from stock recalculation/noise.
4. Compare an identical unstretched tank craft. Test staging with one or more
   stretched tanks and symmetry copies.
5. Repeat with TweakScale and B9PartSwitch fuel-only tanks (subtype changes
   should still yield appropriate dry mass/cost); verify resources do not
   refill, drain, or oscillate when neither engine nor subtype changes.
6. If the issue remains, compare full `KSP.log` and the save/craft file and
   report displayed delta-v values, installed tank/engine modules, and steps
   to reproduce. The in-game simulator has not been run during this change.
