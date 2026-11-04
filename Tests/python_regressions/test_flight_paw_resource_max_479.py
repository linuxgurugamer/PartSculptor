"""Regression checks for direct synchronization of flight PAW resource maxima."""
from pathlib import Path
import unittest
import json

ROOT = Path(__file__).resolve().parents[2]
SOURCE = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()


class FlightPawResourceMax479(unittest.TestCase):
    def test_reads_real_resource_from_paw_item_name(self):
        self.assertIn('UIPartActionResource[] resourceDisplayItems;', SOURCE)
        self.assertIn('window.GetComponentsInChildren<UIPartActionResource>(true)', SOURCE)
        self.assertIn('part.Resources.Get(item.Resource.resourceName)', SOURCE)
        self.assertIn('item.Part != part', SOURCE)

    def test_corrects_just_a_stale_maximum_text(self):
        body = SOURCE.split('private void SynchronizeResourceDisplay()', 1)[1].split(
            'private static bool ResourceMaximumMatches', 1)[0]
        self.assertIn('ResourceMaximumMatches(shown, live.maxAmount)', body)
        self.assertIn('item.resourceMax.text = corrected;', body)
        self.assertIn('CultureInfo.CurrentCulture', body)
        self.assertNotIn('resource.maxAmount =', body)
        self.assertNotIn('resource.amount =', body)
        self.assertNotIn('MonoUtilities.RefreshPartContextWindow(part)', body)

    def test_progress_bar_does_not_emit_transfer_changes(self):
        self.assertIn('!item.progBar.interactable', SOURCE)
        self.assertIn('live.amount / live.maxAmount', SOURCE)
        self.assertIn('item.progBar.SetValueWithoutNotify(targetValue)', SOURCE)

    def test_handles_reopened_and_rebuilt_flight_paw(self):
        self.assertIn('resourceDisplayWindow = null;', SOURCE)
        self.assertIn('resourceDisplayItems = null;', SOURCE)
        self.assertIn('Time.unscaledTime >= nextResourceWidgetScan', SOURCE)
        self.assertIn('nextResourceWidgetScan = Time.unscaledTime + 0.5f', SOURCE)
        self.assertIn('resourceDisplayDirty = false;', SOURCE)

    def test_preserves_unscaled_paw_and_no_repeated_rebuild(self):
        self.assertIn('HighLogic.LoadedSceneIsFlight', SOURCE)
        self.assertIn('HighLogic.LoadedSceneIsEditor', SOURCE)
        refresh = SOURCE.split('private void RefreshFlightResourceDisplay()', 1)[1].split(
            'private void SynchronizeResourceDisplay()', 1)[0]
        self.assertNotIn('displayDirty = true', refresh)
        self.assertNotIn('MonoUtilities.RefreshPartContextWindow', refresh)

    def test_version_and_manual_plan(self):
        for path in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT / path).read_text())['VERSION']['BUILD'], 81)
        self.assertIn('AssemblyVersion("0.0.4.81")',
                      (ROOT / 'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertIn('UIPartActionResource', (ROOT / 'Changelog.md').read_text())
        self.assertTrue((ROOT / 'Tests/FLIGHT_PAW_RESOURCE_MAX_0.0.4.79.md').is_file())


if __name__ == '__main__':
    unittest.main()
