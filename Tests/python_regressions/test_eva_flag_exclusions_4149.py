"""Source contract for EVA/flag exclusions. In-game validation remains required."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
PATCH = (ROOT/'GameData/PartSculptor/Patches/PartSculptor.cfg').read_text()
SOURCE = (ROOT/'PartSculptor/PartSculptor.cs').read_text()

class EVAFlagExclusions(unittest.TestCase):
    def test_patch_filters(self):
        line = next(l for l in PATCH.splitlines() if l.startswith('@PART'))
        for token in ('!MODULE[KerbalEVA]', '!MODULE[FlagSite]',
                      '~name[kerbalEVA*]', '~name[flag]', '~name[flagSite*]',
                      '~name[flagPart*]', '!MODULE[ProceduralPart]'):
            self.assertIn(token, line)
        for token in ('CrewCapacity', 'FlagDecal', 'ModuleCommand'):
            self.assertNotIn(token, line)
        self.assertTrue(line.endswith(':NEEDS[PartSculptor]:FINAL'))

    def test_runtime_detection(self):
        self.assertIn('module is KerbalEVA || module is FlagSite', SOURCE)
        self.assertIn('IsExcludedPartName(candidate.partInfo.name)', SOURCE)
        for name in ('kerbalEVA', 'flag', 'flagSite', 'flagPart'):
            self.assertRegex(SOURCE, r'"'+name+r'", StringComparison.OrdinalIgnoreCase')

    def test_start_guard_precedes_geometry(self):
        body = SOURCE.split('public override void OnStart(StartState state)',1)[1].split('model = part.transform.Find',1)[0]
        for token in ('if (IsIgnoredPart)', 'ignoredPart = true;', 'HideFields();',
                      'isEnabled = false;', 'enabled = false;', 'return;'):
            self.assertIn(token, body)

    def test_early_and_direct_callbacks(self):
        for method in ('OnLoad', 'OnSave', 'OnStartFinished', 'Update', 'LateUpdate'):
            self.assertRegex(SOURCE, r'void '+method+r'\([^)]*\)\s*\{\s*(?:base\.[^;]+;\s*)?if \(IsIgnoredPart\) return;')
        for method in ('GetModuleMass', 'GetModuleCost'):
            self.assertRegex(SOURCE, r'float '+method+r'\([^)]*\)\s*\{\s*if \(IsIgnoredPart\) return 0f;')
        self.assertIn('ignoredPart || (!initialized && IsExcludedPart(part))', SOURCE)
        self.assertIn('if (IsIgnoredPart || model == null) return;', SOURCE)
