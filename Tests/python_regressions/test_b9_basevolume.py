"""B9 baseVolume noncompounding integration smoke checks."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
SCALER = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()
B9 = (ROOT / 'PartSculptor/B9PartSwitchSupport.cs').read_text()
MATH = (ROOT / 'PartSculptor/ScaleMath.cs').read_text()
RUNNER = (ROOT / 'Tests/RegressionRunner/Program.cs').read_text()

class B9BaseVolumeIntegration(unittest.TestCase):
    def test_native_volume_saved_and_restored(self):
        self.assertIn('PARTSCULPTOR_B9_BASE_VOLUME', SCALER)
        self.assertIn('b9BaseVolumes.Clear();', SCALER)
        self.assertIn('nativeBaseVolume', SCALER)
        self.assertIn('b9BaseVolumes,', SCALER)

    def test_reflection_not_hard_dependency(self):
        self.assertIn('"baseVolume"', B9)
        self.assertIn('"UpdateVolume"', B9)
        self.assertIn('VolumeKey(i, Value(module, "moduleID"))', B9)
        self.assertIn('prefabSwitchers', B9)
        self.assertIn('SelectedSubtype(module)', B9)
        self.assertIn('CurrentSubtypeName', B9)
        self.assertNotIn('using B9PartSwitch;', B9)

    def test_b9_owns_its_resources(self):
        self.assertIn('b9Support.OwnsScaledResource(resource.resourceName)', SCALER)
        self.assertIn('b9Support.ApplyBaseVolumes(part, StretchVolumeFactor,', SCALER)
        self.assertIn('if (!editor) return;', B9)
        self.assertIn('fillFractions', B9)

    def test_existing_b9_modifier_not_multiplied_twice(self):
        self.assertIn('b9Support.ExtraMass(', SCALER)
        self.assertIn('b9Support.ExtraCost(', SCALER)
        self.assertIn('ScaleMath.B9ModifierExtra(', B9)
        self.assertIn('B9ModifierExtra(double current', MATH)

    def test_math_is_called_from_production_and_runner(self):
        self.assertIn('ScaleMath.B9BaseVolume(', B9)
        self.assertIn('ScaleMath.B9BaseVolume(', RUNNER)
        self.assertIn('B9VolumeScaling();', RUNNER)
        self.assertIn('ScaleMath.PositiveFinite(', B9)

if __name__ == '__main__': unittest.main()
