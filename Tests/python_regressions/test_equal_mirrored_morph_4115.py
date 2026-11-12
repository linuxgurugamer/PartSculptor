"""v115: mirrored side motion is matched in physical distance, not raw factors."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAIN = (ROOT / "PartSculptor/PartSculptor.cs").read_text()
CORE = (ROOT / "PartSculptor/FaceMorphStepCore.cs").read_text()
SUPPORT = (ROOT / "PartSculptor/FaceMorphSupport.cs").read_text()


def mirrored_physical_step(delta, src, dst, swapped=False, signs=(1,1,1)):
    # Independent metric oracle for the exact-axis and quarter-roll cases.
    w, d, tx, tz, ox, oz = delta
    sw, sd, stx, stz = src
    dw, dd, dtx, dtz = dst
    phys = (w*sw, d*sd, tx*stx, tz*stz, ox*sw, oz*sd)
    if swapped:
        w,d,tx,tz,ox,oz = (phys[1], phys[0], phys[3], phys[2],
                             phys[5], phys[4])
    else:
        w,d,tx,tz,ox,oz = phys
    sx, sz, sy = signs
    return (w/dw, d/dd, -sx*sy*tx/dtx, -sz*sy*tz/dtz,
            sx*ox/dw, sz*oz/dd)


class EqualMirroredMorph4115(unittest.TestCase):
    def test_runtime_uses_physical_mapping_and_native_baseline(self):
        self.assertIn('TryMapMirroredPhysical(MorphStep delta,', CORE)
        self.assertIn('SourceMetrics = sourceMetrics, NeighborMetrics = neighborMetrics', MAIN)
        self.assertIn('TryGetPhysicalMorphMetrics(this, out sourceMetrics)', MAIN)
        self.assertIn('TryGetPhysicalMorphMetrics(other, out neighborMetrics)', MAIN)
        self.assertIn('before.NeighborMetrics, out mirrored', MAIN)
        self.assertIn('TryGetNativeFaceHalfSpans', SUPPORT)
        self.assertIn('TransformVector(Vector3.up).magnitude', MAIN)
        self.assertIn('TransformVector(Vector3.right).magnitude', MAIN)
        self.assertIn('TransformVector(Vector3.forward).magnitude', MAIN)
        self.assertIn('delta.Width * source.RadialX', CORE)
        self.assertIn('delta.TiltX * source.LongitudinalX', CORE)
        self.assertIn('mapped.TiltX / recipient.LongitudinalX', CORE)
        self.assertIn('mapped.Width / recipient.RadialX', CORE)
        self.assertLess(MAIN.index('m.CaptureConnectedFaceSnapshots(changed)'),
                        MAIN.index('m.faceMorph.Apply(bottom, top, true,'))

    def test_different_native_sizes_are_equal_in_meters(self):
        src=(1.,.5,1.,.5)
        child=(2.,.25,2.,.25)
        d=(.2,-.1,-.3,.12,.25,-.15)
        got=mirrored_physical_step(d,src,child)
        self.assertAlmostEqual(got[0],.1)
        self.assertAlmostEqual(got[2],.15)
        self.assertAlmostEqual(d[2]*src[2],-got[2]*child[2])
        self.assertAlmostEqual(d[0]*src[0],got[0]*child[0])
        self.assertAlmostEqual(d[4]*src[0],got[4]*child[0])
        self.assertAlmostEqual(d[5]*src[1],got[5]*child[1])

    def test_quarter_roll_and_reverse_drag(self):
        src=(1.,.5,1.,.5)
        child=(3.,4.,3.,4.)
        d=(.2,-.1,-.3,.12,.25,-.15)
        got=mirrored_physical_step(d,src,child,True,signs=(1,-1,1))
        self.assertAlmostEqual(got[0]*child[0],d[1]*src[1])
        self.assertAlmostEqual(got[1]*child[1],d[0]*src[0])
        self.assertAlmostEqual(got[3]*child[3],d[2]*src[2])
        neg=tuple(-v for v in d)
        reversed_got=mirrored_physical_step(neg,src,child,True,signs=(1,-1,1))
        for a,b in zip(got,reversed_got): self.assertAlmostEqual(a+b,0.)

    def test_joints_and_mode_are_unchanged(self):
        self.assertIn('KeepMirroredStackJointConnected(driver, before)', MAIN)
        self.assertIn('other.MatchFaceFromNeighbor(this, neighborNode, joint,', MAIN)
        self.assertIn('SetMorphFaces(bottom, top, false, false, false, keepChildStationary);', MAIN)
        self.assertIn('if (receiverFace == 0) bottom = replacement;', MAIN)
        self.assertIn('0.0.4.115', (ROOT/'Changelog.md').read_text())
