"""Source integration checks for the vertex collider fallback preference."""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
CODE = (ROOT / 'PartSculptor/ScaleSettingsWindow.cs').read_text()
SETTINGS = (ROOT / 'PartSculptor/Settings.cs').read_text()

class CollisionFallbackToggle5016(unittest.TestCase):
    def test_load_preserves_config_default_until_user_override(self):
        self.assertIn('static bool? generateVertexCollisionFallback;', CODE)
        self.assertIn('bool.TryParse(n.GetValue("generateVertexCollisionFallback"), out parsed)', CODE)
        self.assertIn('if (generateVertexCollisionFallback.HasValue)\n                Settings.GenerateVertexCollisionFallback = generateVertexCollisionFallback.Value;', CODE)
        self.assertLess(SETTINGS.index('TryGetValue("generateVertexCollisionFallback"'), SETTINGS.index('ScaleUserSettings.Load('))

    def test_toggle_is_staged_until_apply_or_save(self):
        draw = CODE.split('void DrawContents(int id)', 1)[1]
        self.assertIn('generateCollisionFallback = GUILayout.Toggle(generateCollisionFallback,', draw)
        self.assertNotIn('Settings.GenerateVertexCollisionFallback =', draw)
        self.assertIn('generateCollisionFallback = Settings.GenerateVertexCollisionFallback;', CODE)
        self.assertIn('hidePawControls, generateCollisionFallback, out error))', CODE)

    def test_atomic_persistence_and_live_update(self):
        save = CODE.split('bool hidePawControls, bool generateCollisionFallback, out string error)', 1)[1].split('internal static bool SaveHidePAWOnly(', 1)[0]
        self.assertIn('if (generateCollisionFallback != Settings.GenerateVertexCollisionFallback)', save)
        self.assertIn('if (fallbackPreference.HasValue)', save)
        self.assertIn('node.AddValue("generateVertexCollisionFallback", fallbackPreference.Value);', save)
        self.assertLess(save.index('File.Replace('), save.index('Settings.GenerateVertexCollisionFallback = generateCollisionFallback;'))

    def test_cancel_restores_both_cache_and_effective_setting(self):
        for snippet in ('GenerateVertexCollisionFallback = generateVertexCollisionFallback,',
                        'generateVertexCollisionFallback = snapshot.GenerateVertexCollisionFallback;',
                        'opened.GenerateCollisionFallback = generateCollisionFallback;',
                        'Settings.GenerateVertexCollisionFallback = opened.GenerateCollisionFallback;'):
            self.assertIn(snippet, CODE)
        self.assertIn('File.WriteAllBytes(temporary, snapshot.FileContents);', CODE)

if __name__ == '__main__': unittest.main()
