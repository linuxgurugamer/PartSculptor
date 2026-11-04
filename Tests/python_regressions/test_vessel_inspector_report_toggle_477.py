"""Source-level checks for retaining the Vessel Inspector report visibility filter."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
REPORT = (ROOT / 'PartSculptor/ScaledValuesWindow.cs').read_text()
INSPECTOR = (ROOT / 'PartSculptor/VesselInspector.cs').read_text()


class VesselInspectorReportToggleTests(unittest.TestCase):
    def test_report_exposes_current_or_pending_toggle(self):
        self.assertIn('internal bool ShowUnchangedValues', REPORT)
        self.assertIn('return hasPendingShowUnchanged ? pendingShowUnchanged : showUnchanged;', REPORT)

    def test_fresh_report_starts_with_previous_toggle_setting(self):
        self.assertIn('internal ScaledValuesWindow(bool initiallyShowUnchanged = false)', REPORT)
        self.assertIn('showUnchanged = initiallyShowUnchanged;', REPORT)
        self.assertIn('selectedReport = new ScaledValuesWindow(selectedReport.ShowUnchangedValues);', INSPECTOR)

    def test_selection_uses_layout_safe_new_report(self):
        self.assertIn('if (Event.current.type == EventType.Layout) RefreshOnLayout();', INSPECTOR)
        self.assertIn('Select(parts.Contains(queuedPart) ? queuedPart : null);', INSPECTOR)
        self.assertIn('if (selectedPart == part) return;', INSPECTOR)
        self.assertIn('selectedReport.DrawEmbedded(module, rightWidth,', INSPECTOR)
        select_block = INSPECTOR.split('void Select(Part part)', 1)[1].split('void ClampWindowSize()', 1)[0]
        self.assertNotIn('selectedReport = new ScaledValuesWindow();', select_block)

    def test_standalone_paw_report_starts_with_its_own_default(self):
        self.assertIn('bool initiallyShowUnchanged = false', REPORT)
        self.assertIn('bool requestedShowUnchanged = GUILayout.Toggle(showUnchanged,', REPORT)
        self.assertIn('"Show unchanged values"', REPORT)
        self.assertIn('hasPendingShowUnchanged = true;', REPORT)

    def test_manual_plan_and_readme(self):
        self.assertIn('**Show unchanged values** toggle', (ROOT / 'README.md').read_text())
        self.assertTrue((ROOT / 'Tests/VESSEL_INSPECTOR_REPORT_TOGGLE_0.0.4.77.md').exists())


if __name__ == '__main__':
    unittest.main()
