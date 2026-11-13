"""0.0.4.109: matching freshly stack-attached end faces without mutating loaded craft."""
import json
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
MAIN = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
SUPPORT = (ROOT/'PartSculptor/FaceMorphSupport.cs').read_text()
CORE = (ROOT/'PartSculptor/FaceAttachmentMatchCore.cs').read_text()
SETTINGS = (ROOT/'PartSculptor/ScaleSettingsWindow.cs').read_text()

class TestMatchAttachedFaces107(unittest.TestCase):
    def test_new_connection_only_and_independent_toggles(self):
        self.assertIn('nodeAttachWatcher.Seed(part.parent);', MAIN)
        self.assertIn('nodeAttachWatcher.Observe(part.parent)', MAIN)
        self.assertIn('if (MatchNodeSizesGlobal)\n                MatchNewStackNodeSize(source, destination);', MAIN)
        self.assertIn('if (MatchAttachedFacesGlobal)\n                MatchNewStackFace(source, destination);', MAIN)
        self.assertLess(MAIN.index('MatchNewStackNodeSize(source, destination);'),
                        MAIN.index('MatchNewStackFace(source, destination);'))
        self.assertIn('nodeAttachWatcher.Complete(); // Never repeatedly reshape this join.', MAIN)
        self.assertIn('if (source == null) return false; // Skip surface-attached parts.', MAIN)

    def test_world_corner_geometry_is_matched_to_new_child_only(self):
        self.assertIn('parentMorph.faceMorph.Corner(parentFace, i,', MAIN)
        self.assertIn('parentMorph.part.transform.TransformPoint(parentCorner)', MAIN)
        self.assertIn('part.transform.InverseTransformPoint(world) - source.position;', MAIN)
        self.assertIn('!parentMorph.HasFaceMorph', MAIN)
        self.assertIn('faceMorph.TryGetEndFace(source, out childFace)', MAIN)
        self.assertIn('FaceAttachmentMatchCore.TryFit(corners,', MAIN)
        self.assertIn('SetMorphFaces(low, high, false, false, requireMorphedSource);', MAIN)
        self.assertIn('if (childFace == 0) low = matched;', MAIN)
        self.assertIn('if (node.attachedPart == part.parent)', SUPPORT)

    def test_safe_fitter_rejects_skew_and_out_of_range(self):
        self.assertIn('for (int a = 0; a < 4; ++a)', CORE)
        self.assertIn('width < 0.25d || width > 4d', CORE)
        self.assertIn('Math.Abs(tiltX) > maxFaceTilt', CORE)
        self.assertIn('bestError <= cornerTolerance;', CORE)
        self.assertIn('nativeNode.Y - faceY', CORE)
        self.assertIn('actual.X - anchor.X', CORE)
        self.assertIn('FaceAttachmentMatchCore.cs"', (ROOT/'PartSculptor/PartSculptor.csproj').read_text())
        self.assertIn('FaceAttachmentMatchCore.cs"', (ROOT/'Tests/RegressionRunner/RegressionRunner.csproj').read_text())
        self.assertIn('AttachedFaceFitting();', (ROOT/'Tests/RegressionRunner/Program.cs').read_text())

    def test_default_persistence_and_cancel(self):
        self.assertIn('public bool matchAttachedFaces = false;', MAIN)
        self.assertIn('public static bool MatchAttachedFacesGlobal;', MAIN)
        self.assertIn('matchAttachedFacesDefault = false',
                      (ROOT/'GameData/PartSculptor/PluginData/Settings.cfg').read_text())
        self.assertIn('node.AddValue("matchAttachedFacesDefault", faceMatchValue);', SETTINGS)
        self.assertIn('opened.ActiveMatchFaces = ModulePartSculptor.MatchAttachedFacesGlobal;', SETTINGS)
        self.assertIn('ModulePartSculptor.MatchAttachedFacesGlobal = opened.ActiveMatchFaces;', SETTINGS)
        self.assertIn('MatchAttachedFacesDefault = matchAttachedFacesDefault,', SETTINGS)

    def test_version_and_documentation(self):
        for name in ('PartSculptor.version','GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT/name).read_text())['VERSION']['BUILD'],137)
        self.assertEqual((ROOT/'README.md').read_bytes(),
                         (ROOT/'GameData/PartSculptor/README.md').read_bytes())
        self.assertIn('Match attached faces', (ROOT/'README.md').read_text())
        self.assertTrue((ROOT/'Tests/MATCH_ATTACHED_FACES_0.0.4.108.md').exists())

if __name__ == '__main__': unittest.main()
