// Per-user PartSculptor preferences and the Vessel Inspector settings dialog.
// Keeps user preferences outside the distributable Settings.cfg so upgrades
// do not reset them. Never writes settings to the craft's persistent data.
using System;
using System.IO;
using System.Globalization;
using ClickThroughFix;
using UnityEngine;

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
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[PartSculptor] Unable to read user settings: " + e);
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
            error = null;
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
                Debug.LogError("[PartSculptor] " + error);
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
        Rect windowRect = new Rect(180f, 160f, 510f, 398f);
        bool visible;
        bool matchNodeDefault;
        bool childrenDefault;
        bool lockDefault;
        bool morphLockDefault;
        int snapSelection;
        bool savePending;
        bool closePending;
        string status = "";

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
            snapSelection = 1;
            for (int i = 0; i < ScaleUserSettings.MorphSnapChoices.Length; ++i)
                if (Math.Abs(ScaleUserSettings.MorphSnapIncrement -
                    ScaleUserSettings.MorphSnapChoices[i]) < 0.00001f) snapSelection = i;
            status = "";
            savePending = false;
            closePending = false;
            visible = true;
            InspectorWindowPosition.KeepPartiallyVisible(ref windowRect);
        }

        internal void Close()
        {
            visible = false;
            savePending = false;
            closePending = false;
        }

        // Process clicks outside IMGUI so the window always has a stable
        // control tree throughout Layout/Repaint events.
        internal void ProcessPending()
        {
            if (closePending) { Close(); return; }
            if (!visible || !savePending) return;
            savePending = false;
            string error;
            if (ScaleUserSettings.SaveDefaults(matchNodeDefault, childrenDefault, lockDefault,
                morphLockDefault, ScaleUserSettings.MorphSnapChoices[snapSelection], out error))
                status = "Defaults saved. New parts will use the updated preferences.";
            else status = error;
        }

        internal void Draw()
        {
            if (!visible) return;
            windowRect = ClickThruBlocker.GUIWindow(WindowId, windowRect,
                DrawContents, "PartSculptor - Settings");
            InspectorWindowPosition.KeepPartiallyVisible(ref windowRect);
        }

        void DrawContents(int id)
        {
            GUILayout.Space(24f);
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
            GUILayout.Label("Match node sizes and Stretch children update now; " +
                "Lock proportions and Lock X/Z apply to new parts only.");
            GUILayout.FlexibleSpace();
            GUILayout.Label(status, GUILayout.MinHeight(32f));
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save", GUILayout.Width(110f))) savePending = true;
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close", GUILayout.Width(110f))) closePending = true;
            }
            GUI.DragWindow();
        }
    }
}
