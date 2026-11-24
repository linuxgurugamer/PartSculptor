"""Source contracts for the compact PG radius row and rendered hover help."""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
UI = (ROOT/'PartSculptor/ProceduralGeometryTool.cs').read_text()
MORPH = (ROOT/'PartSculptor/FaceMorphGizmo.cs').read_text()

class PGHelpLayout5018(unittest.TestCase):
    def test_radius_controls_share_one_row_in_order(self):
        body = UI.split('GUILayout.Label(HelpContent("Radius (m)"', 1)[1].split('GUILayout.EndHorizontal();', 1)[0]
        self.assertLess(body.index('GUILayout.HorizontalSlider('), body.index('GUILayout.TextField('))
        self.assertIn('GUILayout.ExpandWidth(true)', body)
        self.assertIn('GUI.skin.textField.CalcSize(new GUIContent("0.000")).x + 4f', body)
        self.assertIn('GUILayout.Width(radiusFieldWidth), GUILayout.ExpandWidth(false)', body)
        self.assertIn('GUILayout.BeginHorizontal();', UI.split('softSelection = GUILayout.Toggle(', 1)[1].split('GUILayout.Label(HelpContent("Radius (m)"', 1)[0])

    def test_sync_preserves_valid_larger_entries(self):
        body = UI.split('float slid = GUILayout.HorizontalSlider(', 1)[1].split('GUILayout.EndHorizontal();',1)[0]
        self.assertLess(body.index('radiusText = FormatRadius(radiusSlider)'), body.index('string oldText = radiusText'))
        self.assertLess(body.index('LimitRadiusText(radiusText, oldText)'), body.index('radiusSlider = typed;'))
        self.assertIn('Mathf.Max(radiusSliderLimit, typed)', body)

    def test_all_toggle_choices_have_help(self):
        for label in ('Rings', 'Vertices', 'Off', 'X', 'Y', 'Z', 'Soft selection', 'Generate vertex collision fallback'):
            self.assertIn('HelpContent("' + label + '",', UI)
        self.assertIn('new GUIContent(text, HelpTooltipPrefix + tooltip)', UI)

    def test_hover_tooltip_draws_outside_scroll_clip(self):
        self.assertIn('raw.StartsWith(HelpTooltipPrefix, StringComparison.Ordinal)', UI)
        self.assertIn('tooltip = HoveredHelpTooltip() ?? tooltip;\n                GUILayout.EndScrollView();\n                GUILayout.EndArea();\n                DrawHelpTooltip(tooltip);', UI)
        self.assertIn('panel.Contains(Event.current.mousePosition)', UI)
        self.assertIn('FaceMorphGizmo.DrawHoverTooltip(tooltip, Event.current.mousePosition)', UI)
        self.assertIn('DrawMorphTip(new Rect(x, y, width, height), message, style);', MORPH)
        self.assertIn('if (y + height > Screen.height) y = pointer.y - height - 12f;', MORPH)

if __name__ == '__main__': unittest.main()
