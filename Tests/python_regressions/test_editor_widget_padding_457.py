"""Regression: editor toolbar Stretch/Morph icons and buttons are flush."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TOOL = (ROOT / 'PartSculptor/EditorStretchTool.cs').read_text()


class EditorWidgetPadding457(unittest.TestCase):
    def test_no_icon_insets(self):
        self.assertRegex(TOOL, r'iconRect\.offsetMin\s*=\s*Vector2\.zero\s*;')
        self.assertRegex(TOOL, r'iconRect\.offsetMax\s*=\s*Vector2\.zero\s*;')
        self.assertRegex(TOOL, r'morphIconRect\.offsetMin\s*=\s*Vector2\.zero\s*;')
        self.assertRegex(TOOL, r'morphIconRect\.offsetMax\s*=\s*Vector2\.zero\s*;')
        self.assertNotIn('new Vector2(4f, 4f)', TOOL)
        self.assertNotIn('new Vector2(-4f, -4f)', TOOL)

    def test_no_extra_tool_gaps(self):
        self.assertIn('private const float IconGap = 3f;', TOOL)
        self.assertIn('float gapWorld = width * (IconGap / referenceWidth);', TOOL)
        self.assertIn('(buttonWorldWidth * 0.5f)', TOOL)
        self.assertIn('(buttonWorldWidth + gapWorld)', TOOL)

    def test_grey_and_selection_states_preserved(self):
        self.assertIn('background.color = EditorToolGrey;', TOOL)
        self.assertIn('morphBackground.color = EditorToolGrey;', TOOL)
        self.assertIn('new Color(0.35f, 0.82f, 0.45f) : EditorToolGrey', TOOL)
        self.assertIn('new Color(0.12f, 0.78f, 0.95f) : EditorToolGrey', TOOL)

    def test_version(self):
        for path in [ROOT / 'PartSculptor.version',
                     ROOT / 'GameData/PartSculptor/PartSculptor.version']:
            self.assertGreaterEqual(json.loads(path.read_text())['VERSION']['BUILD'], 57)
        assembly = (ROOT / 'PartSculptor/AssemblyVersion.cs').read_text()
        self.assertRegex(assembly, r'AssemblyVersion\("0\.0\.4\.\d+"\)')
        self.assertRegex(assembly, r'AssemblyFileVersion\("0\.0\.4\.\d+"\)')


if __name__ == '__main__':
    unittest.main()
