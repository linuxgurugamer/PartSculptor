"""0.0.4.88-89: Cancel restores settings from opening; tip opacity is true panel alpha."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
SETTINGS = (ROOT / 'PartSculptor/ScaleSettingsWindow.cs').read_text()
MORPH = (ROOT / 'PartSculptor/FaceMorphGizmo.cs').read_text()


class SettingsCancelAndSolidTips488(unittest.TestCase):
    def test_cancel_restores_all_staged_settings(self):
        body = SETTINGS.split('void RestoreOpeningSettings()', 1)[1].split('internal void Toggle()', 1)[0]
        for name in ('matchNodeDefault', 'childrenDefault', 'lockDefault',
                     'morphLockDefault', 'snapSelection', 'tipOpacity', 'alternateSkin'):
            self.assertIn(name + ' = opened.', body)
        self.assertIn('Settings.LockMorphXZDefault = opened.LockMorphXZ;', body)
        self.assertIn('ModulePartSculptor.MatchNodeSizesGlobal = opened.ActiveMatchNodes;', body)
        self.assertIn('ModulePartSculptor.ScaleChildrenGlobal = opened.ActiveStretchChildren;', body)

    def test_cancel_rolls_back_to_opening_even_after_apply(self):
        body = SETTINGS.split('internal void ProcessPending()', 1)[1].split('void ClampWindowToScreen()', 1)[0]
        cancel = body.split('if (cancelPending)', 1)[1].split('if (!visible', 1)[0]
        self.assertIn('ScaleUserSettings.RestoreSnapshot(openingPreferences,', cancel)
        self.assertIn('appliedSinceOpening, out restoreError', cancel)
        self.assertIn('RestoreOpeningSettings();', cancel)
        self.assertIn('Close();', cancel)
        self.assertNotIn('SaveDefaults(', cancel)
        self.assertIn('appliedSinceOpening = true;', body)
        # Apply never changes the original restoration point.
        self.assertNotIn('CaptureOpeningSettings();', body)
        self.assertNotIn('TryCaptureSnapshot(', body)

    def test_open_captures_current_saved_settings(self):
        body = SETTINGS.split('void Open()', 1)[1].split('internal void Close()', 1)[0]
        self.assertIn('Settings.Load();', body)
        self.assertIn('tipOpacity = ScaleUserSettings.MorphTipOpacity;', body)
        self.assertIn('alternateSkin = ScaleUserSettings.UseAlternateSkin;', body)
        self.assertIn('CaptureOpeningSettings();', body)
        self.assertIn('TryCaptureSnapshot(out preferences, out snapshotError)', body)

    def test_tip_background_is_solid_at_full_opacity_independent_of_skin(self):
        body = MORPH.split('private static void DrawMorphTip(', 1)[1].split('internal static bool IsPointerOnHandleOrDragging', 1)[0]
        self.assertIn('panelColor.a = Mathf.Clamp01(ScaleUserSettings.MorphTipOpacity);', body)
        self.assertIn('GUI.DrawTexture(bounds, Texture2D.whiteTexture);', body)
        self.assertNotIn('previousColor.a * ScaleUserSettings.MorphTipOpacity', body)
        self.assertNotIn('GUI.Box(bounds, GUIContent.none', body)
        self.assertLess(body.index('GUI.DrawTexture(bounds, Texture2D.whiteTexture);'),
                        body.index('GUI.Label(bounds, message, textStyle);'))

    def test_text_alpha_and_gui_state_restoration(self):
        body = MORPH.split('private static void DrawMorphTip(', 1)[1].split('internal static bool IsPointerOnHandleOrDragging', 1)[0]
        self.assertIn('previousColor.b, 1f);', body)
        self.assertIn('textStyle.normal.background = null;', body)
        self.assertIn('GUI.color = previousColor;', body)
        self.assertIn('GUI.skin = previousSkin;', body)


if __name__ == '__main__':
    unittest.main()
