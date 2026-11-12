"""0.0.4.112: Copy changes the attached part's far end; Match doesn't."""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAIN = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()
CORE = (ROOT / 'PartSculptor/FaceMorphStepCore.cs').read_text()
RUNNER = (ROOT / 'Tests/RegressionRunner/Program.cs').read_text()

class ThroughPartCopy112(unittest.TestCase):
    def test_snapshot_both_ends_before_any_rotation(self):
        self.assertIn('DriverFaceBefore = GetMorphFace(sourceFace)', MAIN)
        self.assertIn('ReceiverFarBefore = other.GetMorphFace(1 - neighborFace)', MAIN)
        self.assertLess(MAIN.index('CaptureConnectedFaceSnapshots(changed)'),
                        MAIN.index('m.morphBottomX = bottom.Width'))
        self.assertIn('ReceiverXSourceX = Vector3.Dot(dstX, srcX)', MAIN)
        self.assertIn('ReceiverZSourceZ = Vector3.Dot(dstZ, srcZ)', MAIN)
        self.assertIn('ReceiverYSourceY = Vector3.Dot(dstY, srcY)', MAIN)

    def test_differs_from_match_by_updating_far_face(self):
        self.assertIn('FaceMorphStepCore.TryCopyMirroredFarFace(', MAIN)
        self.assertIn('before.ReceiverFarBefore', MAIN)
        self.assertIn('MorphFace farReplacement = FromMorphStep(farStep);', MAIN)
        self.assertIn('bottom = replacement;\n                top = farReplacement;', MAIN)
        self.assertIn('top = replacement;\n                bottom = farReplacement;', MAIN)
        self.assertIn('MorphFacesDiffer(GetMorphFace(1 - receiverFace), farReplacement)', MAIN)
        self.assertIn('SetMorphFaces(bottom, top, false, false, false, keepChildStationary);', MAIN)
        self.assertIn('other.MatchFaceFromNeighbor(this, neighborNode, joint,', MAIN)
        self.assertIn('TryMirrorWorldCorners(before.SourceBefore,', MAIN)

    def test_mirrors_increment_and_not_absolute_geometry(self):
        self.assertIn('MorphStep.Subtract(sourceAfter, sourceBefore)', CORE)
        self.assertIn('MorphStep.Add(destinationBefore, mapped)', CORE)
        self.assertIn('mapped.TiltX = -mapped.TiltX', CORE)
        self.assertIn('mapped.TiltZ = -mapped.TiltZ', CORE)
        self.assertIn('if (!MorphStep.Valid(candidate)) return false;', CORE)
        self.assertIn('CopiedMorphThroughAttachedPart();', RUNNER)
        self.assertIn('Equal(farAfter.Width, 1.75', RUNNER)
        self.assertIn('Equal(cancelled.Width, farBefore.Width', RUNNER)

    def test_version_docs_and_mode_independence(self):
        for filename in ['PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version']:
            self.assertEqual(json.loads((ROOT/filename).read_text())['VERSION']['BUILD'], 112)
        self.assertIn('AssemblyVersion("0.0.4.112")', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertEqual((ROOT/'README.md').read_bytes(), (ROOT/'GameData/PartSculptor/README.md').read_bytes())
        self.assertIn('Copy attached morph steps', (ROOT/'README.md').read_text())
        self.assertIn('opposite end', (ROOT/'README.md').read_text())
        self.assertIn('MatchAttachedFacesGlobal || CopyAttachedMorphStepsGlobal', MAIN)
        self.assertTrue((ROOT/'Tests/COPY_THROUGH_ATTACHED_PART_0.0.4.112.md').exists())

if __name__ == '__main__': unittest.main()
