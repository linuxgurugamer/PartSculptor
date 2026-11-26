"""Source integration and independent reference checks; C# runner exercises production math."""
import unittest
from pathlib import Path
from collections import Counter

ROOT=Path(__file__).resolve().parents[2]
MAIN=(ROOT/'PartSculptor/PartSculptor.cs').read_text()
CORE=(ROOT/'PartSculptor/FaceMorphCore.cs').read_text()
RUNNER=(ROOT/'Tests/RegressionRunner/Program.cs').read_text()

class PartVolume0521(unittest.TestCase):
    def test_paw_readonly_in_both_scenes(self):
        self.assertIn('guiActiveEditor = true, guiActive = true, guiName = "Part volume"', MAIN)
        self.assertIn('public string displayPartVolume', MAIN)
        self.assertIn('RefreshPartVolumeDisplay();', MAIN.split('public void LateUpdate()')[1])
        body=MAIN.split('private void RefreshPartVolumeDisplay()')[1].split('void UpdateCalculatedValues()')[0]
        for token in ('part.PartActionWindow.gameObject.activeInHierarchy','nextPartVolumeRead','displayDirty = true','Settings.HidePartSculptorPAWControls'):
            self.assertIn(token,body)
    def test_current_visual_mesh_not_collision_hull_or_multiplier(self):
        body=MAIN.split('private void RefreshPartVolumeDisplay()')[1].split('void UpdateCalculatedValues()')[0]
        for token in ('GetComponentsInChildren<MeshFilter>(false)','renderer.enabled','filter.sharedMesh','mesh.vertices','mesh.GetTriangles(sub)','localToWorldMatrix.determinant','PartVolumeCore.Measure'):
            self.assertIn(token,body)
        for token in ('MeshCollider','StretchVolumeFactor','VolumeFactor','ApplyResourceScaling','TrySetProcedural'):
            self.assertNotIn(token,body)
    def test_closed_shells_and_nested_voids(self):
        for token in ('uses.Count!=2','if (!closed','depth%2==0?1d:-1d','BoundsContain','Contains(outer,shell','Math.Atan2','remap[i]=id'):
            self.assertIn(token,CORE)
        self.assertIn('measurement.Partial',MAIN)
        self.assertIn('measurement.ClosedShells > 1',MAIN)
        self.assertIn('"Unavailable"',MAIN)
    def test_production_math_runner_includes_cases(self):
        self.assertIn('Compile Include="FaceMorphCore.cs"', (ROOT/'PartSculptor/PartSculptor.csproj').read_text())
        self.assertIn('FaceMorphCore.cs', (ROOT/'Tests/RegressionRunner/RegressionRunner.csproj').read_text())
        for token in ('GeometricPartVolume();','sealed hollow cube excludes inner cavity','open-ended hollow tube wall volume','stretched and translated tube','open boundary is not filled','welded UV seams'):
            self.assertIn(token,RUNNER)
    def test_volume_types_are_in_existing_compilation_unit(self):
        self.assertIn('internal struct PartVolumeResult', CORE)
        self.assertIn('internal static class PartVolumeCore', CORE)
        self.assertIn('using System.Collections.Generic;', CORE)
        self.assertFalse((ROOT/'PartSculptor/PartVolumeCore.cs').exists())
        for name in ('PartSculptor/PartSculptor.csproj','Tests/RegressionRunner/RegressionRunner.csproj'):
            self.assertNotIn('PartVolumeCore.cs', (ROOT/name).read_text())
    def test_reference_hollow_tube_triangle_integral(self):
        # Direct independent signed tetrahedron sum for a closed wall surface.
        def ring(y,h): return [(-h,y,-h),(h,y,-h),(h,y,h),(-h,y,h)]
        v=ring(-1,1)+ring(1,1)+ring(-1,.5)+ring(1,.5)
        faces=[]
        def quad(a,b,c,d): faces.extend([(a,b,c),(a,c,d)])
        for i in range(4):
            j=(i+1)%4
            quad(i,j,4+j,4+i)
            quad(8+i,12+i,12+j,8+j)
            quad(i,8+i,8+j,j)
            quad(4+i,4+j,12+j,12+i)
        edges=Counter(tuple(sorted((t[i],t[(i+1)%3]))) for t in faces for i in range(3))
        self.assertTrue(all(n==2 for n in edges.values()))
        def det(a,b,c): return a[0]*(b[1]*c[2]-b[2]*c[1])-a[1]*(b[0]*c[2]-b[2]*c[0])+a[2]*(b[0]*c[1]-b[1]*c[0])
        self.assertAlmostEqual(abs(sum(det(v[a],v[b],v[c])/6 for a,b,c in faces)),6.0)
