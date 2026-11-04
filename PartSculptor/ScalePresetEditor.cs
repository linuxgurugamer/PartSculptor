// In-game editor for per-axis scaling presets. One shared window is hosted by
// the part selected in the Vessel Inspector; definitions are shared by all parts.
using System;
using System.Collections.Generic;
using System.Globalization;
using ClickThroughFix;
using UnityEngine;

namespace PartSculptor
{
    internal static class ScalePresetEditor
    {
        const int WindowId = 0x514339;
        const float LeftWidth = 230f;
        static ModulePartSculptor owner;
        static Rect window = new Rect(160f, 130f, 755f, 465f);
        static Vector2 listScroll;
        static List<ScalePresetDefinition> snapshot = new List<ScalePresetDefinition>();
        static string selectedKey;
        static string pendingSelection;
        static string nameText = "";
        static string xText = "1", yText = "1", zText = "1";
        static bool relative;
        static bool creating;
        static string status = "";
        static string pendingAction;

        internal static void Toggle(ModulePartSculptor module)
        {
            if (owner == module) { CloseFor(module); return; }
            if (module == null || !HighLogic.LoadedSceneIsEditor) return;
            ScalePresetRegistry.EnsureLoaded();
            owner = module;
            selectedKey = ScalePresetRegistry.Key(module.LastAppliedPreset,
                module.LastAppliedRelative);
            if (ScalePresetRegistry.FindByKey(selectedKey) == null) selectedKey = null;
            if (selectedKey == null)
            {
                var definitions = ScalePresetRegistry.All();
                if (definitions.Count != 0)
                    selectedKey = ScalePresetRegistry.Key(definitions[0]);
            }
            LoadSelection();
            status = "Select a preset, or create one using the part's current size.";
            window.width = Mathf.Max(620, window.width);
            window.height = Mathf.Max(350, window.height);
            InspectorWindowPosition.KeepPartiallyVisible(ref window);
        }

        internal static void CloseFor(ModulePartSculptor module)
        {
            if (owner != module) return;
            owner = null;
            pendingAction = null;
            pendingSelection = null;
        }

        static string Number(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        static void LoadSelection()
        {
            ScalePresetDefinition p = ScalePresetRegistry.FindByKey(selectedKey);
            creating = p == null;
            nameText = p != null ? p.Name : "";
            xText = p != null ? Number(p.X) : "1";
            yText = p != null ? Number(p.Y) : "1";
            zText = p != null ? Number(p.Z) : "1";
            relative = p != null && p.Relative;
        }

        static void NewFromCurrent()
        {
            Vector3 current = owner.GizmoScale;
            selectedKey = null;
            creating = true;
            nameText = "";
            xText = Number(current.x);
            yText = Number(current.y);
            zText = Number(current.z);
            relative = false;
            status = "Enter a name, then Save custom preset.";
        }

        static void Duplicate()
        {
            ScalePresetDefinition original = ScalePresetRegistry.FindByKey(selectedKey);
            if (original == null) return;
            selectedKey = null;
            creating = true;
            nameText = original.Name + " copy";
            status = "Rename this copy, then Save custom preset.";
        }

        static void ProcessPending()
        {
            if (!string.IsNullOrEmpty(pendingSelection))
            {
                selectedKey = pendingSelection;
                pendingSelection = null;
                LoadSelection();
                status = "";
            }
            string action = pendingAction;
            pendingAction = null;
            if (action == null) return;
            if (action == "New") NewFromCurrent();
            else if (action == "Duplicate") Duplicate();
            else if (action == "Reload")
            {
                ScalePresetRegistry.Reload();
                ModulePartSculptor.RefreshPresetOptionsAll();
                if (ScalePresetRegistry.FindByKey(selectedKey) == null) selectedKey = null;
                LoadSelection();
                status = "Presets reloaded from GameData and PluginData.";
            }
            else if (action == "Save")
            {
                double x, y, z;
                if (!double.TryParse(xText, NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
                    !double.TryParse(yText, NumberStyles.Float, CultureInfo.InvariantCulture, out y) ||
                    !double.TryParse(zText, NumberStyles.Float, CultureInfo.InvariantCulture, out z))
                {
                    status = "Invalid number. Use values such as 1.5 or 2.0.";
                    return;
                }
                var item = new ScalePresetDefinition(nameText.Trim(), relative, x, y, z, false);
                string error;
                // A renamed custom preset replaces its previous record; a new
                // draft creates one. Built-in defaults can only be duplicated.
                ScalePresetDefinition previous = creating ? null :
                    ScalePresetRegistry.FindByKey(selectedKey);
                string oldName = previous != null ? previous.Name : null;
                bool oldRelative = previous != null && previous.Relative;
                if (ScalePresetRegistry.SaveCustom(item, oldName, oldRelative, out error))
                {
                    selectedKey = ScalePresetRegistry.Key(item);
                    creating = false;
                    status = "Saved to GameData/PartSculptor/PluginData/CustomPresets.cfg";
                }
                else status = error;
            }
            else if (action == "Delete")
            {
                string error = "";
                ScalePresetDefinition previous = ScalePresetRegistry.FindByKey(selectedKey);
                if (previous != null &&
                    ScalePresetRegistry.DeleteCustom(previous.Name, previous.Relative, out error))
                {
                    selectedKey = null;
                    LoadSelection();
                    status = "Custom preset deleted.";
                }
                else status = error;
            }
            else if (action == "Apply")
            {
                ScalePresetDefinition p = ScalePresetRegistry.FindByKey(selectedKey);
                if (p == null) status = "Save the custom preset before applying it.";
                else
                {
                    owner.ApplyNamedPresetFromEditor(p.Name, p.Relative);
                    status = "Applied " + p.Name + " using PartSculptor's normal safety checks.";
                }
            }
        }

        internal static void DrawFor(ModulePartSculptor module)
        {
            if (owner == null || owner != module || !HighLogic.LoadedSceneIsEditor) return;
            if (Event.current.type == EventType.Layout)
            {
                ProcessPending();
                snapshot = ScalePresetRegistry.All();
            }
            GUISkin previous = GUI.skin;
            try
            {
                GUI.skin = ScaleUserSettings.WindowSkin(previous);
                window = ClickThruBlocker.GUIWindow(WindowId, window, DrawWindow,
                    "PartSculptor - Preset Editor");
                InspectorWindowPosition.KeepPartiallyVisible(ref window);
            }
            finally { GUI.skin = previous; }
        }

        static void DrawWindow(int id)
        {
            GUILayout.Space(24f);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(LeftWidth));
            GUILayout.Label("Available presets");
            listScroll = GUILayout.BeginScrollView(listScroll, GUILayout.ExpandHeight(true));
            foreach (ScalePresetDefinition p in snapshot)
            {
                string key = ScalePresetRegistry.Key(p);
                string caption = p.Name + (p.Relative ? " [relative]" : " [absolute]") +
                    (p.BuiltIn ? "  [default]" : "");
                if (GUILayout.Button(caption) && selectedKey != key)
                    pendingSelection = key;
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("New from current scale")) pendingAction = "New";
            if (GUILayout.Button("Duplicate selected")) pendingAction = "Duplicate";
            if (GUILayout.Button("Reload files")) pendingAction = "Reload";
            GUILayout.EndVertical();
            GUILayout.Space(10f);
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            ScalePresetDefinition selected = ScalePresetRegistry.FindByKey(selectedKey);
            bool builtIn = selected != null && selected.BuiltIn && !creating;
            GUILayout.Label(builtIn ? "Built-in preset (read only)" : "Custom preset");
            if (builtIn) GUILayout.Label("Use Duplicate selected to make an editable copy.");
            bool oldEnabled = GUI.enabled;
            GUI.enabled = !builtIn;
            GUILayout.Label("Name");
            nameText = GUILayout.TextField(nameText, 64);
            relative = GUILayout.Toggle(relative, "Multiply current axes (relative preset)");
            GUILayout.Label(relative ? "Multipliers X / Y / Z" : "Absolute scales X / Y / Z");
            GUILayout.BeginHorizontal();
            GUILayout.Label("X", GUILayout.Width(18));
            xText = GUILayout.TextField(xText, GUILayout.MinWidth(60));
            GUILayout.Label("Y", GUILayout.Width(18));
            yText = GUILayout.TextField(yText, GUILayout.MinWidth(60));
            GUILayout.Label("Z", GUILayout.Width(18));
            zText = GUILayout.TextField(zText, GUILayout.MinWidth(60));
            GUILayout.EndHorizontal();
            GUILayout.Space(12f);
            if (GUILayout.Button("Save custom preset")) pendingAction = "Save";
            GUI.enabled = oldEnabled && selected != null && !builtIn && !creating;
            if (GUILayout.Button("Delete custom preset")) pendingAction = "Delete";
            GUI.enabled = oldEnabled && selected != null;
            if (GUILayout.Button("Apply selected to part")) pendingAction = "Apply";
            GUI.enabled = oldEnabled;
            GUILayout.FlexibleSpace();
            GUILayout.Label(status, GUILayout.MinHeight(36));
            if (GUILayout.Button("Close")) CloseFor(owner);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }
    }
}
