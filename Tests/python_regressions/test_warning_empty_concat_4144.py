"""Regression: warning strings do not use redundant empty-string concatenation."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

class WarningEmptyConcat4145(unittest.TestCase):
    def test_warning_calls_have_no_empty_string_prefix(self):
        pattern = re.compile(r'\bLog\s*\.\s*Warning\s*\(\s*""\s*\+')
        for path in (ROOT / 'PartSculptor').rglob('*.cs'):
            self.assertIsNone(pattern.search(path.read_text(encoding='utf-8-sig')), str(path))

    def test_warning_severity_and_context_preserved(self):
        expected = {
            'CrewCapacityScaling.cs': 'Log.Warning(part.name +',
            'EditorStretchTool.cs': 'Log.Warning(faceStatus);',
            'FuelSwitchSupport.cs': 'Log.Warning(p.Kind +',
            'PartSculptor.cs': 'Log.Warning(part.name + ": " + warning);',
            'ScaleSettingsWindow.cs': 'Log.Warning(error);',
        }
        for filename, fragment in expected.items():
            content = (ROOT / 'PartSculptor' / filename).read_text(encoding='utf-8-sig')
            self.assertIn(fragment, content)

if __name__ == '__main__':
    unittest.main()
