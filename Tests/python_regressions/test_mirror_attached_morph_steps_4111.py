"""0.0.4.113: same-side increment with opposite longitudinal tilt."""
import json
import math
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAIN = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
SUPPORT = (ROOT/'PartSculptor/FaceMorphSupport.cs').read_text()
CORE = (ROOT/'PartSculptor/FaceMorphStepCore.cs').read_text()
RUNNER = (ROOT/'Tests/RegressionRunner/Program.cs').read_text()

class MirroredLiveMorph111(unittest.TestCase):
    def test_collects_before_moving_either_part(self):
        before_snapshot = MAIN.index('CaptureConnectedFaceSnapshots(changed)')
        changing_source = MAIN.index('m.morphBottomX = bottom.Width;', before_snapshot)
        self.assertLess(before_snapshot, changing_source)
        self.assertIn('SourceBefore = ToMorphStep(GetMorphFace(sourceFace))', MAIN)
        self.assertIn('NeighborBefore = ToMorphStep(other.GetMorphFace(neighborFace))', MAIN)
        self.assertIn('ToMorphStep(driver.GetMorphFace(driverFace))', MAIN)
        self.assertIn('FaceMorphStepCore.TryMapMirrored(delta,', MAIN)
        self.assertIn('XFromX = Vector3.Dot(rx, sx)', MAIN)

    def test_precise_planar_fit_and_no_double_rotation(self):
        self.assertIn('MorphStep proposed = MorphStep.Add(before.NeighborBefore, mirrored);', MAIN)
        self.assertIn('MorphStep.Valid(proposed, Settings.MaxFaceTilt)', MAIN)
        self.assertIn('Part keepChildStationary = driver.part.parent == part ? driver.part : null;', MAIN)
        self.assertIn('SetMorphFaces(bottom, top, false, false, false, keepChildStationary);', MAIN)
        self.assertIn('node.attachedPart != stationaryConnectedChild', SUPPORT)
        self.assertIn('m == this ? stationaryConnectedChild : null', MAIN)
        self.assertIn('MorphStep.Subtract(', MAIN)
        self.assertIn('if (receiverFace == 0) bottom = replacement;', MAIN)
        self.assertIn('Math.Abs', CORE)

    def test_rotated_correspondence_not_assumed_identical_corner_indices(self):
        self.assertIn('TryMapMirrored(MorphStep delta', CORE)
        self.assertIn('TryMap(delta, recipientXSourceX', CORE)
        self.assertIn('-mapped.TiltX, -mapped.TiltZ', CORE)
        self.assertIn('mapped.Width, mapped.Depth', CORE)
        self.assertIn('mapped.OffsetX, mapped.OffsetZ', CORE)
        self.assertIn('ZFromZ = Vector3.Dot(rz, sz)', MAIN)

    def test_keep_existing_mode_and_version(self):
        self.assertIn('other.MatchFaceFromNeighbor(this, neighborNode, joint,', MAIN)
        self.assertIn('if (CopyAttachedMorphStepsGlobal)', MAIN)
        self.assertIn('MirroredAttachedMorphSteps();', RUNNER)
        self.assertEqual(json.loads((ROOT/'PartSculptor.version').read_text())['VERSION']['BUILD'], 130)
        self.assertEqual(json.loads((ROOT/'GameData/PartSculptor/PartSculptor.version').read_text())['VERSION']['BUILD'], 130)
        self.assertIn('AssemblyVersion("0.0.4.130")', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertEqual((ROOT/'README.md').read_bytes(), (ROOT/'GameData/PartSculptor/README.md').read_bytes())
        self.assertTrue((ROOT/'Tests/MIRRORED_ATTACHED_FACE_STEPS_0.0.4.111.md').exists())

if __name__ == '__main__': unittest.main()
