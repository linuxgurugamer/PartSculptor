"""All C# logging uses the common Log helper without redundant literal mod prefixes."""
import unittest
from pathlib import Path

SOURCE = Path(__file__).resolve().parents[2] / "PartSculptor"

class LogPrefix4141(unittest.TestCase):
    def test_no_redundant_prefix_in_code(self):
        for path in SOURCE.rglob("*.cs"):
            with self.subTest(file=path.name):
                self.assertNotIn("[PartSculptor]", path.read_text())

    def test_screen_context_warning_uses_common_logger(self):
        code = (SOURCE / "PartSculptorScreenMessages.cs").read_text()
        self.assertIn('Log.Warning("[ScreenMessage] Part context lookup failed: " + e);', code)
        self.assertNotIn("Debug.LogWarning(", code)
        self.assertIn('Log.Info("[ScreenMessage] source="', code)
        self.assertIn('part.partInfo.partUrl', code)
        self.assertIn('part.partInfo.configFileFullName', code)

    def test_scale_debug_uses_debug_logger(self):
        code = (SOURCE / "ScaleDebug.cs").read_text()
        self.assertEqual(code.count('Log.Debug('), 3)
        self.assertNotIn('Log.Info("[DEBUG] ', code)

if __name__ == "__main__":
    unittest.main()
