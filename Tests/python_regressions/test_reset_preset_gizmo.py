"""Static checks for reset-selector sync, gizmo reach, and cursor tracking."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]

class TestResetPresetGizmo(unittest.TestCase):
    def test_reset_updates_preset_after_committing(self):
        source = (ROOT / 'PartSculptor/PartSculptor.cs').read_text(encoding='utf-8')
        body = source.split('public void ResetScale()', 1)[1].split('public void SwapXZ()', 1)[0]
        self.assertIn('scaleX = scaleY = scaleZ = 1f;', body)
        self.assertLess(body.index('CommitChange(before, null);'), body.index('scalePreset = "1x uniform";'))
        self.assertIn('RefreshPresetOptions();', body)
        self.assertIn('module.scalePreset = "1x uniform";', body)

    def test_gizmo_reach_extension(self):
        source = (ROOT / 'PartSculptor/ScaleGizmo.cs').read_text(encoding='utf-8')
        self.assertIn('Mathf.Max(handleSize * 2f, 0.25f) * 3f;', source)
        self.assertIn('Vector3 end = center + direction * (radius + stemExtra);', source)
        self.assertIn('ScaleMath.GizmoFollowValue', source)
        self.assertIn('dragClickOffset = mouse - screenHandles[draggingAxis];', source)
        self.assertIn('UpdateGeometry();', source)
        self.assertIn('projected / scaleStep', source)
        self.assertNotIn('pixels * 0.85f', source)
        self.assertNotIn('nextApplyTime', source)

if __name__ == '__main__':
    unittest.main()
