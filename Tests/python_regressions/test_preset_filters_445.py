"""Integration guardrails for user-visible preset handling in v0.0.4.45."""
from pathlib import Path
import unittest
R = Path(__file__).resolve().parents[2]
S = R / 'PartSculptor'
G = R / 'GameData/PartSculptor'

class PresetFilters445(unittest.TestCase):
    def test_editor_error_variable_initialization(self):
        code = (S / 'ScalePresetEditor.cs').read_text()
        self.assertIn('else if (action == "Delete")\n            {\n                string error = "";', code)

    def test_settings_load_directly_from_plugin_data(self):
        self.assertFalse((G/'Settings.cfg').exists())
        self.assertTrue((G/'PluginData/Settings.cfg').exists())
        code = (S/'PartSculptor.cs').read_text()
        self.assertIn('GameData/PartSculptor/PluginData/Settings.cfg', code)
        self.assertIn('ConfigNode.Load(path)', code)
        self.assertNotIn('GameDatabase.Instance.GetConfigNodes("STRETCHSCALE_SETTINGS")', code)
        self.assertIn('ScaleUserSettings.Load(ref MatchNodeSizesDefault', code)

    def test_browser_filters_modes_and_selects_visible_only(self):
        code = (S/'ScalePresetsWindow.cs').read_text()
        self.assertIn('static bool showAbsolute = false;', code)
        self.assertIn('static bool showRelative = true;', code)
        self.assertIn('GUILayout.Toggle(showAbsolute, "")', code)
        self.assertIn('GUILayout.Label("Show absolute")', code)
        self.assertIn('GUILayout.Toggle(showRelative, "")', code)
        self.assertIn('GUILayout.Label("Show relative")', code)
        self.assertIn('preset.Relative ? showRelative : showAbsolute', code)
        self.assertIn('foreach (ScalePresetDefinition preset in visiblePresets)', code)
        self.assertIn('selectedKey = visiblePresets.Count > 0 ?', code)
        self.assertIn('if (Event.current.type == EventType.Layout)', code)

    def test_custom_readout_is_not_applied_as_preset(self):
        code = (S/'PartSculptor.cs').read_text()
        registry = (S/'ScalePresetRegistry.cs').read_text()
        self.assertIn('names.Add("Custom")', registry)
        self.assertIn('SetPresetDisplay("Custom")', code)
        self.assertIn('SetPresetDisplay(matches ? definition.Name : "Custom")', code)
        self.assertIn('if (selected != null && !selected.Relative)', code)
        self.assertIn('lastAppliedRelative = definition.Relative;', code)
        self.assertIn('ApplyPreset(ScalePresetRegistry.Find(lastAppliedPreset, lastAppliedRelative));', code)
        self.assertIn('if (Array.IndexOf(options, scalePreset) < 0)', code)

    def test_absolute_reset_and_relative_state(self):
        code = (S/'PartSculptor.cs').read_text()
        self.assertIn('scalePreset = "1x uniform";', code)
        self.assertIn('if ((Target - before).sqrMagnitude > 1e-10f)', code)
        self.assertIn('SetPresetDisplay(matches ? definition.Name : "Custom")', code)
        self.assertIn('other.scalePreset = name;', code)

if __name__ == '__main__': unittest.main()
