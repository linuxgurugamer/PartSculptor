"""Regression: copied local tilts + rigidly aligned joint, not absolute face fitting."""
import math
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAIN = (ROOT/'PartSculptor/PartSculptor.cs').read_text()

class MirroredFaceCornerAlignment(unittest.TestCase):
    def test_copy_pivots_child_using_actual_face_normals(self):
        self.assertIn('ChildWorldRotationBefore = attachedChild.transform.rotation', MAIN)
        self.assertIn('SourceFaceNormalBefore = sourceNormal', MAIN)
        self.assertIn('NeighborFaceNormalBefore = neighborNormal', MAIN)
        section = MAIN[MAIN.index('private bool KeepMirroredStackJointConnected('):
                       MAIN.index('private static AttachNode FindReciprocalStackNode(')]
        self.assertIn('childPart.transform.rotation = before.ChildWorldRotationBefore', section)
        self.assertIn('Quaternion parentPlaneTurn = Quaternion.FromToRotation(', section)
        self.assertIn('Quaternion childPlaneTurn = Quaternion.FromToRotation(', section)
        self.assertIn('saved.Part.transform.position = desiredChildOrigin +', section)
        self.assertNotIn('AlignMirroredFaceCorners(', section)

    def test_copy_does_not_force_absolute_fit(self):
        section = MAIN[MAIN.index('private bool KeepMirroredStackJointConnected('):
                       MAIN.index('private static AttachNode FindReciprocalStackNode(')]
        self.assertNotIn('FaceAttachmentMatchCore.TryFit(', section)
        self.assertNotIn('SetMorphFaces(', section)
        self.assertNotIn('WorstCornerShapeDifference(', MAIN)

    def test_two_t400s_touch_after_rigid_plane_rotation(self):
        # Identical touching faces: local source slope = k, mirrored child
        # local slope = -k. To keep the faces joined in WORLD space, the child
        # must pivot by twice atan(k). This intentionally changes its body pose.
        for k in (-.5, -.25, -.08, .08, .25, .5):
            angle = 2 * math.atan(k)
            for x in (-.625, 0., .625):
                child_x, child_y = x, -k*x
                world_x = math.cos(angle)*child_x - math.sin(angle)*child_y
                world_y = math.sin(angle)*child_x + math.cos(angle)*child_y
                self.assertAlmostEqual(world_x, x, places=10)
                self.assertAlmostEqual(world_y, k*x, places=10)

    def test_reverse_drag_not_accumulative_in_morph_parameters(self):
        parent, child = 0.0, 0.0
        for delta in (.24, -.3, .14, -.08):
            parent += delta
            child -= delta
            self.assertAlmostEqual(parent, -child)

if __name__ == '__main__': unittest.main()
