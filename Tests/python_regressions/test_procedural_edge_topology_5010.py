"""Static integration contracts and independent edge topology references.
These do not execute Unity mesh cooking or the KSP editor.
"""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / 'PartSculptor'
BUILDER = (SRC / 'ProceduralMeshBuilder.cs').read_text()
UI = (SRC / 'ProceduralGeometryTool.cs').read_text()
SUPPORT = (SRC / 'FaceMorphSupport.cs').read_text()
MAIN = (SRC / 'PartSculptor.cs').read_text()


def area(a,b,c):
    return ((b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0]))/2


def split_reference(points, groups, edge, fraction):
    """Reference: retain separate indexed edges at seams, share otherwise."""
    points = list(points)
    start,end = (points[i] for i in edge)
    new_point = tuple(a+(b-a)*fraction for a,b in zip(start,end))
    cache, output = {}, []
    for group in groups:
        result = []
        for tri in group:
            for k in range(3):
                a,b,c = tri[k],tri[(k+1)%3],tri[(k+2)%3]
                if {points[a],points[b]} != {start,end}: continue
                key=tuple(sorted((a,b)))
                if key not in cache:
                    cache[key]=len(points); points.append(new_point)
                v=cache[key]
                result.extend([(a,v,c),(v,b,c)])
                break
            else: result.append(tri)
        output.append(result)
    return points,output,cache


class EdgeTopology5010(unittest.TestCase):
    def test_double_gizmo_and_shared_hit_length(self):
        self.assertIn('VertexGizmoLength = 84f',UI)
        self.assertIn('12f, VertexGizmoLength',UI)
        self.assertIn('tip = center + d * VertexGizmoLength',UI)
        self.assertIn('tip - d * 18f + side * 8f',UI)

    def test_shared_edge_preserves_winding_area_and_connectivity(self):
        p=[(0,0),(2,0),(1,1),(1,-1)]
        groups=[[(0,1,2)],[(1,0,3)]]
        q,g,cache=split_reference(p,groups,(0,1),.25)
        self.assertEqual(len(q),5)
        self.assertEqual(q[-1],(.5,0))
        self.assertEqual(len(cache),1)
        self.assertEqual(g,[[(0,4,2),(4,1,2)],[(1,4,3),(4,0,3)]])
        self.assertTrue(all(area(*(q[i] for i in t))>0 for group in g for t in group))
        self.assertAlmostEqual(sum(area(*(q[i] for i in t)) for group in g for t in group),2)
        for contract in ('foreach (List<int> triangles in submeshes)', 'EdgeKey(a, b)',
                         'data.Interpolate(a, b, u)', 'new[] { index, b, c }',
                         'j += 3; // both replacement triangles'):
            self.assertIn(contract,BUILDER)

    def test_material_seam_copies_share_one_edit_without_double_falloff(self):
        p=[(0,0),(2,0),(1,1),(2,0),(0,0),(1,-1)]
        q,g,cache=split_reference(p,[[(0,1,2)],[(3,4,5)]],(0,1),.5)
        self.assertEqual(len(cache),2)
        self.assertEqual(q[6],q[7])
        self.assertEqual(len(g[0]),2); self.assertEqual(len(g[1]),2)
        self.assertIn('SamePoint(pa, from) && SamePoint(pb, to)',BUILDER)
        self.assertIn('data.positions[index] = anchor;',BUILDER)
        self.assertIn('if (!softEdits.Add(binding.EditIndex)) continue;',SUPPORT)
        self.assertIn('if (!supportEdits.Add(binding.EditIndex)) continue;',SUPPORT)

    def test_ring_edge_picking_and_replay(self):
        self.assertIn('Add edge/ring vertex',UI)
        self.assertIn('camera, addingEdgeVertex',UI)
        self.assertIn('if (snapEdge || !ProceduralMeshBuilder.Interior(hitWeights))',SUPPORT)
        self.assertIn('best > 14f*14f',SUPPORT)
        self.assertIn('vertex.Anchor = Vector3.Lerp(picked.vertices',SUPPORT)
        self.assertLess(BUILDER.index('foreach (ProceduralRing ring in rings)'),BUILDER.index('var replayBindings'))
        self.assertIn('else if (edge < 0)',BUILDER)
        self.assertIn('if (endpoint >= 0)',BUILDER)
        self.assertIn('new ProceduralVertexBinding(e, i, i, i, i, Vector3.right)',BUILDER)
        self.assertIn('An editable vertex already exists at this point',BUILDER)

    def test_deletion_replays_remaining_edits_and_restores_native_topology(self):
        # Replaying just the second edge point after deletion does not retain the first.
        p=[(0,0),(2,0),(1,1),(1,-1)]; g=[[(0,1,2),(1,0,3)]]
        q,h,_=split_reference(p,g,(0,1),.5)
        q2,h2,_=split_reference(q,h,(0,4),.5)
        restored,faces,_=split_reference(p,g,(0,1),.25)
        self.assertEqual(q2[-1],restored[-1]); self.assertEqual(len(restored),5)
        self.assertNotIn((1,0),restored)
        self.assertEqual(len(faces[0]),4)
        self.assertIn('DeleteSelectedVertex(module)',UI)
        self.assertIn('foreach (int index in descending) updated.RemoveAt(index);',UI)
        self.assertIn('CommitVertices(new List<ProceduralVertex>())',UI)
        self.assertIn('ProceduralMeshBuilder.TryBuild(r.native',SUPPORT)
        self.assertIn('else prepared = UnityEngine.Object.Instantiate(r.native);',SUPPORT)
        self.assertIn('module.faceMorph.Apply(module.GetMorphFace(0), module.GetMorphFace(1), true);',MAIN)
        self.assertIn('module.RefreshProceduralResources();',MAIN)
        self.assertIn('EditorUndoSupport.RecordBeforeChange();',MAIN)
