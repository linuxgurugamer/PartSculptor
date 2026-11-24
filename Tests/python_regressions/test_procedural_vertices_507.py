"""Static integration contracts plus independent geometry references.
No Unity runtime, collider cooking, or KSP editor input is executed here.
"""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'PartSculptor'
UI = (SOURCE / 'ProceduralGeometryTool.cs').read_text()
MAIN = (SOURCE / 'PartSculptor.cs').read_text()
SUPPORT = (SOURCE / 'FaceMorphSupport.cs').read_text()
BUILDER = (SOURCE / 'ProceduralMeshBuilder.cs').read_text()

def interpolate(a,b,c,w):
    return tuple(a[i]*w[0]+b[i]*w[1]+c[i]*w[2] for i in range(len(a)))

def signed_area(a,b,c):
    return ((b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0]))/2

class ProceduralVertices507(unittest.TestCase):
    def test_mode_and_modifier(self):
        for x in ('KeyCode.LeftAlt', 'KeyCode.RightAlt', 'bool vertical = AltHeld();',
                  'dragTilt = vertical && nearest != 4;', 'if (dragging == 4)',
                  'GUILayout.Toggle(!vertexMode, HelpContent("Rings"', 'GUILayout.Toggle(vertexMode, HelpContent("Vertices"',
                  'if (vertexMode) return UpdateVertexHandles(module);', '"Add vertex"',
                  'GUI.color = Color.yellow;', 'vertexVertical = AltHeld();',
                  'existing.TiltX, existing.TiltZ'):
            self.assertIn(x,UI)
        self.assertNotIn('KeyCode.LeftShift',UI)
        self.assertNotIn('KeyCode.RightShift',UI)
    def test_surface_selection_and_safety(self):
        for x in ('TryPickProceduralVertex(camera.ScreenPointToRay(mouse)',
                  'if (addingVertex)', 'Input.GetKeyDown(KeyCode.Escape)',
                  'Input.GetMouseButtonDown(1)', 'IsPointerOverPanel(mouse)'):
            self.assertIn(x,UI)
        for x in ('ProceduralMeshBuilder.RayTriangle(localRay, live[a], live[b], live[c]',
                  'distance /= localPerWorld;', 'if (distance >= closest) continue;',
                  'r.vertices[a] * weights.x', 'ProceduralMeshBuilder.Interior(hitWeights)',
                  'Another part is in front', 'renderer.enabled'):
            self.assertIn(x,SUPPORT)
        self.assertIn('MaxIndividualVertices = 64',BUILDER)
        self.assertIn('Vertex anchor lies on overlapping triangles',BUILDER)
    def test_split_preserves_winding_and_area(self):
        a,b,c=(0,0),(4,0),(0,6)
        v=interpolate(a,b,c,(.25,.25,.5))
        parts=[signed_area(a,b,v),signed_area(b,c,v),signed_area(c,a,v)]
        self.assertTrue(all(x>0 for x in parts))
        self.assertAlmostEqual(sum(parts),signed_area(a,b,c))
        self.assertEqual(v,(1,3))
        self.assertIn('triangles[found + 2] = index;',BUILDER)
        self.assertIn('new[] { b, c, index, c, a, index }',BUILDER)
        self.assertIn('positions[a] * w.x + positions[b] * w.y + positions[c] * w.z',BUILDER)
        for attr in ('normals','tangents','colors','uv[i]'):
            self.assertIn(f'{attr}[a] * w.x',BUILDER)
    def test_insert_on_deformed_triangle_and_ordered_child_binding(self):
        a,b,c=(0,0,0),(4,0,1),(0,6,3)
        v=interpolate(a,b,c,(.25,.25,.5))
        self.assertEqual(v,(1,3,1.75))
        moved=(v[0],v[1]+2,v[2])
        child=interpolate(a,b,moved,(.25,.25,.5))
        self.assertEqual(child,(1.5,2.5,1.125))
        self.assertIn('transformed[binding.Index] = transformed[binding.A] * w.x',SUPPORT)
        self.assertIn('edits[binding.EditIndex].Offset',SUPPORT)
        self.assertLess(BUILDER.index('foreach (ProceduralRing ring in rings)'),BUILDER.index('if (edits != null && vertexMeshKey != null)'))
        self.assertIn('key.Append("|V").Append(i)',SUPPORT)
    def test_craft_round_trip_and_undo_symmetry(self):
        for x in ('PARTSCULPTOR_PROCEDURAL_VERTEX','node.RemoveNodes(PROCEDURAL_VERTEX_NODE)',
                  'ReadB9Vector(vertexNode, "anchor", out anchor)',
                  'SaveB9Vector(vertexNode, "anchor", vertex.Anchor)',
                  'SaveB9Vector(vertexNode, "offset", vertex.Offset)',
                  'copy.proceduralVertices = new List<ProceduralVertex>(proceduralVertices)',
                  'faceMorph.SetProceduralGeometry(proceduralRings, proceduralVertices, out proceduralError)',
                  'module.faceMorph.CanSetProceduralGeometry(ordered, individual, out error)',
                  'module.proceduralVertices = new List<ProceduralVertex>(individual)'):
            self.assertIn(x,MAIN)
        section=MAIN.split('private bool TrySetProceduralGeometry',1)[1].split('internal MorphFace GetMorphFace',1)[0]
        self.assertEqual(section.count('EditorUndoSupport.RecordBeforeChange()'),1)
        self.assertLess(section.index('CanSetProceduralGeometry'),section.index('EditorUndoSupport.RecordBeforeChange()'))
    def test_drag_commits_once_with_physics(self):
        section=UI.split('private bool UpdateVertexHandles',1)[1].split('private void DrawVertexHandles',1)[0]
        drag=section.split('if (draggingVertex >= 0)',1)[1].split('if (addingVertex &&',1)[0]
        self.assertEqual(drag.count('CommitVertices(updated)'),1)
        self.assertLess(drag.index('if (!Input.GetMouseButton(0))'),drag.index('CommitVertices(updated)'))
        self.assertIn('if (f != null) r.vertexMeshKey = VertexMeshKey(f, native);',SUPPORT)
        self.assertIn('if (r.filter == null) return false;',SUPPORT)
        self.assertIn('Release to update mesh, collider, affected attachment nodes and calculated volume',UI)
        self.assertIn('ProceduralMeshBuilder.VolumeFactor(ordered)',MAIN)
