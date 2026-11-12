"""Regression guards for the editor placement lifecycle and bounded face fitting."""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()
WATCHER = (ROOT / 'PartSculptor/ScaleMath.cs').read_text()
CORE = (ROOT / 'PartSculptor/FaceAttachmentMatchCore.cs').read_text()
RUNNER = (ROOT / 'Tests/RegressionRunner/Program.cs').read_text()


class FaceAttachmentReliability108(unittest.TestCase):
    def test_ksp_placement_event_and_late_initializer(self):
        self.assertIn('GameEvents.onEditorPartPlaced.Add(editorShipModifiedListener.OnPartPlaced);', SOURCE)
        self.assertIn('GameEvents.onEditorPartPlaced.Remove(editorShipModifiedListener.OnPartPlaced);', SOURCE)
        self.assertIn('OnAnyEditorPartPlaced(Part placed)', SOURCE)
        self.assertIn('module.nodeAttachWatcher.Placed(placed.parent);', SOURCE)
        self.assertIn('nodeAttachWatcher.Seed(part.parent);', SOURCE)
        self.assertIn('if (!placedEventPending)', WATCHER)
        self.assertIn('internal void Placed(object parent)', WATCHER)
        self.assertIn('if (++attempts >= 90) Complete();', WATCHER)
        self.assertIn('!parentMorph.initialized', SOURCE)
        self.assertIn('"early place survives OnStart seed"', RUNNER)

    def test_closest_fit_is_bounded_and_manual_retry_available(self):
        self.assertIn('bool allowCloseFit = false', CORE)
        self.assertIn('0.075d * targetSpan', CORE)
        self.assertIn('bestError <= cornerTolerance', CORE)
        self.assertIn('out fitted, out error, true, Settings.MaxFaceTilt)', SOURCE)
        self.assertIn('"Match face to parent now"', SOURCE)
        self.assertIn('public void MatchFaceToParentNow()', SOURCE)
        self.assertIn('ScreenMessages.PostScreenMessage("PartSculptor: " + reason', SOURCE)
        self.assertIn('"close fit must allow slight skew caused by tilted nodes"', RUNNER)
        self.assertIn('"approximate match must still reject 45-degree roll"', RUNNER)
        self.assertLess(SOURCE.index('MatchNewStackNodeSize(source, destination);'),
                        SOURCE.index('MatchNewStackFace(source, destination);'))

    def test_versions_and_documentation(self):
        for path in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT / path).read_text())['VERSION']['BUILD'], 130)
        self.assertIn('AssemblyVersion("0.0.4.130")',
                      (ROOT / 'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertEqual((ROOT / 'README.md').read_bytes(),
                         (ROOT / 'GameData/PartSculptor/README.md').read_bytes())
        self.assertTrue((ROOT / 'Tests/MATCH_ATTACHED_FACES_0.0.4.108.md').is_file())


if __name__ == '__main__':
    unittest.main()
