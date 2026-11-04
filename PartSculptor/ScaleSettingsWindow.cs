// Per-user PartSculptor preferences and the Vessel Inspector settings dialog.
// Keeps user preferences outside the distributable Settings.cfg so upgrades
// do not reset them. Never writes settings to the craft's persistent data.
using System;
using System.IO;
using System.Globalization;
using ClickThroughFix;
using UnityEngine;

using static PartSculptor.RegisterToolbar;

namespace PartSculptor
{
    internal static class ScaleUserSettings
    {
        const string NodeName = "PARTSCULPTOR_USER_SETTINGS";
        static bool loaded;
        static bool? matchNodeSizesDefault;
        static bool? scaleChildrenDefault;
        static bool? lockProportionsDefault;
        static bool? lockMorphXZDefault;
        static float morphSnapIncrement = 0.05f;
        static float morphTipOpacity = 1f;
        static bool useAlternateSkin;
        // Opacity applies only to Morph tip backgrounds; text stays fully readable.
        internal static float MorphTipOpacity { get { return morphTipOpacity; } }
        internal static bool UseAlternateSkin { get { return useAlternateSkin; } }
        internal static bool IsValidMorphTipOpacity(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) &&
                value >= 0.1f && value <= 1f;
        }
        // The default is KSP's skin; the alternative is the IMGUI skin already
        // active in the host. Never modify HighLogic.Skin itself.
        internal static GUISkin WindowSkin(GUISkin host)
        {
            return useAlternateSkin || HighLogic.Skin == null ? host : HighLogic.Skin;
        }
        internal static float MorphSnapIncrement { get { return morphSnapIncrement; } }
        internal static readonly float[] MorphSnapChoices = { 0.01f, 0.05f, 0.10f, 0.25f };
        internal static bool IsValidMorphSnap(float value)
        {
            foreach (float choice in MorphSnapChoices)
                if (Math.Abs(choice - value) < 0.00001f) return true;
            return false;
        }

        internal static string UserSettingsPath
        {
            get { return Path.Combine(KSPUtil.ApplicationRootPath,
                "GameData/PartSculptor/PluginData/UserSettings.cfg"); }
        }

        // The GameData option is the fallback, even when the file is absent or
        // malformed. Only a valid user preference can override it.
        internal static void Load(ref bool matchDefault, ref bool childrenDefault,
            ref bool lockDefault, ref bool morphLockDefault)
        {
            if (!loaded)
            {
                loaded = true;
                try
                {
                    if (File.Exists(UserSettingsPath))
                    {
                        ConfigNode root = ConfigNode.Load(UserSettingsPath);
                        if (root != null)
                        {
                            ConfigNode n = root.name == NodeName ? root : root.GetNode(NodeName);
                            if (n == null)
                                n = root.name == "PARTSCULPTOR_USER_SETTINGS" ? root :
                                    root.GetNode("PARTSCULPTOR_USER_SETTINGS");
                            if (n != null)
                            {
                                bool parsed;
                                if (bool.TryParse(n.GetValue("matchNodeSizesDefault"), out parsed))
                                    matchNodeSizesDefault = parsed;
                                if (bool.TryParse(n.GetValue("scaleChildrenDefault"), out parsed))
                                    scaleChildrenDefault = parsed;
                                if (bool.TryParse(n.GetValue("lockProportionsDefault"), out parsed))
                                    lockProportionsDefault = parsed;
                                if (bool.TryParse(n.GetValue("lockMorphXZDefault"), out parsed))
                                    lockMorphXZDefault = parsed;
                                float snap;
                                if (float.TryParse(n.GetValue("morphSnapIncrement"),
                                    NumberStyles.Float, CultureInfo.InvariantCulture, out snap) &&
                                    IsValidMorphSnap(snap)) morphSnapIncrement = snap;
                                float opacity;
                                if (float.TryParse(n.GetValue("morphTipOpacity"),
                                    NumberStyles.Float, CultureInfo.InvariantCulture, out opacity) &&
                                    IsValidMorphTipOpacity(opacity)) morphTipOpacity = opacity;
                                if (bool.TryParse(n.GetValue("useAlternateSkin"), out parsed))
                                    useAlternateSkin = parsed;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Warning("[PartSculptor] Unable to read user settings: " + e);
                }
            }
            if (matchNodeSizesDefault.HasValue)
                matchDefault = matchNodeSizesDefault.Value;
            if (scaleChildrenDefault.HasValue)
                childrenDefault = scaleChildrenDefault.Value;
            if (lockProportionsDefault.HasValue)
                lockDefault = lockProportionsDefault.Value;
            if (lockMorphXZDefault.HasValue)
                morphLockDefault = lockMorphXZDefault.Value;
        }

        // Keep the state from when Settings was opened, not from the most
        // recent Apply. Cancel can then roll back an Apply in the same session,
        // including restoring the original preferences file exactly.
        internal sealed class UserSettingsSnapshot
        {
            internal bool Loaded;
            internal bool? MatchNodeSizesDefault, ScaleChildrenDefault;
            internal bool? LockProportionsDefault, LockMorphXZDefault;
            internal float MorphSnapIncrement, MorphTipOpacity;
            internal bool UseAlternateSkin;
            internal bool FileExisted;
            internal byte[] FileContents;
        }

        internal static bool TryCaptureSnapshot(out UserSettingsSnapshot snapshot,
            out string error)
        {
            snapshot = null;
            error = null;
            try
            {
                string path = UserSettingsPath;
                var captured = new UserSettingsSnapshot
                {
                    Loaded = loaded,
                    MatchNodeSizesDefault = matchNodeSizesDefault,
                    ScaleChildrenDefault = scaleChildrenDefault,
                    LockProportionsDefault = lockProportionsDefault,
                    LockMorphXZDefault = lockMorphXZDefault,
                    MorphSnapIncrement = morphSnapIncrement,
                    MorphTipOpacity = morphTipOpacity,
                    UseAlternateSkin = useAlternateSkin,
                    FileExisted = File.Exists(path)
                };
                if (captured.FileExisted)
                    captured.FileContents = File.ReadAllBytes(path);
                snapshot = captured;
                return true;
            }
            catch (Exception e)
            {
                error = "Could not capture settings before opening: " + e.Message;
                Log.Error("[PartSculptor] " + error);
                return false;
            }
        }

        internal static bool RestoreSnapshot(UserSettingsSnapshot snapshot,
            bool appliedSinceOpening, out string error)
        {
            error = null;
            if (snapshot == null)
            {
                error = "No opening settings snapshot is available";
                return false;
            }
            string temporary = null;
            try
            {
                // Apply writes UserSettings.cfg immediately. On Cancel, undo
                // those writes too. Preserve the original bytes (and even the
                // original absence of a file) rather than saving new defaults.
                if (appliedSinceOpening)
                {
                    string path = UserSettingsPath;
                    if (snapshot.FileExisted)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        temporary = path + ".cancel.tmp";
                        File.WriteAllBytes(temporary, snapshot.FileContents);
                        if (File.Exists(path)) File.Replace(temporary, path, null);
                        else File.Move(temporary, path);
                    }
                    else if (File.Exists(path)) File.Delete(path);
                }

                // Apply also changed cached settings used by the editor. Put
                // those back to their values at window opening, not Apply time.
                loaded = snapshot.Loaded;
                matchNodeSizesDefault = snapshot.MatchNodeSizesDefault;
                scaleChildrenDefault = snapshot.ScaleChildrenDefault;
                lockProportionsDefault = snapshot.LockProportionsDefault;
                lockMorphXZDefault = snapshot.LockMorphXZDefault;
                morphSnapIncrement = snapshot.MorphSnapIncrement;
                morphTipOpacity = snapshot.MorphTipOpacity;
                useAlternateSkin = snapshot.UseAlternateSkin;
                return true;
            }
            catch (Exception e)
            {
                error = "Could not restore settings from when the window opened: " + e.Message;
                Log.Error("[PartSculptor] " + error);
                return false;
            }
            finally
            {
                if (temporary != null && File.Exists(temporary))
                    try { File.Delete(temporary); } catch { }
            }
        }

        internal static bool SaveDefaults(bool matchValue, bool childrenValue, bool lockValue, out string error)
        {
            return SaveDefaults(matchValue, childrenValue, lockValue,
                Settings.LockMorphXZDefault, morphSnapIncrement, out error);
        }

        internal static bool SaveDefaults(bool matchValue, bool childrenValue,
            bool lockValue, float snapValue, out string error)
        {
            return SaveDefaults(matchValue, childrenValue, lockValue,
                Settings.LockMorphXZDefault, snapValue, out error);
        }

        internal static bool SaveDefaults(bool matchValue, bool childrenValue,
            bool lockValue, bool morphLockValue, float snapValue, out string error)
        {
            return SaveDefaults(matchValue, childrenValue, lockValue, morphLockValue,
                snapValue, morphTipOpacity, useAlternateSkin, out error);
        }

        internal static bool SaveDefaults(bool matchValue, bool childrenValue,
            bool lockValue, bool morphLockValue, float snapValue, float opacityValue,
            bool alternateSkinValue, out string error)
        {
            error = null;
            if (!IsValidMorphTipOpacity(opacityValue))
            {
                error = "Morph tip opacity must be between 10% and 100%";
                return false;
            }
            if (!IsValidMorphSnap(snapValue))
            {
                error = "Unsupported morph snap increment";
                return false;
            }
            string temporary = null;
            try
            {
                string path = UserSettingsPath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var root = new ConfigNode();
                ConfigNode node = root.AddNode(NodeName);
                node.AddValue("matchNodeSizesDefault", matchValue);
                node.AddValue("scaleChildrenDefault", childrenValue);
                node.AddValue("lockProportionsDefault", lockValue);
                node.AddValue("lockMorphXZDefault", morphLockValue);
                node.AddValue("morphSnapIncrement", snapValue.ToString("0.00", CultureInfo.InvariantCulture));
                node.AddValue("morphTipOpacity", opacityValue.ToString("0.00", CultureInfo.InvariantCulture));
                node.AddValue("useAlternateSkin", alternateSkinValue);
                temporary = path + ".tmp";
                root.Save(temporary);
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);

                loaded = true;
                matchNodeSizesDefault = matchValue;
                scaleChildrenDefault = childrenValue;
                lockProportionsDefault = lockValue;
                lockMorphXZDefault = morphLockValue;
                morphSnapIncrement = snapValue;
                morphTipOpacity = opacityValue;
                useAlternateSkin = alternateSkinValue;
                Settings.MatchNodeSizesDefault = matchValue;
                Settings.ScaleChildrenDefault = childrenValue;
                Settings.LockProportionsDefault = lockValue;
                Settings.LockMorphXZDefault = morphLockValue;
                // Immediately update the shared editor option as well. Every
                // ModulePartSculptor PAW synchronizes from this flag in Update.
                ModulePartSculptor.MatchNodeSizesGlobal = matchValue;
                ModulePartSculptor.ScaleChildrenGlobal = childrenValue;
                return true;
            }
            catch (Exception e)
            {
                error = "Could not save PartSculptor settings: " + e.Message;
                Log.Error("[PartSculptor] " + error);
                return false;
            }
            finally
            {
                if (temporary != null && File.Exists(temporary))
                    try { File.Delete(temporary); } catch { }
            }
        }
    }

    internal sealed class ScaleSettingsWindow
    {
        const int WindowId = 0x515338;
        Rect windowRect = new Rect(180f, 130f, 510f, 560f);
        Vector2 optionsScroll;
        bool visible;
        bool matchNodeDefault;
        bool childrenDefault;
        bool lockDefault;
        bool morphLockDefault;
        int snapSelection;
        float tipOpacity;
        bool alternateSkin;
        bool savePending;
        bool applyPending;
        bool cancelPending;
        string status = "";

        // Cancel returns to the exact values from when the window opened,
        // even if Apply was clicked one or more times in this session.
        struct OpeningSettings
        {
            internal bool MatchNodes, StretchChildren, LockProportions, LockMorphXZ;
            internal int SnapSelection;
            internal float TipOpacity;
            internal bool AlternateSkin;
            // Session-level PAW toggles can differ from the default settings.
            internal bool ActiveMatchNodes, ActiveStretchChildren;
        }
        OpeningSettings opened;
        ScaleUserSettings.UserSettingsSnapshot openingPreferences;
        bool appliedSinceOpening;

        void CaptureOpeningSettings()
        {
            opened.MatchNodes = matchNodeDefault;
            opened.StretchChildren = childrenDefault;
            opened.LockProportions = lockDefault;
            opened.LockMorphXZ = morphLockDefault;
            opened.SnapSelection = snapSelection;
            opened.TipOpacity = tipOpacity;
            opened.AlternateSkin = alternateSkin;
            opened.ActiveMatchNodes = ModulePartSculptor.MatchNodeSizesGlobal;
            opened.ActiveStretchChildren = ModulePartSculptor.ScaleChildrenGlobal;
        }

        void RestoreOpeningSettings()
        {
            matchNodeDefault = opened.MatchNodes;
            childrenDefault = opened.StretchChildren;
            lockDefault = opened.LockProportions;
            morphLockDefault = opened.LockMorphXZ;
            snapSelection = opened.SnapSelection;
            tipOpacity = opened.TipOpacity;
            alternateSkin = opened.AlternateSkin;

            // Restore the original editor defaults and live PAW toggles,
            // including changes that Apply made while this dialog was open.
            Settings.MatchNodeSizesDefault = opened.MatchNodes;
            Settings.ScaleChildrenDefault = opened.StretchChildren;
            Settings.LockProportionsDefault = opened.LockProportions;
            Settings.LockMorphXZDefault = opened.LockMorphXZ;
            ModulePartSculptor.MatchNodeSizesGlobal = opened.ActiveMatchNodes;
            ModulePartSculptor.ScaleChildrenGlobal = opened.ActiveStretchChildren;
        }

        internal void Toggle()
        {
            if (visible) Close();
            else Open();
        }

        void Open()
        {
            Settings.Load();
            matchNodeDefault = Settings.MatchNodeSizesDefault;
            childrenDefault = Settings.ScaleChildrenDefault;
            lockDefault = Settings.LockProportionsDefault;
            morphLockDefault = Settings.LockMorphXZDefault;
            tipOpacity = ScaleUserSettings.MorphTipOpacity;
            alternateSkin = ScaleUserSettings.UseAlternateSkin;
            snapSelection = 1;
            for (int i = 0; i < ScaleUserSettings.MorphSnapChoices.Length; ++i)
                if (Math.Abs(ScaleUserSettings.MorphSnapIncrement -
                    ScaleUserSettings.MorphSnapChoices[i]) < 0.00001f) snapSelection = i;
            string snapshotError;
            ScaleUserSettings.UserSettingsSnapshot preferences;
            if (!ScaleUserSettings.TryCaptureSnapshot(out preferences, out snapshotError))
                return;
            openingPreferences = preferences;
            CaptureOpeningSettings();
            appliedSinceOpening = false;
            status = "";
            savePending = false;
            applyPending = false;
            cancelPending = false;
            optionsScroll = Vector2.zero;
            visible = true;
            ClampWindowToScreen();
        }

        internal void Close()
        {
            visible = false;
            savePending = false;
            applyPending = false;
            cancelPending = false;
        }

        // Process clicks outside IMGUI so the window always has a stable
        // control tree throughout Layout/Repaint events.
        internal void ProcessPending()
        {
            // Cancel always restores the opening snapshot, even following
            // multiple Apply clicks. Applied preferences are rolled back on disk.
            if (cancelPending)
            {
                cancelPending = false;
                string restoreError;
                if (!ScaleUserSettings.RestoreSnapshot(openingPreferences,
                    appliedSinceOpening, out restoreError))
                {
                    status = restoreError;
                    return; // Keep the window open if disk rollback failed.
                }
                RestoreOpeningSettings();
                Close();
                return;
            }
            if (!visible || (!savePending && !applyPending)) return;
            bool closeAfterSaving = savePending;
            savePending = false;
            applyPending = false;
            string error;
            if (ScaleUserSettings.SaveDefaults(matchNodeDefault, childrenDefault, lockDefault,
                morphLockDefault, ScaleUserSettings.MorphSnapChoices[snapSelection],
                tipOpacity, alternateSkin, out error))
            {
                appliedSinceOpening = true;
                if (closeAfterSaving) Close();
                else status = "Settings applied.";
            }
            else status = error;
        }

        // Keep the entire footer accessible, not just the title bar. Small
        // screens get a shorter, scrollable settings area instead of clipped
        // Save/Apply/Cancel buttons below the screen edge.
        void ClampWindowToScreen()
        {
            windowRect.width = Mathf.Min(510f, Mathf.Max(240f, Screen.width - 16f));
            windowRect.height = Mathf.Min(560f, Mathf.Max(180f, Screen.height - 36f));
            windowRect.x = Mathf.Clamp(windowRect.x, 0f,
                Mathf.Max(0f, Screen.width - windowRect.width));
            windowRect.y = Mathf.Clamp(windowRect.y, 0f,
                Mathf.Max(0f, Screen.height - windowRect.height));
        }

        internal void Draw()
        {
            if (!visible) return;
            ClampWindowToScreen();
            windowRect = ClickThruBlocker.GUIWindow(WindowId, windowRect,
                DrawContents, "PartSculptor - Settings");
            ClampWindowToScreen();
        }

        void DrawContents(int id)
        {
            GUILayout.Space(24f);
            // The footer is outside this scroll view, so all three buttons
            // remain reachable at any KSP UI scale or screen height.
            optionsScroll = GUILayout.BeginScrollView(optionsScroll, false, true,
                GUILayout.Height(Mathf.Max(75f, windowRect.height - 143f)));
            GUILayout.Label("Editor defaults", GUI.skin.label);
            matchNodeDefault = GUILayout.Toggle(matchNodeDefault,
                new GUIContent("Match node sizes by default",
                    "When enabled, newly attached stack parts automatically resize to match the connected node."));
            childrenDefault = GUILayout.Toggle(childrenDefault,
                new GUIContent("Stretch children by default",
                    "Sets the shared Stretch children option when editing begins."));
            lockDefault = GUILayout.Toggle(lockDefault,
                new GUIContent("Lock proportions by default",
                    "Used by new parts; saved craft part settings are preserved."));
            morphLockDefault = GUILayout.Toggle(morphLockDefault,
                new GUIContent("Lock X/Z by default",
                    "New parts preserve the selected face's X/Z proportions during ordinary Morph corner drags. " +
                    "Existing saved part choices are preserved."));
            GUILayout.Space(8f);
            GUILayout.Label(new GUIContent("Face morph snap increment",
                "Shift constrains resize adjustments; Shift+Alt translates the four corners together with this increment."));
            snapSelection = GUILayout.SelectionGrid(snapSelection,
                new[] { "0.01", "0.05", "0.10", "0.25" }, 4);
            GUILayout.Space(8f);
            GUILayout.Label(new GUIContent("Morph tip opacity: " +
                Mathf.RoundToInt(tipOpacity * 100f) + "%",
                "Opacity of Morph tip backgrounds; the text remains fully opaque."));
            tipOpacity = GUILayout.HorizontalSlider(tipOpacity, 0.1f, 1f);
            // Clamp/round slider precision before saving to make repeat saves stable.
            tipOpacity = Mathf.Round(Mathf.Clamp(tipOpacity, 0.1f, 1f) * 100f) / 100f;
            alternateSkin = GUILayout.Toggle(alternateSkin,
                new GUIContent("Use alternate skin",
                    "Use the host Unity IMGUI skin instead of the KSP skin in PartSculptor windows."));
            GUILayout.Label("Match node sizes and Stretch children update now; " +
                "Lock proportions and Lock X/Z apply to new parts only.");
            GUILayout.EndScrollView();
            GUILayout.Label(status, GUILayout.Height(24f));
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save", GUILayout.Height(28f))) savePending = true;
                if (GUILayout.Button("Apply", GUILayout.Height(28f))) applyPending = true;
                if (GUILayout.Button("Cancel", GUILayout.Height(28f))) cancelPending = true;
            }
            // Only the title bar drags the window; scroll and button clicks
            // remain independent of window movement.
            GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 23f));
        }
    }
}
