"""Structural tests for the editor Stretch tool wiring (no KSP runtime)."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TOOL = (ROOT / 'PartSculptor' / 'EditorStretchTool.cs').read_text()
GIZMO = (ROOT / 'PartSculptor' / 'ScaleGizmo.cs').read_text()
PROJ = (ROOT / 'PartSculptor' / 'PartSculptor.csproj').read_text()


class EditorStretchToolTests(unittest.TestCase):
    def test_registered_in_editor_only(self):
        self.assertIn('KSPAddon.Startup.EditorAny, false', TOOL)
        self.assertIn('EditorStretchTool.cs', PROJ)
    def test_button_is_adjacent_native_four(self):
        self.assertIn('EditorLogic.fetch.toolsUI', TOOL)
        self.assertIn('AlignNextToStockTools()', TOOL)
        self.assertIn('stockReference.GetWorldCorners(corners)', TOOL)
        self.assertIn('centerRight + right.normalized', TOOL)
    def test_tool_selects_part_without_detaching(self):
        self.assertIn('InputLockManager.SetControlLock(mask, LockId)', TOOL)
        self.assertIn('FindModuleImplementing<ModulePartSculptor>', TOOL)
        self.assertIn('ScaleGizmo.Show(selectedModule)', TOOL)
        self.assertIn('FaceMorphGizmo.Show(selectedModule)', TOOL)
        self.assertIn('ScaleGizmo.IsPointerOnHandleOrDragging()', TOOL)
    def test_native_exit_and_cleanup(self):
        self.assertIn('nativeState.onTrue.AddListener(nativeClickHandler)', TOOL)
        self.assertIn('Input.GetKeyDown(KeyCode.Alpha1)', TOOL)
        self.assertIn('private void OnDestroy() { Cleanup(); }', TOOL)
        self.assertIn('ReleaseLock();', TOOL)
        self.assertIn('ScaleGizmo.Hide();', TOOL)
    def test_gizmo_is_non_compounding_existing_scaler(self):
        self.assertIn('internal static void Show(ModulePartSculptor module)', GIZMO)
        self.assertIn('module.CanUseScaleGizmo', GIZMO)
        self.assertIn('SetGizmoAxis', GIZMO)
    def test_icon_plugin_data_loaded_as_file(self):
        self.assertIn('File.ReadAllBytes(path)', TOOL)
        self.assertIn('GameData/PartSculptor/PluginData/Icons/inspector_38_gizmo.png', TOOL)

if __name__ == '__main__':
    unittest.main()
