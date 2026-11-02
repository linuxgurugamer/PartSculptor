"""Integration guards for face-based geometry, physics and editor UI."""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
CORE = (ROOT/'PartSculptor/FaceMorphCore.cs').read_text()
SUPPORT = (ROOT/'PartSculptor/FaceMorphSupport.cs').read_text()
GIZMO = (ROOT/'PartSculptor/FaceMorphGizmo.cs').read_text()
MAIN = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
TOOL = (ROOT/'PartSculptor/EditorStretchTool.cs').read_text()
PROJECT = (ROOT/'PartSculptor/PartSculptor.csproj').read_text()
RUNNER = (ROOT/'Tests/RegressionRunner/RegressionRunner.csproj').read_text()

class FaceMorph450(unittest.TestCase):
    def test_two_faces_are_saved_on_part(self):
        for name in ('morphBottomX','morphBottomZ','morphBottomTiltX','morphBottomTiltZ',
                     'morphTopX','morphTopZ','morphTopTiltX','morphTopTiltZ'):
            self.assertIn('public float '+name+' =', MAIN)
        self.assertIn('if (HasFaceMorph)', MAIN)
        self.assertIn('faceMorph.Apply(GetMorphFace(0), GetMorphFace(1), false)', MAIN)
    def test_mesh_and_collider_baselines_are_not_mutated(self):
        self.assertIn('r.vertices = native.vertices', SUPPORT)
        self.assertIn('Object.Instantiate(native)', SUPPORT)
        self.assertIn('r.instance.vertices = transformed', SUPPORT)
        self.assertIn('r.collider.sharedMesh = null;', SUPPORT)
        self.assertIn('r.collider.sharedMesh = r.instance;', SUPPORT)
        self.assertIn('r.filter.sharedMesh = r.native;', SUPPORT)
    def test_not_visual_only_on_unsupported_geometry(self):
        self.assertIn('BoxCollider ||', SUPPORT)
        self.assertIn('SphereCollider || c is CapsuleCollider', SUPPORT)
        self.assertIn('AddPrimitive(c, proxyMesh)', SUPPORT)
        self.assertIn('Readable visual mesh and supported collider are required', SUPPORT)
        self.assertIn('Skinned/animated meshes are not supported', SUPPORT)
        self.assertIn('morphFailure', MAIN)
    def test_two_grouped_faces_and_tilt(self):
        self.assertIn('new Transform[8]', GIZMO)
        self.assertIn('corners[f * 4 + i]', GIZMO)
        self.assertIn('dragTilt', GIZMO)
        self.assertIn('Input.GetKey(KeyCode.LeftAlt)', GIZMO)
        self.assertIn('f.Width +=', GIZMO)
        self.assertIn('f.TiltX +=', GIZMO)
        self.assertIn('module.SetMorphFaces(b, top)', GIZMO)
        self.assertIn('FaceMorphMath.Transform', SUPPORT)
    def test_ui_and_editor_lock_lifecycle(self):
        self.assertIn('private void ToggleMorph() { ToggleMode(true); }', TOOL)
        self.assertIn('morphToolButton.onClick.AddListener(ToggleMorph)', TOOL)
        self.assertIn('Input.GetKeyDown(KeyCode.M)', TOOL)
        self.assertIn('FaceMorphGizmo.Hide();', TOOL)
        self.assertIn('FaceMorphGizmo.IsPointerOnHandleOrDragging()', TOOL)
        self.assertIn('FaceMorphGizmo.HideFor(this)', MAIN)
        self.assertIn('FaceMorphGizmo.UpdateFor(this)', MAIN)
        self.assertIn('FaceMorphGizmo.DrawActive(helpRect)', TOOL)
    def test_production_math_tested_in_runner(self):
        self.assertIn('internal static double VolumeFactor', CORE)
        self.assertIn('FaceMorphCore.cs', PROJECT)
        self.assertIn('FaceMorphCore.cs', RUNNER)
        self.assertIn('FaceMorphMath.VolumeFactor', MAIN)
