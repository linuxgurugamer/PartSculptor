"""PartSculptor 0.0.4.97: exact user Morph replacement and Stretch key modifiers."""
import hashlib
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CODE = ROOT / 'PartSculptor'

class StretchAltAndMorphReplacement496(unittest.TestCase):
    def test_user_uploaded_morph_file_is_exact(self):
        digest = hashlib.sha256((CODE / 'FaceMorphGizmo.cs').read_bytes()).hexdigest()
        self.assertEqual(digest, '45917f15e4cb0ccef51d0fc5f125efa402d6b8a2308c7d229582970840c5f42a')

    def test_morph_help_panel_calculates_width_per_line(self):
        code = (CODE / 'FaceMorphGizmo.cs').read_text()
        self.assertIn("StringSplitOptions.RemoveEmptyEntries", code)
        self.assertIn('style.wordWrap = false;', code)
        self.assertIn('style.CalcMinMaxWidth(new GUIContent(lines[i])', code)
        self.assertIn('bounds.width = maxLineWidth + style.padding.left + style.padding.right;', code)

    def test_shift_still_snaps_and_alt_is_temporary_on_stretch(self):
        code = (CODE / 'ScaleGizmo.cs').read_text()
        self.assertIn('Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)', code)
        self.assertIn('Input.GetKey(KeyCode.LeftAlt) ||', code)
        self.assertIn('Input.GetKey(KeyCode.RightAlt)', code)
        self.assertIn('snap ? 0.05d : 0d', code)
        self.assertIn('module.SetGizmoAxis(draggingAxis, desired,\n                    temporaryLockProportions, temporaryLockXZ);', code)

    def test_alt_uses_existing_proportional_math_without_changing_paw(self):
        code = (CODE / 'PartSculptor.cs').read_text()
        self.assertIn('if (temporaryLockProportions)', code)
        self.assertIn('axis, value, true, Settings.MinScale, Settings.MaxScale)', code)
        self.assertIn('SetGizmoAxis(axis, value, false, false);', code)
        self.assertNotIn('lockAxes = temporaryLockProportions;', code)
        self.assertIn('ScaleMath.GizmoAxes(', code)

    def test_single_undo_checkpoint_preserved(self):
        code = (CODE / 'ScaleGizmo.cs').read_text()
        self.assertEqual(code.count('EditorUndoSupport.RecordBeforeChange();'), 1)
        self.assertEqual(code.count('undoCheckpointTaken = false;'), 1)
        self.assertIn('module.SetGizmoScale(dragStartScale);', code)

    def test_both_readmes_document_modifiers(self):
        a = (ROOT / 'README.md').read_bytes()
        b = (ROOT / 'GameData/PartSculptor/README.md').read_bytes()
        self.assertEqual(a, b)
        self.assertIn(b'Hold **Alt** to temporarily lock X/Y/Z proportions', a)
        self.assertIn(b'**Shift+Alt**', a)

if __name__ == '__main__':
    unittest.main()
