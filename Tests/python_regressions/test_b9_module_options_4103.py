"""Regression checks for allowed module-level B9 option keys (0.0.4.103)."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
HELPER = (ROOT / "PartSculptor/B9FaceMorphCompatibility.cs").read_text()


def allowed(group):
    match = re.search(r'HashSet<string> ' + group + r'\s*= Set\((.*?)\);',
                      HELPER, flags=re.S)
    assert match is not None, group
    return set(re.findall(r'"([^" ]+)"', match.group(1)))


class B9ModuleOptions4103(unittest.TestCase):
    def test_fAR_option_allowed_in_module(self):
        self.assertIn('affectFARVoxels', allowed('ModuleFields'))

    def test_advanced_tweakables_option_allowed_in_module(self):
        self.assertIn('advancedTweakablesOnly', allowed('ModuleFields'))

    def test_new_options_are_module_fields_only(self):
        for field in ('affectFARVoxels', 'advancedTweakablesOnly'):
            for group in ('SubtypeFields', 'ResourceFields', 'TextureFields'):
                self.assertNotIn(field, allowed(group))

    def test_existing_module_values_retained(self):
        for field in ('moduleID','baseVolume','switchInFlight','affectDragCubes',
                      'showCurrentSubtype', 'showInfo'):
            self.assertIn(field, allowed('ModuleFields'))

    def test_unknown_values_still_rejected(self):
        self.assertIn('if (!AllowedValues(config, ModuleFields, label, out reason)) return false;', HELPER)
        self.assertIn('reason = location + " has unsupported field', HELPER)
        self.assertNotIn('mesh', allowed('ModuleFields'))

    def test_geometry_switches_still_rejected(self):
        self.assertIn('contains geometry/unknown node', HELPER)
        self.assertIn('TRANSFORM position/rotation/scale modifiers', HELPER)
        self.assertNotIn('transform', allowed('ModuleFields'))

    def test_readme_mirrors_and_version(self):
        readme = (ROOT / 'README.md').read_bytes()
        self.assertEqual(readme, (ROOT / 'GameData/PartSculptor/README.md').read_bytes())
        for token in (b'affectFARVoxels', b'advancedTweakablesOnly'):
            self.assertIn(token, readme)
        for name in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT / name).read_text())['VERSION']['BUILD'], 137)
        self.assertIn('AssemblyVersion("0.0.4.137")',
                      (ROOT / 'PartSculptor/AssemblyVersion.cs').read_text())


if __name__ == '__main__':
    unittest.main()
