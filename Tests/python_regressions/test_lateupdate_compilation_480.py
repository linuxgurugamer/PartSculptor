"""PartSculptor 0.0.4.80: keep only one Unity LateUpdate and both scene paths."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
SOURCE = (ROOT / "PartSculptor/PartSculptor.cs").read_text()

class LateUpdateCompilation480(unittest.TestCase):
    def test_only_one_late_update_signature(self):
        signatures = re.findall(r"\b(?:public|private|protected|internal)\s+void\s+LateUpdate\s*\(\s*\)", SOURCE)
        self.assertEqual(len(signatures), 1, signatures)
        self.assertIn("public void LateUpdate()", SOURCE)

    def test_flight_sync_is_called_from_existing_callback(self):
        callback = SOURCE.split("public void LateUpdate()", 1)[1].split(
            "internal static void RefreshPresetOptionsAll", 1)[0]
        self.assertIn("if (HighLogic.LoadedSceneIsFlight)", callback)
        self.assertIn("SynchronizeResourceDisplay();", callback)
        self.assertIn("shipNotificationPending", callback)
        self.assertIn("GameEvents.onEditorShipModified.Fire", callback)

    def test_flight_helper_keeps_resource_label_fix(self):
        method = SOURCE.split("private void SynchronizeResourceDisplay()", 1)[1].split(
            "private static bool ResourceMaximumMatches", 1)[0]
        self.assertIn("part.Resources.Get(item.Resource.resourceName)", method)
        self.assertIn("item.resourceMax.text = corrected", method)
        self.assertIn("SetValueWithoutNotify(targetValue)", method)
        self.assertNotIn("MonoUtilities.RefreshPartContextWindow", method)

if __name__ == "__main__":
    unittest.main()
