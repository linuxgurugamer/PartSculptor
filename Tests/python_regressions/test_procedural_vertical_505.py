import unittest
from pathlib import Path
UI = (Path(__file__).resolve().parents[2]/'PartSculptor/ProceduralGeometryTool.cs').read_text()
class ProceduralVertical505(unittest.TestCase):
    def test_vertical_projection_precedes_corner_check(self):
        section=UI.split('private bool UpdateHandlesInternal',1)[1].split('int nearest = -1;',1)[1].split('if (nearest < 0)',1)[0]
        self.assertLess(section.index('if (vertical && i != 4)'),section.index('else if (i >= 5)'))
        self.assertIn('ProjectHandle(module, camera, OffsetRing(ring, 4, step), i, out shifted)',section)
    def test_mode_latched_and_height_clamped(self):
        self.assertIn('dragVertical = vertical;',UI)
        self.assertIn('if (dragging == 4)',UI)
        self.assertIn('dragTilt = vertical && nearest != 4;',UI)
        self.assertIn('bool vertical = AltHeld();',UI)
        self.assertIn('OffsetRing(originalRing, dragVertical ? 4 : dragging, delta)',UI)
        self.assertIn('else if (handle == 4) ring.Height += delta;',UI)
