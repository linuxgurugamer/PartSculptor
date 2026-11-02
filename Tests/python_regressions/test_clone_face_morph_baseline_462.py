"""0.0.4.62: editor clone must not deform its copied private mesh twice."""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
SUPPORT = (ROOT / 'PartSculptor/FaceMorphSupport.cs').read_text()
MAIN = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()
RUNNER = (ROOT / 'Tests/RegressionRunner/Program.cs').read_text()

class FaceMorphCloneBaseline462(unittest.TestCase):
    def test_copied_visual_mesh_uses_native_prefab_source(self):
        self.assertIn('private const string MorphMeshPrefix', SUPPORT)
        self.assertIn('IsMorphInstance(f.sharedMesh) ? PrefabMesh(f) : f.sharedMesh', SUPPORT)
        self.assertIn('Add(f, null, native);', SUPPORT)
        self.assertIn('Readable native visual mesh is unavailable', SUPPORT)
        self.assertIn('if (max.y - min.y < 0.01f', SUPPORT)
    def test_copied_collision_mesh_uses_native_prefab_source(self):
        self.assertIn('IsMorphInstance(c.sharedMesh) ? PrefabMesh(c) : c.sharedMesh', SUPPORT)
        self.assertIn('Add(null, c, native);', SUPPORT)
        self.assertIn('Readable native collider mesh is unavailable', SUPPORT)
    def test_copied_primitive_proxy_restores_native_colliders(self):
        self.assertIn('RestoreCopiedPrimitiveColliders();', SUPPORT)
        self.assertIn('primitive.enabled = native.enabled;', SUPPORT)
        self.assertIn('proxy.enabled = false;', SUPPORT)
        self.assertIn('UnityEngine.Object.Destroy(proxy);', SUPPORT)
        self.assertIn('IsPrimitiveMorphProxy(c)) continue;', SUPPORT)
    def test_lookup_handles_duplicate_named_model_nodes(self):
        self.assertIn('private Transform PrefabTransform(Transform live)', SUPPORT)
        self.assertIn('child.GetSiblingIndex()', SUPPORT)
        self.assertIn('steps[i].Value', SUPPORT)
    def test_saved_morph_reapplied_once_over_clean_nodes(self):
        self.assertIn('node.orientation = prefabNodeOrientation[node.id];', MAIN)
        self.assertIn('faceMorph.Apply(GetMorphFace(0), GetMorphFace(1), false);', MAIN)
        self.assertIn('clone face normal aligned with native mesh', RUNNER)

if __name__ == '__main__':
    unittest.main()
