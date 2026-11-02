// Sixteen handles: four corners plus four edge midpoints on each END FACE.
// Drag a corner to resize a face in X/Z; Alt tilts the face, Shift+Alt
// translates the chosen face, and Ctrl+Alt uniformly expands/contracts it
// around its center axis, preserving the ratio between X and Z dimensions.
// Uses runtime Unity objects, never UnityEditor classes.
using System;
using UnityEngine;
using UnityEngine.EventSystems;

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
        private MorphFace dragBottom, dragTop;
        private Vector2 clickMouse;
        private Vector3 clickWorld;
        private Plane dragPlane;
        private float dragHalfX, dragHalfZ;

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
                Debug.LogWarning("[PartSculptor] Face morph is not supported for " +
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
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.wordWrap = true;
            style.alignment = TextAnchor.UpperLeft;
            style.padding = new RectOffset(6, 6, 5, 5);
            float height = style.CalcHeight(new GUIContent(message), bounds.width);
            bounds.height = Mathf.Max(25f, height);
            // Keep the help panel on screen, even with larger KSP UI scales.
            bounds.height = Mathf.Min(bounds.height, Mathf.Max(0f, Screen.height - bounds.y));
            if (bounds.width > 0f && bounds.height > 0f)
                GUI.Label(bounds, message, style);
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
                    Vector3 baseline = camera.WorldToScreenPoint(clickWorld);
                    Vector3 raised = camera.WorldToScreenPoint(clickWorld +
                        module.part.transform.TransformDirection(Vector3.up));
                    Vector2 up = new Vector2(raised.x-baseline.x, raised.y-baseline.y);
                    float pixelsPerUnit = Mathf.Max(12f, up.magnitude);
                    float vertical = Vector2.Dot(mouse - clickMouse,
                        up.sqrMagnitude > 0.001f ? up.normalized : Vector2.up) / pixelsPerUnit;
                    // A corner tilts in both directions; an edge midpoint
                    // tilts only across the face axis normal to that edge.
                    f.TiltX += vertical * xSign /
                        Mathf.Max(0.01f, dragHalfX) * 0.5f;
                    f.TiltZ += vertical * zSign /
                        Mathf.Max(0.01f, dragHalfZ) * 0.5f;
                }
                else
                {
                    Ray ray = camera.ScreenPointToRay(mouse);
                    float distance;
                    if (!dragPlane.Raycast(ray, out distance)) return;
                    Vector3 displacement = module.part.transform.InverseTransformVector(
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
                FaceMorphSupport m = module.FaceMorph;
                dragHalfX = Mathf.Max(0.01f,(m.Max.x-m.Min.x)*0.5f);
                dragHalfZ = Mathf.Max(0.01f,(m.Max.z-m.Min.z)*0.5f);
                dragPlane = new Plane(module.part.transform.up, clickWorld);
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
                    "Face morph: corners = X/Z taper, edge midpoints = one axis;\n" +
                    "Alt = tilt; Shift = snap;\n" +
                    "Shift+Alt = move face; Ctrl+Alt = radial resize;\n" +
                    "Ctrl+Alt+Shift = radial snap; Lock X/Z = " +
                    (module.lockMorphXZ ? "On" : "Off") + " (part PAW)");
                return;
            }
            Vector3 mouse = Input.mousePosition;
            GUI.Label(new Rect(mouse.x + 20f, Screen.height - mouse.y + 20f, 300f, 25f),
                dragRadial ? "Radial resize all corners (Esc to cancel)" :
                dragLinked ? "Move entire face (Esc to cancel)" :
                dragTilt ? "Tilt face (Esc to cancel)" :
                dragLockXZ ? "Resize X/Z together (Esc to cancel)" :
                dragging >= CornerCount ? "Resize face from edge (Esc to cancel)" :
                "Resize face (Esc to cancel)", GUI.skin.box);
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
