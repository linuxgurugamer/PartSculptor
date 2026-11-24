"""B9 NODE compatibility tests (source wiring + native arithmetic model).

KSP, Unity, and B9PartSwitch are not available here: geometry must be
validated in the VAB. These tests assert that NODE no longer triggers the
morph-safety blanket rejection and that subtype-native geometry is used.
"""
from pathlib import Path
import json,re,unittest
ROOT=Path(__file__).resolve().parents[2]
B9=(ROOT/'PartSculptor/B9PartSwitchSupport.cs').read_text()
MAIN=(ROOT/'PartSculptor/PartSculptor.cs').read_text()
COMP=(ROOT/'PartSculptor/B9FaceMorphCompatibility.cs').read_text()

class B9NodeModifiers4145(unittest.TestCase):
    def test_version(self):
        for p in ('PartSculptor.version','GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT/p).read_text())['VERSION']['BUILD'],149)
        self.assertIn('AssemblyVersion("0.0.4.149")',(ROOT/'PartSculptor/AssemblyVersion.cs').read_text())

    def test_valid_node_is_not_blanket_blocked(self):
        self.assertIn('private static bool AllowsNodeModifier(',COMP)
        self.assertIn('string.Equals(nested.name, "NODE"', COMP)
        self.assertIn('AllowsNodeModifier(part, nested, where + " / NODE"',COMP)
        self.assertIn('"name", "position", "positionOffset", "size"',COMP)
        self.assertIn('B9PartSwitchSupport.NodeNameMatches(selector, attachNode.id)',COMP)
        self.assertIn('B9PartSwitchSupport.TryReadNodeVector', COMP)
        self.assertIn('!node.HasValue("size")', COMP)
        self.assertIn('if (!matches)', COMP)

    def test_unrelated_geometry_still_rejected(self):
        nested=COMP[COMP.index('foreach (ConfigNode nested in child.nodes)'):]
        self.assertNotIn('string.Equals(nested.name, "TRANSFORM"', nested)
        self.assertIn('string.Equals(nested.name, "MODULE"', nested)
        self.assertNotIn('string.Equals(nested.name, "MATERIAL"', nested)
        self.assertIn('contains geometry/unknown node',nested)
        # `node = xyz` hides or shows nodes; it's NOT the NODE stanza.
        subtype=COMP[COMP.index('HashSet<string> SubtypeFields'):COMP.index('HashSet<string> ResourceFields')]
        self.assertNotIn('"node"', subtype)

    def test_native_size_position_and_fallback(self):
        self.assertIn('TryGetConfiguredStackNode(Part part, string id,', B9)
        for term in ('GetNodes("SUBTYPE")','GetNodes("NODE")',
                     'NodeNameMatches(selector, id)','GetValue("position")',
                     'GetValue("positionOffset")','GetValue("size")',
                     'nativePosition = defaultPosition;', 'nativeSize = defaultSize;'):
            self.assertIn(term, B9)
        self.assertIn('TryGetConfiguredStackNode(part, node.id,',MAIN)
        self.assertIn('prefabNodeSize[node.id] = configuredSize;',MAIN)
        self.assertIn('history.NativeSize = configuredSize;',MAIN)
        self.assertIn('b9ConfiguredStackNodes.Add(node.id);',MAIN)
        self.assertIn('foreign && !b9ConfiguredStackNodes.Contains(node.id)',MAIN)

    def test_supported_patterns_and_invalid_regex_fail_closed(self):
        self.assertIn('Regex.IsMatch(nodeId',B9)
        self.assertIn('Regex.Escape(c.ToString())',B9)
        self.assertIn('RegexMatchTimeoutException',B9)
        self.assertIn('catch (ArgumentException)',B9)
        self.assertIn('TimeSpan.FromMilliseconds(50)',B9)

    def test_oranges_native_not_prior_subtype(self):
        defaults={'top':(2.4885,2)}
        modifiers={'NLSUS-A':{},'NLSUS-B':{'top':{'position':1.208}}}
        def resolve(subtype,stretch):
            value=modifiers[subtype].get('top',{}).get('position',defaults['top'][0])
            return value*stretch
        for subtype,expected in [('NLSUS-B',1.812),('NLSUS-A',3.73275),
                                 ('NLSUS-B',1.812),('NLSUS-A',3.73275)]:
            self.assertAlmostEqual(resolve(subtype,1.5),expected)

if __name__=='__main__':unittest.main()
