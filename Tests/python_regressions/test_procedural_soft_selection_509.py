"""Offline integration contracts and reference falloff/axis math, not Unity tests."""
import math
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT/'PartSculptor'
UI = (SRC/'ProceduralGeometryTool.cs').read_text()
MAIN = (SRC/'PartSculptor.cs').read_text()
SUPPORT = (SRC/'FaceMorphSupport.cs').read_text()
BUILDER = (SRC/'ProceduralMeshBuilder.cs').read_text()

def weight(distance, radius):
    if radius <= 0 or distance >= radius:
        return 0
    t = 1 - distance/radius
    return t*t*(3-2*t)

class SoftSelection509(unittest.TestCase):
    def test_falloff_reference(self):
        self.assertEqual(weight(0, 1), 1)
        self.assertEqual(weight(.25, 1), .84375)
        self.assertEqual(weight(.5, 1), .5)
        self.assertEqual(weight(.75, 1), .15625)
        self.assertEqual(weight(1, 1), 0)
        self.assertEqual(weight(2, 1), 0)
        self.assertEqual(weight(0, 0), 0)
        self.assertIn('t * t * (3f - 2f * t)', BUILDER)
        self.assertIn('i == binding.Index ? 1f', SUPPORT)

    def test_soft_field_uses_immutable_part_space_and_no_double_offset(self):
        self.assertIn('edits[binding.EditIndex].InfluenceRadius <= 0f ?', SUPPORT)
        self.assertIn('toPart.MultiplyPoint3x4(baseline[binding.Index])', SUPPORT)
        self.assertIn('toPart.MultiplyPoint3x4(baseline[i]) - anchor', SUPPORT)
        self.assertIn('transformed[i] += edit.Offset * weight;', SUPPORT)
        # Two equal-position seam vertices get the same result.
        a=(.5, 0, 0); b=(.5, 0, 0)
        self.assertEqual(weight(math.dist(a,(0,0,0)),1),weight(math.dist(b,(0,0,0)),1))

    def test_axis_gizmo_and_constrained_motion(self):
        for text in ('VertexGizmoLength = 84f', 'Color.red, Color.green', 'PickVertexAxis(pointer, center, axes)',
                     'Vector3.right, Vector3.up, Vector3.forward', 'if (vertexDragAxis >= 0)',
                     'Vector2.Dot(displacement, axis) / axis.sqrMagnitude', 'DrawLine(center + d * 9f, tip)',
                     'GUI.color = Color.yellow;', 'vertexVertical = AltHeld();'):
            self.assertIn(text, UI)
        # Drag along a projected axis ignores the perpendicular displacement.
        axis=(30,40); displacement=(60,-45)
        self.assertEqual(sum(a*b for a,b in zip(axis,displacement))/sum(a*a for a in axis),0)
        self.assertEqual(sum(a*b for a,b in zip(axis,(60,80)))/sum(a*a for a in axis),2)

    def test_radius_persistence_validation_and_undo(self):
        for text in ('float radius = 0f;', 'vertexNode.HasValue("influenceRadius")',
                     'vertexNode.AddValue("influenceRadius", vertex.InfluenceRadius.ToString',
                     'new ProceduralVertex(meshKey, submesh, anchor, offset, radius)'):
            self.assertIn(text, MAIN)
        self.assertIn('float influenceRadius = 0f',BUILDER)
        self.assertIn('!float.IsInfinity(radius) && !float.IsNaN(radius)',BUILDER)
        self.assertIn('ValidInfluenceRadius(edit.InfluenceRadius)',SUPPORT)
        for text in ('GUILayout.Toggle(softSelection, HelpContent("Soft selection",', 'GUILayout.Label(HelpContent("Radius (m)"',
                     'GUILayout.Button("Apply influence radius")', 'current.InfluenceRadius = radius;',
                     'if (changed && CommitVertices(updated)) SelectVertex(updated, selectedVertex);'):
            self.assertIn(text, UI)

    def test_shared_physics_path(self):
        self.assertIn('result.Surface.Edited.Add(toPart.MultiplyPoint3x4(transformed[i]))',SUPPORT)
        self.assertIn('return PrepareVertexPhysics(surfaces, edits, out volume, out error);',SUPPORT)
        self.assertIn('collision.Surface.Edited[i] += hit.Delta;',SUPPORT)
        self.assertIn('if (individual) MoveNodeWithVertices(surfaces, node);',SUPPORT)
        self.assertIn('module.RefreshProceduralResources();',MAIN)
