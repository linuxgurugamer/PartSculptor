# EVA and flag exclusions — in-game checks

1. Start KSP 1.12.x with ModuleManager. Check ModuleManager.ConfigCache: EVA variants, planted flags and flagPart panels must lack ModulePartSculptor, while a normal crewed pod with a flag decal must retain it.
2. Temporarily add ModulePartSculptor to an EVA/flag part via a test patch after the normal injection. Start a flight, EVA, and plant a flag. Verify no PartSculptor UI or geometry, resource, mass or cost changes, and no exceptions in KSP.log. Repeat with a modded part carrying KerbalEVA or FlagSite under a different name.
3. Remove the test patch and reload a save with stale module data. Check ordinary parts still stretch, morph and save/reload normally.
