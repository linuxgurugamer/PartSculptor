"""0.0.4.55: Ctrl+Alt radially scales all four corners as a single face."""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
GIZMO = (ROOT / 'PartSculptor/FaceMorphGizmo.cs').read_text()
CORE = (ROOT / 'PartSculptor/FaceMorphCore.cs').read_text()
README = (ROOT / 'README.md').read_text()

class RadialFaceMorph455(unittest.TestCase):
    def test_mode_is_captured_and_has_priority_over_shift_alt(self):
        self.assertIn('dragRadial = alt && ctrl;', GIZMO)
        self.assertIn('dragLinked = alt && shift && !ctrl;', GIZMO)
        self.assertIn('dragTilt = alt && !shift && !ctrl;', GIZMO)
        self.assertIn('if (dragRadial || dragLockXZ)', GIZMO)
        self.assertIn('else if (dragLinked)', GIZMO)
        self.assertIn('dragRadial ? "Radial resize all corners', GIZMO)
        self.assertIn('Ctrl+Alt = radial resize', GIZMO)
    def test_uniform_factor_and_preserved_face_center(self):
        self.assertIn('FaceMorphCore.RadialScaleFactor(', GIZMO)
        self.assertIn('f.Width *= factor;', GIZMO)
        self.assertIn('f.Depth *= factor;', GIZMO)
        self.assertNotIn('f.OffsetX *= factor;', GIZMO)
        self.assertNotIn('f.OffsetZ *= factor;', GIZMO)
        self.assertIn('radiusX * deltaX + radiusZ * deltaZ', CORE)
        self.assertIn('radiusX * radiusX + radiusZ * radiusZ', CORE)
        self.assertIn('Math.Max(0.25d / initialWidth, 0.25d / initialDepth)', CORE)
        self.assertIn('Math.Min(4d / initialWidth, 4d / initialDepth)', CORE)
    def test_radial_snapping_does_not_change_aspect_ratio(self):
        self.assertIn('radialSnap ? ScaleUserSettings.MorphSnapIncrement : 0d', GIZMO)
        self.assertIn('if (snapIncrement > 0d) delta = SnapDelta(delta, snapIncrement);', CORE)
        self.assertIn('if (dragRadial || dragLockXZ)', GIZMO[GIZMO.index('if (snap)'):])
        self.assertRegex(README, r'Ctrl\s*\+\s*Alt\s*\+\s*Shift')

if __name__ == '__main__': unittest.main()
