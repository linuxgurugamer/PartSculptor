import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
GIZMO = (ROOT / 'PartSculptor/FaceMorphGizmo.cs').read_text()
TOOL = (ROOT / 'PartSculptor/EditorStretchTool.cs').read_text()
MODULE = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()
class FaceMorphVisibilityTests(unittest.TestCase):
    def test_editor_camera_preferred(self):
        self.assertIn('EditorLogic.fetch.editorCamera', GIZMO)
        self.assertLess(GIZMO.index('Camera editor ='), GIZMO.index('Camera main ='))
        self.assertIn('if (editor != null && editor.isActiveAndEnabled) return editor;', GIZMO)
    def test_model_layer_culling(self):
        self.assertIn('FindRenderLayer()', GIZMO)
        self.assertIn('obj.layer = renderLayer;', GIZMO)
        self.assertIn('lineObj.layer = renderLayer;', GIZMO)
        self.assertIn('candidate.cullingMask & (1 << layer)', GIZMO)
    def test_screen_space_fallback(self):
        self.assertIn('GUI.DrawTexture(', GIZMO)
        self.assertIn('GUIUtility.RotateAroundPivot(', GIZMO)
        self.assertIn('screen[i].y', GIZMO)
        self.assertIn('Screen.height - screen[i].y', GIZMO)
        self.assertIn('Texture2D.whiteTexture', GIZMO)
    def test_editor_addon_always_draws_when_active(self):
        self.assertIn('FaceMorphGizmo.DrawActive(helpRect);', TOOL)
        self.assertIn('FaceMorphGizmo.DrawActive(helpRect);', TOOL[TOOL.index('private void OnGUI()'):])
        self.assertLess(TOOL.index('FaceMorphGizmo.DrawActive(helpRect);'), TOOL.index('if (modeActive && faceMode', TOOL.index('private void OnGUI()')))
        self.assertNotIn('FaceMorphGizmo.DrawFor(this)', MODULE)
if __name__ == '__main__': unittest.main()
