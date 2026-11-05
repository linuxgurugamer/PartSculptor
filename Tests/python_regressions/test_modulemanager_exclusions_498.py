"""0.0.4.99: part-module exclusions must apply individually and cumulatively."""
import json
import re
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
PATCH=ROOT/'GameData/PartSculptor/Patches/PartSculptor.cfg'
EXCLUDED=('ModulePartSculptor','ProceduralPart','WingProcedural',
          'SmartTankPart','DiameterMatcher','TextureDefaulter')

class ModuleManagerExclusions498(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.code=PATCH.read_text(encoding='utf-8')
        cls.directives=re.findall(r'(?m)^@PART\[\*\]:HAS\[([^\n]+)\]:NEEDS\[PartSculptor\]:FINAL$', cls.code)

    def test_exactly_one_universal_patch(self):
        self.assertEqual(len(self.directives),1)
        self.assertEqual(self.code.count('name = ModulePartSculptor'),1)

    def test_each_module_individually_excluded(self):
        for name in EXCLUDED:
            with self.subTest(name=name):
                self.assertEqual(self.directives[0].count('!MODULE['+name+']'),1)

    def test_all_filters_same_has_condition(self):
        self.assertEqual(self.directives[0], ','.join('!MODULE['+name+']' for name in EXCLUDED) + ',!MODEL:HAS[#model[ModularSegmentedSRBs/*]]')

    def test_only_parts_carrying_modules_excluded(self):
        self.assertNotIn('!ProceduralParts', self.code)
        self.assertNotIn('!ModuleManager', self.code)
        self.assertNotIn('!MODULE[ModuleFuelTanks]', self.code)
        self.assertTrue((PATCH.parent/'ScaleExponents.cfg').exists())

    def test_documentation_is_in_both_readmes(self):
        a=(ROOT/'README.md').read_bytes()
        b=(ROOT/'GameData/PartSculptor/README.md').read_bytes()
        self.assertEqual(a,b)
        for name in EXCLUDED:
            self.assertIn(name, a.decode('utf-8'))

    def test_release_metadata(self):
        for p in [ROOT/'PartSculptor.version',ROOT/'GameData/PartSculptor/PartSculptor.version']:
            self.assertEqual(json.loads(p.read_text())['VERSION']['BUILD'], 99)
        self.assertIn('AssemblyVersion("0.0.4.99")',(ROOT/'PartSculptor/AssemblyVersion.cs').read_text())

if __name__=='__main__':
    unittest.main()
