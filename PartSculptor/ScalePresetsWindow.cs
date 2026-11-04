// PAW-accessible preset browser for ALL absolute and relative definitions.
// The absolute-only quick selector is in the part action window.
using System;
using System.Collections.Generic;
using System.Globalization;
using ClickThroughFix;
using UnityEngine;

namespace PartSculptor
{
    internal static class ScalePresetsWindow
    {
        const int WindowId = 0x514341;
        static Rect window = new Rect(170f, 125f, 410f, 554f);
        static Vector2 scroll;
        static ModulePartSculptor owner;
        static List<ScalePresetDefinition> all = new List<ScalePresetDefinition>();
        static List<ScalePresetDefinition> visiblePresets = new List<ScalePresetDefinition>();
        static bool showAbsolute = false;   // Default to relative-only view.
        static bool showRelative = true;
        static bool? queuedShowAbsolute;
        static bool? queuedShowRelative;
        static string selectedKey;
        static string queuedSelection;
        static string queuedApply;
        static bool closeQueued;
        static string status = "";

        internal static void Toggle(ModulePartSculptor module)
        {
            if (module == null || !HighLogic.LoadedSceneIsEditor) return;
            if (owner == module) { CloseFor(module); return; }
            owner = module;
            selectedKey = ScalePresetRegistry.Key(module.LastAppliedPreset,
                module.LastAppliedRelative);
            queuedSelection = null;
            queuedApply = null;
            queuedShowAbsolute = null;
            queuedShowRelative = null;
            showAbsolute = false;
            showRelative = true;
            closeQueued = false;
            status = "Choose a preset, then click Apply.";
            Refresh();
            InspectorWindowPosition.KeepPartiallyVisible(ref window);
        }

        internal static void CloseFor(ModulePartSculptor module)
        {
            if (owner != module) return;
            owner = null;
            queuedSelection = null;
            queuedApply = null;
            queuedShowAbsolute = null;
            queuedShowRelative = null;
            closeQueued = false;
        }

        static void Refresh()
        {
            all = ScalePresetRegistry.All();
            visiblePresets.Clear();
            foreach (ScalePresetDefinition preset in all)
                if (preset.Relative ? showRelative : showAbsolute)
                    visiblePresets.Add(preset);
            bool found = false;
            foreach (ScalePresetDefinition preset in visiblePresets)
                if (string.Equals(ScalePresetRegistry.Key(preset), selectedKey,
                        StringComparison.OrdinalIgnoreCase)) { found = true; break; }
            if (!found)
                selectedKey = visiblePresets.Count > 0 ?
                    ScalePresetRegistry.Key(visiblePresets[0]) : null;
        }

        // Apply button actions in PartModule.Update, never while building an
        // IMGUI layout: CommitChange can fire editor events and rebuild PAWs.
        internal static void ProcessPendingFor(ModulePartSculptor module)
        {
            if (owner == null || owner != module) return;
            if (closeQueued) { CloseFor(module); return; }
            if (queuedSelection != null)
            {
                selectedKey = queuedSelection;
                queuedSelection = null;
            }
            if (queuedApply != null)
            {
                string key = queuedApply;
                queuedApply = null;
                ScalePresetDefinition choice = ScalePresetRegistry.FindByKey(key);
                if (choice != null)
                {
                    owner.ApplyNamedPresetFromEditor(choice.Name, choice.Relative);
                    status = "Applied " + choice.Name +
                        (choice.Relative ? " [relative]." : " [absolute].");
                }
            }
        }

        internal static void DrawFor(ModulePartSculptor module)
        {
            if (owner == null || owner != module || !HighLogic.LoadedSceneIsEditor)
                return;
            if (Event.current.type == EventType.Layout)
            {
                if (queuedShowAbsolute.HasValue)
                {
                    showAbsolute = queuedShowAbsolute.Value;
                    queuedShowAbsolute = null;
                }
                if (queuedShowRelative.HasValue)
                {
                    showRelative = queuedShowRelative.Value;
                    queuedShowRelative = null;
                }
                Refresh();
            }
            if (owner == null) return;
            GUISkin previous = GUI.skin;
            try
            {
                GUI.skin = ScaleUserSettings.WindowSkin(previous);
                window = ClickThruBlocker.GUIWindow(WindowId, window, DrawWindow,
                    "PartSculptor - Presets");
                InspectorWindowPosition.KeepPartiallyVisible(ref window);
            }
            finally { GUI.skin = previous; }
        }

        static string Axes(ScalePresetDefinition p)
        {
            return p.X.ToString("G5", CultureInfo.InvariantCulture) + " / " +
                p.Y.ToString("G5", CultureInfo.InvariantCulture) + " / " +
                p.Z.ToString("G5", CultureInfo.InvariantCulture);
        }

        static void DrawWindow(int id)
        {
            GUILayout.Space(24f);
            using (new GUILayout.HorizontalScope())
            {
                bool nextAbsolute = GUILayout.Toggle(showAbsolute, "");
                GUILayout.Label("Show absolute");
                GUILayout.FlexibleSpace();
                bool nextRelative = GUILayout.Toggle(showRelative, "");
                GUILayout.Label("Show relative");
                GUILayout.FlexibleSpace();
                // Do not mutate the visible list during Repaint: it changes
                // GUILayout control counts and causes Unity IMGUI exceptions.
                if (nextAbsolute != showAbsolute) queuedShowAbsolute = nextAbsolute;
                if (nextRelative != showRelative) queuedShowRelative = nextRelative;
            }
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            foreach (ScalePresetDefinition preset in visiblePresets)
            {
                bool selected = string.Equals(selectedKey,
                    ScalePresetRegistry.Key(preset), StringComparison.OrdinalIgnoreCase);
                string line = (selected ? "> " : "  ") + preset.Name +
                    "  [" + (preset.Relative ? "relative" : "absolute") + "]  " + Axes(preset);
                if (GUILayout.Button(line)) queuedSelection = ScalePresetRegistry.Key(preset);
            }
            if (visiblePresets.Count == 0)
                GUILayout.Label("No presets shown. Enable either filter above.");
            GUILayout.EndScrollView();
            ScalePresetDefinition current = visiblePresets.Count > 0 ?
                ScalePresetRegistry.FindByKey(selectedKey) : null;
            GUILayout.Label(current == null ? "No preset selected" :
                "Selected: " + current.Name +
                (current.Relative ? " (multiplies current scales)" : " (sets absolute scales)"));
            GUILayout.Label(status, GUILayout.MinHeight(24f));
            using (new GUILayout.HorizontalScope())
            {
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && current != null;
                if (GUILayout.Button("Apply selected", GUILayout.Width(155f)))
                    queuedApply = selectedKey;
                GUI.enabled = enabled;
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close", GUILayout.Width(105f))) closeQueued = true;
            }
            GUI.DragWindow();
        }
    }
}
