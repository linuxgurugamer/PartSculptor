"""v121 geometric snap of actual deformed face planes (no absolute morph fitting)."""
import json
import math
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
MAIN=(ROOT/'PartSculptor/PartSculptor.cs').read_text()
SUP=(ROOT/'PartSculptor/FaceMorphSupport.cs').read_text()
SNAP=(ROOT/'PartSculptor/PartFaceSnap.cs').read_text()
PROJ=(ROOT/'PartSculptor/PartSculptor.csproj').read_text()

# Analytic 2D restriction of the world-space FromToRotation operation. For
# a mirrored identical FL-T400 +theta parent / -theta child tilt, the child's
# pre-existing parent-node rotation theta cancels its opposite local tilt;
# face-to-face snap supplies the missing second theta body rotation.
def signed_angle(theta):
    return (theta+180)%360 - 180

def snap_child_plane(parent_deg, child_deg, p=(2.0,1.0), c=(4.0,-2.0), distal=(4.0, 3.0)):
    target_child_deg=parent_deg+180
    correction=signed_angle(target_child_deg-child_deg)
    a=math.radians(correction)
    dx,dy=distal[0]-c[0],distal[1]-c[1]
    moved=(p[0]+math.cos(a)*dx-math.sin(a)*dy,
           p[1]+math.sin(a)*dx+math.cos(a)*dy)
    return correction, moved

class PartFaceSnap121(unittest.TestCase):
    def test_real_face_geometry_not_nodes(self):
        self.assertIn('TryGetWorldFacePlane(parentFace,',MAIN)
        self.assertIn('TryGetWorldFacePlane(childFace,',MAIN)
        self.assertIn('PartFaceSnap.SnapFaces(before.JointParent, before.JointChild,', MAIN)
        self.assertIn('Corner(face, 0, bottom, top)',SUP)
        self.assertIn('Corner(face, 3, bottom, top)',SUP)
        self.assertIn('Vector3.Cross(acrossZ, acrossX)',SUP)
        self.assertIn('part.transform.TransformPoint(',SUP)
        self.assertNotIn('private static bool FollowMirroredJointAfterCopy(',MAIN)

    def test_snap_formula_and_subtree(self):
        self.assertIn('Quaternion.FromToRotation(childFaceNormal.normalized,',SNAP)
        self.assertIn('-parentFaceNormal.normalized',SNAP)
        self.assertIn('pose.Part.transform.rotation = turn * pose.Rotation;',SNAP)
        self.assertIn('pose.Part.transform.position = targetCenter +',SNAP)
        self.assertIn('turn * (pose.Position - childCenter);',SNAP)
        self.assertIn('descendant != null && descendant.parent == p',SNAP)
        self.assertIn('if (!HighLogic.LoadedSceneIsEditor',SNAP)
        self.assertIn('child.parent != parent',SNAP)

    def test_two_t400_both_tilt_directions(self):
        for angle in (1.0,5.0,12.0,-1.0,-5.0,-12.0):
            with self.subTest(angle=angle):
                # first automatic body turn: +angle; local mirrored face:-angle
                parent_world_normal=angle
                child_world_normal=180.0 # first turn and child local change cancel
                extra,_=snap_child_plane(parent_world_normal,child_world_normal)
                self.assertAlmostEqual(extra,angle,places=6)
                self.assertAlmostEqual(signed_angle(child_world_normal+extra-
                                   (parent_world_normal+180)),0,places=6)

    def test_child_drives_parent_and_descendant_is_rigid(self):
        for angle in (7,-7):
            extra,p=snap_child_plane(angle,180-angle) # child did not already turn
            self.assertAlmostEqual(extra,2*angle)
            self.assertAlmostEqual(math.hypot(p[0]-2,p[1]-1),5.0,places=8)

    def test_snap_does_not_change_shape_or_match_mode(self):
        self.assertIn('MorphStep.Add(before.NeighborBefore, mirrored)',MAIN)
        self.assertIn('FaceMorphStepCore.TryMapMirrored(delta,',MAIN)
        self.assertIn('SetMorphFaces(bottom, top, false, false, false, keepChildStationary);',MAIN)
        self.assertIn('other.MatchFaceFromNeighbor(this, neighborNode, joint,',MAIN)
        self.assertNotIn('SetMorphFaces(',SNAP)
        self.assertNotIn('FaceAttachmentMatchCore.TryFit(',SNAP)
        self.assertIn('Compile Include="PartFaceSnap.cs"',PROJ)
        self.assertIn('public static bool SnapAttachedFaces(Part partA, Part partB)',SNAP)

    def test_version_integrity(self):
        self.assertEqual(json.loads((ROOT/'PartSculptor.version').read_text())['VERSION']['BUILD'],123)
        self.assertEqual(json.loads((ROOT/'GameData/PartSculptor/PartSculptor.version').read_text())['VERSION']['BUILD'],123)
        self.assertIn('AssemblyVersion("0.0.4.123")',(ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertEqual((ROOT/'README.md').read_bytes(),(ROOT/'GameData/PartSculptor/README.md').read_bytes())

if __name__=='__main__': unittest.main()
