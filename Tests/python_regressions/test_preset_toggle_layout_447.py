"""Regression guards for preset visibility toggle layout."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = (ROOT / "PartSculptor" / "ScalePresetsWindow.cs").read_text()

class ToggleLayoutTests(unittest.TestCase):
    def test_separate_checkbox_labels(self):
        self.assertIn('GUILayout.Toggle(showAbsolute, "")', SRC)
        self.assertIn('GUILayout.Label("Show absolute")', SRC)
        self.assertIn('GUILayout.Toggle(showRelative, "")', SRC)
        self.assertIn('GUILayout.Label("Show relative")', SRC)
        self.assertGreaterEqual(SRC.count('GUILayout.FlexibleSpace();'), 2)

    def test_deferred_filter_changes(self):
        self.assertIn('if (nextAbsolute != showAbsolute) queuedShowAbsolute = nextAbsolute;', SRC)
        self.assertIn('if (nextRelative != showRelative) queuedShowRelative = nextRelative;', SRC)
        self.assertIn('if (Event.current.type == EventType.Layout)', SRC)

if __name__ == '__main__':
    unittest.main()
