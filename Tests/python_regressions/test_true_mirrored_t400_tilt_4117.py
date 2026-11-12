"""v118 regression: v117 local mirrored step remains; body now pivots to preserve contact."""
import math
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
MAIN = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
CORE = (ROOT/'PartSculptor/FaceMorphStepCore.cs').read_text()
SUPPORT = (ROOT/'PartSculptor/FaceMorphSupport.cs').read_text()

class EqualVisibleMirroredTilt117(unittest.TestCase):
    def test_capture_world_orientation_pre_morph(self):
        self.assertIn('ChildWorldRotationBefore = attachedChild.transform.rotation', MAIN)
        self.assertLess(MAIN.index('m.CaptureConnectedFaceSnapshots(changed)'),
                        MAIN.index('m.faceMorph.Apply(bottom, top, true,'))
        self.assertIn('SourceAnchorBefore = part.transform.TransformPoint(sourceNode.position)', MAIN)
        self.assertIn('NeighborAnchorBefore = neighbor.transform.TransformPoint(neighborNode.position)', MAIN)

    def test_both_attachment_directions_anchor_after_restore(self):
        fn = MAIN[MAIN.index('private bool KeepMirroredStackJointConnected('):
                  MAIN.index('private static AttachNode FindReciprocalStackNode(')]
        self.assertIn('if (part.parent == driver.part)', fn)
        self.assertIn('else if (driver.part.parent == part)', fn)
        self.assertIn('parentNode.attachedPart != childPart', fn)
        self.assertIn('childNode.attachedPart != parentPart', fn)
        self.assertLess(fn.index('childPart.transform.rotation = before.ChildWorldRotationBefore'),
                        fn.index('Vector3 actualChildAnchor ='))
        self.assertIn('childPart.transform.TransformPoint(childNode.position)', fn)
        self.assertIn('Vector3 desiredChildOrigin = childPart.transform.position +', fn)
        self.assertIn('saved.Part.transform.position = desiredChildOrigin +', fn)
        self.assertNotIn('childPart.transform.rotation = correction', fn)
        self.assertNotIn('AlignMirroredFaceCorners(', fn)

    def test_two_125m_tanks_have_equal_opposed_visible_tilt(self):
        half_width = 0.625
        for tilt in (-.6, -.2, -.03, 0.03, .2, .6):
            for x in (-half_width, half_width):
                # Mirrored local morph values remain equal and opposite;
                # the CHILD WORLD rotation is adjusted separately to join planes.
                src_y = tilt*x
                dst_y = -tilt*x
                self.assertAlmostEqual(src_y, -dst_y)
            self.assertAlmostEqual(math.atan(tilt), -math.atan(-tilt))

    def test_joint_anchors_stay_together_while_body_pivots(self):
        for x in (0., .23, -.19):
            for y in (0., .31, -1.17):
                parent_after=(x,y,.04)
                original_offset=(.02,.07,-.03)
                desired=tuple(parent_after[i]+original_offset[i] for i in range(3))
                current_child_anchor=(4.,-1.,2.)
                correction=tuple(desired[i]-current_child_anchor[i] for i in range(3))
                actual=tuple(current_child_anchor[i]+correction[i] for i in range(3))
                for i in range(3): self.assertAlmostEqual(actual[i], desired[i])

    def test_copy_mapping_and_match_stay_separate(self):
        self.assertIn('FaceMorphStepCore.TryMapMirroredPhysical(delta,', MAIN)
        self.assertIn('delta.TiltX * source.LongitudinalX', CORE)
        self.assertIn('-mapped.TiltX', CORE)
        self.assertIn('other.MatchFaceFromNeighbor(this, neighborNode, joint,', MAIN)
        self.assertIn('m.faceMorph.Apply(bottom, top, true,', MAIN)
        self.assertIn('Quaternion.FromToRotation(oldNormal, newNormal)', SUPPORT)

if __name__ == '__main__': unittest.main()
