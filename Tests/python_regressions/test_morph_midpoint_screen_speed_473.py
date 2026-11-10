"""0.0.4.73: midpoint morph drags follow projected mouse movement."""
import unittest
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
GIZMO = (ROOT/'PartSculptor/FaceMorphGizmo.cs').read_text()
CORE = (ROOT/'PartSculptor/FaceMorphCore.cs').read_text()
RUNNER = (ROOT/'Tests/RegressionRunner/Program.cs').read_text()

class MidpointScreenSpeed473(unittest.TestCase):
    def test_uses_screen_projection_not_world_delta_for_midpoint(self):
        self.assertIn('private bool ResizeMidpointFromScreen(', GIZMO)
        self.assertIn('FaceMorphCore.ProjectedHandleParameterDelta(', GIZMO)
        self.assertIn('camera.WorldToScreenPoint(', GIZMO)
        self.assertIn('edge >= 0 && !dragLinked &&', GIZMO)
        self.assertIn('ResizeMidpointFromScreen(face, edge, mouse, ref f)', GIZMO)

    def test_uses_mouse_down_anchor_and_perspective_correction(self):
        self.assertIn('clickHandleScreen = new Vector2(', GIZMO)
        self.assertIn('Vector2 pointerDelta = mouse - clickMouse;', GIZMO)
        self.assertIn('currentScreen - clickHandleScreen', GIZMO)
        self.assertIn('for (int iteration = 0; iteration < 4; ++iteration)', GIZMO)

    def test_xz_lock_and_shift_snap_preserved(self):
        self.assertIn('bool proportional = dragRadial || dragLockXZ;', GIZMO)
        self.assertIn('value = Mathf.Clamp(1f + FaceMorphMath.SnapDelta(', GIZMO)
        self.assertIn('f.Width *= value;', GIZMO)
        self.assertIn('f.Depth *= value;', GIZMO)
        self.assertIn('else if (edge < 2) f.Width = value;', GIZMO)
        self.assertIn('else f.Depth = value;', GIZMO)

    def test_pure_math_handles_screen_diagonal_and_degenerate(self):
        self.assertIn('internal static double ProjectedHandleParameterDelta(', CORE)
        self.assertIn('squared < 1e-8d', CORE)
        self.assertIn('private static void ProjectedMidpointDrag()', RUNNER)
        self.assertIn('"projected midpoint tracks pixels"', RUNNER)
        self.assertIn('"perpendicular mouse ignored"', RUNNER)

    def test_release(self):
        for path in (ROOT/'PartSculptor.version', ROOT/'GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads(path.read_text())['VERSION']['BUILD'], 106)
        self.assertIn('AssemblyVersion("0.0.4.106")', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())

if __name__ == '__main__': unittest.main()
