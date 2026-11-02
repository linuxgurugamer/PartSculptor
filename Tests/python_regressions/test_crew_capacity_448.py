"""Verify that crew scaling is seat-aware and separated from generic exponent bindings."""
import unittest
from pathlib import Path

R = Path(__file__).resolve().parents[2]
S = (R / 'PartSculptor' / 'PartSculptor.cs').read_text()
E = (R / 'PartSculptor' / 'ScaleExponents.cs').read_text()
C = (R / 'PartSculptor' / 'CrewCapacityScaling.cs').read_text()
M = (R / 'PartSculptor' / 'CrewCapacityMath.cs').read_text()

class CrewCapacityTests(unittest.TestCase):
    def test_generic_crew_field_is_skipped(self):
        self.assertIn('key.Equals("CrewCapacity", StringComparison.OrdinalIgnoreCase))) continue;', E)
        self.assertIn('HasCrewRule = partRule.Fields.TryGetValue("CrewCapacity"', E)
    def test_iva_seats_guard(self):
        self.assertIn('part.internalModel.seats.Count', C)
        self.assertIn('prefab.internalModel.seats.Count', C)
        self.assertIn('GetConfigNodes("INTERNAL")', C)
        self.assertIn('GetConfigNodes("PROP")', C)
        self.assertIn('Math.Min(result.Requested, result.SeatLimit)', M)
        self.assertIn('Crew aboard: capacity unchanged until empty', M)
    def test_apply_and_report(self):
        self.assertIn('new CrewCapacityScaling(part, exponentScaler.CrewExponent)', S)
        self.assertIn('crewCapacityScaling.Apply(scaleX, scaleY, scaleZ)', S)
        self.assertIn('crewCapacityScaling.AppendReport(values)', S)
        self.assertIn('FireEditorShipModified()', S)
    def test_production_math_is_tested(self):
        csproj = (R / 'Tests' / 'RegressionRunner' / 'RegressionRunner.csproj').read_text()
        self.assertIn('CrewCapacityMath.cs', csproj)
        self.assertIn('CrewCapacity();', (R / 'Tests' / 'RegressionRunner' / 'Program.cs').read_text())

if __name__ == '__main__': unittest.main()
