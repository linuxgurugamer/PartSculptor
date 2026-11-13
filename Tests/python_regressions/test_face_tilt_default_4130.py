"""0.0.4.131: distributed and runtime maximum face-tilt defaults agree."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

class FaceTiltDefault130(unittest.TestCase):
    def read(self, relative):
        return (ROOT / relative).read_text()

    def test_distributed_default(self):
        cfg = self.read('GameData/PartSculptor/PluginData/Settings.cfg')
        self.assertRegex(cfg, r'(?m)^\s*maxFaceTilt\s*=\s*2\.5\s*$')
        self.assertIn('Restart KSP', cfg)

    def test_runtime_fallback(self):
        s = self.read('PartSculptor/Settings.cs')
        self.assertIn('public const float DefaultMaxFaceTilt = 2.5f;', s)
        self.assertIn('public static float MaxFaceTilt = DefaultMaxFaceTilt;', s)
        self.assertIn('MaxFaceTilt = DefaultMaxFaceTilt;', s)
        self.assertIn('MaxFaceTilt > 4f', s)
        self.assertIn('n.TryGetValue("maxFaceTilt", ref MaxFaceTilt);', s)

    def test_geometry_fitting_default(self):
        s = self.read('PartSculptor/FaceAttachmentMatchCore.cs')
        self.assertIn('double maxFaceTilt = 2.5d', s)
        self.assertIn('maxFaceTilt > 4d', s)
        self.assertIn('out fitted, out error, true, Settings.MaxFaceTilt)',
                      self.read('PartSculptor/PartSculptor.cs'))

    def test_version_and_mirrored_docs(self):
        for relative in ['PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version']:
            self.assertEqual(json.loads(self.read(relative))['VERSION']['BUILD'], 137)
        self.assertIn('AssemblyVersion("0.0.4.137")', self.read('PartSculptor/AssemblyVersion.cs'))
        self.assertEqual(self.read('README.md'), self.read('GameData/PartSculptor/README.md'))
        self.assertIn('maxFaceTilt = 2.5', self.read('README.md'))

if __name__ == '__main__':
    unittest.main()
