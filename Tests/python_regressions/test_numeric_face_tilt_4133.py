"""Numeric face-angle editor regression checks (v0.0.4.133)."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
CORE = (ROOT/'PartSculptor/FaceMorphCore.cs').read_text()

class NumericTilt133(unittest.TestCase):
    def test_nonpersisted_controls_and_apply(self):
        self.assertIn('guiName = "Tilt face"', SOURCE)
        self.assertIn('"Bottom", "Top"', SOURCE)
        self.assertIn('guiName = "Tilt X (degrees)"', SOURCE)
        self.assertIn('guiName = "Tilt Z (degrees)"', SOURCE)
        self.assertRegex(SOURCE, r'\[KSPField\(isPersistant = false, guiActiveEditor = true, guiName = "Tilt face"')
        self.assertIn('public void ApplyNumericFaceTilt()', SOURCE)
        self.assertIn('SetMorphFaces(bottom, top);', SOURCE)
        self.assertIn('EditorUndoSupport.RecordBeforeChange();', SOURCE)

    def test_inverse_uses_same_stretch_and_dimensions(self):
        self.assertIn('internal static bool TryTiltFromDegrees', CORE)
        self.assertIn('Math.Tan(degreesX * Math.PI / 180d) * width * scaleX / scaleY', CORE)
        self.assertIn('Math.Tan(degreesZ * Math.PI / 180d) * depth * scaleZ / scaleY', CORE)
        self.assertIn('Math.Abs(x) > maxFaceTilt + 1e-8d', CORE)
        self.assertIn('Math.Abs(z) > maxFaceTilt + 1e-8d', CORE)
        self.assertIn('Settings.MaxFaceTilt, out xTilt, out zTilt', SOURCE)

    def test_staged_inputs_and_face_selection(self):
        self.assertIn('void OnNumericTiltInputChanged', SOURCE)
        self.assertIn('numericTiltPending = true;', SOURCE)
        self.assertIn('void OnNumericTiltFaceChanged', SOURCE)
        self.assertIn('LoadNumericTiltFromFace(true);', SOURCE)
        self.assertIn('if (!numericTiltPending && HighLogic.LoadedSceneIsEditor)', SOURCE)
        self.assertIn('GetFaceTiltDegrees(numericTiltFace == "Bottom" ? 0 : 1)', SOURCE)

    def test_history_preserved(self):
        # Copy/Match propagation and gizmos are still called by the source method.
        self.assertIn('targets[i].MatchConnectedFaces(changedFaces[i], updated,', SOURCE)
        self.assertIn('m.CaptureConnectedFaceSnapshots(changed)', SOURCE)
        self.assertIn('FaceMorphGizmo.Show(this)', SOURCE)
        self.assertIn('numericTiltStatus', SOURCE)

    def test_version_and_manual_test(self):
        for p in [ROOT/'PartSculptor.version', ROOT/'GameData/PartSculptor/PartSculptor.version']:
            self.assertEqual(json.loads(p.read_text())['VERSION']['BUILD'],137)
        self.assertIn('AssemblyVersion("0.0.4.137")', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertTrue((ROOT/'Tests/NUMERIC_FACE_TILT_0.0.4.133.md').exists())

if __name__ == '__main__': unittest.main()
