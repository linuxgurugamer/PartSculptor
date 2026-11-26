"""v142: ScaleDebug calls Log.Debug and logger level reflects Settings.cfg."""
import unittest
from pathlib import Path

SOURCE = Path(__file__).resolve().parents[2] / 'PartSculptor'

class DebugLogger4142(unittest.TestCase):
    def test_all_scoped_debug_methods_use_debug_level(self):
        code = (SOURCE / 'ScaleDebug.cs').read_text()
        self.assertEqual(code.count('Log.Debug('), 3)
        self.assertNotIn('Log.Info("[DEBUG] " +', code)
        for name in ['Change(', 'Info(', 'Computed(']:
            self.assertIn(name, code)
        self.assertIn('if (!Enabled) return;', code)
        self.assertIn('if (!Enabled || owner == null) return;', code)

    def test_config_drives_logger_level(self):
        code = (SOURCE / 'RegisterToolbar.cs').read_text()
        self.assertIn('ToolbarControl.RegisterMod(VesselInspectorHost.ModId, "PartSculptor");', code)
        self.assertIn('if (Settings.DebugLogChanges)', code)
        self.assertIn('Log = new Log("PartSculptor", Log.LEVEL.DETAIL);', code)
        self.assertIn('Log = new Log("PartSculptor", Log.LEVEL.ERROR);', code)
        self.assertNotIn('#if DEBUG', code)

    def test_settings_load_after_bootstrap_and_before_level_selection(self):
        code = (SOURCE / 'RegisterToolbar.cs').read_text()
        self.assertLess(code.index('Log = new Log("PartSculptor", Log.LEVEL.ERROR);'),
                        code.index('Settings.Load();'))
        self.assertLess(code.index('Settings.Load();'), code.index('if (Settings.DebugLogChanges)'))
        settings = (SOURCE / 'Settings.cs').read_text()
        self.assertIn('n.TryGetValue("debugLogChanges", ref DebugLogChanges);', settings)

    def test_version(self):
        self.assertIn('AssemblyVersion("0.0.5.23")', (SOURCE/'AssemblyVersion.cs').read_text())

if __name__ == '__main__':
    unittest.main()
