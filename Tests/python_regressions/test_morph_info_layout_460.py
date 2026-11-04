"""0.0.4.60: morph info box directly under six editor tools."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
EDITOR = (ROOT / 'PartSculptor/EditorStretchTool.cs').read_text()
MORPH = (ROOT / 'PartSculptor/FaceMorphGizmo.cs').read_text()

class MorphInfoLayout460(unittest.TestCase):
    def test_box_uses_all_six_buttons(self):
        self.assertIn('private readonly List<RectTransform> stockToolRects', EDITOR)
        self.assertIn('foreach (Selectable nativeTool in best)', EDITOR)
        self.assertIn('stockToolRects.Add(nativeRect)', EDITOR)
        self.assertIn('foreach (RectTransform nativeTool in stockToolRects)', EDITOR)
        self.assertIn('IncludeButtonBounds(toolRect, uiCamera', EDITOR)
        self.assertIn('IncludeButtonBounds(morphToolRect, uiCamera', EDITOR)

    def test_left_edge_not_past_first_tool(self):
        self.assertIn('left = Mathf.Min(left, screen.x);', EDITOR)
        self.assertIn('right = Mathf.Max(right, screen.x);', EDITOR)
        self.assertIn('float x = Mathf.Max(0f, left);', EDITOR)
        self.assertIn('Mathf.Min(Screen.width, right) - x', EDITOR)
        self.assertIn('Screen.height - bottom + 4f', EDITOR)
        self.assertIn('RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i])', EDITOR)

    def test_help_and_failures_share_adaptive_box(self):
        self.assertIn('FaceMorphGizmo.DrawActive(helpRect);', EDITOR)
        self.assertIn('FaceMorphGizmo.DrawHelpBox(helpRect, faceStatus);', EDITOR)
        self.assertIn('private void DrawLabels(Rect helpRect)', MORPH)
        self.assertIn('style.wordWrap = false;', MORPH)
        self.assertIn('style.CalcMinMaxWidth(new GUIContent(lines[i])', MORPH)
        self.assertIn('style.CalcHeight(new GUIContent(lines[i]), bounds.width)', MORPH)
        self.assertIn('DrawHelpBox(helpRect,', MORPH)
        self.assertNotIn('new Rect(12f, 249f, 790f, 27f)', MORPH)
        self.assertNotIn('new Rect(14f, 176f, 630f, 45f)', EDITOR)

    def test_toolbar_cleanup(self):
        self.assertIn('stockToolRects.Clear();', EDITOR)
        self.assertIn('if (float.IsInfinity(left)', EDITOR)
        self.assertIn('Screen.height - bounds.y', MORPH)

if __name__ == '__main__': unittest.main()
