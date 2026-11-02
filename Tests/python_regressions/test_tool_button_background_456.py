"""Regression checks: transparent Stretch/Morph icons sit over KSP-grey UI Images."""
import json
import re
import struct
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = (ROOT / 'PartSculptor/EditorStretchTool.cs').read_text()
ICON_DIR = ROOT / 'GameData/PartSculptor/PluginData/Icons'


class ToolBackground456(unittest.TestCase):
    def test_buttons_do_not_use_white_idle_background(self):
        self.assertIn('private static readonly Color EditorToolGrey', SOURCE)
        self.assertIn('background.color = EditorToolGrey;', SOURCE)
        self.assertIn('morphBackground.color = EditorToolGrey;', SOURCE)
        region = SOURCE.split('private void RefreshButtonColor()', 1)[1].split(
            'private static Part PickPart', 1)[0]
        self.assertIn('new Color(0.35f, 0.82f, 0.45f) : EditorToolGrey', region)
        self.assertIn('new Color(0.12f, 0.78f, 0.95f) : EditorToolGrey', region)
        self.assertNotIn(': Color.white', region)

    def test_both_icons_use_png_alpha_over_grey(self):
        self.assertIn('typeof(RawImage)', SOURCE)
        self.assertIn('icon.raycastTarget = false;', SOURCE)
        self.assertIn('morphIcon.raycastTarget = false;', SOURCE)
        self.assertIn('if (texture != null)', SOURCE)
        self.assertIn('icon.color = Color.white;', SOURCE)
        self.assertIn('if (morphTexture != null)', SOURCE)
        self.assertIn('morphIcon.color = Color.white;', SOURCE)
        # Portable IHDR check: both PNGs have RGBA (type 6) rather than
        # RGB (type 2), ensuring that the source icon has an alpha channel.
        for name in ['inspector_38.png', 'morph_38.png']:
            raw = (ICON_DIR / name).read_bytes()
            self.assertTrue(raw.startswith(b'\x89PNG\r\n\x1a\n'))
            self.assertEqual(raw[12:16], b'IHDR')
            self.assertEqual(raw[25], 6, name + ' is not RGBA')

    def test_version_and_selection_still_work(self):
        version = json.loads((ROOT / 'PartSculptor.version').read_text())['VERSION']
        self.assertGreaterEqual(version['BUILD'], 56)
        self.assertEqual(version, json.loads((ROOT /
            'GameData/PartSculptor/PartSculptor.version').read_text())['VERSION'])
        expected = '{}.{}.{}.{}'.format(version['MAJOR'], version['MINOR'],
                                       version['PATCH'], version['BUILD'])
        self.assertIn('AssemblyVersion("{}")'.format(expected),
                      (ROOT / 'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertIn('toolButton.onClick.AddListener(ToggleStretch)', SOURCE)
        self.assertIn('morphToolButton.onClick.AddListener(ToggleMorph)', SOURCE)


if __name__ == '__main__':
    unittest.main()
