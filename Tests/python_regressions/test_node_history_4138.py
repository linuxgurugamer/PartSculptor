"""v0.0.4.138: StretchScale 1.3 foreign node write history in PartSculptor.

These are source-path checks because Unity/KSP is unavailable to Python.  The
matching in-game plan covers the dynamic and attachment geometry cases.
"""
import json
import re
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]


def read(name):
    return (ROOT/name).read_text(encoding='utf-8-sig')


class NodeHistory4138(unittest.TestCase):
    def test_version_and_project_inclusion(self):
        for name in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(20, json.loads(read(name))['VERSION']['BUILD'])
        self.assertIn('AssemblyVersion("0.0.5.20")', read('PartSculptor/AssemblyVersion.cs'))
        self.assertIn('<Compile Include="NodeGeometryHistory.cs" />',read('PartSculptor/PartSculptor.csproj'))

    def test_preserves_three_distinct_node_states_and_craft_persistence(self):
        history = read('PartSculptor/NodeGeometryHistory.cs')
        for token in ('NativePosition', 'StretchedPosition', 'WrittenPosition',
                      'WrittenOriginal', 'WrittenOrientation', 'WrittenSize',
                      'PARTSCULPTOR_NODE_HISTORY', 'internal void Load(ConfigNode owner)',
                      'internal void Save(ConfigNode owner)', 'GetNodes(SaveNode)',
                      'owner.RemoveNodes(SaveNode)'):
            self.assertIn(token, history)
        module = read('PartSculptor/PartSculptor.cs')
        self.assertIn('nodeHistory.Load(node);', module)
        self.assertIn('nodeHistory.Save(node);', module)

    def test_foreign_only_if_final_pose_differs(self):
        h = read('PartSculptor/NodeGeometryHistory.cs')
        self.assertIn('Near(node.position, record.WrittenPosition)', h)
        self.assertIn('Near(node.originalPosition, record.WrittenOriginal)', h)
        self.assertIn('Near(node.orientation, record.WrittenOrientation)', h)
        self.assertIn('node.size == record.WrittenSize', h)
        self.assertIn('if (!record.HasWritten || !Near(node.position, record.WrittenPosition))',h)
        self.assertIn('if (!record.HasWritten || !Near(node.orientation, record.WrittenOrientation))',h)

    def test_face_morph_writes_final_not_native(self):
        main = read('PartSculptor/PartSculptor.cs')
        self.assertRegex(main,r'm\.faceMorph\.Apply\(bottom, top, true,[\s\S]{0,420}m\.nodeHistory\.RecordFinal\(m\.part\)')
        self.assertRegex(main,r'faceMorph\.RebaseNodes\(\);[\s\S]*?faceMorph\.Apply\(GetMorphFace\(0\), GetMorphFace\(1\), false\);')
        self.assertIn('nodeHistory.RecordFinal(part);',main)

    def test_external_repair_and_startup_modes(self):
        main = read('PartSculptor/PartSculptor.cs')
        self.assertIn('NodeApplyReason.Foreign', main)
        self.assertIn('NodeApplyReason.TweakScale', main)
        self.assertIn('NodeApplyReason.Load', main)
        self.assertIn('Time.frameCount - geometryInitializedFrame > 30', main)
        self.assertIn('Time.frameCount - geometryInitializedFrame < 120', main)
        self.assertIn('nodeHistory.ChangedExternally(part)', main)
        self.assertIn('Apply(moveForeign, moveForeign, moveForeign, NodeApplyReason.Foreign);',main)
        self.assertIn('Apply(moveForeign, moveForeign, false, NodeApplyReason.Foreign);',main)

    def test_b9_partial_node_writes_retain_other_native_fields(self):
        main = read('PartSculptor/PartSculptor.cs')
        self.assertIn('NodeGeometryHistory.Near(node.position, previous.WrittenPosition)', main)
        self.assertIn('? previous.NativePosition : node.position / BaseFactor;', main)
        self.assertIn('? previous.NativeOriginal : node.originalPosition / BaseFactor;', main)
        self.assertIn('? previous.NativeOrientation : node.orientation;', main)
        self.assertIn('? previous.NativeSize : node.size;', main)

    def test_unstretched_parts_retain_foreign_node_ownership(self):
        main = read('PartSculptor/PartSculptor.cs')
        self.assertIn('bool preserveNativeForeignNodes', main)
        self.assertIn('if (!preserveNativeForeignNodes)\n                model.localScale', main)
        self.assertIn('if (preserveNativeForeignNodes)\n                {', main)

    def test_no_change_to_copy_match_or_attachment_fix(self):
        baseline = Path('/mnt/data/PartSculptor_0.0.4.137_Source.zip')
        # These components should be byte-for-byte untouched by the port.
        if not baseline.exists():
            self.skipTest('The optional 0.0.4.137 archive is not present')
        import zipfile
        with zipfile.ZipFile(baseline) as z:
            for name in ('FaceMorphStepCore.cs', 'FaceAttachmentMatchCore.cs'):
                original=z.read('PartSculptor/PartSculptor/'+name).decode('utf-8-sig')
                self.assertEqual(original.replace('[PartSculptor] ', '').replace('[PartSculptor]', ''), read('PartSculptor/'+name))


if __name__ == '__main__':
    unittest.main()
