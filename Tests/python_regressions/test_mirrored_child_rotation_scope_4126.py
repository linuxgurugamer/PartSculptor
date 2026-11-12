"""v126 regression: avoid C# CS0136 duplicate local names in nested/outer scopes."""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
S = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()

class MirroredChildRotationScope126(unittest.TestCase):
    def test_branch_local_uses_distinct_name(self):
        start = S.index('private bool MirrorFaceStepFromNeighbor(')
        end = S.index('private static bool ApplyMirroredChildEditorRotation(', start)
        method = S[start:end]
        self.assertIn('bool existingJointUpdated = ApplyMirroredChildEditorRotation(before);', method)
        self.assertIn('status = existingJointUpdated ?', method)
        self.assertEqual(method.count('bool followed = ApplyMirroredChildEditorRotation(before);'), 1)
        self.assertIn('(followed ? "; child body rotated to follow the joint"', method)
