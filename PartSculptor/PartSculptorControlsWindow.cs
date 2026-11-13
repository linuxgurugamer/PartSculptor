// A complete, scrollable alternative to the editor PartSculptor PAW group.
// All edits invoke the existing ModulePartSculptor methods: no second scaling,
// morphing, symmetry, attachment or undo implementation lives in this UI.
using System;
using System.Globalization;
using ClickThroughFix;
using UnityEngine;

namespace PartSculptor
{
    public partial class ModulePartSculptor
    {
        private static ModulePartSculptor controlsWindowOwner;
        private static Part controlsWindowSelectedPart;
        private static bool controlsWindowVisible;
        private static Rect controlsWindowRect = new Rect(180f, 105f, 560f, 655f);
        private static Vector2 controlsWindowScroll;
        private static readonly string[] controlsScaleText = new string[3];
        private static readonly bool[] controlsScaleDirty = new bool[3];
        private static string controlsTiltXText;
        private static string controlsTiltZText;
        private static bool controlsTiltDirty;
        private static string controlsSelectedPreset;
        private static bool controlsPresetDirty;
        private static string controlsLastTiltFace;
        private static string controlsStatus = "";

        internal static bool ControlsWindowVisible { get { return controlsWindowVisible; } }

        [KSPEvent(guiActiveEditor = true, guiName = "Show PartSculptor controls window",
            groupName = GROUP, groupDisplayName = GROUP_TITLE)]
        public void ToggleControlsWindow()
        {
            if (!initialized || !HighLogic.LoadedSceneIsEditor || part == null) return;
            if (controlsWindowVisible && controlsWindowOwner == this)
            {
                CloseControlsWindow();
                return;
            }
            controlsWindowVisible = true;
            SelectControlsWindowPart(part);
            controlsWindowRect.width = Mathf.Min(560f, Mathf.Max(280f, Screen.width - 16f));
            controlsWindowRect.height = Mathf.Min(655f, Mathf.Max(260f, Screen.height - 24f));
            InspectorWindowPosition.KeepPartiallyVisible(ref controlsWindowRect);
            EditorPartSculptorControlsSelection.RememberPAWSelection();
        }

        internal static bool controlsSelectionQueued;
        internal static Part queuedControlsPart;

        // Avoid changing a separate IMGUI window's widget count in the middle
        // of another window's Layout/Repaint pass.
        internal static void RequestControlsWindowPart(Part selected)
        {
            if (!controlsWindowVisible) return;
            queuedControlsPart = selected;
            controlsSelectionQueued = true;
            EditorPartSculptorControlsSelection.RememberPAWSelection();
        }

        // Selection is independent of the module which originally opened the
        // window. A selected part without PartSculptor displays a placeholder.
        internal static void SelectControlsWindowPart(Part selected)
        {
            if (!controlsWindowVisible || !HighLogic.LoadedSceneIsEditor) return;
            if (selected != null && EditorLogic.fetch != null &&
                EditorLogic.fetch.ship != null &&
                !EditorLogic.fetch.ship.parts.Contains(selected)) return;
            ModulePartSculptor next = selected != null
                ? selected.FindModuleImplementing<ModulePartSculptor>() : null;
            if (next != null && !next.initialized) next = null;
            if (selected == controlsWindowSelectedPart && next == controlsWindowOwner) return;
            ModulePartSculptor previous = controlsWindowOwner;
            controlsWindowOwner = next;
            controlsWindowSelectedPart = selected;
            controlsWindowScroll = Vector2.zero;
            controlsStatus = "";
            for (int i = 0; i < 3; ++i) controlsScaleDirty[i] = false;
            controlsTiltDirty = false;
            controlsPresetDirty = false;
            if (next != null) next.RefreshControlsWindowEdits(true);
            if (previous != null) previous.UpdateControlsWindowPAWLabel();
            if (next != null && next != previous) next.UpdateControlsWindowPAWLabel();
        }

        private void UpdateControlsWindowPAWLabel()
        {
            if (Events != null)
            {
                BaseEvent action = Events["ToggleControlsWindow"];
                if (action != null)
                    action.guiName = controlsWindowVisible && controlsWindowOwner == this
                        ? "Hide PartSculptor controls window"
                        : "Show PartSculptor controls window";
            }
            MarkNumericTiltPAWDirty();
        }

        private static void CloseControlsWindow()
        {
            ModulePartSculptor previous = controlsWindowOwner;
            controlsWindowOwner = null;
            controlsWindowSelectedPart = null;
            controlsWindowVisible = false;
            controlsSelectionQueued = false;
            queuedControlsPart = null;
            if (previous != null) previous.UpdateControlsWindowPAWLabel();
        }

        private static void CloseControlsWindowFor(ModulePartSculptor module)
        {
            if (controlsWindowOwner != module) return;
            CloseControlsWindow();
        }

        internal static void CloseControlsWindowOnEditorExit()
        {
            if (controlsWindowVisible) CloseControlsWindow();
        }

        private void RefreshControlsWindowEdits(bool force)
        {
            if (controlsWindowOwner != this) return;
            float[] axes = { scaleX, scaleY, scaleZ };
            for (int i = 0; i < 3; ++i)
                if (force || !controlsScaleDirty[i])
                    controlsScaleText[i] = axes[i].ToString("0.###", CultureInfo.InvariantCulture);
            if (force || !string.Equals(controlsLastTiltFace, numericTiltFace, StringComparison.Ordinal))
            {
                controlsTiltDirty = false;
                controlsLastTiltFace = numericTiltFace;
            }
            if (force || !controlsTiltDirty)
            {
                // Follow PAW's staged values if its float editors are in use.
                controlsTiltXText = numericTiltXDegrees.ToString("0.##", CultureInfo.InvariantCulture);
                controlsTiltZText = numericTiltZDegrees.ToString("0.##", CultureInfo.InvariantCulture);
            }
            if (force || !controlsPresetDirty)
                controlsSelectedPreset = scalePreset;
        }

        private static bool TryControlsFloat(string text, out float value)
        {
            if (!float.TryParse(text, NumberStyles.Float,
                CultureInfo.InvariantCulture, out value) &&
                !float.TryParse(text, NumberStyles.Float,
                    CultureInfo.CurrentCulture, out value)) return false;
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void DrawControlsLabel(string label, string value)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(160f));
                GUILayout.Label(value ?? "", GUILayout.ExpandWidth(true));
            }
        }

        private static void DrawControlsSection(string caption)
        {
            GUILayout.Space(9f);
            GUIStyle header = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            GUILayout.Label(caption, header);
        }

        private void ApplyControlsScale(int axis)
        {
            float requested;
            if (!TryControlsFloat(controlsScaleText[axis], out requested) ||
                requested < Settings.MinScale || requested > Settings.MaxScale)
            {
                controlsStatus = "Scale must be between " + Settings.MinScale +
                    " and " + Settings.MaxScale + ".";
                return;
            }
            Vector3 before = GizmoScale;
            float previous = axis == 0 ? before.x : axis == 1 ? before.y : before.z;
            if (Mathf.Abs(requested - previous) < 1e-5f)
            {
                controlsScaleDirty[axis] = false;
                controlsStatus = "Scale already has the requested value.";
                return;
            }
            EditorUndoSupport.RecordBeforeChange();
            SetGizmoAxis(axis, requested);
            lastX = scaleX; lastY = scaleY; lastZ = scaleZ;
            controlsScaleDirty[axis] = false;
            float actual = axis == 0 ? scaleX : axis == 1 ? scaleY : scaleZ;
            controlsStatus = Mathf.Abs(actual - requested) < 0.001f
                ? "Scale applied." : "Scale constrained by proportions or safety limits.";
            RefreshControlsWindowEdits(false);
        }

        private void SetControlsPerPartLock(bool morph, bool enabled)
        {
            if (morph ? lockMorphXZ == enabled : lockAxes == enabled) return;
            EditorUndoSupport.RecordBeforeChange();
            if (morph) lockMorphXZ = enabled;
            else lockAxes = enabled;
            // Both lock PAW fields have affectSymCounterparts = Editor.
            if (part != null)
                foreach (Part counterpart in part.symmetryCounterparts)
                {
                    ModulePartSculptor other = counterpart != null
                        ? counterpart.FindModuleImplementing<ModulePartSculptor>() : null;
                    if (other == null) continue;
                    if (morph) other.lockMorphXZ = enabled;
                    else other.lockAxes = enabled;
                    if (counterpart.PartActionWindow != null)
                        counterpart.PartActionWindow.displayDirty = true;
                }
            MarkNumericTiltPAWDirty();
            FireEditorShipModified();
        }

        private void DrawControlsSharedToggles()
        {
            bool choice = GUILayout.Toggle(scaleChildren, "Stretch children");
            if (choice != scaleChildren)
            {
                scaleChildren = choice;
                OnScaleChildrenModified(null);
                MarkNumericTiltPAWDirty();
            }
            choice = GUILayout.Toggle(matchNodeSizes, "Match node sizes");
            if (choice != matchNodeSizes)
            {
                matchNodeSizes = choice;
                OnMatchNodeSizesModified(null);
                MarkNumericTiltPAWDirty();
            }
            choice = GUILayout.Toggle(matchAttachedFaces, "Match attached faces");
            if (choice != matchAttachedFaces)
            {
                matchAttachedFaces = choice;
                OnMatchAttachedFacesModified(null);
                MarkNumericTiltPAWDirty();
            }
            choice = GUILayout.Toggle(copyAttachedMorphSteps, "Copy attached morph steps");
            if (choice != copyAttachedMorphSteps)
            {
                copyAttachedMorphSteps = choice;
                OnCopyAttachedMorphStepsModified(null);
                MarkNumericTiltPAWDirty();
            }
        }

        private void DrawControlsAxis(int axis, string caption)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(caption, GUILayout.Width(160f));
                string changed = GUILayout.TextField(controlsScaleText[axis] ?? "", 24,
                    GUILayout.Width(60));
                if (changed != controlsScaleText[axis])
                {
                    controlsScaleText[axis] = changed;
                    controlsScaleDirty[axis] = true;
                }
                if (GUILayout.Button("Set", GUILayout.Width(58f)))
                    ApplyControlsScale(axis);
            }
        }

        private void DrawControlsPresets()
        {
            string[] choices = ScalePresetRegistry.AbsoluteOptionNames();
            if (choices.Length == 0)
            {
                GUILayout.Label("No scale presets available.");
                return;
            }
            int index = Array.IndexOf(choices, controlsSelectedPreset);
            if (index < 0) index = 0;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Scale preset", GUILayout.Width(160f));
                if (GUILayout.Button("<", GUILayout.Width(28f)))
                {
                    index = (index + choices.Length - 1) % choices.Length;
                    controlsPresetDirty = true;
                }
                GUIStyle presetLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter
                };
                GUILayout.Label(controlsSelectedPreset ?? choices[index],
                    presetLabelStyle, GUILayout.Width(120f));
                if (GUILayout.Button(">", GUILayout.Width(28f)))
                {
                    index = (index + 1) % choices.Length;
                    controlsPresetDirty = true;
                }
                controlsSelectedPreset = choices[index];
                if (GUILayout.Button("Apply", GUILayout.Width(62f)))
                {
                    if (ScalePresetRegistry.Find(controlsSelectedPreset) != null &&
                        controlsSelectedPreset != "Custom")
                    {
                        EditorUndoSupport.RecordBeforeChange();
                        ApplyPreset(controlsSelectedPreset);
                        controlsPresetDirty = false;
                        RefreshControlsWindowEdits(true);
                        controlsStatus = "Preset applied: " + controlsSelectedPreset;
                    }
                }
            }
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Browse presets")) ShowPresets();
                if (GUILayout.Button("Repeat preset"))
                {
                    EditorUndoSupport.RecordBeforeChange();
                    controlsPresetDirty = false;
                    RepeatPreset();
                    RefreshControlsWindowEdits(true);
                }
            }
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset scale"))
                {
                    EditorUndoSupport.RecordBeforeChange();
                    controlsPresetDirty = false;
                    ResetScale();
                    RefreshControlsWindowEdits(true);
                }
                if (GUILayout.Button("Swap X / Z"))
                {
                    EditorUndoSupport.RecordBeforeChange();
                    controlsPresetDirty = false;
                    SwapXZ();
                    RefreshControlsWindowEdits(true);
                }
            }
        }

        private void DrawControlsNumericTilt()
        {
            DrawControlsLabel("Bottom face tilt", displayBottomFaceTilt);
            DrawControlsLabel("Top face tilt", displayTopFaceTilt);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Tilt face", GUILayout.Width(160f));
                int selected = numericTiltFace == "Bottom" ? 0 : 1;
                int next = GUILayout.SelectionGrid(selected, new[] { "Bottom", "Top" }, 2,
                    GUILayout.ExpandWidth(true));
                if (selected != next)
                {
                    numericTiltFace = next == 0 ? "Bottom" : "Top";
                    OnNumericTiltFaceChanged(null, null);
                    controlsTiltDirty = false;
                    controlsLastTiltFace = numericTiltFace;
                    RefreshControlsWindowEdits(false);
                }
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Tilt X (degrees)", GUILayout.Width(160f));
                string changed = GUILayout.TextField(controlsTiltXText ?? "", 24, GUILayout.Width(60));
                if (changed != controlsTiltXText)
                {
                    controlsTiltXText = changed;
                    controlsTiltDirty = true;
                }
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Tilt Z (degrees)", GUILayout.Width(160f));
                string changed = GUILayout.TextField(controlsTiltZText ?? "", 24, GUILayout.Width(60));
                if (changed != controlsTiltZText)
                {
                    controlsTiltZText = changed;
                    controlsTiltDirty = true;
                }
            }
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply face tilt"))
                {
                    float degreesX, degreesZ;
                    if (!TryControlsFloat(controlsTiltXText, out degreesX) ||
                        !TryControlsFloat(controlsTiltZText, out degreesZ) ||
                        Mathf.Abs(degreesX) >= 89f || Mathf.Abs(degreesZ) >= 89f)
                        controlsStatus = "Enter finite X/Z angles between -89 and 89 degrees.";
                    else
                    {
                        numericTiltXDegrees = degreesX;
                        numericTiltZDegrees = degreesZ;
                        ApplyNumericFaceTilt();
                        controlsStatus = numericTiltStatus;
                        if (controlsStatus.StartsWith("Applied ", StringComparison.Ordinal) ||
                            controlsStatus.StartsWith("Face already has", StringComparison.Ordinal))
                        {
                            controlsTiltDirty = false;
                            RefreshControlsWindowEdits(false);
                        }
                    }
                }
            }
            GUILayout.Label("Numeric tilt status: " +
                (controlsTiltDirty ? "Pending — click Apply face tilt" : numericTiltStatus));
        }

        private void DrawControlsActions()
        {
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(FaceMorphGizmo.IsFor(this)
                    ? "Hide face morph handles" : "Show face morph handles"))
                    ToggleFaceMorphGizmo();
                if (GUILayout.Button("Reset face morph"))
                {
                    EditorUndoSupport.RecordBeforeChange();
                    ResetFaceMorph();
                }
            }
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(ScaleGizmo.IsFor(this)
                    ? "Hide scaling gizmo" : "Show scaling gizmo"))
                    ToggleScalingGizmo();
                if (GUILayout.Button("Show scaled values")) ShowScaledValues();
            }
            if (GUILayout.Button("Match face to parent now"))
            {
                EditorUndoSupport.RecordBeforeChange();
                MatchFaceToParentNow();
            }
        }

        private void DrawControlsWindowContents(int id)
        {
            GUILayout.Space(25f);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Part:", GUILayout.ExpandWidth(false));
                // Use the active skin so the highlighted part name remains
                // legible with either of the PartSculptor window skins.
                GUIStyle partNameStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold
                };
                GUILayout.Label(part.partInfo != null ? part.partInfo.title : part.name,
                    partNameStyle, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Close", GUILayout.Width(66f)))
                    CloseControlsWindowFor(this);
            }
            bool hidePAW = GUILayout.Toggle(Settings.HidePartSculptorPAWControls,
                "Hide other PartSculptor PAW controls (keep window button)");
            if (hidePAW != Settings.HidePartSculptorPAWControls)
            {
                string preferenceError;
                if (!ScaleUserSettings.SaveHidePAWOnly(hidePAW, out preferenceError))
                    controlsStatus = preferenceError;
                else controlsStatus = hidePAW
                    ? "PartSculptor PAW controls hidden." : "PartSculptor PAW controls restored.";
            }
            controlsWindowScroll = GUILayout.BeginScrollView(controlsWindowScroll, false, true);
            bool mayScale = CanUseScaleGizmo;
            if (!mayScale && !string.IsNullOrEmpty(restrictedReason))
                GUILayout.Label("Scaling restricted: " + restrictedReason);

            DrawControlsSection("Scaling");
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && mayScale;
            DrawControlsAxis(0, "Scale X (width)");
            DrawControlsAxis(1, "Scale Y (height)");
            DrawControlsAxis(2, "Scale Z (depth)");
            bool choice = GUILayout.Toggle(lockAxes, "Lock proportions");
            if (choice != lockAxes) SetControlsPerPartLock(false, choice);
            GUI.enabled = enabled;

            DrawControlsSection("Attachment options");
            DrawControlsSharedToggles();

            DrawControlsSection("Morphing");
            choice = GUILayout.Toggle(lockMorphXZ, "Lock X/Z");
            if (choice != lockMorphXZ) SetControlsPerPartLock(true, choice);
            DrawControlsNumericTilt();

            DrawControlsSection("Calculated values");
            DrawControlsLabel("Volume multiplier", displayVolume);
            DrawControlsLabel("Dry mass", displayDryMass);
            DrawControlsLabel("Dry cost", displayDryCost);
            DrawControlsLabel("Tank capacity", displayCapacity);
            DrawControlsLabel("Scale safety", displayScaleSafety);
            DrawControlsLabel("Career scale limits", displayCareerLimits);

            DrawControlsSection("Presets");
            GUI.enabled = enabled && mayScale;
            DrawControlsPresets();
            GUI.enabled = enabled;

            DrawControlsSection("Actions");
            DrawControlsActions();
            if (!string.IsNullOrEmpty(controlsStatus))
            {
                GUILayout.Space(8f);
                GUILayout.Label(controlsStatus);
            }
            GUILayout.EndScrollView();
            // The whole window can be dragged from any area not consumed by
            // an interactive control. Register the drag region LAST, so text
            // fields, toggles, buttons, and scrollbars keep their input.
            GUI.DragWindow();
        }

        private static void DrawControlsWindowForSelectedPart(int id)
        {
            ModulePartSculptor module = controlsWindowOwner;
            if (module != null && module.part != null)
            {
                module.RefreshControlsWindowEdits(false);
                module.DrawControlsWindowContents(id);
                return;
            }
            GUILayout.Space(25f);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(controlsWindowSelectedPart == null
                    ? "Select a craft part to display its controls."
                    : "This part has no PartSculptor controls.", GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Close", GUILayout.Width(66f))) CloseControlsWindow();
            }
            // Also allow whole-window dragging for the no-selection placeholder.
            GUI.DragWindow();
        }

        internal static void DrawControlsWindow()
        {
            if (!controlsWindowVisible || !HighLogic.LoadedSceneIsEditor) return;
            controlsWindowRect.width = Mathf.Min(controlsWindowRect.width, Screen.width - 8f);
            controlsWindowRect.height = Mathf.Min(controlsWindowRect.height, Screen.height - 8f);
            InspectorWindowPosition.KeepPartiallyVisible(ref controlsWindowRect);
            GUISkin previousSkin = GUI.skin;
            try
            {
                GUI.skin = ScaleUserSettings.WindowSkin(previousSkin);
                // Stable ID: changing selected parts must not replace the IMGUI window.
                controlsWindowRect = ClickThruBlocker.GUIWindow(0x51439,
                    controlsWindowRect, DrawControlsWindowForSelectedPart,
                    "PartSculptor - All Controls");
                InspectorWindowPosition.KeepPartiallyVisible(ref controlsWindowRect);
            }
            finally { GUI.skin = previousSkin; }
        }
    }

    // A single editor-scene host, not an IMGUI window per PartModule.
    // A PAW opening and the stock editor's selected/held part both update the
    // controls window. The inspector also calls SelectControlsWindowPart.
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    internal sealed class EditorPartSculptorControlsSelection : MonoBehaviour
    {
        private static EditorPartSculptorControlsSelection instance;
        private Part lastNativeSelection;

        private void Start()
        {
            instance = this;
            lastNativeSelection = EditorLogic.fetch != null ? EditorLogic.SelectedPart : null;
            GameEvents.onPartActionUICreate.Add(OnPartActionUICreate);
        }

        internal static void RememberPAWSelection()
        {
            if (instance != null) instance.lastNativeSelection = EditorLogic.fetch != null
                ? EditorLogic.SelectedPart : null;
        }

        private void OnPartActionUICreate(Part selected)
        {
            lastNativeSelection = EditorLogic.fetch != null ? EditorLogic.SelectedPart : null;
            ModulePartSculptor.RequestControlsWindowPart(selected);
        }

        private void Update()
        {
            if (!ModulePartSculptor.ControlsWindowVisible) return;
            if (ModulePartSculptor.controlsSelectionQueued)
            {
                Part queued = ModulePartSculptor.queuedControlsPart;
                ModulePartSculptor.controlsSelectionQueued = false;
                ModulePartSculptor.queuedControlsPart = null;
                ModulePartSculptor.SelectControlsWindowPart(queued);
                return;
            }
            Part selected = EditorLogic.fetch != null ? EditorLogic.SelectedPart : null;
            if (selected != lastNativeSelection)
            {
                lastNativeSelection = selected;
                // Do not clear the last meaningful choice when KSP releases
                // a part from the mouse. Retain that part until another choice.
                if (selected != null)
                    ModulePartSculptor.SelectControlsWindowPart(selected);
            }
        }

        private void OnGUI()
        {
            ModulePartSculptor.DrawControlsWindow();
        }

        private void OnDestroy()
        {
            GameEvents.onPartActionUICreate.Remove(OnPartActionUICreate);
            if (instance == this) instance = null;
            ModulePartSculptor.CloseControlsWindowOnEditorExit();
        }
    }
}
