"""0.0.4.78: persisted Morph tip styling and deferred flight capacity UI update."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
CODE = ROOT / 'PartSculptor'
PREFS = (CODE / 'ScaleSettingsWindow.cs').read_text()
GIZMO = (CODE / 'FaceMorphGizmo.cs').read_text()
PART = (CODE / 'PartSculptor.cs').read_text()

class MorphTipsAndFlightCapacity478(unittest.TestCase):
    def test_opacity_slider_range_and_persistence(self):
        self.assertIn('static float morphTipOpacity = 1f;', PREFS)
        self.assertIn('GUILayout.HorizontalSlider(tipOpacity, 0.1f, 1f)', PREFS)
        self.assertIn('IsValidMorphTipOpacity', PREFS)
        self.assertIn('n.GetValue("morphTipOpacity")', PREFS)
        self.assertIn('node.AddValue("morphTipOpacity",', PREFS)
        self.assertIn('morphTipOpacity = opacityValue;', PREFS)
        self.assertIn('CultureInfo.InvariantCulture', PREFS)

    def test_alternate_skin_option_is_saved_and_used_for_all_windows(self):
        self.assertIn('useAlternateSkin = parsed;', PREFS)
        self.assertIn('node.AddValue("useAlternateSkin", alternateSkinValue)', PREFS)
        self.assertIn('"Use alternate skin"', PREFS)
        self.assertIn('return useAlternateSkin || HighLogic.Skin == null ? host : HighLogic.Skin;', PREFS)
        for filename in ('VesselInspector.cs','ScaledValuesWindow.cs',
                         'ScalePresetsWindow.cs','ScalePresetEditor.cs'):
            self.assertIn('GUI.skin = ScaleUserSettings.WindowSkin(previous);',
                          (CODE / filename).read_text())

    def test_both_morph_tips_honor_transparency_and_restore_gui_state(self):
        self.assertIn('DrawMorphTip(bounds, message, style)', GIZMO)
        self.assertIn('DrawMorphTip(tipRect,', GIZMO)
        self.assertIn('GUI.DrawTexture(bounds, Texture2D.whiteTexture);', GIZMO)
        self.assertIn('panelColor.a = Mathf.Clamp01(ScaleUserSettings.MorphTipOpacity)', GIZMO)
        self.assertIn('GUI.color = new Color(previousColor.r', GIZMO)
        self.assertIn('GUI.Label(bounds, message, textStyle);', GIZMO)
        self.assertIn('GUI.color = previousColor;', GIZMO)
        self.assertIn('GUI.skin = previousSkin;', GIZMO)

    def test_flight_uses_deferred_one_time_paw_refresh(self):
        self.assertIn('if (HighLogic.LoadedSceneIsFlight)', PART)
        self.assertIn('RefreshFlightResourceDisplay();', PART)
        body = PART.split('private void RefreshFlightResourceDisplay()',1)[1].split(
            'void CheckForNewStackAttachment()',1)[0]
        self.assertIn('resourceDisplayDirty', body)
        self.assertIn('part.PartActionWindow == null', body)
        self.assertIn('!part.PartActionWindow.gameObject.activeInHierarchy', body)
        self.assertIn('resourceDisplayDirty = false;', body)
        self.assertIn('part.PartActionWindow.displayDirty = true;', body)
        self.assertIn('MonoUtilities.RefreshPartContextWindow(part);', body)
        self.assertNotIn('ApplyResourceScaling(', body)

    def test_initial_flight_resource_capacities_get_refresh_even_if_unchanged(self):
        startup = PART.split('public override void OnStartFinished(',1)[1].split(
            'void HideFields()',1)[0]
        self.assertIn('Math.Abs(StretchVolumeFactor - 1d) > 1e-6d', startup)
        self.assertIn('resourceDisplayDirty = true;', startup)
        rescale = PART.split('void ApplyResourceScaling(',1)[1].split(
            '// Live snapshot of everything',1)[0]
        self.assertIn('if (!changed && !structureChanged) return;', rescale)
        self.assertIn('resourceDisplayDirty = true;', rescale)
        self.assertIn('if (!HighLogic.LoadedSceneIsEditor) return;', rescale)

    def test_updated_documentation(self):
        guide=(ROOT/'README.md').read_text()
        self.assertIn('Morph tip opacity', guide)
        self.assertIn('Alternate skin', guide)
        self.assertIn('flight', (ROOT/'Changelog.md').read_text().lower())

if __name__ == '__main__': unittest.main()
