"""Offline structural regressions; no KSP or .NET runtime required.

Run using python -m unittest discover -s Tests/python_regressions.
These are smoke checks, not substitutes for running production C# tests.
"""
from pathlib import Path
import json
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
CODE = (ROOT / 'PartSculptor/PartSculptor.cs').read_text(encoding='utf-8')
MATH = (ROOT / 'PartSculptor/ScaleMath.cs').read_text(encoding='utf-8')
PROJECT = (ROOT / 'PartSculptor/PartSculptor.csproj').read_text(encoding='utf-8')
SETTINGS = (ROOT / 'GameData/PartSculptor/PluginData/Settings.cfg').read_text(encoding='utf-8')

class TestIntegration(unittest.TestCase):
    def test_project_links_production_math(self):
        self.assertIn('Compile Include="ScaleMath.cs"', PROJECT)
        self.assertIn('Compile Include="../../PartSculptor/ScaleMath.cs"',
                      (ROOT / 'Tests/RegressionRunner/RegressionRunner.csproj').read_text())

    def test_uses_actual_math_not_duplicate_calculation(self):
        self.assertIn('return ScaleMath.ExponentFactor(StretchVolumeFactor, exponent);', CODE)
        self.assertIn('return ScaleMath.NodeSizeTier(originalSize', CODE)

    def test_automatic_matching_editor_only_on_new_parent(self):
        self.assertIn('void CheckForNewStackAttachment()', CODE)
        self.assertIn('if (!HighLogic.LoadedSceneIsEditor || part == null) return;', CODE)
        self.assertIn('nodeAttachWatcher.Observe(part.parent)', CODE)
        self.assertIn('nodeAttachWatcher.Complete(); // Never repeatedly reshape this join.', CODE)
        self.assertIn('nodeAttachWatcher.Seed(part.parent);', CODE)
        self.assertIn('nodeAttachWatcher.Retry();', CODE)
        self.assertIn('internal sealed class NodeAttachWatcher', MATH)
        self.assertIn('if (source == null) return false; // Skip surface-attached parts.', CODE)
        self.assertIn('CommitChange(before, null);', CODE)

    def test_switch_and_limits(self):
        self.assertIn('matchNodeSizesDefault = false', SETTINGS)
        self.assertIn('matchNodeSizes = MatchNodeSizesGlobal;', CODE)
        self.assertIn('Fields["matchNodeSizes"].OnValueModified += OnMatchNodeSizesModified;', CODE)
        self.assertIn('if ((!MatchNodeSizesGlobal && !MatchAttachedFacesGlobal) ||', CODE)

    def test_math_protects_zero_and_previous_value(self):
        self.assertIn('return baseline * ExponentFactor(volume, exponent);', MATH)
        self.assertIn('if (!PositiveFinite(targetDiameter) || !PositiveFinite(sourceDiameter))', MATH)
        self.assertNotIn('Math.Pow(StretchVolumeFactor, exponent / 3d)', CODE)


    def test_gizmo_routes_through_scaling_engine(self):
        gizmo = (ROOT / 'PartSculptor/ScaleGizmo.cs').read_text(encoding='utf-8')
        self.assertIn('Compile Include="ScaleGizmo.cs"', PROJECT)
        self.assertIn('ScaleGizmo.Toggle(this);', CODE)
        self.assertIn('ScaleGizmo.UpdateFor(this);', CODE)
        self.assertIn('ScaleGizmo.HideFor(this);', CODE)
        self.assertIn('CommitChange(before, null);', CODE)
        self.assertIn('ScaleMath.GizmoAxes(', CODE)
        self.assertIn('ScaleMath.GizmoFollowValue(', gizmo)
        self.assertIn('GizmoScaleValue(double baseline, double pixelDelta', MATH)
        self.assertIn('GizmoAxes(double x, double y, double z', MATH)
        self.assertIn('InputLockManager.RemoveControlLock(LockName);', gizmo)
        self.assertIn('collider.enabled = false;', gizmo)

    def test_specialized_docking_integration(self):
        helper = (ROOT / 'PartSculptor/DockingPortScaling.cs').read_text()
        self.assertIn('Compile Include="DockingPortScaling.cs"', PROJECT)
        self.assertIn('DockingPortScaling.Create(part, prefab)', CODE)
        self.assertIn('dockingPorts.Apply(Target * BaseFactor', CODE)
        self.assertIn('dockingPorts.AppendReport(values)', CODE)
        self.assertIn('ScaleDockingCaptureDistances', helper)
        self.assertIn('MatchDockingPortTypes', helper)
        self.assertIn('node.otherNode != null', helper)
        self.assertIn('node.dockedPartUId != 0', helper)
        self.assertIn('port.multipleTypes', helper)
        self.assertIn('b9Present', helper)
        self.assertIn('port.acquireRange * face', helper)
        self.assertIn('Settings.ScaleDockingCaptureDistances && !b9Present', helper)
        self.assertIn('ScaleMath.DockingNodeType', helper)
        self.assertIn('DockingFaceScale(', MATH)
        self.assertIn('matchDockingPortTypes = true', SETTINGS)

    def test_docking_prefab_local_scope(self):
        # The variable declared in OnStart() is not visible in OnStartFinished().
        on_start_finished = CODE.split('public override void OnStartFinished(', 1)[1]
        on_start_finished = on_start_finished.split('void HideFields()', 1)[0]
        self.assertIn('Part prefab = part.partInfo != null ? part.partInfo.partPrefab : null;',
                      on_start_finished)
        self.assertIn('dockingPorts = DockingPortScaling.Create(part, prefab);',
                      on_start_finished)
        self.assertLess(on_start_finished.index('Part prefab = part.partInfo'),
                        on_start_finished.index('DockingPortScaling.Create(part, prefab)'))

    def test_version(self):
        a = json.loads((ROOT / 'PartSculptor.version').read_text())['VERSION']
        b = json.loads((ROOT / 'GameData/PartSculptor/PartSculptor.version').read_text())['VERSION']
        self.assertEqual(a, b)
        self.assertEqual((a['MAJOR'], a['MINOR'], a['PATCH']), (0,0,4))
        self.assertGreaterEqual(a['BUILD'], 59)
        self.assertIn('AssemblyVersion("0.0.4.%d")' % a['BUILD'],
                      (ROOT / 'PartSculptor/AssemblyVersion.cs').read_text())

if __name__ == '__main__':
    unittest.main()
