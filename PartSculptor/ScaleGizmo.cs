// PartSculptor's editor-only, three-axis direct-manipulation gizmo.
// No UnityEditor dependency: this uses ordinary Unity runtime renderers.
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PartSculptor
{
    internal sealed class ScaleGizmo
    {
        private const float HitRadiusPixels = 19f;
        private const string LockName = "PartSculptor_EditorGizmo";
        private static readonly Color[] Colors =
        {
            new Color(1f, 0.30f, 0.25f, 1f),  // local X
            new Color(0.27f, 1f, 0.40f, 1f),  // local Y
            new Color(0.35f, 0.65f, 1f, 1f)   // local Z
        };
        private static readonly Vector3[] LocalDirections =
            { Vector3.right, Vector3.up, Vector3.forward };

        private static ScaleGizmo active;
        private readonly ModulePartSculptor module;
        private readonly GameObject root;
        private readonly LineRenderer[] lines = new LineRenderer[3];
        private readonly Transform[] handles = new Transform[3];
        private readonly Material[] materials = new Material[3];
        private readonly Vector3[] worldHandles = new Vector3[3];
        private Vector2 screenCenter;
        private bool centerVisible;
        private readonly Vector2[] screenHandles = new Vector2[3];
        private readonly Vector2[] screenDirections = new Vector2[3];
        private readonly bool[] handleVisible = new bool[3];
        private Camera editorCamera;
        private int hoveredAxis = -1;
        private int draggingAxis = -1;
        private bool inputLocked;
        // Mouse position relative to the grabbed handle, so picking an edge
        // does not make the handle jump to the cursor center.
        private Vector2 dragClickOffset;
        private readonly float[] pixelsPerUnitScale = new float[3];
        private Vector3 dragStartScale;
        private float lastAppliedScale;
        private bool disposed;

        // The editor Stretch tool must not interpret a handle drag as a new
        // scene-part selection, even though the gizmo cubes have no colliders.
        internal static bool IsDragging
        {
            get { return active != null && active.draggingAxis >= 0; }
        }

        internal static bool IsPointerOnHandleOrDragging()
        {
            if (active == null || active.disposed) return false;
            if (active.draggingAxis >= 0) return true;
            Vector3 mouse = Input.mousePosition;
            Vector2 pointer = new Vector2(mouse.x, mouse.y);
            for (int i = 0; i < 3; ++i)
                if (active.handleVisible[i] &&
                    (pointer - active.screenHandles[i]).sqrMagnitude <=
                    HitRadiusPixels * HitRadiusPixels)
                    return true;
            return false;
        }

        internal static void Show(ModulePartSculptor module)
        {
            if (IsFor(module)) return;
            Hide();
            if (module != null && module.CanUseScaleGizmo)
            {
                active = new ScaleGizmo(module);
                module.UpdateGizmoPAWLabel();
            }
        }

        internal static bool IsFor(ModulePartSculptor module)
        {
            return active != null && active.module == module && !active.disposed;
        }

        internal static void Toggle(ModulePartSculptor module)
        {
            if (IsFor(module)) { Hide(); return; }
            Hide();
            if (module != null && module.CanUseScaleGizmo)
            {
                active = new ScaleGizmo(module);
                module.UpdateGizmoPAWLabel();
            }
        }

        internal static void HideFor(ModulePartSculptor module)
        {
            if (IsFor(module)) Hide();
        }

        internal static void Hide()
        {
            if (active == null) return;
            ScaleGizmo old = active;
            active = null;
            old.Dispose();
            if (old.module != null) old.module.UpdateGizmoPAWLabel();
        }

        internal static void UpdateFor(ModulePartSculptor module)
        {
            if (IsFor(module)) active.Update();
        }

        // Draw from EditorStretchTool.OnGUI rather than the PartModule, so
        // the markers render consistently above attached parts in the editor.
        internal static void DrawActive()
        {
            if (active != null && !active.disposed) active.DrawLabels();
        }

        private ScaleGizmo(ModulePartSculptor selected)
        {
            module = selected;
            root = new GameObject("PartSculptor Editor Gizmo");
            root.hideFlags = HideFlags.DontSave;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ??
                Shader.Find("Diffuse");
            for (int i = 0; i < 3; ++i)
            {
                GameObject lineObject = new GameObject("PartSculptor axis " + "XYZ"[i]);
                lineObject.transform.SetParent(root.transform, false);
                LineRenderer line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = 0.045f;
                line.endWidth = 0.045f;
                line.startColor = Colors[i];
                line.endColor = Colors[i];
                materials[i] = new Material(shader);
                materials[i].color = Colors[i];
                line.material = materials[i];
                lines[i] = line;

                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "PartSculptor " + "XYZ"[i] + " handle";
                cube.transform.SetParent(root.transform, false);
                Collider collider = cube.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false; // Never obstruct KSP part raycasts.
                    UnityEngine.Object.Destroy(collider);
                }
                Renderer r = cube.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = materials[i];
                handles[i] = cube.transform;
            }
        }

        private static bool CameraSees(Camera camera, Vector3 point)
        {
            if (camera == null || !camera.isActiveAndEnabled) return false;
            Vector3 v = camera.WorldToViewportPoint(point);
            return v.z > 0.01f && v.x > -0.5f && v.x < 1.5f &&
                v.y > -0.5f && v.y < 1.5f;
        }

        private Camera FindCamera(Vector3 point)
        {
            // The editor runs multiple cameras. A UI camera may project the
            // position but cannot render the part itself, so check its layer.
            int partLayer = module.part.gameObject.layer;
            int mask = 1 << partLayer;
            if (CameraSees(editorCamera, point) && (editorCamera.cullingMask & mask) != 0)
                return editorCamera;
            Camera candidate = Camera.main;
            if (CameraSees(candidate, point) && (candidate.cullingMask & mask) != 0)
                return candidate;
            foreach (Camera c in Camera.allCameras)
                if (CameraSees(c, point) && (c.cullingMask & mask) != 0 &&
                    c.name.IndexOf("UI", StringComparison.OrdinalIgnoreCase) < 0)
                    return c;
            return null;
        }

        private void UpdateGeometry()
        {
            Part part = module.part;
            Transform t = part.transform;
            Vector3 center = t.position;
            Vector3 extent = Vector3.one * 0.35f;
            bool hasBounds = false;
            Transform model = t.Find("model");
            if (model != null)
            {
                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
                {
                    if (renderer == null || !renderer.enabled || renderer is ParticleSystemRenderer)
                        continue;
                    if (!hasBounds)
                    {
                        center = renderer.bounds.center;
                        extent = renderer.bounds.extents;
                        hasBounds = true;
                    }
                    else
                    {
                        Bounds b = new Bounds(center, extent * 2f);
                        b.Encapsulate(renderer.bounds);
                        center = b.center;
                        extent = b.extents;
                    }
                }
            }
            editorCamera = FindCamera(center);
            Vector3 centerScreen = editorCamera != null ?
                editorCamera.WorldToScreenPoint(center) : Vector3.zero;
            screenCenter = new Vector2(centerScreen.x, centerScreen.y);
            centerVisible = editorCamera != null && centerScreen.z > 0f;
            float distance = editorCamera != null ?
                Vector3.Distance(editorCamera.transform.position, center) : 5f;
            float handleSize = Mathf.Clamp(distance * 0.018f, 0.10f, 0.55f);
            // Keep the handles farther from the part without changing the size
            // of their clickable cubes. Stem length does NOT change when the
            // part is resized; drag sensitivity must not depend on it.
            float stemExtra = Mathf.Max(handleSize * 2f, 0.25f) * 3f;
            for (int i = 0; i < 3; ++i)
            {
                Vector3 direction = t.TransformDirection(LocalDirections[i]).normalized;
                float radius = Mathf.Abs(direction.x) * extent.x +
                    Mathf.Abs(direction.y) * extent.y + Mathf.Abs(direction.z) * extent.z;
                // Use part bounds for long tanks as well as small radial parts.
                Vector3 end = center + direction * (radius + stemExtra);
                worldHandles[i] = end;
                lines[i].SetPosition(0, center);
                lines[i].SetPosition(1, end);
                handles[i].position = end;
                handles[i].rotation = t.rotation;
                handles[i].localScale = Vector3.one * handleSize *
                    (draggingAxis == i || hoveredAxis == i ? 1.35f : 1f);
                Vector3 screen = editorCamera != null ?
                    editorCamera.WorldToScreenPoint(end) : Vector3.zero;
                Vector2 screenDelta = new Vector2(screen.x - centerScreen.x,
                    screen.y - centerScreen.y);
                handleVisible[i] = editorCamera != null && screen.z > 0f &&
                    centerScreen.z > 0f && screenDelta.sqrMagnitude >= 16f;
                screenHandles[i] = new Vector2(screen.x, screen.y);
                screenDirections[i] = handleVisible[i] ? screenDelta.normalized : Vector2.up;
                // The part's radius grows with its scale; the extra handle stem
                // does not. Determine pixels/scale from a small WORLD-space
                // displacement of the actual handle, not its total screen
                // distance from the part center. Recompute as the view moves.
                float axisScale = i == 0 ? module.GizmoScale.x :
                    i == 1 ? module.GizmoScale.y : module.GizmoScale.z;
                float scaleStep = 0.02f;
                float worldStep = radius / Mathf.Max(axisScale, 0.05f) * scaleStep;
                Vector3 sample = editorCamera != null ?
                    editorCamera.WorldToScreenPoint(end + direction * worldStep) : Vector3.zero;
                float projected = Vector2.Dot(new Vector2(sample.x - screen.x,
                    sample.y - screen.y), screenDirections[i]);
                // A nearly camera-aligned axis projects to very few pixels.
                // Keep a safe minimum sensitivity to avoid a giant scale jump
                // from a one-pixel mouse movement at these angles.
                pixelsPerUnitScale[i] = handleVisible[i] && sample.z > 0f ?
                    Mathf.Max(12f, projected / scaleStep) : 0f;
                lines[i].enabled = handleVisible[i];
                handles[i].gameObject.SetActive(handleVisible[i]);
            }
        }

        private static ControlTypes EditorPickLock()
        {
            ControlTypes result;
            if (Enum.TryParse("EDITOR_PAD_PICK_PLACE", out result) &&
                Enum.IsDefined(typeof(ControlTypes), result)) return result;
            if (Enum.TryParse("EDITOR_LOCK", out result) &&
                Enum.IsDefined(typeof(ControlTypes), result)) return result;
            return (ControlTypes)0;
        }

        private void LockInput(bool wanted)
        {
            if (wanted == inputLocked) return;
            if (wanted)
            {
                ControlTypes mask = EditorPickLock();
                if (mask == (ControlTypes)0) return;
                InputLockManager.SetControlLock(mask, LockName);
                inputLocked = true;
            }
            else
            {
                InputLockManager.RemoveControlLock(LockName);
                inputLocked = false;
            }
        }

        private void Update()
        {
            if (disposed || !HighLogic.LoadedSceneIsEditor || !module.CanUseScaleGizmo ||
                module.part == null)
            {
                Hide();
                return;
            }
            UpdateGeometry();
            Vector3 rawMouse = Input.mousePosition;
            Vector2 mouse = new Vector2(rawMouse.x, rawMouse.y);
            bool overUI = EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject();

            if (draggingAxis >= 0)
            {
                LockInput(true);
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                {
                    module.SetGizmoScale(dragStartScale);
                    draggingAxis = -1;
                    hoveredAxis = -1;
                    LockInput(false);
                    return;
                }
                if (!Input.GetMouseButton(0))
                {
                    draggingAxis = -1;
                    LockInput(false);
                    return;
                }
                if (!handleVisible[draggingAxis] || pixelsPerUnitScale[draggingAxis] <= 0f)
                    return;
                // Drive the handle toward the actual mouse position in screen
                // space. The renderer bounds, camera and part position can move
                // while scaling, so the start-of-drag fixed pixel ratio drifted
                // increasingly far from the cursor (especially with long stems).
                // Retain the original grab offset to prevent a jump on click.
                Vector2 handleError = (mouse - dragClickOffset) -
                    screenHandles[draggingAxis];
                double projectedError = Vector2.Dot(handleError,
                    screenDirections[draggingAxis]);
                bool snap = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                double baseline = draggingAxis == 0 ? dragStartScale.x :
                    draggingAxis == 1 ? dragStartScale.y : dragStartScale.z;
                Vector3 currentAxes = module.GizmoScale;
                double current = draggingAxis == 0 ? currentAxes.x :
                    draggingAxis == 1 ? currentAxes.y : currentAxes.z;
                float desired = (float)ScaleMath.GizmoFollowValue(baseline,
                    current, projectedError, pixelsPerUnitScale[draggingAxis],
                    Settings.MinScale, Settings.MaxScale, snap ? 0.05d : 0d);
                if (Mathf.Abs(desired - lastAppliedScale) < 0.0025f) return;
                lastAppliedScale = desired;
                module.SetGizmoAxis(draggingAxis, desired);
                // Update rendered endpoints immediately after resizing rather
                // than drawing them at the previous frame's part dimensions.
                UpdateGeometry();
                return;
            }

            hoveredAxis = -1;
            if (!overUI && rawMouse.x >= 0f && rawMouse.y >= 0f &&
                rawMouse.x < Screen.width && rawMouse.y < Screen.height)
            {
                float closest = HitRadiusPixels * HitRadiusPixels;
                for (int i = 0; i < 3; ++i)
                {
                    float dist = (screenHandles[i] - mouse).sqrMagnitude;
                    if (handleVisible[i] && dist <= closest)
                    {
                        closest = dist;
                        hoveredAxis = i;
                    }
                }
            }
            // Acquire the editor pickup lock on hover, BEFORE starting the drag.
            // We do not lock camera movement or part-picking elsewhere.
            LockInput(hoveredAxis >= 0);
            if (hoveredAxis >= 0 && Input.GetMouseButtonDown(0) && inputLocked)
            {
                draggingAxis = hoveredAxis;
                dragStartScale = module.GizmoScale;
                dragClickOffset = mouse - screenHandles[draggingAxis];
                lastAppliedScale = draggingAxis == 0 ? dragStartScale.x :
                    draggingAxis == 1 ? dragStartScale.y : dragStartScale.z;
            }
        }

        private static void DrawForegroundLine(Vector2 from, Vector2 to, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.5f) return;
            Vector2 center = (from + to) * 0.5f;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) *
                Mathf.Rad2Deg, center);
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x-length*0.5f, center.y-2f,
                length, 4f), Texture2D.whiteTexture);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void DrawLabels()
        {
            if (disposed || !HighLogic.LoadedSceneIsEditor || editorCamera == null)
                return;
            // World-space cubes and lines are depth-tested, so an adjacent
            // attached part can hide the axis handles. Render an additional
            // non-interactive IMGUI copy using the EXACT positions used for
            // mouse picking. Unlike raycastable UI, this cannot steal clicks.
            if (Event.current.type == EventType.Repaint)
            {
                for (int i = 0; i < 3; ++i)
                {
                    if (!handleVisible[i]) continue;
                    Vector2 handle = new Vector2(screenHandles[i].x,
                        Screen.height - screenHandles[i].y);
                    if (centerVisible)
                    {
                        Vector2 from = new Vector2(screenCenter.x,
                            Screen.height - screenCenter.y);
                        DrawForegroundLine(from, handle, Colors[i]);
                    }
                    float half = (hoveredAxis == i || draggingAxis == i) ? 11f : 8f;
                    Color old = GUI.color;
                    GUI.color = Color.black;
                    GUI.DrawTexture(new Rect(handle.x-half-2f, handle.y-half-2f,
                        2f*half+4f, 2f*half+4f), Texture2D.whiteTexture);
                    GUI.color = Colors[i];
                    GUI.DrawTexture(new Rect(handle.x-half, handle.y-half,
                        2f*half, 2f*half), Texture2D.whiteTexture);
                    GUI.color = old;
                }
            }
            Vector3 scale = module.GizmoScale;
            for (int i = 0; i < 3; ++i)
            {
                if (!handleVisible[i]) continue;
                Vector2 screen = screenHandles[i];
                float y = Screen.height - screen.y;
                float value = i == 0 ? scale.x : i == 1 ? scale.y : scale.z;
                GUI.Label(new Rect(screen.x + 12f, y - 16f, 100f, 24f),
                    "XYZ"[i] + ": " + value.ToString("0.###"), GUI.skin.box);
            }
            if (draggingAxis >= 0)
            {
                Vector3 mouse = Input.mousePosition;
                GUI.Label(new Rect(mouse.x + 18f, Screen.height - mouse.y + 18f,
                    230f, 26f), "Drag to scale; Shift = 0.05 steps; Esc = cancel",
                    GUI.skin.box);
            }
        }

        private void Dispose()
        {
            if (disposed) return;
            disposed = true;
            LockInput(false);
            if (root != null) UnityEngine.Object.Destroy(root);
            foreach (Material material in materials)
                if (material != null) UnityEngine.Object.Destroy(material);
        }
    }
}
