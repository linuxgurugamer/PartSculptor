"""Regression checks for PartSculptor project identity, config and module setup."""
from pathlib import Path
import json
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'PartSculptor'
GAME = ROOT / 'GameData' / 'PartSculptor'

class PartSculptorRename471(unittest.TestCase):
    def test_new_project_and_assembly_identifiers(self):
        self.assertTrue((ROOT/'PartSculptor.slnx').is_file())
        csproj = SOURCE/'PartSculptor.csproj'
        self.assertTrue(csproj.is_file())
        doc = ET.parse(str(csproj)).getroot()
        ns = {'ms': 'http://schemas.microsoft.com/developer/msbuild/2003'}
        self.assertEqual(doc.findtext('.//ms:AssemblyName', namespaces=ns), 'PartSculptor')
        self.assertEqual(doc.findtext('.//ms:RootNamespace', namespaces=ns), 'PartSculptor')
        for item in doc.findall('.//ms:Compile', namespaces=ns):
            self.assertTrue((SOURCE / item.attrib['Include'].replace('\\', '/')).is_file(), item.attrib['Include'])
        self.assertIn('PartSculptor.version', (SOURCE/'AssemblyVersion.tt').read_text())
        self.assertIn('AssemblyTitle("PartSculptor")', (SOURCE/'Properties/AssemblyInfo.cs').read_text())

    def test_version_names_paths_and_number(self):
        for p in [ROOT/'PartSculptor.version', GAME/'PartSculptor.version']:
            data = json.loads(p.read_text())
            self.assertEqual(data['NAME'], 'PartSculptor')
            self.assertEqual(data['VERSION']['BUILD'], 130)
            self.assertEqual(data['GITHUB']['REPOSITORY'], 'PartSculptor')
        self.assertIn('AssemblyVersion("0.0.4.130")', (SOURCE/'AssemblyVersion.cs').read_text())

    def test_module_identity_and_saved_data_nodes(self):
        code = (SOURCE/'PartSculptor.cs').read_text()
        self.assertIn('public class ModulePartSculptor : PartModule', code)
        self.assertIn('const string RESOURCE_NODE = "PARTSCULPTOR_RESOURCE";', code)
        self.assertIn('const string EXPONENT_BASE_NODE = "PARTSCULPTOR_EXPONENT_BASE";', code)
        exponents = (SOURCE/'ScaleExponents.cs').read_text()
        self.assertIn('ModulePartSculptor owner', exponents)
        self.assertIn('"ModulePartSculptor"', exponents)

    def test_all_parts_patch_and_exponents(self):
        patches = list((GAME/'Patches').glob('*.cfg'))
        self.assertEqual(set(p.name for p in patches), {'PartSculptor.cfg','ScaleExponents.cfg'})
        self.assertIn('@PART[*]:HAS[!MODULE[ModulePartSculptor],!MODULE[ProceduralPart],!MODULE[WingProcedural],!MODULE[SmartTankPart],!MODULE[DiameterMatcher],!MODULE[TextureDefaulter],!MODEL:HAS[#model[ModularSegmentedSRBs/*]]]:NEEDS[PartSculptor]:FINAL',
                      (GAME/'Patches/PartSculptor.cfg').read_text())
        self.assertIn('name = ModulePartSculptor', (GAME/'Patches/PartSculptor.cfg').read_text())
        self.assertFalse((GAME/'PatchManager').exists())

    def test_cfg_paths_and_user_preferences(self):
        self.assertIn('PARTSCULPTOR_SETTINGS', (GAME/'PluginData/Settings.cfg').read_text())
        self.assertIn('PARTSCULPTOR_PRESETS', (GAME/'PluginData/Presets.cfg').read_text())
        settings=(SOURCE/'ScaleSettingsWindow.cs').read_text()
        presets=(SOURCE/'ScalePresetRegistry.cs').read_text()
        for marker in ['PARTSCULPTOR_USER_SETTINGS','GameData/PartSculptor']:
            self.assertIn(marker, settings)
        for marker in ['PARTSCULPTOR_CUSTOM_PRESETS', 'GameData/PartSculptor']:
            self.assertIn(marker, presets)

    def test_documentation_and_deploy_paths(self):
        readme = (ROOT/'README.md').read_text()
        self.assertTrue(readme.startswith('# PartSculptor'))
        self.assertNotIn('ThisMod', readme)
        self.assertIn('## Installation', readme)
        self.assertIn('ModulePartSculptor', readme)
        self.assertIn('## Fuel/Part Switchers', readme)
        self.assertIn('**SpaceTuxLibrary**', readme)
        self.assertIn('set GAMEDIR=PartSculptor', (ROOT/'deploy.bat').read_text())
        self.assertIn('set GAMEDIR=PartSculptor', (ROOT/'buildRelease.bat').read_text())
        self.assertIn('1:PartSculptor, PartSculptor.version', (ROOT/'jenkins.txt').read_text())

if __name__ == '__main__':
    unittest.main()
