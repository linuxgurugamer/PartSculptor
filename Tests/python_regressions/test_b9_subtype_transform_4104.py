"""Source-level compatibility checks for B9 SUBTYPE transform variants (v0.0.4.104)."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
HELPER = (ROOT / 'PartSculptor/B9FaceMorphCompatibility.cs').read_text()
MORPH = (ROOT / 'PartSculptor/FaceMorphSupport.cs').read_text()
MAIN = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()
README = (ROOT / 'README.md').read_text()


def keys(group):
    match = re.search(r'HashSet<string> ' + group + r'\s*= Set\((.*?)\);',
                      HELPER, re.S)
    assert match, group
    return set(re.findall(r'"([^"]+)"', match.group(1)))


class B9SubtypeTransform4104(unittest.TestCase):
    def test_parent_id_allowed_at_module_level_only(self):
        self.assertIn('parentID', keys('ModuleFields'))
        for group in ('SubtypeFields', 'ResourceFields', 'TextureFields'):
            self.assertNotIn('parentID', keys(group))

    def test_toggle_transform_allowed_on_subtype(self):
        self.assertIn('transform', keys('SubtypeFields'))
        self.assertNotIn('transform', keys('ModuleFields'))
        self.assertNotIn('baseTransform', keys('SubtypeFields'))

    def test_repeated_selectors_handled_individually(self):
        body = HELPER[HELPER.index('private static bool AllowsSubtypeTransformToggles'):]
        body = body[:body.index('internal static bool IsB9')]
        self.assertIn('foreach (ConfigNode.Value entry in subtype.values)', body)
        self.assertIn('entry.value', body)
        self.assertIn('StringComparison.OrdinalIgnoreCase', body)
        self.assertNotIn('GetValue("transform")', body)
        self.assertIn('string.IsNullOrWhiteSpace(entry.value)', body)
        self.assertIn('contains an empty transform selector', body)

    def test_all_subtypes_must_pass_toggle_validation(self):
        self.assertIn('if (!AllowsSubtypeTransformToggles(child, where, out reason)) return false;', HELPER)
        self.assertIn('foreach (ConfigNode child in config.nodes)', HELPER)
        self.assertIn('if (!AllowedValues(child, SubtypeFields, where, out reason)) return false;', HELPER)

    def test_moving_transform_nodes_and_attach_nodes_still_rejected(self):
        nested = HELPER[HELPER.index('foreach (ConfigNode nested in child.nodes)'):]
        for operation in ('TRANSFORM', 'NODE', 'MODULE', 'MATERIAL'):
            self.assertNotIn('string.Equals(nested.name, "' + operation + '"', nested)
        self.assertIn('contains geometry/unknown node', nested)
        self.assertNotIn('node', keys('SubtypeFields'))
        self.assertNotIn('attachNode', keys('SubtypeFields'))

    def test_active_and_inactive_visual_variants_are_cloned(self):
        self.assertIn('model.GetComponentsInChildren<MeshFilter>(true)', MORPH)
        self.assertIn('if (f != null) f.sharedMesh = r.instance;', MORPH)
        self.assertIn('r.instance = UnityEngine.Object.Instantiate(native);', MORPH)
        self.assertIn('r.vertices = native.vertices;', MORPH)
        self.assertNotIn('.SetActive(', MORPH)

    def test_inactive_variants_colliders_also_included(self):
        self.assertIn('part.GetComponentsInChildren<MeshCollider>(true)', MORPH)
        self.assertIn('part.GetComponentsInChildren<Collider>(true)', MORPH)
        self.assertIn('r.collider.sharedMesh = r.instance;', MORPH)
        self.assertIn('r.primitive.enabled = identity && r.originalEnabled;', MORPH)

    def test_b9_changes_reapply_existing_morph(self):
        callback = MAIN[MAIN.index('void OnB9SubtypeChanged()'):]
        callback = callback[:callback.index('void OnFuelSwitchChanged()')]
        self.assertIn('Apply(false, false, false);', callback)
        self.assertIn('faceMorph.RebaseNodes();', MAIN)
        self.assertIn('faceMorph.Apply(GetMorphFace(0), GetMorphFace(1), false);', MAIN)
        self.assertIn('B9FaceMorphCompatibility.AllowsFaceMorph(part, other, out reason)', MAIN)

    def test_no_new_b9_dependency(self):
        project = (ROOT/'PartSculptor/PartSculptor.csproj').read_text()
        self.assertIn('<Compile Include="B9FaceMorphCompatibility.cs" />', project)
        self.assertNotIn('using B9PartSwitch', HELPER)
        self.assertIn('B9PartSwitchSupport.Create', MAIN)

    def test_version_and_docs_are_consistent(self):
        self.assertIn('parentID', README)
        self.assertIn('`transform = someModelName`', README)
        self.assertIn('Nested `TRANSFORM { ... }`', README)
        self.assertIn('pre-existing static model variants', README)
        self.assertEqual((ROOT/'README.md').read_bytes(),
                         (ROOT/'GameData/PartSculptor/README.md').read_bytes())
        for name in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT/name).read_text())['VERSION']['BUILD'], 130)
        self.assertIn('AssemblyVersion("0.0.4.130")',
                      (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())

if __name__ == '__main__':
    unittest.main()
