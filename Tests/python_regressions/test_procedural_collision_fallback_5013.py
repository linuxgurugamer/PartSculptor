"""Independent ranking reference and production source contracts; not a Unity test."""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
MORPH = (ROOT/'PartSculptor/FaceMorphSupport.cs').read_text()
SETTINGS = (ROOT/'PartSculptor/Settings.cs').read_text()
CFG = (ROOT/'GameData/PartSculptor/PluginData/Settings.cfg').read_text()


def compatible(a, b, modules):
    if a is None or b is None or a == b:
        return True
    for subtypes in modules:
        if (any(a in s for s in subtypes) and any(b in s for s in subtypes)
                and not any(a in s and b in s for s in subtypes)):
            return False
    return True


def select(visual, candidates, modules):
    # Candidate = (name, variant, active, eligible, valid_surface, distance)
    a, active = visual
    ranked = []
    for name, b, b_active, eligible, valid, distance in candidates:
        if not eligible or not valid:
            continue
        strict = active == b_active and (a is None or b is None or a == b)
        fallback = active and b_active and compatible(a, b, modules)
        if strict or fallback:
            ranked.append((0 if strict else 1, distance, name))
    return min(ranked)[2] if ranked else None


class CollisionFallback5013(unittest.TestCase):
    def test_strict_priority_and_nearest_valid_triangle(self):
        c = [('far', 'A', True, True, True, 9),
             ('near', 'A', True, True, True, 2),
             ('degenerate', 'A', True, True, False, 0),
             ('trigger', None, True, False, True, 0),
             ('coactive', 'B', True, True, True, 1)]
        self.assertEqual(select(('A', True), c, [[{'A','B'}]]), 'near')
        self.assertIn('for (int tier = 0; tier < 2; ++tier)', MORPH)
        self.assertIn('hit.DistanceSquared < best.DistanceSquared', MORPH)
        self.assertIn('no valid triangle', MORPH)
        self.assertIn('no closed convex baseline', MORPH)

    def test_fallback_to_coactive_distinct_transform(self):
        c = [('disabled strict', 'A', True, False, True, 0),
             ('wrong variant', 'C', False, True, True, 0),
             ('coactive', 'B', True, True, True, 2)]
        self.assertEqual(select(('A', True), c, [[{'A','B'}, {'C'}]]), 'coactive')
        self.assertIn('ActiveFallbackCompatible(visual.Record, collision.Record)', MORPH)
        self.assertIn('moduleSelectors.Add(selectors)', MORPH)

    def test_exclude_incompatible_b9_and_inactive_alternative(self):
        c = [('exclusive', 'B', True, True, True, 0),
             ('inactive', 'A', False, True, True, 0)]
        self.assertIsNone(select(('A', True), c, [[{'A'}, {'B'}]]))
        self.assertIsNone(select(('A', False), [('active shared', None, True, True, True, 0)], []))
        self.assertIn('if (hasA && hasB && !together) return false;', MORPH)
        self.assertIn('PG collision fallback deferred inactive visual', MORPH)
        self.assertIn('f.GetComponentInParent<Part>() != part', MORPH)

    def test_primitive_and_disabled_trigger_eligibility(self):
        self.assertIn('!r.collider.isTrigger && r.originalEnabled', MORPH)
        self.assertIn('r.collider.enabled || (r.primitive != null && r.primitive.enabled)', MORPH)
        self.assertIn('UsableCollisionBaseline(collision)', MORPH)

    def test_generation_is_opt_in_and_preflight_has_no_live_mutation(self):
        self.assertIn('GenerateVertexCollisionFallback = false', SETTINGS)
        self.assertIn('generateVertexCollisionFallback = false', CFG)
        self.assertIn('TryGetValue("generateVertexCollisionFallback", ref GenerateVertexCollisionFallback)', SETTINGS)
        prep = MORPH.split('private bool PrepareVertexPhysics',1)[1].split('private void MoveNodeWithVertices',1)[0]
        self.assertIn('if (!Settings.GenerateVertexCollisionFallback)', prep)
        self.assertIn('ProceduralConvexHull.TryPair(visual.Surface.Base, visual.Surface.Edited', prep)
        self.assertIn('before += shape.GeneratedBaselineVolume;', prep)
        self.assertIn('after += shape.GeneratedCollision.Volume();', prep)
        self.assertNotIn('AddComponent<MeshCollider>', prep)
        self.assertNotIn('sharedMesh =', prep)

    def test_generation_commit_and_cleanup(self):
        self.assertIn('if (r.filter != null) ApplyGeneratedCollision(shape);', MORPH)
        self.assertIn('if (shape.GeneratedCollision == null)\n            { RemoveGeneratedCollision(r); return; }', MORPH)
        self.assertIn('r.generatedCollider.convex = true;', MORPH)
        self.assertIn('r.generatedCollider.sharedMesh = null;', MORPH)
        self.assertIn('RemoveCopiedGeneratedColliders();', MORPH)
        self.assertIn('if (r.generatedMesh != null) UnityEngine.Object.Destroy(r.generatedMesh);', MORPH)
        self.assertIn('RemoveGeneratedCollision(r);', MORPH.split('internal void Dispose()',1)[1])

    def test_diagnostics_include_unregistered_colliders(self):
        self.assertIn('PG collision inventory:', MORPH)
        self.assertIn('excluded trigger', MORPH)
        self.assertIn('excluded disabled', MORPH)
        self.assertIn('excluded missing mesh', MORPH)
        self.assertIn('incompatible B9 variant or activation', MORPH)
        self.assertIn('candidate distance=', MORPH)
        self.assertIn('visual hull fallback disabled', MORPH)

if __name__ == '__main__': unittest.main()
