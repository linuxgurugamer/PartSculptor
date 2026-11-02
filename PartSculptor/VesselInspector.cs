// Two-pane, read-only inspector for the editor craft / active flight vessel.
// ToolbarController handles the stock and optional Blizzy toolbar button.
// ClickThroughBlocker prevents clicks through the window into the editor/flight scene.
using ClickThroughFix;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using ToolbarControl_NS;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PartSculptor
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    internal sealed class RegisterVesselInspectorToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(VesselInspectorHost.ModId, "PartSculptor");
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    internal sealed class EditorVesselInspector : VesselInspectorHost { }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    internal sealed class FlightVesselInspector : VesselInspectorHost { }

    // A single host exists in each supported scene, not one per part.
    internal abstract class VesselInspectorHost : MonoBehaviour
    {
        internal const string ModId = "PartSculptor_NS";
        const string ToolbarId = "PartSculptorVesselInspector";
        const float SideWidth = 270f;
        const float TitleGap = 24f;
        const float MinWindowWidth = 560f;
        const float MinWindowHeight = 350f;
        const float ResizeGripSize = 24f;

        const string LargeIcon = "PartSculptor/PluginData/Icons/inspector_38";
        const string SmallIcon = "PartSculptor/PluginData/Icons/inspector_24";

        readonly List<Part> parts = new List<Part>();
        Vector2 partScroll;
        Rect windowRect;
        // The grip is an IMGUI hot control, so dragging it cannot also drag
        // the window or click a part underneath. Dimensions are session-local.
        bool resizing;
        int resizeHotControlId;
        Vector2 resizeStartMouse;
        Vector2 resizeStartSize;
        Vector2 requestedSize;
        bool sizeChangePending;
        static ToolbarControl toolbarControl;
        bool visible;
        bool scaledPartsOnly = true; // Show only resized parts when the inspector first opens.
        // The hover highlight is temporary and restores the part's existing visual state.
        static readonly Color HoverPartColor = new Color(0.3f, 0.95f, 0.4f, 1f);
        Part hoveredPart;
        bool hoveredWasHighlighted;
        Part.HighlightType hoveredHighlightType;
        Color hoveredHighlightColor;
        // Inspect-only mode temporarily disables stock editor part pickup while
        // leaving our independent camera raycast active. Session-local setting.
        bool inspectOnlyMode;
        string inspectOnlyLockId;
        bool inspectOnlyLockHeld;
        bool warnedUnavailableLock;
        bool pendingScaledPartsOnly;
        bool filterChangeQueued;
        int totalPartCount;
        Part selectedPart;
        Part queuedPart;
        bool selectionQueued;
        // Open/close the preset editor in Update, outside any IMGUI event, so
        // its PartModule host does not gain/lose controls between passes.
        ModuleStretchScale queuedPresetEditorOwner;
        bool presetEditorToggleQueued;
        bool settingsToggleQueued;
        readonly ScaleSettingsWindow settingsWindow = new ScaleSettingsWindow();
        ScaledValuesWindow selectedReport = new ScaledValuesWindow();
        int instanceID;
        protected void Start()
        {
            instanceID = GetInstanceID() ^ 0x41553;
            // KSP fires this when a part action menu is created by right-clicking
            // a part. It complements raycast picking for parts with unusual colliders.
            GameEvents.onPartActionUICreate.Add(OnPartActionUICreate);
            if (toolbarControl == null)
            {
                try
                {
                    toolbarControl = gameObject.AddComponent<ToolbarControl>();
                    ApplicationLauncher.AppScenes scenes =
                        ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH |
                        ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW;

                    toolbarControl.AddToAllToolbars(Open, Hide, scenes, ModId, ToolbarId,
                        LargeIcon, SmallIcon, "Inspect PartSculptor values on every vessel part");
                }
                catch (Exception e)
                {
                    Debug.LogError("[PartSculptor] Unable to create ToolbarController button: " + e);
                }
            }
        }

        // Follow clicks on current-craft parts when the inspector is open.
        // In inspect-only mode KSP part-picking is locked separately, but the
        // raycast still selects the report. Apply the selection on IMGUI Layout.
        void Update()
        {
            if (settingsToggleQueued)
            {
                settingsToggleQueued = false;
                if (visible) settingsWindow.Toggle();
            }
            settingsWindow.ProcessPending();
            // Handle the button from the preceding OnGUI event before the next
            // Layout/Repaint cycle begins. A toggle during Layout can otherwise
            // desynchronize this window and the separate PartModule.OnGUI host.
            if (presetEditorToggleQueued)
            {
                ModuleStretchScale target = queuedPresetEditorOwner;
                presetEditorToggleQueued = false;
                queuedPresetEditorOwner = null;
                if (visible && HighLogic.LoadedSceneIsEditor &&
                    target != null && target.part != null && IsCurrentCraftPart(target.part))
                    ScalePresetEditor.Toggle(target);
            }
            SyncInspectOnlyLock();
            if (!visible || (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight))
                return;
            // Inspect-only selection uses left click. Right clicks in normal
            // mode still follow the PAW event and the existing scene raycast.
            if (!Input.GetMouseButtonDown(0) &&
                (inspectOnlyMode || !Input.GetMouseButtonDown(1)))
                return;
            if (HighLogic.LoadedSceneIsFlight && MapView.MapIsEnabled)
                return;

            Vector3 mouse = Input.mousePosition;
            if (mouse.x < 0f || mouse.y < 0f || mouse.x >= Screen.width || mouse.y >= Screen.height)
                return;
            // IMGUI coordinates have their origin at the top-left; mouse positions
            // from Input have their origin at the bottom-left.
            if (windowRect.Contains(new Vector2(mouse.x, Screen.height - mouse.y)))
                return;
            // Don't treat clicks on the stock UI, PAWs or part palette as craft clicks.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            // EditorLogic.mouseOverGUI is not available in KSP 1.12.x.
            // The EventSystem check above and our own window bounds check
            // guard UI clicks without relying on that removed member.

            Part clicked = PickCurrentCraftPart(mouse);
            if (clicked == null) return;

            QueueSceneSelection(clicked);
        }

        void OnPartActionUICreate(Part part)
        {
            // A stock PAW click isn't needed for inspect-only selection;
            // avoid replacing the report with an unrelated PAW selection.
            if (visible && !inspectOnlyMode && IsCurrentCraftPart(part))
                QueueSceneSelection(part);
        }

        // Only the native editor picking action is locked; the inspector keeps
        // receiving input and ClickThroughBlocker still handles window clicks.
        // Resolve control names at runtime for compatibility with KSP builds
        // where particular input lock flags may be unavailable.
        static ControlTypes GetNamedControl(string name)
        {
            ControlTypes result;
            return Enum.TryParse(name, out result) && Enum.IsDefined(typeof(ControlTypes), result)
                ? result : (ControlTypes)0;
        }

        ControlTypes InspectOnlyControlMask()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                ControlTypes specific = GetNamedControl("EDITOR_PAD_PICK_PLACE");
                if (specific != (ControlTypes)0) return specific;
                // Some builds expose the aggregate editor lock but not the
                // granular pickup lock. This fallback is more restrictive.
                return GetNamedControl("EDITOR_LOCK");
            }
            return (ControlTypes)0;
        }

        void SyncInspectOnlyLock()
        {
            // Inspect-only is an editor feature. Flight always uses normal
            // part clicks, and must never retain an editor input lock.
            if (!HighLogic.LoadedSceneIsEditor)
            {
                inspectOnlyMode = false;
                ReleaseInspectOnlyLock();
                return;
            }
            bool wanted = visible && inspectOnlyMode;
            if (!wanted)
            {
                ReleaseInspectOnlyLock();
                return;
            }
            if (inspectOnlyLockHeld) return;
            if (inspectOnlyLockId == null)
                inspectOnlyLockId = "PartSculptorInspector_InspectOnly_" + GetInstanceID();
            ControlTypes mask = InspectOnlyControlMask();
            if (mask == (ControlTypes)0)
            {
                if (!warnedUnavailableLock)
                {
                    warnedUnavailableLock = true;
                    Debug.LogWarning("[PartSculptor] Inspect-only: KSP input control mask unavailable in this scene; stock part actions may still occur.");
                }
                return;
            }
            InputLockManager.SetControlLock(mask, inspectOnlyLockId);
            inspectOnlyLockHeld = true;
        }

        void ReleaseInspectOnlyLock()
        {
            if (!inspectOnlyLockHeld) return;
            InputLockManager.RemoveControlLock(inspectOnlyLockId);
            inspectOnlyLockHeld = false;
        }

        void QueueSceneSelection(Part clicked)
        {
            // An unscaled part is deliberately selectable even if the inspector
            // was filtered. Clear the filter so the clicked part is visible on
            // the left, and its report is available in the right pane.
            if (scaledPartsOnly && !HasChangedScale(clicked))
            {
                pendingScaledPartsOnly = false;
                filterChangeQueued = true;
            }
            queuedPart = clicked;
            selectionQueued = true;
        }

        bool IsCurrentCraftPart(Part part)
        {
            if (part == null) return false;
            if (HighLogic.LoadedSceneIsEditor)
            {
                ShipConstruct ship = EditorLogic.fetch != null ? EditorLogic.fetch.ship : null;
                return ship != null && ship.parts != null && ship.parts.Contains(part);
            }
            Vessel vessel = FlightGlobals.ActiveVessel;
            return vessel != null && vessel.parts != null && vessel.parts.Contains(part);
        }

        Part RaycastCraftPart(Camera camera, Vector3 mouse)
        {
            if (camera == null || !camera.enabled || !camera.pixelRect.Contains(new Vector2(mouse.x, mouse.y)))
                return null;

            Ray ray = camera.ScreenPointToRay(mouse);
            RaycastHit[] hits = Physics.RaycastAll(ray, camera.farClipPlane);
            Part nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.distance >= nearestDistance) continue;
                Part part = hit.collider.GetComponentInParent<Part>();
                if (!IsCurrentCraftPart(part)) continue;
                nearest = part;
                nearestDistance = hit.distance;
            }
            return nearest;
        }

        Part PickCurrentCraftPart(Vector3 mouse)
        {
            // Prefer KSP's active camera over Camera.main (which can resolve to
            // an unrelated camera in modded games). Try fallbacks for editor
            // camera rigs whose Camera component is on a child GameObject.
            Camera preferred = null;
            if (HighLogic.LoadedSceneIsFlight && FlightCamera.fetch != null)
                preferred = FlightCamera.fetch.mainCamera;
            else if (HighLogic.LoadedSceneIsEditor && EditorDriver.fetch != null)
            {
                if (EditorDriver.fetch.vabCamera != null && EditorDriver.fetch.vabCamera.enabled)
                    preferred = EditorDriver.fetch.vabCamera.GetComponentInChildren<Camera>();
                if (preferred == null && EditorDriver.fetch.sphCamera != null && EditorDriver.fetch.sphCamera.enabled)
                    preferred = EditorDriver.fetch.sphCamera.GetComponentInChildren<Camera>();
            }

            Part found = RaycastCraftPart(preferred, mouse);
            if (found != null) return found;
            Camera main = Camera.main;
            if (main != preferred)
            {
                found = RaycastCraftPart(main, mouse);
                if (found != null) return found;
            }
            foreach (Camera camera in Camera.allCameras)
            {
                if (camera == preferred || camera == main) continue;
                found = RaycastCraftPart(camera, mouse);
                if (found != null) return found;
            }
            return null;
        }

        // Highlight only the row under the pointer, not the currently selected
        // report part. The saved state matters because KSP and other mods may
        // already be highlighting a part for their own reasons.
        void SetHoveredPart(Part part)
        {
            if (hoveredPart == part) return;
            ClearHoverHighlight();
            if (!visible || part == null || !IsCurrentCraftPart(part)) return;

            try
            {
                hoveredWasHighlighted = part.HighlightActive;
                hoveredHighlightType = part.highlightType;
                hoveredHighlightColor = part.highlightColor;
                hoveredPart = part;
                part.SetHighlightType(Part.HighlightType.AlwaysOn);
                part.SetHighlightColor(HoverPartColor);
                part.SetHighlight(true, false); // Just this part, not its children.
            }
            catch (Exception e)
            {
                Debug.LogWarning("[PartSculptor] Could not highlight hovered part: " + e.Message);
                ClearHoverHighlight();
            }
        }

        void ClearHoverHighlight()
        {
            Part oldPart = hoveredPart;
            hoveredPart = null;
            if (oldPart == null) return; // The part may have been destroyed.

            try
            {
                oldPart.SetHighlightType(hoveredHighlightType);
                oldPart.SetHighlightColor(hoveredHighlightColor);
                oldPart.SetHighlight(hoveredWasHighlighted, false);
            }
            catch (Exception)
            {
                // A part can be destroyed while it is hovered, e.g. on undo,
                // vessel switch, docking, or scene shutdown. Nothing to restore.
            }
        }

        void Open()
        {
            float w = Mathf.Min(1130f, Mathf.Max(420f, Screen.width - 24f));
            float h = Mathf.Min(690f, Mathf.Max(280f, Screen.height - 48f));
            if (windowRect.width < 10f)
                windowRect = new Rect((Screen.width - w) * 0.5f,
                    (Screen.height - h) * 0.5f, w, h);
            // Retain any user-resized width and height when reopening. The
            // initial size is only used for the first opening in this scene.
            ClampWindowSize();
            InspectorWindowPosition.KeepPartiallyVisible(ref windowRect);
            visible = true;
            SyncInspectOnlyLock();
        }

        void Hide()
        {
            ClearHoverHighlight();
            CancelResize();
            visible = false;
            ReleaseInspectOnlyLock();
            inspectOnlyMode = false; // A reopened inspector starts in normal edit mode.
            selectionQueued = false;
            queuedPart = null;
            queuedPresetEditorOwner = null;
            presetEditorToggleQueued = false;
            settingsToggleQueued = false;
            settingsWindow.Close();
        }

        void CloseFromWindow()
        {
            Hide();
            if (toolbarControl != null)
                toolbarControl.SetFalse();
        }

        protected void OnDisable()
        {
            ClearHoverHighlight();
            CancelResize();
            ReleaseInspectOnlyLock();
            settingsWindow.Close();
        }

        protected void OnDestroy()
        {
            ClearHoverHighlight();
            CancelResize();
            ReleaseInspectOnlyLock();
            GameEvents.onPartActionUICreate.Remove(OnPartActionUICreate);
            visible = false;
            parts.Clear();
            selectedPart = null;
            queuedPart = null;
            selectionQueued = false;
            queuedPresetEditorOwner = null;
            presetEditorToggleQueued = false;
            settingsToggleQueued = false;
            settingsWindow.Close();
        }

        // Work on a stable snapshot per Layout/Repaint group. Unity IMGUI
        // throws control-count exceptions if the part list or selected panel
        // changes on MouseUp/Repaint rather than at the next Layout event.
        // A part is scaled only if at least one stretch axis differs from its
        // unscaled (1x) value. Merely having ModuleStretchScale is insufficient.
        static bool HasChangedScale(Part part)
        {
            if (part == null) return false;
            ModuleStretchScale scale = part.FindModuleImplementing<ModuleStretchScale>();
            if (scale == null) return false;
            const float epsilon = 0.0001f;
            return Mathf.Abs(scale.scaleX - 1f) > epsilon ||
                   Mathf.Abs(scale.scaleY - 1f) > epsilon ||
                   Mathf.Abs(scale.scaleZ - 1f) > epsilon;
        }

        // Refresh data only during Layout so the left list and the right report
        // do not change their GUILayout control counts on MouseUp or Repaint.
        void RefreshOnLayout()
        {
            if (filterChangeQueued)
            {
                scaledPartsOnly = pendingScaledPartsOnly;
                filterChangeQueued = false;
            }

            parts.Clear();
            totalPartCount = 0;
            if (HighLogic.LoadedSceneIsEditor)
            {
                ShipConstruct ship = EditorLogic.fetch != null ? EditorLogic.fetch.ship : null;
                if (ship != null && ship.parts != null)
                    foreach (Part p in ship.parts) AddToList(p);
            }
            else
            {
                Vessel vessel = FlightGlobals.ActiveVessel;
                if (vessel != null && vessel.parts != null)
                    foreach (Part p in vessel.parts) AddToList(p);
            }

            // Release a highlight immediately if its part is removed or hidden.
            if (hoveredPart != null && !parts.Contains(hoveredPart))
                ClearHoverHighlight();

            if (selectionQueued)
            {
                // The requested part may have disappeared or become filtered.
                Select(parts.Contains(queuedPart) ? queuedPart : null);
                selectionQueued = false;
                queuedPart = null;
            }

            if (selectedPart == null || !parts.Contains(selectedPart))
            {
                Part preferred = null;
                foreach (Part p in parts)
                {
                    if (p.FindModuleImplementing<ModuleStretchScale>() != null)
                    {
                        preferred = p;
                        break;
                    }
                }
                Select(preferred ?? (parts.Count > 0 ? parts[0] : null));
            }
        }

        void AddToList(Part part)
        {
            if (part == null) return;
            ++totalPartCount;
            if (!scaledPartsOnly || HasChangedScale(part)) parts.Add(part);
        }

        void Select(Part part)
        {
            if (selectedPart == part) return;
            selectedPart = part;
            // Independent report state for the selected part; PAW windows are
            // untouched, and the report never scales or mutates the part.
            selectedReport = new ScaledValuesWindow();
        }

        // Window dimensions can be adjusted independently; they are never
        // derived from a previous GUILayout-reported size. This also keeps
        // the left and right panes within a usable minimum.
        void ClampWindowSize()
        {
            float maxWidth = Mathf.Max(MinWindowWidth, Screen.width - 12f);
            float maxHeight = Mathf.Max(MinWindowHeight, Screen.height - 12f);
            windowRect.width = Mathf.Clamp(windowRect.width, MinWindowWidth, maxWidth);
            windowRect.height = Mathf.Clamp(windowRect.height, MinWindowHeight, maxHeight);
        }

        void CancelResize()
        {
            if (resizeHotControlId != 0 && GUIUtility.hotControl == resizeHotControlId)
                GUIUtility.hotControl = 0;
            resizeHotControlId = 0;
            resizing = false;
            sizeChangePending = false;
        }

        // The handle uses an explicit hot control, instead of GUI.DragWindow,
        // to keep the bottom-right corner fixed to the resize gesture while
        // other areas of the window retain normal drag-to-move behavior.
        void DrawResizeGrip()
        {
            Rect grip = new Rect(windowRect.width - ResizeGripSize - 3f,
                windowRect.height - ResizeGripSize - 3f,
                ResizeGripSize, ResizeGripSize);
            int controlId = GUIUtility.GetControlID(FocusType.Passive, grip);
            Event e = Event.current;
            switch (e.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && grip.Contains(e.mousePosition))
                    {
                        resizing = true;
                        resizeStartMouse = e.mousePosition;
                        resizeStartSize = new Vector2(windowRect.width, windowRect.height);
                        requestedSize = resizeStartSize;
                        GUIUtility.hotControl = controlId;
                        resizeHotControlId = controlId;
                        e.Use(); // Do not also start GUI.DragWindow().
                    }
                    break;
                case EventType.MouseDrag:
                    if (resizing && GUIUtility.hotControl == controlId)
                    {
                        Vector2 delta = e.mousePosition - resizeStartMouse;
                        requestedSize.x = Mathf.Clamp(resizeStartSize.x + delta.x,
                            MinWindowWidth, Mathf.Max(MinWindowWidth, Screen.width - 12f));
                        requestedSize.y = Mathf.Clamp(resizeStartSize.y + delta.y,
                            MinWindowHeight, Mathf.Max(MinWindowHeight, Screen.height - 12f));
                        sizeChangePending = true;
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId && e.button == 0)
                    {
                        CancelResize();
                        e.Use();
                    }
                    break;
                case EventType.Layout:
                    // Recover if the release happened outside the window.
                    if (resizing && !Input.GetMouseButton(0)) CancelResize();
                    break;
            }

            // No GUILayout controls are introduced or removed on hover.
            // The slashes remain visible even when the right pane is scrolled.
            GUI.Box(grip, new GUIContent("///", "Drag to resize"), GUI.skin.box);
        }

        protected void OnGUI()
        {
            if (!visible || (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight))
            {
                ClearHoverHighlight();
                return;
            }
            ClampWindowSize();
            InspectorWindowPosition.KeepPartiallyVisible(ref windowRect);
            Rect beforeWindow = windowRect;

            GUISkin previous = GUI.skin;
            try
            {
                GUI.skin = HighLogic.Skin;
                windowRect = ClickThruBlocker.GUILayoutWindow(instanceID,
                    windowRect, DrawWindow, "PartSculptor - Vessel Inspector");
                // Settings is a separate ClickThroughBlocker window layered over
                // the inspector; it does not depend on a selected vessel part.
                settingsWindow.Draw();
                // GUIWindow returns the rectangle passed to it. Apply mouse
                // drag resize after the callback so that result cannot reset
                // the new width/height until the next IMGUI layout pass.
                if (sizeChangePending)
                {
                    windowRect.x = beforeWindow.x;
                    windowRect.y = beforeWindow.y;
                    windowRect.width = requestedSize.x;
                    windowRect.height = requestedSize.y;
                    sizeChangePending = false;
                }
                ClampWindowSize();
                InspectorWindowPosition.KeepPartiallyVisible(ref windowRect);
            }
            finally { GUI.skin = previous; }
        }

        static string DisplayName(Part part)
        {
            if (part == null) return "(missing part)";
            return part.partInfo != null && !string.IsNullOrEmpty(part.partInfo.title)
                ? part.partInfo.title : part.name;
        }

        void DrawWindow(int id)
        {
            if (Event.current.type == EventType.Layout) RefreshOnLayout();
            // Use Repaint geometry only: GetLastRect() is not reliable on Layout.
            bool detectHover = Event.current.type == EventType.Repaint;
            Part partUnderMouse = null;
            GUILayout.Space(TitleGap);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(HighLogic.LoadedSceneIsEditor ? "Current craft" : "Active vessel",
                    GUILayout.ExpandWidth(true));
                GUILayout.Label(scaledPartsOnly ?
                    (parts.Count + " / " + totalPartCount + " parts") :
                    (parts.Count + " parts"), GUILayout.Width(108f));
                // Preset editing is an editor-only action, acting on the part
                // currently selected in the inspector rather than a PAW event.
                if (HighLogic.LoadedSceneIsEditor)
                {
                    ModuleStretchScale target = selectedPart != null
                        ? selectedPart.FindModuleImplementing<ModuleStretchScale>()
                        : null;
                    bool previousEnabled = GUI.enabled;
                    GUI.enabled = previousEnabled && target != null;
                    if (GUILayout.Button(new GUIContent("Edit presets",
                            "Open the preset editor for the selected PartSculptor part"),
                            GUILayout.Width(112f)))
                    {
                        queuedPresetEditorOwner = target;
                        presetEditorToggleQueued = true;
                    }
                    GUI.enabled = previousEnabled;
                }
                if (GUILayout.Button(new GUIContent("Settings",
                    "Configure PartSculptor defaults"), GUILayout.Width(85f)))
                    settingsToggleQueued = true;
                if (GUILayout.Button("Close", GUILayout.Width(70f))) CloseFromWindow();
            }

            float leftWidth = Mathf.Min(SideWidth, Mathf.Max(115f, windowRect.width * 0.28f));
            float rightWidth = Mathf.Max(180f, windowRect.width - leftWidth - 35f);
            using (new GUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
            {

                // Left: every part (including ones without ModuleStretchScale).
                using (new GUILayout.VerticalScope(GUI.skin.box, GUILayout.Width(leftWidth),
                    GUILayout.ExpandHeight(true)))
                {
                    GUILayout.Label("Vessel parts", GUI.skin.label);
                    bool requestedFilter = GUILayout.Toggle(scaledPartsOnly,
                        "Scaled parts only");
                    if (requestedFilter != scaledPartsOnly)
                    {
                        pendingScaledPartsOnly = requestedFilter;
                        filterChangeQueued = true;
                    }
                    // Hiding the toggle in flight preserves the left-pane layout and
                    // avoids offering an editor-only pickup lock in flight scenes.
                    if (HighLogic.LoadedSceneIsEditor)
                    {
                        bool inspectOnlyRequested = GUILayout.Toggle(inspectOnlyMode,
                            new GUIContent("Selection mode (inspect only)",
                                "Click a part to inspect it without picking it up in the editor. " +
                                "Turn off to restore normal part interactions."));
                        if (inspectOnlyRequested != inspectOnlyMode)
                        {
                            inspectOnlyMode = inspectOnlyRequested;
                            warnedUnavailableLock = false;
                            SyncInspectOnlyLock();
                        }
                    }
                    partScroll = GUILayout.BeginScrollView(partScroll, false, true,
                        GUILayout.ExpandHeight(true));
                    if (parts.Count == 0)
                        GUILayout.Label(scaledPartsOnly && totalPartCount > 0
                            ? "No parts have been scaled."
                            : "No craft or vessel loaded.");
                    foreach (Part part in parts)
                    {
                        bool hasScale = part.FindModuleImplementing<ModuleStretchScale>() != null;
                        string title = DisplayName(part);
                        string note = hasScale ? "" : " (no PartSculptor)";
                        GUIStyle buttonStyle = part == selectedPart ? GUI.skin.box : GUI.skin.button;
                        if (GUILayout.Button(new GUIContent(title + note,
                                part.name + (hasScale ? "" : "\nThis part has no ModuleStretchScale")),
                                buttonStyle, GUILayout.ExpandWidth(true)))
                        {
                            // Defer the selection so the right pane has the same
                            // control tree during all passes of this event.
                            queuedPart = part;
                            selectionQueued = true;
                        }
                        // Rect is in the current scroll-view clip space, as is
                        // Event.current.mousePosition. No new GUILayout controls
                        // are introduced on hover, avoiding Layout/Repaint errors.
                        if (detectHover && GUILayoutUtility.GetLastRect().Contains(
                                Event.current.mousePosition))
                            partUnderMouse = part;
                    }
                    GUILayout.EndScrollView();
                }

                // Right: the existing scaled-values report, not a log or a copy
                // of its scaling calculations.
                using (new GUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandHeight(true)))
                {
                    if (selectedPart == null)
                        GUILayout.Label("Select a part on the left.");
                    else
                    {
                        ModuleStretchScale module = selectedPart.FindModuleImplementing<ModuleStretchScale>();
                        if (module == null)
                        {
                            GUILayout.Label(DisplayName(selectedPart));
                            GUILayout.Label("This part does not contain ModuleStretchScale. " +
                                "No PartSculptor changes are available to inspect.");
                        }
                        else
                            selectedReport.DrawEmbedded(module, rightWidth,
                                windowRect.width, windowRect.height);
                    }
                }
            }

            if (detectHover)
                SetHoveredPart(partUnderMouse); // Clears on exit or over empty space.

            // Keep the resize grip clear of scrollbars and bottom-most data.
            GUILayout.Space(ResizeGripSize + 3f);
            DrawResizeGrip();
            // Render after leaving BOTH pane scopes so the tooltip is not
            // clipped by the right pane and can use the whole window.
            selectedReport.DrawTooltipOverlay(windowRect.width, windowRect.height);
            // Dragging still works from any other non-interactive area.
            GUI.DragWindow();
        }
    }
}
