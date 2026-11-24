"""Kerbals and flags must remain outside geometry and modifier ownership."""
import fnmatch
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PATCH = (ROOT/'GameData/PartSculptor/Patches/PartSculptor.cfg').read_text()
MAIN = (ROOT/'PartSculptor/PartSculptor.cs').read_text()

class KerbalFlagExclusion0520(unittest.TestCase):
    def test_patch_catches_stock_names_and_special_modules(self):
        rule = next(x for x in PATCH.splitlines() if x.startswith('@PART['))
        names = re.findall(r'~name\[([^\]]+)\]', rule)
        modules = re.findall(r'!MODULE\[([^\]]+)\]', rule)
        self.assertEqual(set(names), {'kerbalEVA*', 'flag', 'FlagSite', 'flagPart*'})
        self.assertTrue({'KerbalEVA', 'FlagSite', 'FlagDecalBackground'}.issubset(modules))
        for name in ('kerbalEVA', 'kerbalEVAfemale', 'kerbalEVAvintage',
                     'kerbalEVAfemaleFuture', 'flag', 'flagPartFlat', 'flagPartSize4'):
            self.assertTrue(any(fnmatch.fnmatchcase(name, pattern) for pattern in names), name)
        for name in ('mk1pod', 'evaJetpack', 'flagDecalAdapter', 'fuelTank'):
            self.assertFalse(any(fnmatch.fnmatchcase(name, pattern) for pattern in names), name)
        self.assertNotIn('FlagDecal', modules)
        self.assertNotIn('ModuleCommand', modules)

    def test_runtime_classifier_checks_modules_and_both_names(self):
        for token in ('candidate.name', 'candidate.partInfo.name',
                      'module is KerbalEVA', 'module is FlagSite', 'module is FlagDecalBackground',
                      'name.StartsWith("kerbalEVA"', 'name.StartsWith("flagPart"',
                      'name.Equals("flag"', 'StringComparison.OrdinalIgnoreCase'):
            self.assertIn(token, MAIN)
        self.assertNotRegex(MAIN, r'module is FlagDecal\b')

    def test_both_startup_paths_exit_before_geometry_and_integrations(self):
        for method in ('OnStart', 'OnStartFinished'):
            self.assertIn(f'base.{method}(state);\n            if (SkipKerbalOrFlagStartup()) return;', MAIN)
        guard = MAIN.split('private bool SkipKerbalOrFlagStartup()')[1].split('enum NodeApplyReason')[0]
        for token in ('initialized = false', 'ignoredKerbalOrFlag = true',
                      'field.guiActive = false', 'field.guiActiveEditor = false',
                      'action.active = false', 'isEnabled = false', 'enabled = false'):
            self.assertIn(token, guard)

    def test_mass_and_cost_callbacks_are_neutral_even_before_start(self):
        for method, arg in (('GetModuleMass','defaultMass'),('GetModuleCost','defaultCost')):
            self.assertIn(f'public float {method}(float {arg}, ModifierStagingSituation sit)\n        {{\n            if (IgnoreKerbalOrFlag) return 0f;', MAIN)
        self.assertIn('ignoredKerbalOrFlag || (!initialized && IsKerbalOrFlagPart(part))', MAIN)

    def test_no_load_save_or_copy_geometry_for_excluded_parts(self):
        for method in ('OnLoad', 'OnSave'):
            self.assertIn(f'base.{method}(node);\n            if (IgnoreKerbalOrFlag) return;', MAIN)
        self.assertIn('if (IgnoreKerbalOrFlag || IsKerbalOrFlagPart(copy.part)) return;', MAIN)
