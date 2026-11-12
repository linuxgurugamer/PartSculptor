"""v127: diagnose and prevent the 22.3-degree Copy cutoff and slope loss."""
import math
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAIN = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
CORE = (ROOT/'PartSculptor/FaceMorphStepCore.cs').read_text()
RUNNER = (ROOT/'Tests/RegressionRunner/Program.cs').read_text()

class CopyInclination127(unittest.TestCase):
    def test_two_t400_angle_cutoff_was_from_body_rotation(self):
        # When the child turns roughly twice as far, the old Y-dot cutoff
        # rejects subsequent Copy frames just after the source reaches 22.8°.
        self.assertGreater(math.cos(math.radians(2 * 22.3)), .70)
        self.assertLess(math.cos(math.radians(2 * 23.0)), .70)
        self.assertIn('private readonly Dictionary<AttachNode, ConnectedSideMap>', MAIN)
        self.assertIn('GetOrRememberConnectedSideMap(', MAIN)
        self.assertIn('connectedSideMaps[node] = cached;', MAIN)
        self.assertIn('other.connectedSideMaps[neighborNode]', MAIN)
        self.assertIn('RecipientYSourceY = axes.YFromY', MAIN)
        self.assertIn('cached.OtherNode == neighborNode', MAIN)

    def test_physical_slope_39_6_degrees(self):
        source_slope = math.tan(math.radians(39.6)) / math.sqrt(2)
        # Old direct copy of Tilt onto a twice-wide face is much shallower.
        uncorrected = math.degrees(math.atan(math.hypot(source_slope / 2,
                                                       source_slope / 2)))
        self.assertAlmostEqual(uncorrected, 22.45, delta=.4)
        # New algorithm copies slope: multiply the receiver raw Tilt by width.
        corrected = math.degrees(math.atan(math.hypot(2 * source_slope / 2,
                                                      2 * source_slope / 2)))
        self.assertAlmostEqual(corrected, 39.6, places=8)
        self.assertIn('TryMirrorInclinationStep(', CORE)
        self.assertIn('sourceAfter, sourceScaleX, sourceScaleY', CORE)
        self.assertIn('receiverProposed.Width *', CORE)
        self.assertIn('FaceMorphStepCore.TryMirrorInclinationStep(', MAIN)
        self.assertIn('CopyAttachedInclination();', RUNNER)

    def test_angle_readouts_logged_before_and_after_rotation(self):
        self.assertIn('FaceTiltAngles childTiltBeforeRotation', MAIN)
        self.assertIn('childTiltAfterRotation = GetFaceTiltDegrees(receiverFace)', MAIN)
        self.assertIn('mirrored face inclination', MAIN)
        self.assertIn('bool followed = ApplyMirroredChildEditorRotation(before);', MAIN)

if __name__ == '__main__': unittest.main()
