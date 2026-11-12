"""Release 0.0.4.109: animation modules do not automatically block morphing."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAIN = (ROOT/'PartSculptor/PartSculptor.cs').read_text()


class AnimateGenericFaceMorph4106(unittest.TestCase):
    def test_animation_module_name_does_not_block_face_morph(self):
        start = MAIN.index('internal bool EnsureFaceMorph()')
        end = MAIN.index('internal void SetMorphFaces(', start)
        body = MAIN[start:end]
        self.assertNotIn('name.IndexOf("Animate"', body)
        self.assertNotIn('name.StartsWith("ModuleAnimate"', body)
        self.assertIn('new FaceMorphSupport(part, model)', body)
        self.assertIn('name.IndexOf("Robotic"', body)
        self.assertIn('name.IndexOf("B9DisableTransform"', body)
        self.assertIn('B9FaceMorphCompatibility.AllowsFaceMorph', body)
        self.assertIn('Robotic / transform-switching geometry unsupported:', body)

    def test_optional_stretch_restriction_remains_unchanged(self):
        begin = MAIN.index('string FindRestriction()')
        end = MAIN.index('void HideRestrictedControls()', begin)
        body = MAIN[begin:end]
        self.assertIn('Settings.RestrictAnimatedParts', body)
        self.assertIn('name.StartsWith("ModuleAnimate"', body)
        self.assertIn('name.StartsWith("ModuleAnimation"', body)
        self.assertIn('name.StartsWith("ModuleDeployable"', body)

    def test_versions_and_docs(self):
        for name in ('PartSculptor.version','GameData/PartSculptor/PartSculptor.version'):
            self.assertIn('"BUILD": 130', (ROOT/name).read_text())
        self.assertIn('AssemblyVersion("0.0.4.130")', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        for name in ('README.md','GameData/PartSculptor/README.md'):
            self.assertIn('`ModuleAnimateGeneric` module alone no longer blocks morphing', (ROOT/name).read_text())


if __name__ == '__main__':
    unittest.main()
