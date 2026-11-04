# Automated + in-game node matching regression plan (0.0.4.29)

Automated checks: `python Tests/run_regressions.py --require-dotnet`

- Production math tests: repeated scale 1.25/1.5/2/1, stock fuel and B9
  alternate capacity baselines, exponent signs, all node tiers, face-only
  stretch, actual non-rounded 1.5x stack diameters, diagonal nodes, invalid
  node sizes and safe no-op values.
- Offline integration checks: PAW toggle and Settings.cfg, editor-only,
  parent-change detection, same-node matching, actual ScaleMath call sites,
  project/version integrity.

Manual (requires KSP 1.12.x; **not** automatically tested):

1. VAB/SPH: attach a fresh 1.25m PartSculptor tank to a 2.5m node. X/Z should
   become 2x (for Y-axis stack nodes), with Y length unchanged; no parent change.
2. Set parent diameter to 1.5x. Reattach fresh tank: its X/Z should become
   1.5x, not 2x due to quantized KSP node size.
3. Toggle **Match node sizes** off, attach another part: no auto-resize.
4. Toggle back on and attach to a matching node: no scale change or log spam.
5. Detach, reattach to another size: matching happens once per new connection;
   idling or an `onEditorShipModified` event must never cause new scale changes.
6. Save/reload a craft containing scaled parts: no auto-matching on load.
7. Surface attach a part: no automatic size matching.
8. Symmetry, stretch children and parent resizing must not oscillate, duplicate
   resource capacity, or recursively attach parts.
9. Career VAB/SPH level 1: requested matching must obey tier limits and show
   Scale safety warning when clamped, not exceed maximum allowed volume.
10. Change a B9 subtype, reload, and attach: node matching should use the
    subtype's correct baseline and must not compound B9 mass/resource changes.
11. In flight or map view, neither auto-matching nor any PAW edit is performed.
12. Confirm other editor input, inspector, Waterfall/stock FX and mouse hover
    still work normally; test Undo and redo after auto-match.
