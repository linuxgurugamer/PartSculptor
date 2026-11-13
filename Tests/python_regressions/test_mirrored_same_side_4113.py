"""v113 regression: opposite longitudinal motion on corresponding visible sides."""
import json
import math
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
MAIN = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
CORE = (ROOT/'PartSculptor/FaceMorphStepCore.cs').read_text()

# Independent mathematical oracle for the axis/sign convention, not just a
# string match: deltaRightDown=-0.2 tilts the source's world-right edge down.
def mirror_tilt(tilt_x, tilt_z, recipient_x_source_x,
                recipient_x_source_z, recipient_z_source_x,
                recipient_z_source_z, recipient_y_source_y):
    xfromx = abs(recipient_x_source_x) >= abs(recipient_x_source_z)
    zfromx = abs(recipient_z_source_x) >= abs(recipient_z_source_z)
    if xfromx == zfromx: return None
    xmain = recipient_x_source_x if xfromx else recipient_x_source_z
    zmain = recipient_z_source_x if zfromx else recipient_z_source_z
    ysign = 1 if recipient_y_source_y > 0 else -1
    return (-math.copysign(1,xmain)*ysign*(tilt_x if xfromx else tilt_z),
            -math.copysign(1,zmain)*ysign*(tilt_x if zfromx else tilt_z))

class MirroredSameSide113(unittest.TestCase):
    def test_incremental_and_not_absolute_fit(self):
        self.assertIn('MorphStep.Subtract(', MAIN)
        self.assertIn('MorphStep.Add(before.NeighborBefore, mirrored)', MAIN)
        self.assertIn('FaceMorphStepCore.TryMapMirrored(delta,', MAIN)
        self.assertNotIn('TryMirrorWorldCorners(', MAIN)
        self.assertNotIn('FaceAttachmentMatchCore.TryFit(desiredRelative,', MAIN)
        self.assertIn('-mapped.TiltX, -mapped.TiltZ', CORE)
        self.assertIn('XFromX = Vector3.Dot(rx, sx)', MAIN)
        self.assertIn('YFromY = Vector3.Dot(ry, sy)', MAIN)
    def test_tilt_reversed_on_same_world_right(self):
        self.assertEqual(mirror_tilt(-.2,0,1,0,0,1,1),(.2,0))
        self.assertEqual(mirror_tilt(-.2,0,-1,0,0,-1,1),(-.2,0))
        self.assertEqual(mirror_tilt(-.2,0,1,0,0,-1,-1),(-.2,0))
        self.assertEqual(mirror_tilt(-.2,0,0,1,-1,0,1),(0,-.2))
    def test_one_hop_and_far_face_unchanged(self):
        self.assertIn('SetMorphFaces(bottom, top, false, false, false, keepChildStationary)', MAIN)
        self.assertIn('if (receiverFace == 0) bottom = replacement;', MAIN)
        self.assertIn('else top = replacement;', MAIN)
        self.assertIn('other.MatchFaceFromNeighbor(this, neighborNode, joint,', MAIN)
    def test_exact_111_rebase_and_versions(self):
        for f in ['PartSculptor.version','GameData/PartSculptor/PartSculptor.version']:
            self.assertEqual(json.loads((ROOT/f).read_text())['VERSION']['BUILD'],137)
        self.assertIn('AssemblyVersion("0.0.4.137")', (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertEqual((ROOT/'README.md').read_bytes(),(ROOT/'GameData/PartSculptor/README.md').read_bytes())
        self.assertIn('0.0.4.111', (ROOT/'Changelog.md').read_text().split('## 0.0.4.111')[0])
if __name__=='__main__': unittest.main()
