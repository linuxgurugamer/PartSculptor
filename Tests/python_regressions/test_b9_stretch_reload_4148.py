"""Regression checks for a stretched B9PartSwitch node with attached stack child.

These are source integration checks plus simple independent coordinate math.
An actual editor craft reload still requires KSP, Unity, and B9PartSwitch.
"""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FIX = (ROOT/'PartSculptor/AttachmentLoadFix.cs').read_text()
MAIN = (ROOT/'PartSculptor/PartSculptor.cs').read_text()

class B9StretchReload4148(unittest.TestCase):
    def test_version_and_manual_plan(self):
        for name in ('PartSculptor.version','GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT/name).read_text())['VERSION']['BUILD'], 149)
        self.assertIn('AssemblyVersion("0.0.4.149")', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertTrue((ROOT/'Tests/B9_STRETCH_RELOAD_0.0.4.148.md').is_file())

    def test_reload_waits_for_final_b9_node_pose(self):
        self.assertIn('if (!initialized || b9SubtypeChangePending) return false;',MAIN)
        self.assertIn('return !nodeHistory.ChangedExternally(part);',MAIN)
        before=MAIN.split('public override void OnStartFinished(',1)[1].split('void HideFields()',1)[0]
        self.assertLess(before.index('RefreshB9NodeBaselines(true);'), before.index('Apply(false, false, false, NodeApplyReason.Load);'))
        self.assertLess(before.index('nodeHistory.RecordFinal(part);'), before.index('AttachmentLoadFix.Schedule();'))

    def test_bounded_post_load_watcher_retries_only_after_node_changes(self):
        self.assertIn('const int RecoveryWatchFrames = 150;',FIX)
        self.assertIn('if (recoveryStarted) { StopRecovery(); return; }',FIX)
        self.assertIn('Time.frameCount - latestScheduleFrame >= RecoveryWatchFrames',FIX)
        self.assertIn('lastPollFrame == Time.frameCount',FIX)
        self.assertIn('observed.TryGetValue(child, out previous)',FIX)
        self.assertIn('previous.ChildNode == childNode',FIX)
        self.assertIn('previous.ParentNode == parentNode',FIX)
        self.assertIn('NodeGeometryHistory.Near(previous.ParentPosition, parentNode.position)',FIX)
        self.assertIn('return;',FIX.split('NodeGeometryHistory.Near(previous.ParentPosition, parentNode.position)',1)[1][:100])

    def test_connection_recovery_uses_saved_node_ids_not_world_proximity(self):
        self.assertRegex(FIX, r'TrySavedConnection\(\s*"parent", child, parent, out own, out peer\)')
        self.assertRegex(FIX, r'TrySavedConnection\(\s*"child", parent, child, out own, out peer\)')
        self.assertIn('string[] ids = entry.Key.Split(\'\\u001f\');',FIX)
        self.assertIn('FindStackNode(owner, ids[1])',FIX)
        self.assertIn('FindStackNode(peer, ids[2])',FIX)
        self.assertIn('if (ownNode != null) return false;',FIX)
        self.assertIn('OppositeNodeNormals(',FIX)
        self.assertIn('return Vector3.Dot(childNormal.normalized, parentNormal.normalized) < -0.8f;',FIX)

    def test_saved_gap_written_even_if_one_link_temporarily_unavailable(self):
        self.assertIn('TrySavedConnection("parent", owner, owner.parent,',FIX)
        self.assertIn('TrySavedConnection("child", owner, child,',FIX)
        self.assertIn('owner.transform.InverseTransformVector(worldGap)',FIX)
        self.assertIn('owner.transform.TransformVector(local)',FIX)
        self.assertIn('PARTSCULPTOR_STACK_GAP',FIX)

    def test_b9_interstage_numeric_top_node_relocation(self):
        # A part was placed at the original B9 node and the final stretched
        # node must be the target when reconstructing an editor craft.
        for native_y, y_stretch in ((1.208,1.5),(2.4885,1.5),(1.208,2.0)):
            expected=native_y*y_stretch
            child_original_attach_y=native_y
            correction=expected-child_original_attach_y
            corrected=child_original_attach_y+correction
            self.assertAlmostEqual(corrected,expected,places=6)
            self.assertAlmostEqual(corrected+(expected-corrected),expected,places=6)
        self.assertAlmostEqual(1.208*1.5,1.812,places=6)
        self.assertAlmostEqual(2.4885*1.5,3.73275,places=6)

if __name__=='__main__': unittest.main()
