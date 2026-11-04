"""Cancel after Apply must undo the whole Settings dialog session."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
CODE = (ROOT/'PartSculptor/ScaleSettingsWindow.cs').read_text()

class SettingsOpeningSnapshot489(unittest.TestCase):
    def test_opening_snapshot_not_refreshed_by_apply(self):
        op = CODE.split('void Open()', 1)[1].split('internal void Close()',1)[0]
        process = CODE.split('internal void ProcessPending()',1)[1].split('void ClampWindowToScreen()',1)[0]
        self.assertIn('CaptureOpeningSettings();', op)
        self.assertIn('TryCaptureSnapshot(out preferences, out snapshotError)', op)
        self.assertIn('appliedSinceOpening = false;', op)
        self.assertNotIn('CaptureOpeningSettings();', process)
        self.assertIn('appliedSinceOpening = true;', process)

    def test_cancel_restores_config_snapshot_and_runtime_state(self):
        process = CODE.split('internal void ProcessPending()',1)[1].split('void ClampWindowToScreen()',1)[0]
        cancel = process.split('if (cancelPending)',1)[1].split('if (!visible',1)[0]
        self.assertIn('RestoreSnapshot(openingPreferences,', cancel)
        self.assertIn('RestoreOpeningSettings();', cancel)
        self.assertLess(cancel.index('RestoreSnapshot('), cancel.index('RestoreOpeningSettings();'))
        self.assertLess(cancel.index('RestoreOpeningSettings();'), cancel.index('Close();'))

    def test_file_restore_is_exact_not_reformatted(self):
        capture = CODE.split('internal static bool TryCaptureSnapshot(',1)[1].split('internal static bool RestoreSnapshot(',1)[0]
        restore = CODE.split('internal static bool RestoreSnapshot(',1)[1].split('internal static bool SaveDefaults(',1)[0]
        self.assertIn('File.ReadAllBytes(path)', capture)
        self.assertIn('File.WriteAllBytes(temporary, snapshot.FileContents)',restore)
        self.assertIn('File.Replace(temporary, path, null)',restore)
        self.assertIn('File.Move(temporary, path)', restore)

    def test_missing_original_cfg_is_removed_on_cancel(self):
        restore = CODE.split('internal static bool RestoreSnapshot(',1)[1].split('internal static bool SaveDefaults(',1)[0]
        self.assertIn('if (snapshot.FileExisted)',restore)
        self.assertIn('else if (File.Exists(path)) File.Delete(path);',restore)
        self.assertIn('if (appliedSinceOpening)',restore)

    def test_caches_and_live_shared_flags_restored(self):
        restore = CODE.split('internal static bool RestoreSnapshot(',1)[1].split('internal static bool SaveDefaults(',1)[0]
        for token in ('loaded = snapshot.Loaded;', 'matchNodeSizesDefault = snapshot.MatchNodeSizesDefault;',
            'scaleChildrenDefault = snapshot.ScaleChildrenDefault;',
            'lockProportionsDefault = snapshot.LockProportionsDefault;',
            'lockMorphXZDefault = snapshot.LockMorphXZDefault;',
            'morphSnapIncrement = snapshot.MorphSnapIncrement;',
            'morphTipOpacity = snapshot.MorphTipOpacity;',
            'useAlternateSkin = snapshot.UseAlternateSkin;'):
            self.assertIn(token, restore)
        session = CODE.split('void RestoreOpeningSettings()',1)[1].split('internal void Toggle()',1)[0]
        self.assertIn('ModulePartSculptor.MatchNodeSizesGlobal = opened.ActiveMatchNodes;',session)
        self.assertIn('ModulePartSculptor.ScaleChildrenGlobal = opened.ActiveStretchChildren;',session)

    def test_failed_rollback_does_not_close_window(self):
        cancel = CODE.split('if (cancelPending)',1)[1].split('if (!visible',1)[0]
        self.assertIn('status = restoreError;',cancel)
        self.assertIn('return; // Keep the window open if disk rollback failed.',cancel)
        self.assertLess(cancel.index('return; // Keep'),cancel.index('Close();'))

if __name__ == '__main__': unittest.main()
