"""B9 parent/child tank contribution integration checks (0.0.4.105)."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = (ROOT/'PartSculptor/B9PartSwitchSupport.cs').read_text()
COMPAT = (ROOT/'PartSculptor/B9FaceMorphCompatibility.cs').read_text()
MAIN = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
MATH = (ROOT/'PartSculptor/ScaleMath.cs').read_text()
RUNNER = (ROOT/'Tests/RegressionRunner/Program.cs').read_text()


class B9ParentVolume105(unittest.TestCase):
    def test_subtype_allowlist_has_new_field_not_at_wrong_level(self):
        def values(name):
            match = re.search(r'HashSet<string> '+name+r'\s*= Set\((.*?)\);', COMPAT, re.S)
            self.assertIsNotNone(match)
            return re.findall(r'"([^"]+)"', match.group(1))
        self.assertIn('volumeAddedToParent', values('SubtypeFields'))
        for place in ('ModuleFields', 'ResourceFields', 'TextureFields'):
            self.assertNotIn('volumeAddedToParent', values(place))
        self.assertIn('if (!AllowedValues(child, SubtypeFields, where, out reason)) return false;', COMPAT)
        self.assertIn('geometry/unknown node', COMPAT)

    def test_reflect_actual_b9_subtypes_and_signed_field(self):
        self.assertIn('ExponentBinding.FindMember(module.GetType(), "subtypes")', SOURCE)
        self.assertIn('"volumeAddedToParent"', SOURCE)
        self.assertIn('SubtypeName(subtype)', SOURCE)
        self.assertIn('foreach (object subtype in Subtypes(module))', SOURCE)
        self.assertIn('"parentID"', SOURCE)
        self.assertIn('ParentVolumeKey(i, Value(module, "moduleID"), name)', SOURCE)

    def test_native_baselines_saved_and_reloaded_with_negative_support(self):
        self.assertIn('const string B9_PARENT_VOLUME_NODE = "PARTSCULPTOR_B9_PARENT_VOLUME";', MAIN)
        self.assertIn('readonly Dictionary<string, double> b9ParentVolumes', MAIN)
        self.assertIn('b9ParentVolumes.Clear();', MAIN)
        self.assertIn('node.GetNodes(B9_PARENT_VOLUME_NODE)', MAIN)
        self.assertIn('node.RemoveNodes(B9_PARENT_VOLUME_NODE);', MAIN)
        self.assertIn('nativeVolumeAddedToParent', MAIN)
        self.assertIn('native >= -float.MaxValue && native <= float.MaxValue', MAIN)
        self.assertIn('record.AddValue("switchKey", switchKey);', MAIN)
        self.assertIn('record.AddValue("subtype", subtype);', MAIN)
        self.assertIn('B9PartSwitchSupport.ResourceKey(switchKey, subtype)', MAIN)
        self.assertNotIn('record.AddValue("key", entry.Key);\n                record.AddValue("nativeVolumeAddedToParent"', MAIN)
        self.assertIn('b9ParentVolumes, StretchVolumeFactor', MAIN)
        self.assertIn('saved.TryGetValue(key, out native)', SOURCE)

    def test_native_prefab_config_before_live_fallback(self):
        self.assertIn('part.partInfo.partConfig', SOURCE)
        self.assertIn('NativeSubtypeVolume(part, i, ordinal, name, out native)', SOURCE)
        self.assertIn('prefabSubtypes[ordinal]', SOURCE)
        self.assertIn('live / (ScaleMath.PositiveFinite(currentStretchVolume)', SOURCE)
        self.assertLess(SOURCE.index('saved.TryGetValue(key, out native)'),
                        SOURCE.index('NativeSubtypeVolume(part, i, ordinal, name, out native)'))

    def test_signed_morph_percentage_math_from_absolute_baseline(self):
        self.assertIn('internal static double B9ParentVolume(double native, double volumeFactor)', MATH)
        self.assertIn('return native * volumeFactor;', MATH)
        self.assertIn('ScaleMath.B9ParentVolume(native, volumeFactor)', SOURCE)
        for expected in ('B9ParentVolume(200, 1.25)', 'B9ParentVolume(-200, 1.25)',
                         'B9ParentVolume(-200, 0.5)', 'B9ParentVolume(-200, 1)',
                         'B9ParentVolume(0, 2)'):
            self.assertIn(expected, RUNNER)
        self.assertIn('for (int i = 0; i < 100; ++i)', RUNNER)
        self.assertIn('if (!FiniteSigned(next)) continue;', SOURCE)

    def test_apply_all_subtypes_before_parent_refresh(self):
        start = SOURCE.index('internal void ApplyBaseVolumes(')
        end = SOURCE.index('// OnStart/Start and subtype initialization', start)
        code = SOURCE[start:end]
        self.assertIn('foreach (ParentVolumeBinding child in parentVolumes)', code)
        self.assertIn('ScaleParentVolume(child.Native, stretchVolume)', code)
        self.assertIn('refresh.Add(parent);', code)
        self.assertIn('"UpdateVolumeFromChildren"', code)
        self.assertLess(code.index('foreach (ParentVolumeBinding child in parentVolumes)'),
                        code.index('foreach (VolumeBinding binding in volumes)'))
        self.assertLess(code.index('"UpdateVolumeFromChildren"'),
                        code.index('if (!editor || refresh.Count == 0) return;'))

    def test_no_updatevolume_in_flight_or_double_refresh(self):
        code = SOURCE[SOURCE.index('internal void ApplyBaseVolumes('):]
        code = code[:code.index('// OnStart/Start and subtype initialization')]
        self.assertIn('if (!editor || refresh.Count == 0) return;', code)
        self.assertIn('var refresh = new HashSet<PartModule>();', code)
        self.assertIn('refresh.Remove(binding.Module)', code)
        self.assertIn('binding.UpdateVolumeMethod.Invoke(binding.Module, null);', code)
        self.assertIn('fillFractions[resource.resourceName]', code)
        self.assertIn('resource.maxAmount * fraction', code)

    def test_parent_zero_base_still_b9_resource_owner(self):
        self.assertIn('private bool HasChildVolume(PartModule parent)', SOURCE)
        self.assertIn('binding.Native <= 0d && !HasChildVolume(binding.Module)', SOURCE)
        self.assertIn('ScaleMath.B9ManagedCapacity(volume,', SOURCE)
        self.assertIn('if (!FiniteNonNegative(volume)) continue;', SOURCE)
        self.assertIn('Equal(ScaleMath.B9ManagedCapacity(0, 2), 0', RUNNER)

    def test_scale_factor_includes_morph_and_stretch_not_tweakscale_twice(self):
        self.assertIn('FaceMorphMath.VolumeFactor(GetMorphFace(0), GetMorphFace(1))', MAIN)
        self.assertIn('b9Support.ApplyBaseVolumes(part, StretchVolumeFactor,', MAIN)
        self.assertIn('m.b9Support.ApplyBaseVolumes(m.part, m.StretchVolumeFactor, true)', MAIN)
        self.assertIn('ScaleMath.B9BaseVolume(binding.Native, stretchVolume)', SOURCE)
        self.assertIn('ScaleParentVolume(child.Native, stretchVolume)', SOURCE)

    def test_mass_cost_correction_uses_native_child_contributions(self):
        self.assertIn('private double NativeVolumeFromChildren(PartModule parent)', SOURCE)
        self.assertIn('object.ReferenceEquals(selected, child.Subtype)', SOURCE)
        self.assertIn('double liveChildren = Number(binding.Module, "VolumeFromChildren");', SOURCE)
        self.assertIn('double nativeChildren = NativeVolumeFromChildren(binding.Module);', SOURCE)
        self.assertIn('binding.Native * multiplier + added + nativeChildren', SOURCE)
        self.assertIn('liveBase * multiplier + added + liveChildren', SOURCE)

    def test_inspector_shows_original_and_scaled_parent_contributions(self):
        self.assertIn('binding.Key + " volumeAddedToParent", binding.Native, current', SOURCE)
        self.assertIn('B9 child subtype contribution to parent tank (signed)', SOURCE)

    def test_documentation_and_version(self):
        readme = (ROOT/'README.md').read_text()
        self.assertIn('### B9 `volumeAddedToParent` and face morphing', readme)
        self.assertIn('`volumeAddedToParent = -200` becomes `-250`', readme)
        self.assertEqual((ROOT/'README.md').read_bytes(),
                         (ROOT/'GameData/PartSculptor/README.md').read_bytes())
        for path in ('PartSculptor.version', 'GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT/path).read_text())['VERSION']['BUILD'], 130)
        self.assertIn('AssemblyVersion("0.0.4.130")',
                      (ROOT/'PartSculptor/AssemblyVersion.cs').read_text())

if __name__ == '__main__':
    unittest.main()
