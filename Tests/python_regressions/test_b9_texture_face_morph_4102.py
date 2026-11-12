"""Source integration checks for B9 SUBTYPE/TEXTURE and face morph (0.0.4.102).

No B9 or KSP runtime is installed in CI. These verify the accepted and rejected
configuration paths and that material changes do not require morph mesh rebuilding.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
HELPER = (ROOT/'PartSculptor/B9FaceMorphCompatibility.cs').read_text()
MAIN = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
MORPH = (ROOT/'PartSculptor/FaceMorphSupport.cs').read_text()
README = (ROOT/'README.md').read_text()


def allowed_keys(group):
    match = re.search(r'HashSet<string> ' + group + r'\s*= Set\((.*?)\);',
                      HELPER, flags=re.S)
    assert match, group
    return set(re.findall(r'"([^" ]+)"', match.group(1)))


class B9TextureFaceMorph4102(unittest.TestCase):
    def test_supported_texture_fields_exhaustive(self):
        self.assertEqual(allowed_keys('TextureFields'), {
            'texture', 'currentTexture', 'isNormalMap', 'shaderProperty',
            'transform', 'baseTransform'})

    def test_texture_only_as_nested_subtype_node(self):
        nested = HELPER[HELPER.index('foreach (ConfigNode nested in child.nodes)'):]
        self.assertIn('string.Equals(nested.name, "TEXTURE"', nested)
        self.assertIn('AllowsTextureNode(nested, where + " / TEXTURE"', nested)
        self.assertNotIn('"TEXTURE"', allowed_keys('SubtypeFields'))
        self.assertNotIn('"TEXTURE"', allowed_keys('ModuleFields'))

    def test_resources_and_texture_can_coexist_in_a_subtype(self):
        nested = HELPER[HELPER.index('foreach (ConfigNode nested in child.nodes)'):]
        self.assertIn('string.Equals(nested.name, "RESOURCE"', nested)
        self.assertIn('string.Equals(nested.name, "TEXTURE"', nested)
        self.assertGreaterEqual(nested.count('continue;'), 2)
        self.assertIn('contains geometry/unknown node', nested)

    def test_texture_switch_does_not_expose_transform_geometry(self):
        for field in ('transform', 'baseTransform'):
            self.assertIn(field, allowed_keys('TextureFields'))
            self.assertIn('transform', allowed_keys('SubtypeFields'))
            self.assertNotIn('baseTransform', allowed_keys('SubtypeFields'))
        for unsafe in ('TRANSFORM', 'MATERIAL', 'NODE', 'MODULE'):
            self.assertNotIn('string.Equals(nested.name, "'+unsafe+'"', HELPER)
        self.assertIn('TRANSFORM position/rotation/scale modifiers', HELPER)

    def test_missing_texture_or_unknown_fields_are_rejected(self):
        self.assertIn('AllowedValues(textureNode, TextureFields, location, out reason)', HELPER)
        self.assertIn('textureNode.nodes.Count != 0', HELPER)
        self.assertIn('string.IsNullOrWhiteSpace(textureNode.GetValue("texture"))', HELPER)
        self.assertIn('missing a texture path', HELPER)
        self.assertIn('contains unsupported nested nodes', HELPER)

    def test_every_subtype_checked_and_unverified_rejected(self):
        self.assertIn('foreach (ConfigNode child in config.nodes)', HELPER)
        self.assertIn('foreach (ConfigNode nested in child.nodes)', HELPER)
        self.assertIn('no verified morph-safe SUBTYPE nodes', HELPER)
        self.assertIn('moduleID mismatch', HELPER)

    def test_materials_are_not_replaced_by_morph(self):
        self.assertIn('MeshRenderer render = f.GetComponent<MeshRenderer>();', MORPH)
        self.assertIn('if (f != null) f.sharedMesh = r.instance;', MORPH)
        self.assertNotIn('render.material =', MORPH)
        self.assertNotIn('render.sharedMaterial =', MORPH)

    def test_switch_event_reapplies_morph_without_new_dependency(self):
        self.assertIn('void OnB9SubtypeChanged()', MAIN)
        self.assertIn('Apply(false, false, false);', MAIN[MAIN.index('void OnB9SubtypeChanged()'):][:800])
        self.assertIn('B9FaceMorphCompatibility.AllowsFaceMorph(part, other, out reason)', MAIN)
        self.assertNotIn('using B9PartSwitch', HELPER)

    def test_docs_and_version_mirrors(self):
        self.assertIn('B9PartSwitch resource and `TEXTURE` changes are supported', README)
        self.assertIn('Nested `TRANSFORM` blocks', README)
        self.assertEqual((ROOT/'README.md').read_bytes(),
                         (ROOT/'GameData/PartSculptor/README.md').read_bytes())
        for name in ['PartSculptor.version','GameData/PartSculptor/PartSculptor.version']:
            self.assertIn('"BUILD": 130', (ROOT/name).read_text())
        self.assertIn('AssemblyVersion("0.0.4.130")',
                      (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())


if __name__ == '__main__':
    unittest.main()
