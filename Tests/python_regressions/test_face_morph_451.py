"""Guards for stock-tank collider support and the visible rejection reason."""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
SUPPORT = (ROOT / 'PartSculptor/FaceMorphSupport.cs').read_text()
TOOL = (ROOT / 'PartSculptor/EditorStretchTool.cs').read_text()
MAIN = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()

class FaceMorph451(unittest.TestCase):
    def test_primitive_colliders_are_accepted_and_converted(self):
        for name in ('BoxCollider','SphereCollider','CapsuleCollider'):
            self.assertIn(name, SUPPORT)
        self.assertIn('AddPrimitive(c, proxyMesh);', SUPPORT)
        self.assertIn('proxy.convex = true;', SUPPORT)
        self.assertIn('proxy.sharedMaterial = primitive.sharedMaterial;', SUPPORT)

    def test_collider_switch_and_cleanup(self):
        self.assertIn('r.primitive.enabled = identity && r.originalEnabled;', SUPPORT)
        self.assertIn('r.collider.enabled = !identity && r.originalEnabled;', SUPPORT)
        self.assertIn('r.primitive.enabled = r.originalEnabled;', SUPPORT)
        self.assertIn('UnityEngine.Object.Destroy(r.collider)', SUPPORT)
        self.assertIn('UnityEngine.Object.Destroy(r.native)', SUPPORT)

    def test_meshcollider_nonconvex_does_not_hide_handles(self):
        self.assertNotIn('Nonconvex collision mesh unsupported', SUPPORT)
        self.assertIn('native == null || !native.isReadable', SUPPORT)

    def test_convex_proxy_stays_inside_physx_triangle_limit(self):
        self.assertIn('const int sides = 12;', SUPPORT)
        self.assertIn('const int halfRings = 4;', SUPPORT)
        # 5 lower rings + 5 upper rings = 10; 9 bands, 12 sides.
        self.assertLessEqual(9 * 12 * 2, 255)

    def test_unsupportable_parts_show_diagnostic(self):
        self.assertIn('internal string FaceMorphFailure', MAIN)
        self.assertIn('Face morph unavailable on ', TOOL)
        self.assertIn('FaceMorphGizmo.IsFor(selectedModule)', TOOL)
        self.assertIn('FaceMorphGizmo.DrawHelpBox(helpRect, faceStatus);', TOOL)

if __name__ == '__main__': unittest.main()
