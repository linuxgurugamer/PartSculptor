"""Static integration checks for optional MFT, FS and IFS adapters."""
from pathlib import Path
import unittest
ROOT = Path(__file__).resolve().parents[2]
SOURCE = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()
ADAPTER = (ROOT / 'PartSculptor/FuelSwitchSupport.cs').read_text()
MATH = (ROOT / 'PartSculptor/FuelSwitchMath.cs').read_text()
PROJECT = (ROOT / 'PartSculptor/PartSculptor.csproj').read_text()

class FuelSwitchIntegration(unittest.TestCase):
    def test_detects_modules_without_hard_dependencies(self):
        for name in ('ModuleFuelTanks', 'FSfuelSwitch', 'InterstellarFuelSwitch'):
            self.assertIn(name, ADAPTER)
        self.assertIn('Compile Include="FuelSwitchSupport.cs"', PROJECT)
        self.assertIn('Compile Include="FuelSwitchMath.cs"', PROJECT)
        self.assertIn('IPartMassModifier', ADAPTER)
        self.assertIn('IPartCostModifier', ADAPTER)

    def test_variant_baselines_saved_and_restored(self):
        self.assertIn('STRETCHSCALE_FUEL_SWITCH_RESOURCE', SOURCE)
        self.assertIn('fuelSwitchResourceBases.Clear();', SOURCE)
        self.assertIn('fuelSwitchResourceBases[FuelSwitchSupport.Key(', SOURCE)
        self.assertIn('fuelSwitchResourceRebasePending', SOURCE)
        self.assertIn('FuelSwitchMath.NativeCapacity(', SOURCE)
        self.assertIn('internal static double AbsoluteCapacity(', MATH)

    def test_switch_rebase_is_delayed(self):
        self.assertIn('Time.frameCount > fuelSwitchChangedFrame', SOURCE)
        self.assertIn('if (b9SubtypeChangePending || fuelSwitchChangePending) return;', SOURCE)
        self.assertIn('OnFuelSwitchChanged()', SOURCE)
        self.assertIn('fuelSwitchResourceRebasePending = false;', SOURCE)

    def test_mass_cost_are_deltas_not_replacements(self):
        self.assertIn('FuelSwitchExtraMass(nativeMass, sit)', SOURCE)
        self.assertIn('FuelSwitchExtraCost((float)nativeCost, sit)', SOURCE)
        self.assertIn('(ExponentFactor(exponentScaler != null ? exponentScaler.MassExponent : 3d) - 1d)', SOURCE)
        self.assertIn('"Fuel Switchers"', SOURCE)

if __name__ == '__main__':
    unittest.main()
