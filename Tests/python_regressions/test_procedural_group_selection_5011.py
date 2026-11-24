"""Group-selection contracts and independent linear-response examples.
These do not execute Unity/KSP or the C# solver.
"""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
UI = (ROOT/'PartSculptor/ProceduralGeometryTool.cs').read_text()
SUPPORT = (ROOT/'PartSculptor/FaceMorphSupport.cs').read_text()
MAIN = (ROOT/'PartSculptor/PartSculptor.cs').read_text()


def solve_two(a,b,c,d):
    determinant = a*d-b*c
    return ((d-b)/determinant,(a-c)/determinant)


class GroupSelection5011(unittest.TestCase):
    def test_selection_controls_and_visibility(self):
        for token in ('KeyCode.LeftControl', 'KeyCode.RightControl',
                      'selectedVertices.Contains(index)', 'selectedVertices.Remove(index)',
                      'else selectedVertices.Add(index)', 'Select all visible', 'Deselect all',
                      'selectedVertices.RemoveAll', 'SameVertexIdentity',
                      'ChooseVertex(edits, nearest, false)', 'ResetVertexSelection(); addingVertex = false;'):
            self.assertIn(token, UI)
        self.assertIn('TryGetProceduralVertexWorld(i, out world, out mesh)', UI)
        self.assertIn('if (!Visible(r) || r.vertexBaselinePart == null) continue;', SUPPORT)

    def test_centroid_and_shared_preview(self):
        points = [(0,2,4),(2,4,6),(4,6,8)]
        center = tuple(sum(p[i] for p in points)/len(points) for i in range(3))
        self.assertEqual(center, (2,4,6))
        for token in ('center /= selectedVertices.Count;', 'TryGroupCenter(module, out group)',
                      'world += MirrorPreview(module, i);',
                      'world += preview / selectedVertices.Count;', 'VertexGizmoLength = 84f'):
            self.assertIn(token, UI)

    def test_hard_parent_dependency_compensation(self):
        # A later insertion inherits 25% of its parent's hard offset.
        a,b=solve_two(1,0,.25,1)
        self.assertEqual((a,b),(1,.75))
        self.assertEqual(.25*a+b,1)
        self.assertIn('inherited[b.Index] = inherited[b.A] * b.Weights.x', SUPPORT)
        self.assertIn('response[row, col] = inherited[indices[row]];', SUPPORT)

    def test_overlapping_soft_selection(self):
        # Two selected edits influence each other by 50%; do not move 150%.
        a,b=solve_two(1,.5,.5,1)
        self.assertAlmostEqual(a,2/3)
        self.assertAlmostEqual(b,2/3)
        self.assertAlmostEqual(a+.5*b,1)
        self.assertAlmostEqual(.5*a+b,1)
        self.assertIn('ProceduralMeshBuilder.InfluenceWeight(', SUPPORT)
        self.assertIn('if (owners[row] == r)', SUPPORT)
        self.assertIn('Math.Abs(divisor) < 1e-7d', SUPPORT)
        self.assertIn('Math.Abs(value) > 1000d', SUPPORT)
        self.assertIn('response[i, n] = desired == null ? 1d : desired[i];', SUPPORT)

    def test_one_commit_and_cancel(self):
        section=UI.split('private bool UpdateVertexHandles',1)[1].split('SyncSelection(module, edits);',1)[0]
        self.assertEqual(section.count('CommitVertices(updated)'),1)
        self.assertIn('new List<ProceduralVertex>(dragEdits)',section)
        self.assertIn('dragTransforms[i].InverseTransformVector(vertexGroupDelta)',section)
        self.assertIn('SameVertexList(dragEdits, edits, true)',section)
        self.assertIn('if (!Input.GetMouseButton(0))',section)
        self.assertIn('CancelDrag(); return true;',section)
        self.assertIn('dragWeights[i]',section)
        self.assertIn('EditorUndoSupport.RecordBeforeChange()',MAIN)

    def test_group_delete_and_radius(self):
        edits=list(range(6))
        for i in sorted([1,4,2], reverse=True): del edits[i]
        self.assertEqual(edits,[0,3,5])
        self.assertIn('descending.Sort((a, b) => b.CompareTo(a))',UI)
        self.assertIn('foreach (int index in descending) updated.RemoveAt(index);',UI)
        self.assertIn('foreach (int index in selectedVertices)',UI)
        self.assertIn('Applied radius: mixed',UI)
        self.assertIn('current.InfluenceRadius = radius;',UI)
