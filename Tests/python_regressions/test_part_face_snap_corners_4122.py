"""v122: numeric tilted-corner rigid fit and pre-drag correspondence tests.

Production C# uses Unity quaternions; these independent vector calculations
exercise the exact face-edge frame construction and its geometric invariants.
"""
import itertools
import json
import math
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAIN = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()
SNAP = (ROOT / 'PartSculptor/PartFaceSnap.cs').read_text()
SUP = (ROOT / 'PartSculptor/FaceMorphSupport.cs').read_text()


def plus(a, b): return tuple(x + y for x, y in zip(a, b))
def minus(a, b): return tuple(x - y for x, y in zip(a, b))
def mul(a, s): return tuple(x * s for x in a)
def dot(a, b): return sum(x * y for x, y in zip(a, b))
def cross(a, b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
def norm(a): return math.sqrt(dot(a, a))
def unit(a): return mul(a, 1.0/norm(a))
def dist(a, b): return norm(minus(a, b))
def center(c): return tuple(sum(p[k] for p in c)/4 for k in range(3))

def corners(tx=0.0, tz=0.0, span=1.25):
    r=span/2
    return [(x, x*tx+z*tz, z) for z in (-r,r) for x in (-r,r)]

def rot_y(p, ang):
    c,s=math.cos(ang),math.sin(ang)
    return (c*p[0]+s*p[2],p[1],-s*p[0]+c*p[2])

def rotate_axis(p, axis, ang):
    c,s=math.cos(ang), math.sin(ang)
    return plus(plus(mul(p,c),mul(cross(axis,p),s)),mul(axis,dot(axis,p)*(1-c)))

def rotate_between(a, b, p):
    a,b=unit(a),unit(b)
    axis=cross(a,b)
    length=norm(axis)
    if length < 1e-12:
        return p if dot(a,b)>0 else mul(p,-1)
    return rotate_axis(p, mul(axis, 1/length), math.atan2(length,dot(a,b)))

def frame(c, idx):
    a,b,cc,d=(c[k] for k in idx)
    x=unit(plus(minus(b,a),minus(d,cc)))
    z=plus(minus(cc,a),minus(d,b))
    z=unit(minus(z,mul(x,dot(x,z))))
    y=unit(cross(z,x))
    return x,y,z

def map_corners(p,c):
    return min(itertools.permutations(range(4)), key=lambda perm:
               sum(dist(p[i],c[perm[i]])**2 for i in range(4)))

def rigid_fit(p,c, mapping):
    px,py,pz=frame(p,range(4))
    cx,cy,cz=frame(c,mapping)
    def spin(v):
        return plus(plus(mul(px,dot(cx,v)), mul(py,dot(cy,v))), mul(pz,dot(cz,v)))
    pc,cc=center(p),center(c)
    aligned=[plus(pc,spin(minus(q,cc))) for q in c]
    return max(dist(p[i],aligned[mapping[i]]) for i in range(4)), aligned

class CornerSnap122(unittest.TestCase):
    def test_has_snapshot_of_corner_identity_before_transform_changes(self):
        self.assertIn('internal int[] ParentCornerToChild;', MAIN)
        self.assertIn('PartFaceSnap.TryBuildCornerMap(parentCorners, childCorners,', MAIN)
        self.assertLess(MAIN.index('m.CaptureConnectedFaceSnapshots(changed)'),
                        MAIN.index('m.morphBottomX = bottom.Width;'))
        self.assertIn('TryGetWorldFaceCorners(', SUP)
        self.assertIn('Corner(face, i, bottom, top)', SUP)
        self.assertIn('PartFaceSnap.SnapCorners(before.JointParent, before.JointChild,', MAIN)
        self.assertIn('Quaternion.LookRotation(sourceZ, sourceUp)',SNAP)
        self.assertIn('Quaternion.LookRotation(childZ, childUp)',SNAP)
        self.assertIn('Quaternion.Inverse(childFrame)', SNAP)
        self.assertIn('parentCorners[i]).magnitude > tolerance',SNAP)
        self.assertIn('PartFaceSnap.SnapFaces(before.JointParent, before.JointChild,', MAIN)
        self.assertNotIn('SetMorphFaces(',SNAP)
        self.assertNotIn('FaceAttachmentMatchCore.TryFit(',SNAP)

    def test_t400_mirrored_tilt_preserves_full_edges(self):
        for tx,tz in ((0.05,0),(0.25,0),(-0.25,0),(0,0.20),(0,-0.25),
                      (0.19,0.11),(-0.2,0.15)):
            with self.subTest(tilt=(tx,tz)):
                p0=corners()
                mapping=map_corners(p0,p0)
                p=corners(tx,tz)
                c=corners(-tx,-tz)
                # KSP may already rotate the whole child once to follow the
                # parent's changing top-node normal. The fit must apply only
                # the missing rigid rotation, not reduce the morph parameters.
                n=unit(cross(minus(p[2],p[0]), minus(p[1],p[0])))
                c=[rotate_between((0,1,0),n,q) for q in c]
                residual, out=rigid_fit(p,c,mapping)
                self.assertLess(residual,1e-8)
                self.assertAlmostEqual(dist(out[0],out[1]),dist(p[0],p[1]),places=8)
                self.assertAlmostEqual(dist(out[0],out[2]),dist(p[0],p[2]),places=8)

    def test_same_visible_side_for_quarter_turn_rolls(self):
        for roll in (0,90,180,270):
            with self.subTest(roll=roll):
                a=math.radians(roll)
                flat=corners()
                initial=[rot_y(q,a) for q in flat]
                mapping=map_corners(flat,initial)
                # Use visible local axis to construct the mirrored parameter
                # after a roll; a 90-degree roll exchanges local X/Z.
                parent=corners(.21,.0)
                local_x,local_z=(.21,0) if roll in (0,180) else (0,.21)
                # Sign mapping determined by which local X/Z edge points at
                # the parent's +X direction in world coordinates.
                if roll==180: local_x=-local_x
                if roll==90: local_z=-local_z
                child=[rot_y(q,a) for q in corners(-local_x,-local_z)]
                n=unit(cross(minus(parent[2],parent[0]),minus(parent[1],parent[0])))
                child=[rotate_between((0,1,0),n,q) for q in child]
                residual,_=rigid_fit(parent,child,mapping)
                self.assertLess(residual,1e-8)

    def test_unequal_faces_cannot_be_fixed_with_rotation(self):
        p=corners(.22,0,span=1.25)
        c=corners(-.22,0,span=1.50)
        residual,_=rigid_fit(p,c,map_corners(corners(span=1.25),corners(span=1.5)))
        self.assertGreater(residual,0.01)

    def test_version(self):
        for name in ('PartSculptor.version','GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT/name).read_text())['VERSION']['BUILD'],123)
        self.assertIn('AssemblyVersion("0.0.4.123")',
                      (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertEqual((ROOT/'README.md').read_bytes(),
                         (ROOT/'GameData/PartSculptor/README.md').read_bytes())

if __name__ == '__main__': unittest.main()
