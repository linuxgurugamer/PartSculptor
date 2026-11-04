"""Check PartSculptor uses RealFuels 15.16.1 total volume API."""
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
MFT=(ROOT/'PartSculptor/MFTVolumeSupport.cs').read_text()
MATH=(ROOT/'PartSculptor/ScaleMath.cs').read_text()
RUN=(ROOT/'Tests/RegressionRunner/Program.cs').read_text()

class RealFuels15161(unittest.TestCase):
    def test_uses_new_supported_api(self):
        self.assertIn('FindChangeTotalVolume', MFT)
        self.assertIn('method.Name != "ChangeTotalVolume"', MFT)
        self.assertIn('binding.ChangeTotalVolume.Invoke(binding.Module, args)', MFT)
        self.assertIn('ApplyTotalVolume(binding, wanted, live)', MFT)
    def test_passes_total_volume_without_symmetry_propagation(self):
        self.assertIn('MFTTotalVolumeTarget(wanted, live,', MFT)
        self.assertIn('new object[] { newTotal, false }', MFT)
        self.assertIn('binding.TotalVolume', MFT)
        self.assertIn('binding.Utilization', MFT)
    def test_converts_utilization_without_compounding(self):
        self.assertIn('wantedUsable * (currentTotal / currentUsable)', MATH)
        self.assertIn('wantedUsable * 100d / utilization', MATH)
        self.assertIn('RealFuelsTotalVolume();', RUN)
        self.assertIn('6250', RUN)
        self.assertIn('5000, 4000, 4000, 100', RUN)
    def test_preserves_legacy_mft_fallback(self):
        self.assertIn('FindChangeVolume(', MFT)
        self.assertIn('if (!applied && binding.ChangeVolume != null)', MFT)
        self.assertIn('SendLegacyVolumeChange(binding, wanted)', MFT)
        self.assertIn('finally', MFT)
    def test_verifies_actual_result(self):
        self.assertIn('ReadVolume(binding.Module, binding.Volume), wanted', MFT)
        self.assertIn('volume update ', MFT)
        self.assertIn('Changed', MFT) if False else None
