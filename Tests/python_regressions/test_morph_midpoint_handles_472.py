"""0.0.4.72: selectable edge-midpoint handles on both morph faces."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
GIZMO = (ROOT/'PartSculptor/FaceMorphGizmo.cs').read_text()
CORE = (ROOT/'PartSculptor/FaceMorphCore.cs').read_text()
README = (ROOT/'README.md').read_text()
RUNNER = (ROOT/'Tests/RegressionRunner/Program.cs').read_text()

class MidpointHandles472(unittest.TestCase):
    def test_sixteen_handles_on_two_faces(self):
        self.assertIn('private const int CornerCount = 8;', GIZMO)
        self.assertIn('private const int HandleCount = 16;', GIZMO)
        self.assertIn('new Transform[8]', GIZMO)
        self.assertIn('midpoints = new Transform[8]', GIZMO)
        for item in ('screen', 'position', 'visible'):
            self.assertRegex(GIZMO, rf'{item} = new [a-zA-Z0-9]+\[HandleCount\]')

    def test_midpoint_indices_match_eight_face_edges(self):
        expected = [(0,2),(1,3),(0,1),(2,3),
                    (4,6),(5,7),(4,5),(6,7)]
        block = GIZMO.split('MidpointPairs = {', 1)[1].split('};', 1)[0]
        pairs = [(int(a),int(b)) for a,b in re.findall(r'\{(\d+),(\d+)\}',block)]
        self.assertEqual(expected,pairs)
        self.assertIn('position[CornerCount + i] = (position[MidpointPairs[i, 0]] +', GIZMO)
        self.assertIn('position[MidpointPairs[i, 1]]) * 0.5f;', GIZMO)

    def test_visibility_picking_and_gui_share_all_sixteen_positions(self):
        self.assertIn('for (int i = 0; i < HandleCount; ++i)', GIZMO)
        self.assertIn('handle.gameObject.SetActive(visible[i]);', GIZMO)
        self.assertIn('screen[i] = new Vector2(projected.x, projected.y);', GIZMO)
        self.assertIn('float dist = (screen[i] - mouse).sqrMagnitude;', GIZMO)
        self.assertIn('bool isMidpoint = i >= CornerCount;', GIZMO)
        self.assertIn('GUI.DrawTexture(new Rect(screen[i].x - radius', GIZMO)
        self.assertNotIn('screenOverlay', GIZMO)

    def test_midpoint_drag_uses_axis_specific_resize_and_tilt(self):
        self.assertIn('int edge = dragging < CornerCount ? -1 :', GIZMO)
        self.assertIn('FaceMorphCore.EdgeWidthSign(edge)', GIZMO)
        self.assertIn('FaceMorphCore.EdgeDepthSign(edge)', GIZMO)
        self.assertIn('else if (edge < 2)', GIZMO)
        self.assertIn('f.Width += xSign * displacement.x / dragHalfX;', GIZMO)
        self.assertIn('f.Depth += zSign * displacement.z / dragHalfZ;', GIZMO)
        self.assertIn('f.TiltX += vertical * xSign', GIZMO)
        self.assertIn('f.TiltZ += vertical * zSign', GIZMO)
        self.assertIn('return edge == 0 ? -1 : edge == 1 ? 1 : 0;', CORE)
        self.assertIn('return edge == 2 ? -1 : edge == 3 ? 1 : 0;', CORE)

    def test_original_modifier_modes_remain_available(self):
        for fragment in ('dragRadial = alt && ctrl;',
                         'dragLinked = alt && shift && !ctrl;',
                         'dragTilt = alt && !shift && !ctrl;',
                         'dragLockXZ = !alt && module.lockMorphXZ;',
                         'if (dragRadial || dragLockXZ)',
                         'FaceMorphCore.RadialScaleFactor(',
                         'module.SetMorphFaces(b, top);'):
            self.assertIn(fragment,GIZMO)
        self.assertIn('MorphEdgeHandles();', RUNNER)
        self.assertIn('each midpoint controls exactly one axis', RUNNER)

    def test_docs_and_versions(self):
        self.assertIn('sixteen handles', README)
        self.assertIn('Drag an edge midpoint', README)
        self.assertIn('Alt** + drag an edge midpoint', README)
        self.assertIn('## 0.0.4.72', (ROOT/'Changelog.md').read_text())
        for path in (ROOT/'PartSculptor.version',ROOT/'GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads(path.read_text())['VERSION']['BUILD'],97)
        self.assertIn('AssemblyVersion("0.0.4.97")', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())

if __name__ == '__main__': unittest.main()
