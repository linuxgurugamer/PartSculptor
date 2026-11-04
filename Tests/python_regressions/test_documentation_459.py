"""0.0.4.59 documentation split: readable user guide + preserved history."""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

class Documentation459(unittest.TestCase):
    def test_changelog_preserves_previous_readme_content(self):
        history = (ROOT / 'Changelog.md').read_text(encoding='utf-8')
        for heading in ('## Smaller editor toolbar buttons (0.0.4.58)',
                        '## Editor toolbar button padding removal (0.0.4.57)',
                        '## Editor toolbar background fix (0.0.4.56)',
                        '### Radially linked face-morph corners (0.0.4.55)',
                        '### Face-based morphing (0.0.4.50)'):
            self.assertIn(heading, history)

    def test_new_readme_describes_features_and_controls(self):
        guide = (ROOT / 'README.md').read_text(encoding='utf-8')
        for needle in ('# PartSculptor', '## Features', '## Requirements',
                       '## Quick start: stretching a part', '## Face morphing',
                       '## Presets', '## Vessel Inspector and settings',
                       '## Career Mode limits', '[License.md](License.md)',
                       'Shift + Alt', 'Ctrl + Alt',
                       'Ctrl + Alt + Shift', '0.01', '0.05',
                       '0.10', '0.25'):
            self.assertIn(needle, guide)
        self.assertLess(len(guide.splitlines()), 260)

    def test_version_files_and_assembly(self):
        a = json.loads((ROOT / 'PartSculptor.version').read_text())['VERSION']
        b = json.loads((ROOT / 'GameData/PartSculptor/PartSculptor.version').read_text())['VERSION']
        self.assertEqual(a, b)
        self.assertGreaterEqual(a['BUILD'], 59)
        self.assertIn('AssemblyVersion("0.0.4.%d")' % a['BUILD'],
                      (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())

if __name__ == '__main__':
    unittest.main()
