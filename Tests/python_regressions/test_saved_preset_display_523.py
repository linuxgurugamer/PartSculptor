"""Source-level guards for craft preset persistence and display-only recovery."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
MAIN = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()
MATH = (ROOT / 'PartSculptor/ScalePresetMath.cs').read_text()

def section(start, end):
    return MAIN.split(start, 1)[1].split(end, 1)[0]

class SavedPresetDisplay523(unittest.TestCase):
    def test_save_load_preset_identity(self):
        self.assertIn('[KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Scale preset",', MAIN)
        self.assertIn('loadedPresetDisplay = node.GetValue("scalePreset");', MAIN)
        self.assertIn('node.SetValue("scalePreset", savedDisplay, true);', MAIN)
        self.assertIn('presetRestorePending ? loadedPresetDisplay : scalePreset', MAIN)

    def test_restore_after_clamp_before_callback(self):
        start = section('public override void OnStart(StartState state)', 'public override void OnStartFinished')
        self.assertLess(start.index('scaleX = Clamp(scaleX);'), start.index('RestorePresetDisplay();'))
        self.assertLess(start.index('RefreshPresetOptions();'), start.index('onFieldChanged = OnPresetChanged;'))
        callback = section('void OnPresetChanged(', 'public void ShowPresets()')
        self.assertIn('if (refreshingPresetOptions || presetRestorePending || !initialized) return;', callback)

    def test_restore_does_not_apply_geometry(self):
        restore = section('void RestorePresetDisplay()', 'void OnPresetChanged(')
        for unwanted in ('ApplyPreset(', 'CommitChange(', 'scaleX =', 'scaleY =', 'scaleZ =', 'SetPresetDisplay('):
            self.assertNotIn(unwanted, restore)
        self.assertIn('scalePreset = "Custom";', restore)
        self.assertIn('finally { refreshingPresetOptions = false; }', restore)

    def test_legacy_matching_and_explicit_custom(self):
        self.assertIn('bool infer = string.IsNullOrEmpty(savedName);', MATH)
        self.assertIn('Valid(preset) && !preset.Relative', MATH)
        self.assertIn('x == (float)preset.X && y == (float)preset.Y && z == (float)preset.Z', MATH)
        self.assertIn('string.Equals(savedName, "Custom", StringComparison.OrdinalIgnoreCase)', MATH)
        self.assertIn('(infer || string.Equals(savedName, preset.Name, StringComparison.OrdinalIgnoreCase))', MATH)

    def test_repeat_command_and_copy(self):
        for token in ('node.GetValue("lastAppliedPreset")', 'node.GetValue("lastAppliedRelative")',
                      'node.SetValue("lastAppliedPreset"', 'node.SetValue("lastAppliedRelative"',
                      'copy.lastAppliedPreset = lastAppliedPreset;', 'copy.lastAppliedRelative = lastAppliedRelative;',
                      'other.lastAppliedPreset = lastAppliedPreset;', 'other.lastAppliedRelative = lastAppliedRelative;'):
            self.assertIn(token, MAIN)

    def test_model_repair_does_not_clear_unchanged_preset(self):
        apply = section('void Apply(bool moveNeighbours', 'Vector3 total = newScale * f;')
        self.assertIn('(newScale - lastAcceptedScale).sqrMagnitude > 1e-10f', apply)
        self.assertIn('if (presetRestorePending)', apply)
        self.assertIn('RestorePresetDisplay();', apply)
        self.assertNotIn('(newScale - oldScale).sqrMagnitude > 1e-10f', apply)
