"""Source contracts and screen projection reference checks; not Unity runtime tests."""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
UI = (ROOT/'PartSculptor/ProceduralGeometryTool.cs').read_text()
EDITOR = (ROOT/'PartSculptor/EditorStretchTool.cs').read_text()
class ProceduralHandles503(unittest.TestCase):
    def test_handle_click_precedes_part_pick(self):
        self.assertLess(EDITOR.index('ProceduralGeometryTool.UpdateHandles(selectedModule)'), EDITOR.index('Part clicked = PickPart(mouse)'))
        self.assertIn('for (int i = 0; i < HandleCount; ++i)', UI)
    def test_drag_commits_only_on_release(self):
        section = UI.split('private bool UpdateHandlesInternal',1)[1].split('private void DrawRingOutline',1)[0]
        self.assertEqual(section.count('Commit(rings)'),1)
        self.assertLess(section.index('if (!Input.GetMouseButton(0))'),section.index('Commit(rings)'))
        self.assertIn('if (changed) Commit(rings)',section)
        self.assertIn('Input.GetKeyDown(KeyCode.Escape)',section)
        self.assertIn('Input.GetMouseButtonDown(1)',section)
    def test_frozen_projection(self):
        self.assertIn('Vector2.Dot(pointer - dragStart, dragAxis) / dragAxis.sqrMagnitude',UI)
        for axis in [(50,0),(-50,0),(30,40)]:
            delta=0.25
            displacement=(axis[0]*delta,axis[1]*delta)
            self.assertAlmostEqual(sum(a*b for a,b in zip(axis,displacement))/sum(a*a for a in axis),delta)
    def test_bounds_and_alt(self):
        for value in ['0.01501f','0.99f','0.1f, 4f','KeyCode.LeftAlt','KeyCode.RightAlt']:
            self.assertIn(value,UI)
