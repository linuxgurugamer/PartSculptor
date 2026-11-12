"""0.0.4.109 live face matching: direct, bidirectional, nonrecursive."""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAIN = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()
GIZMO = (ROOT / 'PartSculptor/FaceMorphGizmo.cs').read_text()
CORE = (ROOT / 'PartSculptor/FaceAttachmentMatchCore.cs').read_text()
RUNNER = (ROOT / 'Tests/RegressionRunner/Program.cs').read_text()
README = (ROOT / 'README.md').read_text()


class LiveAttachedFaceMorph109(unittest.TestCase):
    def test_shared_toggle_automatically_matches_during_edits(self):
        self.assertIn('if (updateConnectedFaces &&', MAIN)
        self.assertIn('MatchAttachedFacesGlobal || CopyAttachedMorphStepsGlobal', MAIN)
        self.assertIn('if (changedFaces[i] != 0)', MAIN)
        self.assertIn('targets[i].MatchConnectedFaces(changedFaces[i], updated,', MAIN)
        self.assertIn('MorphFacesDiffer(m.GetMorphFace(0), bottom) ? 1 : 0', MAIN)
        self.assertIn('MorphFacesDiffer(m.GetMorphFace(1), top) ? 2 : 0', MAIN)
        self.assertIn('module.SetMorphFaces(b, top);', GIZMO)
        self.assertIn('module.SetMorphFaces(dragBottom, dragTop);', GIZMO)
        self.assertIn('public void ResetFaceMorph()', MAIN)
        self.assertIn('0.0.4.109', README)

    def test_direct_reciprocal_connections_both_directions(self):
        self.assertIn('Part neighbor = joint != null ? joint.attachedPart : null;', MAIN)
        self.assertIn('(neighbor != part.parent && neighbor.parent != part)', MAIN)
        self.assertIn('faceMorph.TryGetEndFace(joint, out sourceFace)', MAIN)
        self.assertIn('(changedFaces & (1 << sourceFace)) == 0', MAIN)
        self.assertIn('neighbor.FindModuleImplementing<ModulePartSculptor>()', MAIN)
        self.assertIn('candidate.attachedPart != owner', MAIN)
        self.assertIn('other.MatchFaceFromNeighbor(this, neighborNode, joint,', MAIN)
        self.assertIn('parentMorph.faceMorph.Corner(parentFace, i,', MAIN)
        self.assertIn('FaceAttachmentMatchCore.TryFit(corners,', MAIN)
        self.assertIn('bool requireMorphedSource)', MAIN)
        self.assertIn('if (requireMorphedSource && !parentMorph.HasFaceMorph)', MAIN)
        self.assertIn('out reason, false)', MAIN)  # Live propagation permits identity reset.

    def test_no_recursive_updates_and_preserved_opposite_face(self):
        self.assertIn('SetMorphFaces(low, high, false, false, requireMorphedSource);', MAIN)
        self.assertIn('bool updateSymmetry = true, Part stationaryConnectedChild = null)', MAIN)
        self.assertIn('if (updateSymmetry)', MAIN)
        self.assertIn('if (childFace == 0) low = matched;', MAIN)
        self.assertIn('else high = matched;', MAIN)
        self.assertIn('if (!MorphFacesDiffer(old, matched))', MAIN)
        self.assertIn('var updated = new HashSet<Part>();', MAIN)
        self.assertIn('updated.Add(neighbor);', MAIN)
        self.assertIn('if (notifyEditor) FireEditorShipModified();', MAIN)
        self.assertIn('bestError <= cornerTolerance;', CORE)

    def test_versions_and_documentation(self):
        for filename in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            self.assertGreaterEqual(json.loads((ROOT / filename).read_text())['VERSION']['BUILD'], 109)
        self.assertIn('AssemblyVersion("0.0.4.130")',
                      (ROOT / 'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertEqual((ROOT / 'README.md').read_bytes(),
                         (ROOT / 'GameData/PartSculptor/README.md').read_bytes())
        self.assertIn('LiveAttachedFaceFitting();', RUNNER)
        self.assertTrue((ROOT / 'Tests/LIVE_ATTACHED_FACE_MORPH_0.0.4.109.md').exists())


if __name__ == '__main__':
    unittest.main()
