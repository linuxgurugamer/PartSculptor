"""Intentional v0.0.4.148 source-policy changes (requires KSP validation)."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = (ROOT / 'PartSculptor/B9FaceMorphCompatibility.cs').read_text()
GIZMO = (ROOT / 'PartSculptor/FaceMorphGizmo.cs').read_text()

class B9ModuleIgnoreAndWrap4147(unittest.TestCase):
    def test_only_subtype_module_is_ignored(self):
        body = SOURCE.split('foreach (ConfigNode nested in child.nodes)', 1)[1]
        body = body.split('if (count == 0)', 1)[0]
        self.assertRegex(body, r'if \(string\.Equals\(nested\.name, "MODULE",\s*StringComparison\.OrdinalIgnoreCase\)\)\s*continue;')
        self.assertLess(body.index('nested.name, "MODULE"'), body.index('nested.name, "RESOURCE"'))
        self.assertLess(body.index('nested.name, "MODULE"'), body.index('contains geometry/unknown node'))
        # SUBTYPE is still validated; unknown root/module-level nodes are not silently ignored.
        self.assertIn('if (!AllowedValues(child, SubtypeFields, where, out reason)) return false;', SOURCE)
        self.assertIn('if (!AllowedValues(config, ModuleFields, label, out reason)) return false;', SOURCE)

    def test_geometry_changing_nodes_remain_unsupported(self):
        body = SOURCE.split('foreach (ConfigNode nested in child.nodes)', 1)[1]
        self.assertIn('AllowsNodeModifier(part, nested, where + " / NODE"', body)
        self.assertIn('AllowsTextureNode(nested, where + " / TEXTURE"', body)
        self.assertIn('contains geometry/unknown node', body)
        self.assertNotIn('string.Equals(nested.name, "TRANSFORM"', body)

    def test_help_box_uses_word_wrap(self):
        body = GIZMO.split('internal static void DrawHelpBox(',1)[1].split('private static void DrawMorphTip',1)[0]
        self.assertIn('style.wordWrap = true;',body)
        self.assertNotIn('style.wordWrap = false;',body)
        self.assertIn('DrawMorphTip(bounds, message, style)',body)

    def test_version_and_readmes(self):
        for f in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT/f).read_text())['VERSION']['BUILD'],149)
        self.assertIn('AssemblyVersion("0.0.4.149")', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertEqual((ROOT/'README.md').read_bytes(), (ROOT/'GameData/PartSculptor/README.md').read_bytes())
        self.assertIn('Nested `MODULE` blocks under B9 subtypes', (ROOT/'README.md').read_text())

if __name__ == '__main__': unittest.main()
