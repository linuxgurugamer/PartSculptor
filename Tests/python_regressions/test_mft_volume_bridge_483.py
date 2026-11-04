"""MFT owns the total allocated tank volume; PartSculptor must change it via MFT."""
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
SRC=(ROOT/'PartSculptor/PartSculptor.cs').read_text()
MFT=(ROOT/'PartSculptor/MFTVolumeSupport.cs').read_text()
EXPS=(ROOT/'PartSculptor/ScaleExponents.cs').read_text()
MATH=(ROOT/'PartSculptor/ScaleMath.cs').read_text()
PROJ=(ROOT/'PartSculptor/PartSculptor.csproj').read_text()

class MFTBridge483(unittest.TestCase):
    def test_optional_reflection_bridge(self):
        self.assertIn('ModuleFuelTanks',MFT)
        self.assertIn('FindChangeVolume',MFT)
        self.assertIn('binding.ChangeVolume.Invoke(',MFT)
        self.assertIn('Compile Include="MFTVolumeSupport.cs"',PROJ)
        self.assertNotIn('using RealFuels',MFT)

    def test_absolute_volume_and_saved_baseline(self):
        self.assertIn('ScaleMath.MFTManagedVolume(binding.Native', MFT)
        self.assertIn('native * stretchVolume * tweakVolume',MATH)
        self.assertIn('PARTSCULPTOR_MFT_VOLUME',SRC)
        self.assertIn('mftNativeVolumes.Clear()',SRC)
        self.assertIn('nativeVolume',SRC)
        self.assertIn('prefabs[ordinal]',MFT)

    def test_editor_and_flight_fuel_handling(self):
        self.assertIn('fraction * resource.maxAmount',MFT)
        self.assertIn(': previous;',MFT)
        self.assertIn('Math.Min(resource.maxAmount, updated)',MFT)
        self.assertIn('Time.realtimeSinceStartup >= nextMFTVolumeCheck',SRC)

    def test_avoids_double_scaling(self):
        self.assertIn('if (mftVolumeSupport != null)',SRC)
        self.assertIn('ModuleFuelTanks manages allocated fuel',SRC)
        self.assertIn('string.Equals(module.moduleName, "ModuleFuelTanks"',EXPS)
        self.assertIn('if (mftVolumeSupport != null &&',SRC)

    def test_does_not_modify_b9_algorithm(self):
        b9=(ROOT/'PartSculptor/B9PartSwitchSupport.cs').read_text()
        self.assertIn('ReconcileManagedCapacities',b9)
        self.assertIn('B9PartSwitchSupport.Create', SRC)

if __name__=='__main__':unittest.main()
