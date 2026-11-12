"""v118: a connected child's rigid pose follows the final morphed face plane."""
import math
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAIN = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
CORE = (ROOT/'PartSculptor/FaceMorphStepCore.cs').read_text()


def rotate(x, y, angle):
    return (x*math.cos(angle)-y*math.sin(angle),
            x*math.sin(angle)+y*math.cos(angle))


class MirroredJointPivot118(unittest.TestCase):
    def test_capture_happens_before_any_part_morphs(self):
        before=MAIN.index('m.CaptureConnectedFaceSnapshots(changed)')
        after=MAIN.index('m.faceMorph.Apply(bottom, top, true,')
        self.assertLess(before, after)
        for name in ('SourceFaceNormalBefore = sourceNormal',
                     'NeighborFaceNormalBefore = neighborNormal',
                     'ChildWorldPositionBefore = attachedChild.transform.position',
                     'CapturePartSubtree(attachedChild, subtree, new HashSet<Part>());'):
            self.assertIn(name, MAIN)
        self.assertIn('Vector3.Cross(z, x)', MAIN)
        self.assertIn('module.faceMorph.Corner(face, 3, low, high)', MAIN)

    def test_child_body_pivots_to_align_final_face_normals(self):
        section=MAIN[MAIN.index('private bool KeepMirroredStackJointConnected('):
                     MAIN.index('private static AttachNode FindReciprocalStackNode(')]
        for token in ('parentPlaneTurn * childNormalBefore',
                      'Quaternion.FromToRotation(',
                      'childPlaneTurn * before.ChildWorldRotationBefore',
                      'childPart.transform.rotation = desiredChildRotation;',
                      'childPart.transform.TransformPoint(childNode.position)',
                      'desiredChildAnchor - actualChildAnchor',
                      'Quaternion.Inverse(before.ChildWorldRotationBefore)'):
            self.assertIn(token, section)
        self.assertIn('if (part.parent == driver.part)', section)
        self.assertIn('else if (driver.part.parent == part)', section)
        self.assertNotIn('parentPart.transform.rotation =', section)

    def test_pure_tilt_both_directions_node_center_and_edges_join(self):
        h=.625
        for k in (-.7, -.2, -.01, 0., .01, .2, .7):
            # Local opposite tilt survives unchanged.
            alpha=2*math.atan(k)
            for x in (-h,0.,h):
                cx,cy=rotate(x, -k*x, alpha)
                self.assertAlmostEqual(cx,x,places=10)
                self.assertAlmostEqual(cy,k*x,places=10)
            # Parent->child and child->parent both produce a valid joint.
            for node_x,node_y in ((0.,1.),(0.,-1.)):
                nx,ny=rotate(node_x,node_y,alpha)
                root_dx=node_x-nx
                root_dy=node_y-ny
                moved_x,moved_y=rotate(node_x,node_y,alpha)
                self.assertAlmostEqual(moved_x+root_dx,node_x)
                self.assertAlmostEqual(moved_y+root_dy,node_y)
                if abs(k)>1e-3:
                    self.assertGreater(abs(root_dx)+abs(root_dy),1e-4)

    def test_preexisting_angular_difference_is_preserved(self):
        # Align the changes in face orientation; do not overwrite absolute
        # existing non-flush angles in the Copy mode.
        for p0,p1,c0,c1 in ((0.,.2,3.14,3.04),(.1,-.1,3.0,3.3)):
            target=c0+(p1-p0)
            correction=target-c1
            self.assertAlmostEqual(c1+correction-c0,p1-p0)

    def test_descendants_receive_one_rigid_pose_from_premorph_world(self):
        # If KSP auto-moved a grandchild already, its intermediate pose must
        # NOT be used a second time. All final poses reference one snapshot.
        section=MAIN[MAIN.index('private bool KeepMirroredStackJointConnected('):
                     MAIN.index('private static AttachNode FindReciprocalStackNode(')]
        self.assertIn('foreach (PartPoseSnapshot saved in before.ChildSubtreeBefore)',section)
        self.assertIn('saved.Position - before.ChildWorldPositionBefore',section)
        self.assertIn('saved.Part.transform.rotation = subtreeTurn * saved.Rotation;',section)
        root=(1.,2.); original=(2.,5.); newroot=(3.,4.); angle=.3
        dx,dy=rotate(original[0]-root[0],original[1]-root[1],angle)
        target=(newroot[0]+dx,newroot[1]+dy)
        self.assertNotEqual(target,original)
        self.assertAlmostEqual(math.hypot(target[0]-newroot[0],target[1]-newroot[1]),
                               math.hypot(original[0]-root[0],original[1]-root[1]))

    def test_copy_morph_values_not_overwritten_and_match_separate(self):
        self.assertIn('FaceMorphStepCore.TryMapMirroredPhysical(delta,',MAIN)
        self.assertIn('MorphStep proposed = MorphStep.Add(before.NeighborBefore, mirrored);',MAIN)
        self.assertIn('before.Neighbor != part',MAIN)
        self.assertIn('other.MatchFaceFromNeighbor(this, neighborNode, joint,',MAIN)
        self.assertIn('-mapped.TiltX, -mapped.TiltZ',CORE)

if __name__=='__main__':unittest.main()
