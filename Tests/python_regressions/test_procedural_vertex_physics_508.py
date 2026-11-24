"""Source integration and independent reference math, not a Unity runtime test."""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT/'PartSculptor'
MAIN = (SRC/'PartSculptor.cs').read_text()
MORPH = (SRC/'FaceMorphSupport.cs').read_text()
PHYSICS = (SRC/'ProceduralSurfacePhysics.cs').read_text()

def minus(a,b): return tuple(x-y for x,y in zip(a,b))
def dot(a,b): return sum(x*y for x,y in zip(a,b))
def cross(a,b): return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def volume(points, faces):
    origin=points[0]
    return abs(sum(dot(minus(points[a],origin),cross(minus(points[b],origin),minus(points[c],origin))) for a,b,c in faces)/6)

class VertexPhysics508(unittest.TestCase):
    def test_volume_of_split_tetrahedron_and_pyramid(self):
        # Unit tetrahedron; displace a new vertex normal to the base face.
        p=[(0,0,0),(1,0,0),(0,1,0),(0,0,1),(1/3,1/3,0)]
        faces=[(0,2,4),(2,1,4),(1,0,4),(0,1,3),(1,2,3),(2,0,3)]
        self.assertAlmostEqual(volume(p,faces),1/6)
        p[4]=(1/3,1/3,-.5)
        self.assertAlmostEqual(volume(p,faces),.25)
        p[4]=(1/3,1/3,.25)
        self.assertAlmostEqual(volume(p,faces),.125)
        self.assertAlmostEqual(volume([(x+123,y-321,z+500) for x,y,z in p],faces),.125)
        self.assertIn('Math.Abs(sum / 6d)',PHYSICS)
        self.assertIn('volume = after / before;',MORPH)
    def test_collision_support_preserves_triangle_winding(self):
        self.assertIn('Triangles[i + 2] = index;',PHYSICS)
        self.assertIn('new[] { b, c, index, c, a, index }',PHYSICS)
        self.assertNotIn('Triangles.Count / 3 + 2 > ProceduralMeshBuilder.MaxConvexTriangles',PHYSICS)
        self.assertIn('ProceduralConvexHull.TryPair(shape.Surface.Base, shape.Surface.Edited',MORPH)
        self.assertIn('Edited[j] = Base[j] + displacement',PHYSICS)
        self.assertIn('CompatibleSurfaces(visual.Record, collision.Record)',MORPH)
        self.assertIn('collision.Surface.Edited[i] += hit.Delta;',MORPH)
    def test_collision_and_node_geometry_are_rebuilt_and_restored(self):
        self.assertIn('node.position += hit.Delta;',MORPH)
        self.assertIn('node.originalPosition += hit.Delta;',MORPH)
        self.assertIn('Quaternion.FromToRotation(hit.BeforeNormal, hit.AfterNormal)',MORPH)
        self.assertIn('if (individual) MoveNodeWithVertices(surfaces, node);',MORPH)
        self.assertIn('r.collider.sharedMesh = r.vertexCollider != null ? r.vertexCollider : r.instance;',MORPH)
        self.assertIn('if (previous != null) UnityEngine.Object.Destroy(previous);',MORPH)
        self.assertIn('if (!anyOffset) return true;',MORPH)
    def test_preflight_before_undo_and_resource_updates_after_geometry(self):
        commit=MAIN.split('private bool TrySetProceduralGeometry',1)[1].split('private void RefreshProceduralResources',1)[0]
        self.assertLess(commit.index('TryEvaluateVertexVolume'),commit.index('EditorUndoSupport.RecordBeforeChange'))
        self.assertLess(commit.index('module.faceMorph.Apply('),commit.index('module.RefreshProceduralResources()'))
        self.assertIn('ProceduralMeshBuilder.VolumeFactor(ordered) * vertexVolume',commit)
        self.assertIn('ProceduralMeshBuilder.VolumeFactor(proceduralRings) * proceduralVertexVolumeFactor',MAIN)
        apply=MAIN.split('void Apply(bool moveNeighbours',1)[1]
        self.assertLess(apply.index('proceduralVertexVolumeFactor = faceMorph.VertexVolumeFactor'),apply.index('b9Support.ApplyBaseVolumes'))
        refresh=MAIN.split('private void RefreshProceduralResources',1)[1].split('internal MorphFace GetMorphFace',1)[0]
        for text in ('b9Support.ApplyBaseVolumes','mftVolumeSupport.Apply','ApplyResourceScaling','exponentScaler.Apply','UpdateCalculatedValues'):
            self.assertIn(text,refresh)
    def test_load_before_resource_scaling_and_backward_compatible_cache(self):
        start=MAIN.split('public override void OnStartFinished',1)[1].split('nodeHistory.RecordFinal(part);',1)[0]
        self.assertLess(start.index('faceMorph.SetProceduralGeometry'),start.index('Apply(false, false, false, NodeApplyReason.Load)'))
        self.assertIn('proceduralVertexVolumeFactor = 1d;',MAIN)
        self.assertIn('node.GetValue("proceduralVertexVolumeFactor")',MAIN)
        self.assertIn('node.SetValue("proceduralVertexVolumeFactor"',MAIN)
        self.assertIn('<Compile Include="ProceduralSurfacePhysics.cs" />',(SRC/'PartSculptor.csproj').read_text())
