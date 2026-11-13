"""PartSculptor 0.0.4.136 editor-selected controls window and PAW setting."""
import json
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
def code(path): return (ROOT/path).read_text(encoding='utf-8-sig')

class SelectedControlsWindow135(unittest.TestCase):
    def test_single_editor_host_and_current_part(self):
        w=code('PartSculptor/PartSculptorControlsWindow.cs')
        s=code('PartSculptor/PartSculptor.cs')
        self.assertIn('KSPAddon.Startup.EditorAny',w)
        self.assertIn('EditorLogic.SelectedPart',w)
        self.assertIn('GameEvents.onPartActionUICreate.Add(OnPartActionUICreate)',w)
        self.assertIn('GameEvents.onPartActionUICreate.Remove(OnPartActionUICreate)',w)
        self.assertIn('internal static void SelectControlsWindowPart(Part selected)',w)
        self.assertIn('controlsWindowSelectedPart = selected;',w)
        self.assertIn('RefreshControlsWindowEdits(true)',w)
        self.assertIn('void DrawControlsWindow()',w)
        self.assertIn('Stable ID:',w)
        self.assertNotIn('DrawControlsWindowFor(this);',s)
        self.assertIn('ModulePartSculptor.DrawControlsWindow();',w)
        self.assertIn('RequestControlsWindowPart(part);',code('PartSculptor/VesselInspector.cs'))
        self.assertIn('RequestControlsWindowPart(clicked);',code('PartSculptor/EditorStretchTool.cs'))

    def test_paw_visibility_preserves_only_opener(self):
        s=code('PartSculptor/PartSculptor.cs')
        self.assertIn('SetPAWControlsHidden(bool hidden)',s)
        self.assertIn('pawFieldsBeforeHide[field] = field.guiActiveEditor;',s)
        self.assertIn('pawEventsBeforeHide[action] = action.guiActiveEditor;',s)
        self.assertIn('if (action == Events["ToggleControlsWindow"]) continue;',s)
        self.assertIn('if (opener != null) opener.guiActiveEditor = true;',s)
        self.assertIn('ApplyPAWControlsVisibility();',s)
        self.assertIn('!Settings.HidePartSculptorPAWControls && Settings.EnableCareerLimits',s)

    def test_user_setting_and_dialog(self):
        sc=code('PartSculptor/ScaleSettingsWindow.cs')
        sett=code('PartSculptor/Settings.cs')
        cfg=code('GameData/PartSculptor/PluginData/Settings.cfg')
        w=code('PartSculptor/PartSculptorControlsWindow.cs')
        self.assertIn('hidePartSculptorPAWControls = false',cfg)
        self.assertIn('n.TryGetValue("hidePartSculptorPAWControls"',sett)
        self.assertIn('Settings.HidePartSculptorPAWControls = hidePartSculptorPAWControls.Value',sc)
        self.assertIn('node.AddValue("hidePartSculptorPAWControls", hidePawControls)',sc)
        self.assertIn('Hide PartSculptor PAW controls (keep window button)',sc)
        self.assertIn('SaveHidePAWOnly(hidePAW, out preferenceError)',w)
        self.assertIn('RestoreOpeningSettings()',sc)

    def test_versions_and_docs(self):
        for name in ('PartSculptor.version','GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(137,json.loads(code(name))['VERSION']['BUILD'])
        self.assertIn('AssemblyVersion("0.0.4.137")',code('PartSculptor/AssemblyVersion.cs'))
        self.assertEqual(code('README.md'),code('GameData/PartSculptor/README.md'))
        self.assertIn('## 0.0.4.136',code('Changelog.md'))

if __name__ == '__main__': unittest.main()
