"""v0.0.5.23 source contract and independent geometric reference checks.

C# Unity mesh code must still be compiled and tested in KSP: these tests
cannot execute Mesh.SetTriangles or PhysX collision cooking.
"""
import json
import math
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAIN = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
SUPPORT = (ROOT/'PartSculptor/FaceMorphSupport.cs').read_text()
BUILDER = (ROOT/'PartSculptor/ProceduralMeshBuilder.cs').read_text()
EDITOR = (ROOT/'PartSculptor/EditorStretchTool.cs').read_text()
UI = (ROOT/'PartSculptor/ProceduralGeometryTool.cs').read_text()
PROJECT = (ROOT/'PartSculptor/PartSculptor.csproj').read_text()


def volume(rings):
    y0, w0, z0, result = 0.0, 1.0, 1.0, 0.0
    for y1, w1, z1 in rings + [(1., 1., 1.)]:
        result += (y1-y0)*(2*w0*z0 + w0*z1 + w1*z0 + 2*w1*z1)/6.
        y0,w0,z0 = y1,w1,z1
    return result

class ProceduralGeometry4149(unittest.TestCase):
    def test_version_and_compilation_units(self):
        for name in ('PartSculptor.version','GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(23, json.loads((ROOT/name).read_text())['VERSION']['BUILD'])
        self.assertIn('AssemblyVersion("0.0.5.23")',
            (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertIn('<Compile Include="ProceduralMeshBuilder.cs" />', PROJECT)
        self.assertIn('<Compile Include="ProceduralGeometryTool.cs" />', PROJECT)

    def test_seventh_widget_is_independent(self):
        for text in ('private void ToggleProcedural()', 'ToggleMode(false, true)',
                     'proceduralToolButton.onClick.AddListener(ToggleProcedural)',
                     'new GameObject("PartSculptor Procedural Geometry Tool"',
                     'glyph.text = "PG"',
                     'proceduralToolRect.position = toolRect.position + right.normalized',
                     'IncludeButtonBounds(proceduralToolRect,',
                     'if (proceduralMode && ProceduralGeometryTool.IsPointerOverPanel(',
                     'if (modeActive && proceduralMode)',
                     'ProceduralGeometryTool.Draw(helpRect, selectedModule)',
                     'if (proceduralToolObject != null) Destroy(proceduralToolObject)',
                     'if (proceduralToolObject != null) proceduralToolObject.SetActive(false)'):
            self.assertIn(text, EDITOR)
        self.assertIn('if (faceMode == morph && proceduralMode == procedural)', EDITOR)

    def test_real_triangle_subdivision_and_mesh_attributes(self):
        for text in ('private static List<int> Clip(', 'private static int Crossing(',
                     'private static long EdgeKey(',
                     'Triangulate(Clip(a, b, c, true,',
                     'Triangulate(Clip(a, b, c, false,',
                     'data.Interpolate(a, b,', 'mesh.SetVertices(data.positions)',
                     'mesh.SetTriangles(submeshes[i], i, false)',
                     'mesh.SetUVs(i, data.uv[i])',
                     'mesh.SetNormals(data.normals)', 'mesh.SetTangents(data.tangents)',
                     'mesh.SetColors(data.colors)',
                     'source.GetTopology(s) != MeshTopology.Triangles'):
            self.assertIn(text, BUILDER)

    def test_mesh_safety_and_ownership(self):
        for text in ('MaxRings = 6', 'MaxVertices = 65000',
                     'MaxConvexTriangles = 255', 'source.blendShapeCount > 0',
                     'source.isReadable', 'convexCollider && triangleCount > MaxConvexTriangles'):
            self.assertIn(text, BUILDER)
        for text in ('RebuildTopology(desired, false, out error, desiredVertices)',
                     'CanSetProceduralRings(', 'if (r.filter != null) r.filter.sharedMesh = replacement;',
                     'if (previous != null) UnityEngine.Object.Destroy(previous)',
                     'foreach (Mesh mesh in replacements)',
                     'r.nativeVertices', 'r.vertices = replacement.vertices',
                     'proceduralRings.Count != 0',
                     'ProceduralMeshBuilder.Profile(p.y,',
                     'FaceMorphMath.Transform(p, min, max, bottom, top)'):
            self.assertIn(text, SUPPORT)

    def test_persistence_and_editor_undo_symmetry(self):
        for text in ('PARTSCULPTOR_PROCEDURAL_RING',
                     'foreach (ConfigNode ringNode in node.GetNodes(PROCEDURAL_RING_NODE))',
                     'node.RemoveNodes(PROCEDURAL_RING_NODE)',
                     'node.AddNode(PROCEDURAL_RING_NODE)',
                     'copy.proceduralRings = new List<ProceduralRing>(proceduralRings)',
                     'if (HasFaceMorph || HasProceduralGeometry || inheritedMorphMeshes)',
                     'faceMorph.SetProceduralGeometry(proceduralRings,',
                     'EditorUndoSupport.RecordBeforeChange()',
                     'foreach (Part counterpart in part.symmetryCounterparts)',
                     'module.faceMorph.CanSetProceduralGeometry(ordered, individual, out error)',
                     'module.faceMorph.Apply(module.GetMorphFace(0), module.GetMorphFace(1), true)',
                     'FireEditorShipModified()'):
            self.assertIn(text, MAIN)
        self.assertIn('ProceduralMeshBuilder.VolumeFactor(proceduralRings)', MAIN)

    def test_ring_panel_user_controls(self):
        for text in ('Add ring','Remove ring','Clear all','Height (%)','X multiplier',
                     'Z multiplier','Apply ring changes', 'ProceduralAddedVertices',
                     'float.TryParse(', 'rings.Sort('):
            self.assertIn(text, UI)
        self.assertTrue((ROOT/'Tests/PROCEDURAL_GEOMETRY_0.0.4.149.md').exists())

    def test_volume_integral_reference(self):
        self.assertAlmostEqual(volume([]), 1.0)
        self.assertAlmostEqual(volume([(0.5,1,1)]), 1.0)
        self.assertAlmostEqual(volume([(0.5,2,2)]), 7./3.)
        self.assertAlmostEqual(volume([(0.5,0.5,0.5)]), 7./12.)
        self.assertAlmostEqual(volume([(0.3,1.2,.8),(.7,.9,1.4)]),
                               volume([(.3,1.2,.8),(.7,.9,1.4)]))

    def test_cross_section_keeps_end_faces_fixed_reference(self):
        def profile(y, rings):
            prevy, prevx, prevz = 0., 1., 1.
            for yy, xx, zz in rings+[(1.,1.,1.)]:
                if y <= yy:
                    t=(y-prevy)/(yy-prevy)
                    return (prevx+(xx-prevx)*t, prevz+(zz-prevz)*t)
                prevy,prevx,prevz=yy,xx,zz
        rings=[(.25,2.,.5),(.75,.7,1.8)]
        self.assertEqual(profile(0,rings),(1.,1.))
        self.assertEqual(profile(1,rings),(1.,1.))
        self.assertEqual(profile(.25,rings),(2.,.5))
        self.assertEqual(profile(.75,rings),(.7,1.8))
        self.assertIn('float end = i == rings.Count ? 1f', BUILDER)
        self.assertIn('float nextX = i == rings.Count ? 1f', BUILDER)

if __name__ == '__main__':
    unittest.main()
