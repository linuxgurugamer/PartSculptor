"""Ensure Log.Debug messages are emitted by the configured logger."""
import unittest
from pathlib import Path

SOURCE = Path(__file__).resolve().parents[2] / 'PartSculptor'

class DetailLogger4143(unittest.TestCase):
    def test_enabled_detail_and_disabled_error(self):
        code = (SOURCE / 'RegisterToolbar.cs').read_text()
        enabled = code.split('if (Settings.DebugLogChanges)', 1)[1].split('else', 1)[0]
        disabled = code.split('else', 1)[1]
        self.assertIn('Log.LEVEL.DETAIL', enabled)
        self.assertNotIn('Log.LEVEL.INFO', enabled)
        self.assertIn('Log.LEVEL.ERROR', disabled)
        self.assertEqual(code.count('Log.LEVEL.DETAIL'), 1)

if __name__ == '__main__':
    unittest.main()
