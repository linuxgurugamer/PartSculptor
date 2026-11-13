"""All PAW controls editor window, based on v0.0.4.133 without morph changes."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MODULE = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()
WINDOW = (ROOT / 'PartSculptor/PartSculptorControlsWindow.cs').read_text()
PROJECT = (ROOT / 'PartSculptor/PartSculptor.csproj').read_text()

class AllPawControlsWindow134(unittest.TestCase):
    def test_window_paw_event_and_registration(self):
        self.assertIn('public partial class ModulePartSculptor : PartModule', MODULE)
        self.assertIn('public partial class ModulePartSculptor', WINDOW)
        self.assertIn('guiName = "Show PartSculptor controls window"', WINDOW)
        self.assertIn('public void ToggleControlsWindow()', WINDOW)
        self.assertIn('ClickThruBlocker.GUIWindow', WINDOW)
        self.assertIn('GUILayout.BeginScrollView', WINDOW)
        self.assertIn('GUI.DragWindow();', WINDOW)
        self.assertIn('PartSculptorControlsWindow.cs', PROJECT)
        self.assertIn('ModulePartSculptor.DrawControlsWindow();', WINDOW)

    def test_covers_each_existing_editor_paw_field(self):
        # The source's KSPFields annotated with guiActiveEditor=true are all
        # represented in the window, even if some are read-only displays.
        field_pairs = re.findall(r'\[KSPField\([^\]]*?guiActiveEditor = true[^\]]*?guiName = "([^"]+)"', MODULE, re.S)
        required = {
            'Scale X (width)', 'Scale Y (height)', 'Scale Z (depth)',
            'Lock proportions', 'Stretch children', 'Match node sizes',
            'Match attached faces', 'Copy attached morph steps', 'Lock X/Z',
            'Bottom face tilt', 'Top face tilt', 'Tilt face',
            'Tilt X (degrees)', 'Tilt Z (degrees)', 'Numeric tilt status',
            'Volume multiplier', 'Dry mass', 'Dry cost', 'Tank capacity',
            'Scale safety', 'Career scale limits', 'Scale preset',
        }
        self.assertTrue(required.issubset(set(field_pairs)))
        for label in required:
            self.assertIn(label, WINDOW, label)

    def test_covers_all_original_paw_actions(self):
        actions = {
            'ShowPresets', 'ApplyNumericFaceTilt', 'ToggleFaceMorphGizmo',
            'ResetFaceMorph', 'ToggleScalingGizmo', 'ShowScaledValues',
            'RepeatPreset', 'ResetScale', 'SwapXZ', 'MatchFaceToParentNow',
        }
        for action in actions:
            self.assertIn(action + '(', WINDOW, action)
        self.assertIn('CloseControlsWindowFor(this)', MODULE)
        self.assertIn('CloseControlsWindowFor(this)', WINDOW)
        self.assertIn('Hide PartSculptor controls window', WINDOW)

    def test_uses_existing_scaling_and_morph_paths(self):
        self.assertIn('SetGizmoAxis(axis, requested);', WINDOW)
        self.assertIn('ApplyNumericFaceTilt();', WINDOW)
        self.assertIn('ApplyPreset(controlsSelectedPreset);', WINDOW)
        self.assertIn('OnMatchAttachedFacesModified(null);', WINDOW)
        self.assertIn('OnCopyAttachedMorphStepsModified(null);', WINDOW)
        self.assertIn('OnMatchNodeSizesModified(null);', WINDOW)
        self.assertIn('OnScaleChildrenModified(null);', WINDOW)
        self.assertIn('EditorUndoSupport.RecordBeforeChange();', WINDOW)
        self.assertNotIn('SetMorphFaces(', WINDOW)
        self.assertNotIn('faceMorph.Apply(', WINDOW)
        self.assertNotIn('part.transform.Rotate(', WINDOW)
        self.assertIn('controlsTiltDirty', WINDOW)
        self.assertIn('controlsScaleDirty', WINDOW)

    def test_version_and_docs(self):
        for name in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT / name).read_text())['VERSION']['BUILD'],137)
        self.assertIn('AssemblyVersion("0.0.4.137")', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        a = (ROOT/'README.md').read_text()
        b = (ROOT/'GameData/PartSculptor/README.md').read_text()
        self.assertEqual(a,b)
        self.assertIn('Show PartSculptor controls window',a)
        self.assertIn('## 0.0.4.134', (ROOT/'Changelog.md').read_text())
        self.assertTrue((ROOT/'Tests/ALL_PAW_CONTROLS_0.0.4.134.md').exists())

if __name__ == '__main__': unittest.main()
