"""Version 0.0.4.99: editor Stretch modifiers and anchored help panel."""
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / 'PartSculptor'
GIZMO = (SRC / 'ScaleGizmo.cs').read_text()
MODULE = (SRC / 'PartSculptor.cs').read_text()
MATH = (SRC / 'ScaleMath.cs').read_text()
EDITOR = (SRC / 'EditorStretchTool.cs').read_text()
RUNNER = (ROOT / 'Tests/RegressionRunner/Program.cs').read_text()


class StretchXZLockHelp497(unittest.TestCase):
    def test_shift_snap_and_shift_alt_both_use_same_snap(self):
        self.assertIn('Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)', GIZMO)
        self.assertIn('snap ? 0.05d : 0d', GIZMO)
        self.assertIn('Input.GetKey(KeyCode.LeftAlt) ||', GIZMO)
        self.assertIn('Input.GetKey(KeyCode.RightAlt)', GIZMO)

    def test_ctrl_both_sides_and_alt_priority(self):
        self.assertIn('Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)', GIZMO)
        self.assertIn('bool temporaryLockXZ = !temporaryLockProportions &&', GIZMO)
        self.assertIn('temporaryLockProportions, temporaryLockXZ);', GIZMO)
        self.assertIn('if (temporaryLockProportions)', MODULE)
        self.assertLess(MODULE.index('if (temporaryLockProportions)'),
                        MODULE.index('else if (temporaryLockXZ)'))

    def test_xz_math_uses_shared_factor_and_clamps_both(self):
        self.assertIn('Axes GizmoXZAxes(double x, double y, double z,', MATH)
        self.assertIn('double oldValue = axis == 0 ? x : z;', MATH)
        self.assertIn('double factor = requested / oldValue;', MATH)
        self.assertIn('Math.Min(max / x, max / z)', MATH)
        self.assertIn('Math.Max(min / x, min / z)', MATH)
        self.assertIn('return new Axes(x * factor, y, z * factor);', MATH)

    def test_ctrl_with_y_scales_only_y(self):
        self.assertIn('if (axis == 1)', MATH)
        self.assertIn('GizmoAxes(x, y, z, axis, requested, false, min, max);', MATH)
        self.assertIn('ScaleMath.GizmoXZAxes(before.x, before.y, before.z,', MODULE)

    def test_paw_lock_is_preserved_and_ctrl_override_temporary(self):
        self.assertIn('axis, value, lockAxes, Settings.MinScale, Settings.MaxScale)', MODULE)
        self.assertNotIn('lockAxes = temporaryLockXZ;', MODULE)
        self.assertNotIn('lockAxes = temporaryLockProportions;', MODULE)
        self.assertIn('SetGizmoAxis(axis, value, false, false);', MODULE)

    def test_help_anchors_below_tool_strip_like_morph(self):
        self.assertIn('ScaleGizmo.DrawActive(helpRect);', EDITOR)
        self.assertIn('FaceMorphGizmo.DrawActive(helpRect);', EDITOR)
        self.assertIn('FaceMorphGizmo.DrawHelpBox(helpRect,', GIZMO)
        for label in ('Stretch gizmo:', 'Shift = snap (0.05)',
                      'Alt = lock proportions (X/Y/Z)', 'Shift+Alt = proportions + snap',
                      'Ctrl = lock X/Z (Y independent)', 'Shift+Ctrl = X/Z + snap'):
            self.assertIn(label, GIZMO)
        self.assertIn('Lock proportions = ', GIZMO)

    def test_undo_and_cancel_still_work(self):
        self.assertEqual(GIZMO.count('EditorUndoSupport.RecordBeforeChange();'), 1)
        self.assertEqual(GIZMO.count('undoCheckpointTaken = false;'), 1)
        self.assertIn('module.SetGizmoScale(dragStartScale);', GIZMO)
        self.assertIn('Input.GetKeyDown(KeyCode.Escape)', GIZMO)
        self.assertIn('Input.GetMouseButtonDown(1)', GIZMO)

    def test_regression_runner_checks_linked_aspect_limits_and_snap(self):
        for label in ('Ctrl X keeps Y fixed', 'Ctrl Z grows X proportionally',
                      'Ctrl Y changes Y alone', 'Ctrl max clamps linked Z',
                      'Ctrl min clamps linked X', 'Shift+Ctrl preserves X/Z ratio'):
            self.assertIn(label, RUNNER)

    def test_documentation_both_copies_and_version(self):
        a = (ROOT / 'README.md').read_bytes()
        b = (ROOT / 'GameData/PartSculptor/README.md').read_bytes()
        self.assertEqual(a, b)
        for text in (b'**Shift+Alt**', b'**Shift+Ctrl**', b'Hold **Ctrl**',
                     b'help panel below the editor tool buttons'):
            self.assertIn(text, a)
        for name in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT/name).read_text())['VERSION']['BUILD'], 99)
        assembly = (SRC / 'AssemblyVersion.cs').read_text()
        self.assertIn('AssemblyVersion("0.0.4.99")', assembly)
        self.assertIn('AssemblyFileVersion("0.0.4.99")', assembly)


if __name__ == '__main__':
    unittest.main()
