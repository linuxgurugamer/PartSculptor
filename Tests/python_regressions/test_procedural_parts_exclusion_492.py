"""Make sure PartSculptor is never automatically injected into Procedural Parts."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
PATCH = ROOT / 'GameData/PartSculptor/Patches/PartSculptor.cfg'

class ProceduralPartsExclusion492(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source = PATCH.read_text(encoding='utf-8')
        cls.active = '\n'.join(l.split('//', 1)[0] for l in cls.source.splitlines())

    def test_procedural_part_module_excluded(self):
        self.assertIn('!MODULE[ProceduralPart]', self.active)

    def test_universal_patch_only_if_not_existing(self):
        self.assertIn('!MODULE[ModulePartSculptor]', self.active)
        self.assertEqual(self.active.count('name = ModulePartSculptor'), 1)

    def test_same_patch_remains_active_and_final(self):
        self.assertRegex(self.active, r'(?m)^@PART\[\*\]:HAS\[!MODULE\[ModulePartSculptor\],!MODULE\[ProceduralPart\]\]:NEEDS\[PartSculptor\]:FINAL$')
        self.assertEqual(len(re.findall(r'(?m)^@PART\[', self.active)), 1)

    def test_no_broad_exclusion_when_procedural_parts_installed(self):
        # The skip applies only to parts carrying ProceduralPart, not ALL
        # eligible parts just because Procedural Parts is installed.
        self.assertNotIn('!ProceduralParts', self.active)
        self.assertNotIn('!MODULE[ModuleFuelTanks]', self.active)

    def test_fallback_exponent_cfg_retained(self):
        self.assertTrue((PATCH.parent/'ScaleExponents.cfg').is_file())

if __name__ == '__main__':
    unittest.main()
