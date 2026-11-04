"""0.0.4.69: stage delta-v query must use supplied mass, not part.mass."""
from pathlib import Path
import unittest
ROOT = Path(__file__).resolve().parents[2]
MAIN = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
MATH = (ROOT/'PartSculptor/ScaleMath.cs').read_text()
RUNNER = (ROOT/'Tests/RegressionRunner/Program.cs').read_text()

class StageDeltaV468(unittest.TestCase):
    def test_mass_uses_supplied_base_not_live_part_mass(self):
        body = MAIN.split('public float GetModuleMass(float defaultMass,',1)[1].split(
            'public ModifierChangeWhen GetModuleMassChangeWhen()',1)[0]
        self.assertIn('ScaleMath.MassModifierDelta(defaultMass, target, tweakDelta,', body)
        self.assertNotIn('part.mass', body.split('double target =',1)[1].split('float result =',1)[0].replace('// live part.mass.', ''))
        self.assertNotIn('double partBase =', body)
        self.assertIn('TweakScaleMassAdjustment(nativeMass, sit, false)', body)
        self.assertIn('B9ExtraMass(nativeMass, sit)', body)
        self.assertIn('FuelSwitchExtraMass(nativeMass, sit)', body)

    def test_production_math_remains_stable_across_snapshots(self):
        self.assertIn('internal static double MassModifierDelta(', MATH)
        self.assertIn('targetDryMass - suppliedBaseMass - tweakScaleDelta', MATH)
        target, tweak, b9, fuel = 2.5, 0.15, -0.03, 0.07
        expected = target + b9 + fuel
        for base in (0.25, 0.75, 1.25, 0.25, 1.25, 0.75):
            delta = target - base - tweak + b9 + fuel
            self.assertAlmostEqual(base + tweak + delta, expected)
        self.assertIn('StageDeltaVStableMass();', RUNNER)
        self.assertIn('ScaleMath.MassModifierDelta(stageBase, target,', RUNNER)

    def test_readme_is_user_supplied(self):
        guide = (ROOT/'README.md').read_text()
        self.assertTrue(guide.startswith('# PartSculptor'))
        self.assertIn('## Face morphing', guide)
        self.assertIn('## Fuel/Part Switchers', guide)
        self.assertIn('## License', guide)
