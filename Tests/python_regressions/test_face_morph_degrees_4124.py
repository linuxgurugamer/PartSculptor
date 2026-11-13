"""0.0.4.124: non-mutating displayed face tilt angles."""
import json
import math
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CORE = (ROOT/'PartSculptor/FaceMorphCore.cs').read_text()
PART = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
GIZMO = (ROOT/'PartSculptor/FaceMorphGizmo.cs').read_text()
RUNNER = (ROOT/'Tests/RegressionRunner/Program.cs').read_text()

class FaceTiltDegrees4124(unittest.TestCase):
    def test_formula_accounts_for_resize_and_stretch(self):
        for part in ('width * scaleX', 'depth * scaleZ',
                     'tiltX * scaleY / xSpan', 'tiltZ * scaleY / zSpan',
                     'Math.Atan(slopeX)', 'Math.Atan(slopeZ)',
                     'Math.Sqrt(slopeX * slopeX + slopeZ * slopeZ)'):
            self.assertIn(part, CORE)
        self.assertIn('FaceTiltDegreeReadout();', RUNNER)
        self.assertIn('widening a tilted face decreases its angle', RUNNER)

    def test_angle_math_independent_example(self):
        x = math.degrees(math.atan(.3/1.5))
        z = math.degrees(math.atan(-.4/2.0))
        total = math.degrees(math.atan(math.hypot(.3/1.5, -.4/2.0)))
        self.assertAlmostEqual(x, 11.309932474, places=7)
        self.assertAlmostEqual(z, -11.309932474, places=7)
        self.assertAlmostEqual(total, 15.793169048, places=7)
        # Two opposite mirrored faces share magnitude and reverse signed X.
        self.assertAlmostEqual(math.degrees(math.atan(-.3/1.5)), -x)

    def test_live_editor_paw_and_tooltip(self):
        for text in ('guiName = "Bottom face tilt"',
                     'guiName = "Top face tilt"',
                     'displayBottomFaceTilt = GetFaceTiltDisplay(0);',
                     'displayTopFaceTilt = GetFaceTiltDisplay(1);',
                     'displayTopFaceTilt)',
                     'GetFaceTiltDegrees(int face)'):
            self.assertIn(text, PART)
        for text in ('module.GetFaceTiltDisplay(0)',
                     'module.GetFaceTiltDisplay(1)',
                     'module.GetFaceTiltDisplay(editedFace)',
                     'DrawMorphTip(tipRect,'):
            self.assertIn(text, GIZMO)
        self.assertIn('editedFace = dragging < 4 || (dragging >= 8 && dragging < 12)', GIZMO)

    def test_no_change_to_morph_parameters_or_joint_handling(self):
        self.assertIn('return FaceMorphCore.GetTiltDegrees', PART)
        self.assertNotIn('SetMorphFaces(', CORE)
        self.assertNotIn('FollowMirroredJointAfterCopy', PART)
        self.assertNotIn('SnapMirroredJointWithPartFaceSnap', PART)

    def test_version_and_documentation(self):
        for filename in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            data = json.loads((ROOT/filename).read_text())
            self.assertEqual(data['VERSION']['BUILD'], 137)
        self.assertIn('AssemblyVersion("0.0.4.137")', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertIn('## 0.0.4.124 — Display face', (ROOT/'Changelog.md').read_text())
        self.assertIn('**Tilt readout:**', (ROOT/'README.md').read_text())
        self.assertEqual((ROOT/'README.md').read_bytes(),
            (ROOT/'GameData/PartSculptor/README.md').read_bytes())

if __name__ == '__main__':
    unittest.main()
