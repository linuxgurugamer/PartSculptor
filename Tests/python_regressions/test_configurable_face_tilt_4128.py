"""Face tilt limit is consistently loaded and enforced for gizmo and matching."""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

class ConfigurableFaceTilt128(unittest.TestCase):
    def source(self, name):
        return (ROOT/name).read_text()

    def test_config_default_and_documentation(self):
        cfg = self.source('GameData/PartSculptor/PluginData/Settings.cfg')
        self.assertIn('maxFaceTilt = 2.5', cfg)
        self.assertIn('not an angle in degrees', cfg)
        self.assertIn('Restart KSP', cfg)
        self.assertIn('maxFaceTilt = 2.5', self.source('README.md'))
        self.assertIn('DefaultMaxFaceTilt = 2.5f', self.source('PartSculptor/Settings.cs'))
        self.assertEqual(self.source('README.md'), self.source('GameData/PartSculptor/README.md'))

    def test_load_validates_limit(self):
        source=self.source('PartSculptor/Settings.cs')
        self.assertIn('public static float MaxFaceTilt = DefaultMaxFaceTilt;', source)
        self.assertIn('n.TryGetValue("maxFaceTilt", ref MaxFaceTilt);', source)
        self.assertIn('!(MaxFaceTilt > 0f)', source)
        self.assertIn('float.IsInfinity(MaxFaceTilt)', source)
        self.assertIn('MaxFaceTilt > 4f', source)
        self.assertIn('MaxFaceTilt = DefaultMaxFaceTilt;', source)

    def test_both_drag_and_set_faces_share_limit(self):
        math=self.source('PartSculptor/FaceMorphMath.cs')
        self.assertIn('Mathf.Clamp(f.TiltX, -Settings.MaxFaceTilt, Settings.MaxFaceTilt)', math)
        self.assertIn('Mathf.Clamp(f.TiltZ, -Settings.MaxFaceTilt, Settings.MaxFaceTilt)', math)
        self.assertIn('FaceMorphMath.Clamp(f)', self.source('PartSculptor/FaceMorphGizmo.cs'))
        part=self.source('PartSculptor/PartSculptor.cs')
        self.assertIn('bottom = FaceMorphMath.Clamp(bottom)',part)
        self.assertIn('top = FaceMorphMath.Clamp(top)',part)

    def test_attached_face_match_uses_same_configured_bound(self):
        core=self.source('PartSculptor/FaceAttachmentMatchCore.cs')
        part=self.source('PartSculptor/PartSculptor.cs')
        self.assertIn('Math.Abs(tiltX) > maxFaceTilt',core)
        self.assertIn('Math.Abs(tiltZ) > maxFaceTilt',core)
        self.assertIn('double maxFaceTilt = 2.5d', core)
        self.assertIn('out fitted, out error, true, Settings.MaxFaceTilt)',part)
        runner=self.source('Tests/RegressionRunner/Program.cs')
        self.assertIn('ConfigurableFaceTiltFitting();',runner)
        self.assertIn('configured 1.5 tilt limit must fit steep face', runner)

    def test_version(self):
        for name in ['PartSculptor.version','GameData/PartSculptor/PartSculptor.version']:
            self.assertEqual(json.loads(self.source(name))['VERSION']['BUILD'],130)
        self.assertIn('AssemblyVersion("0.0.4.130")',self.source('PartSculptor/AssemblyVersion.cs'))

if __name__ == '__main__': unittest.main()
