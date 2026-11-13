"""Source-level guardrails for node-pivoted attached child movement, v0.0.4.101."""
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
SUPPORT = (ROOT / 'PartSculptor/FaceMorphSupport.cs').read_text(encoding='utf-8')
MAIN = (ROOT / 'PartSculptor/PartSculptor.cs').read_text(encoding='utf-8')


class TestFaceMorphAttachedChildFollow4100(unittest.TestCase):
    def test_remembers_previous_pose_before_new_face_normal(self):
        self.assertIn('Vector3 was = node.position;', SUPPORT)
        self.assertIn('Vector3 previousOrientation = node.orientation;', SUPPORT)
        self.assertIn('node.orientation = FaceMorphMath.TransformNormal(', SUPPORT)
        self.assertIn('previousPosition = was,', SUPPORT)
        self.assertIn('previousOrientation = previousOrientation', SUPPORT)

    def test_parent_connection_anchors_position_without_moving_parent(self):
        self.assertIn('if (node.attachedPart == part.parent)', SUPPORT)
        self.assertIn('selfMove -= node.position - was;', SUPPORT)
        self.assertIn('part.transform.position += part.transform.TransformVector(selfMove);', SUPPORT)
        self.assertNotIn('node.attachedPart.transform.rotation', SUPPORT)

    def test_rotation_tracks_node_normal_in_world_space(self):
        self.assertIn('part.transform.TransformDirection(motion.previousOrientation)', SUPPORT)
        self.assertIn('part.transform.TransformDirection(motion.node.orientation)', SUPPORT)
        self.assertIn('Quaternion.FromToRotation(oldNormal, newNormal)', SUPPORT)
        self.assertIn('child.transform.rotation = turn * previousRotation;', SUPPORT)

    def test_child_pivots_about_attachment_not_part_center(self):
        self.assertIn('part.transform.TransformPoint(motion.previousPosition)', SUPPORT)
        self.assertIn('part.transform.TransformPoint(motion.node.position)', SUPPORT)
        self.assertIn('child.transform.position - beforeAnchor', SUPPORT)
        self.assertIn('child.transform.position = afterAnchor + turn * offsetFromNode;', SUPPORT)

    def test_node_pose_updates_before_any_attached_child_move(self):
        self.assertLess(SUPPORT.index('node.orientation = FaceMorphMath.TransformNormal('),
                        SUPPORT.index('foreach (AttachedChildMotion motion in childMotions)'))
        self.assertLess(SUPPORT.index('part.transform.position += part.transform.TransformVector(selfMove);'),
                        SUPPORT.index('foreach (AttachedChildMotion motion in childMotions)'))
        self.assertIn('if (!moveAttachments) return;', SUPPORT)
        self.assertIn('child.parent != part', SUPPORT)

    def test_morph_editor_active_only_and_symmetry_kept(self):
        self.assertIn('if (!CanUseScaleGizmo) return;', MAIN)
        self.assertIn('foreach (Part counterpart in part.symmetryCounterparts)', MAIN)
        self.assertIn('m.faceMorph.Apply(bottom, top, true,', MAIN)
        self.assertIn('faceMorph.Apply(GetMorphFace(0), GetMorphFace(1), false);', MAIN)

    def test_release_metadata_and_mirrored_readme(self):
        for name in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT / name).read_text())['VERSION']['BUILD'], 137)
        self.assertIn('AssemblyVersion("0.0.4.137")',
                      (ROOT / 'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertEqual((ROOT / 'README.md').read_bytes(),
                         (ROOT / 'GameData/PartSculptor/README.md').read_bytes())
        self.assertIn(b'rotate with those nodes', (ROOT / 'README.md').read_bytes())


if __name__ == '__main__':
    unittest.main()
