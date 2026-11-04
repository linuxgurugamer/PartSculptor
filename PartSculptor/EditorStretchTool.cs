// Fifth and sixth VAB/SPH editor tools: Stretch (axis scaling) and Morph
// (independent end-face deformation). Both use ModulePartSculptor's existing
// commit/validation pipeline rather than manipulating craft parts directly.
// Runtime UI discovery avoids relying on private KSP editor prefab fields.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using static PartSculptor.RegisterToolbar;

namespace PartSculptor
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    internal sealed class EditorStretchTool : MonoBehaviour
    {
        private const string LockId = "PartSculptor_StretchTool_Pick";
        private const float FindInterval = 0.5f;
        // No extra margin between the fourth stock editor tool, Stretch and Morph.
        private const float IconGap = 3f;
        // Logical KSP UI units for each custom Stretch and Morph button.
        private const float ToolButtonSize = 28f;
        // A Unity UI Image with no sprite renders an opaque WHITE rectangle.
        // Our editor tools are outside the stock editor-tool background, so
        // transparent pixels in the PNG would otherwise reveal white instead
        // of the normal grey KSP tool button. Always tint the button itself
        // grey when inactive, whether or not a stock sprite is available.
        private static readonly Color EditorToolGrey =
            new Color(0.43f, 0.45f, 0.48f, 1f);
        private readonly List<Button> stockButtons = new List<Button>();
        private readonly List<Toggle> stockToggles = new List<Toggle>();
        private readonly List<RectTransform> stockToolRects = new List<RectTransform>();
        private readonly List<KSP.UI.UIStateToggleButton> nativeStateButtons =
            new List<KSP.UI.UIStateToggleButton>();
        private UnityAction nativeClickHandler;
        private UnityAction<bool> nativeToggleHandler;
        private RectTransform stockReference;
        private RectTransform toolRect;
        private GameObject toolObject;
        private Image background;
        private Button toolButton;
        private Texture2D ownedTexture;
        private RectTransform morphToolRect;
        private GameObject morphToolObject;
        private Image morphBackground;
        private Button morphToolButton;
        private Texture2D ownedMorphTexture;
        private float nextFindTime;
        private bool modeActive;
        private bool faceMode;
        private string faceStatus = "";
        private ModulePartSculptor selectedModule;
        private bool hasLock;
        private bool nativeModeKnown;
        private ConstructionMode nativeModeOnActivation;
        private bool switchingToPlace;
        private bool disposing;

        private void Start()
        {
            nativeClickHandler = OnNativeClick;
            nativeToggleHandler = OnNativeToggle;
        }

        private void Update()
        {
            if (!HighLogic.LoadedSceneIsEditor || EditorLogic.fetch == null)
            {
                ExitStretchMode();
                return;
            }
            if (toolObject != null && (stockReference == null || morphToolObject == null))
            {
                // The stock strip was rebuilt, or either custom button was
                // destroyed. Recreate BOTH without leaving an input lock.
                ExitStretchMode();
                RemoveNativeListeners();
                DestroyToolButtons();
            }
            if (toolObject == null && Time.unscaledTime >= nextFindTime)
            {
                nextFindTime = Time.unscaledTime + FindInterval;
                TryInstallButton();
            }
            // A stock Ctrl+Z/Ctrl+Y can replace entire Part instances. Exit
            // before KSP restores the craft so no custom gizmo/selection keeps
            // referencing a stale part and the editor pickup lock is released.
            // This also dismisses a gizmo opened directly from the part PAW.
            if (EditorUndoSupport.UndoRedoShortcutPressed() &&
                !EditorLogic.fetch.AnyTextFieldHasFocus())
            {
                ExitStretchMode();
                ScaleGizmo.Hide();
                FaceMorphGizmo.Hide();
                return; // Do not consume the shortcut: KSP performs the undo.
            }
            if (!modeActive) return;
            if (!hasLock)
            {
                // No suitable editor pickup lock: do not allow Stretch mode to
                // move the craft while it is trying to inspect parts.
                ExitStretchMode();
                return;
            }
            if (nativeModeKnown && !switchingToPlace &&
                EditorLogic.fetch.EditorConstructionMode != nativeModeOnActivation)
            {
                ExitStretchMode();
                return;
            }
            // Stock tool hotkeys must also release our mode, including "1"
            // where the stock place mode may already be the selected mode.
            if (!EditorLogic.fetch.AnyTextFieldHasFocus() &&
                (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2) ||
                 Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Alpha4)))
            {
                ExitStretchMode();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // First Esc cancels either handle drag; later Esc exits.
                if (!ScaleGizmo.IsDragging && !FaceMorphGizmo.IsDragging)
                    ExitStretchMode();
                return;
            }
            if (!EditorLogic.fetch.AnyTextFieldHasFocus() &&
                !ScaleGizmo.IsDragging && !FaceMorphGizmo.IsDragging &&
                Input.GetKeyDown(KeyCode.M))
            {
                ChangeFaceMode();
                return;
            }
            if (!Input.GetMouseButtonDown(0) || ScaleGizmo.IsPointerOnHandleOrDragging() ||
                FaceMorphGizmo.IsPointerOnHandleOrDragging())
                return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            Vector3 mouse = Input.mousePosition;
            if (mouse.x < 0 || mouse.y < 0 || mouse.x >= Screen.width || mouse.y >= Screen.height)
                return;
            Part clicked = PickPart(mouse);
            if (clicked == null) return;
            ModulePartSculptor module = clicked.FindModuleImplementing<ModulePartSculptor>();
            string partTitle = clicked.partInfo != null ? clicked.partInfo.title : clicked.name;
            if (module == null || !module.CanUseScaleGizmo)
            {
                ScaleGizmo.Hide();
                FaceMorphGizmo.Hide();
                selectedModule = null;
                if (faceMode)
                {
                    faceStatus = module == null ?
                        "Morph unavailable on " + partTitle +
                        ": no PartSculptor module" :
                        "Morph unavailable on " + partTitle + ": " +
                        module.MorphSelectionFailure;
                    Log.Warning("[PartSculptor] " + faceStatus);
                    ScreenMessages.PostScreenMessage(faceStatus, Settings.screenMessageTime,
                        ScreenMessageStyle.UPPER_CENTER);
                }
                else faceStatus = "";
                return;
            }
            selectedModule = module;
            faceStatus = "";
            SelectGizmo();
        }

        private void LateUpdate()
        {
            if (toolObject != null && stockReference != null)
            {
                if (!stockReference.gameObject.activeInHierarchy)
                {
                    // If KSP hides its construction tools, abandon our mode
                    // instead of leaving the editor pickup input locked.
                    ExitStretchMode();
                    toolObject.SetActive(false);
                    if (morphToolObject != null) morphToolObject.SetActive(false);
                    return;
                }
                if (!toolObject.activeSelf) toolObject.SetActive(true);
                if (morphToolObject != null && !morphToolObject.activeSelf)
                    morphToolObject.SetActive(true);
                AlignNextToStockTools();
            }
        }

        private static ControlTypes EditorPickLock()
        {
            ControlTypes mask;
            if (Enum.TryParse("EDITOR_PAD_PICK_PLACE", out mask) &&
                Enum.IsDefined(typeof(ControlTypes), mask)) return mask;
            // Never fall back to EDITOR_LOCK here: that broad lock can also
            // prevent the player from clicking the native tool buttons.
            return (ControlTypes)0;
        }

        private bool AcquireLock()
        {
            if (hasLock) return true;
            ControlTypes mask = EditorPickLock();
            if (mask == (ControlTypes)0)
            {
                Log.Warning("[PartSculptor] Stretch tool could not locate an editor pickup lock.");
                return false;
            }
            InputLockManager.SetControlLock(mask, LockId);
            hasLock = true;
            return true;
        }

        private void ReleaseLock()
        {
            if (!hasLock) return;
            InputLockManager.RemoveControlLock(LockId);
            hasLock = false;
        }

        private void ToggleStretch() { ToggleMode(false); }
        private void ToggleMorph() { ToggleMode(true); }

        private void ToggleMode(bool morph)
        {
            if (modeActive)
            {
                if (faceMode == morph) { ExitStretchMode(); return; }
                // Switching between custom tools must not drop selection or
                // unlock the editor: the chosen part immediately gets its
                // corresponding gizmo. Both buttons stay mutually exclusive.
                faceMode = morph;
                faceStatus = "";
                ScaleGizmo.Hide();
                FaceMorphGizmo.Hide();
                SelectGizmo();
                RefreshButtonColor();
                return;
            }
            if (!HighLogic.LoadedSceneIsEditor || !AcquireLock()) return;
            // The stock place tool is the least intrusive mode to leave behind
            // the custom gizmo. Avoid assuming the enum's spelling across builds.
            EditorToolsUI tools = EditorLogic.fetch != null ? EditorLogic.fetch.toolsUI : null;
            if (tools != null)
            {
                ConstructionMode place;
                bool hasPlace = Enum.TryParse("Place", true, out place) ||
                    Enum.TryParse("Placement", true, out place);
                if (!hasPlace)
                    foreach (ConstructionMode candidate in Enum.GetValues(typeof(ConstructionMode)))
                        if (candidate.ToString().IndexOf("place", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            place = candidate;
                            hasPlace = true;
                            break;
                        }
                if (hasPlace)
                {
                    switchingToPlace = true;
                    try { tools.SetMode(place); }
                    finally { switchingToPlace = false; }
                }
            }
            nativeModeOnActivation = EditorLogic.fetch.EditorConstructionMode;
            nativeModeKnown = true;
            ScaleGizmo.Hide();
            FaceMorphGizmo.Hide();
            selectedModule = null;
            faceStatus = "";
            faceMode = morph;
            modeActive = true;
            RefreshButtonColor();
        }

        private void ExitStretchMode()
        {
            if (!modeActive && !hasLock) return;
            modeActive = false;
            nativeModeKnown = false;
            ReleaseLock();
            ScaleGizmo.Hide();
            FaceMorphGizmo.Hide();
            selectedModule = null;
            faceStatus = "";
            faceMode = false;
            RefreshButtonColor();
        }

        private void OnGUI()
        {
            if (!HighLogic.LoadedSceneIsEditor) return;
            // IMGUI uses a top-left origin, while the editor's RectTransforms
            // are located in screen coordinates with a bottom-left origin.
            // Anchor the entire morph help area under all SIX actual tools.
            Rect helpRect = MorphHelpRect();
            FaceMorphGizmo.DrawActive(helpRect); // Sixth tool or direct PAW activation
            ScaleGizmo.DrawActive(helpRect); // Fifth tool: foreground axes + anchored help
            // Unsupported meshes use the same anchored area as the normal help.
            if (modeActive && faceMode && !string.IsNullOrEmpty(faceStatus))
                FaceMorphGizmo.DrawHelpBox(helpRect, faceStatus);
        }

        private static void IncludeButtonBounds(RectTransform button, Camera uiCamera,
            ref float left, ref float right, ref float bottom)
        {
            if (button == null) return;
            Vector3[] corners = new Vector3[4];
            button.GetWorldCorners(corners);
            for (int i = 0; i < corners.Length; ++i)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]);
                left = Mathf.Min(left, screen.x);
                right = Mathf.Max(right, screen.x);
                bottom = Mathf.Min(bottom, screen.y);
            }
        }

        private Rect MorphHelpRect()
        {
            // A PAW button may activate face morph before the custom toolbar
            // can be discovered. Fall back only when no tool layout exists.
            if (stockToolRects.Count == 0 || toolRect == null || morphToolRect == null)
                return new Rect(12f, 222f, 200f, 0f);

            Canvas canvas = toolRect.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ?
                canvas.worldCamera : null;
            float left = float.PositiveInfinity;
            float right = float.NegativeInfinity;
            float bottom = float.PositiveInfinity;
            foreach (RectTransform nativeTool in stockToolRects)
                IncludeButtonBounds(nativeTool, uiCamera, ref left, ref right, ref bottom);
            IncludeButtonBounds(toolRect, uiCamera, ref left, ref right, ref bottom);
            IncludeButtonBounds(morphToolRect, uiCamera, ref left, ref right, ref bottom);
            if (float.IsInfinity(left) || float.IsInfinity(right) || float.IsInfinity(bottom) ||
                float.IsNaN(left) || float.IsNaN(right) || float.IsNaN(bottom))
                return new Rect(12f, 222f, 200f, 0f);

            // The left edge aligns with the FIRST native gizmo. The right edge
            // aligns with Morph; the box cannot spill left of the tool strip.
            float x = Mathf.Max(0f, left);
            float width = Mathf.Max(1f, Mathf.Min(Screen.width, right) - x);
            float y = Mathf.Max(0f, Screen.height - bottom + 4f);
            return new Rect(x, y, width, 0f); // Height follows wrapped text.
        }

        // Keep M as an optional shortcut, but two independent editor buttons
        // now provide the visible way of choosing Stretch vs Morph.
        private void ChangeFaceMode()
        {
            ToggleMode(!faceMode);
        }

        private void SelectGizmo()
        {
            if (selectedModule == null) return;
            if (faceMode)
            {
                ScaleGizmo.Hide();
                FaceMorphGizmo.Show(selectedModule);
                faceStatus = FaceMorphGizmo.IsFor(selectedModule) ? "" :
                    "Face morph unavailable on " +
                    (selectedModule.part.partInfo != null ?
                        selectedModule.part.partInfo.title : selectedModule.part.name) +
                    ": " + selectedModule.FaceMorphFailure;
                if (!string.IsNullOrEmpty(faceStatus))
                {
                    Log.Warning("[PartSculptor] " + faceStatus);
                    ScreenMessages.PostScreenMessage(faceStatus, Settings.screenMessageTime,
                        ScreenMessageStyle.UPPER_CENTER);
                }
            }
            else
            {
                faceStatus = "";
                FaceMorphGizmo.Hide();
                ScaleGizmo.Show(selectedModule);
            }
        }

        private void OnNativeClick()
        {
            if (!switchingToPlace) ExitStretchMode();
        }

        private void OnNativeToggle(bool value)
        {
            if (value && !switchingToPlace) ExitStretchMode();
        }

        private static bool IsModeName(string name)
        {
            string lower = name.ToLowerInvariant();
            return lower.Contains("place") || lower.Contains("offset") ||
                lower.Contains("move") || lower.Contains("rotate") ||
                lower.Contains("root") || lower.Contains("gizmo");
        }

        private void TryInstallButton()
        {
            EditorToolsUI tools = EditorLogic.fetch != null ? EditorLogic.fetch.toolsUI : null;
            if (tools == null) return;
            Canvas canvas = tools.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            // Stock KSP uses a common UI container for its four editor tools.
            // Search within EditorToolsUI only (not the entire editor canvas).
            Selectable[] choices = tools.GetComponentsInChildren<Selectable>(true);
            Dictionary<Transform, List<Selectable>> groups =
                new Dictionary<Transform, List<Selectable>>();
            foreach (Selectable choice in choices)
            {
                if (choice == null || !(choice.transform is RectTransform) ||
                    choice.transform.parent == null) continue;
                List<Selectable> items;
                if (!groups.TryGetValue(choice.transform.parent, out items))
                    groups[choice.transform.parent] = items = new List<Selectable>();
                items.Add(choice);
            }
            List<Selectable> best = null;
            int bestScore = int.MinValue;
            foreach (KeyValuePair<Transform, List<Selectable>> pair in groups)
            {
                List<Selectable> items = pair.Value;
                if (items.Count < 4 || items.Count > 6) continue;
                int matches = 0;
                foreach (Selectable item in items)
                    if (IsModeName(item.name)) ++matches;
                int score = (items.Count == 4 ? 50 : 0) + matches * 10;
                if (IsModeName(pair.Key.name)) score += 10;
                if (score > bestScore) { bestScore = score; best = items; }
            }
            // On some KSP builds EditorToolsUI's four mode buttons are nested
            // under separate RectTransforms. In that layout use the four stock
            // state-toggle Button references instead of requiring siblings.
            if (best == null)
            {
                KSP.UI.UIStateToggleButton[] states =
                    tools.GetComponentsInChildren<KSP.UI.UIStateToggleButton>(true);
                List<Selectable> candidates = new List<Selectable>();
                foreach (KSP.UI.UIStateToggleButton state in states)
                {
                    if (state != null && state.Button != null &&
                        !candidates.Contains(state.Button))
                        candidates.Add(state.Button);
                }
                if (candidates.Count == 4) best = candidates;
            }
            // Never inject beside an unrelated group of UI buttons.
            if (best == null) return;
            bool sameParent = true;
            for (int i = 1; i < best.Count; ++i)
                if (best[i].transform.parent != best[0].transform.parent)
                    sameParent = false;
            if (sameParent)
                best.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(
                    b.transform.GetSiblingIndex()));
            else
                best.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            Selectable last = best[best.Count - 1];
            stockToolRects.Clear();
            foreach (Selectable nativeTool in best)
            {
                RectTransform nativeRect = nativeTool.transform as RectTransform;
                if (nativeRect != null) stockToolRects.Add(nativeRect);
            }
            stockReference = last.transform as RectTransform;
            if (stockReference == null) return;
            // Render under the Canvas, not inside the stock four-button mask:
            // a stock clipping rect would otherwise hide the fifth button.
            toolObject = new GameObject("PartSculptor Editor Tool", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button));
            toolObject.transform.SetParent(canvas.transform, false);
            toolRect = toolObject.GetComponent<RectTransform>();
            toolRect.anchorMin = toolRect.anchorMax = new Vector2(0.5f, 0.5f);
            toolRect.pivot = new Vector2(0.5f, 0.5f);
            toolRect.sizeDelta = new Vector2(ToolButtonSize, ToolButtonSize);
            background = toolObject.GetComponent<Image>();
            Image nativeImage = last.GetComponent<Image>();
            // Keep stock button contours when the native Image has a sprite,
            // but never inherit the Image's default white fill. Our PNG icon
            // is a separate RawImage above this grey button background.
            background.sprite = nativeImage != null ? nativeImage.sprite : null;
            background.type = nativeImage != null && nativeImage.sprite != null ?
                nativeImage.type : Image.Type.Simple;
            background.color = EditorToolGrey;
            toolButton = toolObject.GetComponent<Button>();
            toolButton.targetGraphic = background;
            toolButton.onClick.AddListener(ToggleStretch);
            ColorBlock colors = toolButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.78f, 0.92f, 1f);
            colors.pressedColor = new Color(0.45f, 0.8f, 1f);
            toolButton.colors = colors;
            GameObject iconObject = new GameObject("Stretch icon", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RawImage));
            iconObject.transform.SetParent(toolRect, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            // Use the entire button rectangle: no inset around the Stretch icon.
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            RawImage icon = iconObject.GetComponent<RawImage>();
            icon.raycastTarget = false;
            // PluginData is intentionally not imported by GameDatabase. Load
            // the existing PartSculptor toolbar icon directly from disk.
            Texture2D texture = null;
            try
            {
                string path = Path.Combine(KSPUtil.ApplicationRootPath,
                    "GameData/PartSculptor/PluginData/Icons/inspector_38_gizmo.png");
                if (File.Exists(path))
                {
                    ownedTexture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                    if (ownedTexture.LoadImage(File.ReadAllBytes(path)))
                        texture = ownedTexture;
                }
            }
            catch (Exception e)
            {
                Log.Warning("[PartSculptor] Cannot load Stretch tool icon: " + e.Message);
            }
            if (texture != null)
            {
                icon.texture = texture;
                icon.color = Color.white; // Preserve PNG alpha; do not fill transparent pixels.
            }
            else { icon.color = new Color(0.2f, 0.95f, 0.5f); }

            // Sixth tool has its own hit target, distinct icon and selected
            // state. It lives under the same unclipped Canvas as Stretch.
            morphToolObject = new GameObject("PartSculptor Morph Editor Tool",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            morphToolObject.transform.SetParent(canvas.transform, false);
            morphToolRect = morphToolObject.GetComponent<RectTransform>();
            morphToolRect.anchorMin = morphToolRect.anchorMax = new Vector2(0.5f, 0.5f);
            morphToolRect.pivot = new Vector2(0.5f, 0.5f);
            morphToolRect.sizeDelta = new Vector2(ToolButtonSize, ToolButtonSize);
            morphBackground = morphToolObject.GetComponent<Image>();
            morphBackground.sprite = background.sprite;
            morphBackground.type = background.type;
            morphBackground.color = EditorToolGrey;
            morphToolButton = morphToolObject.GetComponent<Button>();
            morphToolButton.targetGraphic = morphBackground;
            morphToolButton.onClick.AddListener(ToggleMorph);
            ColorBlock morphColors = morphToolButton.colors;
            morphColors.normalColor = Color.white;
            morphColors.highlightedColor = new Color(0.7f, 0.92f, 1f);
            morphColors.pressedColor = new Color(0.3f, 0.7f, 1f);
            morphToolButton.colors = morphColors;
            GameObject morphIconObject = new GameObject("Morph icon", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RawImage));
            morphIconObject.transform.SetParent(morphToolRect, false);
            RectTransform morphIconRect = morphIconObject.GetComponent<RectTransform>();
            morphIconRect.anchorMin = Vector2.zero;
            morphIconRect.anchorMax = Vector2.one;
            // Use the entire button rectangle: no inset around the Morph icon.
            morphIconRect.offsetMin = Vector2.zero;
            morphIconRect.offsetMax = Vector2.zero;
            RawImage morphIcon = morphIconObject.GetComponent<RawImage>();
            morphIcon.raycastTarget = false;
            Texture2D morphTexture = null;
            try
            {
                string path = Path.Combine(KSPUtil.ApplicationRootPath,
                    "GameData/PartSculptor/PluginData/Icons/morph_38.png");
                if (File.Exists(path))
                {
                    ownedMorphTexture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                    if (ownedMorphTexture.LoadImage(File.ReadAllBytes(path)))
                        morphTexture = ownedMorphTexture;
                }
            }
            catch (Exception e)
            {
                Log.Warning("[PartSculptor] Cannot load Morph tool icon: " + e.Message);
            }
            if (morphTexture != null)
            {
                morphIcon.texture = morphTexture;
                morphIcon.color = Color.white; // Blend transparent PNG pixels over button grey.
            }
            else morphIcon.color = new Color(0.12f, 0.85f, 1f);

            // Switching back to any native tool exits either custom mode.
            foreach (Selectable native in best)
            {
                KSP.UI.UIStateToggleButton nativeState =
                    native.GetComponent<KSP.UI.UIStateToggleButton>();
                if (nativeState == null)
                    nativeState = native.GetComponentInParent<KSP.UI.UIStateToggleButton>();
                if (nativeState != null)
                {
                    if (nativeState.onClick != null)
                        nativeState.onClick.AddListener(nativeClickHandler);
                    if (nativeState.onTrue != null)
                        nativeState.onTrue.AddListener(nativeClickHandler);
                    nativeStateButtons.Add(nativeState);
                }
                Button button = native as Button;
                if (button != null)
                {
                    button.onClick.AddListener(nativeClickHandler);
                    stockButtons.Add(button);
                }
                Toggle toggle = native as Toggle;
                if (toggle != null)
                {
                    toggle.onValueChanged.AddListener(nativeToggleHandler);
                    stockToggles.Add(toggle);
                }
            }
            AlignNextToStockTools();
            RefreshButtonColor();
        }

        private void AlignNextToStockTools()
        {
            if (stockReference == null || toolRect == null) return;
            Vector3[] corners = new Vector3[4];
            stockReference.GetWorldCorners(corners);
            Vector3 right = corners[3] - corners[0];
            float width = right.magnitude;
            if (width < 0.0001f) return;
            // Position is computed from the actual fourth button, so editor UI
            // scaling, aspect ratio and mods moving the tool strip are handled.
            Vector3 up = corners[1] - corners[0];
            float height = up.magnitude;
            Vector3 centerRight = (corners[2] + corners[3]) * 0.5f;
            // Convert the requested 28 UI units to native world size so it
            // follows KSP UI scaling even though these buttons live on the
            // unclipped Canvas rather than the stock strip's parent.
            float referenceWidth = Mathf.Max(0.0001f, stockReference.rect.width);
            float referenceHeight = Mathf.Max(0.0001f, stockReference.rect.height);
            float buttonWorldWidth = width * (ToolButtonSize / referenceWidth);
            float buttonWorldHeight = height * (ToolButtonSize / referenceHeight);
            float gapWorld = width * (IconGap / referenceWidth);
            // Position from the actual edges, not the stock button's width;
            // otherwise shrinking 32 -> 28 leaves a gap between the tools.
            toolRect.position = centerRight + right.normalized *
                (buttonWorldWidth * 0.5f);
            toolRect.rotation = stockReference.rotation;
            if (morphToolRect != null)
            {
                morphToolRect.position = toolRect.position + right.normalized *
                    (buttonWorldWidth + gapWorld);
                morphToolRect.rotation = stockReference.rotation;
            }
            // Match requested world dimensions despite different Canvas parents.
            float unitX = toolRect.parent.TransformVector(Vector3.right).magnitude;
            float unitY = toolRect.parent.TransformVector(Vector3.up).magnitude;
            if (unitX > 0.00001f && unitY > 0.00001f)
            {
                toolRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    buttonWorldWidth / unitX);
                toolRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                    buttonWorldHeight / unitY);
            }
            if (morphToolRect != null && morphToolRect.parent != null)
            {
                float morphX = morphToolRect.parent.TransformVector(Vector3.right).magnitude;
                float morphY = morphToolRect.parent.TransformVector(Vector3.up).magnitude;
                if (morphX > 0.00001f && morphY > 0.00001f)
                {
                    morphToolRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                        buttonWorldWidth / morphX);
                    morphToolRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                        buttonWorldHeight / morphY);
                }
            }
        }

        private void RefreshButtonColor()
        {
            if (background != null)
                background.color = modeActive && !faceMode ?
                    new Color(0.35f, 0.82f, 0.45f) : EditorToolGrey;
            if (morphBackground != null)
                morphBackground.color = modeActive && faceMode ?
                    new Color(0.12f, 0.78f, 0.95f) : EditorToolGrey;
        }

        private static Part PickPart(Vector3 mouse)
        {
            if (EditorLogic.fetch == null || EditorLogic.fetch.ship == null) return null;
            Camera camera = EditorLogic.fetch.editorCamera;
            if (camera == null) camera = Camera.main;
            if (camera == null) return null;
            RaycastHit[] hits = Physics.RaycastAll(camera.ScreenPointToRay(mouse), 5000f);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null) continue;
                Part part = hit.collider.GetComponentInParent<Part>();
                if (part != null && EditorLogic.fetch.ship.parts.Contains(part))
                    return part;
            }
            return null;
        }

        private void OnDisable() { Cleanup(); }
        private void OnDestroy() { Cleanup(); }

        private void RemoveNativeListeners()
        {
            foreach (KSP.UI.UIStateToggleButton native in nativeStateButtons)
            {
                if (native == null) continue;
                if (native.onClick != null)
                    native.onClick.RemoveListener(nativeClickHandler);
                if (native.onTrue != null)
                    native.onTrue.RemoveListener(nativeClickHandler);
            }
            nativeStateButtons.Clear();
            foreach (Button button in stockButtons)
                if (button != null) button.onClick.RemoveListener(nativeClickHandler);
            foreach (Toggle toggle in stockToggles)
                if (toggle != null) toggle.onValueChanged.RemoveListener(nativeToggleHandler);
            stockButtons.Clear();
            stockToggles.Clear();
        }

        private void DestroyToolButtons()
        {
            if (toolObject != null) Destroy(toolObject);
            if (morphToolObject != null) Destroy(morphToolObject);
            if (ownedTexture != null) Destroy(ownedTexture);
            if (ownedMorphTexture != null) Destroy(ownedMorphTexture);
            toolObject = null;
            morphToolObject = null;
            toolRect = null;
            morphToolRect = null;
            toolButton = null;
            morphToolButton = null;
            background = null;
            morphBackground = null;
            stockReference = null;
            stockToolRects.Clear();
            ownedTexture = null;
            ownedMorphTexture = null;
        }

        private void Cleanup()
        {
            if (disposing) return;
            disposing = true;
            ExitStretchMode();
            RemoveNativeListeners();
            DestroyToolButtons();
        }
    }
}
