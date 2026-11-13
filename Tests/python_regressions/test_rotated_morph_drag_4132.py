"""Face Morph drag uses the actual rotated pose at mouse-down (v132)."""
import json
from pathlib import Path
import math
import unittest

ROOT = Path(__file__).resolve().parents[2]
GIZMO = (ROOT/'PartSculptor/FaceMorphGizmo.cs').read_text()

class RotatedMorphDrag132(unittest.TestCase):
    def test_start_pose_frozen_current_not_initial_part_orientation(self):
        click = GIZMO[GIZMO.index('if (hovered >= 0 && Input.GetMouseButtonDown(0)'):]
        self.assertIn('dragLocalToWorld = module.part.transform.localToWorldMatrix;', click)
        self.assertIn('dragWorldToLocal = module.part.transform.worldToLocalMatrix;', click)
        self.assertIn('dragCamera = camera;', click)
        self.assertIn('dragLocalToWorld.MultiplyVector(Vector3.up)', click)
        self.assertIn('dragPlane = new Plane(planeNormal.normalized, clickWorld)', click)

    def test_all_drag_projections_use_frozen_pose(self):
        self.assertIn('dragLocalToWorld.MultiplyPoint3x4(local)', GIZMO)
        self.assertIn('dragCamera.WorldToScreenPoint(', GIZMO)
        self.assertIn('return ProjectAtDragStart(local);', GIZMO)
        self.assertIn('dragWorldToLocal.MultiplyVector(', GIZMO)
        self.assertIn('Ray ray = dragCamera.ScreenPointToRay(mouse);', GIZMO)
        self.assertNotIn('module.part.transform.InverseTransformVector(\n                        ray.GetPoint(distance) - clickWorld)', GIZMO)

    def test_tilt_probe_matches_selected_handle_and_current_pose(self):
        self.assertIn('private Vector2 ProjectTiltMotion(int handleIndex)', GIZMO)
        self.assertIn('Vector2 projectedUp = ProjectTiltMotion(hovered);', GIZMO)
        self.assertIn('LocalHandlePosition(handleIndex, b, t)', GIZMO)
        self.assertIn('LocalHandlePosition(handleIndex, dragBottom, dragTop)', GIZMO)
        self.assertIn('vertical *= dragTiltSign;', GIZMO)
        self.assertIn('IsParentAttachedFace(editedFace)', GIZMO)
        # A true 50-degree pose changes the screen tangent; the mathematical
        # projection tracks a rotated basis instead of the original vertical.
        for a in (0, 30, 50, -40, 85):
            radians = math.radians(a)
            axis = (-math.sin(radians), math.cos(radians))
            mouse = (axis[0]*20, axis[1]*20)
            vertical = mouse[0]*axis[0]+mouse[1]*axis[1]
            self.assertAlmostEqual(vertical, 20, places=6)

    def test_visual_position_still_follows_live_pose(self):
        self.assertIn('position[i] = t.TransformPoint(morph.Corner(', GIZMO)
        self.assertIn('handle.position = position[i];', GIZMO)
        self.assertIn('UpdateGeometry();', GIZMO)
        self.assertIn('EditorUndoSupport.RecordBeforeChange();', GIZMO)

    def test_version(self):
        for p in (ROOT/'PartSculptor.version',ROOT/'GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads(p.read_text())['VERSION']['BUILD'],137)
        self.assertIn('AssemblyVersion("0.0.4.137")',(ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertTrue((ROOT/'Tests/ROTATED_MORPH_DRAG_0.0.4.132.md').exists())

if __name__=='__main__':unittest.main()
