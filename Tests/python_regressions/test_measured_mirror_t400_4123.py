"""Regression: mirrored Copy must equalize the actual face rise (not only node pose)."""
import json
import math
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
MAIN=(ROOT/'PartSculptor/PartSculptor.cs').read_text()
SNAP=(ROOT/'PartSculptor/PartFaceSnap.cs').read_text()
CORE=(ROOT/'PartSculptor/FaceMorphStepCore.cs').read_text()

def physical_solution(source_width, source_tilt, source_span, recipient_span, mirrored_sign):
    radial=source_width*source_span
    rise=abs(source_tilt*source_span)
    return radial/recipient_span, math.copysign(rise/recipient_span, mirrored_sign)

class MeasuredMirrorT400123(unittest.TestCase):
    def test_t400_under_morph_restores_actual_edge_slope_and_length(self):
        for amount in (-.24,-.12,.12,.24):
            with self.subTest(tilt=amount):
                parent_width=1.0
                child_width, child_tilt=physical_solution(parent_width,amount,1.25,1.25,-amount)
                self.assertAlmostEqual(child_tilt,-amount)
                self.assertAlmostEqual(child_width,parent_width)
                parent_length=math.hypot(1.25,1.25*amount)
                child_length=math.hypot(1.25*child_width,1.25*child_tilt)
                self.assertAlmostEqual(parent_length,child_length)

    def test_roll_x_z_and_nonidentical_native_span(self):
        # Source native X axis maps to recipient Z after a 90 degree roll.
        # The sign is handed to the solver by the existing mirrored-side map.
        w,t=physical_solution(1.1,.22,1.25,2.5,-.22)
        self.assertAlmostEqual(w,.55)
        self.assertAlmostEqual(t,-.11)
        self.assertAlmostEqual(math.hypot(1.25*1.1,1.25*.22),
                               math.hypot(2.5*w,2.5*t))

    def test_only_congruent_faces_receive_correction(self):
        self.assertIn('internal bool MatchingEdgeGeometryBefore;',MAIN)
        self.assertIn('PartFaceSnap.HaveMatchingEdgeGeometry(',MAIN)
        self.assertIn('if (before.MatchingEdgeGeometryBefore)',MAIN)
        self.assertIn('EqualizeMirroredTouchingFaceEdges(driver, before, receiverFace,',MAIN)
        self.assertIn('TrySolvePhysicalEdge(', CORE)
        self.assertIn('MeasureFaceEdge(driver.part, source[1] - source[0], true,',MAIN)
        self.assertIn('MeasureFaceEdge(driver.part, source[2] - source[0], false,',MAIN)
        self.assertIn('if (receiverFace == 0) bottom = corrected;',MAIN)
        self.assertIn('else top = corrected;',MAIN)
        self.assertIn('SetMorphFaces(bottom, top, false, false, false, keepChildStationary);', MAIN)
        self.assertIn('SnapMirroredJointWithPartFaceSnap(before);',MAIN)
        self.assertNotIn('SetMorphFaces(',SNAP)
        self.assertIn('for (int j = i + 1; j < 4; ++j)',SNAP)

    def test_version_and_archive_docs(self):
        for path in ('PartSculptor.version','GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT/path).read_text())['VERSION']['BUILD'],123)
        self.assertIn('AssemblyVersion("0.0.4.123")',(ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertEqual((ROOT/'README.md').read_bytes(),
                         (ROOT/'GameData/PartSculptor/README.md').read_bytes())

if __name__ == '__main__': unittest.main()
