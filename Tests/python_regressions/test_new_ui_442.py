"""Regression guardrails for default settings and the dedicated presets browser."""
from pathlib import Path
import unittest
ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT/'PartSculptor'

class TestNewUI(unittest.TestCase):
    def test_default_settings_persist_together(self):
        settings = (SRC/'ScaleSettingsWindow.cs').read_text()
        mod = (SRC/'PartSculptor.cs').read_text()
        cfg = (ROOT/'GameData/PartSculptor/PluginData/Settings.cfg').read_text()
        for key in ('matchNodeSizesDefault','scaleChildrenDefault','lockProportionsDefault'):
            self.assertIn('node.AddValue("'+key+'",',settings)
            self.assertIn('n.GetValue("'+key+'")',settings)
        for label in ('Match node sizes by default','Stretch children by default','Lock proportions by default'):
            self.assertIn(label,settings)
        self.assertIn('SaveDefaults(matchNodeDefault, childrenDefault, lockDefault,', settings)
        self.assertIn('lockProportionsDefault = false',cfg)
        self.assertIn('scaleChildrenDefault = false',cfg)
        self.assertIn('if (!hasSavedLockAxes)',mod)
        self.assertIn('node.HasValue("lockAxes")',mod)
        self.assertIn('lockAxes = Settings.LockProportionsDefault;',mod)
        self.assertIn('ScaleChildrenGlobal = Settings.ScaleChildrenDefault;',mod)

    def test_absolute_paw_slider_and_compact_full_browser(self):
        mod = (SRC/'PartSculptor.cs').read_text()
        screen = (SRC/'ScalePresetsWindow.cs').read_text()
        registry = (SRC/'ScalePresetRegistry.cs').read_text()
        proj = (SRC/'PartSculptor.csproj').read_text()
        self.assertIn('guiActiveEditor = true, guiName = "Scale preset"', mod)
        self.assertIn('[UI_ChooseOption(scene = UI_Scene.Editor,', mod)
        self.assertIn('Fields["scalePreset"].uiControlEditor.onFieldChanged = OnPresetChanged;', mod)
        self.assertIn('ScalePresetRegistry.AbsoluteOptionNames()', mod)
        self.assertIn('internal static string[] AbsoluteOptionNames()', registry)
        self.assertIn('if (!preset.Relative) result.Add(preset)', registry)
        self.assertIn('guiName = "Browse presets"', mod)
        self.assertIn('ScalePresetsWindow.Toggle(this);', mod)
        self.assertIn('Compile Include="ScalePresetsWindow.cs"', proj)
        self.assertIn('ScalePresetRegistry.All()', screen)
        self.assertNotIn('GUILayout.HorizontalSlider', screen)
        self.assertNotIn('QuickIndex()', screen)
        self.assertNotIn('ScalePresetRegistry.AbsolutePresets()', screen)
        self.assertIn('new Rect(170f, 125f, 410f, 554f)', screen)
        self.assertIn('preset.Relative ? "relative" : "absolute"', screen)
        self.assertIn('ApplyNamedPresetFromEditor(choice.Name, choice.Relative)', screen)
        self.assertIn('ScalePresetsWindow.DrawFor(this);', mod)
        self.assertIn('ScalePresetsWindow.ProcessPendingFor(this);', mod)
        self.assertIn('internal static void ProcessPendingFor(ModulePartSculptor module)', screen)
        self.assertIn('ScalePresetsWindow.CloseFor(this);', mod)
        self.assertIn('GUI.DragWindow()', screen)

    def test_relative_presets_show_custom_but_keep_last_applied(self):
        mod = (SRC/'PartSculptor.cs').read_text()
        screen = (SRC/'ScalePresetsWindow.cs').read_text()
        registry = (SRC/'ScalePresetRegistry.cs').read_text()
        self.assertIn('string lastAppliedPreset = "1x uniform";', mod)
        self.assertIn('SetPresetDisplay(matches ? definition.Name : "Custom");', mod)
        self.assertIn('lastAppliedPreset = definition.Name;', mod)
        self.assertIn('ApplyPreset(ScalePresetRegistry.Find(lastAppliedPreset, lastAppliedRelative));', mod)
        self.assertIn('if (selected != null && !selected.Relative)', mod)
        self.assertIn('scalePreset = "1x uniform";', mod)
        self.assertIn('lastAppliedPreset = "1x uniform";', mod)
        self.assertIn('module.lastAppliedPreset = "1x uniform";', mod)
        self.assertIn('module.LastAppliedPreset', screen)
        self.assertIn('names.Add("Custom")', registry)
        self.assertIn('if (!preset.Relative) result.Add(preset)', registry)

if __name__=='__main__': unittest.main()
