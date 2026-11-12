"""0.0.4.54: protect face translations, persisted parameters and snap UI."""
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
CORE=(ROOT/'PartSculptor/FaceMorphCore.cs').read_text()
MATH=(ROOT/'PartSculptor/FaceMorphMath.cs').read_text()
GIZMO=(ROOT/'PartSculptor/FaceMorphGizmo.cs').read_text()
MAIN=(ROOT/'PartSculptor/PartSculptor.cs').read_text()
SETTINGS=(ROOT/'PartSculptor/ScaleSettingsWindow.cs').read_text()

class LinkedFaceAndSnap(unittest.TestCase):
    def test_offsets_are_not_resizes_and_are_interpolated(self):
        self.assertIn('double ox = bottomOffsetX + (topOffsetX - bottomOffsetX) * t;', CORE)
        self.assertIn('double oz = bottomOffsetZ + (topOffsetZ - bottomOffsetZ) * t;', CORE)
        self.assertIn('(max.X - min.X) * .5d * ox', CORE)
        self.assertIn('(max.Z - min.Z) * .5d * oz', CORE)
        self.assertIn('p.Y + x * ax + z * az', CORE)
        self.assertIn('OffsetX, OffsetZ;', MATH)
        self.assertIn('Mathf.Abs(face.OffsetX)', MATH)
        self.assertIn('Mathf.Abs(face.OffsetZ)', MATH)
        self.assertIn('f.OffsetX = Mathf.Clamp', MATH)
        self.assertIn('f.OffsetZ = Mathf.Clamp', MATH)
    def test_shift_alt_mode_is_distinct_and_held_at_drag_start(self):
        self.assertIn('dragLinked = alt && shift && !ctrl;', GIZMO)
        self.assertIn('dragTilt = alt && !shift && !ctrl;', GIZMO)
        self.assertIn('if (dragLinked)', GIZMO)
        self.assertIn('f.OffsetX += displacement.x / dragHalfX;', GIZMO)
        self.assertIn('f.OffsetZ += displacement.z / dragHalfZ;', GIZMO)
        self.assertIn('f.Width +=', GIZMO)
        self.assertIn('f.TiltX +=', GIZMO)
        self.assertIn('dragLinked ? "Move entire face', GIZMO)
    def test_snap_preserves_baselines_and_supports_all_choices(self):
        self.assertIn('SnapDelta(double delta, double increment)', CORE)
        self.assertIn('MidpointRounding.AwayFromZero', CORE)
        self.assertIn('float step = ScaleUserSettings.MorphSnapIncrement;', GIZMO)
        self.assertIn('f.OffsetX = original.OffsetX + FaceMorphMath.SnapDelta', GIZMO)
        self.assertIn('f.Width = original.Width + FaceMorphMath.SnapDelta', GIZMO)
        self.assertIn('f.TiltX = original.TiltX + FaceMorphMath.SnapDelta', GIZMO)
        for choice in ['0.01f','0.05f','0.10f','0.25f']:
            self.assertIn(choice, SETTINGS)
        self.assertIn('GUILayout.SelectionGrid(snapSelection', SETTINGS)
        self.assertIn('morphSnapIncrement', SETTINGS)
        self.assertIn('NumberStyles.Float, CultureInfo.InvariantCulture', SETTINGS)
        self.assertIn('"morphSnapIncrement", snapValue.ToString', SETTINGS)
        self.assertIn('File.Replace(temporary, path, null)', SETTINGS)
    def test_offsets_persist_reset_and_survive_symmetry(self):
        for name in ['morphBottomOffsetX','morphBottomOffsetZ','morphTopOffsetX','morphTopOffsetZ']:
            self.assertIn('[KSPField(isPersistant = true)] public float '+name+' = 0f;', MAIN)
            self.assertIn('m.'+name+' =', MAIN)
        self.assertIn('m.faceMorph.Apply(bottom, top, true,', MAIN)
        self.assertIn('new MorphFace(1f, 1f, 0f, 0f)', MAIN)
        self.assertIn('FaceMorphMath.IsIdentity(GetMorphFace', MAIN)
        self.assertIn('"Bottom offset X"', MAIN)
        self.assertIn('"Top offset Z"', MAIN)

if __name__=='__main__': unittest.main()
