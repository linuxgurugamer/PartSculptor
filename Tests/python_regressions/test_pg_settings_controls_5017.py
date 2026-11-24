"""Source integration contracts for PG radius and shared settings controls."""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
UI = (ROOT/'PartSculptor/ProceduralGeometryTool.cs').read_text()
SETTINGS = (ROOT/'PartSculptor/ScaleSettingsWindow.cs').read_text()

class PGSettingsControls5017(unittest.TestCase):
    def test_radius_precision_for_selection_and_slider(self):
        self.assertIn('Math.Round((double)radius, 3, MidpointRounding.AwayFromZero)', UI)
        self.assertIn('radius.ToString("0.###", CultureInfo.InvariantCulture)', UI)
        self.assertIn('radiusSlider = RoundRadius(softSelection ? displayedRadius : 0.25f);', UI)
        self.assertIn('radiusSlider = RoundRadius(slid);', UI)
        self.assertNotIn('radiusText = slid.ToString("R"', UI)

    def test_typed_radius_limited_before_parse(self):
        self.assertIn("if (c == '.' && point < 0)", UI)
        self.assertIn("else if (c < '0' || c > '9') return previous;", UI)
        self.assertIn('text.Length - point - 1 > 3', UI)
        self.assertIn('text.Substring(0, point + 4)', UI)
        self.assertLess(UI.index('radiusText = LimitRadiusText(radiusText, oldText);'),
                        UI.index('float.TryParse(radiusText, NumberStyles.Float,'))

    def test_full_window_drag_after_interactive_controls(self):
        body = SETTINGS.split('void DrawContents(int id)', 1)[1]
        self.assertIn('GUI.DragWindow();', body)
        self.assertNotIn('GUI.DragWindow(new Rect', body)
        self.assertLess(body.index('GUILayout.EndScrollView();'), body.index('GUI.DragWindow();'))
        self.assertLess(body.index('GUILayout.Button("Cancel"'), body.index('GUI.DragWindow();'))

    def test_pg_toggle_reads_effective_setting_below_radius(self):
        body = UI.split('private void DrawVertexPanel(', 1)[1].split('private bool UpdateHandlesInternal(', 1)[0]
        self.assertLess(body.index('GUILayout.Button("Apply influence radius")'),
                        body.index('HelpContent("Generate vertex collision fallback"'))
        self.assertIn('bool fallback = Settings.GenerateVertexCollisionFallback;', body)
        self.assertIn('if (requestedFallback != fallback)', body)
        self.assertIn('ScaleSettingsWindow.SetCollisionFallbackFromPG(requestedFallback, out error)', body)

    def test_preference_only_write_and_atomic_commit(self):
        body = SETTINGS.split('internal static bool SaveCollisionFallbackOnly(',1)[1].split('internal static bool SaveHidePAWOnly(',1)[0]
        self.assertIn('ConfigNode.Load(path)', body)
        self.assertIn('node.SetValue("generateVertexCollisionFallback", enabled.ToString(), true);', body)
        self.assertLess(body.index('File.Replace(temporary, path, null)'), body.index('Settings.GenerateVertexCollisionFallback = enabled;'))
        self.assertNotIn('SaveDefaults(', body)
        self.assertNotIn('ModulePartSculptor.', body)

    def test_open_settings_window_tracks_pg_toggle_and_cancel(self):
        self.assertIn('activeWindow = this;', SETTINGS)
        self.assertIn('if (activeWindow == this) activeWindow = null;', SETTINGS)
        self.assertIn('activeWindow.generateCollisionFallback = enabled;', SETTINGS)
        self.assertIn('activeWindow.appliedSinceOpening = true;', SETTINGS)
        self.assertIn('Settings.GenerateVertexCollisionFallback = opened.GenerateCollisionFallback;', SETTINGS)

if __name__ == '__main__': unittest.main()
