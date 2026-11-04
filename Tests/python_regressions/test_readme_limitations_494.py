"""Regression checks for the 0.0.4.97 limitations append."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LIMITATIONS = ("### Limitations\n"
    "- B9PartSwitch subtypes that change the part's mesh, nodes, or materials are not supported. Only subtypes that change resources (such as fuel type or added mass) are compatible.\n"
    "- Procedural Parts (`ProceduralPart`) are not supported.  The ModuleManager patch that adds `ModulePartSculptor` skips any part that already has `ModulePartSculptor` or contains `ProceduralPart`.\n")

class ReadmeLimitations494(unittest.TestCase):
    def test_limitations_are_final_section(self):
        readme = (ROOT / 'README.md').read_text(encoding='utf-8')
        self.assertTrue(readme.endswith('\n\n'+LIMITATIONS))
        self.assertEqual(readme.count('### Limitations'), 1)

    def test_two_readme_copies_identical(self):
        self.assertEqual((ROOT / 'README.md').read_bytes(),
                         (ROOT / 'GameData/PartSculptor/README.md').read_bytes())

    def test_version_metadata(self):
        for name in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            version = __import__('json').loads((ROOT/name).read_text())
            self.assertEqual(version['VERSION']['BUILD'], 97)
        assembly = (ROOT/'PartSculptor/AssemblyVersion.cs').read_text()
        self.assertIn('AssemblyVersion("0.0.4.97")', assembly)

if __name__ == '__main__':
    unittest.main()
