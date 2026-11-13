"""v0.0.4.136: Controls window draggable outside title, bold part name."""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WINDOW = (ROOT / 'PartSculptor/PartSculptorControlsWindow.cs').read_text()

class ControlsWindowDrag136(unittest.TestCase):
    def test_full_window_drag_for_part_and_placeholder(self):
        self.assertEqual(2, WINDOW.count('GUI.DragWindow();'))
        self.assertNotIn('GUI.DragWindow(new Rect(0f, 0f, controlsWindowRect.width, 25f))', WINDOW)
        self.assertGreater(WINDOW.index('GUI.DragWindow();'),
                           WINDOW.index('GUILayout.EndScrollView();'))
        self.assertGreater(WINDOW.index('GUI.DragWindow();', WINDOW.index('GUI.DragWindow();') + 1),
                           WINDOW.index('This part has no PartSculptor controls.'))

    def test_interactive_controls_drawn_before_drag_region(self):
        self.assertLess(WINDOW.index('DrawControlsActions();'), WINDOW.index('GUI.DragWindow();'))
        self.assertLess(WINDOW.index('GUILayout.TextField('), WINDOW.index('GUI.DragWindow();'))
        self.assertLess(WINDOW.index('GUILayout.BeginScrollView('), WINDOW.index('GUI.DragWindow();'))
        self.assertIn('ClickThruBlocker.GUIWindow', WINDOW)

    def test_bold_part_name_with_existing_close_control(self):
        self.assertIn('GUILayout.Label("Part:", GUILayout.ExpandWidth(false));', WINDOW)
        self.assertIn('GUIStyle partNameStyle = new GUIStyle(GUI.skin.label)', WINDOW)
        self.assertIn('fontStyle = FontStyle.Bold', WINDOW)
        self.assertIn('partNameStyle, GUILayout.ExpandWidth(true)', WINDOW)
        self.assertIn('GUILayout.Button("Close"', WINDOW)

    def test_release_metadata_and_documentation(self):
        for rel in ['PartSculptor.version','GameData/PartSculptor/PartSculptor.version']:
            self.assertEqual(137, json.loads((ROOT/rel).read_text())['VERSION']['BUILD'])
        self.assertIn('AssemblyVersion("0.0.4.137")', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertIn('## 0.0.4.136', (ROOT/'Changelog.md').read_text())
        a = (ROOT/'README.md').read_text()
        b = (ROOT/'GameData/PartSculptor/README.md').read_text()
        self.assertEqual(a,b)
        self.assertIn('unused interior areas', a)

if __name__ == '__main__':
    unittest.main()
