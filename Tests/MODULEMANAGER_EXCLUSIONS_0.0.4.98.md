# Manual KSP test — ModuleManager exclusions (0.0.4.98)

1. Start a KSP 1.12.x install with PartSculptor, ModuleManager and representative mods supplying each of the `ProceduralPart`, `WingProcedural`, `SmartTankPart`, `DiameterMatcher`, and `TextureDefaulter` modules.
2. Search `GameData/ModuleManager.ConfigCache` for parts containing each of these modules. Verify none receives `ModulePartSculptor` from this injection patch (unless explicitly added by another patch).
3. Check normal stock parts, including crewed parts: they should receive exactly one `ModulePartSculptor`.
4. Check a part which already defines `ModulePartSculptor`: there should still be only one instance.
5. Verify the exclusion works if a part contains any ONE of these modules (rather than requiring several), and that simply installing the companion mods does not suppress injection on unrelated parts.
6. Re-enter the editor and confirm the excluded parts do not show PartSculptor controls. Existing saved craft with serialized `ModulePartSculptor` are not automatically cleaned by the patch.

The Python regression suite verifies patch structure and metadata, not live ModuleManager evaluation; verify actual behavior in-game.
