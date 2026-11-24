"""Static regression checks for unknown B9 value filtering in v0.0.4.148."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = (ROOT / 'PartSculptor/B9FaceMorphCompatibility.cs').read_text()


class B9UnknownValues4146(unittest.TestCase):
    def test_using_static_register_toolbar_not_ksp_log(self):
        self.assertIn('using static PartSculptor.RegisterToolbar;', SOURCE)
        self.assertNotIn('using KSP_Log;', SOURCE)

    def test_allowed_values_only_rejects_unrecognized_numeric_values(self):
        body = SOURCE.split('private static bool AllowedValues(', 1)[1].split('// A malformed texture', 1)[0]
        self.assertIn('if (fields.Contains(entry.name) || !IsNumericValue(entry.value))', body)
        self.assertIn('unsupported numeric field', body)
        self.assertNotIn('if (fields.Contains(entry.name)) continue;', body)

    def test_numeric_detection_is_culture_invariant_and_handles_vectors(self):
        body = SOURCE.split('private static bool IsNumericValue(', 1)[1].split('private static bool AllowedValues(', 1)[0]
        self.assertIn('double.TryParse(value, NumberStyles.Float,', body)
        self.assertIn('CultureInfo.InvariantCulture', body)
        self.assertIn("value.Split(',')", body)
        self.assertIn('components.Length < 2', body)
        self.assertIn('double.TryParse(component.Trim(), NumberStyles.Float,', body)
        self.assertIn('if (string.IsNullOrWhiteSpace(value)) return false;', body)

    def test_nested_nodes_still_validated_separately(self):
        self.assertIn('contains geometry/unknown node', SOURCE)
        self.assertIn('AllowsNodeModifier(part, nested, where + " / NODE"', SOURCE)
        self.assertIn('contains unsupported nested nodes', SOURCE)

    def test_version_and_readme_mirror(self):
        self.assertIn('AssemblyVersion("0.0.4.149")', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertEqual((ROOT/'README.md').read_bytes(),
                         (ROOT/'GameData/PartSculptor/README.md').read_bytes())


if __name__ == '__main__':
    unittest.main()
