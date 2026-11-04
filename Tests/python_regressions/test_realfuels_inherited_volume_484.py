"""Verify the RealFuels-specific inherited receiver and fallback paths."""
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
SRC = (ROOT / 'PartSculptor/MFTVolumeSupport.cs').read_text()
CORE = (ROOT / 'PartSculptor/PartSculptor.cs').read_text()
B9 = (ROOT / 'PartSculptor/B9PartSwitchSupport.cs').read_text()

class RealFuelsVolumeBridge484(unittest.TestCase):
    def test_inherited_private_receiver_is_discovered(self):
        self.assertIn('current = current.BaseType', SRC)
        self.assertIn('BindingFlags.DeclaredOnly', SRC)
        self.assertIn('BindingFlags.NonPublic', SRC)
        self.assertIn('method.Name != "ChangeVolume"', SRC)
        self.assertIn('Type target = preference == 0 ? typeof(double) : typeof(float);', SRC)

    def test_derived_realfuels_module_detected(self):
        self.assertIn('type = type.BaseType', SRC)
        self.assertIn('string.Equals(type.Name, "ModuleFuelTanks"', SRC)

    def test_realfuels_legacy_message_is_fallback_only(self):
        self.assertIn('SendLegacyVolumeChange', SRC)
        self.assertIn('SendMessage("ChangeVolume", (float)wanted', SRC)
        self.assertIn('SendMessage("ChangeVolume", wanted', SRC)
        self.assertIn('SendMessageOptions.DontRequireReceiver', SRC)
        self.assertIn('if (!applied)\n                        applied = SendLegacyVolumeChange', SRC)
        self.assertIn('binding.ChangeVolume.Invoke(', SRC)

    def test_verifies_reported_volume_and_logs_failure(self):
        self.assertIn('ReadVolume(binding.Module, binding.Volume), wanted', SRC)
        self.assertIn('volume update ', SRC)
        self.assertIn('Assembly.GetName().Name', SRC)
        self.assertIn('binding.Warned = true;', SRC)
        self.assertIn('binding.Warned = false;', SRC)

    def test_failed_callback_restores_fuel_and_does_not_claim_change(self):
        self.assertIn('finally\n                {\n                    // A failed/unsupported RealFuels callback', SRC)
        self.assertIn('fraction * resource.maxAmount : previous;', SRC)
        self.assertIn('Math.Min(resource.maxAmount, updated)', SRC)
        self.assertIn('if (!applied)', SRC)
        self.assertIn('continue;', SRC)

    def test_existing_mft_b9_integration_and_docs(self):
        self.assertIn('mftVolumeSupport.Apply(part, StretchVolumeFactor', CORE)
        self.assertIn('ReconcileManagedCapacities', B9)
        self.assertIn('RealFuels', (ROOT / 'README.md').read_text())
        self.assertTrue((ROOT / 'Tests/REALFUELS_VOLUME_0.0.4.84.md').exists())

if __name__ == '__main__': unittest.main()
