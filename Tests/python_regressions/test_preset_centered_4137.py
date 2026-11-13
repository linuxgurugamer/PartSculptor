"""v0.0.4.137: user-supplied Controls window with centered preset label."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
WINDOW = (ROOT / 'PartSculptor/PartSculptorControlsWindow.cs').read_text(encoding='utf-8-sig')


class PresetCentered137(unittest.TestCase):
    def test_preset_label_centered_inside_fixed_width(self):
        method = WINDOW.split('private void DrawControlsPresets()', 1)[1].split('private void DrawControlsNumericTilt()', 1)[0]
        self.assertIn('alignment = TextAnchor.MiddleCenter', method)
        self.assertRegex(method, r'GUILayout\.Label\(controlsSelectedPreset \?\? choices\[index\],\s*presetLabelStyle, GUILayout\.Width\(120f\)\)')
        self.assertIn('GUILayout.Button("<", GUILayout.Width(28f))', method)
        self.assertIn('GUILayout.Button(">", GUILayout.Width(28f))', method)
        self.assertIn('GUILayout.Button("Apply", GUILayout.Width(62f))', method)

    def test_user_fields_preserved(self):
        self.assertIn('GUILayout.Width(60)', WINDOW)
        self.assertIn('controlsTiltXText ?? "", 24, GUILayout.Width(60)', WINDOW)
        self.assertIn('controlsTiltZText ?? "", 24, GUILayout.Width(60)', WINDOW)
        self.assertIn('fontStyle = FontStyle.Bold', WINDOW)
        self.assertIn('GUI.DragWindow();', WINDOW)
        self.assertIn('PartSculptor - All Controls', WINDOW)

    def test_version_and_docs(self):
        import json
        for rel in ('PartSculptor.version','GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(137,json.loads((ROOT/rel).read_text())['VERSION']['BUILD'])
        self.assertIn('AssemblyVersion("0.0.4.137")',(ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertEqual((ROOT/'README.md').read_text(),(ROOT/'GameData/PartSculptor/README.md').read_text())
        self.assertIn('## 0.0.4.137', (ROOT/'Changelog.md').read_text())

if __name__ == '__main__':
    unittest.main()
