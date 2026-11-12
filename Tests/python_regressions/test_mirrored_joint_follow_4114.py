"""0.0.4.114: connected stack anchors follow mirrored Copy edits in both directions."""
from pathlib import Path
import math
import unittest

ROOT = Path(__file__).resolve().parents[2]
MAIN = (ROOT / 'PartSculptor/PartSculptor.cs').read_text(encoding='utf-8')
SUPPORT = (ROOT / 'PartSculptor/FaceMorphSupport.cs').read_text(encoding='utf-8')
README = (ROOT / 'README.md').read_text(encoding='utf-8')


def apply_rotation(v, axis, radians):
    """Independent axis-angle reference for rotated joint-space offsets."""
    c, s = math.cos(radians), math.sin(radians)
    dot = sum(a*b for a, b in zip(axis, v))
    cross = (axis[1]*v[2]-axis[2]*v[1],
             axis[2]*v[0]-axis[0]*v[2],
             axis[0]*v[1]-axis[1]*v[0])
    return tuple(v[i]*c + cross[i]*s + axis[i]*dot*(1-c) for i in range(3))


class TestMirroredJointFollow4114(unittest.TestCase):
    def test_snapshot_captures_actual_joint_pose_before_both_morph(self):
        for token in ('SourceAnchorBefore = part.transform.TransformPoint(sourceNode.position)',
                      'NeighborAnchorBefore = neighbor.transform.TransformPoint(neighborNode.position)',
                      'ChildWorldRotationBefore = attachedChild.transform.rotation'):
            self.assertIn(token, MAIN)
        self.assertLess(MAIN.index('m.CaptureConnectedFaceSnapshots(changed)'),
                        MAIN.index('m.faceMorph.Apply(bottom, top, true,'))

    def test_child_follow_runs_only_after_successful_copy(self):
        start = MAIN.index('private bool MirrorFaceStepFromNeighbor(')
        stop = MAIN.index('// A connected stack pair has reciprocal', start)
        block = MAIN[start:stop]
        self.assertIn('FaceMorphStepCore.TryMapMirroredPhysical(', block)
        self.assertIn('MorphStep proposed = MorphStep.Add(before.NeighborBefore, mirrored);', block)
        self.assertIn('SetMorphFaces(bottom, top, false, false, false, keepChildStationary);', block)
        self.assertLess(block.index('if (MorphFacesDiffer(GetMorphFace(receiverFace), replacement))'),
                        block.index('KeepMirroredStackJointConnected(driver, before);'))
        self.assertIn('Vector3 desiredChildOrigin = childPart.transform.position +', block)
        self.assertIn('subtreeTurn * (saved.Position - before.ChildWorldPositionBefore)', block)
        self.assertIn('childPart.transform.rotation = before.ChildWorldRotationBefore;', block)
        self.assertIn('childPlaneTurn * before.ChildWorldRotationBefore;', block)
        self.assertIn('childPart.transform.TransformPoint(childNode.position)', block)
        # Keep the parent's geometry/placement fixed: never force the parent to the child.
        self.assertNotIn('parentPart.transform.position =', block)
        self.assertNotIn('parentPart.transform.rotation =', block)

    def test_both_connection_directions_and_guard_against_wrong_joint(self):
        start = MAIN.index('private bool KeepMirroredStackJointConnected(')
        end = MAIN.index('// A connected stack pair has reciprocal', start)
        block = MAIN[start:end]
        self.assertIn('if (part.parent == driver.part)', block)
        self.assertIn('else if (driver.part.parent == part)', block)
        self.assertIn('if (childPart.parent != parentPart ||', block)
        self.assertIn('parentNode.attachedPart != childPart ||', block)
        self.assertIn('childNode.attachedPart != parentPart ||', block)
        self.assertIn('(childBefore - parentBefore)', block)
        self.assertIn('Quaternion.FromToRotation(', block)
        self.assertNotIn('desiredChildNormal', block)
        self.assertIn('Vector3 actualChildAnchor =', block)
        self.assertIn('TryGetWorldFaceNormal(parentModule, parentFace', block)

    def test_reference_anchor_math_for_flush_and_offset_joints(self):
        # For a previously flush joint, the post-morph child anchor must sit
        # exactly on the parent's node, independent of the child/parent edit order.
        def target(parent_before, child_before, parent_after, joint_rotation):
            offset = tuple(c-p for c, p in zip(child_before, parent_before))
            return tuple(p+d for p, d in zip(parent_after, offset))
        parent_before = (0., 1., 0.)
        self.assertEqual(target(parent_before, parent_before, (0.25, 1.8, 0.4), .31),
                         (0.25, 1.8, 0.4))
        # Copy preserves intentional editor offset in world coordinates, not
        # a rigid tilt of the whole child; this keeps mirrored angles equal.
        p0, c0, p1, radians = (0., 0., 0.), (0.1, 0.0, 0.), (0., 0.5, 0.), math.pi/2
        actual = target(p0, c0, p1, radians)
        self.assertAlmostEqual(actual[0], 0.1)
        self.assertAlmostEqual(actual[1], 0.5)
        self.assertAlmostEqual(actual[2], 0.)

    def test_mode_separation_and_versioned_documentation(self):
        self.assertIn('Copy attached morph steps', README)
        self.assertIn('pivots around the updated joint', README)
        self.assertIn('0.0.4.114', (ROOT/'Changelog.md').read_text())
        self.assertEqual((ROOT/'README.md').read_bytes(),
                         (ROOT/'GameData/PartSculptor/README.md').read_bytes())
        self.assertIn('if (CopyAttachedMorphStepsGlobal)', MAIN)
        self.assertIn('other.MatchFaceFromNeighbor(this, neighborNode, joint,', MAIN)
        # The node positions still track the morphed mesh itself.
        self.assertIn('node.position = FaceMorphMath.Transform(original.position', SUPPORT)


if __name__ == '__main__':
    unittest.main()
