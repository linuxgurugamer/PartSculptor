"""Regression checks for PartSculptor 0.0.4.92 user-supplied Vessel Inspector rows."""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
CODE = (ROOT / "PartSculptor" / "VesselInspector.cs").read_text(encoding="utf-8-sig")

class VesselInspectorToggleTests(unittest.TestCase):
    def test_part_row_uses_selected_toggle(self):
        self.assertIn('if (GUILayout.Toggle(', CODE)
        self.assertIn('part == selectedPart,', CODE)
        self.assertIn('GUI.skin.button) &&', CODE)

    def test_selection_changes_only_for_different_part(self):
        self.assertIn('part != selectedPart)', CODE)
        self.assertIn('queuedPart = part;', CODE)
        self.assertIn('selectionQueued = true;', CODE)

    def test_inspector_keeps_hover_behavior(self):
        self.assertIn('partUnderMouse = part;', CODE)
        self.assertIn('SetHoveredPart(partUnderMouse)', CODE)

    def test_selected_report_retains_show_unchanged_toggle(self):
        self.assertIn('new ScaledValuesWindow(selectedReport.ShowUnchangedValues)', CODE)

if __name__ == '__main__':
    unittest.main()
