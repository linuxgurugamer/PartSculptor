import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
GIZMO = (ROOT / "PartSculptor/FaceMorphGizmo.cs").read_text()
TOOL = (ROOT / "PartSculptor/EditorStretchTool.cs").read_text()
MAIN = (ROOT / "PartSculptor/PartSculptor.cs").read_text()

class MorphCompatibility466(unittest.TestCase):
    def test_camera_does_not_require_center_visible(self):
        candidate = GIZMO[GIZMO.index('private static bool CameraSees('):
                          GIZMO.index('private void UpdateGeometry()',
                                      GIZMO.index('private static bool CameraSees('))]
        self.assertNotIn('WorldToViewportPoint(world)', candidate)
        self.assertIn('if (editor != null && editor.isActiveAndEnabled) return editor;', candidate)
        self.assertLess(candidate.index('Camera editor ='), candidate.index('Camera main ='))
        self.assertIn('camera.WorldToScreenPoint(position[i])', GIZMO)
        self.assertIn('visible[i] = camera != null && projected.z > 0f;', GIZMO)

    def test_unsupported_selection_has_reason(self):
        self.assertIn('internal string MorphSelectionFailure', MAIN)
        self.assertIn('module.MorphSelectionFailure', TOOL)
        self.assertIn('module == null ?', TOOL)
        self.assertIn('no PartSculptor module', TOOL)
        self.assertIn('selectedModule.FaceMorphFailure', TOOL)
        self.assertIn('ScreenMessages.PostScreenMessage(faceStatus', TOOL)

    def test_camera_hint_and_protected_geometry(self):
        self.assertIn('if (onCamera == 0)', GIZMO)
        self.assertIn('behind the editor camera', GIZMO)
        self.assertIn('FaceMorphSupport(part, model)', MAIN)
        self.assertIn('Robotic / transform-switching geometry unsupported', MAIN)

    def test_versions(self):
        self.assertIn('"BUILD": 137', (ROOT/'PartSculptor.version').read_text())
        self.assertIn('"BUILD": 137', (ROOT/'GameData/PartSculptor/PartSculptor.version').read_text())
        self.assertIn('0.0.4.137', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())

if __name__ == '__main__': unittest.main()
