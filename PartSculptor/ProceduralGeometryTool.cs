// IMGUI controls for the seventh editor tool. The part's mesh is subdivided
// at real Y-plane intersections; parameters are committed only on button clicks
// to create exactly one KSP undo checkpoint for each operation.
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PartSculptor
{
    internal sealed class ProceduralGeometryTool
    {
        private static readonly ProceduralGeometryTool instance = new ProceduralGeometryTool();
        private ModulePartSculptor selectedModule;
        private int selectedRing;
        private string heightText = "50", widthText = "1", depthText = "1";
        private string status = "Select a part to add cross-sections";
        private Rect panel;
        private int dragging = -1;
        private Vector2 dragStart, dragAxis, dragDepthAxis;
        private const int HandleCount = 9;
        private ProceduralRing originalRing, previewRing;
        private int dragRingIndex;
        private bool dragVertical;
        private bool dragTilt;
        private const float HandleRadius = 9f;
        private const string HelpTooltipPrefix = "PartSculptor.PG.Help:";

        private static GUIContent HelpContent(string text, string tooltip)
        {
            return new GUIContent(text, HelpTooltipPrefix + tooltip);
        }

        private static string HoveredHelpTooltip()
        {
            string raw = GUI.tooltip;
            return !string.IsNullOrEmpty(raw) && raw.StartsWith(HelpTooltipPrefix, StringComparison.Ordinal)
                ? raw.Substring(HelpTooltipPrefix.Length) : null;
        }

        private void DrawHelpTooltip(string tooltip)
        {
            if (Event.current.type == EventType.Repaint && !IsDragging &&
                panel.Contains(Event.current.mousePosition) && !string.IsNullOrEmpty(tooltip))
                FaceMorphGizmo.DrawHoverTooltip(tooltip, Event.current.mousePosition);
        }
        private bool vertexMode, addingVertex, addingEdgeVertex;
        private int selectedVertex = -1, draggingVertex = -1;
        private bool vertexVertical;
        private int vertexDragAxis = -1; // -1 = free X/Z or Alt Y; 0/1/2 = axis
        private const float VertexGizmoLength = 84f;
        private string radiusText = "0.25";
        private bool softSelection;
        private float radiusSlider = 0.25f, radiusSliderLimit = 1f;
        private int mirrorAxis = -1; // Off, X, Y or Z (one plane at a time)
        private int dragMirrorAxis = -1;
        private double[] dragMirrorWeights;
        private float[] dragMirrorSigns;
        private float displayedRadius = -1f;
        private Vector2 vertexPanelScroll;
        private string displayedMeshKey;
        private Vector3 displayedAnchor;
        private static readonly Color[] VertexAxisColors = {
            Color.red, Color.green, new Color(0.25f, 0.6f, 1f) };
        private readonly List<int> selectedVertices = new List<int>();
        private List<ProceduralVertex> selectionIdentity = new List<ProceduralVertex>();
        private List<ProceduralVertex> dragEdits;
        private int[] dragSelection;
        private Transform[] dragTransforms;
        private double[] dragWeights;
        private Vector3 vertexGroupDelta;
        private Vector2 vertexDragStart, vertexScreenX, vertexScreenY, vertexScreenZ;

        private static bool AltHeld()
        { return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt); }

        internal static bool IsDragging { get { return instance.dragging >= 0 || instance.draggingVertex >= 0; } }
        internal static bool UpdateHandles(ModulePartSculptor module)
        { return instance.UpdateHandlesInternal(module); }

        private void CancelDrag()
        {
            dragging = -1; draggingVertex = -1;
            dragEdits = null; dragSelection = null; dragTransforms = null; dragWeights = null;
            vertexGroupDelta = Vector3.zero;
            dragMirrorWeights = null; dragMirrorSigns = null; dragMirrorAxis = -1;
        }


        internal static void Clear()
        {
            instance.CancelDrag(); instance.selectedModule = null; instance.status = "";
            instance.panel = new Rect(); instance.vertexMode = false;
            instance.addingVertex = false; instance.ResetVertexSelection(); instance.mirrorAxis = -1;
        }

        internal static bool IsPointerOverPanel(Vector3 pointer)
        {
            return instance.panel.width > 0 &&
                instance.panel.Contains(new Vector2(pointer.x, Screen.height - pointer.y));
        }

        private void Select(ModulePartSculptor module)
        {
            if (selectedModule == module) return;
            CancelDrag();
            selectedModule = module;
            selectedRing = 0;
            ResetVertexSelection(); addingVertex = false;
            status = module == null ? "Click a part to edit its geometry" : "";
            SyncFields();
        }

        private void SyncFields()
        {
            if (selectedModule == null || selectedModule.ProceduralRings.Count == 0)
            { heightText = "50"; widthText = "1"; depthText = "1"; return; }
            selectedRing = Mathf.Clamp(selectedRing, 0, selectedModule.ProceduralRings.Count - 1);
            ProceduralRing r = selectedModule.ProceduralRings[selectedRing];
            heightText = (r.Height * 100f).ToString("0.###", CultureInfo.InvariantCulture);
            widthText = r.Width.ToString("0.###", CultureInfo.InvariantCulture);
            depthText = r.Depth.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private void Commit(List<ProceduralRing> rings)
        {
            if (selectedModule == null) return;
            string error;
            if (!selectedModule.TrySetProceduralRings(rings, out error))
                status = "Cannot modify geometry: " + error;
            else { status = "Geometry updated"; SyncFields(); }
        }

        private void AddRing()
        {
            if (selectedModule == null) return;
            var rings = new List<ProceduralRing>(selectedModule.ProceduralRings);
            if (rings.Count >= ProceduralMeshBuilder.MaxRings)
            { status = "Maximum of six cross-sections"; return; }
            // Choose the largest gap between existing rings and the fixed end
            // faces; insertion does not depend on the last ring's position.
            float start = 0f, bestStart = 0f, bestGap = 0f;
            for (int i = 0; i <= rings.Count; ++i)
            {
                float end = i == rings.Count ? 1f : rings[i].Height;
                if (end - start > bestGap)
                { bestGap = end - start; bestStart = start; }
                start = end;
            }
            float at = bestStart + bestGap * 0.5f;
            rings.Add(new ProceduralRing(at, 1f, 1f));
            rings.Sort((a, b) => a.Height.CompareTo(b.Height));
            Commit(rings);
            if (status == "Geometry updated")
            {
                selectedRing = rings.FindIndex(r => Mathf.Abs(r.Height - at) < 0.00001f);
                SyncFields();
            }
        }

        private void ApplyRing()
        {
            if (selectedModule == null || selectedModule.ProceduralRings.Count == 0) return;
            float height, width, depth;
            if (!float.TryParse(heightText, NumberStyles.Float, CultureInfo.InvariantCulture,
                    out height) ||
                !float.TryParse(widthText, NumberStyles.Float, CultureInfo.InvariantCulture,
                    out width) ||
                !float.TryParse(depthText, NumberStyles.Float, CultureInfo.InvariantCulture,
                    out depth))
            { status = "Enter numeric height, X scale and Z scale"; return; }
            var rings = new List<ProceduralRing>(selectedModule.ProceduralRings);
            ProceduralRing existing = rings[selectedRing];
            rings[selectedRing] = new ProceduralRing(height * 0.01f, width, depth,
                existing.TiltX, existing.TiltZ);
            rings.Sort((a, b) => a.Height.CompareTo(b.Height));
            Commit(rings);
            if (status == "Geometry updated")
            {
                selectedRing = rings.FindIndex(r =>
                    Mathf.Abs(r.Height - height * 0.01f) < 0.00001f);
                SyncFields();
            }
        }

        private static void FieldRow(string label, ref string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(106f));
            value = GUILayout.TextField(value, GUILayout.Width(125f));
            GUILayout.EndHorizontal();
        }

        // The preview uses the same local geometry mapping as the existing
        // outline. A drag changes only the preview until release, avoiding
        // mesh/collider reconstruction on every mouse movement.
        private static void DrawLine(Vector2 a, Vector2 b)
        {
            Matrix4x4 previous = GUI.matrix;
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.DrawTexture(new Rect(a.x, a.y, (b - a).magnitude, 2.5f),
                Texture2D.whiteTexture);
            GUI.matrix = previous;
        }

        private static Camera EditorCamera()
        {
            Camera camera = EditorLogic.fetch != null ? EditorLogic.fetch.editorCamera : null;
            return camera != null ? camera : Camera.main;
        }

        private static bool Project(ModulePartSculptor module, Camera camera,
            ProceduralRing ring, float x, float z, out Vector2 screen)
        {
            Vector3 low = module.FaceMorph.Min, high = module.FaceMorph.Max;
            Vector2 range = module.FaceMorph.PreviewRingYRange;
            float halfX = (high.x - low.x) * 0.5f;
            float halfZ = (high.z - low.z) * 0.5f;
            Vector3 point = new Vector3(
                (low.x + high.x) * 0.5f + halfX * x * ring.Width,
                Mathf.Lerp(range.x, range.y, ring.Height) +
                    RingCornerYOffset(ring, halfX * x, halfZ * z),
                (low.z + high.z) * 0.5f + halfZ * z * ring.Depth);
            point = FaceMorphMath.Transform(point, low, high,
                module.GetMorphFace(0), module.GetMorphFace(1));
            Vector3 projected = camera.WorldToScreenPoint(module.part.transform.TransformPoint(point));
            screen = new Vector2(projected.x, Screen.height - projected.y);
            return projected.z > 0f;
        }

        private static bool ProjectHandle(ModulePartSculptor module, Camera camera,
            ProceduralRing ring, int handle, out Vector2 screen)
        {
            if (handle >= 5)
            {
                int corner = handle - 5;
                return Project(module, camera, ring,
                    (corner & 1) == 0 ? -1f : 1f,
                    (corner & 2) == 0 ? -1f : 1f, out screen);
            }
            return Project(module, camera, ring,
                handle == 0 ? -1f : handle == 1 ? 1f : 0f,
                handle == 2 ? -1f : handle == 3 ? 1f : 0f, out screen);
        }

        private static ProceduralRing OffsetRing(ProceduralRing ring, int handle, float delta)
        {
            if (handle < 2) ring.Width += delta;
            else if (handle < 4) ring.Depth += delta;
            else if (handle == 4) ring.Height += delta;
            return ring;
        }

        private static ProceduralRing OffsetRingTilt(ProceduralRing ring, int handle, float delta,
            float halfX, float halfZ)
        {
            float xSign = handle == 0 ? -1f : handle == 1 ? 1f :
                handle >= 5 ? (((handle - 5) & 1) == 0 ? -1f : 1f) : 0f;
            float zSign = handle == 2 ? -1f : handle == 3 ? 1f :
                handle >= 5 ? (((handle - 5) & 2) == 0 ? -1f : 1f) : 0f;
            if (Mathf.Abs(xSign) > 0f)
                ring.TiltX += delta * xSign / Mathf.Max(0.01f, halfX) * 0.5f;
            if (Mathf.Abs(zSign) > 0f)
                ring.TiltZ += delta * zSign / Mathf.Max(0.01f, halfZ) * 0.5f;
            return ring;
        }

        private static float RingCornerYOffset(ProceduralRing ring, float x, float z)
        {
            return x * ring.Width * ring.TiltX + z * ring.Depth * ring.TiltZ;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        // Solve the two projected local axes together rather than projecting
        // independently onto potentially non-orthogonal screen directions.
        private static bool CornerAxesUsable(Vector2 x, Vector2 z)
        {
            return x.sqrMagnitude >= 16f && z.sqrMagnitude >= 16f &&
                Mathf.Abs(Cross(x, z)) >= 0.1f * x.magnitude * z.magnitude;
        }

        private bool TryGetRing(ModulePartSculptor module, out ProceduralRing ring)
        {
            ring = default(ProceduralRing);
            if (module == null || module.part == null || module.FaceMorph == null ||
                module.ProceduralRings.Count == 0) return false;
            selectedRing = Mathf.Clamp(selectedRing, 0, module.ProceduralRings.Count - 1);
            ring = dragging >= 0 ? previewRing : module.ProceduralRings[selectedRing];
            return true;
        }

        private bool CommitVertices(List<ProceduralVertex> edits)
        {
            string error;
            if (selectedModule == null) return false;
            if (!selectedModule.TrySetProceduralVertices(edits, out error))
            {
                status = "Cannot modify geometry: " + error;
                PartSculptorScreenMessages.Post(selectedModule.part, status,
                    Settings.screenMessageTime, ScreenMessageStyle.UPPER_CENTER,
                    "ProceduralGeometryTool.CommitVertices");
                return false;
            }
            status = "Geometry updated"; return true;
        }

        private static bool WorldScreen(Camera camera, Vector3 world, out Vector2 screen, out float depth)
        {
            Vector3 p = camera.WorldToScreenPoint(world);
            screen = new Vector2(p.x, Screen.height - p.y); depth = p.z;
            return p.z > 0f;
        }

        private void SelectVertex(IList<ProceduralVertex> edits, int index)
        {
            bool valid = index >= 0 && index < edits.Count;
            ProceduralVertex edit = valid ? edits[index] : default(ProceduralVertex);
            if (selectedVertex == index && displayedRadius == edit.InfluenceRadius &&
                displayedMeshKey == edit.MeshKey && displayedAnchor == edit.Anchor) return;
            selectedVertex = index;
            displayedRadius = edit.InfluenceRadius;
            displayedMeshKey = edit.MeshKey;
            displayedAnchor = edit.Anchor;
            softSelection = displayedRadius > 0f;
            radiusSlider = RoundRadius(softSelection ? displayedRadius : 0.25f);
            radiusSliderLimit = Mathf.Max(1f, radiusSlider);
            radiusText = FormatRadius(radiusSlider);
        }

        private static bool CtrlHeld()
        { return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl); }

        private void ResetVertexSelection()
        {
            selectedVertices.Clear(); selectionIdentity.Clear();
            selectedVertex = -1; displayedRadius = -1f; displayedMeshKey = null;
        }

        private static bool SameVertexIdentity(ProceduralVertex a, ProceduralVertex b)
        { return a.MeshKey == b.MeshKey && a.Submesh == b.Submesh && a.Anchor.Equals(b.Anchor); }

        private static bool SameVertexList(IList<ProceduralVertex> a, IList<ProceduralVertex> b, bool values)
        {
            if (a == null || b == null || a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; ++i)
                if (!SameVertexIdentity(a[i], b[i]) || (values &&
                    (!a[i].Offset.Equals(b[i].Offset) || a[i].InfluenceRadius != b[i].InfluenceRadius))) return false;
            return true;
        }

        private void SyncSelection(ModulePartSculptor module, IList<ProceduralVertex> edits)
        {
            if (!SameVertexList(selectionIdentity, edits, false))
            { ResetVertexSelection(); selectionIdentity = new List<ProceduralVertex>(edits); }
            selectedVertices.RemoveAll(i => {
                Vector3 world; Transform mesh;
                return !module.FaceMorph.TryGetProceduralVertexWorld(i, out world, out mesh);
            });
            SelectVertex(edits, selectedVertices.Count == 0 ? -1 : selectedVertices[selectedVertices.Count - 1]);
        }

        private void ChooseVertex(IList<ProceduralVertex> edits, int index, bool toggle)
        {
            if (!toggle) selectedVertices.Clear();
            if (index >= 0 && index < edits.Count)
            {
                if (toggle && selectedVertices.Contains(index)) selectedVertices.Remove(index);
                else selectedVertices.Add(index);
            }
            selectionIdentity = new List<ProceduralVertex>(edits);
            SelectVertex(edits, selectedVertices.Count == 0 ? -1 : selectedVertices[selectedVertices.Count - 1]);
        }

        private bool TryGroupCenter(ModulePartSculptor module, out Vector3 center)
        {
            center = Vector3.zero;
            if (selectedVertices.Count == 0) return false;
            foreach (int i in selectedVertices)
            {
                Vector3 world; Transform mesh;
                if (!module.FaceMorph.TryGetProceduralVertexWorld(i, out world, out mesh)) return false;
                center += world;
            }
            center /= selectedVertices.Count;
            return true;
        }

        // Screen-space arrows use a fixed size, but their directions and drag
        // derivatives are projected from the actual part-local axes.
        private static Vector2[] VertexAxes(ModulePartSculptor module, Camera camera,
            Vector3 world, out Vector2 center)
        {
            float ignored;
            if (!WorldScreen(camera, world, out center, out ignored)) return null;
            Vector3[] local = { Vector3.right, Vector3.up, Vector3.forward };
            Vector2[] axes = new Vector2[3];
            const float probe = 0.01f;
            for (int i = 0; i < 3; ++i)
            {
                Vector2 endpoint;
                if (WorldScreen(camera, world + module.part.transform.TransformVector(local[i]) * probe,
                    out endpoint, out ignored)) axes[i] = (endpoint - center) / probe;
            }
            return axes;
        }

        private static int PickVertexAxis(Vector2 pointer, Vector2 center, Vector2[] axes)
        {
            int nearest = -1;
            float best = 7f * 7f;
            if (axes == null || (pointer - center).sqrMagnitude < 11f * 11f) return -1;
            for (int i = 0; i < 3; ++i)
            {
                if (axes[i].sqrMagnitude < 16f) continue;
                Vector2 direction = axes[i].normalized;
                float along = Mathf.Clamp(Vector2.Dot(pointer - center, direction), 12f, VertexGizmoLength);
                float distance = (pointer - center - direction * along).sqrMagnitude;
                if (distance <= best) { best = distance; nearest = i; }
            }
            return nearest;
        }

        // A selected pair has one driver (the last selected member wins).
        // Normal-axis displacement is reflected, tangential motion is shared.
        // A self-mirrored vertex is constrained to the plane.
        private bool TryMirrorTargets(ModulePartSculptor module, out List<int> targets,
            out List<float> signs)
        {
            targets = new List<int>(); signs = new List<float>();
            IList<ProceduralVertex> edits = module.ProceduralVertices;
            for (int k = selectedVertices.Count - 1; k >= 0; --k)
            {
                int index = selectedVertices[k];
                if (targets.Contains(index)) continue;
                if (mirrorAxis < 0) { targets.Add(index); signs.Add(1f); continue; }
                int partner; ProceduralVertex mirrored; string error;
                if (!module.FaceMorph.TryMirrorVertex(edits[index], edits, mirrorAxis,
                    out partner, out mirrored, out error) || partner < 0)
                { status = partner < 0 && string.IsNullOrEmpty(error) ?
                    "Missing counterpart: use Create missing mirrors first" : error; return false; }
                if (targets.Contains(partner))
                { status = "Ambiguous mirror pairing"; return false; }
                targets.Add(index); signs.Add(partner == index ? 0f : 1f);
                if (partner != index) { targets.Add(partner); signs.Add(-1f); }
            }
            return targets.Count > 0;
        }

        private bool AddMirror(ModulePartSculptor module, List<ProceduralVertex> updated, int index)
        {
            int partner; ProceduralVertex mirrored; string error;
            if (!module.FaceMorph.TryMirrorVertex(updated[index], updated, mirrorAxis,
                out partner, out mirrored, out error)) { status = error; return false; }
            if (partner >= 0) return true;
            if (updated.Count >= ProceduralMeshBuilder.MaxIndividualVertices)
            { status = "Not enough room for the mirrored vertex (64 maximum)"; return false; }
            updated.Add(mirrored); return true;
        }

        private void CreateMissingMirrors(ModulePartSculptor module)
        {
            var updated = new List<ProceduralVertex>(module.ProceduralVertices);
            foreach (int index in selectedVertices)
                if (!AddMirror(module, updated, index)) return;
            if (updated.Count == module.ProceduralVertices.Count)
            { status = "All selected vertices already have mirrors"; return; }
            if (CommitVertices(updated))
                selectionIdentity = new List<ProceduralVertex>(updated);
        }

        private Vector3 MirrorPreview(ModulePartSculptor module, int index)
        {
            if (dragSelection == null) return Vector3.zero;
            int i = Array.IndexOf(dragSelection, index);
            if (i < 0) return Vector3.zero;
            Vector3 local = module.part.transform.InverseTransformVector(vertexGroupDelta);
            if (dragMirrorAxis >= 0) local[dragMirrorAxis] *= dragMirrorSigns[i];
            return module.part.transform.TransformVector(local);
        }

        private bool UpdateVertexHandles(ModulePartSculptor module)
        {
            if (module == null || module.FaceMorph == null || !module.FaceMorph.Valid)
            { CancelDrag(); addingVertex = false; return false; }
            IList<ProceduralVertex> edits = module.ProceduralVertices;
            Vector3 mouse = Input.mousePosition;
            Vector2 pointer = new Vector2(mouse.x, Screen.height - mouse.y);
            if (draggingVertex >= 0)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                { CancelDrag(); return true; }
                if (!Application.isFocused || !SameVertexList(dragEdits, edits, true))
                {
                    status = !Application.isFocused ? "Vertex drag cancelled: editor lost focus" :
                        "Vertex drag cancelled: geometry changed during dragging";
                    CancelDrag();
                    PartSculptorScreenMessages.Post(module.part, status, Settings.screenMessageTime,
                        ScreenMessageStyle.UPPER_CENTER, "ProceduralGeometryTool.DragCancelled");
                    return true;
                }
                for (int i = 0; i < dragSelection.Length; ++i)
                {
                    Vector3 world; Transform mesh;
                    if (dragTransforms[i] == null ||
                        !module.FaceMorph.TryGetProceduralVertexWorld(dragSelection[i], out world, out mesh) ||
                        mesh != dragTransforms[i])
                    {
                        status = "Vertex drag cancelled: selected mesh changed or disappeared";
                        CancelDrag();
                        PartSculptorScreenMessages.Post(module.part, status, Settings.screenMessageTime,
                            ScreenMessageStyle.UPPER_CENTER, "ProceduralGeometryTool.DragCancelled");
                        return true;
                    }
                }
                Vector2 displacement = pointer - vertexDragStart;
                Vector3 localDelta;
                if (vertexDragAxis >= 0)
                {
                    Vector2 axis = vertexDragAxis == 0 ? vertexScreenX :
                        vertexDragAxis == 1 ? vertexScreenY : vertexScreenZ;
                    Vector3 local = vertexDragAxis == 0 ? Vector3.right :
                        vertexDragAxis == 1 ? Vector3.up : Vector3.forward;
                    localDelta = local * (Vector2.Dot(displacement, axis) / axis.sqrMagnitude);
                }
                else if (vertexVertical)
                    localDelta = Vector3.up *
                        (Vector2.Dot(displacement, vertexScreenY) / vertexScreenY.sqrMagnitude);
                else
                {
                    float determinant = Cross(vertexScreenX, vertexScreenZ);
                    localDelta = Vector3.right * (Cross(displacement, vertexScreenZ) / determinant) +
                        Vector3.forward * (Cross(vertexScreenX, displacement) / determinant);
                }
                vertexGroupDelta = module.part.transform.TransformVector(localDelta);
                if (!Input.GetMouseButton(0))
                {
                    var updated = new List<ProceduralVertex>(dragEdits);
                    bool changed = false;
                    for (int i = 0; i < dragSelection.Length; ++i)
                    {
                        int index = dragSelection[i];
                        ProceduralVertex edit = updated[index];
                        if (dragMirrorAxis < 0)
                            edit.Offset += dragTransforms[i].InverseTransformVector(vertexGroupDelta) * (float)dragWeights[i];
                        else
                        {
                            Vector3 compensated = localDelta * (float)dragWeights[i];
                            compensated[dragMirrorAxis] = localDelta[dragMirrorAxis] * (float)dragMirrorWeights[i];
                            edit.Offset += dragTransforms[i].InverseTransformVector(module.part.transform.TransformVector(compensated));
                        }
                        changed |= (edit.Offset - updated[index].Offset).sqrMagnitude > 1e-12f;
                        updated[index] = edit;
                    }
                    CancelDrag();
                    if (changed) CommitVertices(updated);
                }
                return true;
            }
            SyncSelection(module, edits);
            if (!addingVertex && selectedVertices.Count > 0 && Input.GetKeyDown(KeyCode.Escape))
            { ChooseVertex(edits, -1, false); return true; }
            if (addingVertex && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
            { addingVertex = false; status = "Insertion cancelled"; return true; }
            if (!Input.GetMouseButtonDown(0) || IsPointerOverPanel(mouse) ||
                mouse.x < 0f || mouse.y < 0f || mouse.x >= Screen.width || mouse.y >= Screen.height ||
                (UnityEngine.EventSystems.EventSystem.current != null &&
                 UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())) return false;
            Camera camera = EditorCamera();
            if (camera == null) return false;
            if (addingVertex)
            {
                if (edits.Count >= ProceduralMeshBuilder.MaxIndividualVertices)
                { status = "Maximum of 64 individual vertices"; addingVertex = false; return true; }
                ProceduralVertex vertex; string error;
                if (!module.FaceMorph.TryPickProceduralVertex(camera.ScreenPointToRay(mouse), out vertex, out error, camera, addingEdgeVertex))
                { status = error; return true; }
                var updated = new List<ProceduralVertex>(edits) { vertex };
                int inserted = updated.Count - 1;
                if (mirrorAxis >= 0 && !AddMirror(module, updated, inserted)) return true;
                if (CommitVertices(updated))
                { ChooseVertex(updated, inserted, false); addingVertex = false; }
                return true;
            }
            int nearest = -1; float best = HandleRadius * HandleRadius, bestDepth = float.MaxValue;
            Vector3 chosenWorld = Vector3.zero;
            bool toggle = CtrlHeld();
            int axisHit = -1;
            // Hit-test the handles first, so Ctrl-click cannot accidentally drag an arrow.
            for (int i = 0; i < edits.Count; ++i)
            {
                Vector3 world; Transform mesh; Vector2 screen; float depth;
                if (!module.FaceMorph.TryGetProceduralVertexWorld(i, out world, out mesh) ||
                    !WorldScreen(camera, world, out screen, out depth)) continue;
                float d = (pointer - screen).sqrMagnitude;
                if (d > best || (d == best && depth >= bestDepth)) continue;
                nearest = i; best = d; bestDepth = depth; chosenWorld = world;
            }
            if (toggle)
            {
                if (nearest < 0) return false;
                ChooseVertex(edits, nearest, true);
                return true;
            }
            if (nearest < 0)
            {
                Vector3 group;
                if (!TryGroupCenter(module, out group)) return false;
                Vector2 center;
                Vector2[] axes = VertexAxes(module, camera, group, out center);
                axisHit = PickVertexAxis(pointer, center, axes);
                if (axes == null || (axisHit < 0 && (pointer - center).sqrMagnitude > HandleRadius * HandleRadius))
                    return false;
                nearest = selectedVertex; chosenWorld = group;
            }
            else if (!selectedVertices.Contains(nearest)) ChooseVertex(edits, nearest, false);
            vertexDragAxis = axisHit;
            vertexVertical = AltHeld();
            Vector2 vertexCenter;
            Vector2[] directions = VertexAxes(module, camera, chosenWorld, out vertexCenter);
            if (directions == null) return true;
            vertexScreenX = directions[0]; vertexScreenY = directions[1]; vertexScreenZ = directions[2];
            bool usable = vertexDragAxis >= 0 ? directions[vertexDragAxis].sqrMagnitude >= 16f :
                vertexVertical ? vertexScreenY.sqrMagnitude >= 16f :
                CornerAxesUsable(vertexScreenX, vertexScreenZ);
            if (!usable)
            { status = "Rotate the camera to expose the drag axes"; return true; }
            List<int> targets; List<float> signs;
            if (!TryMirrorTargets(module, out targets, out signs)) return true;
            double[] weights, reflected = null;
            if (!module.FaceMorph.TryGetVertexGroupWeights(targets, out weights) ||
                (mirrorAxis >= 0 && !module.FaceMorph.TryGetVertexGroupWeights(targets, out reflected, signs)))
            { status = "Cannot move this group: reduce overlapping influence radii or select fewer vertices"; return true; }
            var transforms = new Transform[targets.Count];
            for (int i = 0; i < targets.Count; ++i)
            {
                Vector3 world;
                if (!module.FaceMorph.TryGetProceduralVertexWorld(targets[i], out world, out transforms[i])) return true;
            }
            dragEdits = new List<ProceduralVertex>(edits);
            dragSelection = targets.ToArray(); dragTransforms = transforms; dragWeights = weights;
            dragMirrorAxis = mirrorAxis; dragMirrorWeights = reflected; dragMirrorSigns = signs.ToArray();
            vertexGroupDelta = Vector3.zero;
            vertexDragStart = pointer;
            draggingVertex = nearest;
            return true;
        }

        private void DrawVertexHandles(ModulePartSculptor module)
        {
            if (Event.current.type != EventType.Repaint || module == null || module.FaceMorph == null) return;
            Camera camera = EditorCamera();
            if (camera == null) return;
            Color previous = GUI.color;
            try
            {
                for (int i = 0; i < module.ProceduralVertices.Count; ++i)
                {
                    Vector3 world; Transform mesh; Vector2 screen; float depth;
                    if (!module.FaceMorph.TryGetProceduralVertexWorld(i, out world, out mesh)) continue;
                    if (draggingVertex >= 0) world += MirrorPreview(module, i);
                    if (!WorldScreen(camera, world, out screen, out depth)) continue;
                    if (selectedVertices.Contains(i))
                    {
                        GUI.color = Color.white;
                        GUI.DrawTexture(new Rect(screen.x - 8f, screen.y - 8f, 16f, 16f), Texture2D.whiteTexture);
                    }
                    GUI.color = Color.yellow;
                    GUI.DrawTexture(new Rect(screen.x - 5f, screen.y - 5f, 10f, 10f), Texture2D.whiteTexture);
                }
                if (!addingVertex)
                {
                    Vector3 world;
                    if (TryGroupCenter(module, out world))
                    {
                        if (draggingVertex >= 0)
                        {
                            Vector3 preview = Vector3.zero;
                            foreach (int index in selectedVertices) preview += MirrorPreview(module, index);
                            world += preview / selectedVertices.Count;
                        }
                        Vector2 center;
                        Vector2[] axes = VertexAxes(module, camera, world, out center);
                        if (axes != null && selectedVertices.Count > 1)
                        {
                            GUI.color = Color.white;
                            GUI.DrawTexture(new Rect(center.x - 8f, center.y - 8f, 16f, 16f), Texture2D.whiteTexture);
                            GUI.color = Color.yellow;
                            GUI.DrawTexture(new Rect(center.x - 5f, center.y - 5f, 10f, 10f), Texture2D.whiteTexture);
                        }
                        if (axes != null)
                            for (int i = 0; i < 3; ++i)
                            {
                                if (axes[i].sqrMagnitude < 16f) continue;
                                Vector2 d = axes[i].normalized, tip = center + d * VertexGizmoLength;
                                Vector2 side = new Vector2(-d.y, d.x);
                                GUI.color = draggingVertex >= 0 && vertexDragAxis == i ? Color.white : VertexAxisColors[i];
                                DrawLine(center + d * 9f, tip);
                                DrawLine(tip - d * 18f + side * 8f, tip);
                                DrawLine(tip - d * 18f - side * 8f, tip);
                                GUI.Label(new Rect(tip.x + side.x * 8f - 5f, tip.y + side.y * 8f - 9f, 20f, 20f),
                                    i == 0 ? "X" : i == 1 ? "Y" : "Z");
                            }
                    }
                }
            }
            finally { GUI.color = previous; }
        }

        private void DeleteSelectedVertex(ModulePartSculptor module)
        {
            if (selectedVertex < 0 || selectedVertex >= module.ProceduralVertices.Count) return;
            var updated = new List<ProceduralVertex>(module.ProceduralVertices);
            List<int> descending; List<float> signs;
            if (!TryMirrorTargets(module, out descending, out signs)) return;
            descending.Sort((a, b) => b.CompareTo(a));
            foreach (int index in descending) updated.RemoveAt(index);
            // Rebuild from native topology, retaining ring cuts and other edits.
            if (CommitVertices(updated))
            { ChooseVertex(updated, -1, false); status = "Selected vertices deleted; topology rebuilt"; }
        }

        private static float RoundRadius(float radius)
        {
            return (float)Math.Round((double)radius, 3, MidpointRounding.AwayFromZero);
        }

        private static string FormatRadius(float radius)
        {
            return radius.ToString("0.###", CultureInfo.InvariantCulture);
        }

        // Allow incomplete input (empty or just a decimal point) while typing.
        // Do not allow exponent notation or extra fractional digits, including paste.
        private static string LimitRadiusText(string text, string previous)
        {
            int point = -1;
            for (int i = 0; i < text.Length; ++i)
            {
                char c = text[i];
                if (c == '.' && point < 0) point = i;
                else if (c < '0' || c > '9') return previous;
            }
            if (point >= 0 && text.Length - point - 1 > 3)
                return text.Substring(0, point + 4);
            return text;
        }

        private void DrawVertexPanel(ModulePartSculptor module)
        {
            int count = module.ProceduralVertices.Count;
            GUILayout.Label("Individual vertices: " + count + " / " + ProceduralMeshBuilder.MaxIndividualVertices);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && !addingVertex;
            GUILayout.Label("Mirror plane (part-local)");
            mirrorAxis = GUILayout.Toolbar(mirrorAxis + 1, new[] {
                HelpContent("Off", "Disable within-part mirroring. Existing mirrored edits are retained."),
                HelpContent("X", "Mirror vertex insertion, movement, radius and deletion across the local Y/Z plane."),
                HelpContent("Y", "Mirror vertex insertion, movement, radius and deletion across the local X/Z plane."),
                HelpContent("Z", "Mirror vertex insertion, movement, radius and deletion across the local X/Y plane.")
            }) - 1;
            GUI.enabled = enabled && !addingVertex && mirrorAxis >= 0 && selectedVertices.Count > 0;
            if (GUILayout.Button("Create missing mirrors")) CreateMissingMirrors(module);
            count = module.ProceduralVertices.Count;
            GUI.enabled = enabled;
            if (mirrorAxis >= 0) GUILayout.Label("Reflect insert, move, radius and delete across the selected plane. Last-selected member drives a pair.",
                new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUI.enabled = enabled && (addingVertex || count < ProceduralMeshBuilder.MaxIndividualVertices);
            if (addingVertex)
            {
                if (GUILayout.Button("Cancel insertion"))
                { addingVertex = false; status = "Insertion cancelled"; }
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Add vertex"))
                { addingVertex = true; addingEdgeVertex = false; status = "Click a visible surface triangle"; }
                if (GUILayout.Button("Add edge/ring vertex"))
                { addingVertex = true; addingEdgeVertex = true; status = "Click near a visible triangle or ring edge"; }
                GUILayout.EndHorizontal();
            }
            GUI.enabled = enabled && count > 0 && !addingVertex;
            GUILayout.BeginHorizontal();
            GUI.enabled = enabled && selectedVertex >= 0 && selectedVertex < count && !addingVertex;
            if (GUILayout.Button("Delete selected")) DeleteSelectedVertex(module);
            GUI.enabled = enabled && count > 0 && !addingVertex;
            if (GUILayout.Button("Clear vertices") && CommitVertices(new List<ProceduralVertex>()))
            { ChooseVertex(module.ProceduralVertices, -1, false); status = "Original ring-only topology restored"; }
            GUILayout.EndHorizontal();
            GUI.enabled = enabled && !addingVertex && count > 0;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Select all visible"))
            {
                selectedVertices.Clear();
                for (int i = 0; i < count; ++i)
                {
                    Vector3 world; Transform mesh;
                    if (module.FaceMorph.TryGetProceduralVertexWorld(i, out world, out mesh)) selectedVertices.Add(i);
                }
                selectionIdentity = new List<ProceduralVertex>(module.ProceduralVertices);
                SelectVertex(module.ProceduralVertices, selectedVertices.Count == 0 ? -1 : selectedVertices[selectedVertices.Count - 1]);
            }
            if (GUILayout.Button("Deselect all")) ChooseVertex(module.ProceduralVertices, -1, false);
            GUILayout.EndHorizontal();
            GUI.enabled = enabled;
            if (selectedVertex >= 0 && selectedVertex < count)
            {
                GUILayout.Label("Selected vertices: " + selectedVertices.Count + " / " + count);
                GUI.enabled = enabled && !addingVertex;
                softSelection = GUILayout.Toggle(softSelection, HelpContent("Soft selection",
                    "Move surrounding vertices with a smooth falloff. Disable or use a zero radius for hard selection. Apply to commit."));
                GUI.enabled = enabled && !addingVertex && softSelection;
                if (module.FaceMorph != null)
                    radiusSliderLimit = Mathf.Max(radiusSliderLimit,
                        (module.FaceMorph.Max - module.FaceMorph.Min).magnitude);
                GUILayout.BeginHorizontal();
                GUILayout.Label(HelpContent("Radius (m)", "Influence distance in part-local metres. Zero affects only the selected vertex."),
                    GUILayout.ExpandWidth(false));
                float slid = GUILayout.HorizontalSlider(radiusSlider, 0f, radiusSliderLimit, GUILayout.ExpandWidth(true));
                if (slid != radiusSlider)
                {
                    radiusSlider = RoundRadius(slid);
                    radiusText = FormatRadius(radiusSlider);
                    radiusSliderLimit = Mathf.Max(radiusSliderLimit, radiusSlider);
                }
                // Size to one leading digit, decimal point and three decimals in the active skin.
                // Larger values remain valid and can scroll within the text field.
                float radiusFieldWidth = GUI.skin.textField.CalcSize(new GUIContent("0.000")).x + 4f;
                string oldText = radiusText;
                radiusText = GUILayout.TextField(radiusText, GUILayout.Width(radiusFieldWidth), GUILayout.ExpandWidth(false));
                radiusText = LimitRadiusText(radiusText, oldText);
                float typed;
                if (radiusText != oldText && float.TryParse(radiusText, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out typed) && ProceduralMeshBuilder.ValidInfluenceRadius(typed))
                {
                    radiusSlider = typed;
                    radiusSliderLimit = Mathf.Max(radiusSliderLimit, typed);
                }
                GUILayout.EndHorizontal();
                GUILayout.Label("Slider range: 0 – " + radiusSliderLimit.ToString("0.###", CultureInfo.InvariantCulture) + " m");
                GUI.enabled = enabled && !addingVertex;
                float radius = 0f;
                bool validRadius = !softSelection || (float.TryParse(radiusText, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out radius) && ProceduralMeshBuilder.ValidInfluenceRadius(radius));
                ProceduralVertex current = module.ProceduralVertices[selectedVertex];
                bool mixed = false, different = false;
                foreach (int index in selectedVertices)
                {
                    mixed |= module.ProceduralVertices[index].InfluenceRadius != current.InfluenceRadius;
                    different |= module.ProceduralVertices[index].InfluenceRadius != radius;
                }
                // Allow synchronizing a counterpart even if the driver's value is unchanged.
                GUI.enabled = GUI.enabled && validRadius && (different || mirrorAxis >= 0);
                if (GUILayout.Button("Apply influence radius"))
                {
                    var updated = new List<ProceduralVertex>(module.ProceduralVertices);
                    List<int> targets; List<float> signs;
                    if (TryMirrorTargets(module, out targets, out signs))
                    {
                        bool changed = false;
                        foreach (int index in targets)
                        {
                            current = updated[index];
                            changed |= current.InfluenceRadius != radius;
                            current.InfluenceRadius = radius;
                            updated[index] = current;
                        }
                        if (changed && CommitVertices(updated)) SelectVertex(updated, selectedVertex);
                    }
                }
                GUI.enabled = enabled;
                GUILayout.Label(mixed ? "Applied radius: mixed" : "Applied radius: " + current.InfluenceRadius.ToString("0.###", CultureInfo.InvariantCulture) + " m");
                if (!validRadius) GUILayout.Label("Enter a nonnegative finite radius.");
            }
            GUI.enabled = enabled && !addingVertex;
            bool fallback = Settings.GenerateVertexCollisionFallback;
            bool requestedFallback = GUILayout.Toggle(fallback,
                HelpContent("Generate vertex collision fallback",
                    "Experimental: generate a convex collider if no compatible native collider exists. " +
                    "Saves immediately and affects subsequent geometry edits."));
            if (requestedFallback != fallback)
            {
                string error;
                if (ScaleSettingsWindow.SetCollisionFallbackFromPG(requestedFallback, out error))
                    status = "Vertex collision fallback " + (requestedFallback ? "enabled" : "disabled");
                else status = error;
            }
            GUI.enabled = enabled;
            GUILayout.Label("Edge/ring insertion snaps to a nearby visible mesh edge. Delete rebuilds topology and retains other edits.",
                new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUILayout.Label("Ctrl-click toggles selection. Drag shared X/Y/Z arrows or a selected yellow handle (X/Z; Alt for Y). Esc/right-click cancels drag. Esc deselects.", new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUILayout.Label("Release to update mesh, collider, affected attachment nodes and calculated volume. Convex collision is approximate.",
                new GUIStyle(GUI.skin.label) { wordWrap = true });
        }

        private bool UpdateHandlesInternal(ModulePartSculptor module)
        {
            Select(module);
            if (vertexMode) return UpdateVertexHandles(module);
            ProceduralRing ring;
            if (!TryGetRing(module, out ring)) { CancelDrag(); return false; }
            Vector3 mouse = Input.mousePosition;
            Vector2 pointer = new Vector2(mouse.x, Screen.height - mouse.y);
            if (dragging >= 0)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1) ||
                    !Application.isFocused || dragRingIndex >= module.ProceduralRings.Count)
                { CancelDrag(); SyncFields(); return true; }
                // Freeze the projected direction at mouse-down; use absolute
                // displacement rather than compounding incremental deltas.
                if (dragTilt)
                {
                    float delta = Vector2.Dot(pointer - dragStart, dragAxis) / dragAxis.sqrMagnitude;
                    Vector3 size = module.FaceMorph.Max - module.FaceMorph.Min;
                    previewRing = OffsetRingTilt(originalRing, dragging, delta,
                        Mathf.Abs(size.x) * 0.5f, Mathf.Abs(size.z) * 0.5f);
                }
                else if (!dragVertical && dragging >= 5)
                {
                    Vector2 displacement = pointer - dragStart;
                    float determinant = Cross(dragAxis, dragDepthAxis);
                    previewRing = originalRing;
                    previewRing.Width += Cross(displacement, dragDepthAxis) / determinant;
                    previewRing.Depth += Cross(dragAxis, displacement) / determinant;
                }
                else
                {
                    float delta = Vector2.Dot(pointer - dragStart, dragAxis) / dragAxis.sqrMagnitude;
                    previewRing = OffsetRing(originalRing, dragVertical ? 4 : dragging, delta);
                }
                previewRing.Width = Mathf.Clamp(previewRing.Width, 0.1f, 4f);
                previewRing.Depth = Mathf.Clamp(previewRing.Depth, 0.1f, 4f);
                previewRing.TiltX = Mathf.Clamp(previewRing.TiltX, -Settings.MaxFaceTilt, Settings.MaxFaceTilt);
                previewRing.TiltZ = Mathf.Clamp(previewRing.TiltZ, -Settings.MaxFaceTilt, Settings.MaxFaceTilt);
                float min = dragRingIndex == 0 ? 0.01501f :
                    module.ProceduralRings[dragRingIndex - 1].Height + 0.01501f;
                float max = dragRingIndex == module.ProceduralRings.Count - 1 ? 0.99f :
                    module.ProceduralRings[dragRingIndex + 1].Height - 0.01501f;
                if (dragging == 4)
                    previewRing.Height = Mathf.Clamp(previewRing.Height, Mathf.Min(min, originalRing.Height),
                        Mathf.Max(max, originalRing.Height));
                if (!Input.GetMouseButton(0))
                {
                    var rings = new List<ProceduralRing>(module.ProceduralRings);
                    rings[dragRingIndex] = previewRing;
                    bool changed = Mathf.Abs(previewRing.Height - originalRing.Height) > 0.00001f ||
                        Mathf.Abs(previewRing.Width - originalRing.Width) > 0.00001f ||
                        Mathf.Abs(previewRing.Depth - originalRing.Depth) > 0.00001f ||
                        Mathf.Abs(previewRing.TiltX - originalRing.TiltX) > 0.00001f ||
                        Mathf.Abs(previewRing.TiltZ - originalRing.TiltZ) > 0.00001f;
                    CancelDrag();
                    if (changed) Commit(rings); // one native undo checkpoint
                    SyncFields();
                }
                return true;
            }
            if (!Input.GetMouseButtonDown(0) || IsPointerOverPanel(mouse) ||
                (UnityEngine.EventSystems.EventSystem.current != null &&
                 UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())) return false;
            Camera camera = EditorCamera();
            if (camera == null) return false;
            bool vertical = AltHeld();
            int nearest = -1;
            float best = HandleRadius * HandleRadius;
            Vector2 axis = Vector2.zero, depthAxis = Vector2.zero;
            for (int i = 0; i < HandleCount; ++i)
            {
                Vector2 pos, shifted;
                const float step = 0.01f;
                if (!ProjectHandle(module, camera, ring, i, out pos)) continue;
                Vector2 direction, depthDirection = Vector2.zero;
                if (vertical && i != 4)
                {
                    Vector3 size = module.FaceMorph.Max - module.FaceMorph.Min;
                    if (!ProjectHandle(module, camera,
                            OffsetRingTilt(ring, i, step, Mathf.Abs(size.x) * 0.5f, Mathf.Abs(size.z) * 0.5f),
                            i, out shifted)) continue;
                    direction = (shifted - pos) / step;
                    if (direction.sqrMagnitude < 16f) continue;
                }
                else if (vertical)
                {
                    // The green center handle still moves the entire ring in Y.
                    if (!ProjectHandle(module, camera, OffsetRing(ring, 4, step), i, out shifted)) continue;
                    direction = (shifted - pos) / step;
                    if (direction.sqrMagnitude < 16f) continue;
                }
                else if (i >= 5)
                {
                    ProceduralRing xRing = ring, zRing = ring;
                    xRing.Width += step;
                    zRing.Depth += step;
                    Vector2 shiftedDepth;
                    if (!ProjectHandle(module, camera, xRing, i, out shifted) ||
                        !ProjectHandle(module, camera, zRing, i, out shiftedDepth)) continue;
                    direction = (shifted - pos) / step;
                    depthDirection = (shiftedDepth - pos) / step;
                    if (!CornerAxesUsable(direction, depthDirection)) continue;
                }
                else
                {
                    if (!ProjectHandle(module, camera, OffsetRing(ring, i, step), i, out shifted)) continue;
                    direction = (shifted - pos) / step;
                    if (direction.sqrMagnitude < 16f) continue; // axis nearly end-on
                }
                float distance = (pointer - pos).sqrMagnitude;
                if (distance <= best)
                { best = distance; nearest = i; axis = direction; depthAxis = depthDirection; }
            }
            if (nearest < 0) return false;
            dragging = nearest;
            dragVertical = vertical;
            dragTilt = vertical && nearest != 4; // Alt-drag on side/corner handles tilts the planar ring.
            dragRingIndex = selectedRing;
            originalRing = previewRing = ring;
            dragStart = pointer;
            dragAxis = axis;
            dragDepthAxis = depthAxis;
            return true;
        }

        private void DrawRingOutline(ModulePartSculptor module)
        {
            ProceduralRing ring;
            if (Event.current.type != EventType.Repaint || !TryGetRing(module, out ring)) return;
            Camera camera = EditorCamera();
            if (camera == null) return;
            Vector2[] screen = new Vector2[4];
            for (int i = 0; i < 4; ++i)
                if (!Project(module, camera, ring, (i & 1) == 0 ? -1f : 1f,
                    (i & 2) == 0 ? -1f : 1f, out screen[i])) return;
            Color oldColor = GUI.color;
            try
            {
                GUI.color = new Color(0.9f, 1f, 0.15f, 0.9f);
                DrawLine(screen[0], screen[1]);
                DrawLine(screen[1], screen[3]);
                DrawLine(screen[3], screen[2]);
                DrawLine(screen[2], screen[0]);
                for (int i = 0; i < HandleCount; ++i)
                {
                    Vector2 pos;
                    if (!ProjectHandle(module, camera, ring, i, out pos)) continue;
                    GUI.color = i == dragging ? Color.white : i < 2 ? Color.red :
                        i < 4 ? new Color(0.25f, 0.6f, 1f) : i == 4 ? Color.green : Color.yellow;
                    GUI.DrawTexture(new Rect(pos.x - 6f, pos.y - 6f, 12f, 12f), Texture2D.whiteTexture);
                }
            }
            finally { GUI.color = oldColor; }
        }

        internal static void Draw(Rect anchor, ModulePartSculptor module)
        {
            instance.DrawInternal(anchor, module);
        }

        private void DrawInternal(Rect anchor, ModulePartSculptor module)
        {
            Select(module);
            if (vertexMode) DrawVertexHandles(module); else DrawRingOutline(module);
            panel = new Rect(Mathf.Clamp(anchor.x, 0f, Mathf.Max(0f, Screen.width - 290f)),
                anchor.y, Mathf.Min(290f, Screen.width),
                Mathf.Min(vertexMode ? 460f : 345f, Mathf.Max(0f, Screen.height - anchor.y)));
            if (panel.height < 60f) return;
            GUILayout.BeginArea(panel, GUI.skin.box);
            GUILayout.Label("Procedural Geometry — cross-section rings");
            if (module == null)
            {
                GUILayout.Label("Select a part in the editor to insert vertices.");
                GUILayout.EndArea();
                return;
            }
            bool panelEnabled = GUI.enabled;
            GUI.enabled = panelEnabled && !IsDragging && module.FaceMorph != null && module.FaceMorph.Valid;
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(!vertexMode, HelpContent("Rings", "Edit cross-section rings with resize, height and Alt-tilt handles."), GUI.skin.button) && vertexMode)
            { vertexMode = false; addingVertex = false; status = ""; }
            if (GUILayout.Toggle(vertexMode, HelpContent("Vertices", "Insert, select, move or delete individual vertices; use soft selection and mirroring."), GUI.skin.button) && !vertexMode)
            { vertexMode = true; status = ""; }
            GUILayout.EndHorizontal();
            string tooltip = HoveredHelpTooltip();
            if (vertexMode)
            {
                vertexPanelScroll = GUILayout.BeginScrollView(vertexPanelScroll);
                DrawVertexPanel(module);
                GUI.enabled = panelEnabled;
                if (!string.IsNullOrEmpty(status)) GUILayout.Label(status, new GUIStyle(GUI.skin.label) { wordWrap = true });
                // Capture while inside the scroll view, then render outside its clipping region.
                tooltip = HoveredHelpTooltip() ?? tooltip;
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                DrawHelpTooltip(tooltip);
                return;
            }
            selectedRing = Mathf.Clamp(selectedRing, 0, Mathf.Max(0, module.ProceduralRings.Count - 1));
            int count = module.ProceduralRings.Count;
            GUILayout.Label("Rings: " + count + " / " + ProceduralMeshBuilder.MaxRings +
                "     Added visual vertices: " + module.ProceduralAddedVertices);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add ring")) AddRing();
            bool oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && count > 0;
            if (GUILayout.Button("Remove ring"))
            {
                var rings = new List<ProceduralRing>(module.ProceduralRings);
                rings.RemoveAt(selectedRing);
                Commit(rings);
            }
            if (GUILayout.Button("Clear all")) Commit(new List<ProceduralRing>());
            GUI.enabled = oldEnabled;
            GUILayout.EndHorizontal();
            count = module.ProceduralRings.Count;
            if (count > 0)
            {
                GUILayout.BeginHorizontal();
                GUI.enabled = oldEnabled && selectedRing > 0;
                if (GUILayout.Button("<", GUILayout.Width(32f)))
                { --selectedRing; SyncFields(); }
                GUI.enabled = oldEnabled;
                GUILayout.Label("Ring " + (selectedRing + 1) + " of " + count);
                GUI.enabled = oldEnabled && selectedRing < count - 1;
                if (GUILayout.Button(">", GUILayout.Width(32f)))
                { ++selectedRing; SyncFields(); }
                GUI.enabled = oldEnabled;
                GUILayout.EndHorizontal();
                FieldRow("Height (%)", ref heightText);
                FieldRow("X multiplier", ref widthText);
                FieldRow("Z multiplier", ref depthText);
                if (GUILayout.Button("Apply ring changes")) ApplyRing();
            }
            GUILayout.Label("Red X, blue Z, green Y, yellow X/Z corners. Alt-drag side/corner handles to tilt the ring. Green moves the whole ring in Y. Esc/right-click cancels.",
                new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUI.enabled = panelEnabled;
            if (dragging >= 0) GUILayout.Label(string.Format(CultureInfo.InvariantCulture,
                "Preview: Y {0:0.0}%   X {1:0.00}   Z {2:0.00}   TiltX {3:0.00}   TiltZ {4:0.00}",
                previewRing.Height * 100f, previewRing.Width, previewRing.Depth,
                previewRing.TiltX, previewRing.TiltZ));
            GUILayout.Label("End faces and attachment nodes stay in place.");
            if (!string.IsNullOrEmpty(status)) GUILayout.Label(status);
            GUILayout.EndArea();
            DrawHelpTooltip(tooltip);
        }
    }
}
