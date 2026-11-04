"""Regression checks for the 0.0.4.44 preset catalog and duplicate-name handling."""
import pathlib, re, unittest
ROOT = pathlib.Path(__file__).resolve().parents[2]
SRC = ROOT / 'PartSculptor'
GAME = ROOT / 'GameData' / 'PartSculptor'

class Catalog444(unittest.TestCase):
    def test_plugin_data_only_and_sixteen_entries(self):
        self.assertFalse((GAME / 'Presets.cfg').exists())
        cfg = (GAME / 'PluginData/Presets.cfg').read_text()
        self.assertEqual(len(re.findall(r'^\s*PRESET\s*$', cfg, re.M)), 16)
        self.assertEqual(len(re.findall(r'^\s*mode = absolute\s*$', cfg, re.M)), 8)
        self.assertEqual(len(re.findall(r'^\s*mode = relative\s*$', cfg, re.M)), 8)
        names = re.findall(r'^\s*name = (.*)$', cfg, re.M)
        self.assertEqual(names[:8], names[8:])

    def test_loader_does_not_use_game_database(self):
        code = (SRC / 'ScalePresetRegistry.cs').read_text()
        self.assertIn('GameData/PartSculptor/PluginData/Presets.cfg', code)
        self.assertIn('ConfigNode.Load(BuiltInPath)', code)
        self.assertNotIn('GetConfigNodes("PARTSCULPTOR_PRESETS")', code)
        self.assertIn('SameIdentity(p, name, relative)', code)
        self.assertIn('internal static ScalePresetDefinition Find(string name, bool relative)', code)
        self.assertIn('return Find(name, false) ?? Find(name, true);', code)

    def test_paw_and_browser_route_modes(self):
        module = (SRC / 'PartSculptor.cs').read_text()
        browser = (SRC / 'ScalePresetsWindow.cs').read_text()
        editor = (SRC / 'ScalePresetEditor.cs').read_text()
        self.assertIn('ScalePresetRegistry.AbsoluteOptionNames()', module)
        self.assertIn('lastAppliedRelative = definition.Relative;', module)
        self.assertIn('ApplyPreset(ScalePresetRegistry.Find(lastAppliedPreset, lastAppliedRelative));', module)
        self.assertIn('ScalePresetRegistry.All()', browser)
        self.assertIn('ScalePresetRegistry.Key(preset)', browser)
        self.assertIn('ScalePresetRegistry.FindByKey(selectedKey)', browser)
        self.assertIn('ApplyNamedPresetFromEditor(choice.Name, choice.Relative)', browser)
        self.assertIn('ScalePresetRegistry.FindByKey(selectedKey)', editor)
        self.assertIn('ScalePresetRegistry.DeleteCustom(previous.Name, previous.Relative, out error)', editor)

    def test_windows_height(self):
        self.assertIn('410f, 554f', (SRC/'ScalePresetsWindow.cs').read_text())
        self.assertIn('510f, 560f', (SRC/'ScaleSettingsWindow.cs').read_text())

if __name__ == '__main__': unittest.main()
