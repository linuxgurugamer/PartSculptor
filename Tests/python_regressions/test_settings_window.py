"""Settings-window and built-in preset regression checks."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
CODE = ROOT / 'PartSculptor'
GAME = ROOT / 'GameData/PartSculptor'

class TestSettingsAndFixedPresets(unittest.TestCase):
    def test_default_off_and_saved_user_override(self):
        cfg = (GAME / 'PluginData/Settings.cfg').read_text()
        mod = (CODE / 'PartSculptor.cs').read_text()
        helper = (CODE / 'ScaleSettingsWindow.cs').read_text()
        self.assertIn('matchNodeSizesDefault = false', cfg)
        self.assertIn('public static bool MatchNodeSizesDefault = false;', mod)
        self.assertIn('public bool matchNodeSizes = false;', mod)
        self.assertIn('ScaleUserSettings.Load(ref MatchNodeSizesDefault, ref ScaleChildrenDefault,', mod)
        self.assertIn('GameData/PartSculptor/PluginData/UserSettings.cfg', helper)
        self.assertIn('ConfigNode.Load(path)', helper)
        self.assertIn('root.AddNode(NodeName)', helper)
        self.assertIn('File.Replace(temporary, path, null)', helper)
        self.assertIn('ModuleStretchScale.MatchNodeSizesGlobal = matchValue;', helper)
        self.assertIn('Settings.MatchNodeSizesDefault = matchValue;', helper)

    def test_settings_inspector_button_and_safe_lifecycle(self):
        code = (CODE / 'VesselInspector.cs').read_text()
        settings = (CODE / 'ScaleSettingsWindow.cs').read_text()
        proj = (CODE / 'PartSculptor.csproj').read_text()
        self.assertIn('Compile Include="ScaleSettingsWindow.cs"', proj)
        self.assertIn('GUILayout.Button(new GUIContent("Settings",', code)
        self.assertIn('settingsWindow.Toggle();', code)
        self.assertIn('settingsWindow.ProcessPending();', code)
        self.assertIn('settingsWindow.Draw();', code)
        self.assertIn('settingsWindow.Close();', code)
        self.assertIn('ClickThruBlocker.GUIWindow', settings)
        self.assertIn('"Match node sizes by default"', settings)
        self.assertIn('GUI.DragWindow();', settings)

    def test_fixed_double_and_half_y_noncompounding(self):
        config = (GAME / 'PluginData/Presets.cfg').read_text()
        for name, expected in [('Double Y', '2'), ('Half Y', '0.5')]:
            block = re.search(r'PRESET\s*\{\s*name = '+name+r'\s*(.*?)\}', config, re.S)
            self.assertIsNotNone(block)
            self.assertIn('mode = absolute', block.group(1))
            self.assertIn('x = 1', block.group(1))
            self.assertIn('y = '+expected, block.group(1))
            self.assertIn('z = 1', block.group(1))
        runner = (ROOT / 'Tests/RegressionRunner/Program.cs').read_text()
        self.assertIn('"Double Y repeat must not compound"', runner)
        self.assertIn('"Half Y repeat must not compound"', runner)
        self.assertIn('"Custom relative preset still multiplies current Y"', runner)

if __name__ == '__main__':
    unittest.main()
