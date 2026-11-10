import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAIN = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()
HELPER = (ROOT / 'PartSculptor/B9FaceMorphCompatibility.cs').read_text()
PROJECT = (ROOT / 'PartSculptor/PartSculptor.csproj').read_text()


def allowed_keys(group):
    match = re.search(r'HashSet<string> ' + group + r'\s*= Set\((.*?)\);',
                      HELPER, flags=re.S)
    assert match, group
    return set(re.findall(r'"([^\"]+)"', match.group(1)))


class B9ResourceOnlyFaceMorph467(unittest.TestCase):
    def test_b9_classifier_is_integrated_before_geometry_gate(self):
        start = MAIN.index('internal bool EnsureFaceMorph()')
        end = MAIN.index('internal void SetMorphFaces(', start)
        body = MAIN[start:end]
        self.assertIn('B9FaceMorphCompatibility.IsB9(other)', body)
        self.assertIn('B9FaceMorphCompatibility.AllowsFaceMorph(part, other, out reason)', body)
        self.assertLess(body.index('B9FaceMorphCompatibility.IsB9'),
                        body.index('name.IndexOf("Robotic"'))
        self.assertIn('continue;', body)
        self.assertIn('faceMorph = new FaceMorphSupport(part, model);', body)

    def test_streamline_four_subtypes_supported_fields(self):
        # The reported DTE OX-50-61 config uses these keys in all subtypes.
        m = allowed_keys('ModuleFields')
        s = allowed_keys('SubtypeFields')
        self.assertTrue({'name', 'moduleID', 'baseVolume',
                         'switcherDescription'} <= m)
        self.assertTrue({'name', 'tankType', 'addedMass'} <= s)
        self.assertTrue({'name', 'unitsPerVolume', 'percentFilled'} <=
                        allowed_keys('ResourceFields'))

    def test_geometry_affecting_fields_still_blocked(self):
        all_allowed = allowed_keys('ModuleFields') | allowed_keys('SubtypeFields')
        for key in ('node', 'attachNode', 'texture',
                    'transformName', 'surfaceAttachNode', 'mesh'):
            self.assertNotIn(key, all_allowed)
        self.assertIn('transform', allowed_keys('SubtypeFields'))
        self.assertNotIn('transform', allowed_keys('ModuleFields'))
        self.assertIn('if (!AllowedValues(child, SubtypeFields', HELPER)
        self.assertIn('if (!AllowedValues(config, ModuleFields', HELPER)

    def test_geometry_child_nodes_blocked(self):
        self.assertIn('string.Equals(nested.name, "RESOURCE"', HELPER)
        self.assertIn('string.Equals(nested.name, "TEXTURE"', HELPER)
        for node in ('TRANSFORM', 'MATERIAL', 'NODE', 'MODULE'):
            self.assertNotIn('string.Equals(nested.name, "' + node + '"',
                             HELPER[HELPER.index('foreach (ConfigNode nested'):])
        self.assertIn('contains geometry/unknown node', HELPER)

    def test_matches_switcher_and_blocks_unverified_configuration(self):
        self.assertIn('part.partInfo.partConfig', HELPER)
        self.assertIn('object.ReferenceEquals(module, switcher)', HELPER)
        self.assertIn('FindMember(switcher.GetType(), "moduleID")', HELPER)
        self.assertIn('moduleID mismatch', HELPER)
        self.assertIn('no verified morph-safe SUBTYPE', HELPER)
        self.assertIn('if (count == 0)', HELPER)

    def test_no_b9_hard_reference_and_preserved_resource_scaling(self):
        self.assertIn('<Compile Include="B9FaceMorphCompatibility.cs" />', PROJECT)
        self.assertIn('B9PartSwitchSupport', MAIN)
        self.assertNotIn('using B9PartSwitch', HELPER)
        self.assertIn('B9DisableTransform', MAIN)

    def test_versions(self):
        self.assertIn('"BUILD": 106', (ROOT/'PartSculptor.version').read_text())
        self.assertIn('"BUILD": 106',
                      (ROOT/'GameData/PartSculptor/PartSculptor.version').read_text())
        self.assertIn('AssemblyVersion("0.0.4.106")',
                      (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())

if __name__ == '__main__':
    unittest.main()
