"""Dedicated sixth editor Morph button: source-level integration checks."""
from pathlib import Path
import unittest
ROOT = Path(__file__).resolve().parents[2]
TOOL = (ROOT / 'PartSculptor/EditorStretchTool.cs').read_text()
MAIN = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()
MORPH = (ROOT / 'PartSculptor/FaceMorphGizmo.cs').read_text()

class MorphTool453(unittest.TestCase):
    def test_two_distinct_editor_buttons(self):
        self.assertIn('private void ToggleStretch() { ToggleMode(false); }', TOOL)
        self.assertIn('private void ToggleMorph() { ToggleMode(true); }', TOOL)
        self.assertIn('toolButton.onClick.AddListener(ToggleStretch)', TOOL)
        self.assertIn('morphToolButton.onClick.AddListener(ToggleMorph)', TOOL)
        self.assertIn('new GameObject("PartSculptor Morph Editor Tool"', TOOL)
        self.assertIn('"GameData/PartSculptor/PluginData/Icons/morph_38.png"', TOOL)
        self.assertTrue((ROOT/'GameData/PartSculptor/PluginData/Icons/morph_38.png').is_file())

    def test_selected_part_retained_on_switch(self):
        section = TOOL.split('private void ToggleMode(bool morph)',1)[1].split('private void ExitStretchMode',1)[0]
        self.assertIn('if (faceMode == morph) { ExitStretchMode(); return; }',section)
        self.assertIn('faceMode = morph;',section)
        self.assertIn('SelectGizmo();',section)
        # Not repicked or forgotten when changing from Stretch to Morph
        self.assertNotIn('selectedModule = null;',section.split('if (modeActive)',1)[1].split('if (!HighLogic.LoadedSceneIsEditor',1)[0])

    def test_both_gizmos_mutually_exclusive_and_existing_morph_pipeline(self):
        selection = TOOL.split('private void SelectGizmo()',1)[1].split('private void OnNativeClick',1)[0]
        self.assertIn('ScaleGizmo.Hide();', selection)
        self.assertIn('FaceMorphGizmo.Hide();', selection)
        self.assertIn('FaceMorphGizmo.Show(selectedModule)', selection)
        self.assertIn('ScaleGizmo.Show(selectedModule)', selection)
        self.assertIn('module.SetMorphFaces(b, top)', MORPH)
        self.assertIn('m.faceMorph.Apply(bottom, top, true);', MAIN)

    def test_sixth_icon_position_and_selected_highlight(self):
        self.assertIn('morphToolRect.position = toolRect.position + right.normalized', TOOL)
        self.assertIn('morphToolRect.SetSizeWithCurrentAnchors', TOOL)
        self.assertIn('background.color = modeActive && !faceMode', TOOL)
        self.assertIn('morphBackground.color = modeActive && faceMode', TOOL)
        self.assertNotIn('GUI.Button(ModeButton', TOOL)

    def test_exits_and_destroys_both_buttons_and_input_lock(self):
        cleanup = TOOL.split('private void DestroyToolButtons()',1)[1]
        self.assertIn('if (toolObject != null) Destroy(toolObject);', cleanup)
        self.assertIn('if (morphToolObject != null) Destroy(morphToolObject);', cleanup)
        self.assertIn('if (ownedMorphTexture != null) Destroy(ownedMorphTexture);', cleanup)
        self.assertIn('ReleaseLock();', TOOL)
        self.assertIn('native.onClick.RemoveListener(nativeClickHandler)', TOOL)
        self.assertIn('Input.GetKeyDown(KeyCode.Alpha4)', TOOL)
        self.assertIn('if (morphToolObject != null) morphToolObject.SetActive(false);', TOOL)

if __name__ == '__main__': unittest.main()
