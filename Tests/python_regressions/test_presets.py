"""Offline source/config checks for data-driven presets and the in-game editor."""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
SRC = ROOT / 'PartSculptor'
GAME = ROOT / 'GameData' / 'PartSculptor'

class PresetIntegration(unittest.TestCase):
    def test_config_has_original_preset_names_and_modes(self):
        text = (GAME / 'PluginData/Presets.cfg').read_text()
        names = re.findall(r'^\s*name\s*=\s*(.+)$', text, re.M)
        originals = ['0.5x uniform', '1x uniform', '1.5x uniform',
                     '2x uniform', '3x uniform', '4x uniform',
                     'Double Y', 'Half Y']
        self.assertEqual(names, originals + originals)
        self.assertEqual(len(re.findall(r'^\s*mode = relative\s*$', text, re.M)), 8)
        self.assertEqual(len(re.findall(r'^\s*mode = absolute\s*$', text, re.M)), 8)

    def test_paw_preset_window_and_inspector_editor_are_connected(self):
        code = (SRC / 'PartSculptor.cs').read_text()
        self.assertIn('ScalePresetRegistry.EnsureLoaded();', code)
        self.assertIn('public void ShowPresets()', code)
        self.assertIn('ScalePresetsWindow.Toggle(this);', code)
        self.assertIn('ScalePresetRegistry.AbsoluteOptionNames()', code)
        self.assertNotIn('GUILayout.HorizontalSlider', (SRC / 'ScalePresetsWindow.cs').read_text())
        self.assertIn('[UI_ChooseOption(', code)
        self.assertIn('ScalePresetsWindow.DrawFor(this);', code)
        self.assertIn('ScalePresetMath.Apply(definition,', code)
        self.assertIn('ScalePresetsWindow.CloseFor(this);', code)
        self.assertIn('CommitChange(before, null);', code)
        inspector = (SRC / 'VesselInspector.cs').read_text()
        self.assertNotIn('guiName = "Edit scale presets"', code)
        self.assertNotIn('public void EditScalePresets()', code)
        self.assertIn('ScalePresetEditor.Toggle(target);', inspector)
        self.assertIn('"Edit presets"', inspector)
        self.assertIn('if (HighLogic.LoadedSceneIsEditor)', inspector)
        self.assertIn('presetEditorToggleQueued', inspector)
        self.assertIn('GUI.enabled = previousEnabled && target != null;', inspector)
        self.assertIn('ScalePresetEditor.DrawFor(this);', code)
        self.assertIn('ScalePresetEditor.CloseFor(this);', code)

    def test_presets_are_saved_only_as_custom_files(self):
        registry = (SRC / 'ScalePresetRegistry.cs').read_text()
        self.assertIn('PluginData/CustomPresets.cfg', registry)
        self.assertIn('root.AddNode("PARTSCULPTOR_CUSTOM_PRESETS")', registry)
        self.assertIn('ConfigNode.Load(customLoadPath)', registry)
        self.assertIn('File.Replace(tmp, path, null)', registry)
        self.assertIn('File.Move(tmp, path)', registry)
        self.assertIn('ModuleStretchScale.RefreshPresetOptionsAll()', registry)
        self.assertIn('if (!ScalePresetMath.Valid(item))', registry)

    def test_default_presets_are_read_only_custom_are_editable(self):
        code = (SRC / 'ScalePresetEditor.cs').read_text()
        self.assertIn('bool builtIn = selected != null && selected.BuiltIn', code)
        self.assertIn('GUI.enabled = !builtIn;', code)
        self.assertIn('New from current scale', code)
        self.assertIn('Duplicate selected', code)
        self.assertIn('Save custom preset', code)
        self.assertIn('Delete custom preset', code)
        self.assertIn('Reload files', code)
        self.assertIn('Apply selected to part', code)
        self.assertIn('ClickThruBlocker.GUIWindow', code)

    def test_production_preset_logic_linked_to_regression_runner(self):
        csproj = (SRC / 'PartSculptor.csproj').read_text()
        runner = (ROOT / 'Tests/RegressionRunner/RegressionRunner.csproj').read_text()
        for filename in ('ScalePresetMath.cs', 'ScalePresetEditor.cs', 'ScalePresetRegistry.cs'):
            self.assertIn('Compile Include="' + filename + '"', csproj)
        self.assertIn('ScalePresetMath.cs', runner)
        self.assertIn('private static void Presets()', (ROOT / 'Tests/RegressionRunner/Program.cs').read_text())

if __name__ == '__main__':
    unittest.main()
