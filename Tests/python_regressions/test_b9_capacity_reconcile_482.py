"""B9 managed capacities are restored from active subtype after B9 startup."""
from pathlib import Path
import json
import unittest
ROOT = Path(__file__).resolve().parents[2]
B9 = (ROOT/'PartSculptor/B9PartSwitchSupport.cs').read_text()
PART = (ROOT/'PartSculptor/PartSculptor.cs').read_text()
MATH = (ROOT/'PartSculptor/ScaleMath.cs').read_text()

class B9Capacity482(unittest.TestCase):
    def test_reads_current_b9_active_subtype_not_prefab_resource_or_ui(self):
        for text in ('"CurrentSubtype"', '"CurrentTankType"', '"GetTotalVolume"',
                     '"ResourceName"', '"unitsPerVolume"'):
            self.assertIn(text, B9)
        self.assertIn('ScaleMath.B9ManagedCapacity(volume,', B9)
        self.assertIn('binding.IsManagedResourceMethod.Invoke(', B9)

    def test_b9_flight_never_calls_update_volume_or_refills_fuel(self):
        method = B9.split('internal bool ReconcileManagedCapacities(',1)[1].split(
            '// B9 already scales its volume-derived mass/cost',1)[0]
        self.assertNotIn('UpdateVolumeMethod.Invoke', method)
        self.assertNotIn('resource.amount = resource.maxAmount', method)
        self.assertIn('if (resource.amount > expected) resource.amount = expected;', method)
        self.assertIn('resource.maxAmount = expected;', method)
        self.assertNotIn('part.AddResource(', method)

    def test_runs_immediately_after_apply_and_after_b9_late_start(self):
        self.assertIn('b9Support.ReconcileManagedCapacities(part)', PART)
        self.assertGreaterEqual(PART.count('b9Support.ReconcileManagedCapacities(part)'),2)
        self.assertIn('nextB9CapacityRecheck = Time.realtimeSinceStartup + 0.75f',PART)
        self.assertIn('!b9SubtypeChangePending', PART)
        self.assertIn('resourceDisplayDirty = true;', PART)

    def test_normal_resource_scaling_not_replaced(self):
        self.assertIn('b9Support.OwnsScaledResource(resource.resourceName)) continue;',PART)
        self.assertIn('ApplyResourceScaling(false);',PART)
        self.assertIn('MonoUtilities.RefreshPartContextWindow(part);',PART)
        self.assertIn('B9ManagedCapacity(double totalVolume',MATH)
        self.assertIn('return totalVolume * unitsPerVolume;',MATH)

    def test_rebased_from_478_and_versioned_482(self):
        self.assertNotIn('SynchronizeFlightResourceDisplay()',PART)
        self.assertNotIn('SynchronizeResourceDisplay()',PART)
        self.assertNotIn('resourceMax.text',PART)
        self.assertIn('## 0.0.4.82', (ROOT/'Changelog.md').read_text())
        for filename in ('PartSculptor.version','GameData/PartSculptor/PartSculptor.version'):
            self.assertEqual(json.loads((ROOT/filename).read_text())['VERSION']['BUILD'], 130)

if __name__=='__main__': unittest.main()
