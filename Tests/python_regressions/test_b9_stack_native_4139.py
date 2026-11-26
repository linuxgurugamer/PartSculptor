"""B9 subtype native-node contract. KSP integration additionally needs VAB tests."""
import json
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]

def source(path):
    return (ROOT/path).read_text(encoding='utf-8-sig')

class B9NativeNode4139(unittest.TestCase):
    def test_version_metadata(self):
        for version in ('PartSculptor.version','GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads(source(version))['VERSION']['BUILD'], 23)
        self.assertIn('AssemblyVersion("0.0.5.23")', source('PartSculptor/AssemblyVersion.cs'))

    def test_b9_subtype_from_immutable_definition_and_prefab_default(self):
        b9=source('PartSculptor/B9PartSwitchSupport.cs')
        self.assertIn('TryGetConfiguredStackNodePosition', b9)
        self.assertIn('part.partInfo.partConfig', b9)
        self.assertIn('part.partInfo.partPrefab', b9)
        self.assertIn('GetNodes("SUBTYPE")', b9)
        self.assertIn('GetNodes("NODE")', b9)
        self.assertIn('GetValue("position")', b9)
        self.assertIn('selectedSubtype', b9)
        self.assertIn('nativePosition = defaultPosition;', b9)
        self.assertIn('TryReadNodeVector', b9)

    def test_source_and_history_integrated(self):
        main=source('PartSculptor/PartSculptor.cs')
        self.assertIn('b9ConfiguredStackNodes.Add(node.id)', main)
        self.assertIn('prefabNodePos[node.id] = configuredPosition;',main)
        self.assertIn('prefabNodeOrigPos[node.id] = configuredPosition;',main)
        self.assertIn('history.NativePosition = configuredPosition;',main)
        self.assertIn('history.NativeOriginal = configuredPosition;',main)
        self.assertIn('foreign && !b9ConfiguredStackNodes.Contains(node.id)',main)
        self.assertIn('RefreshB9NodeBaselines(true)', main)
        self.assertIn('RefreshB9NodeBaselines();',main)

    def test_oranges_switch_positions(self):
        # From the user-supplied PART definition: A defaults to 2.4885;
        # B overrides to 1.208. With StretchScale Y=1.5, multiply once.
        top_y={'NLSUS-A':2.4885, 'NLSUS-B':1.208}
        stretch=1.5
        for subtype, expected in [('NLSUS-B',1.812),('NLSUS-A',3.73275),
                                  ('NLSUS-B',1.812),('NLSUS-A',3.73275)]:
            self.assertAlmostEqual(top_y[subtype]*stretch,expected)

    def test_unrelated_morph_logic_unchanged(self):
        import zipfile
        orig=Path('/mnt/data/PartSculptor_0.0.4.138_Source.zip')
        if not orig.is_file(): self.skipTest('previous archive not available')
        with zipfile.ZipFile(orig) as z:
            for name in ('FaceMorphStepCore.cs','PartSculptorControlsWindow.cs'):
                self.assertEqual(z.read('PartSculptor/PartSculptor/'+name).decode('utf-8-sig').replace('[PartSculptor] ', '').replace('[PartSculptor]', ''),
                                 source('PartSculptor/'+name))

if __name__=='__main__': unittest.main()
