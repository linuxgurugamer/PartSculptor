"""Every on-screen PartSculptor notification must record part/mod provenance."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / 'PartSculptor'
HELPER = (SRC / 'PartSculptorScreenMessages.cs').read_text()
MAIN = (SRC / 'PartSculptor.cs').read_text()
TOOL = (SRC / 'EditorStretchTool.cs').read_text()


class ScreenMessageLogging4140(unittest.TestCase):
    def test_all_screen_messages_are_wrapped(self):
        sources = {p.name: p.read_text() for p in SRC.glob('*.cs')}
        direct = [(name, len(re.findall(r'ScreenMessages\.PostScreenMessage\s*\(', source)))
                  for name, source in sources.items()]
        self.assertEqual([(name, count) for name, count in direct if count],
                         [('PartSculptorScreenMessages.cs', 1)])
        self.assertEqual(MAIN.count('PartSculptorScreenMessages.Post('), 3)
        self.assertEqual(TOOL.count('PartSculptorScreenMessages.Post('), 2)
        self.assertIn('clicked, faceStatus', TOOL)
        self.assertIn('selectedModule.part, faceStatus', TOOL)

    def test_helper_writes_log_for_each_message_even_without_debug(self):
        self.assertIn('Log.Info("[ScreenMessage]', HELPER)
        self.assertIn('ScreenMessages.PostScreenMessage(message, duration, style);', HELPER)
        self.assertLess(HELPER.index('Log.Info("[ScreenMessage]'),
                        HELPER.index('ScreenMessages.PostScreenMessage(message, duration, style);'))
        self.assertNotIn('if (!Settings.DebugLogChanges)', HELPER)
        self.assertIn('Safe(message)', HELPER)
        self.assertIn('Safe(source)', HELPER)
        self.assertIn('part.GetInstanceID()', HELPER)
        self.assertIn('part.partInfo.title', HELPER)
        self.assertIn('part.partInfo.partUrl', HELPER)
        self.assertIn('part.partInfo.configFileFullName', HELPER)
        self.assertIn('"GameData/" + url', HELPER)
        self.assertIn('"<unavailable>"', HELPER)

    def test_all_sources_and_project_entry(self):
        for label in ('"NumericTiltError"', '"ToggleFaceMorphGizmo"',
                      '"MatchFaceToParentNow"'):
            self.assertIn(label, MAIN)
        for label in ('"EditorStretchTool.PickPart"', '"EditorStretchTool.SelectGizmo"'):
            self.assertIn(label, TOOL)
        self.assertIn('<Compile Include="PartSculptorScreenMessages.cs" />',
                      (SRC / 'PartSculptor.csproj').read_text())

    def test_version_and_docs(self):
        for relative in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(148, json.loads((ROOT / relative).read_text())['VERSION']['BUILD'])
        self.assertIn('AssemblyVersion("0.0.4.148")',
                      (SRC / 'AssemblyVersion.cs').read_text())
        self.assertEqual((ROOT / 'README.md').read_bytes(),
                         (ROOT / 'GameData/PartSculptor/README.md').read_bytes())


if __name__ == '__main__':
    unittest.main()
