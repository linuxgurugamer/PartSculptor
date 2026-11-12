"""Regression for the second (Copy-only) face tilt limit, fixed in .129."""
import json
import math
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

class CopyFaceTiltLimit129(unittest.TestCase):
    def read(self, rel):
        return (ROOT / rel).read_text()

    def test_no_hidden_point_75_in_copy_validity(self):
        core = self.read('PartSculptor/FaceMorphStepCore.cs')
        self.assertIn('Valid(MorphStep s, double maxFaceTilt)', core)
        self.assertIn('Math.Abs(s.TiltX) <= maxFaceTilt', core)
        self.assertIn('Math.Abs(s.TiltZ) <= maxFaceTilt', core)
        self.assertNotIn('Math.Abs(s.TiltX) <= .75d', core)
        self.assertNotIn('Math.Abs(s.TiltZ) <= .75d', core)
        self.assertIn('!Finite(maxFaceTilt) || maxFaceTilt <= 0d', core)

    def test_runtime_passes_configured_limit_on_copy_path(self):
        main = self.read('PartSculptor/PartSculptor.cs')
        self.assertIn('if (!MorphStep.Valid(proposed, Settings.MaxFaceTilt))',main)
        self.assertIn('FaceMorphStepCore.TryMirrorInclinationStep(',main)
        self.assertIn('bool followed = ApplyMirroredChildEditorRotation(before);',main)
        self.assertIn('Mathf.Clamp(f.TiltX, -Settings.MaxFaceTilt, Settings.MaxFaceTilt)',
                      self.read('PartSculptor/FaceMorphMath.cs'))

    def test_steep_two_t400_copy_math_and_bounds(self):
        # Both T400s start identical and unscaled; an X-only +1.5 tilt
        # gives a magnitude above the historical .75 slope limit.
        target = 1.5
        angle = math.degrees(math.atan(target))
        self.assertGreater(angle, 36.9)
        self.assertAlmostEqual(math.degrees(math.atan(abs(-target))), angle)
        self.assertLessEqual(abs(-target), 4.0)
        self.assertGreater(abs(-target), .75)
        runner = self.read('Tests/RegressionRunner/Program.cs')
        self.assertIn('ConfigurableCopyTiltLimit();', runner)
        self.assertIn('MorphStep.Valid(steep, 4d)', runner)
        self.assertIn('MorphStep.Valid(steep, .75d)', runner)

    def test_version_and_docs(self):
        for path in ['PartSculptor.version','GameData/PartSculptor/PartSculptor.version']:
            self.assertEqual(json.loads(self.read(path))['VERSION']['BUILD'], 130)
        self.assertIn('AssemblyVersion("0.0.4.130")', self.read('PartSculptor/AssemblyVersion.cs'))
        self.assertEqual(self.read('README.md'),self.read('GameData/PartSculptor/README.md'))
        self.assertIn('Copy attached',self.read('README.md'))

if __name__=='__main__':unittest.main()
