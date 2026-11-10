"""Regression checks for morph mesh independence when parts are Alt-copied (0.0.4.101)."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SUPPORT = (ROOT / 'PartSculptor/FaceMorphSupport.cs').read_text(encoding='utf-8')
MODULE = (ROOT / 'PartSculptor/PartSculptor.cs').read_text(encoding='utf-8')


class MorphCopyIsolation4101(unittest.TestCase):
    def test_copied_components_are_rebound_before_source_can_morph(self):
        self.assertIn('public override void OnWasCopied(PartModule copyPartModule, bool asSymCounterpart)', MODULE)
        self.assertIn('copy.copiedMorphMeshInstances = detached;', MODULE)
        self.assertIn('FaceMorphSupport.DetachCopiedMorphMeshes(copy.part)', MODULE)
        self.assertIn('base.OnWasCopied(copyPartModule, asSymCounterpart);', MODULE)

    def test_both_visible_and_collision_meshes_are_independent(self):
        self.assertIn('foreach (MeshFilter filter in copyModel.GetComponentsInChildren<MeshFilter>(true))', SUPPORT)
        self.assertIn('filter.sharedMesh = IndependentCopy(filter.sharedMesh, copies, owned);', SUPPORT)
        self.assertIn('foreach (MeshCollider collider in copy.GetComponentsInChildren<MeshCollider>(true))', SUPPORT)
        self.assertIn('collider.sharedMesh = IndependentCopy(collider.sharedMesh, copies, owned);', SUPPORT)
        self.assertIn('collider.GetComponentInParent<Part>() != copy', SUPPORT)

    def test_only_custom_morph_meshes_are_cloned(self):
        self.assertIn('filter.GetComponentInParent<Part>() != copy', SUPPORT)
        self.assertIn('!IsMorphInstance(filter.sharedMesh)) continue;', SUPPORT)
        self.assertIn('!IsMorphInstance(collider.sharedMesh)) continue;', SUPPORT)
        self.assertIn('independent = UnityEngine.Object.Instantiate(original);', SUPPORT)
        self.assertIn('copies.TryGetValue(original, out independent)', SUPPORT)

    def test_copy_of_identity_morph_becomes_independent_on_start(self):
        self.assertIn('bool inheritedMorphMeshes = FaceMorphSupport.ContainsMorphMeshes(part);', MODULE)
        self.assertIn('if (HasFaceMorph || inheritedMorphMeshes)', MODULE)
        self.assertIn('faceMorph.Apply(GetMorphFace(0), GetMorphFace(1), false);', MODULE)

    def test_existing_morph_support_is_not_detached_from_its_mesh(self):
        self.assertIn('if (ReferenceEquals(copy.faceMorph, faceMorph)) copy.faceMorph = null;', MODULE)
        self.assertIn('if (copy.faceMorph != null && copy.faceMorph.Valid) return;', MODULE)

    def test_temporary_meshes_are_not_destroyed_while_referenced(self):
        self.assertIn('if (referenced) continue;', SUPPORT)
        self.assertIn('FaceMorphSupport.ReleaseDetachedMorphMeshes(part, copiedMorphMeshInstances);', MODULE)
        self.assertIn('copiedMorphMeshInstances.Clear();', MODULE)

    def test_symmetry_fields_still_propagate_intentionally(self):
        self.assertIn('foreach (Part counterpart in part.symmetryCounterparts)', MODULE)
        self.assertIn('m.faceMorph.Apply(bottom, top, true);', MODULE)
        self.assertIn('Symmetry only shares face VALUES.', MODULE)

    def test_updated_version_and_readmes(self):
        for path in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT / path).read_text())['VERSION']['BUILD'], 106)
        self.assertIn('AssemblyVersion("0.0.4.106")', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertEqual((ROOT/'README.md').read_bytes(), (ROOT/'GameData/PartSculptor/README.md').read_bytes())
        self.assertIn('subsequent Morph changes to the original do not alter an Alt-copied part',
                      (ROOT/'README.md').read_text())

if __name__ == '__main__':
    unittest.main()
