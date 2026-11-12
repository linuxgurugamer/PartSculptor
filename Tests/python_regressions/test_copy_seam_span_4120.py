"""v120: a double-tilted FL-T400 has matching physical edge lengths."""
import json
import math
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MODULE = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
CORE = (ROOT/'PartSculptor/FaceMorphStepCore.cs').read_text()


def solve_width(axis2, axis_rise, rise2, target):
    disc = axis_rise*axis_rise - axis2*(rise2-target*target)
    if axis2 <= 0 or disc < 0:
        return None
    return (-axis_rise+math.sqrt(disc))/axis2

class CopySeamSpan120(unittest.TestCase):
    def test_two_t400_tilt_directions(self):
        diameter = 1.25
        for tilt in (-.60, -.30, -.10, .10, .30, .60):
            # Source edge is (diameter,diameter*tilt). Mirrored child has
            # the opposite local Y tilt, but must have the same 3D length.
            target = diameter*math.hypot(1, tilt)
            width = solve_width(diameter**2, 0,
                                (diameter*tilt)**2, target)
            self.assertAlmostEqual(width, 1.0)
            # A child with a 2% short width must regain its physical span.
            actual = diameter*math.hypot(.98, tilt)
            self.assertLess(actual,target)
            self.assertAlmostEqual(diameter*math.hypot(width,tilt),target)

    def test_two_rotation_mirrored_face_corners(self):
        # In the X/Y section the parent tangent is (1,+tilt); the
        # child's *local* mirrored tangent is (1,-tilt). The net body
        # turn must rotate this latter tangent onto the former. Two
        # equal FL-T400s then have exactly coincident end-edge corners.
        for tilt in (-.5,-.25,-.1,.1,.25,.5):
            theta=2*math.atan(tilt)
            c,s=math.cos(theta),math.sin(theta)
            for side in (-1,1):
                x,y=side*.625, -side*.625*tilt
                xrot=c*x-s*y
                yrot=s*x+c*y
                self.assertAlmostEqual(xrot,side*.625,places=10)
                self.assertAlmostEqual(yrot,side*.625*tilt,places=10)

    def test_roll_and_nonorthogonal_axes(self):
        # X/Z swaps do not change the formula, only the mapped target edge.
        for target_width,target_depth in [(1.4,.85),(.85,1.4)]:
            for native in [1.25,2.5]:
                for factor in [target_width,target_depth]:
                    wanted=native*math.hypot(factor,.25)
                    solved=solve_width(native*native,0,
                                       native*native*.25*.25,wanted)
                    self.assertAlmostEqual(solved,factor)
        # Nonuniform transform can yield a nonzero cross term.
        a,b,rise=4,.25,.36
        target=math.sqrt(a*1.15**2+2*b*1.15+rise)
        self.assertAlmostEqual(solve_width(a,b,rise,target),1.15)
        self.assertIsNone(solve_width(1,0,1,.5))

    def test_wired_only_to_live_copy_and_initially_comparable_faces(self):
        self.assertIn('ComparableEndFaces = comparable', MODULE)
        self.assertIn('TryMapFaceCorners(parentCorners, childCorners,', MODULE)
        self.assertIn('if (before.ComparableEndFaces && before.JointChild == part)', MODULE)
        self.assertIn('CorrectChildSeamSpan(before);', MODULE)
        self.assertIn('FaceMorphStepCore.TrySolveEdgeSpan(', MODULE)
        self.assertIn('Quaternion cornerTurn = parentFrame *', MODULE)
        self.assertIn('Quaternion.LookRotation(', MODULE)
        self.assertIn('Mathf.Abs(nextX - current.Width) > current.Width * 0.10f', MODULE)
        self.assertIn('current.TiltZ, current.OffsetX, current.OffsetZ',MODULE)
        self.assertIn('static bool TrySolveEdgeSpan(', CORE)
        self.assertLess(MODULE.index('CorrectChildSeamSpan(before);'),
                        MODULE.index('FollowMirroredJointAfterCopy(before);',
                                     MODULE.index('CorrectChildSeamSpan(before);')))

    def test_versions_docs_and_original_copy_behavior(self):
        for p in ('PartSculptor.version','GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT/p).read_text())['VERSION']['BUILD'],120)
        self.assertIn('AssemblyVersion("0.0.4.120")',
                      (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())
        self.assertEqual((ROOT/'README.md').read_bytes(),
                         (ROOT/'GameData/PartSculptor/README.md').read_bytes())
        self.assertIn('MorphStep proposed = MorphStep.Add(before.NeighborBefore, mirrored);',MODULE)
        self.assertIn('FaceMorphStepCore.TryMapMirrored(delta,',MODULE)
        self.assertIn('SetMorphFaces(bottom, top, false, false, false, keepChildStationary);',MODULE)
        self.assertIn('other.MatchFaceFromNeighbor(this, neighborNode, joint,',MODULE)
        self.assertIn('## 0.0.4.120', (ROOT/'Changelog.md').read_text())
        self.assertTrue((ROOT/'Tests/CORRECT_SEAM_SPAN_0.0.4.120.md').exists())

if __name__ == '__main__': unittest.main()
