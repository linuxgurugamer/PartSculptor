"""Static regression checks: 28x28 buttons and edge-adjacent alignment."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = (ROOT / 'PartSculptor/EditorStretchTool.cs').read_text()


class EditorWidgetSize458(unittest.TestCase):
    def test_both_buttons_28_by_28(self):
        self.assertIn('private const float ToolButtonSize = 28f;', SOURCE)
        self.assertIn('toolRect.sizeDelta = new Vector2(ToolButtonSize, ToolButtonSize);', SOURCE)
        self.assertIn('morphToolRect.sizeDelta = new Vector2(ToolButtonSize, ToolButtonSize);', SOURCE)
        self.assertNotIn('sizeDelta = stockReference.rect.size;', SOURCE)

    def test_world_size_respects_ksp_ui_scale(self):
        self.assertIn('buttonWorldWidth = width * (ToolButtonSize / referenceWidth)', SOURCE)
        self.assertIn('buttonWorldHeight = height * (ToolButtonSize / referenceHeight)', SOURCE)
        self.assertIn('buttonWorldWidth / unitX', SOURCE)
        self.assertIn('buttonWorldHeight / unitY', SOURCE)
        self.assertIn('buttonWorldWidth / morphX', SOURCE)
        self.assertIn('buttonWorldHeight / morphY', SOURCE)

    def test_edge_alignment_without_gaps(self):
        self.assertIn('private const float IconGap = 0f;', SOURCE)
        self.assertIn('(buttonWorldWidth * 0.5f + gapWorld)', SOURCE)
        self.assertIn('(buttonWorldWidth + gapWorld)', SOURCE)
        # Example native 32px button width, custom 28px width:
        right_edge_stock = 32
        custom_center = right_edge_stock + 28 / 2
        morph_center = custom_center + 28
        self.assertEqual(custom_center - 28 / 2, right_edge_stock)
        self.assertEqual(morph_center - 28 / 2, custom_center + 28 / 2)
        self.assertEqual(morph_center + 28 / 2, right_edge_stock + 56)

    def test_transparency_colors_padding_preserved(self):
        self.assertIn('iconRect.offsetMin = Vector2.zero;', SOURCE)
        self.assertIn('iconRect.offsetMax = Vector2.zero;', SOURCE)
        self.assertIn('morphIconRect.offsetMin = Vector2.zero;', SOURCE)
        self.assertIn('morphIconRect.offsetMax = Vector2.zero;', SOURCE)
        self.assertIn('background.color = EditorToolGrey;', SOURCE)
        self.assertIn('morphBackground.color = EditorToolGrey;', SOURCE)

    def test_version_at_least_458(self):
        for name in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            self.assertGreaterEqual(json.loads((ROOT/name).read_text())['VERSION']['BUILD'], 58)
        assembly = (ROOT/'PartSculptor/AssemblyVersion.cs').read_text()
        version = json.loads((ROOT/'PartSculptor.version').read_text())['VERSION']
        expected = '0.0.%s.%s' % (version['PATCH'], version['BUILD'])
        self.assertIn('AssemblyVersion("%s")' % expected, assembly)
        self.assertIn('AssemblyFileVersion("%s")' % expected, assembly)


if __name__ == '__main__':
    unittest.main()
