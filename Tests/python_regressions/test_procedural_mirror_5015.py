"""Source contracts and independent numerical checks (not Unity runtime tests)."""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
UI = (ROOT/'PartSculptor/ProceduralGeometryTool.cs').read_text()
SUPPORT = (ROOT/'PartSculptor/FaceMorphSupport.cs').read_text()

class Mirror5015(unittest.TestCase):
    def test_radius_sync_both_directions(self):
        for token in ('radiusSlider = typed;', 'radiusSliderLimit = Mathf.Max(radiusSliderLimit, typed)',
                      'GUILayout.HorizontalSlider(radiusSlider, 0f, radiusSliderLimit,',
                      'radiusText = FormatRadius(radiusSlider)',
                      'ProceduralMeshBuilder.ValidInfluenceRadius(typed)', 'Apply influence radius'):
            self.assertIn(token, UI)

    def test_reflect_native_anchor(self):
        lo, hi, p = [-2,-4,1], [4,6,5], [3,2,4]
        for axis in range(3):
            q = p.copy(); q[axis] = lo[axis]+hi[axis]-p[axis]
            self.assertAlmostEqual(q[axis]+p[axis], lo[axis]+hi[axis])
            q[axis] = lo[axis]+hi[axis]-q[axis]
            self.assertEqual(p,q)
        for token in ('target[axis] = lo[axis] + hi[axis] - anchor[axis]',
                      'r.nativeVertices', 'ProceduralSurfacePhysics.ClosestWeights',
                      'ActiveFallbackCompatible(owner, r)', 'if (distance > best',
                      'offset[axis] = -offset[axis]', 'if (match == null) return false;'):
            self.assertIn(token, SUPPORT)

    def test_reflected_motion_with_overlapping_soft_selection(self):
        # R = [[1, .5], [.5, 1]]. Normal response is [+1,-1], tangential [+1,+1].
        for desired, expected in [((1,-1),(2,-2)),((1,1),(2/3,2/3))]:
            a,b=desired
            x,y=(a-.5*b)/.75,(b-.5*a)/.75
            self.assertAlmostEqual(x,expected[0]); self.assertAlmostEqual(y,expected[1])
            self.assertAlmostEqual(x+.5*y,a); self.assertAlmostEqual(.5*x+y,b)
        self.assertIn('response[i, n] = desired == null ? 1d : desired[i]', SUPPORT)
        self.assertIn('compensated[dragMirrorAxis] = localDelta[dragMirrorAxis] * (float)dragMirrorWeights[i]',UI)
        self.assertIn('local[dragMirrorAxis] *= dragMirrorSigns[i]',UI)

    def test_pair_dedup_and_center_plane(self):
        self.assertIn('if (targets.Contains(index)) continue;',UI)
        self.assertIn('signs.Add(partner == index ? 0f : 1f)',UI)
        self.assertIn('signs.Add(-1f)',UI)
        self.assertIn('edits[i].Anchor.Equals(source.Anchor)', SUPPORT)

    def test_atomic_insert_and_existing_mirror(self):
        self.assertIn('if (mirrorAxis >= 0 && !AddMirror(module, updated, inserted)) return true;',UI)
        self.assertIn('if (updated.Count >= ProceduralMeshBuilder.MaxIndividualVertices)',UI)
        self.assertIn('Create missing mirrors',UI)
        self.assertIn('if (partner >= 0) return true;',UI)
        self.assertIn('updated.Add(mirrored); return true;',UI)

    def test_delete_radius_and_preview(self):
        self.assertIn('if (!TryMirrorTargets(module, out descending, out signs)) return;',UI)
        self.assertIn('if (TryMirrorTargets(module, out targets, out signs))',UI)
        self.assertIn('foreach (int index in targets)',UI)
        self.assertIn('if (draggingVertex >= 0) world += MirrorPreview(module, i);',UI)
