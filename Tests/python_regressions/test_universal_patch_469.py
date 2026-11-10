"""All-parts ModuleManager injection remains active alongside fallback exponent rules."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
GAMEDATA = ROOT / 'GameData/PartSculptor'
PATCHES = GAMEDATA / 'Patches'

class UniversalPatch469(unittest.TestCase):
    def test_all_parts_install_patch_and_exponents_only(self):
        self.assertFalse((GAMEDATA / 'PatchManager').exists())
        self.assertEqual(sorted(p.name for p in PATCHES.glob('*.cfg')),
                         ['PartSculptor.cfg', 'ScaleExponents.cfg'])
        self.assertTrue((PATCHES / 'ScaleExponents.cfg').is_file())

    def test_all_parts_including_crewed_no_duplicate(self):
        code = (PATCHES / 'PartSculptor.cfg').read_text(encoding='utf-8')
        active = '\n'.join(line.split('//', 1)[0] for line in code.splitlines())
        self.assertRegex(active, r'@PART\[\*\]:HAS\[!MODULE\[ModulePartSculptor\],!MODULE\[ProceduralPart\],!MODULE\[WingProcedural\],!MODULE\[SmartTankPart\],!MODULE\[DiameterMatcher\],!MODULE\[TextureDefaulter\],!MODEL:HAS\[#model\[ModularSegmentedSRBs/\*\]\]\]:NEEDS\[PartSculptor\]:FINAL')
        self.assertEqual(len(re.findall(r'(?m)^\s*name\s*=\s*ModulePartSculptor\s*$', active)), 1)
        self.assertNotIn('CrewCapacity', active)
        self.assertEqual(len(re.findall(r'(?m)^@PART\[', active)), 1)

    def test_no_obsolete_patch_references_in_active_ui(self):
        solution = (ROOT / 'PartSculptor.slnx').read_text(encoding='utf-8')
        self.assertIn('Patches/ScaleExponents.cfg', solution)
        readme = (ROOT / 'README.md').read_text(encoding='utf-8')
        self.assertIn('eligible parts', readme)
        self.assertIn('ProceduralPart', readme)
        self.assertIn('including command pods and cockpits', readme)
        self.assertNotIn('Install PatchManager.', readme)
        self.assertNotIn('enable crewed parts', readme.lower())

    def test_project_source_and_versions_untouched_except_metadata(self):
        from json import loads
        for file in (ROOT / 'PartSculptor.version', GAMEDATA / 'PartSculptor.version'):
            self.assertEqual(loads(file.read_text())['VERSION']['BUILD'], 106)
        self.assertIn('AssemblyVersion("0.0.4.106")',
                      (ROOT / 'PartSculptor/AssemblyVersion.cs').read_text())

if __name__ == '__main__':
    unittest.main()
