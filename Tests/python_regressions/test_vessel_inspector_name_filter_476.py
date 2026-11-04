"""Source-level regression checks for the Vessel Inspector's name search."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
CODE = (ROOT / 'PartSculptor/VesselInspector.cs').read_text()

class VesselInspectorNameFilterTests(unittest.TestCase):
    def test_filter_matches_display_title_and_internal_name(self):
        self.assertIn('static bool MatchesNameFilter(Part part, string query)', CODE)
        self.assertIn('string title = DisplayName(part);', CODE)
        self.assertIn('title.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0', CODE)
        self.assertIn('part.name.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0', CODE)
        self.assertIn('query.Trim()', CODE)
        self.assertIn('string.IsNullOrWhiteSpace(query)', CODE)

    def test_search_box_and_clear_action_present(self):
        self.assertIn('GUILayout.Label("Filter by name:")', CODE)
        self.assertIn('GUILayout.TextField(nameFilter, 128,', CODE)
        self.assertIn('GUILayout.Button("Clear", GUILayout.Width(48f))', CODE)
        self.assertIn('pendingNameFilter = "";', CODE)
        self.assertIn('GUI.FocusControl(null);', CODE)

    def test_filters_are_combined_not_replaced(self):
        self.assertIn('if ((!scaledPartsOnly || HasChangedScale(part)) &&', CODE)
        self.assertIn('MatchesNameFilter(part, nameFilter)) parts.Add(part);', CODE)
        self.assertIn('++totalPartCount;', CODE)
        self.assertIn('"No parts match the name filter."', CODE)

    def test_queue_changes_only_apply_in_layout(self):
        self.assertIn('if (Event.current.type == EventType.Layout) RefreshOnLayout();', CODE)
        self.assertIn('bool nameFilterChangeQueued;', CODE)
        self.assertIn('if (nameFilterChangeQueued)', CODE)
        self.assertIn('nameFilter = pendingNameFilter;', CODE)
        self.assertIn('partScroll.y = 0f;', CODE)

    def test_scene_selection_reveals_part_behind_filters(self):
        self.assertIn('if (!MatchesNameFilter(clicked, nameFilter))', CODE)
        self.assertIn('pendingNameFilter = "";', CODE)
        self.assertIn('if (scaledPartsOnly && !HasChangedScale(clicked))', CODE)
        self.assertIn('Select(parts.Contains(queuedPart) ? queuedPart : null);', CODE)

    def test_readme_and_manual_test_plan_cover_search(self):
        readme = (ROOT / 'README.md').read_text()
        self.assertIn('**Filter by name**', readme)
        self.assertIn('internal part name', readme)
        self.assertIn('**Scaled parts only**', readme)
        self.assertTrue((ROOT / 'Tests/VESSEL_INSPECTOR_NAME_FILTER_0.0.4.76.md').exists())

if __name__ == '__main__':
    unittest.main()
