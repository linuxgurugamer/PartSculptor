# PartSculptor 0.0.4.129 — Copy face tilt limit in VAB/SPH

1. Set `maxFaceTilt = 4` in `GameData/PartSculptor/PluginData/Settings.cfg` and restart KSP.
2. Stack two stock FL-T400 tanks, lower tank as parent, upper tank as child. Enable **Copy attached morph steps** (leave **Match attached faces** off).
3. Morph the upper end of the lower tank by dragging one edge until its displayed tilt exceeds 36.9°. Verify the lower end of the child tank continues increasing past the same historical 0.75 slope limit rather than freezing, for both tilt directions.
4. Check the parent's face angle, child's face angle, body movement, and node/joint attachment while passing 40°, 50°, and higher as permitted by geometry and the limit.
5. Test a combination of X and Z tilts, then reverse the drag and use Undo; check the child returns to its original state. Confirm unrelated geometry and attached parts are preserved.
6. Set `maxFaceTilt = 0.75` and restart. Verify **Copy** again rejects steep child morphs while both faces use the same configured slope bound.

Note: The slope limit applies independently to TiltX and TiltZ; the combined displayed angle is not constrained to one fixed number. Extreme angles may violate other geometry/volume/attachment safety constraints. In-game testing is necessary.
