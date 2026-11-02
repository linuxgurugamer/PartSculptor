"""0.0.4.71: fallback exponent file restored without PatchManager."""
from pathlib import Path
from zipfile import ZipFile
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
PATCHES = ROOT / 'GameData/PartSculptor/Patches'

class RestoredExponents470(unittest.TestCase):
    def test_restored_original_file_exactly(self):
        cfg = (PATCHES / 'ScaleExponents.cfg').read_bytes()
        self.assertGreater(len(cfg), 6000)
        self.assertIn(b'CC-BY-NC-SA', cfg)
        self.assertIn(b'JonnyOThan', cfg)
        self.assertIn(b'TWEAKSCALEEXPONENTS:NEEDS[!TweakScale]', cfg)
        self.assertIn(b'TWEAKSCALEBEHAVIOR:NEEDS[!TweakScale]', cfg)

    def test_every_active_rule_gated_when_tweakscale_not_installed(self):
        cfg = (PATCHES / 'ScaleExponents.cfg').read_text(encoding='utf8')
        directives = [line.strip().split('//')[0].strip() for line in cfg.splitlines()
                      if re.match(r'^TWEAKSCALE(EXPONENTS|BEHAVIOR):', line)]
        self.assertGreaterEqual(len(directives), 35)
        self.assertTrue(all(':NEEDS[!TweakScale]' in line for line in directives))

    def test_universal_module_patch_unchanged_and_no_patchmanager(self):
        cfg = (PATCHES / 'PartSculptor.cfg').read_text(encoding='utf8')
        self.assertIn('@PART[*]:HAS[!MODULE[ModulePartSculptor],!MODULE[ModuleStretchScale]]:NEEDS[PartSculptor]:FINAL',cfg)
        self.assertNotIn('CrewCapacity',cfg)
        self.assertFalse((ROOT/'GameData/PartSculptor/PatchManager').exists())

    def test_documentation(self):
        readme = (ROOT / 'README.md').read_text(encoding='utf8')
        self.assertIn('includes `Patches/ScaleExponents.cfg`', readme)
        self.assertNotIn('remove any leftover `GameData/PartSculptor/Patches/ScaleExponents.cfg`', readme)

if __name__ == '__main__':
    unittest.main()
