"""v125: editor-integrated child pose correction for v113 mirrored steps."""
import math
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
PART = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
ROT = (ROOT/'PartSculptor/EditorPartMorphRotation.cs').read_text()
PROJ = (ROOT/'PartSculptor/PartSculptor.csproj').read_text()

# Simplified exact two-T400 signed X tilt in a common X/Y plane. When the
# parent's node turns by a and the child's new local face normal changes by
# -a after auto-rotation, the additional body correction is +a (2a total).
def signed_angle(v):
    return math.atan2(v[0], v[1])
def normal(theta):
    return (math.sin(theta), math.cos(theta))
def required_additional_turn(parent_angle, child_local_angle):
    desired_world_child = parent_angle
    current_world_child = parent_angle + child_local_angle
    return desired_world_child - current_world_child

class EditorChildRotation125(unittest.TestCase):
    def test_follows_both_mirrored_tilts_for_two_t400s(self):
        for angle_degrees in [-25,-12,-2,2,12,25]:
            a=math.radians(angle_degrees)
            child_local=-a
            correction=required_additional_turn(a, child_local)
            self.assertAlmostEqual(math.degrees(correction), angle_degrees)
            self.assertAlmostEqual(math.degrees(a+correction),2*angle_degrees)

    def test_is_after_successful_receiver_morph_only(self):
        start=PART.index('private bool MirrorFaceStepFromNeighbor(')
        finish=PART.index('private static AttachNode FindReciprocalStackNode(',start)
        method=PART[start:finish]
        self.assertIn('SetMorphFaces(bottom, top, false, false, false, keepChildStationary);',method)
        self.assertLess(method.index('SetMorphFaces(bottom, top, false, false, false, keepChildStationary);'),
            method.rindex('bool followed = ApplyMirroredChildEditorRotation(before);'))
        self.assertIn('if (MorphFacesDiffer(GetMorphFace(receiverFace), replacement))',method)
        self.assertIn('Quaternion.FromToRotation(childNow, childDesiredNormal)',method)
        self.assertIn('Quaternion.FromToRotation(parentOld, parentNow)',method)
        self.assertIn('before.JointChild.parent != before.JointParent',method)

    def test_unchanged_local_morph_and_far_face(self):
        self.assertIn('MorphStep.Add(before.NeighborBefore, mirrored)',PART)
        self.assertIn('FaceMorphStepCore.TryMapMirrored(delta,', PART)
        self.assertIn('if (receiverFace == 0) bottom = replacement;',PART)
        self.assertIn('else top = replacement;',PART)
        self.assertIn('bool updateConnectedFaces = true',PART)
        self.assertIn('if (updateConnectedFaces &&',PART)

    def test_user_editor_rotation_integration(self):
        for expected in ['part.transform.Rotate(eulerAngles, space);',
                         'pose.Part.transform.Rotate(axis, angle, Space.World);',
                         'GameEvents.onEditorPartEvent.Fire(ConstructionEventType.PartRotated, part);',
                         'RotateMirrorSymmetryCounterparts(',
                         'RotateRadialSymmetryCounterparts(',
                         'ScaleGizmo.UpdateFor(module);',
                         'FaceMorphGizmo.UpdateFor(module);']:
            self.assertIn(expected,ROT)
        self.assertIn('<Compile Include="EditorPartMorphRotation.cs" />',PROJ)

    def test_no_undo_flood_or_double_symmetry_or_gizmo_recursion(self):
        x=ROT[ROT.index('internal static bool RotateJointSubtree('):ROT.index('private static void Capture(')]
        self.assertNotIn('SetBackup(',x)
        self.assertNotIn('RotateMirrorSymmetryCounterparts(',x)
        self.assertNotIn('RotateRadialSymmetryCounterparts(',x)
        self.assertIn('if (!FaceMorphGizmo.IsDragging)',ROT)
        self.assertIn('EditorUndoSupport.RecordBeforeChange();',ROT)
        self.assertIn('new HashSet<Part>()', ROT)
        self.assertIn('foreach (Pose pose in poses)', ROT)

    def test_version_and_documentation(self):
        import json
        for file in ('PartSculptor.version','GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT/file).read_text())['VERSION']['BUILD'],137)
        self.assertIn('AssemblyVersion("0.0.4.137")',(ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertEqual((ROOT/'README.md').read_bytes(),(ROOT/'GameData/PartSculptor/README.md').read_bytes())
        self.assertIn('## 0.0.4.134', (ROOT/'Changelog.md').read_text())

if __name__=='__main__':unittest.main()
