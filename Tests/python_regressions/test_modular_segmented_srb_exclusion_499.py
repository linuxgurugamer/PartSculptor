"""Regressions for the ModularSegmentedSRBs model path exclusion."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PATCH = ROOT/'GameData/PartSculptor/Patches/PartSculptor.cfg'
MODEL_EXCLUSION = '!MODEL:HAS[#model[ModularSegmentedSRBs/*]]'

class ModularSegmentedSRBExclusion499(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.patch = PATCH.read_text(encoding='utf-8')
        cls.rule = next(line for line in cls.patch.splitlines() if line.startswith('@PART['))

    def test_model_path_exclusion_in_has(self):
        self.assertIn(MODEL_EXCLUSION, self.rule)
        self.assertIn('HAS[!MODULE[ModulePartSculptor]', self.rule)

    def test_existing_module_exclusions_retained(self):
        for mod in ('ModulePartSculptor','ProceduralPart','WingProcedural',
                    'SmartTankPart','DiameterMatcher','TextureDefaulter'):
            with self.subTest(mod=mod):
                self.assertIn('!MODULE['+mod+']', self.rule)

    def test_mod_presence_does_not_disable_all_parts(self):
        self.assertNotIn('!ModularSegmentedSRBs', self.rule)
        self.assertEqual(self.rule.count(':NEEDS[PartSculptor]'),1)

    def test_only_one_injection_patch(self):
        self.assertEqual(len(re.findall(r'(?m)^@PART\[',self.patch)),1)
        self.assertEqual(len(re.findall(r'(?m)^\s*name = ModulePartSculptor$',self.patch)),1)

    def test_documentation_mirrored_and_versioned(self):
        readme=(ROOT/'README.md').read_bytes()
        self.assertEqual(readme,(ROOT/'GameData/PartSculptor/README.md').read_bytes())
        self.assertIn(b'ModularSegmentedSRBs/',readme)
        for version in (ROOT/'PartSculptor.version',ROOT/'GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads(version.read_text())['VERSION']['BUILD'],99)

if __name__=='__main__': unittest.main()
