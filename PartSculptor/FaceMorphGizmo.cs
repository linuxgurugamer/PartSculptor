// Sixteen handles: four corners plus four edge midpoints on each END FACE.
// Drag a corner to resize a face in X/Z; Alt tilts the face, Shift+Alt
// translates the chosen face, and Ctrl+Alt uniformly expands/contracts it
// around its center axis, preserving the ratio between X and Z dimensions.
// Uses runtime Unity objects, never UnityEditor classes.
using System;
using UnityEngine;
using UnityEngine.EventSystems;

using static PartSculptor.RegisterToolbar;

namespace PartSculptor
{
    internal sealed class FaceMorphGizmo
    {
        private const string LockId = "PartSculptor_FaceMorph_Pick";
        private const float HitRadius = 20f;
        private const int CornerCount = 8;
        private const int HandleCount = 16;
        // For each face, midpoint order is X-, X+, Z-, Z+.
        // Pairs refer to the existing 8 corners, preserving saved-craft math.
        private static readonly int[,] MidpointPairs = {
            {0,2},{1,3},{0,1},{2,3},
            {4,6},{5,7},{4,5},{6,7}
        };
        private static FaceMorphGizmo active;
        private readonly ModulePartSculptor module;
        private readonly GameObject root;
        private readonly Material[] materials = new Material[2];
        private readonly Transform[] corners = new Transform[8];
        private readonly Transform[] midpoints = new Transform[8];
        private readonly LineRenderer[] edges = new LineRenderer[12];
        private readonly Vector2[] screen = new Vector2[HandleCount];
        private readonly Vector3[] position = new Vector3[HandleCount];
        private readonly bool[] visible = new bool[HandleCount];
        // The editor addon's OnGUI draws a screen-space marker/cage on every
        // Repaint. This must not be conditional on an optional Unity Canvas:
        // a successfully instantiated Canvas can still render nothing on
        // some KSP editor UI configurations (the 0.0.4.64 regression).
        // Mouse picking and this drawing both use the same screen[] points.
        private static readonly int[,] CagePairs = {
            {0,1},{1,3},{3,2},{2,0}, {4,5},{5,7},{7,6},{6,4},
            {0,4},{1,5},{2,6},{3,7}
        };
        private Camera camera;
        private int hovered = -1, dragging = -1;
        private bool locked, disposed, dragTilt, dragLinked, dragRadial, dragLockXZ;
        private bool undoCheckpointTaken;
        private MorphFace dragBottom, dragTop;
        private Vector2 clickMouse;
        private Vector3 clickWorld;
        private Vector2 clickHandleScreen;
        private Plane dragPlane;
        // One gesture uses the part's ACTUAL pose at mouse-down. A previously
        // morphed attachment may already have rotated this transform, and Copy
        // mode can rotate it again while the same drag is in progress.
        // Screen-to-morph mapping must use neither world-vertical nor the
        // changing mid-drag pose; those make the handles drift or reverse.
        private Matrix4x4 dragLocalToWorld = Matrix4x4.identity;
        private Matrix4x4 dragWorldToLocal = Matrix4x4.identity;
        private Camera dragCamera;
        private float dragHalfX, dragHalfZ;
        // Freeze the projected local longitudinal direction for one gesture.
        // A connected child can rotate while its own face is being morphed;
        // recomputing this vector on every frame reverses the drag direction.
        private Vector2 dragTiltScreenUp = Vector2.up;
        private float dragTiltPixelsPerUnit = 12f;
        private int dragTiltSign = 1;

        internal static bool IsDragging { get { return active != null && active.dragging >= 0; } }

        internal static void Show(ModulePartSculptor owner)
        {
            if (active != null && active.module == owner) return;
            Hide();
            if (owner != null && owner.EnsureFaceMorph())
            {
                ScaleGizmo.Hide();
                active = new FaceMorphGizmo(owner);
            }
            else if (owner != null)
                Log.Warning("[PartSculptor] Face morph is not supported for " +
                    (owner.part != null ? owner.part.name : "unknown part"));
        }
        internal static bool IsFor(ModulePartSculptor owner)
        { return active != null && active.module == owner; }
        internal static void HideFor(ModulePartSculptor owner)
        { if (IsFor(owner)) Hide(); }
        internal static void Hide()
        {
            if (active == null) return;
            FaceMorphGizmo old = active;
            active = null;
            old.Dispose();
        }
        internal static void UpdateFor(ModulePartSculptor owner)
        { if (IsFor(owner)) active.Update(); }
        // Draw from the editor addon, which receives OnGUI even if KSP does
        // not forward IMGUI callbacks to a PartModule on the selected part.
        internal static void DrawActive(Rect helpRect)
        { if (active != null && !active.disposed) active.DrawLabels(helpRect); }

        // One wrapped information panel instead of two wide, hard-coded
        // horizontal labels; its x/width come from the native editor toolbar.
        internal static void DrawHelpBox(Rect bounds, string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            var lines = message.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.wordWrap = false; //  true;
            style.alignment = TextAnchor.UpperLeft;
            style.padding = new RectOffset(6, 6, 5, 5);

            int maxLineWidth = 0;
            float height = 0;
            for (int i = 0; i < lines.Length; ++i)
            { 
                lines[i] = lines[i].Trim(); 
                style.CalcMinMaxWidth(new GUIContent(lines[i]), out float minWidth, out float maxWidth);
                maxLineWidth = Math.Max(maxLineWidth, (int)maxWidth +20);
                height += style.CalcHeight(new GUIContent(lines[i]), bounds.width);
            }
            bounds.width = maxLineWidth + style.padding.left + style.padding.right;

            bounds.height = Mathf.Max(25f, height);
            // Keep the help panel on screen, even with larger KSP UI scales.
            bounds.height = Mathf.Min(bounds.height, Mathf.Max(0f, Screen.height - bounds.y));
            if (bounds.width > 0f && bounds.height > 0f)
                DrawMorphTip(bounds, message, style);
        }
        // IMGUI skins may use transparent box textures even when GUI.color.a is
        // 1. Draw a solid-color background ourselves, with opacity independent
        // of any inherited alpha or texture transparency. Text uses its own
        // pass at full alpha, so the slider never fades the instructions.
        private static void DrawMorphTip(Rect bounds, string message, GUIStyle style)
        {
            GUISkin previousSkin = GUI.skin;
            Color previousColor = GUI.color;
            try
            {
                GUI.skin = ScaleUserSettings.WindowSkin(previousSkin);
                GUIStyle textStyle = new GUIStyle(GUI.skin.box);
                // Carry over the current skin's font and text color, but not
                // its semitransparent background texture.
                textStyle.normal.background = null;
                textStyle.hover.background = null;
                textStyle.active.background = null;
                textStyle.focused.background = null;
                textStyle.onNormal.background = null;
                textStyle.onHover.background = null;
                textStyle.onActive.background = null;
                textStyle.onFocused.background = null;
                textStyle.wordWrap = style.wordWrap;
                textStyle.alignment = style.alignment;
                textStyle.padding = style.padding;

                Color textColor = textStyle.normal.textColor;
                // Make the background contrast with the current skin's text.
                float brightness = textColor.r * 0.299f +
                    textColor.g * 0.587f + textColor.b * 0.114f;
                Color panelColor = brightness >= 0.5f
                    ? new Color(0.13f, 0.15f, 0.18f, 1f)
                    : new Color(0.86f, 0.86f, 0.86f, 1f);
                // 100% is truly opaque, regardless of GUI.skin.box or any
                // inherited GUI.color alpha. This also works at 10%–99%.
                panelColor.a = Mathf.Clamp01(ScaleUserSettings.MorphTipOpacity);
                GUI.color = panelColor;
                GUI.DrawTexture(bounds, Texture2D.whiteTexture);

                // Restore the RGB tint, but never let background opacity fade
                // labels, numbers or controls inside the Morph tip.
                GUI.color = new Color(previousColor.r, previousColor.g,
                    previousColor.b, 1f);
                GUI.Label(bounds, message, textStyle);
            }
            finally
            {
                GUI.color = previousColor;
                GUI.skin = previousSkin;
            }
        }
        internal static bool IsPointerOnHandleOrDragging()
        {
            if (active == null || active.disposed) return false;
            if (active.dragging >= 0) return true;
            Vector2 mouse = Input.mousePosition;
            for (int i = 0; i < HandleCount; ++i)
                if (active.visible[i] && (active.screen[i] - mouse).sqrMagnitude <=
                    HitRadius * HitRadius) return true;
            return false;
        }

        private FaceMorphGizmo(ModulePartSculptor owner)
        {
            module = owner;
            root = new GameObject("PartSculptor face morph handles");
            root.hideFlags = HideFlags.DontSave;
            int renderLayer = FindRenderLayer();
            root.layer = renderLayer;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ??
                Shader.Find("Diffuse");
            Color[] colors = { new Color(1f, 0.55f, 0.12f), new Color(0.12f, 0.85f, 1f) };
            for (int f = 0; f < 2; ++f)
            {
                // Some installations strip built-in shaders; the GUI overlay
                // below must still work when no suitable 3D shader exists.
                if (shader != null)
                {
                    materials[f] = new Material(shader);
                    materials[f].color = colors[f];
                }
                for (int i = 0; i < 4; ++i)
                {
                    GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.name = f == 0 ? "Bottom face corner" : "Top face corner";
                    obj.layer = renderLayer;
                    obj.transform.SetParent(root.transform, false);
                    Collider hit = obj.GetComponent<Collider>();
                    if (hit != null) { hit.enabled = false; UnityEngine.Object.Destroy(hit); }
                    Renderer r = obj.GetComponent<Renderer>();
                    if (r != null)
                    {
                        if (materials[f] != null) r.sharedMaterial = materials[f];
                        else r.enabled = false;
                    }
                    corners[f * 4 + i] = obj.transform;
                }
            }
            // Midpoints are separate, selectable cubes; their world-space
            // coordinates are recomputed from the deformed corners each frame.
            for (int i = 0; i < midpoints.Length; ++i)
            {
                int face = i / 4;
                GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = face == 0 ? "Bottom face edge midpoint" :
                    "Top face edge midpoint";
                obj.layer = renderLayer;
                obj.transform.SetParent(root.transform, false);
                Collider hit = obj.GetComponent<Collider>();
                if (hit != null) { hit.enabled = false; UnityEngine.Object.Destroy(hit); }
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (materials[face] != null) renderer.sharedMaterial = materials[face];
                    else renderer.enabled = false;
                }
                midpoints[i] = obj.transform;
            }
            for (int i = 0; i < edges.Length; i++)
            {
                GameObject lineObj = new GameObject("Morph cage edge");
                lineObj.layer = renderLayer;
                lineObj.transform.SetParent(root.transform, false);
                LineRenderer l = lineObj.AddComponent<LineRenderer>();
                l.useWorldSpace = true;
                l.positionCount = 2;
                l.startWidth = l.endWidth = 0.025f;
                if (materials[i < 4 ? 0 : 1] != null)
                    l.material = materials[i < 4 ? 0 : 1];
                else l.enabled = false;
                edges[i] = l;
            }
        }

        private int FindRenderLayer()
        {
            // The part root and the actual visible KSP model do not always
            // share a layer. Render with the model's layer, not Unity's
            // default layer of newly created primitives.
            if (module.part != null)
            {
                Transform model = module.part.transform.Find("model");
                if (model != null)
                    foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
                        if (r != null && r.enabled && !(r is ParticleSystemRenderer))
                            return r.gameObject.layer;
                return module.part.gameObject.layer;
            }
            return 0;
        }

        private static bool CameraSees(Camera candidate, Vector3 world, int layer)
        {
            if (candidate == null || !candidate.isActiveAndEnabled ||
                (candidate.cullingMask & (1 << layer)) == 0) return false;
            // Do not require the part's bounds CENTER to be within the viewport.
            // For long, wide or offset models the center can be off screen
            // while one or more of the eight corner handles is visible.
            // Individual handle visibility is determined after projection.
            return true;
        }

        private Camera FindCamera(Vector3 center, int layer)
        {
            // The native editor camera is also the camera used for part
            // picking and dragging. Unlike 3D handle rendering, our IMGUI
            // foreground handles do not depend on its culling mask. Prefer
            // it even if the selected part has a nonstandard render layer.
            Camera editor = EditorLogic.fetch != null ?
                EditorLogic.fetch.editorCamera : null;
            if (editor != null && editor.isActiveAndEnabled) return editor;
            Camera main = Camera.main;
            if (CameraSees(main, center, layer)) return main;
            foreach (Camera c in Camera.allCameras)
                if (CameraSees(c, center, layer) &&
                    c.name.IndexOf("UI", StringComparison.OrdinalIgnoreCase) < 0)
                    return c;
            return null;
        }

        private void UpdateGeometry()
        {
            FaceMorphSupport morph = module.FaceMorph;
            if (morph == null || !morph.Valid) return;
            Transform t = module.part.transform;
            int renderLayer = FindRenderLayer();
            if (root.layer != renderLayer)
            {
                root.layer = renderLayer;
                foreach (Transform corner in corners) corner.gameObject.layer = renderLayer;
                foreach (Transform midpoint in midpoints) midpoint.gameObject.layer = renderLayer;
                foreach (LineRenderer edge in edges) edge.gameObject.layer = renderLayer;
            }
            camera = FindCamera(t.TransformPoint((morph.Min + morph.Max) * 0.5f),
                renderLayer);
            MorphFace b = module.GetMorphFace(0), top = module.GetMorphFace(1);
            float size = camera != null ? Mathf.Clamp(
                Vector3.Distance(camera.transform.position, t.position) * 0.012f,
                0.08f, 0.32f) : 0.1f;
            // Keep the first eight indices stable for existing corner modes.
            for (int i = 0; i < CornerCount; ++i)
                position[i] = t.TransformPoint(morph.Corner(i / 4, i % 4, b, top));
            // The next eight lie precisely halfway along each deformed face
            // perimeter edge. This also works for already-tilted/offset faces.
            for (int i = 0; i < midpoints.Length; ++i)
                position[CornerCount + i] = (position[MidpointPairs[i, 0]] +
                    position[MidpointPairs[i, 1]]) * 0.5f;
            for (int i = 0; i < HandleCount; ++i)
            {
                bool edgeHandle = i >= CornerCount;
                Transform handle = edgeHandle ? midpoints[i - CornerCount] : corners[i];
                handle.position = position[i];
                handle.localScale = Vector3.one * size * (edgeHandle ? 0.78f : 1f) *
                    (i == hovered || i == dragging ? 1.4f : 1f);
                Vector3 projected = camera != null ? camera.WorldToScreenPoint(position[i]) :
                    Vector3.zero;
                screen[i] = new Vector2(projected.x, projected.y);
                visible[i] = camera != null && projected.z > 0f;
                handle.gameObject.SetActive(visible[i]);
            }
            for (int i = 0; i < 12; ++i)
            {
                edges[i].SetPosition(0, position[CagePairs[i,0]]);
                edges[i].SetPosition(1, position[CagePairs[i,1]]);
                edges[i].enabled = materials[i < 4 ? 0 : 1] != null &&
                    visible[CagePairs[i,0]] && visible[CagePairs[i,1]];
            }
        }

        private static ControlTypes PickLock()
        {
            ControlTypes result;
            return Enum.TryParse("EDITOR_PAD_PICK_PLACE", out result) &&
                Enum.IsDefined(typeof(ControlTypes), result) ? result : (ControlTypes)0;
        }
        private void Lock(bool value)
        {
            if (value == locked) return;
            if (value)
            {
                ControlTypes control = PickLock();
                if (control == (ControlTypes)0) return;
                InputLockManager.SetControlLock(control, LockId);
                locked = true;
            }
            else { InputLockManager.RemoveControlLock(LockId); locked = false; }
        }

        // A midpoint controls one degree of freedom (X, Z, or the shared
        // X/Z scale). Translate mouse pixels into that control's projected
        // motion rather than measuring mouse movement on the part's X/Z
        // plane: at an oblique camera angle the latter can make an edge
        // handle visibly lag behind the cursor.
        // Work in the frozen start-of-drag transform, rather than following
        // the child's subsequent rigid editor rotations. The visual handles
        // still follow the current live part transform in UpdateGeometry().
        private Vector2 ProjectAtDragStart(Vector3 local)
        {
            Vector3 projected = dragCamera.WorldToScreenPoint(
                dragLocalToWorld.MultiplyPoint3x4(local));
            return new Vector2(projected.x, projected.y);
        }

        private Vector3 LocalHandlePosition(int handleIndex,
            MorphFace bottom, MorphFace top)
        {
            FaceMorphSupport morph = module.FaceMorph;
            if (handleIndex < CornerCount)
                return morph.Corner(handleIndex / 4, handleIndex % 4,
                    bottom, top);
            int index = handleIndex - CornerCount;
            int a = MidpointPairs[index, 0];
            int b = MidpointPairs[index, 1];
            return (morph.Corner(a / 4, a % 4, bottom, top) +
                morph.Corner(b / 4, b % 4, bottom, top)) * 0.5f;
        }

        // Probe the actual selected handle's LOCAL deformation, then project
        // it through the part's current (captured) orientation. This supports
        // a child already tilted by a prior attached-face morph, including
        // nonuniformly stretched/tilted faces and edge midpoints. Using only
        // the screen's vertical axis incorrectly assumes an upright part.
        private Vector2 ProjectTiltMotion(int handleIndex)
        {
            int face = handleIndex < CornerCount ? handleIndex / 4 :
                (handleIndex - CornerCount) / 4;
            int corner = handleIndex % 4;
            int edge = handleIndex < CornerCount ? -1 :
                (handleIndex - CornerCount) % 4;
            float xSign = edge < 0 ? ((corner & 1) == 0 ? -1f : 1f) :
                FaceMorphCore.EdgeWidthSign(edge);
            float zSign = edge < 0 ? ((corner & 2) == 0 ? -1f : 1f) :
                FaceMorphCore.EdgeDepthSign(edge);
            MorphFace initial = face == 0 ? dragBottom : dragTop;
            MorphFace probe = initial;
            const float probeDistance = 0.25f;
            probe.TiltX += probeDistance * xSign /
                Mathf.Max(0.01f, dragHalfX) * 0.5f;
            probe.TiltZ += probeDistance * zSign /
                Mathf.Max(0.01f, dragHalfZ) * 0.5f;
            MorphFace b = face == 0 ? probe : dragBottom;
            MorphFace t = face == 0 ? dragTop : probe;
            Vector2 projected = ProjectAtDragStart(
                LocalHandlePosition(handleIndex, b, t));
            Vector2 initialScreen = ProjectAtDragStart(
                LocalHandlePosition(handleIndex, dragBottom, dragTop));
            return (projected - initialScreen) / probeDistance;
        }

        private Vector2 ProjectMidpoint(int face, int edge, MorphFace candidate)
        {
            FaceMorphSupport morph = module.FaceMorph;
            int index = face * 4 + edge;
            int a = MidpointPairs[index, 0];
            int b = MidpointPairs[index, 1];
            MorphFace bottom = face == 0 ? candidate : dragBottom;
            MorphFace top = face == 0 ? dragTop : candidate;
            Vector3 local = (morph.Corner(a / 4, a % 4, bottom, top) +
                morph.Corner(b / 4, b % 4, bottom, top)) * 0.5f;
            return ProjectAtDragStart(local);
        }

        private Vector2 ProjectMidpointParameter(int face, int edge,
            MorphFace initial, bool proportional, float value)
        {
            MorphFace candidate = initial;
            if (proportional)
            {
                candidate.Width *= value;
                candidate.Depth *= value;
            }
            else if (edge < 2) candidate.Width = value;
            else candidate.Depth = value;
            return ProjectMidpoint(face, edge, candidate);
        }

        private bool ResizeMidpointFromScreen(int face, int edge,
            Vector2 mouse, ref MorphFace f)
        {
            bool proportional = dragRadial || dragLockXZ;
            float lower = proportional ? Mathf.Max(0.25f / f.Width,
                0.25f / f.Depth) : 0.25f;
            float upper = proportional ? Mathf.Min(4f / f.Width,
                4f / f.Depth) : 4f;
            float initial = proportional ? 1f : edge < 2 ? f.Width : f.Depth;
            float probe = Mathf.Clamp(initial + 0.05f, lower, upper);
            if (Mathf.Abs(probe - initial) < 0.0001f)
                probe = Mathf.Clamp(initial - 0.05f, lower, upper);
            if (Mathf.Abs(probe - initial) < 0.0001f) return false;

            Vector2 probeScreen = ProjectMidpointParameter(face, edge, f,
                proportional, probe);
            Vector2 projectedStep = probeScreen - clickHandleScreen;
            if (projectedStep.sqrMagnitude < 0.01f) return false;

            Vector2 pointerDelta = mouse - clickMouse;
            float value = Mathf.Clamp(initial + (float)
                FaceMorphCore.ProjectedHandleParameterDelta(pointerDelta.x,
                    pointerDelta.y, projectedStep.x, projectedStep.y,
                    probe - initial), lower, upper);
            // Perspective projection is not perfectly linear. A few small
            // corrections make the selected handle track the pointer along
            // its screen-space motion, even when stretched or face-tilted.
            Vector2 direction = projectedStep.normalized;
            for (int iteration = 0; iteration < 4; ++iteration)
            {
                Vector2 currentScreen = ProjectMidpointParameter(face, edge,
                    f, proportional, value);
                float error = Vector2.Dot(pointerDelta -
                    (currentScreen - clickHandleScreen), direction);
                if (Mathf.Abs(error) < 0.05f) break;
                float next = Mathf.Clamp(value + 0.025f, lower, upper);
                if (Mathf.Abs(next - value) < 0.0001f)
                    next = Mathf.Clamp(value - 0.025f, lower, upper);
                if (Mathf.Abs(next - value) < 0.0001f) break;
                Vector2 nextScreen = ProjectMidpointParameter(face, edge,
                    f, proportional, next);
                float slope = Vector2.Dot(nextScreen - currentScreen,
                    direction) / (next - value);
                if (Mathf.Abs(slope) < 0.01f) break;
                value = Mathf.Clamp(value + error / slope, lower, upper);
            }

            if (proportional)
            {
                // Snap the common factor, never the individual dimensions.
                // This also preserves the face aspect ratio for edge handles.
                if (Input.GetKey(KeyCode.LeftShift) ||
                    Input.GetKey(KeyCode.RightShift))
                    value = Mathf.Clamp(1f + FaceMorphMath.SnapDelta(
                        value - 1f, ScaleUserSettings.MorphSnapIncrement),
                        lower, upper);
                f.Width *= value;
                f.Depth *= value;
            }
            else if (edge < 2) f.Width = value;
            else f.Depth = value;
            return true;
        }

        private static bool FacesDiffer(MorphFace a, MorphFace b)
        {
            const float epsilon = 0.00001f;
            return Mathf.Abs(a.Width - b.Width) > epsilon ||
                   Mathf.Abs(a.Depth - b.Depth) > epsilon ||
                   Mathf.Abs(a.TiltX - b.TiltX) > epsilon ||
                   Mathf.Abs(a.TiltZ - b.TiltZ) > epsilon ||
                   Mathf.Abs(a.OffsetX - b.OffsetX) > epsilon ||
                   Mathf.Abs(a.OffsetZ - b.OffsetZ) > epsilon;
        }

        // In Copy mode, editing the CHILD'S parent-facing end morphs the
        // parent's end in the opposite local direction, and then rotates the
        // child to keep the stack joint connected.  For a matching pair, that
        // rigid-body rotation reverses the visible movement of the child's
        // dragged edge.  Invert only this drag input, not the morph values or
        // the mirroring/rotation code.  Free ends and parent-driven edits
        // retain the usual drag sense.
        private bool IsParentAttachedFace(int face)
        {
            Part part = module.part;
            if (part == null || part.parent == null || part.attachNodes == null ||
                module.FaceMorph == null ||
                part.parent.FindModuleImplementing<ModulePartSculptor>() == null)
                return false;
            foreach (AttachNode node in part.attachNodes)
            {
                int attachedFace;
                if (node != null && node.attachedPart == part.parent &&
                    module.FaceMorph.TryGetEndFace(node, out attachedFace) &&
                    attachedFace == face)
                    return true;
            }
            return false;
        }

        private void Update()
        {
            if (disposed || !HighLogic.LoadedSceneIsEditor ||
                !module.CanUseScaleGizmo || module.part == null ||
                module.FaceMorph == null || !module.FaceMorph.Valid)
            { Hide(); return; }
            UpdateGeometry();
            Vector2 mouse = Input.mousePosition;
            if (dragging >= 0)
            {
                Lock(true);
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                {
                    module.SetMorphFaces(dragBottom, dragTop);
                    dragging = hovered = -1;
                    Lock(false);
                    return;
                }
                if (!Input.GetMouseButton(0)) { dragging = -1; Lock(false); return; }
                int face = dragging < CornerCount ? dragging / 4 :
                    (dragging - CornerCount) / 4;
                int corner = dragging % 4;
                // Edge midpoint order is X-, X+, Z-, Z+. A midpoint only
                // modifies the dimension/tilt perpendicular to its edge.
                int edge = dragging < CornerCount ? -1 :
                    (dragging - CornerCount) % 4;
                float xSign = edge < 0 ? ((corner & 1) == 0 ? -1f : 1f) :
                    FaceMorphCore.EdgeWidthSign(edge);
                float zSign = edge < 0 ? ((corner & 2) == 0 ? -1f : 1f) :
                    FaceMorphCore.EdgeDepthSign(edge);
                MorphFace b = dragBottom, top = dragTop;
                MorphFace f = face == 0 ? b : top;
                if (dragTilt)
                {
                    // Alt+vertical drag tilts the entire planar face. The
                    // picked quadrant determines the tilt direction.
                    // The part (and therefore its local +Y axis) can rotate
                    // during this same drag. Use the direction captured at
                    // mouse-down, not one that changes or flips mid-gesture.
                    float vertical = Vector2.Dot(mouse - clickMouse,
                        dragTiltScreenUp) / dragTiltPixelsPerUnit;
                    vertical *= dragTiltSign;
                    // A corner tilts in both directions; an edge midpoint
                    // tilts only across the face axis normal to that edge.
                    f.TiltX += vertical * xSign /
                        Mathf.Max(0.01f, dragHalfX) * 0.5f;
                    f.TiltZ += vertical * zSign /
                        Mathf.Max(0.01f, dragHalfZ) * 0.5f;
                }
                else if (edge >= 0 && !dragLinked &&
                    ResizeMidpointFromScreen(face, edge, mouse, ref f))
                {
                    // Projected midpoint movement already applied to f.
                    // The existing Shift snapping below handles unlocked X/Z.
                }
                else
                {
                    Ray ray = dragCamera.ScreenPointToRay(mouse);
                    float distance;
                    if (!dragPlane.Raycast(ray, out distance)) return;
                    Vector3 displacement = dragWorldToLocal.MultiplyVector(
                        ray.GetPoint(distance) - clickWorld);
                    if (dragRadial || dragLockXZ)
                    {
                        // Ctrl+Alt OR a normal drag with Lock X/Z: scale both
                        // dimensions together around the face center.
                        // Project mouse displacement onto the picked corner's
                        // original radial vector. One common factor changes X
                        // and Z, keeping the initial width/depth ratio intact.
                        float radiusX = xSign * dragHalfX * f.Width;
                        float radiusZ = zSign * dragHalfZ * f.Depth;
                        bool radialSnap = Input.GetKey(KeyCode.LeftShift) ||
                            Input.GetKey(KeyCode.RightShift);
                        float factor = (float)FaceMorphCore.RadialScaleFactor(
                            radiusX, radiusZ, displacement.x, displacement.z,
                            f.Width, f.Depth,
                            radialSnap ? ScaleUserSettings.MorphSnapIncrement : 0d);
                        f.Width *= factor;
                        f.Depth *= factor;
                    }
                    else if (dragLinked)
                    {
                        // A rigid X/Z translation of the chosen face:
                        // all four corners receive the SAME offset.
                        // Face dimensions, planar tilt and Y height are unchanged.
                        f.OffsetX += displacement.x / dragHalfX;
                        f.OffsetZ += displacement.z / dragHalfZ;
                    }
                    else if (edge < 0)
                    {
                        // Preserve the existing corner resize behavior.
                        f.Width += ((corner & 1) == 0 ? -1f : 1f) *
                            displacement.x / dragHalfX;
                        f.Depth += ((corner & 2) == 0 ? -1f : 1f) *
                            displacement.z / dragHalfZ;
                    }
                    else if (edge < 2)
                    {
                        f.Width += xSign * displacement.x / dragHalfX;
                    }
                    else
                    {
                        f.Depth += zSign * displacement.z / dragHalfZ;
                    }
                }
                // Quantize the change since drag start, not the stored value.
                // Only the properties modified in this gesture are snapped.
                bool snap = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (snap)
                {
                    float step = ScaleUserSettings.MorphSnapIncrement;
                    MorphFace original = face == 0 ? dragBottom : dragTop;
                    if (dragRadial || dragLockXZ)
                    {
                        // Already snapped the shared radial factor above;
                        // snapping Width and Depth separately would distort
                        // the face aspect ratio.
                    }
                    else if (dragLinked)
                    {
                        f.OffsetX = original.OffsetX + FaceMorphMath.SnapDelta(f.OffsetX - original.OffsetX, step);
                        f.OffsetZ = original.OffsetZ + FaceMorphMath.SnapDelta(f.OffsetZ - original.OffsetZ, step);
                    }
                    else if (dragTilt)
                    {
                        f.TiltX = original.TiltX + FaceMorphMath.SnapDelta(f.TiltX - original.TiltX, step);
                        f.TiltZ = original.TiltZ + FaceMorphMath.SnapDelta(f.TiltZ - original.TiltZ, step);
                    }
                    else
                    {
                        f.Width = original.Width + FaceMorphMath.SnapDelta(f.Width - original.Width, step);
                        f.Depth = original.Depth + FaceMorphMath.SnapDelta(f.Depth - original.Depth, step);
                    }
                }
                f = FaceMorphMath.Clamp(f);
                if (face == 0) b = f; else top = f;
                // Morph KSPFields are written directly, unlike stock PAW
                // fields. Record their initial state before the first actual
                // change, so Ctrl+Z restores the entire drag in one step.
                if (!undoCheckpointTaken &&
                    (FacesDiffer(b, module.GetMorphFace(0)) ||
                     FacesDiffer(top, module.GetMorphFace(1))))
                {
                    // Even if KSP declines the backup, avoid retrying/logging
                    // on every frame of this one drag.
                    undoCheckpointTaken = true;
                    EditorUndoSupport.RecordBeforeChange();
                }
                module.SetMorphFaces(b, top);
                return;
            }
            hovered = -1;
            bool overUI = EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject();
            if (!overUI && mouse.x >= 0 && mouse.y >= 0 &&
                mouse.x < Screen.width && mouse.y < Screen.height)
            {
                float best = HitRadius * HitRadius;
                for (int i = 0; i < HandleCount; ++i)
                {
                    float dist = (screen[i] - mouse).sqrMagnitude;
                    if (visible[i] && dist < best) { hovered = i; best = dist; }
                }
            }
            Lock(hovered >= 0);
            if (hovered >= 0 && Input.GetMouseButtonDown(0) && locked && camera != null)
            {
                dragging = hovered;
                undoCheckpointTaken = false;
                bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                // Capture mode on mouse-down so a modifier released during
                // the drag cannot jump between radial, translation or tilt.
                // Ctrl+Alt takes precedence even when Shift is held, so
                // Ctrl+Alt+Shift means radial movement WITH snapping.
                dragRadial = alt && ctrl;
                dragLinked = alt && shift && !ctrl;
                dragTilt = alt && !shift && !ctrl;
                // Shift alone may snap a locked resize; Alt modes are unchanged.
                dragLockXZ = !alt && module.lockMorphXZ;
                dragBottom = module.GetMorphFace(0);
                dragTop = module.GetMorphFace(1);
                clickMouse = mouse;
                clickWorld = position[hovered];
                dragCamera = camera;
                dragLocalToWorld = module.part.transform.localToWorldMatrix;
                dragWorldToLocal = module.part.transform.worldToLocalMatrix;
                FaceMorphSupport m = module.FaceMorph;
                dragHalfX = Mathf.Max(0.01f, (m.Max.x - m.Min.x) * 0.5f);
                dragHalfZ = Mathf.Max(0.01f, (m.Max.z - m.Min.z) * 0.5f);
                clickHandleScreen = ProjectAtDragStart(
                    LocalHandlePosition(hovered, dragBottom, dragTop));
                if (dragTilt)
                {
                    // Projection of the selected handle's REAL tilt response
                    // in the rotation already applied to this part, not the
                    // screen/world Y direction or its old flat orientation.
                    Vector2 projectedUp = ProjectTiltMotion(hovered);
                    dragTiltScreenUp = projectedUp.sqrMagnitude > 0.001f ?
                        projectedUp.normalized : Vector2.zero;
                    dragTiltPixelsPerUnit = Mathf.Max(12f, projectedUp.magnitude);
                    int editedFace = hovered < CornerCount ? hovered / 4 :
                        (hovered - CornerCount) / 4;
                    dragTiltSign = FaceMorphCore.TiltDragSign(
                        ModulePartSculptor.CopyAttachedMorphStepsGlobal,
                        IsParentAttachedFace(editedFace));
                }
                // The drag plane follows the part's CURRENT local X/Z face
                // plane at mouse-down, not global horizontal, and remains
                // stable even if Copy mode rotates the child during this drag.
                Vector3 planeNormal = dragLocalToWorld.MultiplyVector(Vector3.up);
                dragPlane = new Plane(planeNormal.normalized, clickWorld);
            }
        }

        // The 3D cubes/cage remain for perspective, while this screen-space
        // version is deliberately independent of the KSP camera's object
        // culling, shaders and depth test (which can hide back-face corners).
        // DrawTexture creates no UI picking surface, so Update() still owns
        // hit testing against the projected coordinates.
        private static void DrawScreenLine(Vector2 from, Vector2 to, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.5f) return;
            Vector2 middle = (from + to) * 0.5f;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) *
                Mathf.Rad2Deg, middle);
            GUI.color = color;
            GUI.DrawTexture(new Rect(middle.x - length * 0.5f,
                middle.y - 1.5f, length, 3f), Texture2D.whiteTexture);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void DrawLabels(Rect helpRect)
        {
            if (disposed || !HighLogic.LoadedSceneIsEditor) return;
            if (camera == null)
            {
                DrawHelpBox(helpRect,
                    "Face morph: no editor camera can see the selected part");
                return;
            }
            // Always paint over the editor's 3D geometry. An instantiated
            // Canvas is not proof that any handles are actually visible.
            if (Event.current.type == EventType.Repaint)
            {
                Color lower = new Color(1f, 0.55f, 0.12f, 1f);
                Color upper = new Color(0.12f, 0.85f, 1f, 1f);
                Color longitudinal = new Color(0.95f, 0.95f, 0.95f, 0.85f);
                int[,] pairs = {
                    {0,1},{1,3},{3,2},{2,0}, {4,5},{5,7},{7,6},{6,4},
                    {0,4},{1,5},{2,6},{3,7}
                };
                for (int i = 0; i < 12; ++i)
                {
                    int a = pairs[i, 0], b = pairs[i, 1];
                    if (!visible[a] || !visible[b]) continue;
                    DrawScreenLine(new Vector2(screen[a].x, Screen.height - screen[a].y),
                        new Vector2(screen[b].x, Screen.height - screen[b].y),
                        i < 4 ? lower : i < 8 ? upper : longitudinal);
                }
                Color previous = GUI.color;
                for (int i = 0; i < HandleCount; ++i)
                {
                    if (!visible[i]) continue;
                    bool isMidpoint = i >= CornerCount;
                    float radius = (i == hovered || i == dragging) ?
                        (isMidpoint ? 9f : 10f) : (isMidpoint ? 5.5f : 7f);
                    GUI.color = Color.black;
                    GUI.DrawTexture(new Rect(screen[i].x - radius - 2f,
                        Screen.height - screen[i].y - radius - 2f,
                        radius * 2f + 4f, radius * 2f + 4f), Texture2D.whiteTexture);
                    GUI.color = i < 4 ? lower : upper;
                    GUI.DrawTexture(new Rect(screen[i].x - radius,
                        Screen.height - screen[i].y - radius,
                        radius * 2f, radius * 2f), Texture2D.whiteTexture);
                }
                GUI.color = previous;
            }
            if (dragging < 0)
            {
                int onCamera = 0;
                for (int i = 0; i < visible.Length; ++i)
                    if (visible[i]) ++onCamera;
                if (onCamera == 0)
                {
                    DrawHelpBox(helpRect,
                        "Morph handles are behind the editor camera. Orbit or zoom " +
                        "the camera to bring the selected part into view.");
                    return;
                }
                DrawHelpBox(helpRect,
                    "Face morph:\n" +
                    "  corners = X/Z taper\n" +
                    "  edge midpoints = one axis\n" +
                    "  Alt = tilt\n" +
                    "  Shift = snap\n" +
                    "  Shift+Alt = move face\n" +
                    "  Ctrl+Alt = radial resize\n" +
                    "  Ctrl+Alt+Shift = radial snap\n" +
                    "  Lock X/Z = " + (module.lockMorphXZ ? "On" : "Off") + " (part PAW)\n" +
                    "  Bottom tilt: " + module.GetFaceTiltDisplay(0) + "\n" +
                    "  Top tilt: " + module.GetFaceTiltDisplay(1));
                return;
            }
            Vector3 mouse = Input.mousePosition;
            int editedFace = dragging < 4 || (dragging >= 8 && dragging < 12) ? 0 : 1;
            string caption = dragRadial ? "Radial resize all corners (Esc to cancel)" :
                dragLinked ? "Move entire face (Esc to cancel)" :
                dragTilt ? "Tilt face (Esc to cancel)" :
                dragLockXZ ? "Resize X/Z together (Esc to cancel)" :
                dragging >= CornerCount ? "Resize face from edge (Esc to cancel)" :
                "Resize face (Esc to cancel)";
            // Two lines need more space than the original 25px tip. Retain
            // DrawMorphTip so drag labels honor the user's background-opacity
            // setting, and clamp the larger tooltip to the editor viewport.
            Rect tipRect = new Rect(mouse.x + 20f, Screen.height - mouse.y + 20f,
                355f, 56f);
            tipRect.x = Mathf.Clamp(tipRect.x, 0f,
                Mathf.Max(0f, Screen.width - tipRect.width));
            tipRect.y = Mathf.Clamp(tipRect.y, 0f,
                Mathf.Max(0f, Screen.height - tipRect.height));
            DrawMorphTip(tipRect,
                caption + "\n" +
                (editedFace == 0 ? "Bottom" : "Top") +
                " tilt: " + module.GetFaceTiltDisplay(editedFace), GUI.skin.box);
        }

        private void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Lock(false);
            if (root != null) UnityEngine.Object.Destroy(root);
            for (int i = 0; i < materials.Length; ++i)
                if (materials[i] != null) UnityEngine.Object.Destroy(materials[i]);
        }
    }
}
