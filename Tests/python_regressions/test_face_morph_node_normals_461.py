"""0.0.4.61: face-morph normals use inverse-transpose and preserve baseline."""
from pathlib import Path
import unittest
ROOT = Path(__file__).resolve().parents[2]
CORE = (ROOT / "PartSculptor/FaceMorphCore.cs").read_text()
MATH = (ROOT / "PartSculptor/FaceMorphMath.cs").read_text()
SUPPORT = (ROOT / "PartSculptor/FaceMorphSupport.cs").read_text()
MAIN = (ROOT / "PartSculptor/PartSculptor.cs").read_text()
RUNNER = (ROOT / "Tests/RegressionRunner/Program.cs").read_text()

class FaceMorphNodeNormals461(unittest.TestCase):
    def test_surface_normal_uses_cofactor_not_tangent(self):
        self.assertIn("internal static MorphPoint TransformNormal(", CORE)
        self.assertIn("MorphPoint nx = Cross(dy, dz);", CORE)
        self.assertIn("MorphPoint ny = Cross(dz, dx);", CORE)
        self.assertIn("MorphPoint nz = Cross(dx, dy);", CORE)
        self.assertIn("internal static Vector3 TransformNormal(", MATH)
    def test_nodes_use_absolute_original_orientation(self):
        self.assertIn("node.orientation = FaceMorphMath.TransformNormal(original.position,", SUPPORT)
        self.assertIn("original.orientation, min, max, bottom, top)", SUPPORT)
        self.assertNotIn("original.orientation * 0.05f", SUPPORT)
        self.assertIn("orientation = n.orientation", SUPPORT)
    def test_regular_stretch_rebases_before_next_face_morph(self):
        self.assertIn("node.orientation = prefabNodeOrientation[node.id];", MAIN)
        self.assertIn("part.srfAttachNode.orientation = prefabSrfOrientation;", MAIN)
        self.assertIn("faceMorph.RebaseNodes();", MAIN)
    def test_csharp_math_regression_cases(self):
        for term in ("tilted top node normal X", "bottom node unaffected by top tilt Y",
                     "opposite stack node normal X", "face translation never rotates top node",
                     "reset restores native attach node orientation",
                     "normal absolutely derived from original on every drag"):
            self.assertIn(term, RUNNER)
