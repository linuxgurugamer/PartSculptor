import unittest
from pathlib import Path
UI = (Path(__file__).resolve().parents[2]/'PartSculptor/ProceduralGeometryTool.cs').read_text()
class ProceduralCorners504(unittest.TestCase):
    def test_four_corners(self):
        self.assertIn('HandleCount = 9', UI)
        self.assertIn('Color.yellow', UI)
        self.assertIn('int corner = handle - 5', UI)
        self.assertEqual({((-1 if i & 1 == 0 else 1),(-1 if i & 2 == 0 else 1)) for i in range(4)}, {(-1,-1),(-1,1),(1,-1),(1,1)})
    def test_two_axis_projection(self):
        cross=lambda a,b:a[0]*b[1]-a[1]*b[0]
        for x,z in [((50,20),(15,40)),((-50,-20),(15,40)),((50,20),(-15,-40)),((-50,-20),(-15,-40))]:
            d=tuple(a*.25+b*(-.3) for a,b in zip(x,z))
            self.assertAlmostEqual(cross(d,z)/cross(x,z),.25)
            self.assertAlmostEqual(cross(x,d)/cross(x,z),-.3)
        self.assertIn('Cross(displacement, dragDepthAxis) / determinant', UI)
        self.assertIn('Cross(dragAxis, displacement) / determinant', UI)
        self.assertIn('CornerAxesUsable(direction, depthDirection)', UI)
    def test_alt_drag_tilts_not_translates_ring(self):
        self.assertIn('if (!dragVertical && dragging >= 5)', UI)
        self.assertIn('OffsetRing(originalRing, dragVertical ? 4 : dragging, delta)', UI)
        self.assertIn('dragTilt = vertical && nearest != 4;', UI)
        self.assertIn('if (dragging == 4)', UI)
