"""v0.0.4.113: copy/match remain independent with mirrored incremental steps."""
import json
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
MAIN=(ROOT/'PartSculptor/PartSculptor.cs').read_text()
SETTINGS=(ROOT/'PartSculptor/ScaleSettingsWindow.cs').read_text()
CORE=(ROOT/'PartSculptor/FaceMorphStepCore.cs').read_text()
RUNNER=(ROOT/'Tests/RegressionRunner/Program.cs').read_text()

class CopyAttachedMorphSteps110(unittest.TestCase):
    def test_independent_mode_and_no_double_propagation(self):
        self.assertIn('CopyAttachedMorphStepsGlobal', MAIN)
        self.assertIn('(MatchAttachedFacesGlobal || CopyAttachedMorphStepsGlobal)', MAIN)
        self.assertIn('if (CopyAttachedMorphStepsGlobal)', MAIN)
        self.assertIn('other.MirrorFaceStepFromNeighbor(this, old, out reason)', MAIN)
        self.assertIn('other.MatchFaceFromNeighbor(this, neighborNode, joint,', MAIN)
        self.assertIn('out reason, false)', MAIN)
        self.assertIn('SetMorphFaces(bottom, top, false, false, false, keepChildStationary);', MAIN)
        self.assertIn('updated.Add(neighbor);', MAIN)
        self.assertIn('(changedFaces & (1 << sourceFace)) == 0', MAIN)
    def test_delta_from_previous_not_shape_copy(self):
        self.assertIn('CaptureConnectedFaceSnapshots(changed)', MAIN)
        self.assertIn('SourceBefore = ToMorphStep(GetMorphFace(sourceFace))', MAIN)
        self.assertIn('NeighborBefore = ToMorphStep(other.GetMorphFace(neighborFace))', MAIN)
        self.assertIn('FaceMorphStepCore.TryMapMirrored(', MAIN)
        self.assertIn('if (receiverFace == 0) bottom = replacement;', MAIN)
        self.assertIn('else top = replacement;', MAIN)
        self.assertIn('MorphStep proposed = MorphStep.Add(before.NeighborBefore, mirrored);', MAIN)
        self.assertIn('TryMapMirrored(MorphStep delta', CORE)
        self.assertIn('-mapped.TiltX, -mapped.TiltZ', CORE)
    def test_user_preferences_and_cancel(self):
        self.assertIn('copyAttachedMorphStepsDefault', SETTINGS)
        self.assertIn('copyAttachedMorphStepsDefault = snapshot.CopyAttachedMorphStepsDefault;', SETTINGS)
        self.assertIn('CopyAttachedMorphStepsGlobal = opened.ActiveCopySteps;', SETTINGS)
        self.assertIn('CopyAttachedMorphStepsGlobal = copyStepsValue;', SETTINGS)
        self.assertIn('Mirror incremental morph steps on the same sides of attached faces', SETTINGS)
        self.assertIn('copyAttachedMorphStepsDefault = false', (ROOT/'GameData/PartSculptor/PluginData/Settings.cfg').read_text())
        self.assertIn('public static bool CopyAttachedMorphStepsDefault = false;', (ROOT/'PartSculptor/Settings.cs').read_text())
    def test_version_docs_and_production_math_test(self):
        self.assertIn('CopyAttachedMorphSteps();', RUNNER)
        self.assertIn('FaceMorphStepCore.cs', (ROOT/'Tests/RegressionRunner/RegressionRunner.csproj').read_text())
        self.assertIn('FaceMorphStepCore.cs', (ROOT/'PartSculptor/PartSculptor.csproj').read_text())
        for filename in ['PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version']:
            self.assertEqual(json.loads((ROOT/filename).read_text())['VERSION']['BUILD'], 130)
        self.assertIn('AssemblyVersion("0.0.4.130")', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertEqual((ROOT/'README.md').read_bytes(), (ROOT/'GameData/PartSculptor/README.md').read_bytes())
        self.assertTrue((ROOT/'Tests/COPY_ATTACHED_MORPH_STEPS_0.0.4.110.md').exists())

if __name__=='__main__': unittest.main()
