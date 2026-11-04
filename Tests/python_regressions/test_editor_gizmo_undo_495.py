"""Regression guards for the Stretch/Morph single-gesture native KSP undo fix."""
import unittest
from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / 'PartSculptor'


def source(name):
    return (SRC / name).read_text(encoding='utf-8')


class EditorGizmoUndo495(unittest.TestCase):
    def test_new_support_file_is_in_old_style_csproj(self):
        tree = ET.parse(SRC / 'PartSculptor.csproj')
        includes = [e.attrib.get('Include') for e in tree.iter()
                    if e.tag.endswith('Compile')]
        self.assertIn('EditorUndoSupport.cs', includes)
        self.assertTrue((SRC / 'EditorUndoSupport.cs').exists())

    def test_ksp_undo_checkpoint_is_native_and_editor_only(self):
        text = source('EditorUndoSupport.cs')
        self.assertIn('EditorLogic.fetch.SetBackup();', text)
        self.assertIn('HighLogic.LoadedSceneIsEditor', text)
        self.assertIn('EditorLogic.fetch.ship == null', text)
        self.assertIn('Input.GetKeyDown(KeyCode.Z)', text)
        self.assertIn('Input.GetKeyDown(KeyCode.Y)', text)
        self.assertIn('Input.GetKey(KeyCode.LeftControl)', text)
        self.assertIn('Input.GetKey(KeyCode.RightControl)', text)
        self.assertNotIn('Input.ResetInputAxes', text)

    def test_stretch_checkpoint_once_before_first_mutation(self):
        text = source('ScaleGizmo.cs')
        self.assertIn('private bool undoCheckpointTaken;', text)
        self.assertEqual(text.count('undoCheckpointTaken = false;'), 1)
        self.assertEqual(text.count('EditorUndoSupport.RecordBeforeChange();'), 1)
        self.assertRegex(text, r'(?s)if \(!undoCheckpointTaken\)\s*\{\s*.*?undoCheckpointTaken = true;\s*EditorUndoSupport.RecordBeforeChange\(\);\s*\}\s*lastAppliedScale = desired;\s*module.SetGizmoAxis')
        self.assertLess(text.index('undoCheckpointTaken = false;', text.index('draggingAxis = hoveredAxis;')),
                        text.index('dragStartScale = module.GizmoScale;'))

    def test_morph_checkpoint_once_before_persisted_face_change(self):
        text = source('FaceMorphGizmo.cs')
        self.assertIn('private bool undoCheckpointTaken;', text)
        self.assertEqual(text.count('undoCheckpointTaken = false;'), 1)
        self.assertEqual(text.count('EditorUndoSupport.RecordBeforeChange();'), 1)
        self.assertRegex(text, r'(?s)if \(!undoCheckpointTaken &&\s*\(FacesDiffer\(b, module.GetMorphFace\(0\)\) \|\|\s*FacesDiffer\(top, module.GetMorphFace\(1\)\)\)\)\s*\{\s*.*?undoCheckpointTaken = true;\s*EditorUndoSupport.RecordBeforeChange\(\);\s*\}\s*module.SetMorphFaces\(b, top\);')
        for field in ('Width', 'Depth', 'TiltX', 'TiltZ', 'OffsetX', 'OffsetZ'):
            self.assertIn('a.' + field + ' - b.' + field, text)
        self.assertIn('undoCheckpointTaken = false;', text)

    def test_undo_exits_custom_tools_and_releases_lock(self):
        text = source('EditorStretchTool.cs')
        call = 'EditorUndoSupport.UndoRedoShortcutPressed()'
        self.assertEqual(text.count(call), 1)
        self.assertLess(text.index(call), text.index('if (!modeActive) return;'))
        logic = text[text.index('if (' + call):text.index('if (!modeActive) return;')]
        self.assertIn('!EditorLogic.fetch.AnyTextFieldHasFocus()', logic)
        self.assertIn('ExitStretchMode();', logic)
        self.assertIn('ScaleGizmo.Hide();', logic)
        self.assertIn('FaceMorphGizmo.Hide();', logic)
        self.assertNotIn('Event.current.Use()', logic)
        self.assertIn('ReleaseLock();', text[text.index('private void ExitStretchMode()'):])

    def test_morph_face_fields_are_persisted_for_native_undo(self):
        text = source('PartSculptor.cs')
        for end in ('Bottom', 'Top'):
            for field in ('X', 'Z', 'TiltX', 'TiltZ', 'OffsetX', 'OffsetZ'):
                self.assertIn('public float morph' + end + field, text)
                self.assertRegex(text, r'\[KSPField\(isPersistant = true\)\]\s+public float morph' + end + field)

    def test_uploaded_readme_update_and_undo_controls_both_copies(self):
        a = (ROOT / 'README.md').read_bytes()
        b = (ROOT / 'GameData/PartSculptor/README.md').read_bytes()
        self.assertEqual(a, b)
        text = a.decode('utf-8')
        for term in ('StretchScale (https://spacedock.info/mod/4718/StretchScale)',
                     'alpha test with a few users',
                     'forum.kerbalspaceprogram.com/topic/232076',
                     'Ctrl+Z', 'Ctrl+Y', '## License', '### Limitations'):
            self.assertIn(term, text)
        self.assertTrue(text.rstrip().endswith('or contains `ProceduralPart`.'))

    def test_version_metadata_consistent(self):
        import json
        for item in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            data = json.loads((ROOT / item).read_text())
            self.assertEqual(data['VERSION']['BUILD'], 97)
        self.assertIn('AssemblyVersion("0.0.4.97")', source('AssemblyVersion.cs'))


if __name__ == '__main__':
    unittest.main()
