"""Version 0.0.4.63: Morph X/Z locking and settings cleanup."""
import re
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
MOD = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
GIZMO = (ROOT/'PartSculptor/FaceMorphGizmo.cs').read_text()
PREFS = (ROOT/'PartSculptor/ScaleSettingsWindow.cs').read_text()
CORE = (ROOT/'PartSculptor/FaceMorphCore.cs').read_text()
CONFIG = (ROOT/'GameData/PartSculptor/PluginData/Settings.cfg').read_text()
README = (ROOT/'README.md').read_text()

class MorphXZLock463(unittest.TestCase):
    def test_saved_per_part_paw_toggle_and_default(self):
        self.assertIn('guiName = "Lock X/Z"', MOD)
        self.assertRegex(MOD, r'\[KSPField\(isPersistant = true, guiActiveEditor = true, guiName = "Lock X/Z"')
        self.assertIn('public bool lockMorphXZ = true;', MOD)
        self.assertIn('affectSymCounterparts = UI_Scene.Editor)', MOD)
        self.assertIn('if (node != null && node.HasValue("lockMorphXZ")) hasSavedLockMorphXZ = true;', MOD)
        self.assertIn('if (!hasSavedLockMorphXZ)\n                lockMorphXZ = Settings.LockMorphXZDefault;', MOD)

    def test_default_true_and_saved_settings_override(self):
        self.assertIn('public static bool LockMorphXZDefault = true;', MOD)
        self.assertIn('lockMorphXZDefault = true', CONFIG)
        self.assertIn('n.TryGetValue("lockMorphXZDefault", ref LockMorphXZDefault);', MOD)
        self.assertIn('ref LockProportionsDefault, ref LockMorphXZDefault)', MOD)
        self.assertIn('"lockMorphXZDefault"', PREFS)
        self.assertIn('lockMorphXZDefault = parsed;', PREFS)
        self.assertIn('node.AddValue("lockMorphXZDefault", morphLockValue);', PREFS)
        self.assertIn('Settings.LockMorphXZDefault = morphLockValue;', PREFS)
        self.assertIn('"Lock X/Z by default"', PREFS)

    def test_morph_lock_uses_same_factor_for_x_and_z(self):
        self.assertIn('dragLockXZ = !alt && module.lockMorphXZ;', GIZMO)
        self.assertIn('if (dragRadial || dragLockXZ)', GIZMO)
        self.assertIn('FaceMorphCore.RadialScaleFactor(', GIZMO)
        self.assertIn('f.Width *= factor;', GIZMO)
        self.assertIn('f.Depth *= factor;', GIZMO)
        self.assertIn('if (dragRadial || dragLockXZ)\n                    {\n                        // Already snapped', GIZMO)
        self.assertIn('Math.Max(0.25d / initialWidth, 0.25d / initialDepth)', CORE)
        self.assertIn('Math.Min(4d / initialWidth, 4d / initialDepth)', CORE)
        self.assertIn('if (snapIncrement > 0d) delta = SnapDelta(delta, snapIncrement);', CORE)

    def test_other_modes_and_independent_resize_remain(self):
        self.assertIn('dragRadial = alt && ctrl;', GIZMO)
        self.assertIn('dragLinked = alt && shift && !ctrl;', GIZMO)
        self.assertIn('dragTilt = alt && !shift && !ctrl;', GIZMO)
        self.assertIn('else if (dragLinked)', GIZMO)
        self.assertIn('f.OffsetX += displacement.x / dragHalfX;', GIZMO)
        self.assertIn('f.TiltX += vertical', GIZMO)
        self.assertIn('f.Width += ((corner & 1) == 0 ? -1f : 1f)', GIZMO)
        self.assertIn('f.Depth += ((corner & 2) == 0 ? -1f : 1f)', GIZMO)

    def test_remove_settings_path_label_only(self):
        self.assertNotIn('Saved in PluginData/UserSettings.cfg (not Settings.cfg).', PREFS)
        self.assertIn('GameData/PartSculptor/PluginData/UserSettings.cfg', PREFS)
        self.assertIn('Lock X/Z', README)
        self.assertIn('Lock X/Z by default', README)

    def test_versions(self):
        for path in (ROOT/'PartSculptor.version', ROOT/'GameData/PartSculptor/PartSculptor.version'):
            match = re.search(r'"BUILD": (\d+)', path.read_text())
            self.assertIsNotNone(match)
            self.assertGreaterEqual(int(match.group(1)), 63)
        assembly = (ROOT/'PartSculptor/AssemblyVersion.cs').read_text()
        match = re.search(r'AssemblyVersion\("0\.0\.4\.(\d+)"\)', assembly)
        self.assertIsNotNone(match)
        self.assertGreaterEqual(int(match.group(1)), 63)

if __name__ == '__main__': unittest.main()
