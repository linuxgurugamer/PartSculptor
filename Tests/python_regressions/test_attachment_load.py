"""Regression guards for the Mk1 pod / 1.5x FL-T800 / engine reload issue."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
CODE = (ROOT / 'PartSculptor/PartSculptor.cs').read_text(encoding='utf-8')
FIX = (ROOT / 'PartSculptor/AttachmentLoadFix.cs').read_text(encoding='utf-8')
MATH = (ROOT / 'PartSculptor/ScaleMath.cs').read_text(encoding='utf-8')
PROJECT = (ROOT / 'PartSculptor/PartSculptor.csproj').read_text(encoding='utf-8')

class TestAttachmentLoad(unittest.TestCase):
    def test_calls_schedule_only_after_initial_scale_is_restored(self):
        start_finished = CODE.split('public override void OnStartFinished(', 1)[1].split('void HideFields()', 1)[0]
        self.assertLess(start_finished.index('Apply(false, false);'), start_finished.index('AttachmentLoadFix.Schedule();'))
        self.assertIn('if ((HasStretchedGeometry || HasFaceMorph) && HighLogic.LoadedSceneIsEditor)', start_finished)
        self.assertIn('AttachmentLoadFix.Poll();', CODE)
        self.assertIn('if (scheduledFrame < 0 || !HighLogic.LoadedSceneIsEditor', FIX)
    def test_does_not_move_on_each_scale_edit(self):
        apply = CODE.split('void Apply(bool moveNeighbours', 1)[1]
        self.assertNotIn('AttachmentLoadFix.Schedule()', apply)
    def test_aligns_stack_only_and_top_down(self):
        self.assertIn('node.attachedPart == parent', FIX)
        self.assertIn('node.attachedPart != child', FIX)
        self.assertIn('RestoreSubtree(p, visited)', FIX)
        self.assertIn('RestoreConnection(child, parent);', FIX)
        self.assertIn('RestoreSubtree(child, visited);', FIX)
        self.assertIn('if (!TryConnection(child, parent, out childNode, out parentNode)) return;', FIX)
        self.assertIn('ScaleMath.AttachmentCorrection(expected.x, actual.x)', FIX)
        self.assertIn('internal static double AttachmentCorrection(', MATH)
    def test_preserves_user_offset_in_save_and_load(self):
        self.assertIn('STRETCHSCALE_STACK_GAP', FIX)
        self.assertIn('owner.transform.InverseTransformVector(worldGap)', FIX)
        self.assertIn('owner.transform.TransformVector(local)', FIX)
        self.assertIn('attachmentFix.Load(node);', CODE)
        self.assertIn('attachmentFix.Save(node, part, initialized);', CODE)
    def test_project_references_helper(self):
        self.assertIn('<Compile Include="AttachmentLoadFix.cs" />', PROJECT)
