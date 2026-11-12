"""v119: keep v113 face deltas; second rigid child rotation follows BOTH normals."""
import json
import math
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
SUP = (ROOT/'PartSculptor/FaceMorphSupport.cs').read_text()

# Analytic angular model for the two FL-T400 stack end normals. All angles are
# in the same world-space tilt plane; opposite face normals differ by 180 deg.
def normalized_degrees(d):
    return (d + 180) % 360 - 180

def finish_child_pose(parent_old, parent_new, child_initial, child_body_after_parent,
                      child_face_local_change):
    # Parent's normal change is already applied to child rigid body by KSP.
    child_now = child_initial + child_body_after_parent + child_face_local_change
    wanted = child_initial + normalized_degrees(parent_new-parent_old)
    second_rotation = normalized_degrees(wanted-child_now)
    return child_body_after_parent+second_rotation, second_rotation

class JointDoubleRotation119(unittest.TestCase):
    def test_t400_positive_and_negative_mirrored_tilt(self):
        # The child already rigidly follows the parent's node by +theta.
        # Its independently mirrored face changes its local normal by -theta.
        # The missing rigid correction is another +theta (total +2theta).
        for theta in (1, 5, 12, -1, -5, -12):
            with self.subTest(theta=theta):
                body, extra = finish_child_pose(0, theta, 180, theta, -theta)
                self.assertAlmostEqual(extra, theta)
                self.assertAlmostEqual(body, 2*theta)
                child_face_world = body + 180 - theta
                parent_face_world = theta
                self.assertAlmostEqual(abs(normalized_degrees(child_face_world-parent_face_world)), 180.0)

    def test_child_drives_parent_both_normals_count(self):
        # The source is the child.  Parent morphs after child, so it doesn't
        # rigidly rotate the child via the regular parent node callback.
        # Both new normals must still drive a post-morph joint correction.
        for theta in (8, -8):
            body, extra = finish_child_pose(0, theta, 180, 0, -theta)
            self.assertAlmostEqual(body, 2*theta)
            self.assertAlmostEqual(extra, 2*theta)

    def test_existing_nonflush_joint_angle_is_retained(self):
        # The correction preserves the original relative normal angle, it
        # doesn't force a genuinely offset joint to 180 degrees.
        old_parent, old_child = 17, 201
        new_parent, child_body, child_local = 28, 11, -11
        body, _ = finish_child_pose(old_parent,new_parent,old_child,child_body,child_local)
        self.assertAlmostEqual(normalized_degrees(body+old_child+child_local-new_parent),
                               normalized_degrees(old_child-old_parent))

    def test_119_normal_based_method_replaced_after_both_morphs(self):
        self.assertNotIn('private static bool FollowMirroredJointAfterCopy(', SRC)
        self.assertNotIn('Quaternion parentNormalTurn =', SRC)
        self.assertNotIn('Quaternion secondTurn =', SRC)
        self.assertIn('private static bool SnapMirroredJointWithPartFaceSnap(', SRC)
        self.assertIn('PartFaceSnap.SnapFaces(before.JointParent, before.JointChild,', SRC)
        self.assertLess(SRC.index('CaptureConnectedFaceSnapshots(changed)'),
                        SRC.index('m.morphBottomX = bottom.Width;'))
        self.assertIn('SetMorphFaces(bottom, top, false, false, false, keepChildStationary);', SRC)
        self.assertLess(SRC.index('SetMorphFaces(bottom, top, false, false, false, keepChildStationary);'),
                        SRC.index('bool jointFollowed = SnapMirroredJointWithPartFaceSnap(before);'))

    def test_snaps_post_morph_actual_planes_not_recessed_nodes(self):
        self.assertIn('parent.faceMorph.TryGetWorldFacePlane(parentFace,', SRC)
        self.assertIn('child.faceMorph.TryGetWorldFacePlane(childFace,', SRC)
        self.assertIn('before.JointChild.parent != before.JointParent', SRC)
        self.assertIn('before.ParentNode.attachedPart != before.JointChild', SRC)
        self.assertIn('before.ChildNode.attachedPart != before.JointParent', SRC)
        self.assertIn('part.transform.TransformPoint(Corner(face, 0, bottom, top))', SUP)
        self.assertIn('Vector3.Cross(acrossZ, acrossX)', SUP)
        self.assertIn('outwardNormal = (face == 1 ? normal : -normal).normalized;', SUP)

    def test_copy_morph_and_match_mode_unchanged(self):
        self.assertIn('MorphStep proposed = MorphStep.Add(before.NeighborBefore, mirrored);',SRC)
        self.assertIn('if (receiverFace == 0) bottom = replacement;',SRC)
        self.assertIn('else top = replacement;',SRC)
        self.assertIn('other.MatchFaceFromNeighbor(this, neighborNode, joint,',SRC)
        self.assertIn('if (CopyAttachedMorphStepsGlobal)',SRC)
        self.assertIn('node.attachedPart != stationaryConnectedChild',SUP)
        self.assertIn('FaceMorphStepCore.TryMapMirrored(delta,',SRC)

    def test_versions_and_rebase(self):
        for p in ('PartSculptor.version','GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT/p).read_text())['VERSION']['BUILD'],123)
        self.assertIn('AssemblyVersion("0.0.4.123")', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertIn('Based **directly on 0.0.4.113**', (ROOT/'Changelog.md').read_text())
        self.assertEqual((ROOT/'README.md').read_bytes(),
                         (ROOT/'GameData/PartSculptor/README.md').read_bytes())

if __name__=='__main__':unittest.main()
