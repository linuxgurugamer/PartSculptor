"""Documentation regression for the user-supplied PartSculptor README in v0.0.4.93."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

class ReadmeReplacement493(unittest.TestCase):
    def test_source_and_gamedata_readmes_are_identical(self):
        self.assertEqual((ROOT / "README.md").read_bytes(),
                         (ROOT / "GameData/PartSculptor/README.md").read_bytes())

    def test_supplied_readme_sections_preserved(self):
        guide = (ROOT / "README.md").read_text(encoding="utf-8")
        for needle in ("# PartSculptor", "## Fuel/Part Switchers",
                       "SpaceTuxLibrary", "ProceduralPart",
                       "## License", "[License.md](License.md)"):
            with self.subTest(section=needle):
                self.assertIn(needle, guide)

if __name__ == "__main__":
    unittest.main()
