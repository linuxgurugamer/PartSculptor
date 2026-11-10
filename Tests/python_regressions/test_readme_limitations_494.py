"""Regression checks for the 0.0.4.99 limitations append."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LIMITATIONS = ("### Limitations\n"
    "- B9PartSwitch resource and `TEXTURE` changes are supported. `transform`/`baseTransform` inside `TEXTURE` select renderers; `transform = ...` directly inside `SUBTYPE` toggles pre-existing static model variants and is also supported. All existing variant meshes, including inactive ones, are morphed. Nested `TRANSFORM` blocks that move/rotate/scale geometry, node switches, `MATERIAL` blocks, and unknown B9 operations remain unsupported. For variant models with different geometry, verify the full set of meshes, collision shapes, attachment placement, and flight behavior in-game.\n"
    "- Procedural Parts (`ProceduralPart`) are not supported.  The ModuleManager patch that adds `ModulePartSculptor` skips any part that already has `ModulePartSculptor` or contains `ProceduralPart`.\n")

class ReadmeLimitations494(unittest.TestCase):
    def test_limitations_are_final_section(self):
        readme = (ROOT / 'README.md').read_text(encoding='utf-8')
        self.assertIn('\n\n'+LIMITATIONS, readme)
        self.assertTrue(readme.rstrip().endswith('verify the loaded ModuleManager cache and add an explicit part-name exclusion if necessary.'))
        self.assertEqual(readme.count('### Limitations'), 1)

    def test_two_readme_copies_identical(self):
        self.assertEqual((ROOT / 'README.md').read_bytes(),
                         (ROOT / 'GameData/PartSculptor/README.md').read_bytes())

    def test_version_metadata(self):
        for name in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            version = __import__('json').loads((ROOT/name).read_text())
            self.assertEqual(version['VERSION']['BUILD'], 106)
        assembly = (ROOT/'PartSculptor/AssemblyVersion.cs').read_text()
        self.assertIn('AssemblyVersion("0.0.4.106")', assembly)

if __name__ == '__main__':
    unittest.main()
