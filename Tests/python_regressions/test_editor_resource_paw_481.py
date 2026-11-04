"""PartSculptor 0.0.4.81: update stale maximum labels in editor and flight."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
SOURCE = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()

class EditorResourcePaw481(unittest.TestCase):
    def test_editor_calls_resource_sync_before_notification_gate(self):
        body = SOURCE.split('public void LateUpdate()', 1)[1].split('internal static void RefreshPresetOptionsAll', 1)[0]
        self.assertIn('if (HighLogic.LoadedSceneIsEditor)\n                SynchronizeResourceDisplay();',body)
        self.assertLess(body.index('SynchronizeResourceDisplay();', body.index('HighLogic.LoadedSceneIsEditor')), body.index('if (!shipNotificationPending'))

    def test_sync_checks_both_scenes_and_live_resource(self):
        body = SOURCE.split('private void SynchronizeResourceDisplay()', 1)[1].split('private static bool ResourceMaximumMatches', 1)[0]
        self.assertIn('!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight',body)
        self.assertIn('part.Resources.Get(item.Resource.resourceName)',body)
        self.assertIn('item.resourceMax.text = corrected;',body)
        self.assertNotIn('resource.maxAmount =',body)
        self.assertNotIn('resource.amount =',body)

    def test_1x_reset_can_repair_stale_gui(self):
        body = SOURCE.split('private void SynchronizeResourceDisplay()', 1)[1].split('private static bool ResourceMaximumMatches', 1)[0]
        self.assertIn('resourceDisplayWasScaled',body)
        self.assertIn('resourceDisplayWasScaled = stretched;',body)
        self.assertIn('resourceDisplayDirty = false;',body)

    def test_editor_does_not_clear_dirty_before_display_checked(self):
        body = SOURCE.split('public void Update()',1)[1].split('private void RefreshFlightResourceDisplay()',1)[0]
        self.assertIn('part.PartActionWindow.displayDirty = true;',body)
        self.assertNotIn('resourceDisplayDirty = false;',body)

    def test_one_lateupdate_and_no_paw_rebuild_in_sync(self):
        import re
        self.assertEqual(len(re.findall(r'\bvoid\s+LateUpdate\s*\(\s*\)', SOURCE)),1)
        body = SOURCE.split('private void SynchronizeResourceDisplay()', 1)[1].split('private static bool ResourceMaximumMatches', 1)[0]
        self.assertNotIn('MonoUtilities.RefreshPartContextWindow',body)

if __name__ == '__main__': unittest.main()
