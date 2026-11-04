"""Source-level regression checks for user-provided custom-preset Reload fix."""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
REGISTRY = ROOT / "PartSculptor/ScalePresetRegistry.cs"


class CustomPresetReload474(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        contents = REGISTRY.read_text(encoding="utf-8")
        cls.method = contents.split("internal static void Reload()", 1)[1].split(
            "static void ReadDefinitions(", 1)[0]

    def test_loads_built_in_and_custom_paths(self):
        self.assertIn("ConfigNode.Load(BuiltInPath)", self.method)
        self.assertIn("ConfigNode.Load(CustomPath)", self.method)
        self.assertIn("File.Exists(CustomPath)", self.method)

    def test_supports_named_custom_root(self):
        self.assertIn('root.name == "PARTSCULPTOR_CUSTOM_PRESETS"', self.method)
        self.assertIn("ReadDefinitions(root, custom, false)", self.method)

    def test_supports_wrapped_custom_root(self):
        self.assertIn('root.GetNodes("PARTSCULPTOR_CUSTOM_PRESETS")', self.method)
        self.assertIn("ReadDefinitions(n, custom, false)", self.method)

    def test_clears_before_reloading_and_reports_counts(self):
        self.assertLess(self.method.index("builtIns.Clear()"), self.method.index("ConfigNode.Load(BuiltInPath)"))
        self.assertLess(self.method.index("custom.Clear()"), self.method.index("ConfigNode.Load(CustomPath)"))
        self.assertIn('custom.Count + " custom scale presets"', self.method)

    def test_preserves_named_and_wrapped_builtins(self):
        self.assertIn('root.name == "PARTSCULPTOR_PRESETS"', self.method)
        self.assertIn('root.GetNodes("PARTSCULPTOR_PRESETS")', self.method)
        self.assertIn('ReadDefinitions(root, builtIns, true)', self.method)
        self.assertIn('ReadDefinitions(n, builtIns, true)', self.method)


if __name__ == "__main__":
    unittest.main()
