"""PartSculptor .87: stable settings footer, correct save semantics, background-only opacity."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
SETTINGS = (ROOT/'PartSculptor/ScaleSettingsWindow.cs').read_text()
MORPH = (ROOT/'PartSculptor/FaceMorphGizmo.cs').read_text()

class SettingsFooterAndMorphTipBackground487(unittest.TestCase):
    def test_window_clamps_entire_rect_inside_screen(self):
        self.assertIn('void ClampWindowToScreen()', SETTINGS)
        self.assertIn('Screen.height - windowRect.height', SETTINGS)
        self.assertIn('Screen.width - windowRect.width', SETTINGS)
        self.assertIn('ClampWindowToScreen();', SETTINGS)

    def test_settings_scroll_separate_from_footer(self):
        body = SETTINGS.split('void DrawContents(int id)', 1)[1]
        scroll = body.index('GUILayout.BeginScrollView(')
        end = body.index('GUILayout.EndScrollView();')
        footer = body.index('GUILayout.Button("Save"')
        self.assertLess(scroll, end)
        self.assertLess(end, footer)
        self.assertIn('GUILayout.Height(Mathf.Max(75f, windowRect.height - 143f))', body)

    def test_save_apply_and_cancel_without_close_button(self):
        self.assertIn('GUILayout.Button("Save",', SETTINGS)
        self.assertIn('GUILayout.Button("Apply",', SETTINGS)
        self.assertIn('GUILayout.Button("Cancel",', SETTINGS)
        self.assertNotIn('GUILayout.Button("Close",', SETTINGS)

    def test_save_closes_apply_keeps_window_open(self):
        body=SETTINGS.split('internal void ProcessPending()',1)[1].split('void ClampWindowToScreen()',1)[0]
        self.assertIn('bool closeAfterSaving = savePending;', body)
        self.assertIn('if (closeAfterSaving) Close();', body)
        self.assertIn('else status = "Settings applied.";', body)
        self.assertIn('ScaleUserSettings.SaveDefaults(', body)

    def test_cancel_does_not_commit_edits(self):
        body=SETTINGS.split('internal void ProcessPending()',1)[1].split('void ClampWindowToScreen()',1)[0]
        self.assertLess(body.index('if (cancelPending)'),
                        body.index('ScaleUserSettings.SaveDefaults('))
        self.assertIn('RestoreOpeningSettings();', body)
        self.assertIn('CaptureOpeningSettings();', SETTINGS)
        self.assertIn('cancelPending = false;', SETTINGS)

    def test_background_only_opacity_leaves_text_full_alpha(self):
        body=MORPH.split('private static void DrawMorphTip(',1)[1].split('internal static bool IsPointerOnHandleOrDragging',1)[0]
        self.assertIn('panelColor.a = Mathf.Clamp01(ScaleUserSettings.MorphTipOpacity)',body)
        background=body.index('GUI.DrawTexture(bounds, Texture2D.whiteTexture);')
        restore=body.index('GUI.color = new Color(previousColor.r', background)
        text=body.index('GUI.Label(bounds, message, textStyle);')
        self.assertLess(background, restore)
        self.assertLess(restore, text)
        self.assertIn('textStyle.normal.background = null;', body)
        self.assertIn('GUI.skin = previousSkin;', body)

    def test_both_tip_types_share_one_draw_path(self):
        self.assertIn('DrawMorphTip(bounds, message, style);', MORPH)
        self.assertIn('DrawMorphTip(tipRect,', MORPH)

    def test_readmes_document_new_behavior(self):
        for path in ('README.md','GameData/PartSculptor/README.md'):
            text=(ROOT/path).read_text()
            self.assertIn('**background transparency**',text)
            self.assertIn('**Apply**',text)
            self.assertIn('**Cancel**',text)

if __name__=='__main__': unittest.main()
