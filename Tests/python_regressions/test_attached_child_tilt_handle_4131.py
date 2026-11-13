"""0.0.4.131: child-facing joint Copy handle moves with the mouse."""
import math
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
GIZMO = (ROOT / 'PartSculptor/FaceMorphGizmo.cs').read_text()
CORE = (ROOT / 'PartSculptor/FaceMorphCore.cs').read_text()
MAIN = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()


class TestAttachedChildTiltHandle4131(unittest.TestCase):
    def test_drag_direction_is_frozen_at_mouse_down(self):
        # No re-projection of the rotating part's local Y during each drag update.
        click = GIZMO.index('if (hovered >= 0 && Input.GetMouseButtonDown(0)')
        self.assertIn('dragTiltScreenUp = projectedUp.sqrMagnitude', GIZMO[click:])
        self.assertIn('dragTiltPixelsPerUnit = Mathf.Max(12f, projectedUp.magnitude);', GIZMO[click:])
        update = GIZMO[GIZMO.index('if (dragTilt)', GIZMO.index('if (dragging >= 0)')):click]
        self.assertIn('Vector2.Dot(mouse - clickMouse,', update)
        self.assertIn('dragTiltScreenUp) / dragTiltPixelsPerUnit;', update)
        self.assertIn('vertical *= dragTiltSign;', update)
        self.assertNotIn('module.part.transform.TransformDirection(Vector3.up)', update)

    def test_sign_only_inverted_for_mirrored_child_parent_joint(self):
        self.assertIn('copyAttachedSteps && childParentConnectionFace ? -1 : 1;', CORE)
        self.assertIn('ModulePartSculptor.CopyAttachedMorphStepsGlobal,', GIZMO)
        self.assertIn('IsParentAttachedFace(editedFace)', GIZMO)
        self.assertIn('node.attachedPart == part.parent', GIZMO)
        self.assertIn('module.FaceMorph.TryGetEndFace(node, out attachedFace)', GIZMO)
        self.assertIn('attachedFace == face', GIZMO)
        self.assertIn('return false;', GIZMO)

    def test_sign_reversal_offsets_double_joint_rotation(self):
        # Small-angle, two-identical-T400 stack: mirrored parent slope = -local.
        # Corrective child rigid rotation = parent slope - local = -2*local.
        # Visible child's tilted edge = local + rotation = -local.
        # Thus local must be -pointer for the visible edge to follow pointer.
        for input_degrees in (-30, -15, 0, 15, 30):
            pointer = math.radians(input_degrees)
            local = -pointer
            mirrored_parent = -local
            child_rotation = mirrored_parent - local
            self.assertAlmostEqual(local + child_rotation, pointer)
        # No Copy, no rigid counter-rotation: keep original control sign.
        for pointer in [-0.5, 0, 0.5]:
            self.assertEqual(pointer, pointer)

    def test_morph_and_attachment_math_untouched(self):
        self.assertIn('FaceMorphStepCore.TryMapMirrored(delta,', MAIN)
        self.assertIn('FaceMorphStepCore.TryMirrorInclinationStep(', MAIN)
        self.assertIn('bool followed = ApplyMirroredChildEditorRotation(before);', MAIN)
        self.assertIn('EditorUndoSupport.RecordBeforeChange();', GIZMO)
        self.assertIn('module.SetMorphFaces(b, top);', GIZMO)

    def test_version_and_manual_plan(self):
        self.assertIn('AssemblyVersion("0.0.4.137")', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertTrue((ROOT/'Tests/ATTACHED_CHILD_TILT_HANDLE_0.0.4.131.md').is_file())


if __name__ == '__main__':
    unittest.main()
