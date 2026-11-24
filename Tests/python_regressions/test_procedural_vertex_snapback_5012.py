"""Independent hull reference and source integration checks (not Unity execution)."""
import itertools
import math
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / 'PartSculptor'
MORPH = (SRC / 'FaceMorphSupport.cs').read_text()
HULL = MORPH.split('internal sealed class ProceduralConvexHull', 1)[1]
PHYSICS = (SRC / 'ProceduralSurfacePhysics.cs').read_text()
UI = (SRC / 'ProceduralGeometryTool.cs').read_text()
MAIN = (SRC / 'PartSculptor.cs').read_text()

def sub(a,b): return tuple(x-y for x,y in zip(a,b))
def dot(a,b): return sum(x*y for x,y in zip(a,b))
def cross(a,b): return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def norm(a): return math.sqrt(dot(a,a))
def mul(a,k): return tuple(x*k for x in a)

def hull_volume(points):
    # Independent incremental envelope reference, with an interior tetrahedron
    # and a horizon edge count. This does not execute the production C#.
    a=min(range(len(points)),key=lambda i: points[i][0])
    b=max(range(len(points)),key=lambda i:norm(sub(points[i],points[a])))
    line=sub(points[b],points[a])
    c=max(range(len(points)),key=lambda i:norm(cross(line,sub(points[i],points[a]))))
    n=cross(line,sub(points[c],points[a]))
    d=max(range(len(points)),key=lambda i:abs(dot(n,sub(points[i],points[a]))))
    inside=tuple(sum(points[i][j] for i in (a,b,c,d))/4 for j in range(3))
    def face(i,j,k):
        n=cross(sub(points[j],points[i]),sub(points[k],points[i]))
        n=mul(n,1/norm(n))
        if dot(n,sub(inside,points[i]))>0: j,k,n=k,j,mul(n,-1)
        return i,j,k,n
    faces=[face(*f) for f in [(a,b,c),(a,d,b),(a,c,d),(b,d,c)]]
    for i,p in enumerate(points):
        if i in (a,b,c,d):continue
        horizon={}; keep=[]
        for j,k,l,n in faces:
            if dot(n,sub(p,points[j]))<=1e-8:
                keep.append((j,k,l,n));continue
            for edge in [(j,k),(k,l),(l,j)]:
                key=tuple(sorted(edge));horizon[key]=horizon.get(key,0)+1
        faces=keep+[face(j,k,i) for (j,k),count in horizon.items() if count==1]
    o=points[0]
    volume=abs(sum(dot(sub(points[i],o),cross(sub(points[j],o),sub(points[k],o))) for i,j,k,n in faces)/6)
    return volume,len(faces)

class VertexSnapback5012(unittest.TestCase):
    def test_cube_surface_support_and_inward_dent(self):
        cube=list(itertools.product((-1.,1.),repeat=3))
        # Existing source winding cannot affect a point-cloud hull.
        self.assertAlmostEqual(hull_volume(cube+[(0.,0.,1.)])[0],8.)
        self.assertAlmostEqual(hull_volume(cube+[(0.,0.,1.5)])[0],8.+2./3.)
        self.assertAlmostEqual(hull_volume(cube+[(0.,0.,.5)])[0],8.)
        self.assertIn('ProceduralConvexHull.TryPair(shape.Surface.Base, shape.Surface.Edited',MORPH)

    def test_dense_cylindrical_surface_support(self):
        sides=32
        cylinder=[(math.cos(i*2*math.pi/sides),y,math.sin(i*2*math.pi/sides)) for y in (-1.,1.) for i in range(sides)]
        base,faces=hull_volume(cylinder+[(1.,0.,0.)])
        self.assertAlmostEqual(base,sides*math.sin(2*math.pi/sides))
        high,_=hull_volume(cylinder+[(1.3,0.,0.)])
        self.assertGreater(high,base)
        self.assertLessEqual(faces,252)

    def test_dense_hulls_have_common_bounded_sample(self):
        self.assertIn('private const int FaceLimit = 252',HULL)
        self.assertIn('Sample(before,indices); Sample(after,indices)',HULL)
        self.assertIn('if (faces.Count>FaceLimit) return false',HULL)
        self.assertIn('low.Add(before[i]); high.Add(after[i]);',HULL)
        self.assertIn('if (e.Count!=2) return false',HULL)
        self.assertIn('shape.CookedCollision = high',MORPH)
        self.assertIn('mesh.SetTriangles(collisionTriangles, 0)',MORPH)
        self.assertNotIn('convex && Triangles.Count / 3 + 2',PHYSICS)

    def test_rejection_and_unexpected_cancellation_visible(self):
        self.assertIn('"ProceduralGeometryTool.CommitVertices"',UI)
        self.assertIn('"ProceduralGeometryTool.DragCancelled"',UI)
        self.assertIn('": collision/volume: " + error',MAIN)
        self.assertIn('": topology: " + error',MAIN)

    def test_hull_compiled_in_same_unit_as_morph_support(self):
        import xml.etree.ElementTree as ET
        project = ET.parse(SRC/'PartSculptor.csproj')
        compiled = [item.attrib['Include'] for item in project.findall(
            './/{http://schemas.microsoft.com/developer/msbuild/2003}Compile')]
        self.assertEqual(compiled.count('FaceMorphSupport.cs'), 1)
        self.assertNotIn('ProceduralConvexHull.cs', compiled)
        self.assertFalse((SRC/'ProceduralConvexHull.cs').exists())
        self.assertEqual(sum((SRC/f.replace('\\','/')).read_text(encoding='utf-8-sig').count(
            'internal sealed class ProceduralConvexHull') for f in compiled), 1)
        self.assertIn('namespace PartSculptor', MORPH)
        self.assertIn('internal static bool TryPair(', HULL)


if __name__=='__main__': unittest.main()
