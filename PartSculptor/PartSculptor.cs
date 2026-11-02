// PartSculptor - per-axis (non-uniform) part scaling for KSP 1.12.x
// Scales model, colliders, attach nodes, dry mass and stock resource capacity.
// Dry mass and resource capacity scale with the non-uniform volume (X * Y * Z).
// Other part/module stats follow TWEAKSCALEEXPONENTS rules loaded from GameData.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;

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
        public static bool ScaleChildrenDefault = false;
        public static bool LockProportionsDefault = false;
        // A new face-morph part defaults to X/Z-coupled resizing.
        public static bool LockMorphXZDefault = true;
        public static bool MatchNodeSizesDefault = false;

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
                            n.TryGetValue("scaleChildrenDefault", ref ScaleChildrenDefault);
                            n.TryGetValue("lockProportionsDefault", ref LockProportionsDefault);
                            n.TryGetValue("lockMorphXZDefault", ref LockMorphXZDefault);
                            n.TryGetValue("matchNodeSizesDefault", ref MatchNodeSizesDefault);
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
                else Debug.LogWarning("[PartSculptor] Settings file not found: " + path);
            }
            catch (Exception e)
            {
                Debug.LogError("[PartSculptor] Unable to load settings from " + path + ": " + e);
            }
            if (!(MinScale > 0f)) MinScale = 0.001f;
            if (!(MaxScale > MinScale)) MaxScale = MinScale * 10f;
            if (!(IncrementLarge > 0f)) IncrementLarge = 0.25f;
            if (!(IncrementSmall > 0f)) IncrementSmall = 0.05f;
            if (!(IncrementSlide > 0f)) IncrementSlide = 0.001f;
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
                ref LockProportionsDefault, ref LockMorphXZDefault);
            NormalizeCareerTier(ref CareerLevel1MinScale, ref CareerLevel1MaxScale, ref CareerLevel1MaxVolume,
                0.75f, 1.5f, 3f);
            NormalizeCareerTier(ref CareerLevel2MinScale, ref CareerLevel2MaxScale, ref CareerLevel2MaxVolume,
                0.5f, 3f, 27f);
            NormalizeCareerTier(ref CareerLevel3MinScale, ref CareerLevel3MaxScale, ref CareerLevel3MaxVolume,
                0.25f, 10f, 1000f);
            Debug.Log(string.Format("[PartSculptor] Axes {0} .. {1}; mass factor <= {2}; total volume factor <= {3}",
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

    // Three stock facility progression tiers. KSP exposes the normalized
    // facility level and the facility's upgrade count; use both to translate
    // that level into the familiar 1/2/3 levels shown in Career Mode.
    internal struct CareerScaleLimit
    {
        internal string facility;
        internal int level;
        internal float minAxis;
        internal float maxAxis;
        internal float maxVolume;
    }

    public class ModuleStretchScale : PartModule, IPartMassModifier, IPartCostModifier
    {
        const string GROUP = "PartSculptor";
        const string GROUP_TITLE = "PartSculptor";
        // Attribute defaults only; real limits come from Settings at runtime.
        const float MIN = 0.05f;
        const float MAX = 100f;

        // Default PAW group state is chosen when the DLL is compiled.
        // Debug builds show the controls; Release builds start collapsed.
#if DEBUG
        const bool PAW_START_COLLAPSED = false;
#else
        const bool PAW_START_COLLAPSED = true;
#endif

        // "Stretch children" is one switch shared by every part, like TweakScale's.
        public static bool ScaleChildrenGlobal;
        public static bool MatchNodeSizesGlobal;
        static bool globalInit;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Scale X (width)", guiFormat = "F3",
            groupName = GROUP, groupDisplayName = GROUP_TITLE, groupStartCollapsed = PAW_START_COLLAPSED)]
        [UI_FloatEdit(scene = UI_Scene.Editor, minValue = MIN, maxValue = MAX,
            incrementLarge = 0.25f, incrementSmall = 0.05f, incrementSlide = 0.001f,
            sigFigs = 3, affectSymCounterparts = UI_Scene.Editor)]
        public float scaleX = 1f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Scale Y (height)", guiFormat = "F3",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        [UI_FloatEdit(scene = UI_Scene.Editor, minValue = MIN, maxValue = MAX,
            incrementLarge = 0.25f, incrementSmall = 0.05f, incrementSlide = 0.001f,
            sigFigs = 3, affectSymCounterparts = UI_Scene.Editor)]
        public float scaleY = 1f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Scale Z (depth)", guiFormat = "F3",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        [UI_FloatEdit(scene = UI_Scene.Editor, minValue = MIN, maxValue = MAX,
            incrementLarge = 0.25f, incrementSmall = 0.05f, incrementSlide = 0.001f,
            sigFigs = 3, affectSymCounterparts = UI_Scene.Editor)]
        public float scaleZ = 1f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Lock proportions",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        [UI_Toggle(scene = UI_Scene.Editor, enabledText = "On", disabledText = "Off",
            affectSymCounterparts = UI_Scene.Editor)]
        public bool lockAxes = false;

        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Stretch children",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        [UI_Toggle(scene = UI_Scene.Editor, enabledText = "On", disabledText = "Off",
            affectSymCounterparts = UI_Scene.None, suppressEditorShipModified = true)]
        public bool scaleChildren = false;

        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Match node sizes",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        [UI_Toggle(scene = UI_Scene.Editor, enabledText = "On", disabledText = "Off",
            affectSymCounterparts = UI_Scene.None, suppressEditorShipModified = true)]
        public bool matchNodeSizes = false;

        // Per-part face-morph control: ordinary corner drags preserve the
        // selected face's X/Z aspect ratio when enabled. Does not affect
        // the three-axis Stretch gizmo or Alt-modified morph operations.
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Lock X/Z",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        [UI_Toggle(scene = UI_Scene.Editor, enabledText = "On", disabledText = "Off",
            affectSymCounterparts = UI_Scene.Editor)]
        public bool lockMorphXZ = true;

        // Two planar, deformable Y end faces. Values are saved with the craft
        // and applied in editor AND flight from immutable mesh vertices.
        [KSPField(isPersistant = true)] public float morphBottomX = 1f;
        [KSPField(isPersistant = true)] public float morphBottomZ = 1f;
        [KSPField(isPersistant = true)] public float morphBottomTiltX = 0f;
        [KSPField(isPersistant = true)] public float morphBottomTiltZ = 0f;
        // Linked face translation (normalized to each native half-width/depth).
        [KSPField(isPersistant = true)] public float morphBottomOffsetX = 0f;
        [KSPField(isPersistant = true)] public float morphBottomOffsetZ = 0f;
        [KSPField(isPersistant = true)] public float morphTopX = 1f;
        [KSPField(isPersistant = true)] public float morphTopZ = 1f;
        [KSPField(isPersistant = true)] public float morphTopTiltX = 0f;
        [KSPField(isPersistant = true)] public float morphTopTiltZ = 0f;
        [KSPField(isPersistant = true)] public float morphTopOffsetX = 0f;
        [KSPField(isPersistant = true)] public float morphTopOffsetZ = 0f;

        // Optional GameData behavior presets. No TweakScale PartModule is
        // required: these names resolve against TWEAKSCALEBEHAVIOR configs.
        // Inherited presets apply first; behavior and local exponent rules win.
        [KSPField(isPersistant = true)]
        public string behavior = "";

        [KSPField(isPersistant = true)]
        public string inheritBehavior = "";

        // Read-only, calculated editor values (not saved with the craft).
        [KSPField(guiActiveEditor = true, guiName = "Volume multiplier",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        public string displayVolume = "1x";

        [KSPField(guiActiveEditor = true, guiName = "Dry mass",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        public string displayDryMass = "";

        [KSPField(guiActiveEditor = true, guiName = "Dry cost",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        public string displayDryCost = "";

        [KSPField(guiActiveEditor = true, guiName = "Tank capacity",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        public string displayCapacity = "";

        [KSPField(guiActiveEditor = true, guiName = "Scale safety",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        public string displayScaleSafety = "OK";

        [KSPField(guiActiveEditor = true, guiName = "Career scale limits",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        public string displayCareerLimits = "";

        // The PAW quick selector offers absolute presets plus a readout-only
        // Custom entry for manual edits and relative preset applications.
        // Keep the last applied command separately so Repeat preset works for relative presets
        // selected from the full Presets window without adding them to the PAW.
        [KSPField(guiActiveEditor = true, guiName = "Scale preset",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        [UI_ChooseOption(scene = UI_Scene.Editor,
            options = new string[] { "1x uniform" })]
        public string scalePreset = "1x uniform";

        string lastAppliedPreset = "1x uniform";
        bool lastAppliedRelative;
        internal string LastAppliedPreset { get { return lastAppliedPreset; } }
        internal bool LastAppliedRelative { get { return lastAppliedRelative; } }

        // Last values seen by the field callbacks (used for proportional lock).
        // OnLoad sees whether an existing craft explicitly persisted its lock
        // setting; settings defaults must never overwrite that choice.
        bool hasSavedLockAxes;
        bool hasSavedLockMorphXZ;
        float lastX = 1f, lastY = 1f, lastZ = 1f;
        bool lastLoggedLockAxes;

        Transform model;
        Vector3 prefabModelScale = Vector3.one;
        readonly Dictionary<string, Vector3> prefabNodePos = new Dictionary<string, Vector3>();
        readonly Dictionary<string, Vector3> prefabNodeOrigPos = new Dictionary<string, Vector3>();
        readonly Dictionary<string, Vector3> prefabNodeOrientation = new Dictionary<string, Vector3>();
        readonly Dictionary<string, int> prefabNodeSize = new Dictionary<string, int>();
        Vector3 prefabSrfPos;
        Vector3 prefabSrfOrientation;
        int prefabSrfSize;
        bool hasSrfNode;
        bool initialized;
        readonly NodeAttachWatcher nodeAttachWatcher = new NodeAttachWatcher();
        readonly AttachmentLoadFix attachmentFix = new AttachmentLoadFix();
        internal AttachmentLoadFix AttachmentFix { get { return attachmentFix; } }
        internal bool AttachmentReady { get { return initialized || model == null; } }
        internal bool HasStretchedGeometry
        {
            get { return initialized && (Mathf.Abs(scaleX - 1f) > 0.0001f ||
                Mathf.Abs(scaleY - 1f) > 0.0001f ||
                Mathf.Abs(scaleZ - 1f) > 0.0001f); }
        }

        // Per-instance tracking prevents switchers which replace resources from
        // inheriting the capacity of a previously selected tank configuration.
        // Resource modules own their native (including TweakScale) capacities;
        // this module owns ONLY the additional X*Y*Z multiplier.
        sealed class ResourceScaleData
        {
            public PartResource resource;
            public string name;
            public double nativeCapacity;        // resource capacity before stretching
            public double lastWrittenCapacity;   // detects writes by another module
        }

        readonly Dictionary<PartResource, ResourceScaleData> liveResourceScale =
            new Dictionary<PartResource, ResourceScaleData>();
        readonly Dictionary<string, double> savedResourceBases =
            new Dictionary<string, double>();
        readonly Dictionary<string, double> prefabResourceCapacity =
            new Dictionary<string, double>();
        bool resourcesInitialized;
        bool resourceCheckPending;
        bool resourceDisplayDirty;
        bool hasDynamicResourceProvider;
        float nextResourceCheckTime;
        int nextGeometryCheckFrame;
        Vector3 lastAcceptedScale = Vector3.one;
        string restrictedReason;
        string limitWarning = "";
        string lastLoggedWarning = "";
        bool writingResources;
        bool changeAlreadyValidated;
        PartExponentScaler exponentScaler;
        CrewCapacityScaling crewCapacityScaling;
        EngineEffectScaling visualEffects;
        DockingPortScaling dockingPorts;
        FaceMorphSupport faceMorph;
        readonly ScaledValuesWindow scaledValuesWindow = new ScaledValuesWindow();

        // A single ship notification is delivered after ALL direct, symmetry,
        // and descendant edits in the current frame. Avoid re-triggering our
        // own resource checks on every part during this notification.
        static bool shipNotificationPending;
        static bool dispatchingShipNotification;
        static int shipNotificationQueuedFrame;

        // Only one editor event subscription for the entire mod, regardless
        // of craft part count. A second (usually small) set contains tanks
        // whose module can rewrite resource capacities.
        static readonly HashSet<ModuleStretchScale> editorModules =
            new HashSet<ModuleStretchScale>();
        static readonly HashSet<ModuleStretchScale> dynamicEditorModules =
            new HashSet<ModuleStretchScale>();

        // KSP's EventData.Add does not accept static method delegates:
        // its internal EvtDelegate constructor dereferences delegate.Target.
        // Keep one long-lived INSTANCE target, so we still subscribe once
        // for the whole mod without binding the listener to any individual
        // part that might be deleted while others remain in the editor.
        sealed class EditorShipModifiedListener
        {
            public void OnShipModified(ShipConstruct ship)
            {
                OnAnyEditorShipModified(ship);
            }
        }

        static readonly EditorShipModifiedListener editorShipModifiedListener =
            new EditorShipModifiedListener();
        static bool editorShipListenerRegistered;

        // Legacy saves store the total multiplier, not just nonuniform stretch.
        // Saved explicitly in OnSave/OnLoad; double is not a reliable KSPField.
        public double lastAppliedResourceVolume = 1d;
        const string RESOURCE_NODE = "STRETCHSCALE_RESOURCE";
        const string B9_RESOURCE_NODE = "STRETCHSCALE_B9_RESOURCE";
        const string B9_VOLUME_NODE = "STRETCHSCALE_B9_BASE_VOLUME";
        // Baseline per B9 switcher, independent of selected subtype.
        readonly Dictionary<string, double> b9BaseVolumes =
            new Dictionary<string, double>(StringComparer.Ordinal);
        const string B9_NODE_BASELINE = "STRETCHSCALE_B9_NODE";
        sealed class B9NodeData
        {
            public Vector3 position;
            public Vector3 original;
            public Vector3 orientation;
            public int size;
        }
        readonly Dictionary<string, B9NodeData> b9NodeBaselines =
            new Dictionary<string, B9NodeData>(StringComparer.Ordinal);
        static string B9NodeKey(string signature, string name)
        {
            return B9PartSwitchSupport.ResourceKey(signature, name);
        }
        static void SaveB9Vector(ConfigNode node, string prefix, Vector3 value)
        {
            node.AddValue(prefix + "X", value.x.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(prefix + "Y", value.y.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(prefix + "Z", value.z.ToString("R", CultureInfo.InvariantCulture));
        }
        static bool ReadB9Vector(ConfigNode node, string prefix, out Vector3 result)
        {
            result = Vector3.zero;
            float x, y, z;
            if (!float.TryParse(node.GetValue(prefix + "X"), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out x) ||
                !float.TryParse(node.GetValue(prefix + "Y"), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out y) ||
                !float.TryParse(node.GetValue(prefix + "Z"), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out z) ||
                float.IsInfinity(x) || float.IsNaN(x) ||
                float.IsInfinity(y) || float.IsNaN(y) ||
                float.IsInfinity(z) || float.IsNaN(z)) return false;
            result = new Vector3(x, y, z);
            return true;
        }

        // Native capacities are keyed by the complete B9 subtype combination
        // so returning to an earlier subtype cannot compound its volume.
        readonly Dictionary<string, double> b9ResourceBaselines =
            new Dictionary<string, double>(StringComparer.Ordinal);
        B9PartSwitchSupport b9Support;
        string b9Signature;
        bool b9SubtypeChangePending;
        bool b9ResourceRebasePending;
        int b9SubtypeChangedFrame;

        // Non-B9 tank switchers own their native contents. Persist separate
        // native capacities for every selected tank configuration so repeated
        // A -> B -> A switches and craft reloads cannot compound stretching.
        const string FUEL_SWITCH_RESOURCE_NODE = "STRETCHSCALE_FUEL_SWITCH_RESOURCE";
        readonly Dictionary<string, double> fuelSwitchResourceBases =
            new Dictionary<string, double>(StringComparer.Ordinal);
        FuelSwitchSupport fuelSwitchSupport;
        string fuelSwitchSignature;
        bool fuelSwitchChangePending;
        bool fuelSwitchResourceRebasePending;
        int fuelSwitchChangedFrame;

        string FuelSwitchKeySignature()
        {
            // Include both identities where a tank supports multiple systems.
            return (b9Signature ?? "") + "@" + (fuelSwitchSignature ?? "");
        }

        const string EXPONENT_BASE_NODE = "STRETCHSCALE_EXPONENT_BASE";
        // Per-binding native values are saved separately from stretched live
        // values so every edit is based on the original field, not the result
        // of the previous edit (also after editor reload or craft copy).
        readonly Dictionary<string, ConfigNode> savedExponentBaselines =
            new Dictionary<string, ConfigNode>(StringComparer.OrdinalIgnoreCase);
        internal IDictionary<string, ConfigNode> SavedExponentBaselines
        {
            get { return savedExponentBaselines; }
        }

        // TweakScale (optional, soft dependency via reflection)
        PartModule tsModule;
        FieldInfo tsFactorField;
        float lastBase = 1f;

        Vector3 Target { get { return new Vector3(scaleX, scaleY, scaleZ); } }

        // Uniform TweakScale factor that the stretch is applied on top of.
        float BaseFactor
        {
            get
            {
                if (tsModule == null || tsFactorField == null) return 1f;
                float f = (float)tsFactorField.GetValue(tsModule);
                return (f > 0f && !float.IsNaN(f) && !float.IsInfinity(f)) ? f : 1f;
            }
        }

        void FindTweakScale()
        {
            foreach (PartModule m in part.Modules)
            {
                for (System.Type t = m.GetType(); t != null && t != typeof(PartModule); t = t.BaseType)
                {
                    if (t.Name != "TweakScale") continue;
                    FieldInfo f = t.GetField("currentScaleFactor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (f == null || f.FieldType != typeof(float)) continue;
                    tsModule = m;
                    tsFactorField = f;

                    // Seed TweakScale's baseline model scale with the prefab's, so it never
                    // captures an already-stretched model as its "unscaled" size.
                    FieldInfo d = t.GetField("defaultTransformScale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (d != null && d.FieldType == typeof(Vector3))
                    {
                        Vector3 v = (Vector3)d.GetValue(m);
                        if (v.x == 0f) d.SetValue(m, prefabModelScale);
                    }
                    return;
                }
            }
        }

        // Sent by TweakScale after it has rescaled the part (it has already reset the
        // model scale and node positions to its own uniform values by then).
        [KSPEvent(guiActive = false, guiActiveEditor = false, active = true)]
        public void OnPartScaleChanged(BaseEventDetails data)
        {
            if (!initialized) return;
            // This runs in the middle of TweakScale's own rescale (before it moves on
            // to child parts), so it must never throw and must not fire editor events.
            try
            {
                AfterTweakScale();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[PartSculptor] Error handling TweakScale change on " + part.name + ": " + e);
            }
        }

        void OnMatchNodeSizesModified(object value)
        {
            MatchNodeSizesGlobal = matchNodeSizes;
        }

        void OnScaleChildrenModified(object value)
        {
            ScaleDebug.Change(part, "ui", "Stretch children", ScaleChildrenGlobal, scaleChildren);
            ScaleChildrenGlobal = scaleChildren;
        }

        void OnDestroy()
        {
            ScaleGizmo.HideFor(this);
            FaceMorphGizmo.HideFor(this);
            ScalePresetEditor.CloseFor(this);
            ScalePresetsWindow.CloseFor(this);
            scaledValuesWindow.Close();
            if (faceMorph != null) { faceMorph.Dispose(); faceMorph = null; }
            BaseField f = Fields != null ? Fields["scaleChildren"] : null;
            if (f != null) f.OnValueModified -= OnScaleChildrenModified;
            f = Fields != null ? Fields["matchNodeSizes"] : null;
            if (f != null) f.OnValueModified -= OnMatchNodeSizesModified;
            editorModules.Remove(this);
            dynamicEditorModules.Remove(this);
            // Unsubscribe only after the last PartSculptor part goes away.
            // Never register or remove a static callback with KSP EventData.
            if (editorModules.Count == 0)
            {
                if (editorShipListenerRegistered)
                {
                    if (GameEvents.onEditorShipModified != null)
                        GameEvents.onEditorShipModified.Remove(editorShipModifiedListener.OnShipModified);
                    editorShipListenerRegistered = false;
                }
                shipNotificationPending = false;
            }
        }

        void AfterTweakScale()
        {
            ScaleDebug.Change(part, "tweakscale", "base scale", lastBase, BaseFactor);
            lastBase = BaseFactor;
            // TweakScale already scaled children's attach points uniformly about this
            // part, which keeps them on our stretched surfaces; only fix ourselves.
            // TweakScale fires the ship-modified event itself when it's done.
            Apply(true, false, false);
        }

        // ------------------------------------------------------------------ setup

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node != null && node.HasValue("lockAxes")) hasSavedLockAxes = true;
            if (node != null && node.HasValue("lockMorphXZ")) hasSavedLockMorphXZ = true;
            attachmentFix.Load(node);
            if (node == null) return;
            double volume;
            if (double.TryParse(node.GetValue("lastAppliedResourceVolume"),
                NumberStyles.Float, CultureInfo.InvariantCulture, out volume) && Valid(volume))
                lastAppliedResourceVolume = volume;

            savedExponentBaselines.Clear();
            foreach (ConfigNode entry in node.GetNodes(EXPONENT_BASE_NODE))
            {
                string path = entry.GetValue("path");
                if (!string.IsNullOrEmpty(path)) savedExponentBaselines[path] = entry;
            }
            b9BaseVolumes.Clear();
            foreach (ConfigNode entry in node.GetNodes(B9_VOLUME_NODE))
            {
                string key = entry.GetValue("key");
                double native;
                if (!string.IsNullOrEmpty(key) &&
                    double.TryParse(entry.GetValue("nativeBaseVolume"), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out native) &&
                    native >= 0d && !double.IsInfinity(native) && !double.IsNaN(native))
                    b9BaseVolumes[key] = native;
            }
            b9NodeBaselines.Clear();
            foreach (ConfigNode entry in node.GetNodes(B9_NODE_BASELINE))
            {
                string signature = entry.GetValue("signature");
                string id = entry.GetValue("id");
                Vector3 pos, orig, normal;
                int size;
                if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(id) ||
                    !ReadB9Vector(entry, "position", out pos) ||
                    !ReadB9Vector(entry, "original", out orig) ||
                    !ReadB9Vector(entry, "orientation", out normal) ||
                    !int.TryParse(entry.GetValue("size"), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out size)) continue;
                b9NodeBaselines[B9NodeKey(signature, id)] = new B9NodeData {
                    position = pos, original = orig, orientation = normal, size = size };
            }
            b9ResourceBaselines.Clear();
            foreach (ConfigNode entry in node.GetNodes(B9_RESOURCE_NODE))
            {
                string signature = entry.GetValue("signature");
                string name = entry.GetValue("name");
                double native;
                if (!string.IsNullOrEmpty(signature) && !string.IsNullOrEmpty(name) &&
                    double.TryParse(entry.GetValue("nativeCapacity"),
                        NumberStyles.Float, CultureInfo.InvariantCulture, out native) && Valid(native))
                    b9ResourceBaselines[B9PartSwitchSupport.ResourceKey(signature, name)] = native;
            }
            fuelSwitchResourceBases.Clear();
            foreach (ConfigNode entry in node.GetNodes(FUEL_SWITCH_RESOURCE_NODE))
            {
                string signature = entry.GetValue("signature");
                string name = entry.GetValue("name");
                double native;
                if (!string.IsNullOrEmpty(signature) && !string.IsNullOrEmpty(name) &&
                    double.TryParse(entry.GetValue("nativeCapacity"),
                        NumberStyles.Float, CultureInfo.InvariantCulture, out native) &&
                    FuelSwitchMath.FinitePositive(native))
                    fuelSwitchResourceBases[FuelSwitchSupport.Key(signature, name)] = native;
            }
            savedResourceBases.Clear();
            foreach (ConfigNode entry in node.GetNodes(RESOURCE_NODE))
            {
                string name = entry.GetValue("name");
                double baseline;
                if (!string.IsNullOrEmpty(name) &&
                    double.TryParse(entry.GetValue("nativeCapacity"),
                        NumberStyles.Float, CultureInfo.InvariantCulture, out baseline) && Valid(baseline))
                    savedResourceBases[name] = baseline;
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            if (node == null) return;
            attachmentFix.Save(node, part, initialized);
            node.SetValue("lastAppliedResourceVolume",
                lastAppliedResourceVolume.ToString("R", CultureInfo.InvariantCulture), true);
            // When the scaler has started, serialize its immutable source
            // values. If OnSave runs before initialization, retain any loaded
            // snapshots rather than replacing them with already-scaled data.
            if (exponentScaler != null)
            {
                node.RemoveNodes(EXPONENT_BASE_NODE);
                exponentScaler.SaveBaselines(node, EXPONENT_BASE_NODE);
            }
            else if (savedExponentBaselines.Count > 0)
            {
                // Preserve loaded baselines if a disabled/not-yet-started part
                // is saved without running the exponent binder.
                node.RemoveNodes(EXPONENT_BASE_NODE);
                foreach (ConfigNode saved in savedExponentBaselines.Values)
                    node.AddNode(saved);
            }
            node.RemoveNodes(B9_VOLUME_NODE);
            foreach (KeyValuePair<string, double> entry in b9BaseVolumes)
            {
                ConfigNode record = node.AddNode(B9_VOLUME_NODE);
                record.AddValue("key", entry.Key);
                record.AddValue("nativeBaseVolume",
                    entry.Value.ToString("R", CultureInfo.InvariantCulture));
            }
            node.RemoveNodes(B9_NODE_BASELINE);
            foreach (KeyValuePair<string, B9NodeData> entry in b9NodeBaselines)
            {
                string signature, id;
                if (!B9PartSwitchSupport.SplitResourceKey(entry.Key, out signature, out id))
                    continue;
                ConfigNode record = node.AddNode(B9_NODE_BASELINE);
                record.AddValue("signature", signature);
                record.AddValue("id", id);
                SaveB9Vector(record, "position", entry.Value.position);
                SaveB9Vector(record, "original", entry.Value.original);
                SaveB9Vector(record, "orientation", entry.Value.orientation);
                record.AddValue("size", entry.Value.size.ToString(CultureInfo.InvariantCulture));
            }
            node.RemoveNodes(B9_RESOURCE_NODE);
            foreach (KeyValuePair<string, double> entry in b9ResourceBaselines)
            {
                string signature, name;
                if (!B9PartSwitchSupport.SplitResourceKey(entry.Key, out signature, out name))
                    continue;
                ConfigNode record = node.AddNode(B9_RESOURCE_NODE);
                record.AddValue("signature", signature);
                record.AddValue("name", name);
                record.AddValue("nativeCapacity", entry.Value.ToString("R", CultureInfo.InvariantCulture));
            }
            node.RemoveNodes(FUEL_SWITCH_RESOURCE_NODE);
            foreach (KeyValuePair<string, double> entry in fuelSwitchResourceBases)
            {
                string signature, name;
                if (!FuelSwitchSupport.SplitKey(entry.Key, out signature, out name)) continue;
                ConfigNode record = node.AddNode(FUEL_SWITCH_RESOURCE_NODE);
                record.AddValue("signature", signature);
                record.AddValue("name", name);
                record.AddValue("nativeCapacity", entry.Value.ToString("R", CultureInfo.InvariantCulture));
            }
            node.RemoveNodes(RESOURCE_NODE);
            foreach (ResourceScaleData data in liveResourceScale.Values)
            {
                if (data.resource == null || !Valid(data.nativeCapacity)) continue;
                ConfigNode entry = node.AddNode(RESOURCE_NODE);
                entry.AddValue("name", data.name);
                entry.AddValue("nativeCapacity",
                    data.nativeCapacity.ToString("R", CultureInfo.InvariantCulture));
            }
        }

        static void OnAnyEditorShipModified(ShipConstruct ship)
        {
            if (dispatchingShipNotification) return;
            // Known resource switchers only. Stock tanks get a direct
            // OnPartResourceChanged callback; optionally include all tanks
            // using checkAllResourcesPeriodically in Settings.cfg.
            foreach (ModuleStretchScale module in dynamicEditorModules)
                if (module != null && module.initialized)
                    module.resourceCheckPending = true;
        }

        // Called once on editor part startup, and retried in Update only if
        // KSP has not initialized this event yet. Multiple parts share one
        // registered callback; nobody subscribes per-part.
        static void EnsureEditorListener()
        {
            if (editorShipListenerRegistered || editorModules.Count == 0 ||
                !HighLogic.LoadedSceneIsEditor || GameEvents.onEditorShipModified == null)
                return;

            GameEvents.onEditorShipModified.Add(editorShipModifiedListener.OnShipModified);
            editorShipListenerRegistered = true;
        }

        void RegisterEditorListener()
        {
            editorModules.Add(this);
            if (hasDynamicResourceProvider || Settings.CheckAllResourcesPeriodically)
                dynamicEditorModules.Add(this);
            EnsureEditorListener();
        }

        // Tank switchers / other modules may send this part-local event when
        // their RESOURCE nodes change. Our own writes also send the event;
        // ignore those to avoid a write / refresh feedback loop.
        [KSPEvent(guiActive = false, guiActiveEditor = false, active = true)]
        public void OnPartResourceChanged(BaseEventDetails details)
        {
            if (!writingResources && initialized)
                resourceCheckPending = true;
        }

        static bool Valid(double n)
        {
            return n > 0d && !double.IsNaN(n) && !double.IsInfinity(n);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            model = part.transform.Find("model");
            Part prefab = part.partInfo != null ? part.partInfo.partPrefab : null;
            if (model == null || prefab == null)
            {
                Debug.LogWarning("[PartSculptor] No 'model' transform or prefab on " + part.name + "; disabling.");
                HideFields();
                return;
            }

            Transform prefabModel = prefab.transform.Find("model");
            prefabModelScale = prefabModel != null ? prefabModel.localScale : Vector3.one;

            prefabNodePos.Clear();
            prefabNodeOrigPos.Clear();
            prefabNodeOrientation.Clear();
            prefabNodeSize.Clear();
            foreach (AttachNode n in prefab.attachNodes)
            {
                if (n == null || string.IsNullOrEmpty(n.id) || prefabNodePos.ContainsKey(n.id)) continue;
                prefabNodePos[n.id] = n.position;
                prefabNodeOrigPos[n.id] = n.originalPosition;
                prefabNodeOrientation[n.id] = n.orientation;
                prefabNodeSize[n.id] = n.size;
            }
            hasSrfNode = prefab.srfAttachNode != null && part.srfAttachNode != null;
            if (hasSrfNode)
            {
                prefabSrfPos = prefab.srfAttachNode.originalPosition;
                prefabSrfOrientation = prefab.srfAttachNode.orientation;
                prefabSrfSize = prefab.srfAttachNode.size;
            }

            Settings.Load();
            ScalePresetRegistry.EnsureLoaded();
            if (ScaleDebug.Enabled)
                ScaleDebug.Info(part, "load", "axes=" + ScaleDebug.Format(Target) +
                    "; saved resource baselines=" + savedResourceBases.Count +
                    "; saved exponent baselines=" + savedExponentBaselines.Count +
                    "; last resource volume=" + ScaleDebug.Format(lastAppliedResourceVolume));
            if (!globalInit)
            {
                globalInit = true;
                ScaleChildrenGlobal = Settings.ScaleChildrenDefault;
                MatchNodeSizesGlobal = Settings.MatchNodeSizesDefault;
            }
            scaleChildren = ScaleChildrenGlobal;
            matchNodeSizes = MatchNodeSizesGlobal;
            if (!hasSavedLockAxes)
                lockAxes = Settings.LockProportionsDefault;
            if (!hasSavedLockMorphXZ)
                lockMorphXZ = Settings.LockMorphXZDefault;
            // Capturing the initial parent prevents changing any previously
            // saved craft just because it was loaded into an editor scene.
            nodeAttachWatcher.Seed(part.parent);
            foreach (string f in new[] { "scaleX", "scaleY", "scaleZ" })
            {
                UI_FloatEdit c = Fields[f].uiControlEditor as UI_FloatEdit;
                if (c == null) continue;
                c.minValue = Settings.MinScale;
                c.maxValue = Settings.MaxScale;
                c.incrementLarge = Settings.IncrementLarge;
                c.incrementSmall = Settings.IncrementSmall;
                c.incrementSlide = Settings.IncrementSlide;
            }
            scaleX = Clamp(scaleX); scaleY = Clamp(scaleY); scaleZ = Clamp(scaleZ);
            lastX = scaleX; lastY = scaleY; lastZ = scaleZ;
            lastLoggedLockAxes = lockAxes;

            prefabResourceCapacity.Clear();
            liveResourceScale.Clear();
            resourcesInitialized = false;
            resourceCheckPending = true;
            resourceDisplayDirty = false;
            hasDynamicResourceProvider = false;
            foreach (PartResource resource in prefab.Resources)
            {
                if (resource != null && Valid(resource.maxAmount) &&
                    !string.IsNullOrEmpty(resource.resourceName))
                    prefabResourceCapacity[resource.resourceName] = resource.maxAmount;
            }
            foreach (PartModule module in part.Modules)
            {
                if (module == this) continue;
                string typeName = module.GetType().Name;
                if (typeName.IndexOf("Switch", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    typeName.IndexOf("FuelTank", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    typeName.IndexOf("TankManager", StringComparison.OrdinalIgnoreCase) >= 0)
                    hasDynamicResourceProvider = true;
            }
            // B9 is optional: locate ModuleB9PartSwitch by its PartModule name
            // and use KSP's modifier interfaces. No B9 binary reference.
            b9Support = B9PartSwitchSupport.Create(part, b9BaseVolumes,
                StretchVolumeFactor);
            b9Signature = b9Support != null ? b9Support.Signature : null;
            if (b9Support != null)
                ScaleDebug.Info(part, "B9", "detected B9PartSwitch; selected=" +
                    b9Support.SelectedSubtypes + "; saved resource variants=" +
                    b9ResourceBaselines.Count + "; saved node variants=" +
                    b9NodeBaselines.Count);
            b9SubtypeChangePending = false;
            fuelSwitchSupport = FuelSwitchSupport.Create(part);
            fuelSwitchSignature = fuelSwitchSupport != null ? fuelSwitchSupport.Signature : null;
            fuelSwitchChangePending = false;
            fuelSwitchResourceRebasePending = false;
            if (fuelSwitchSupport != null)
                ScaleDebug.Info(part, "fuel switch", "detected " + fuelSwitchSupport.Description +
                    "; saved variant capacities=" + fuelSwitchResourceBases.Count);
            // Stagger compatibility passes for many tanks in the same craft.
            nextResourceCheckTime = Time.realtimeSinceStartup +
                Settings.ResourceFallbackSeconds + (part.GetInstanceID() & 31) * 0.05f;
            nextGeometryCheckFrame = Time.frameCount + 12 + (part.GetInstanceID() & 7);
            restrictedReason = FindRestriction();

            FindTweakScale();

            if (HighLogic.LoadedSceneIsEditor)
            {
                Fields["scaleX"].uiControlEditor.onFieldChanged = OnAxisChanged;
                Fields["scaleY"].uiControlEditor.onFieldChanged = OnAxisChanged;
                Fields["scaleZ"].uiControlEditor.onFieldChanged = OnAxisChanged;
                Fields["scaleX"].uiControlEditor.onSymmetryFieldChanged = OnSymAxisChanged;
                Fields["scaleY"].uiControlEditor.onSymmetryFieldChanged = OnSymAxisChanged;
                Fields["scaleZ"].uiControlEditor.onSymmetryFieldChanged = OnSymAxisChanged;
                Fields["scaleChildren"].OnValueModified += OnScaleChildrenModified;
                Fields["matchNodeSizes"].OnValueModified += OnMatchNodeSizesModified;
                Fields["scalePreset"].uiControlEditor.onFieldChanged = OnPresetChanged;
                RegisterEditorListener();
                RefreshPresetOptions();
                if (!string.IsNullOrEmpty(restrictedReason)) HideRestrictedControls();
            }

        }

        // Runs after every module's OnStart (incl. TweakScale), so our scale wins.
        public override void OnStartFinished(StartState state)
        {
            base.OnStartFinished(state);
            if (model == null || prefabNodePos == null) return;
            lastBase = BaseFactor;
            // Bind AFTER all stock/third-party modules have completed OnStart.
            // The current runtime values provide the native baselines; prefab
            // values help distinguish previously saved scaling on craft reload.
            exponentScaler = new PartExponentScaler(part, this,
                (float)Math.Pow(StretchVolumeFactor, 1d / 3d), BaseFactor);
            if (exponentScaler.HasCrewRule)
                crewCapacityScaling = new CrewCapacityScaling(part, exponentScaler.CrewExponent);
            ScaleDebug.Info(part, "startup", "axes=" + ScaleDebug.Format(Target) +
                "; prefab mass=" + ScaleDebug.Format(NativeDryMass(part.mass)) +
                "; prefab dry cost=" + ScaleDebug.Format(BaseDryCost(0f)) +
                "; TweakScale factor=" + ScaleDebug.Format(BaseFactor) +
                "; dynamic resources=" + hasDynamicResourceProvider +
                "; mass exponent=" + ScaleDebug.Format(exponentScaler.MassExponent) +
                "; cost exponent=" + ScaleDebug.Format(exponentScaler.CostExponent));
            // B9 may already have applied a subtype's attach-node offsets in
            // OnStart. Preserve those as the geometry baselines, not the
            // unrelated part-prefab nodes, whenever they differ.
            if (b9Support != null)
                RefreshB9NodeBaselines(true);
            // Capture stock and optional Waterfall effect trees after every
            // PartModule has initialized. Effects under the model are scaled
            // automatically; off-model effect roots get absolute-scale wrappers.
            visualEffects = new EngineEffectScaling(part, model);
            Part prefab = part.partInfo != null ? part.partInfo.partPrefab : null;
            dockingPorts = DockingPortScaling.Create(part, prefab);
            // Initial application never moves neighbours: the craft file / save /
            // clone already holds the correct part positions.
            // Loaded crafts retain their saved dimensions, even if new limits
            // or restrictions have been enabled since they were saved.
            Apply(false, false);
            // Restore saved face shape after ordinary PartSculptor has rebuilt
            // its native geometry and attachment nodes. In flight as well as
            // the editor, the launched part must retain the same collision mesh.
            if (HasFaceMorph)
            {
                if (EnsureFaceMorph())
                    faceMorph.Apply(GetMorphFace(0), GetMorphFace(1), false);
                else
                    Debug.LogWarning("[PartSculptor] Cannot restore saved face morph on " +
                        part.name + ": " + morphFailure);
            }
            lastAcceptedScale = Target;
            initialized = true;
            UpdateCalculatedValues();
            // KSP may reconstruct craft stack joins from prefab node positions
            // before this module restores scaled attachment nodes. Once the
            // complete editor craft is initialized, align each stack join once.
            if ((HasStretchedGeometry || HasFaceMorph) && HighLogic.LoadedSceneIsEditor)
                AttachmentLoadFix.Schedule();
        }

        void HideFields()
        {
            foreach (BaseField f in Fields) f.guiActiveEditor = false;
            foreach (BaseEvent e in Events) e.guiActiveEditor = false;
        }

        // A restriction blocks NEW editor resizes; it never silently alters
        // already-stretched crafts during loading or flight.
        string FindRestriction()
        {
            if (part == null || part.Modules == null) return null;
            foreach (PartModule module in part.Modules)
            {
                if (module == null || module == this) continue;
                string name = module.moduleName ?? module.GetType().Name;
                if (Settings.RestrictWheels &&
                    (name.StartsWith("ModuleWheel", StringComparison.OrdinalIgnoreCase) ||
                     name.StartsWith("KSPWheel", StringComparison.OrdinalIgnoreCase)))
                    return "Wheel module: " + name;
                if (Settings.RestrictDockingPorts &&
                    name.IndexOf("DockingNode", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Docking port: " + name;
                if (Settings.RestrictAnimatedParts &&
                    (name.StartsWith("ModuleAnimate", StringComparison.OrdinalIgnoreCase) ||
                     name.StartsWith("ModuleAnimation", StringComparison.OrdinalIgnoreCase) ||
                     name.StartsWith("ModuleDeployable", StringComparison.OrdinalIgnoreCase)))
                    return "Animation: " + name;
            }
            return null;
        }

        void HideRestrictedControls()
        {
            if (Events["ToggleScalingGizmo"] != null)
                Events["ToggleScalingGizmo"].guiActiveEditor = false;
            foreach (string name in new[] { "scaleX", "scaleY", "scaleZ", "lockAxes", "scalePreset" })
                Fields[name].guiActiveEditor = false;
            foreach (string name in new[] { "ShowPresets", "RepeatPreset", "ResetScale", "SwapXZ" })
            {
                BaseEvent action = Events[name];
                if (action != null) action.guiActiveEditor = false;
            }
            Debug.LogWarning("[PartSculptor] Editing disabled on " + part.name + ": " + restrictedReason);
        }

        static float Clamp(float v)
        {
            if (float.IsNaN(v) || v <= 0f) return 1f;
            return Mathf.Clamp(v, Settings.MinScale, Settings.MaxScale);
        }

        // ------------------------------------------------------------------ UI

        void OnAxisChanged(BaseField field, object oldValue)
        {
            float uiValue = (float)field.GetValue(this);
            ScaleDebug.Info(part, "ui", "axis edit " + field.name +
                " requested=" + ScaleDebug.Format(uiValue) +
                "; old=" + ScaleDebug.Format(oldValue) +
                "; lock proportions=" + lockAxes);
            Vector3 before = new Vector3(lastX, lastY, lastZ);
            if (lockAxes)
            {
                float oldV, newV;
                switch (field.name)
                {
                    case "scaleX": oldV = lastX; newV = scaleX; break;
                    case "scaleY": oldV = lastY; newV = scaleY; break;
                    default: oldV = lastZ; newV = scaleZ; break;
                }
                if (oldV > 0f && !Mathf.Approximately(oldV, newV))
                {
                    float k = newV / oldV;
                    // Don't let the other axes leave the allowed range.
                    float maxOther = Mathf.Max(field.name == "scaleX" ? 0f : lastX,
                                               field.name == "scaleY" ? 0f : lastY,
                                               field.name == "scaleZ" ? 0f : lastZ);
                    float minOther = Mathf.Min(field.name == "scaleX" ? Settings.MaxScale : lastX,
                                               field.name == "scaleY" ? Settings.MaxScale : lastY,
                                               field.name == "scaleZ" ? Settings.MaxScale : lastZ);
                    k = Mathf.Clamp(k, Settings.MinScale / minOther, Settings.MaxScale / maxOther);
                    scaleX = Clamp(lastX * k);
                    scaleY = Clamp(lastY * k);
                    scaleZ = Clamp(lastZ * k);
                }
            }
            lastX = scaleX; lastY = scaleY; lastZ = scaleZ;
            bool altered = !Mathf.Approximately((float)field.GetValue(this), uiValue);
            CommitChange(before, altered ? null : field.name);
        }

        void OnSymAxisChanged(BaseField field, object oldValue)
        {
            ScaleDebug.Change(part, "symmetry", field.name, oldValue,
                field.GetValue(this), "symmetry counterpart");
            // Values are pushed explicitly by the originating part; geometry is
            // applied in Update().
            lastX = scaleX; lastY = scaleY; lastZ = scaleZ;
        }

        // KSP's float-edit PAW widget never re-reads its field, so after changing values
        // from code we have to redraw it ourselves (UpdateDisplay is private).
        static MethodInfo updateDisplay;
        static bool updateDisplayLooked;

        void RefreshDisplays(string skip = null)
        {
            if (!HighLogic.LoadedSceneIsEditor) return;
            if (!updateDisplayLooked)
            {
                updateDisplayLooked = true;
                updateDisplay = typeof(UIPartActionFloatEdit).GetMethod("UpdateDisplay",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }
            bool ok = updateDisplay != null;
            if (ok)
            {
                try
                {
                    if (skip != "scaleX") RefreshField("scaleX", scaleX);
                    if (skip != "scaleY") RefreshField("scaleY", scaleY);
                    if (skip != "scaleZ") RefreshField("scaleZ", scaleZ);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[PartSculptor] PAW refresh failed, rebuilding window: " + e.Message);
                    ok = false;
                }
            }
            if (!ok && part.PartActionWindow != null)
                MonoUtilities.RefreshPartContextWindow(part);
        }

        void RefreshField(string name, float value)
        {
            UI_Control c = Fields[name].uiControlEditor;
            UIPartActionFloatEdit item = c != null ? c.partActionItem as UIPartActionFloatEdit : null;
            if (item != null && item.gameObject.activeInHierarchy)
                updateDisplay.Invoke(item, new object[] { value, null });
        }

        // A user-driven change: update this part and its symmetry counterparts, and
        // if "Scale children" is on, stretch every descendant by the same ratio.
        void CommitChange(Vector3 before, string skipLocal)
        {
            ScaleDebug.Change(part, "axes", "requested", before, Target,
                "proportions locked=" + lockAxes + "; children=" + scaleChildren);
            if (!ValidateScaleChange(before, skipLocal))
            {
                ScaleDebug.Info(part, "axes", "edit rejected; reason=" + limitWarning);
                RefreshDisplays();
                UpdateCalculatedValues();
                return;
            }
            ScaleDebug.Change(part, "axes", "accepted", before, Target,
                string.IsNullOrEmpty(limitWarning) ? "unclamped" : limitWarning);
            // Every direct scale change (PAW edit, gizmo, node matcher, etc.)
            // invalidates the displayed absolute preset. Applying a named
            // absolute preset or Reset scale overrides this AFTER CommitChange.
            if ((Target - before).sqrMagnitude > 1e-10f)
                SetPresetDisplay("Custom");
            Vector3 ratio = new Vector3(Div(scaleX, before.x), Div(scaleY, before.y), Div(scaleZ, before.z));
            // If the requested field was clamped by a safety limit, refresh
            // that float-edit widget instead of leaving its input text stale.
            PushToSymmetry(string.IsNullOrEmpty(limitWarning) ? skipLocal : null);

            var roots = new List<ModuleStretchScale> { this };
            foreach (Part p in part.symmetryCounterparts)
            {
                ModuleStretchScale m = p != null ? p.FindModuleImplementing<ModuleStretchScale>() : null;
                if (m != null && m.initialized && m.model != null) roots.Add(m);
            }
            foreach (ModuleStretchScale m in roots) m.Apply(true, true, false);

            if (scaleChildren && (ratio - Vector3.one).sqrMagnitude >= 1e-12f)
                foreach (ModuleStretchScale m in roots)
                    ScaleDescendants(m.part, m.part.transform, ratio);

            // One notification per user action, even when dozens of descendants
            // and symmetry counterparts are updated.
            FireEditorShipModified();
        }

        // The whole subtree is stretched by 'ratio' measured along the axes of 'frame'
        // (the part the user edited). Each child gets that stretch expressed along its
        // own axes, so rotated children are handled too. Parents are applied before
        // their children, so every part re-seats itself on an already-final parent.
        static void ScaleDescendants(Part p, Transform frame, Vector3 ratio)
        {
            foreach (Part child in p.children)
            {
                if (child == null) continue;
                ModuleStretchScale m = child.FindModuleImplementing<ModuleStretchScale>();
                if (m == null || !m.initialized || m.model == null ||
                    !string.IsNullOrEmpty(m.restrictedReason)) continue; // stop at unscalable parts
                // If a safety limit clipped the child's requested scale,
                // stop at that child rather than stretching its descendants
                // by a larger factor than their immediate parent received.
                if (m.ScaleBy(frame, ratio))
                    ScaleDescendants(child, frame, ratio);
            }
        }

        bool ScaleBy(Transform frame, Vector3 ratio)
        {
            Transform t = part.transform;
            Vector3 before = Target;
            scaleX = Clamp(scaleX * AxisFactor(frame, t.right, ratio));
            scaleY = Clamp(scaleY * AxisFactor(frame, t.up, ratio));
            scaleZ = Clamp(scaleZ * AxisFactor(frame, t.forward, ratio));
            Vector3 requested = Target;
            if (!ValidateScaleChange(before, null))
            {
                RefreshDisplays();
                return false;
            }
            bool fullyApplied = (Target - requested).sqrMagnitude < 1e-10f;
            lastX = scaleX; lastY = scaleY; lastZ = scaleZ;
            if ((Target - before).sqrMagnitude > 1e-10f)
                SetPresetDisplay("Custom");
            Apply(true, true, false);
            RefreshDisplays();
            return fullyApplied;
        }

        static float AxisFactor(Transform frame, Vector3 worldAxis, Vector3 ratio)
        {
            Vector3 d = frame.InverseTransformDirection(worldAxis);
            return Vector3.Scale(d, ratio).magnitude;
        }

        void PushToSymmetry(string skipLocal = null)
        {
            foreach (Part p in part.symmetryCounterparts)
            {
                if (p == null) continue;
                ModuleStretchScale m = p.FindModuleImplementing<ModuleStretchScale>();
                if (m == null) continue;
                m.scaleX = scaleX; m.scaleY = scaleY; m.scaleZ = scaleZ;
                m.lastX = scaleX; m.lastY = scaleY; m.lastZ = scaleZ;
                m.RefreshDisplays();
            }
            RefreshDisplays(skipLocal);
        }

        static void FireEditorShipModified()
        {
            if (!HighLogic.LoadedSceneIsEditor || EditorLogic.fetch == null ||
                EditorLogic.fetch.ship == null) return;
            if (!shipNotificationPending)
                shipNotificationQueuedFrame = Time.frameCount;
            shipNotificationPending = true;
        }

        // Called by each live module, but only the FIRST caller drains the
        // global pending notification. Other listeners are suppressed during
        // that send, preventing a refresh cascade across a large craft.
        public void LateUpdate()
        {
            if (!shipNotificationPending || dispatchingShipNotification ||
                !HighLogic.LoadedSceneIsEditor ||
                Time.frameCount < shipNotificationQueuedFrame) return;
            shipNotificationPending = false;
            if (EditorLogic.fetch == null || EditorLogic.fetch.ship == null ||
                GameEvents.onEditorShipModified == null) return;
            try
            {
                dispatchingShipNotification = true;
                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
            }
            finally { dispatchingShipNotification = false; }
        }

        // Rebuild PAW quick choices after a custom absolute preset is edited.
        // Relative presets are available only in the full Presets window.
        internal static void RefreshPresetOptionsAll()
        {
            foreach (ModuleStretchScale module in editorModules)
                if (module != null) module.RefreshPresetOptions();
        }

        // Display-state only: Custom is not a preset and does not modify the
        // last-applied preset used by Repeat preset. Keep symmetry PAWs in sync.
        void SetPresetDisplay(string name)
        {
            if (part == null) return;
            scalePreset = name;
            if (part.PartActionWindow != null)
                part.PartActionWindow.displayDirty = true;
            foreach (Part counterpart in part.symmetryCounterparts)
            {
                ModuleStretchScale other = counterpart != null ?
                    counterpart.FindModuleImplementing<ModuleStretchScale>() : null;
                if (other == null) continue;
                other.scalePreset = name;
                if (counterpart.PartActionWindow != null)
                    counterpart.PartActionWindow.displayDirty = true;
            }
        }

        void RefreshPresetOptions()
        {
            if (!HighLogic.LoadedSceneIsEditor || Fields == null) return;
            BaseField f = Fields["scalePreset"];
            UI_ChooseOption choice = f != null ? f.uiControlEditor as UI_ChooseOption : null;
            if (choice == null) return;
            string[] options = ScalePresetRegistry.AbsoluteOptionNames();
            choice.options = options;
            if (Array.IndexOf(options, scalePreset) < 0)
                scalePreset = Array.IndexOf(options, "1x uniform") >= 0
                    ? "1x uniform" : options[0];
            if (part != null && part.PartActionWindow != null)
                part.PartActionWindow.displayDirty = true;
        }

        void OnPresetChanged(BaseField field, object oldValue)
        {
            ScalePresetDefinition selected = ScalePresetRegistry.Find(scalePreset);
            if (selected != null && !selected.Relative)
                ApplyPreset(scalePreset);
        }

        // Full browser (including relative presets), separate from the
        // absolute-only PAW selector.
        [KSPEvent(guiActiveEditor = true, guiName = "Browse presets",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        public void ShowPresets()
        {
            if (!initialized || !HighLogic.LoadedSceneIsEditor ||
                !string.IsNullOrEmpty(restrictedReason)) return;
            ScalePresetsWindow.Toggle(this);
        }

        private string morphFailure = "";
        internal string FaceMorphFailure { get { return morphFailure; } }
        internal FaceMorphSupport FaceMorph { get { return faceMorph; } }
        internal bool HasFaceMorph
        {
            get { return !FaceMorphMath.IsIdentity(GetMorphFace(0)) ||
                !FaceMorphMath.IsIdentity(GetMorphFace(1)); }
        }

        internal MorphFace GetMorphFace(int face)
        {
            return face == 0 ?
                new MorphFace(morphBottomX, morphBottomZ, morphBottomTiltX,
                    morphBottomTiltZ, morphBottomOffsetX, morphBottomOffsetZ) :
                new MorphFace(morphTopX, morphTopZ, morphTopTiltX,
                    morphTopTiltZ, morphTopOffsetX, morphTopOffsetZ);
        }

        internal bool EnsureFaceMorph()
        {
            if (faceMorph != null) return faceMorph.Valid;
            if (part == null || model == null)
            {
                morphFailure = "Part is missing a model transform";
                return false;
            }
            if (part.Modules != null)
                foreach (PartModule other in part.Modules)
                {
                    if (other == null || other == this) continue;
                    string name = other.moduleName ?? other.GetType().Name;
                    if (B9FaceMorphCompatibility.IsB9(other))
                    {
                        string reason;
                        if (!B9FaceMorphCompatibility.AllowsFaceMorph(part, other, out reason))
                        {
                            morphFailure = "B9 geometry switch not supported: " + reason;
                            return false;
                        }
                        // Resource-only B9 fuel switching is safe; its resource
                        // volumes remain managed by B9PartSwitchSupport.
                        continue;
                    }
                    if (name.IndexOf("Animate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Robotic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("B9DisableTransform", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        morphFailure = "Animated / switchable geometry unsupported: " + name;
                        return false;
                    }
                }
            faceMorph = new FaceMorphSupport(part, model);
            if (faceMorph.Valid)
            {
                morphFailure = "";
                return true;
            }
            morphFailure = faceMorph.Error;
            faceMorph.Dispose();
            faceMorph = null;
            return false;
        }

        // Editor manipulation only; never mutate flight parts. One source
        // shape is shared by all symmetry counterparts, without sharing meshes.
        internal void SetMorphFaces(MorphFace bottom, MorphFace top)
        {
            if (!CanUseScaleGizmo) return;
            bottom = FaceMorphMath.Clamp(bottom);
            top = FaceMorphMath.Clamp(top);
            // Morphing changes real volume and effective face dimensions.
            // Enforce the same building/volume restrictions as ordinary axes.
            CareerScaleLimit limits;
            bool career = TryGetCareerLimits(out limits);
            double desiredVolume = (double)scaleX * scaleY * scaleZ *
                FaceMorphMath.VolumeFactor(bottom, top);
            double ceiling = Settings.MaxVolumeMultiplier /
                Math.Max(1e-8d, Math.Pow(BaseFactor, 3d));
            if (career) ceiling = Math.Min(ceiling, limits.maxVolume);
            // A grandfathered craft may reduce a previously oversized morph.
            if (desiredVolume > ceiling * (1d + 1e-6d) &&
                desiredVolume > StretchVolumeFactor * (1d + 1e-6d)) return;
            if (career)
            {
                MorphFace[] faces = { bottom, top };
                foreach (MorphFace face in faces)
                {
                    double x = scaleX * face.Width;
                    double z = scaleZ * face.Depth;
                    if ((x > limits.maxAxis && x > scaleX * GetMorphFace(0).Width) ||
                        (z > limits.maxAxis && z > scaleZ * GetMorphFace(0).Depth))
                        return;
                }
            }
            if (!EnsureFaceMorph()) return;
            var targets = new List<ModuleStretchScale> { this };
            foreach (Part counterpart in part.symmetryCounterparts)
            {
                ModuleStretchScale m = counterpart != null ?
                    counterpart.FindModuleImplementing<ModuleStretchScale>() : null;
                if (m == null || !m.EnsureFaceMorph()) return; // all-or-nothing
                targets.Add(m);
            }
            foreach (ModuleStretchScale m in targets)
            {
                m.morphBottomX = bottom.Width;
                m.morphBottomZ = bottom.Depth;
                m.morphBottomTiltX = bottom.TiltX;
                m.morphBottomTiltZ = bottom.TiltZ;
                m.morphBottomOffsetX = bottom.OffsetX;
                m.morphBottomOffsetZ = bottom.OffsetZ;
                m.morphTopX = top.Width;
                m.morphTopZ = top.Depth;
                m.morphTopTiltX = top.TiltX;
                m.morphTopTiltZ = top.TiltZ;
                m.morphTopOffsetX = top.OffsetX;
                m.morphTopOffsetZ = top.OffsetZ;
                m.faceMorph.Apply(bottom, top, true);
                m.SetPresetDisplay("Custom");
                if (m.b9Support != null)
                    m.b9Support.ApplyBaseVolumes(m.part, m.StretchVolumeFactor, true);
                m.ApplyResourceScaling(false);
                if (m.exponentScaler != null)
                    m.exponentScaler.Apply(m.scaleX, m.scaleY, m.scaleZ);
                m.UpdateCalculatedValues();
            }
            FireEditorShipModified();
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Show face morph handles",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        public void ToggleFaceMorphGizmo()
        {
            if (FaceMorphGizmo.IsFor(this)) FaceMorphGizmo.Hide();
            else if (CanUseScaleGizmo)
            {
                if (EnsureFaceMorph()) FaceMorphGizmo.Show(this);
                else
                {
                    Debug.LogWarning("[PartSculptor] Face morph unavailable: " + morphFailure);
                    ScreenMessages.PostScreenMessage("Face morph unavailable: " + morphFailure,
                        5f, ScreenMessageStyle.UPPER_CENTER);
                }
            }
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Reset face morph",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        public void ResetFaceMorph()
        {
            SetMorphFaces(new MorphFace(1f, 1f, 0f, 0f),
                new MorphFace(1f, 1f, 0f, 0f));
        }

        // A single editor gizmo is active at a time. PAW and gizmo both call
        // CommitChange, preserving the same safety, symmetry and child scaling.
        internal bool CanUseScaleGizmo
        {
            get { return initialized && part != null && model != null &&
                string.IsNullOrEmpty(restrictedReason) && HighLogic.LoadedSceneIsEditor; }
        }

        // A selected part can have PartSculptor installed but still be unable
        // to open the morph gizmo. Show a useful reason instead of silently
        // hiding its handles in that case.
        internal string MorphSelectionFailure
        {
            get
            {
                if (part == null) return "Part is unavailable";
                if (!HighLogic.LoadedSceneIsEditor) return "Editor mode is required";
                if (!initialized) return "PartSculptor has not initialized on this part";
                if (model == null) return "Part has no model transform";
                if (!string.IsNullOrEmpty(restrictedReason)) return restrictedReason;
                return "Morph gizmo is unavailable";
            }
        }

        internal Vector3 GizmoScale { get { return Target; } }

        internal void SetGizmoAxis(int axis, float value)
        {
            if (!CanUseScaleGizmo || axis < 0 || axis > 2) return;
            Vector3 before = Target;
            ScaleMath.Axes target = ScaleMath.GizmoAxes(before.x, before.y,
                before.z, axis, value, lockAxes, Settings.MinScale, Settings.MaxScale);
            SetGizmoScale(new Vector3((float)target.X, (float)target.Y, (float)target.Z));
        }

        internal void SetGizmoScale(Vector3 target)
        {
            if (!CanUseScaleGizmo) return;
            Vector3 before = Target;
            scaleX = Clamp(target.x);
            scaleY = Clamp(target.y);
            scaleZ = Clamp(target.z);
            if ((Target - before).sqrMagnitude < 1e-10f) return;
            CommitChange(before, null);
        }

        internal void UpdateGizmoPAWLabel()
        {
            if (Events == null) return;
            BaseEvent action = Events["ToggleScalingGizmo"];
            if (action == null) return;
            action.guiName = ScaleGizmo.IsFor(this) ? "Hide scaling gizmo" : "Show scaling gizmo";
            if (part != null && part.PartActionWindow != null)
                part.PartActionWindow.displayDirty = true;
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Show scaling gizmo",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        public void ToggleScalingGizmo()
        {
            FaceMorphGizmo.Hide();
            ScaleGizmo.Toggle(this);
        }

        // Editor PAW button. The report reads live property values and immutable
        // exponent baselines; DEBUG logging does not need to be enabled.
        [KSPEvent(guiActiveEditor = true, guiName = "Show scaled values",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        public void ShowScaledValues()
        {
            if (!initialized || !HighLogic.LoadedSceneIsEditor) return;
            scaledValuesWindow.Toggle(part);
        }

        // Unity IMGUI: each PartModule draws only its own window when opened.
        void OnGUI()
        {
            if (!HighLogic.LoadedSceneIsEditor)
            {
                scaledValuesWindow.Close();
                return;
            }
            if (initialized && part != null)
                scaledValuesWindow.Draw(this);
            ScalePresetEditor.DrawFor(this);
            ScalePresetsWindow.DrawFor(this);
        }

        void OnDisable()
        {
            ScaleGizmo.HideFor(this);
            FaceMorphGizmo.HideFor(this);
            // A scene change may disable the part before OnDestroy is called.
            scaledValuesWindow.Close();
            ScalePresetsWindow.CloseFor(this);
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Repeat preset",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        public void RepeatPreset()
        {
            // The last applied preset may be relative and must not appear in
            // the PAW's absolute-only choices.
            ApplyPreset(ScalePresetRegistry.Find(lastAppliedPreset, lastAppliedRelative));
        }

        // Shared by the Presets window and the custom preset editor.
        internal void ApplyNamedPresetFromEditor(string name)
        {
            ScalePresetDefinition selected = ScalePresetRegistry.Find(name);
            if (selected != null) ApplyNamedPresetFromEditor(name, selected.Relative);
        }

        internal void ApplyNamedPresetFromEditor(string name, bool relative)
        {
            if (!HighLogic.LoadedSceneIsEditor || !string.IsNullOrEmpty(restrictedReason)) return;
            ScalePresetDefinition selected = ScalePresetRegistry.Find(name, relative);
            if (selected == null) return;
            // Applying either mode updates the display after CommitChange.
            // A relative preset always displays Custom, even if a same-named
            // absolute preset is present in the quick selector.
            ApplyPreset(selected);
        }

        void ApplyPreset(string preset)
        {
            ApplyPreset(ScalePresetRegistry.Find(preset, false));
        }

        void ApplyPreset(ScalePresetDefinition definition)
        {
            if (definition == null || !ScalePresetMath.Valid(definition)) return;
            lastAppliedPreset = definition.Name;
            lastAppliedRelative = definition.Relative;
            ScaleDebug.Info(part, "ui", "preset requested: " + definition.Name +
                (definition.Relative ? " [relative]" : " [absolute]"));
            Vector3 before = Target;
            ScaleMath.Axes next = ScalePresetMath.Apply(definition,
                before.x, before.y, before.z);
            scaleX = Clamp((float)next.X);
            scaleY = Clamp((float)next.Y);
            scaleZ = Clamp((float)next.Z);
            lastX = scaleX; lastY = scaleY; lastZ = scaleZ;
            CommitChange(before, null);
            // Relative commands have no absolute scale identity. If a Career
            // or safety limit clamped an absolute preset, show Custom too.
            bool matches = !definition.Relative &&
                Mathf.Approximately(scaleX, (float)next.X) &&
                Mathf.Approximately(scaleY, (float)next.Y) &&
                Mathf.Approximately(scaleZ, (float)next.Z);
            SetPresetDisplay(matches ? definition.Name : "Custom");
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Reset scale", groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        public void ResetScale()
        {
            ScaleDebug.Info(part, "ui", "reset scale requested");
            Vector3 before = Target;
            scaleX = scaleY = scaleZ = 1f;
            lastX = lastY = lastZ = 1f;
            CommitChange(before, null);
            // Reset the last-used preset as well as the dimensions.
            scalePreset = "1x uniform";
            lastAppliedPreset = "1x uniform";
            lastAppliedRelative = false;
            RefreshPresetOptions();
            // Keep symmetry counterparts' repeat-preset command in sync.
            if (part != null)
            {
                foreach (Part counterpart in part.symmetryCounterparts)
                {
                    ModuleStretchScale module = counterpart != null ?
                        counterpart.FindModuleImplementing<ModuleStretchScale>() : null;
                    if (module == null) continue;
                    module.scalePreset = "1x uniform";
                    module.lastAppliedPreset = "1x uniform";
                    module.lastAppliedRelative = false;
                    module.RefreshPresetOptions();
                }
            }
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Swap X / Z", groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        public void SwapXZ()
        {
            ScaleDebug.Info(part, "ui", "swap X/Z requested");
            Vector3 before = Target;
            float t = scaleX; scaleX = scaleZ; scaleZ = t;
            lastX = scaleX; lastZ = scaleZ;
            CommitChange(before, null);
        }

        // Apply after B9 has finished its own subtype transition. This is an
        // explicit rebase of *externally replaced* values, not the automatic
        // per-click rebasing that previously caused runaway mass and thrust.
        void OnB9SubtypeChanged()
        {
            if (!b9SubtypeChangePending) return;
            b9SubtypeChangePending = false;
            b9ResourceRebasePending = true;
            if (exponentScaler != null)
                exponentScaler.RebaseExternallyChangedFields(scaleX, scaleY, scaleZ);
            RefreshB9NodeBaselines();
            resourceCheckPending = true;
            Apply(false, false, false);
            if (HighLogic.LoadedSceneIsEditor)
                FireEditorShipModified();
        }

        // Firespitter / IFS select numbered fuel setups; MFT changes the
        // definition and utilization. Wait one frame to allow the external
        // module to finish rebuilding resources before capturing new baselines.
        void OnFuelSwitchChanged()
        {
            if (!fuelSwitchChangePending) return;
            fuelSwitchChangePending = false;
            fuelSwitchResourceRebasePending = true;
            resourceCheckPending = true;
            ApplyResourceScaling(false);
            if (HighLogic.LoadedSceneIsEditor) FireEditorShipModified();
        }

        // Only record node baselines if B9 has actually moved/replaced the
        // node. Unchanged nodes retain the original prefab baseline.
        void RefreshB9NodeBaselines(bool onLoad = false)
        {
            if (part == null || b9Support == null) return;
            Vector3 total = Target * BaseFactor;
            bool alreadyStretched = (Target - Vector3.one).sqrMagnitude > 1e-8f;
            foreach (AttachNode node in part.attachNodes)
            {
                if (node == null || string.IsNullOrEmpty(node.id)) continue;
                Vector3 native, nativeOriginal;
                if (!prefabNodePos.TryGetValue(node.id, out native))
                {
                    native = Vector3.zero;
                    prefabNodePos[node.id] = native;
                    prefabNodeOrigPos[node.id] = native;
                    prefabNodeOrientation[node.id] = node.orientation;
                    prefabNodeSize[node.id] = node.size;
                }
                nativeOriginal = prefabNodeOrigPos[node.id];
                string key = B9NodeKey(b9Signature, node.id);
                B9NodeData recorded;
                if (onLoad && b9NodeBaselines.TryGetValue(key, out recorded))
                {
                    // Saved native coordinates always win on craft reload.
                    prefabNodePos[node.id] = recorded.position;
                    prefabNodeOrigPos[node.id] = recorded.original;
                    prefabNodeOrientation[node.id] = recorded.orientation;
                    prefabNodeSize[node.id] = recorded.size;
                    continue;
                }
                Vector3 expected = Vector3.Scale(native, total);
                Vector3 expectedOriginal = Vector3.Scale(nativeOriginal, total);
                int expectedSize = ScaledNodeSize(prefabNodeSize[node.id],
                    prefabNodeOrientation[node.id], total);
                bool b9Changed = (expected - node.position).sqrMagnitude > 1e-6f ||
                    (expectedOriginal - node.originalPosition).sqrMagnitude > 1e-6f ||
                    expectedSize != node.size;
                // A legacy scaled craft has no saved B9 baseline. Its current
                // nodes may already be stretched; never capture them as raw.
                if (onLoad && alreadyStretched) continue;
                if (b9Changed)
                {
                    prefabNodePos[node.id] = new Vector3(
                        Div(node.position.x, BaseFactor),
                        Div(node.position.y, BaseFactor),
                        Div(node.position.z, BaseFactor));
                    prefabNodeOrigPos[node.id] = new Vector3(
                        Div(node.originalPosition.x, BaseFactor),
                        Div(node.originalPosition.y, BaseFactor),
                        Div(node.originalPosition.z, BaseFactor));
                    prefabNodeOrientation[node.id] = node.orientation;
                    prefabNodeSize[node.id] = node.size;
                }
                b9NodeBaselines[key] = new B9NodeData {
                    position = prefabNodePos[node.id],
                    original = prefabNodeOrigPos[node.id],
                    orientation = prefabNodeOrientation[node.id],
                    size = prefabNodeSize[node.id]
                };
            }
            if (hasSrfNode && part.srfAttachNode != null)
            {
                AttachNode surface = part.srfAttachNode;
                string key = B9NodeKey(b9Signature, "$surface");
                B9NodeData recorded;
                if (onLoad && b9NodeBaselines.TryGetValue(key, out recorded))
                {
                    prefabSrfPos = recorded.position;
                    prefabSrfOrientation = recorded.orientation;
                    prefabSrfSize = recorded.size;
                    return;
                }
                if (onLoad && alreadyStretched) return;
                Vector3 expected = Vector3.Scale(prefabSrfPos, total);
                if ((expected - surface.position).sqrMagnitude > 1e-6f ||
                    ScaledNodeSize(prefabSrfSize, prefabSrfOrientation, total) != surface.size)
                {
                    Vector3 pos = surface.position;
                    prefabSrfPos = new Vector3(Div(pos.x, BaseFactor),
                        Div(pos.y, BaseFactor), Div(pos.z, BaseFactor));
                    prefabSrfOrientation = surface.orientation;
                    prefabSrfSize = surface.size;
                }
                b9NodeBaselines[key] = new B9NodeData {
                    position = prefabSrfPos, original = prefabSrfPos,
                    orientation = prefabSrfOrientation, size = prefabSrfSize
                };
            }
        }

        // Catches every other way the values can change (symmetry counterparts,
        // other mods, undo) and applies the geometry once.
        public void Update()
        {
            if (!initialized || model == null) return;
            ScalePresetsWindow.ProcessPendingFor(this);
            ScaleGizmo.UpdateFor(this);
            FaceMorphGizmo.UpdateFor(this);
            // Poll selected B9 subtype identities (including multiple B9
            // modules on one part). B9 can change resources, nodes, and other
            // module fields without emitting an OnPartResourceChanged event.
            if (b9Support != null)
            {
                string signature = b9Support.Signature;
                if (!string.Equals(signature, b9Signature, StringComparison.Ordinal))
                {
                    ScaleDebug.Change(part, "B9", "subtype", b9Signature, signature);
                    b9Signature = signature;
                    b9SubtypeChangePending = true;
                    b9SubtypeChangedFrame = Time.frameCount;
                    resourceCheckPending = true;
                }
                if (b9SubtypeChangePending && Time.frameCount > b9SubtypeChangedFrame)
                    OnB9SubtypeChanged();
            }
            if (fuelSwitchSupport != null)
            {
                string signature = fuelSwitchSupport.Signature;
                if (!string.Equals(signature, fuelSwitchSignature, StringComparison.Ordinal))
                {
                    ScaleDebug.Change(part, "fuel switch", "configuration",
                        fuelSwitchSignature, signature);
                    fuelSwitchSignature = signature;
                    fuelSwitchChangePending = true;
                    fuelSwitchChangedFrame = Time.frameCount;
                    resourceCheckPending = true;
                }
                if (fuelSwitchChangePending && Time.frameCount > fuelSwitchChangedFrame)
                    OnFuelSwitchChanged();
            }
            if (visualEffects != null) visualEffects.Update();
            if (!HighLogic.LoadedSceneIsEditor) return;
            AttachmentLoadFix.Poll();
            if (lockAxes != lastLoggedLockAxes)
            {
                ScaleDebug.Change(part, "ui", "Lock proportions", lastLoggedLockAxes, lockAxes);
                lastLoggedLockAxes = lockAxes;
            }

            // OnStart can precede KSP's editor-event initialization.
            // Retry only until the single shared listener is registered.
            if (!editorShipListenerRegistered) EnsureEditorListener();

            if (resourceDisplayDirty)
            {
                resourceDisplayDirty = false;
                if (part.PartActionWindow != null)
                    part.PartActionWindow.displayDirty = true;
            }

            if (matchNodeSizes != MatchNodeSizesGlobal)
            {
                matchNodeSizes = MatchNodeSizesGlobal;
                UI_Control matchControl = Fields["matchNodeSizes"].uiControlEditor;
                if (matchControl != null && matchControl.partActionItem != null)
                    matchControl.partActionItem.UpdateItem();
            }
            CheckForNewStackAttachment();
            if (scaleChildren != ScaleChildrenGlobal)
            {
                scaleChildren = ScaleChildrenGlobal;
                UI_Control c = Fields["scaleChildren"].uiControlEditor;
                if (c != null && c.partActionItem != null) c.partActionItem.UpdateItem();
            }
            // Geometry changes from our own controls apply immediately. This
            // staggered check only catches edits from other mods / undo.
            if (Time.frameCount >= nextGeometryCheckFrame)
            {
                nextGeometryCheckFrame = Time.frameCount + 12 + (part.GetInstanceID() & 7);
                if (!Mathf.Approximately(BaseFactor, lastBase))
                    AfterTweakScale();
                else if ((CurrentScale() - Target).sqrMagnitude > 1e-10f)
                    Apply(true, true);
                else if (exponentScaler != null)
                {
                    exponentScaler.Apply(scaleX, scaleY, scaleZ);
                    if (crewCapacityScaling != null && crewCapacityScaling.Apply(scaleX, scaleY, scaleZ))
                        FireEditorShipModified();
                    if (dockingPorts != null) dockingPorts.Apply(Target * BaseFactor, b9Support != null);
                }
            }
            // Resource events drive ordinary updates. A slower compatibility
            // scan remains for silent fuel switchers, with no full traversal
            // of stock tanks unless explicitly enabled in Settings.cfg.
            bool compatibilityScan = hasDynamicResourceProvider ||
                Settings.CheckAllResourcesPeriodically;
            if (resourceCheckPending ||
                (compatibilityScan && Time.realtimeSinceStartup >= nextResourceCheckTime))
            {
                resourceCheckPending = false;
                nextResourceCheckTime = Time.realtimeSinceStartup +
                    Settings.ResourceFallbackSeconds + (part.GetInstanceID() & 31) * 0.05f;
                ApplyResourceScaling(false);
            }
        }

        // Match the newly attached child to its parent stack node. Never run
        // this when loading an existing craft, or when an already attached
        // part merely gets resized: only a parent transition arms matching.
        // Poll briefly because KSP can assign parent before filling the node
        // attachment references on the following editor frame.
        void CheckForNewStackAttachment()
        {
            if (!HighLogic.LoadedSceneIsEditor || part == null) return;
            if (!nodeAttachWatcher.Observe(part.parent) || !initialized) return;
            if (!MatchNodeSizesGlobal || !string.IsNullOrEmpty(restrictedReason))
            {
                nodeAttachWatcher.Complete();
                return;
            }
            AttachNode source, destination;
            if (!FindConnectionNodes(part.parent, out source, out destination))
            {
                nodeAttachWatcher.Retry();
                return;
            }
            nodeAttachWatcher.Complete();  // One attempt per NEW attachment only.
            float desiredDiameter = (float)EffectiveNodeDiameter(part.parent, destination);
            float currentDiameter = (float)EffectiveNodeDiameter(part, source);
            if (!(desiredDiameter > 0f && currentDiameter > 0f)) return;
            double ratio = desiredDiameter / currentDiameter;
            if (ratio < 1.005d && ratio > 0.995d) return;

            ScaleMath.Axes multiplier = ScaleMath.NodeMatchMultipliers(
                source.orientation.x, source.orientation.y, source.orientation.z,
                desiredDiameter, currentDiameter);
            Vector3 before = Target;
            scaleX = Clamp((float)(before.x * multiplier.X));
            scaleY = Clamp((float)(before.y * multiplier.Y));
            scaleZ = Clamp((float)(before.z * multiplier.Z));
            if ((Target - before).sqrMagnitude < 1e-10f) return;
            ScaleDebug.Info(part, "node match", "attached to " + part.parent.name +
                "; target diameter=" + ScaleDebug.Format(desiredDiameter) +
                "; original diameter=" + ScaleDebug.Format(currentDiameter) +
                "; requested axes=" + ScaleDebug.Format(Target));
            // Uses the same Career/safety/behavior validation and symmetry
            // propagation as a PAW edit. No direct writes to mass/resources.
            CommitChange(before, null);
        }

        // Prefer actual node connection references rather than guessing based
        // on node labels (top/bottom are not reliable for rotated parts).
        bool FindConnectionNodes(Part parent, out AttachNode source, out AttachNode destination)
        {
            source = null;
            destination = null;
            if (parent == null) return false;
            foreach (AttachNode node in part.attachNodes)
            {
                if (node != null && !string.IsNullOrEmpty(node.id) &&
                    node.attachedPart == parent)
                {
                    source = node;
                    break;
                }
            }
            if (source == null) return false; // Skip surface-attached parts.
            float shortest = float.MaxValue;
            Vector3 sourceWorld = part.transform.TransformPoint(source.position);
            foreach (AttachNode node in parent.attachNodes)
            {
                if (node == null || node.attachedPart != part) continue;
                float sqDistance = (parent.transform.TransformPoint(node.position) -
                    sourceWorld).sqrMagnitude;
                if (sqDistance < shortest)
                {
                    shortest = sqDistance;
                    destination = node;
                }
            }
            return destination != null;
        }

        double EffectiveNodeDiameter(Part owner, AttachNode node)
        {
            if (owner == null || node == null) return 0d;
            ModuleStretchScale scaler = owner.FindModuleImplementing<ModuleStretchScale>();
            if (scaler == null || !scaler.initialized) return ScaleMath.NominalNodeDiameter(node.size);
            int originalTier;
            Vector3 normal;
            if (!scaler.prefabNodeSize.TryGetValue(node.id, out originalTier) ||
                !scaler.prefabNodeOrientation.TryGetValue(node.id, out normal))
                return ScaleMath.NominalNodeDiameter(node.size);
            Vector3 axes = scaler.Target * scaler.BaseFactor;
            return ScaleMath.NodeDiameter(originalTier, normal.x, normal.y, normal.z,
                axes.x, axes.y, axes.z);
        }

        // ------------------------------------------------------------------ mass / cost / resources

        double StretchVolumeFactor
        {
            get { return (double)scaleX * scaleY * scaleZ *
                FaceMorphMath.VolumeFactor(GetMorphFace(0), GetMorphFace(1)); }
        }

        double ExponentFactor(double exponent)
        {
            // Cubic-root equivalent linear stretch. Uniform (s,s,s) exactly
            // reproduces TweakScale's s^exponent behavior.
            return ScaleMath.ExponentFactor(StretchVolumeFactor, exponent);
        }


        // KSP supplies a MASS MODIFIER, not the finished mass. Never use a
        // previously scaled Part.mass (or a mass modifier that may already
        // incorporate the stretch) as our unscaled reference. In particular,
        // multiplying the sum of other modules' dynamic mass modifiers by the
        // PartSculptor factor causes a feedback loop on fuel-switching tanks.
        // The part prefab's dry mass is stable across PAW edits and reloads.
        float NativeDryMass(float fallbackMass)
        {
            Part prefab = part != null && part.partInfo != null ?
                part.partInfo.partPrefab : null;
            return prefab != null ? prefab.mass : fallbackMass;
        }

        // The uniform TweakScale module (if present) owns its own mass delta.
        // PartSculptor multiplies the part's TweakScale-adjusted *base* mass,
        // not unrelated IPartMassModifier deltas, which may already be scaled.
        // Always query TweakScale using the ORIGINAL part mass, not part.mass.
        float TweakScaleMassAdjustment(float nativeMass, ModifierStagingSituation sit,
            bool logDebug = true)
        {
            if (tsModule == null || !tsModule.isEnabled) return 0f;
            IPartMassModifier modifier = tsModule as IPartMassModifier;
            if (modifier == null) return 0f;
            try
            {
                float delta = modifier.GetModuleMass(nativeMass, sit);
                if (logDebug && ScaleDebug.Enabled)
                    ScaleDebug.Computed(tsModule, part, "mass", "TweakScale modifier", delta,
                        "native=" + ScaleDebug.Format(nativeMass));
                return delta;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PartSculptor] TweakScale mass modifier failed on " +
                    part.name + ": " + ex.Message);
                return 0f;
            }
        }

        // Absolute target for the mass portion owned by the base part and
        // TweakScale. Other modules' modifiers are deliberately left additive.
        double ScaledDryMassTarget(float nativeMass, ModifierStagingSituation sit)
        {
            return (nativeMass + TweakScaleMassAdjustment(nativeMass, sit)) *
                ExponentFactor(exponentScaler != null ? exponentScaler.MassExponent : 3d);
        }

        // Used for the PAW display. AvailablePart.cost includes the cost of
        // the prefab's initially-filled resources, so subtract those to show
        // dry cost rather than accidentally charging for fuel twice.
        // GetModuleCost uses the game's supplied defaultCost instead.
        float BaseDryCost(float fallbackCost)
        {
            if (part == null || part.partInfo == null) return fallbackCost;
            double dryCost = part.partInfo.cost;
            Part prefab = part.partInfo.partPrefab;
            if (prefab != null && prefab.Resources != null)
            {
                foreach (PartResource resource in prefab.Resources)
                {
                    if (resource != null && resource.info != null)
                        dryCost -= resource.amount * resource.info.unitCost;
                }
            }
            return (float)dryCost;
        }

        // Same rule as mass: TweakScale's dry-cost delta is part of the
        // stretchable base, while other modules' potentially dynamic cost
        // adjustments remain additive and are never multiplied repeatedly.
        float TweakScaleCostAdjustment(float nativeCost, ModifierStagingSituation sit)
        {
            if (tsModule == null || !tsModule.isEnabled) return 0f;
            IPartCostModifier modifier = tsModule as IPartCostModifier;
            if (modifier == null) return 0f;
            try
            {
                float delta = modifier.GetModuleCost(nativeCost, sit);
                if (ScaleDebug.Enabled)
                    ScaleDebug.Computed(tsModule, part, "cost", "TweakScale modifier", delta,
                        "native=" + ScaleDebug.Format(nativeCost));
                return delta;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PartSculptor] TweakScale cost modifier failed on " +
                    part.name + ": " + ex.Message);
                return 0f;
            }
        }

        bool calculatingMass;
        bool calculatingCost;

        float OtherMassAdjustments(float baseMass, ModifierStagingSituation sit)
        {
            if (calculatingMass || part == null) return 0f;
            calculatingMass = true;
            float total = 0f;
            try
            {
                foreach (PartModule module in part.Modules)
                {
                    if (module == this || module == tsModule || !module.isEnabled) continue;
                    IPartMassModifier modifier = module as IPartMassModifier;
                    if (modifier != null)
                    {
                        try
                        {
                            float contribution = modifier.GetModuleMass(baseMass, sit);
                            total += contribution;
                            if (ScaleDebug.Enabled)
                                ScaleDebug.Computed(module, part, "mass", "external module " +
                                    module.moduleName, contribution,
                                    "base=" + ScaleDebug.Format(baseMass));
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning("[PartSculptor] Mass modifier failed on " +
                                module.moduleName + ": " + ex.Message);
                        }
                    }
                }
            }
            finally { calculatingMass = false; }
            return total;
        }

        float OtherCostAdjustments(float baseCost, ModifierStagingSituation sit)
        {
            if (calculatingCost || part == null) return 0f;
            calculatingCost = true;
            float total = 0f;
            try
            {
                foreach (PartModule module in part.Modules)
                {
                    if (module == this || module == tsModule || !module.isEnabled) continue;
                    IPartCostModifier modifier = module as IPartCostModifier;
                    if (modifier != null)
                    {
                        try
                        {
                            float contribution = modifier.GetModuleCost(baseCost, sit);
                            total += contribution;
                            if (ScaleDebug.Enabled)
                                ScaleDebug.Computed(module, part, "cost", "external module " +
                                    module.moduleName, contribution,
                                    "base=" + ScaleDebug.Format(baseCost));
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning("[PartSculptor] Cost modifier failed on " +
                                module.moduleName + ": " + ex.Message);
                        }
                    }
                }
            }
            finally { calculatingCost = false; }
            return total;
        }

        // B9's baseVolume already changes its live volume-derived mass/cost.
        // Apply only the remainder needed to reach the original-value exponent
        // target, including fixed subtype addedMass and addedCost.
        double FuelSwitchExtraMass(float nativeMass, ModifierStagingSituation sit)
        {
            return fuelSwitchSupport == null ? 0d :
                fuelSwitchSupport.NativeMass(nativeMass, sit) *
                (ExponentFactor(exponentScaler != null ? exponentScaler.MassExponent : 3d) - 1d);
        }

        double FuelSwitchExtraCost(float nativeCost, ModifierStagingSituation sit)
        {
            return fuelSwitchSupport == null ? 0d :
                fuelSwitchSupport.NativeCost(nativeCost, sit) *
                (ExponentFactor(exponentScaler != null ? exponentScaler.CostExponent : 3d) - 1d);
        }

        double B9ExtraMass(float nativeMass, ModifierStagingSituation sit)
        {
            return b9Support == null ? 0d : b9Support.ExtraMass(nativeMass, sit,
                ExponentFactor(exponentScaler != null ? exponentScaler.MassExponent : 3d));
        }

        double B9ExtraCost(float nativeCost, ModifierStagingSituation sit)
        {
            return b9Support == null ? 0d : b9Support.ExtraCost(nativeCost, sit,
                ExponentFactor(exponentScaler != null ? exponentScaler.CostExponent : 3d));
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            // KSP adds each IPartMassModifier delta to the base mass supplied
            // to THAT query (including VesselDeltaV staging snapshots). Reach
            // the prefab-anchored target without sampling the live part.mass.
            // Other module deltas remain additive and unscaled.
            float nativeMass = NativeDryMass(defaultMass);
            // KSP queries this frequently; avoid ScaleDebug calls on this path,
            // including the TweakScale mass adjustment helper.
            double tweakDelta = TweakScaleMassAdjustment(nativeMass, sit, false);
            // Use the mass BASE supplied for this individual KSP mass query.
            // In particular, VesselDeltaV can evaluate staging snapshots whose
            // defaultMass differs from the live part.mass. Subtracting live
            // part.mass here would make the combined simulator mass oscillate
            // between the snapshots despite an unchanged vessel.
            double target = (nativeMass + tweakDelta) * ExponentFactor(
                exponentScaler != null ? exponentScaler.MassExponent : 3d);
            double delta = ScaleMath.MassModifierDelta(defaultMass, target, tweakDelta,
                B9ExtraMass(nativeMass, sit), FuelSwitchExtraMass(nativeMass, sit));
            float result = !double.IsNaN(delta) && !double.IsInfinity(delta) &&
                Math.Abs(delta) <= float.MaxValue ? (float)delta : 0f;
            return result;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            // Anchor dry cost to the prefab, not to a possibly already-scaled
            // value returned by KSP or a dynamic part-cost modifier. Leave
            // other modules' cost deltas additive, just as for dry mass.
            double nativeCost = BaseDryCost(defaultCost);
            double tweakDelta = TweakScaleCostAdjustment((float)nativeCost, sit);
            double target = (nativeCost + tweakDelta) * ExponentFactor(
                exponentScaler != null ? exponentScaler.CostExponent : 3d);
            double delta = target - defaultCost - tweakDelta + B9ExtraCost((float)nativeCost, sit) +
                FuelSwitchExtraCost((float)nativeCost, sit);
            float result = !double.IsNaN(delta) && !double.IsInfinity(delta) &&
                Math.Abs(delta) <= float.MaxValue ? (float)delta : 0f;
            if (ScaleDebug.Enabled)
                ScaleDebug.Computed(this, part, "cost", "KSP cost modifier delta", result,
                    "native=" + ScaleDebug.Format(nativeCost) +
                    "; defaultCost=" + ScaleDebug.Format(defaultCost) +
                    "; TweakScale delta=" + ScaleDebug.Format(tweakDelta) +
                    "; target=" + ScaleDebug.Format(target) +
                    "; exponent=" + ScaleDebug.Format(exponentScaler != null ? exponentScaler.CostExponent : 3d));
            return result;
        }

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }

        static bool AlmostEqual(double a, double b)
        {
            return Math.Abs(a - b) <= Math.Max(1e-7d, Math.Max(Math.Abs(a), Math.Abs(b)) * 1e-7d);
        }

        // Scales standard PartResource storage. If a tank-switch module replaces
        // a resource or rewrites a capacity, adopt the new capacity as the native
        // value, then apply ONLY our non-uniform stretch. This avoids fighting
        // with third-party fuel selection, while keeping half-full tanks half-full.
        void ApplyResourceScaling(bool notifyEditorShip = false)
        {
            if (part == null || part.Resources == null) return;
            // Never race B9's own resource/node/field updates during a switch.
            if (b9SubtypeChangePending || fuelSwitchChangePending) return;
            double tweakVolume = Math.Pow(BaseFactor, 3d);
            // The TweakScale module owns the uniform factor; we apply only the
            // independent PartSculptor exponent on top of its capacities.
            if (exponentScaler != null && !exponentScaler.ScaleResources) return;
            double stretch = ExponentFactor(exponentScaler != null ?
                exponentScaler.CapacityExponent : 3d);
            double totalVolume = tweakVolume * stretch;
            if (!Valid(totalVolume) || !Valid(stretch)) return;

            double oldTotalVolume = Valid(lastAppliedResourceVolume) ?
                lastAppliedResourceVolume : 1d;
            bool changed = false;
            bool structureChanged = false;
            var current = new HashSet<PartResource>();
            foreach (PartResource resource in part.Resources)
            {
                if (resource == null || string.IsNullOrEmpty(resource.resourceName) ||
                    !Valid(resource.maxAmount)) continue;
                // B9's baseVolume and UpdateVolume now control B9 tank resources.
                // Scaling maxAmount again here would apply the volume twice.
                if (b9Support != null &&
                    b9Support.OwnsScaledResource(resource.resourceName)) continue;
                current.Add(resource);
                ResourceScaleData data;
                if (!liveResourceScale.TryGetValue(resource, out data))
                {
                    // On a loaded craft, capacities may already include our
                    // saved multiplier. Saved per-resource bases disambiguate
                    // those values from fuel-switcher capacities.
                    double baseline;
                    double saved;
                    double prefab;
                    if (!hasDynamicResourceProvider &&
                        savedResourceBases.TryGetValue(resource.resourceName, out saved))
                        baseline = saved; // unchanged stock tank, even if KSP rewrote maxAmount
                    else if (!hasDynamicResourceProvider &&
                        prefabResourceCapacity.TryGetValue(resource.resourceName, out prefab))
                        baseline = prefab; // never infer stock tank baseline from scaled capacity
                    else if (savedResourceBases.TryGetValue(resource.resourceName, out saved) &&
                        AlmostEqual(resource.maxAmount, saved * oldTotalVolume))
                        baseline = saved;
                    else if (prefabResourceCapacity.TryGetValue(resource.resourceName, out prefab) &&
                        AlmostEqual(resource.maxAmount, prefab * oldTotalVolume))
                        baseline = prefab;
                    else if (prefabResourceCapacity.TryGetValue(resource.resourceName, out prefab) &&
                        (AlmostEqual(resource.maxAmount, prefab * tweakVolume) ||
                         AlmostEqual(resource.maxAmount, prefab * totalVolume)))
                        baseline = prefab; // also recovers old crafts without saved bases
                    else if (!resourcesInitialized && oldTotalVolume > 1.000001d &&
                        savedResourceBases.Count == 0)
                        baseline = resource.maxAmount / oldTotalVolume; // legacy craft
                    else
                        baseline = resource.maxAmount / tweakVolume; // new/switcher resource

                    // Prefer the exact native baseline for this B9 subtype.
                    // Saved legacy baselines are still a fallback for pre-B9
                    // integration craft files. Do not adopt an already scaled
                    // capacity when reopening a craft.
                    if (b9Support != null)
                    {
                        double cached;
                        if (b9ResourceBaselines.TryGetValue(
                            B9PartSwitchSupport.ResourceKey(b9Signature, resource.resourceName),
                            out cached) && Valid(cached))
                            baseline = cached;
                    }
                    if (fuelSwitchSupport != null)
                    {
                        double stored;
                        if (fuelSwitchResourceBases.TryGetValue(
                            FuelSwitchSupport.Key(FuelSwitchKeySignature(), resource.resourceName),
                            out stored) && FuelSwitchMath.FinitePositive(stored))
                            baseline = stored;
                    }
                    data = new ResourceScaleData
                    {
                        resource = resource,
                        name = resource.resourceName,
                        nativeCapacity = baseline,
                        lastWrittenCapacity = resource.maxAmount
                    };
                    liveResourceScale.Add(resource, data);
                    if (fuelSwitchSupport != null && FuelSwitchMath.FinitePositive(data.nativeCapacity))
                        fuelSwitchResourceBases[FuelSwitchSupport.Key(
                            FuelSwitchKeySignature(), data.name)] = data.nativeCapacity;
                    if (b9Support != null && Valid(data.nativeCapacity))
                        b9ResourceBaselines[B9PartSwitchSupport.ResourceKey(
                            b9Signature, data.name)] = data.nativeCapacity;
                    if (ScaleDebug.Enabled)
                        ScaleDebug.Info(part, "resource", resource.resourceName +
                        " baseline captured=" + ScaleDebug.Format(baseline) +
                        "; live capacity=" + ScaleDebug.Format(resource.maxAmount) +
                        "; volume factor=" + ScaleDebug.Format(totalVolume));
                    structureChanged = true;
                }
                else if (data.name != resource.resourceName)
                {
                    // Some fuel switchers mutate an existing PartResource in-place.
                    string previousName = data.name;
                    double previousNative = data.nativeCapacity;
                    data.name = resource.resourceName;
                    double b9Replacement;
                    data.nativeCapacity = b9Support != null && b9ResourceBaselines.TryGetValue(
                        B9PartSwitchSupport.ResourceKey(b9Signature, data.name), out b9Replacement) ?
                        b9Replacement : resource.maxAmount / tweakVolume;
                    if (fuelSwitchSupport != null)
                    {
                        double stored;
                        bool found = fuelSwitchResourceBases.TryGetValue(
                            FuelSwitchSupport.Key(FuelSwitchKeySignature(), data.name), out stored);
                        data.nativeCapacity = FuelSwitchMath.NativeCapacity(resource.maxAmount,
                            tweakVolume, stored, found, stretch, previousNative, false);
                        fuelSwitchResourceBases[FuelSwitchSupport.Key(
                            FuelSwitchKeySignature(), data.name)] = data.nativeCapacity;
                    }
                    if (b9Support != null)
                        b9ResourceBaselines[B9PartSwitchSupport.ResourceKey(
                            b9Signature, data.name)] = data.nativeCapacity;
                    ScaleDebug.Info(part, "resource", "resource replaced " + previousName +
                        " -> " + data.name);
                    ScaleDebug.Change(part, "resource", data.name + ".nativeCapacity",
                        previousNative, data.nativeCapacity, "switcher change");
                    structureChanged = true;
                }
                else if (fuelSwitchSupport != null && fuelSwitchResourceRebasePending)
                {
                    // The same PartResource object may represent a different
                    // setup (and even the same named fuel with a new capacity).
                    // A mesh-only switch keeps the old baseline; a real tank
                    // switch rebases to that setup's saved/native capacity.
                    double stored;
                    bool found = fuelSwitchResourceBases.TryGetValue(
                        FuelSwitchSupport.Key(FuelSwitchKeySignature(), data.name), out stored);
                    bool unchanged = AlmostEqual(resource.maxAmount, data.lastWrittenCapacity);
                    data.nativeCapacity = FuelSwitchMath.NativeCapacity(resource.maxAmount,
                        tweakVolume, stored, found, stretch, data.nativeCapacity, unchanged);
                    fuelSwitchResourceBases[FuelSwitchSupport.Key(
                        FuelSwitchKeySignature(), data.name)] = data.nativeCapacity;
                }
                else if (b9Support != null && b9ResourceRebasePending)
                {
                    // Switchers may reuse the very same PartResource object
                    // and resource name for different capacities. The subtype
                    // identity, not just object identity, determines the base.
                    double cached;
                    if (AlmostEqual(resource.maxAmount, data.lastWrittenCapacity))
                    {
                        // B9 did not change this resource (mesh-only switch).
                        // Keep the former baseline, even if the name matches.
                    }
                    else if (b9ResourceBaselines.TryGetValue(
                        B9PartSwitchSupport.ResourceKey(b9Signature, data.name), out cached) &&
                        (AlmostEqual(resource.maxAmount, cached * tweakVolume) ||
                         AlmostEqual(resource.maxAmount, cached * oldTotalVolume)))
                        data.nativeCapacity = cached;
                    else
                        data.nativeCapacity = resource.maxAmount / tweakVolume;
                    b9ResourceBaselines[B9PartSwitchSupport.ResourceKey(
                        b9Signature, data.name)] = data.nativeCapacity;
                }
                else if (!AlmostEqual(resource.maxAmount, data.lastWrittenCapacity))
                {
                    // A GUI refresh or another module can rewrite maxAmount
                    // using the already-stretched capacity. That does NOT make
                    // it a new native capacity. For normal (non-switching)
                    // tanks retain our captured native value indefinitely.
                    // Fuel-switching mods may genuinely change it: only they
                    // are allowed to supply a new native capacity here.
                    if (hasDynamicResourceProvider)
                    {
                        double expectedOld = data.nativeCapacity * oldTotalVolume;
                        if (!AlmostEqual(resource.maxAmount, expectedOld))
                        {
                            // New switcher value is normally un-stretched.
                            // An unchanged already-scaled value must not be
                            // adopted as a new baseline.
                            double raw = resource.maxAmount / tweakVolume;
                            double scaled = resource.maxAmount / oldTotalVolume;
                            if (AlmostEqual(raw, data.nativeCapacity) ||
                                AlmostEqual(scaled, data.nativeCapacity))
                            {
                                // No actual switch; keep the original base.
                            }
                            else
                            {
                                ScaleDebug.Change(part, "resource", data.name + ".nativeCapacity",
                                    data.nativeCapacity, raw, "external tank-switcher value");
                                data.nativeCapacity = raw;
                            }
                        }
                    }
                }

                double desired = fuelSwitchSupport != null ?
                    FuelSwitchMath.AbsoluteCapacity(data.nativeCapacity, tweakVolume, stretch) :
                    data.nativeCapacity * totalVolume;
                if (!Valid(desired)) continue;
                if (!AlmostEqual(resource.maxAmount, desired))
                {
                    double originalMax = resource.maxAmount;
                    double previousAmount = resource.amount;
                    resource.maxAmount = desired;
                    double amountExponent = exponentScaler != null ?
                        exponentScaler.AmountExponent : 3d;
                    double capacityExponent = exponentScaler != null ?
                        exponentScaler.CapacityExponent : 3d;
                    if (Math.Abs(amountExponent - capacityExponent) < 1e-8d)
                        resource.amount = desired * Math.Max(0d,
                            Math.Min(1d, previousAmount / originalMax));
                    else
                    {
                        // Separate !amount exponent: scale the current amount by
                        // the change of stretch (rather than refilling a tank).
                        double oldStretch = oldTotalVolume / tweakVolume;
                        double currentLinear = Math.Pow(StretchVolumeFactor, 1d / 3d);
                        double oldLinear = Math.Abs(capacityExponent) > 1e-10d ?
                            Math.Pow(Math.Max(1e-12d, oldStretch), 1d / capacityExponent) :
                            currentLinear;
                        double amountRatio = Math.Pow(currentLinear / oldLinear,
                            amountExponent);
                        resource.amount = Math.Max(0d,
                            Math.Min(desired, previousAmount * amountRatio));
                    }
                    if (ScaleDebug.Enabled)
                    {
                        ScaleDebug.Change(part, "resource", resource.resourceName + ".maxAmount",
                            originalMax, resource.maxAmount,
                            "native=" + ScaleDebug.Format(data.nativeCapacity) +
                            "; factor=" + ScaleDebug.Format(totalVolume));
                        ScaleDebug.Change(part, "resource", resource.resourceName + ".amount",
                            previousAmount, resource.amount,
                            "amount exponent=" + ScaleDebug.Format(amountExponent));
                    }
                    changed = true;
                }
                data.lastWrittenCapacity = resource.maxAmount;
                if (fuelSwitchSupport != null && FuelSwitchMath.FinitePositive(data.nativeCapacity))
                    fuelSwitchResourceBases[FuelSwitchSupport.Key(
                        FuelSwitchKeySignature(), data.name)] = data.nativeCapacity;
                if (b9Support != null && Valid(data.nativeCapacity))
                    b9ResourceBaselines[B9PartSwitchSupport.ResourceKey(
                        b9Signature, data.name)] = data.nativeCapacity;
            }

            if (liveResourceScale.Count != current.Count)
            {
                var removed = new List<PartResource>();
                foreach (PartResource r in liveResourceScale.Keys)
                    if (!current.Contains(r)) removed.Add(r);
                foreach (PartResource r in removed)
                {
                    ScaleDebug.Info(part, "resource", "removed resource binding: " +
                        r.resourceName);
                    liveResourceScale.Remove(r);
                }
                structureChanged |= removed.Count != 0;
            }

            b9ResourceRebasePending = false;
            fuelSwitchResourceRebasePending = false;
            lastAppliedResourceVolume = totalVolume;
            resourcesInitialized = true;
            UpdateCalculatedValues();
            if ((!changed && !structureChanged) || !HighLogic.LoadedSceneIsEditor) return;

            // Notify other modules only when a real capacity change occurred.
            if (changed)
            {
                try
                {
                    writingResources = true;
                    BaseEventDetails details = new BaseEventDetails(BaseEventDetails.Sender.USER);
                    details.Set<int>("InstanceID", part.GetInstanceID());
                    details.Set<Type>("issuer", GetType());
                    part.SendEvent("OnPartResourceChanged", details, 0);
                }
                finally { writingResources = false; }
            }
            resourceDisplayDirty = true;
            if (notifyEditorShip) FireEditorShipModified();
        }

        // Live snapshot of everything PartSculptor changes that has a stable
        // original/reference value. Values are not sourced from the DEBUG log;
        // re-opening the window or adjusting an axis recomputes the list.
        internal void CollectScaledValues(List<ScaledValue> values)
        {
            if (part == null || values == null) return;

            values.Add(new ScaledValue("Scale", "X axis", 1d, scaleX, "×"));
            values.Add(new ScaledValue("Scale", "Y axis", 1d, scaleY, "×"));
            values.Add(new ScaledValue("Scale", "Z axis", 1d, scaleZ, "×"));
            values.Add(new ScaledValue("Scale", "Volume multiplier", 1d,
                StretchVolumeFactor, "×"));
            if (HasFaceMorph)
            {
                MorphFace low = GetMorphFace(0), high = GetMorphFace(1);
                values.Add(new ScaledValue("Face morph", "Bottom face X", 1f, low.Width, "width multiplier"));
                values.Add(new ScaledValue("Face morph", "Bottom face Z", 1f, low.Depth, "depth multiplier"));
                values.Add(new ScaledValue("Face morph", "Bottom tilt X", 0f, low.TiltX, "planar shear"));
                values.Add(new ScaledValue("Face morph", "Bottom tilt Z", 0f, low.TiltZ, "planar shear"));
                values.Add(new ScaledValue("Face morph", "Bottom offset X", 0f, low.OffsetX, "half-width units"));
                values.Add(new ScaledValue("Face morph", "Bottom offset Z", 0f, low.OffsetZ, "half-depth units"));
                values.Add(new ScaledValue("Face morph", "Top face X", 1f, high.Width, "width multiplier"));
                values.Add(new ScaledValue("Face morph", "Top face Z", 1f, high.Depth, "depth multiplier"));
                values.Add(new ScaledValue("Face morph", "Top tilt X", 0f, high.TiltX, "planar shear"));
                values.Add(new ScaledValue("Face morph", "Top tilt Z", 0f, high.TiltZ, "planar shear"));
                values.Add(new ScaledValue("Face morph", "Top offset X", 0f, high.OffsetX, "half-width units"));
                values.Add(new ScaledValue("Face morph", "Top offset Z", 0f, high.OffsetZ, "half-depth units"));
                values.Add(new ScaledValue("Face morph", "Face-volume factor", 1d,
                    FaceMorphMath.VolumeFactor(low, high), "approximate cross-section integral"));
            }

            if (b9Support != null)
            {
                double b9Factor = ExponentFactor(exponentScaler != null ?
                    exponentScaler.MassExponent : 3d);
                ModifierStagingSituation b9Situation = default(ModifierStagingSituation);
                float nativePartMass = NativeDryMass(part.mass);
                float nativePartCost = BaseDryCost(0f);
                float b9Mass = b9Support.GetMassModifier(nativePartMass, b9Situation);
                float b9Cost = b9Support.GetCostModifier(nativePartCost, b9Situation);
                double costFactor = ExponentFactor(exponentScaler != null ?
                    exponentScaler.CostExponent : 3d);
                // B9's live modifiers already include scaled baseVolume.
                // The additional PartSculptor delta completes the exponent
                // target; never display another multiplication of live values.
                double finalMass = b9Mass + b9Support.ExtraMass(
                    nativePartMass, b9Situation, b9Factor);
                double finalCost = b9Cost + b9Support.ExtraCost(
                    nativePartCost, b9Situation, costFactor);
                values.Add(new ScaledValue("B9PartSwitch", "Selected subtypes",
                    "B9 native", b9Support.SelectedSubtypes, "Live variant selection"));
                b9Support.CollectVolumeValues(values);
                values.Add(new ScaledValue("B9PartSwitch", "Subtype mass modifier",
                    b9Factor > 0d ? finalMass / b9Factor : b9Mass,
                    finalMass, "tonnes; includes B9 baseVolume change"));
                values.Add(new ScaledValue("B9PartSwitch", "Subtype cost modifier",
                    costFactor > 0d ? finalCost / costFactor : b9Cost,
                    finalCost, "funds; includes B9 baseVolume change"));
            }

            if (fuelSwitchSupport != null)
            {
                ModifierStagingSituation providerSituation = default(ModifierStagingSituation);
                float providerMass = fuelSwitchSupport.NativeMass(NativeDryMass(part.mass), providerSituation);
                float providerCost = fuelSwitchSupport.NativeCost(BaseDryCost(0f), providerSituation);
                values.Add(new ScaledValue("Fuel Switchers", "Selected setup", "native",
                    fuelSwitchSupport.Description, "MFT / Firespitter / IFS"));
                values.Add(new ScaledValue("Fuel Switchers", "Module mass modifier", providerMass,
                    providerMass * ExponentFactor(exponentScaler != null ?
                        exponentScaler.MassExponent : 3d), "tonnes"));
                values.Add(new ScaledValue("Fuel Switchers", "Module cost modifier", providerCost,
                    providerCost * ExponentFactor(exponentScaler != null ?
                        exponentScaler.CostExponent : 3d), "funds"));
            }

            float nativeMass = NativeDryMass(part.mass);
            float nativeCost = BaseDryCost(0f);
            ModifierStagingSituation situation = default(ModifierStagingSituation);
            // The base part plus TweakScale is stretched; unrelated modifier
            // deltas are added unchanged, exactly like the PAW dry readouts.
            float tweakMass = TweakScaleMassAdjustment(nativeMass, situation);
            float otherMass = OtherMassAdjustments(nativeMass, situation);
            double massBaseline = nativeMass + tweakMass + otherMass;
            double massCurrent = ScaledDryMassTarget(nativeMass, situation) + otherMass +
                B9ExtraMass(nativeMass, situation) + FuelSwitchExtraMass(nativeMass, situation);
            values.Add(new ScaledValue("Mass / Cost", "Dry mass", massBaseline,
                massCurrent, "t; exponent " + (exponentScaler != null ?
                exponentScaler.MassExponent : 3d).ToString("0.###", CultureInfo.InvariantCulture)));

            float tweakCost = TweakScaleCostAdjustment(nativeCost, situation);
            float otherCost = OtherCostAdjustments(nativeCost, situation);
            double costBaseline = nativeCost + tweakCost + otherCost;
            double costCurrent = (nativeCost + tweakCost) * ExponentFactor(
                exponentScaler != null ? exponentScaler.CostExponent : 3d) + otherCost +
                B9ExtraCost(nativeCost, situation) + FuelSwitchExtraCost(nativeCost, situation);
            values.Add(new ScaledValue("Mass / Cost", "Dry cost", costBaseline,
                costCurrent, "funds; exponent " + (exponentScaler != null ?
                exponentScaler.CostExponent : 3d).ToString("0.###", CultureInfo.InvariantCulture)));

            // Show actual live capacities and amounts; the original capacity
            // was captured on first binding or restored from the saved craft.
            double tweakVolume = Math.Pow(BaseFactor, 3d);
            foreach (PartResource resource in part.Resources)
            {
                if (resource == null) continue;
                ResourceScaleData data;
                if (!liveResourceScale.TryGetValue(resource, out data)) continue;
                string prefix = resource.resourceName + " ";
                double baseCapacity = data.nativeCapacity * tweakVolume;
                values.Add(new ScaledValue("Resources", prefix + "capacity",
                    baseCapacity, resource.maxAmount, "units"));
                // Resource amount is mutable in flight/editor. Rather than
                // presenting a stale initial fill as its actual original,
                // back-calculate an equivalent at the present fill level.
                double amountFactor = ExponentFactor(exponentScaler != null ?
                    exponentScaler.AmountExponent : 3d);
                if (amountFactor > 0d && !double.IsInfinity(amountFactor) &&
                    !double.IsNaN(amountFactor))
                    values.Add(new ScaledValue("Resources", prefix + "amount",
                        resource.amount / amountFactor, resource.amount,
                        "units; baseline inferred at current fill"));
            }

            Vector3 baselineModel = prefabModelScale * BaseFactor;
            if (model != null)
                values.Add(new ScaledValue("Geometry", "Model local scale",
                    baselineModel, model.localScale, "X / Y / Z"));

            Vector3 baseTotal = new Vector3(BaseFactor, BaseFactor, BaseFactor);
            foreach (AttachNode node in part.attachNodes)
            {
                if (node == null || string.IsNullOrEmpty(node.id)) continue;
                Vector3 prefabPos, prefabOriginalPos, normal;
                int originalSize;
                if (!prefabNodePos.TryGetValue(node.id, out prefabPos) ||
                    !prefabNodeOrigPos.TryGetValue(node.id, out prefabOriginalPos) ||
                    !prefabNodeOrientation.TryGetValue(node.id, out normal) ||
                    !prefabNodeSize.TryGetValue(node.id, out originalSize)) continue;
                values.Add(new ScaledValue("Attach nodes", node.id + " position",
                    Vector3.Scale(prefabPos, baseTotal), node.position, "local"));
                values.Add(new ScaledValue("Attach nodes", node.id + " original position",
                    Vector3.Scale(prefabOriginalPos, baseTotal), node.originalPosition, "local"));
                values.Add(new ScaledValue("Attach nodes", node.id + " size",
                    ScaledNodeSize(originalSize, normal, baseTotal), node.size, "tier"));
            }
            if (hasSrfNode && part.srfAttachNode != null)
            {
                AttachNode node = part.srfAttachNode;
                Vector3 originalPos = Vector3.Scale(prefabSrfPos, baseTotal);
                values.Add(new ScaledValue("Attach nodes", "Surface position",
                    originalPos, node.position, "local"));
                values.Add(new ScaledValue("Attach nodes", "Surface original position",
                    originalPos, node.originalPosition, "local"));
                values.Add(new ScaledValue("Attach nodes", "Surface size",
                    ScaledNodeSize(prefabSrfSize, prefabSrfOrientation, baseTotal),
                    node.size, "tier"));
            }
            if (exponentScaler != null)
                exponentScaler.CollectScaledValues(values);
            if (crewCapacityScaling != null)
                crewCapacityScaling.AppendReport(values);
            if (dockingPorts != null) dockingPorts.AppendReport(values);
            if (visualEffects != null)
                visualEffects.AppendReport(values);
        }

        void UpdateCalculatedValues()
        {
            string previous = displayVolume + "|" + displayDryMass + "|" +
                displayDryCost + "|" + displayCapacity + "|" + displayScaleSafety + "|" +
                displayCareerLimits;
            displayVolume = StretchVolumeFactor.ToString("0.###", CultureInfo.InvariantCulture) + "x";
            if (part == null) return;
            float nativeMass = NativeDryMass(part.mass);
            float baseCost = BaseDryCost(0f);
            // The UI mirrors KSP's additive mass-modifier result: immutable
            // prefab/TweakScale portion is stretched once; other modifier
            // contributions are added once, not multiplied again.
            double mass = ScaledDryMassTarget(nativeMass,
                default(ModifierStagingSituation));
            float otherMass = OtherMassAdjustments(nativeMass,
                default(ModifierStagingSituation));
            mass += otherMass + B9ExtraMass(nativeMass,
                default(ModifierStagingSituation));
            double cost = (baseCost + TweakScaleCostAdjustment(baseCost,
                default(ModifierStagingSituation))) * ExponentFactor(exponentScaler != null ?
                exponentScaler.CostExponent : 3d);
            cost += OtherCostAdjustments(baseCost, default(ModifierStagingSituation)) +
                B9ExtraCost(baseCost, default(ModifierStagingSituation));
            if (ScaleDebug.Enabled)
            {
                ScaleDebug.Computed(this, part, "mass", "display dry mass", mass,
                    "native=" + ScaleDebug.Format(nativeMass) +
                    "; other modifiers=" + ScaleDebug.Format(otherMass));
                ScaleDebug.Computed(this, part, "cost", "display dry cost", cost,
                    "native=" + ScaleDebug.Format(baseCost));
            }
            displayDryMass = mass.ToString("0.###", CultureInfo.InvariantCulture) + " t";
            displayDryCost = cost.ToString("N0", CultureInfo.InvariantCulture) + " funds";
            var capacity = new List<string>();
            foreach (PartResource resource in part.Resources)
            {
                if (resource == null) continue;
                if (capacity.Count >= 3) { capacity.Add("..."); break; }
                capacity.Add(resource.resourceName + " " +
                    resource.maxAmount.ToString("0.##", CultureInfo.InvariantCulture));
            }
            displayCapacity = capacity.Count == 0 ? "None" : string.Join(", ", capacity.ToArray());
            UpdateSafetyStatus();
            CareerScaleLimit career;
            if (TryGetCareerLimits(out career))
                displayCareerLimits = string.Format(CultureInfo.InvariantCulture,
                    "{0} level {1}: axes {2:0.###}-{3:0.###}x; volume <= {4:0.###}x",
                    career.facility, career.level, career.minAxis, career.maxAxis, career.maxVolume);
            else
                displayCareerLimits = "Not restricted";
            if (Fields != null && Fields["displayCareerLimits"] != null)
                Fields["displayCareerLimits"].guiActiveEditor =
                    Settings.EnableCareerLimits && HighLogic.CurrentGame != null &&
                    HighLogic.CurrentGame.Mode == Game.Modes.CAREER;
            if (initialized && HighLogic.LoadedSceneIsEditor &&
                previous != displayVolume + "|" + displayDryMass + "|" +
                    displayDryCost + "|" + displayCapacity + "|" + displayScaleSafety + "|" +
                    displayCareerLimits)
                resourceDisplayDirty = true;
        }

        // ------------------------------------------------------------------ geometry

        Vector3 CurrentScale()
        {
            Vector3 s = model.localScale / BaseFactor;
            return new Vector3(Div(s.x, prefabModelScale.x), Div(s.y, prefabModelScale.y), Div(s.z, prefabModelScale.z));
        }

        // Read building levels on demand: a new editor session after an upgrade
        // gets the updated limit without keeping it in a save or part module.
        // Science/Sandbox and disabled Career limits are never affected.
        static bool TryGetCareerLimits(out CareerScaleLimit limits)
        {
            limits = new CareerScaleLimit();
            if (!Settings.EnableCareerLimits || !HighLogic.LoadedSceneIsEditor ||
                HighLogic.CurrentGame == null || HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
                return false;

            bool sph = EditorDriver.editorFacility == EditorFacility.SPH;
            SpaceCenterFacility facility = sph ? SpaceCenterFacility.SpaceplaneHangar :
                SpaceCenterFacility.VehicleAssemblyBuilding;
            limits.facility = sph ? "SPH" : "VAB";
            int level = 1;
            try
            {
                // With building upgrades turned off KSP treats the facilities
                // as fully upgraded; keep the highest tier available.
                if (ScenarioUpgradeableFacilities.Instance != null &&
                    !ScenarioUpgradeableFacilities.Instance.enabled)
                    level = 3;
                else if (ScenarioUpgradeableFacilities.Instance != null)
                {
                    double normalized = ScenarioUpgradeableFacilities.GetFacilityLevel(facility);
                    double upgrades = ScenarioUpgradeableFacilities.GetFacilityLevelCount(facility);
                    if (upgrades <= 0d || double.IsNaN(upgrades)) upgrades = 2d;
                    if (!double.IsNaN(normalized) && !double.IsInfinity(normalized))
                        level = (int)Math.Round(Math.Max(0d, Math.Min(1d, normalized)) * upgrades) + 1;
                }
            }
            catch (Exception e)
            {
                // Fail conservatively at level 1 rather than silently
                // allowing an unlimited resize when the API is unavailable.
                Debug.LogWarning("[PartSculptor] Could not read Career building level: " + e.Message);
            }
            limits.level = Math.Max(1, Math.Min(3, level));
            switch (limits.level)
            {
                case 1:
                    limits.minAxis = Settings.CareerLevel1MinScale;
                    limits.maxAxis = Settings.CareerLevel1MaxScale;
                    limits.maxVolume = Settings.CareerLevel1MaxVolume;
                    break;
                case 2:
                    limits.minAxis = Settings.CareerLevel2MinScale;
                    limits.maxAxis = Settings.CareerLevel2MaxScale;
                    limits.maxVolume = Settings.CareerLevel2MaxVolume;
                    break;
                default:
                    limits.minAxis = Settings.CareerLevel3MinScale;
                    limits.maxAxis = Settings.CareerLevel3MaxScale;
                    limits.maxVolume = Settings.CareerLevel3MaxVolume;
                    break;
            }
            // Global axis limits always remain authoritative.
            limits.minAxis = Mathf.Clamp(limits.minAxis, Settings.MinScale, Settings.MaxScale);
            limits.maxAxis = Mathf.Min(Settings.MaxScale, limits.maxAxis);
            if (limits.maxAxis < limits.minAxis) limits.maxAxis = limits.minAxis;
            return true;
        }

        // An existing craft may have axes beyond the current progression cap.
        // Do not snap those saved dimensions back when modifying a different
        // axis; permit steps TOWARDS the permitted interval, not further away.
        static float LimitCareerAxis(float before, float desired, float min, float max)
        {
            if (Mathf.Approximately(before, desired)) return desired;
            if (before < min)
                return desired < before ? before : Mathf.Min(desired, max);
            if (before > max)
                return desired > before ? before : Mathf.Max(desired, min);
            return Mathf.Clamp(desired, min, max);
        }

        // All axis sources (manual edit, preset, child, symmetry, undo) use
        // one volume/mass gate. For a single-axis edit, preserve the other
        // axes if possible; for presets, reduce all three proportionally.
        bool ValidateScaleChange(Vector3 before, string preferredAxis)
        {
            if ((Target - before).sqrMagnitude < 1e-10f) return true;
            limitWarning = "";
            if (!string.IsNullOrEmpty(restrictedReason))
            {
                scaleX = before.x; scaleY = before.y; scaleZ = before.z;
                lastX = scaleX; lastY = scaleY; lastZ = scaleZ;
                limitWarning = "Restricted: " + restrictedReason;
                WarnOnce(limitWarning);
                return false;
            }

            // TweakScale Science's allowGrow=false is meaningful for the
            // PartSculptor UI too. Do not alter already-saved craft dimensions;
            // simply block further growth past their existing value or 1x.
            if (exponentScaler != null && !exponentScaler.AllowGrow)
            {
                Vector3 wanted = Target;
                scaleX = Mathf.Min(scaleX, Mathf.Max(1f, before.x));
                scaleY = Mathf.Min(scaleY, Mathf.Max(1f, before.y));
                scaleZ = Mathf.Min(scaleZ, Mathf.Max(1f, before.z));
                if ((wanted - Target).sqrMagnitude > 1e-10f)
                {
                    limitWarning = "Behavior prohibits growth above 1x";
                    WarnOnce(limitWarning);
                }
            }

            CareerScaleLimit career;
            bool careerActive = TryGetCareerLimits(out career);
            if (careerActive)
            {
                Vector3 requested = Target;
                scaleX = LimitCareerAxis(before.x, scaleX, career.minAxis, career.maxAxis);
                scaleY = LimitCareerAxis(before.y, scaleY, career.minAxis, career.maxAxis);
                scaleZ = LimitCareerAxis(before.z, scaleZ, career.minAxis, career.maxAxis);
                if ((requested - Target).sqrMagnitude > 1e-10f)
                    limitWarning = "Limited by " + career.facility + " Career level " + career.level;
            }

            // The geometric limit includes TweakScale; the dry mass limit
            // constrains only the additional PartSculptor mass multiplier.
            double baseVolume = Math.Pow(BaseFactor, 3d);
            double massExponent = exponentScaler != null ? exponentScaler.MassExponent : 3d;
            double massVolumeAllowed = massExponent > 0d ?
                Math.Pow(Settings.MaxMassMultiplier, 3d / massExponent) : double.MaxValue;
            double allowed = Math.Min(massVolumeAllowed,
                Settings.MaxVolumeMultiplier / baseVolume);
            if (careerActive) allowed = Math.Min(allowed, career.maxVolume);

            // Grandfather old crafts above newly configured caps: an edit may
            // reduce their volume without being forced to shrink to today's cap.
            // Never let such an edit INCREASE above the already-saved volume.
            double oldVolume = (double)before.x * before.y * before.z;
            if (oldVolume > allowed) allowed = oldVolume;
            if (!(allowed > 0d) || double.IsNaN(allowed)) allowed = 0d;
            double targetVolume = StretchVolumeFactor;
            if (targetVolume > allowed * (1d + 1e-8d))
            {
                // Use the Career minimum for new edits, but do not force a
                // previously undersized saved axis upward just to clamp volume.
                double floorX = careerActive ? Math.Min(before.x, career.minAxis) : Settings.MinScale;
                double floorY = careerActive ? Math.Min(before.y, career.minAxis) : Settings.MinScale;
                double floorZ = careerActive ? Math.Min(before.z, career.minAxis) : Settings.MinScale;
                floorX = Math.Max(Settings.MinScale, floorX);
                floorY = Math.Max(Settings.MinScale, floorY);
                floorZ = Math.Max(Settings.MinScale, floorZ);
                double minimumPossible = floorX * floorY * floorZ;
                if (allowed < minimumPossible)
                {
                    // TweakScale may itself exceed the global volume limit;
                    // never corrupt geometry trying to solve an impossible cap.
                    scaleX = before.x; scaleY = before.y; scaleZ = before.z;
                    lastX = scaleX; lastY = scaleY; lastZ = scaleZ;
                    limitWarning = "Configured limit prevents resizing";
                    WarnOnce(limitWarning);
                    return false;
                }

                bool singleAxis = false;
                if (preferredAxis == "scaleX")
                {
                    double value = allowed / ((double)scaleY * scaleZ);
                    if (value >= floorX)
                    { scaleX = Clamp((float)Math.Min(value, careerActive ?
                        Math.Max(before.x, career.maxAxis) : Settings.MaxScale)); singleAxis = true; }
                }
                else if (preferredAxis == "scaleY")
                {
                    double value = allowed / ((double)scaleX * scaleZ);
                    if (value >= floorY)
                    { scaleY = Clamp((float)Math.Min(value, careerActive ?
                        Math.Max(before.y, career.maxAxis) : Settings.MaxScale)); singleAxis = true; }
                }
                else if (preferredAxis == "scaleZ")
                {
                    double value = allowed / ((double)scaleX * scaleY);
                    if (value >= floorZ)
                    { scaleZ = Clamp((float)Math.Min(value, careerActive ?
                        Math.Max(before.z, career.maxAxis) : Settings.MaxScale)); singleAxis = true; }
                }
                if (!singleAxis || StretchVolumeFactor > allowed * (1d + 1e-6d))
                {
                    // Binary search the greatest common reduction that fits
                    // while respecting per-axis minimums.
                    Vector3 desired = Target;
                    double low = 0d, high = 1d;
                    for (int i = 0; i < 48; i++)
                    {
                        double mid = (low + high) * 0.5d;
                        double x = Math.Max(floorX, desired.x * mid);
                        double y = Math.Max(floorY, desired.y * mid);
                        double z = Math.Max(floorZ, desired.z * mid);
                        if (x * y * z <= allowed) low = mid;
                        else high = mid;
                    }
                    scaleX = Clamp((float)Math.Max(floorX, desired.x * low));
                    scaleY = Clamp((float)Math.Max(floorY, desired.y * low));
                    scaleZ = Clamp((float)Math.Max(floorZ, desired.z * low));
                }
                limitWarning = careerActive && allowed <= career.maxVolume * (1d + 1e-7d)
                    ? "Limited by " + career.facility + " Career level " + career.level + " volume"
                    : "Limited by maximum mass/volume multiplier";
                WarnOnce(limitWarning);
            }
            lastX = scaleX; lastY = scaleY; lastZ = scaleZ;
            changeAlreadyValidated = true;
            return true;
        }

        void WarnOnce(string warning)
        {
            if (warning == lastLoggedWarning) return;
            lastLoggedWarning = warning;
            if (!string.IsNullOrEmpty(warning))
                Debug.LogWarning("[PartSculptor] " + part.name + ": " + warning);
        }

        void UpdateSafetyStatus()
        {
            if (!string.IsNullOrEmpty(restrictedReason))
                displayScaleSafety = "Restricted: " + restrictedReason;
            else if (!string.IsNullOrEmpty(limitWarning))
                displayScaleSafety = limitWarning;
            else
            {
                double massFactor = ExponentFactor(exponentScaler != null ?
                    exponentScaler.MassExponent : 3d);
                double volumeFactor = StretchVolumeFactor * Math.Pow(BaseFactor, 3d);
                if (massFactor >= Settings.WarnMassMultiplier ||
                    massFactor <= 1d / Settings.WarnMassMultiplier ||
                    volumeFactor >= Settings.WarnVolumeMultiplier ||
                    volumeFactor <= 1d / Settings.WarnVolumeMultiplier)
                    displayScaleSafety = "Warning: extreme scale (mass " +
                        massFactor.ToString("G4", CultureInfo.InvariantCulture) + "x; volume " +
                        volumeFactor.ToString("G4", CultureInfo.InvariantCulture) + "x)";
                else
                    displayScaleSafety = "OK";
            }
            if (displayScaleSafety == "OK") WarnOnce("");
            else WarnOnce(displayScaleSafety);
        }

        static float Div(float a, float b) { return Mathf.Abs(b) < 1e-9f ? 1f : a / b; }

        // KSP's AttachNode.size is an integer tier, not a continuous diameter.
        // Tier 0 is nominally 0.625 m; tiers 1, 2, 3 ... represent about
        // 1.25 m, 2.5 m, 3.75 m ... . Convert to a nominal diameter, apply
        // the effective *cross-sectional* scale, then choose the nearest tier.
        // This also permits a tier-0 node to grow when the part is widened.
        static int ScaledNodeSize(int originalSize, Vector3 normal, Vector3 totalScale)
        {
            return ScaleMath.NodeSizeTier(originalSize, normal.x, normal.y, normal.z,
                totalScale.x, totalScale.y, totalScale.z);
        }

        void Apply(bool moveNeighbours, bool moveChildren, bool fireModified = true)
        {
            if (model == null) return;

            // Detect changes from symmetry, external mods, and undo. Direct
            // UI actions were validated in CommitChange(), but this path can
            // run independently of those callbacks.
            if (!changeAlreadyValidated && initialized &&
                (Target - lastAcceptedScale).sqrMagnitude > 1e-10f)
                ValidateScaleChange(lastAcceptedScale, null);
            changeAlreadyValidated = false;

            float f = BaseFactor;
            Vector3 oldScale = CurrentScale();
            Vector3 newScale = Target;
            // Also track edits coming from external modules, symmetry/undo,
            // rather than solely PAW callbacks and gizmo interactions.
            if (HighLogic.LoadedSceneIsEditor && initialized &&
                (newScale - oldScale).sqrMagnitude > 1e-10f)
                SetPresetDisplay("Custom");
            Vector3 total = newScale * f;   // stretch on top of TweakScale
            Vector3 ratio = new Vector3(Div(newScale.x, oldScale.x), Div(newScale.y, oldScale.y), Div(newScale.z, oldScale.z));

            bool editor = HighLogic.LoadedSceneIsEditor && moveNeighbours;
            if (ScaleDebug.Enabled && ((newScale - oldScale).sqrMagnitude > 1e-10f ||
                !Mathf.Approximately(f, lastBase)))
                ScaleDebug.Info(part, "apply", "scale " + ScaleDebug.Format(oldScale) +
                    " -> " + ScaleDebug.Format(newScale) +
                    "; base TweakScale=" + ScaleDebug.Format(f) +
                    "; volume=" + ScaleDebug.Format(StretchVolumeFactor) +
                    "; move neighbours=" + moveNeighbours +
                    "; move children=" + moveChildren);
            ScaleDebug.Change(part, "geometry", "stretch axes", oldScale, newScale,
                "TweakScale=" + ScaleDebug.Format(f) +
                "; volume=" + ScaleDebug.Format(StretchVolumeFactor));

            // Work out every move in this part's local space first.
            Vector3 selfMove = Vector3.zero;
            var childMoves = new List<KeyValuePair<Part, Vector3>>();

            // Stack nodes
            foreach (AttachNode node in part.attachNodes)
            {
                Vector3 basePos, baseOrig;
                if (node == null || node.id == null || !prefabNodePos.TryGetValue(node.id, out basePos)) continue;
                baseOrig = prefabNodeOrigPos[node.id];

                Vector3 newPos = Vector3.Scale(basePos, total);
                Vector3 oldPos = node.position;
                Vector3 delta = newPos - oldPos;
                Vector3 oldOriginal = node.originalPosition;
                int oldSize = node.size;
                node.position = newPos;
                node.originalPosition = Vector3.Scale(baseOrig, total);
                // Rebuild node orientation from its unmorphed prefab baseline
                // before FaceMorphSupport.RebaseNodes captures it. Otherwise
                // each ordinary scale/edit would re-morph the prior normal.
                node.orientation = prefabNodeOrientation[node.id];
                node.size = ScaledNodeSize(prefabNodeSize[node.id],
                    prefabNodeOrientation[node.id], total);
                ScaleDebug.Change(part, "attach node", node.id + ".position",
                    oldPos, node.position);
                ScaleDebug.Change(part, "attach node", node.id + ".originalPosition",
                    oldOriginal, node.originalPosition);
                ScaleDebug.Change(part, "attach node", node.id + ".size",
                    oldSize, node.size);

                if (!editor || node.attachedPart == null || delta.sqrMagnitude < 1e-12f) continue;
                if (node.attachedPart == part.parent)
                    selfMove -= delta;
                else if (moveChildren && node.attachedPart.parent == part)
                    childMoves.Add(new KeyValuePair<Part, Vector3>(node.attachedPart, delta));
            }

            // Our own surface-attach node
            if (hasSrfNode)
            {
                Vector3 newPos = Vector3.Scale(prefabSrfPos, total);
                Vector3 oldPos = part.srfAttachNode.position;
                Vector3 oldOriginal = part.srfAttachNode.originalPosition;
                int oldSize = part.srfAttachNode.size;
                Vector3 delta = newPos - oldPos;
                part.srfAttachNode.position = newPos;
                part.srfAttachNode.originalPosition = newPos;
                part.srfAttachNode.orientation = prefabSrfOrientation;
                part.srfAttachNode.size = ScaledNodeSize(prefabSrfSize, prefabSrfOrientation, total);
                ScaleDebug.Change(part, "attach node", "surface.position", oldPos, part.srfAttachNode.position);
                ScaleDebug.Change(part, "attach node", "surface.originalPosition", oldOriginal, part.srfAttachNode.originalPosition);
                ScaleDebug.Change(part, "attach node", "surface.size", oldSize, part.srfAttachNode.size);
                if (editor && part.parent != null && part.srfAttachNode.attachedPart == part.parent)
                    selfMove -= delta;
            }

            // Children surface-attached to us: scale their attach point with our surface.
            if (editor && moveChildren)
            {
                foreach (Part child in part.children)
                {
                    if (child == null || child.srfAttachNode == null || child.srfAttachNode.attachedPart != part) continue;
                    Vector3 attachWorld = child.transform.TransformPoint(child.srfAttachNode.position);
                    Vector3 local = part.transform.InverseTransformPoint(attachWorld);
                    Vector3 delta = Vector3.Scale(local, ratio) - local;
                    if (delta.sqrMagnitude > 1e-12f)
                        childMoves.Add(new KeyValuePair<Part, Vector3>(child, delta));
                }
            }

            // Scale the model (meshes + colliders).
            Vector3 priorModelScale = model.localScale;
            model.localScale = Vector3.Scale(prefabModelScale, total);
            ScaleDebug.Change(part, "geometry", "model.localScale", priorModelScale,
                model.localScale, "prefab=" + ScaleDebug.Format(prefabModelScale));
            lastAcceptedScale = Target;
            if (visualEffects != null) visualEffects.Apply(newScale);

            // Update B9 baseVolume from its immutable native value, before
            // considering other resources. B9 owns its own managed capacities.
            // Only refresh B9 resources in the editor: its flight refresh
            // intentionally zeros the amount and must not be invoked here.
            if (b9Support != null)
                b9Support.ApplyBaseVolumes(part, StretchVolumeFactor,
                    HighLogic.LoadedSceneIsEditor);
            // Called for initial load, direct edits, symmetry counterparts,
            // child stretching and changes originating from TweakScale.
            ApplyResourceScaling(false);
            if (ScaleDebug.Enabled && ((newScale - oldScale).sqrMagnitude > 1e-10f ||
                !Mathf.Approximately(f, lastBase)))
            {
                float nativeMass = NativeDryMass(part.mass);
                double targetMass = ScaledDryMassTarget(nativeMass, default(ModifierStagingSituation));
                double nativeCost = BaseDryCost(0f);
                double targetCost = (nativeCost + TweakScaleCostAdjustment((float)nativeCost,
                    default(ModifierStagingSituation))) * ExponentFactor(exponentScaler != null ?
                    exponentScaler.CostExponent : 3d);
                ScaleDebug.Info(part, "mass", "prefab=" + ScaleDebug.Format(nativeMass) +
                    "; live Part.mass=" + ScaleDebug.Format(part.mass) +
                    "; target=" + ScaleDebug.Format(targetMass) +
                    "; exponent=" + ScaleDebug.Format(exponentScaler != null ? exponentScaler.MassExponent : 3d));
                ScaleDebug.Info(part, "cost", "prefab dry cost=" + ScaleDebug.Format(nativeCost) +
                    "; target=" + ScaleDebug.Format(targetCost) +
                    "; exponent=" + ScaleDebug.Format(exponentScaler != null ? exponentScaler.CostExponent : 3d));
            }
            bool crewCapacityChanged = false;
            if (exponentScaler != null)
            {
                exponentScaler.Apply(scaleX, scaleY, scaleZ);
                if (crewCapacityScaling != null)
                    crewCapacityChanged = crewCapacityScaling.Apply(scaleX, scaleY, scaleZ);
                UpdateCalculatedValues();
            }
            if (dockingPorts != null) dockingPorts.Apply(Target * BaseFactor,
                b9Support != null);
            if (faceMorph != null && faceMorph.Valid)
            {
                // PartSculptor.Apply first resets every node to its
                // conventional non-morphed position. Include the *extra*
                // morph displacement in the same existing neighbour-move
                // calculation, otherwise normal scale edits can embed pods.
                var unMorphed = new Dictionary<AttachNode, Vector3>();
                if (editor)
                {
                    foreach (AttachNode n in part.attachNodes)
                        if (n != null) unMorphed[n] = n.position;
                    if (part.srfAttachNode != null)
                        unMorphed[part.srfAttachNode] = part.srfAttachNode.position;
                }
                faceMorph.RebaseNodes();
                faceMorph.Apply(GetMorphFace(0), GetMorphFace(1), false);
                if (editor)
                {
                    foreach (KeyValuePair<AttachNode, Vector3> node in unMorphed)
                    {
                        if (node.Key == null || node.Key.attachedPart == null) continue;
                        Vector3 delta = node.Key.position - node.Value;
                        if (delta.sqrMagnitude < 1e-12f) continue;
                        if (node.Key.attachedPart == part.parent)
                            selfMove -= delta;
                        else if (moveChildren && node.Key.attachedPart.parent == part)
                            childMoves.Add(new KeyValuePair<Part, Vector3>(
                                node.Key.attachedPart, delta));
                    }
                }
            }

            if (!editor)
            {
                if (crewCapacityChanged) FireEditorShipModified();
                return;
            }

            // Children are transform-children of this part in the editor, so a
            // self move carries them along; child deltas are applied on top.
            if (selfMove.sqrMagnitude > 0f)
            {
                Vector3 oldPosition = part.transform.position;
                part.transform.position += part.transform.TransformVector(selfMove);
                ScaleDebug.Change(part, "position", "self world position",
                    oldPosition, part.transform.position, "attachment move");
            }
            foreach (var kv in childMoves)
            {
                Vector3 oldChildPosition = kv.Key.transform.position;
                kv.Key.transform.position += part.transform.TransformVector(kv.Value);
                ScaleDebug.Change(kv.Key, "position", "child world position",
                    oldChildPosition, kv.Key.transform.position,
                    "moved by parent " + part.name);
            }

            if (fireModified || crewCapacityChanged) FireEditorShipModified();
        }
    }
    // Canonical PartSculptor module for newly patched parts. Inheriting the
    // original implementation preserves all existing KSPFields and PAW events.
    // ModuleStretchScale remains loadable for pre-rename craft and save files.
    public class ModulePartSculptor : ModuleStretchScale
    {
    }

}
