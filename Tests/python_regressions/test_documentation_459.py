"""0.0.4.59 documentation split: readable user guide + preserved history."""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

class Documentation459(unittest.TestCase):
    def test_changelog_preserves_previous_readme_content(self):
        history = (ROOT / 'Changelog.md').read_text(encoding='utf-8')
        # The user's abbreviated changelog replaces the prior detailed history.
        for heading in ('0.0.4.148', '0.0.4.147', '0.0.4.142',
                        '0.0.4.139', '0.0.4.137', '0.0.4.101'):
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
        self.assertLess(len(guide.splitlines()), 440)

    def test_version_files_and_assembly(self):
        a = json.loads((ROOT / 'PartSculptor.version').read_text())['VERSION']
        b = json.loads((ROOT / 'GameData/PartSculptor/PartSculptor.version').read_text())['VERSION']
        self.assertEqual(a, b)
        self.assertGreaterEqual(a['BUILD'], 5)
        self.assertIn('AssemblyVersion("0.0.5.%d")' % a['BUILD'],
                      (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())

if __name__ == '__main__':
    unittest.main()
