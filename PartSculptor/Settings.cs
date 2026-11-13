using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;

using static PartSculptor.RegisterToolbar;

namespace PartSculptor
{
    // Read once directly from PluginData/Settings.cfg (GameDatabase excludes PluginData).
    public static class Settings
    {
        public static float MinScale = 0.05f;
        public static float MaxScale = 100f;
        public static float IncrementLarge = 0.25f;
        public static float IncrementSmall = 0.05f;
        public static float IncrementSlide = 0.001f;
        // Absolute bound on either local face-tilt slope (not degrees).
        // Default +/-2.5; maximum allowed configured magnitude remains 4.0.
        public const float DefaultMaxFaceTilt = 2.5f;
        public static float MaxFaceTilt = DefaultMaxFaceTilt;
        public static bool ScaleChildrenDefault = false;
        public static bool LockProportionsDefault = false;
        // A new face-morph part defaults to X/Z-coupled resizing.
        public static bool LockMorphXZDefault = true;
        public static bool MatchNodeSizesDefault = false;
        public static bool MatchAttachedFacesDefault = false;
        public static bool CopyAttachedMorphStepsDefault = false;
        // Keep the PAW opener but put all other PartSculptor controls in its window.
        public static bool HidePartSculptorPAWControls = false;

        // These limits apply to the non-uniform mass factor and the final
        // geometric volume (including an optional TweakScale multiplier).
        public static float MaxMassMultiplier = 10000f;
        public static float MaxVolumeMultiplier = 10000f;
        public static float WarnMassMultiplier = 100f;
        public static float WarnVolumeMultiplier = 100f;

        // Career progression uses the level of the building used for editing:
        // VAB in the VAB, Spaceplane Hangar in the SPH. These tier limits
        // apply only to new edits, not saved craft dimensions.
        public static bool EnableCareerLimits = true;
        public static float CareerLevel1MinScale = 0.75f;
        public static float CareerLevel1MaxScale = 1.5f;
        public static float CareerLevel1MaxVolume = 3f;
        public static float CareerLevel2MinScale = 0.5f;
        public static float CareerLevel2MaxScale = 3f;
        public static float CareerLevel2MaxVolume = 27f;
        public static float CareerLevel3MinScale = 0.25f;
        public static float CareerLevel3MaxScale = 10f;
        public static float CareerLevel3MaxVolume = 1000f;

        // Optional restrictions, off by default to preserve existing crafts.
        public static bool RestrictWheels = false;
        public static bool RestrictDockingPorts = false;

        // Docking capture envelopes follow the actual stretched face diameter.
        // Auto compatibility changes ONLY stock, single-type undocked ports.
        public static bool ScaleDockingCaptureDistances = true;
        public static bool MatchDockingPortTypes = true;
        public static float DockingStockSizeTolerance = 0.05f;
        public static bool RestrictAnimatedParts = false;

        // Unannounced fuel-switcher updates: only suspect parts are checked
        // periodically, rather than rescanning every part every few frames.
        public static float ResourceFallbackSeconds = 10f;
        public static bool CheckAllResourcesPeriodically = false;

        // Visual FX are sized from the same absolute X/Y/Z stretch axes.
        // Waterfall is optional; effects already under `model` inherit scale.
        public static bool ScaleEngineEffects = true;
        public static bool ScaleWaterfallEffects = true;
        public static float EffectRefreshSeconds = 2f;

        // Time for screen messages to stay on screen
        public static float screenMessageTime = 10f;

        // Log every actual scaling change to KSP.log. Enabled by default in
        // Debug builds; optional in Release builds via debugLogChanges = true.
#if DEBUG
        public static bool DebugLogChanges = true;
#else
        public static bool DebugLogChanges = false;
#endif

        static bool loaded;

        public static void Load()
        {
            if (loaded) return;
            loaded = true;
            // KSP's GameDatabase deliberately ignores PluginData. Read the
            // distributed settings directly, just like Presets.cfg.
            string path = Path.Combine(KSPUtil.ApplicationRootPath,
                "GameData/PartSculptor/PluginData/Settings.cfg");
            try
            {
                if (File.Exists(path))
                {
                    ConfigNode root = ConfigNode.Load(path);
                    if (root != null)
                    {
                        var settingsNodes = new List<ConfigNode>();
                        if (root.name == "PARTSCULPTOR_SETTINGS") settingsNodes.Add(root);
                        else settingsNodes.AddRange(root.GetNodes("PARTSCULPTOR_SETTINGS"));
                        foreach (ConfigNode n in settingsNodes)
                        {
                            n.TryGetValue("minScale", ref MinScale);
                            n.TryGetValue("maxScale", ref MaxScale);
                            n.TryGetValue("incrementLarge", ref IncrementLarge);
                            n.TryGetValue("incrementSmall", ref IncrementSmall);
                            n.TryGetValue("incrementSlide", ref IncrementSlide);
                            n.TryGetValue("maxFaceTilt", ref MaxFaceTilt);
                            n.TryGetValue("scaleChildrenDefault", ref ScaleChildrenDefault);
                            n.TryGetValue("lockProportionsDefault", ref LockProportionsDefault);
                            n.TryGetValue("lockMorphXZDefault", ref LockMorphXZDefault);
                            n.TryGetValue("matchNodeSizesDefault", ref MatchNodeSizesDefault);
                            n.TryGetValue("matchAttachedFacesDefault", ref MatchAttachedFacesDefault);
                            n.TryGetValue("copyAttachedMorphStepsDefault", ref CopyAttachedMorphStepsDefault);
                            n.TryGetValue("hidePartSculptorPAWControls", ref HidePartSculptorPAWControls);
                            n.TryGetValue("maxMassMultiplier", ref MaxMassMultiplier);
                            n.TryGetValue("maxVolumeMultiplier", ref MaxVolumeMultiplier);
                            n.TryGetValue("warnMassMultiplier", ref WarnMassMultiplier);
                            n.TryGetValue("warnVolumeMultiplier", ref WarnVolumeMultiplier);
                            n.TryGetValue("enableCareerLimits", ref EnableCareerLimits);
                            n.TryGetValue("careerLevel1MinScale", ref CareerLevel1MinScale);
                            n.TryGetValue("careerLevel1MaxScale", ref CareerLevel1MaxScale);
                            n.TryGetValue("careerLevel1MaxVolume", ref CareerLevel1MaxVolume);
                            n.TryGetValue("careerLevel2MinScale", ref CareerLevel2MinScale);
                            n.TryGetValue("careerLevel2MaxScale", ref CareerLevel2MaxScale);
                            n.TryGetValue("careerLevel2MaxVolume", ref CareerLevel2MaxVolume);
                            n.TryGetValue("careerLevel3MinScale", ref CareerLevel3MinScale);
                            n.TryGetValue("careerLevel3MaxScale", ref CareerLevel3MaxScale);
                            n.TryGetValue("careerLevel3MaxVolume", ref CareerLevel3MaxVolume);
                            n.TryGetValue("restrictWheels", ref RestrictWheels);
                            n.TryGetValue("restrictDockingPorts", ref RestrictDockingPorts);
                            n.TryGetValue("scaleDockingCaptureDistances", ref ScaleDockingCaptureDistances);
                            n.TryGetValue("matchDockingPortTypes", ref MatchDockingPortTypes);
                            n.TryGetValue("dockingStockSizeTolerance", ref DockingStockSizeTolerance);
                            n.TryGetValue("restrictAnimatedParts", ref RestrictAnimatedParts);
                            n.TryGetValue("resourceFallbackSeconds", ref ResourceFallbackSeconds);
                            n.TryGetValue("checkAllResourcesPeriodically", ref CheckAllResourcesPeriodically);
                            n.TryGetValue("scaleEngineEffects", ref ScaleEngineEffects);
                            n.TryGetValue("scaleWaterfallEffects", ref ScaleWaterfallEffects);
                            n.TryGetValue("effectRefreshSeconds", ref EffectRefreshSeconds);
                            n.TryGetValue("debugLogChanges", ref DebugLogChanges);
                        }
                    }
                }
                else Log.Warning("[PartSculptor] Settings file not found: " + path);
            }
            catch (Exception e)
            {
                Log.Error("[PartSculptor] Unable to load settings from " + path + ": " + e);
            }
            if (!(MinScale > 0f)) MinScale = 0.001f;
            if (!(MaxScale > MinScale)) MaxScale = MinScale * 10f;
            if (!(IncrementLarge > 0f)) IncrementLarge = 0.25f;
            if (!(IncrementSmall > 0f)) IncrementSmall = 0.05f;
            if (!(IncrementSlide > 0f)) IncrementSlide = 0.001f;
            // Reject NaN, infinity, nonpositive and impractically large
            // values rather than letting a malformed cfg disable the clamp.
            if (!(MaxFaceTilt > 0f) || float.IsInfinity(MaxFaceTilt) || MaxFaceTilt > 4f)
                MaxFaceTilt = DefaultMaxFaceTilt;
            if (!(MaxMassMultiplier > 0f) || float.IsInfinity(MaxMassMultiplier)) MaxMassMultiplier = 10000f;
            if (!(MaxVolumeMultiplier > 0f) || float.IsInfinity(MaxVolumeMultiplier)) MaxVolumeMultiplier = 10000f;
            if (!(WarnMassMultiplier > 1f) || float.IsInfinity(WarnMassMultiplier)) WarnMassMultiplier = 100f;
            if (!(WarnVolumeMultiplier > 1f) || float.IsInfinity(WarnVolumeMultiplier)) WarnVolumeMultiplier = 100f;
            if (!(ResourceFallbackSeconds >= 1f) || float.IsInfinity(ResourceFallbackSeconds)) ResourceFallbackSeconds = 10f;
            if (!(DockingStockSizeTolerance >= 0f) || DockingStockSizeTolerance > 0.25f ||
                float.IsNaN(DockingStockSizeTolerance)) DockingStockSizeTolerance = 0.05f;
            if (!(EffectRefreshSeconds >= 0.25f) || float.IsInfinity(EffectRefreshSeconds))
                EffectRefreshSeconds = 2f;
            // A user-set default takes precedence over the shipped Settings.cfg.
            // The PAW toggle itself remains a session-level control.
            ScaleUserSettings.Load(ref MatchNodeSizesDefault, ref ScaleChildrenDefault,
                ref LockProportionsDefault, ref LockMorphXZDefault,
                ref MatchAttachedFacesDefault);
            NormalizeCareerTier(ref CareerLevel1MinScale, ref CareerLevel1MaxScale, ref CareerLevel1MaxVolume,
                0.75f, 1.5f, 3f);
            NormalizeCareerTier(ref CareerLevel2MinScale, ref CareerLevel2MaxScale, ref CareerLevel2MaxVolume,
                0.5f, 3f, 27f);
            NormalizeCareerTier(ref CareerLevel3MinScale, ref CareerLevel3MaxScale, ref CareerLevel3MaxVolume,
                0.25f, 10f, 1000f);
            Log.Info(string.Format("[PartSculptor] Axes {0} .. {1}; mass factor <= {2}; total volume factor <= {3}",
                MinScale, MaxScale, MaxMassMultiplier, MaxVolumeMultiplier));
        }

        static void NormalizeCareerTier(ref float min, ref float max, ref float volume,
            float defaultMin, float defaultMax, float defaultVolume)
        {
            if (!(min > 0f) || float.IsInfinity(min)) min = defaultMin;
            if (!(max >= min) || float.IsInfinity(max)) max = Math.Max(min, defaultMax);
            if (!(volume > 0f) || float.IsInfinity(volume)) volume = defaultVolume;
        }
    }
}