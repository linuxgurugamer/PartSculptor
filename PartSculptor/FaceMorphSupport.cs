// Mesh/collider deformation for static KSP1 parts. Never edit prefab/shared
// meshes. Rebuild each instance from unmodified vertex snapshots on EVERY edit.
using System;
using System.Collections.Generic;
using UnityEngine;
using static PartSculptor.RegisterToolbar;

namespace PartSculptor
{
    internal sealed class FaceMorphSupport
    {
        private sealed class MeshRecord
        {
            internal MeshFilter filter;
            internal MeshCollider collider;
            internal Transform transform;
            internal Mesh native;
            internal Mesh instance;
            internal Mesh vertexCollider;
            // Visual-derived fallback is deliberately not in the native
            // collider collection; it is regenerated only after preflight.
            internal MeshCollider generatedCollider;
            internal Mesh generatedMesh;
            internal Vector3[] vertices;
            internal Vector3[] nativeVertices;
            internal string topologyKey = "";
            internal string vertexMeshKey;
            internal List<ProceduralVertexBinding> vertexBindings = new List<ProceduralVertexBinding>();
            internal Vector3[] currentVertices;
            internal Vector3[] vertexBaselinePart;
            internal string b9VariantTransform;
            // A primitive collider is kept intact and restored when the morph
            // is reset or this part is destroyed. Only the proxy deforms.
            internal Collider primitive;
            internal bool originalEnabled;
            internal bool ownedNative;
        }

        private struct NodeRecord
        {
            internal Vector3 position, originalPosition, orientation;
            internal int size;
        }

        // Capture the node's *previous* local pose before applying a face
        // deformation. A child must follow the whole node pose (position and
        // normal), not just the offset of its center point.
        private struct AttachedChildMotion
        {
            internal AttachNode node;
            internal Part child;
            internal Vector3 previousPosition, previousOrientation;
        }

        private readonly Part part;
        private readonly Transform model;
        private readonly List<MeshRecord> meshes = new List<MeshRecord>();
        private readonly Dictionary<AttachNode, NodeRecord> nodes =
            new Dictionary<AttachNode, NodeRecord>();
        private Vector3 min, max;
        private readonly List<ProceduralRing> proceduralRings = new List<ProceduralRing>();
        private readonly List<ProceduralVertex> proceduralVertices = new List<ProceduralVertex>();
        // Each B9 mesh alternative may have a different length. Rings must
        // terminate at THAT variant's ends, not the longest inactive variant's.
        private readonly HashSet<string> b9MeshVariants =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly List<List<HashSet<string>>> b9SubtypeSelectors =
            new List<List<HashSet<string>>>();
        private readonly Dictionary<string, Vector2> variantYRanges =
            new Dictionary<string, Vector2>(StringComparer.Ordinal);
        internal Vector2 PreviewRingYRange
        {
            get
            {
                foreach (MeshRecord r in meshes)
                {
                    Vector2 bounds;
                    if (r.filter != null && r.filter.gameObject.activeInHierarchy &&
                        r.b9VariantTransform != null &&
                        variantYRanges.TryGetValue(r.b9VariantTransform, out bounds))
                        return bounds;
                }
                return new Vector2(min.y, max.y);
            }
        }
        internal int ProceduralVertexCount
        {
            get
            {
                int count = 0;
                foreach (MeshRecord r in meshes)
                    if (r.filter != null) count += r.vertices.Length - r.nativeVertices.Length;
                return count;
            }
        }
        internal double VertexVolumeFactor { get; private set; } = 1d;
        private bool valid;
        internal string Error { get; private set; }
        internal bool Valid { get { return valid; } }
        internal Vector3 Min { get { return min; } }
        internal Vector3 Max { get { return max; } }

        // The editor duplicates a part's live GameObjects, including meshes
        // owned by an active FaceMorphSupport. Those meshes are ALREADY bent.
        // Applying the copied KSPField morph a second time bends the cloned
        // face again, while attach nodes (rebuilt from prefab) rotate only
        // once. Always recover the unmodified prefab mesh for a copied morph.
        private const string MorphMeshPrefix = "PartSculptor face morph ";
        private const string GeneratedColliderPrefix = MorphMeshPrefix + "generated vertex collision ";

        private static bool IsGeneratedVertexCollider(MeshCollider collider)
        {
            return collider != null && collider.sharedMesh != null &&
                collider.sharedMesh.name.StartsWith(GeneratedColliderPrefix, StringComparison.Ordinal);
        }

        private void RemoveCopiedGeneratedColliders()
        {
            foreach (MeshCollider c in part.GetComponentsInChildren<MeshCollider>(true))
                if (c != null && c.GetComponentInParent<Part>() == part && IsGeneratedVertexCollider(c))
                {
                    // A clone may still reference a source part's mesh. Do not
                    // destroy that mesh here; the detached-copy owner handles it.
                    c.enabled = false;
                    c.sharedMesh = null;
                    UnityEngine.Object.Destroy(c);
                }
        }

        private static bool IsMorphInstance(Mesh mesh)
        {
            return mesh != null && mesh.name != null &&
                (mesh.name.StartsWith(MorphMeshPrefix, StringComparison.Ordinal) );
        }

        // KSP Alt-copy and symmetry construction copy MeshFilter.sharedMesh and
        // MeshCollider.sharedMesh references from the live source GameObject.
        // If the source already has a FaceMorphSupport, its private, MUTABLE
        // meshes are therefore shared with the copy until the latter builds its
        // own morph support. Detach them during PartModule.OnWasCopied, before
        // any later drag on the source can deform the copied part too.
        internal static List<Mesh> DetachCopiedMorphMeshes(Part copy)
        {
            var owned = new List<Mesh>();
            if (copy == null) return owned;
            var copies = new Dictionary<Mesh, Mesh>();
            Transform copyModel = copy.transform.Find("model");
            if (copyModel != null)
            {
                foreach (MeshFilter filter in copyModel.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter == null || filter.GetComponentInParent<Part>() != copy ||
                        !IsMorphInstance(filter.sharedMesh)) continue;
                    filter.sharedMesh = IndependentCopy(filter.sharedMesh, copies, owned);
                }
            }
            foreach (MeshCollider collider in copy.GetComponentsInChildren<MeshCollider>(true))
            {
                if (collider == null || collider.GetComponentInParent<Part>() != copy ||
                    !IsMorphInstance(collider.sharedMesh)) continue;
                collider.sharedMesh = IndependentCopy(collider.sharedMesh, copies, owned);
            }
            return owned;
        }

        private static Mesh IndependentCopy(Mesh original,
            Dictionary<Mesh, Mesh> copies, List<Mesh> owned)
        {
            Mesh independent;
            if (!copies.TryGetValue(original, out independent))
            {
                independent = UnityEngine.Object.Instantiate(original);
                // Keep the prefix: EnsureFaceMorph can then restore the clean
                // prefab baseline and reapply the copied part's saved face values.
                independent.name = original.name;
                copies.Add(original, independent);
                owned.Add(independent);
            }
            return independent;
        }

        // Meshes detached during copying are provisional. Once this part has
        // rebuilt its own FaceMorphSupport, release only meshes no longer used
        // by any of its renderers or colliders. Never destroy a shared source.
        internal static void ReleaseDetachedMorphMeshes(Part copy, List<Mesh> owned)
        {
            if (owned == null || owned.Count == 0) return;
            for (int i = owned.Count - 1; i >= 0; --i)
            {
                Mesh mesh = owned[i];
                bool referenced = false;
                if (copy != null)
                {
                    foreach (MeshFilter f in copy.GetComponentsInChildren<MeshFilter>(true))
                        if (f != null && f.GetComponentInParent<Part>() == copy &&
                            f.sharedMesh == mesh) { referenced = true; break; }
                    if (!referenced)
                        foreach (MeshCollider c in copy.GetComponentsInChildren<MeshCollider>(true))
                            if (c != null && c.GetComponentInParent<Part>() == copy &&
                                c.sharedMesh == mesh) { referenced = true; break; }
                }
                if (referenced) continue;
                if (mesh != null) UnityEngine.Object.Destroy(mesh);
                owned.RemoveAt(i);
            }
        }

        // Fallback for copies restored through craft loading/editor paths that
        // do not invoke OnWasCopied before OnStartFinished.
        internal static bool ContainsMorphMeshes(Part copy)
        {
            if (copy == null) return false;
            Transform copyModel = copy.transform.Find("model");
            if (copyModel != null)
                foreach (MeshFilter f in copyModel.GetComponentsInChildren<MeshFilter>(true))
                    if (f != null && IsMorphInstance(f.sharedMesh)) return true;
            foreach (MeshCollider c in copy.GetComponentsInChildren<MeshCollider>(true))
                if (c != null && c.GetComponentInParent<Part>() == copy &&
                    IsMorphInstance(c.sharedMesh)) return true;
            return false;
        }

        // Resolve the same transform in the prefab by name and occurrence
        // among same-named siblings. Stock and mod models can repeat names,
        // so Transform.Find(path) alone is not sufficient.
        private Transform PrefabTransform(Transform live)
        {
            Part prefab = part != null && part.partInfo != null ?
                part.partInfo.partPrefab : null;
            if (prefab == null || live == null) return null;
            if (live == part.transform) return prefab.transform;

            var steps = new List<KeyValuePair<string, int>>();
            Transform child = live;
            while (child != null && child != part.transform)
            {
                Transform parent = child.parent;
                if (parent == null) return null;
                int occurrence = 0;
                for (int i = 0; i < child.GetSiblingIndex(); ++i)
                    if (parent.GetChild(i).name == child.name) ++occurrence;
                steps.Add(new KeyValuePair<string, int>(child.name, occurrence));
                child = parent;
            }
            if (child != part.transform) return null;
            Transform result = prefab.transform;
            for (int i = steps.Count - 1; i >= 0; --i)
            {
                int occurrence = 0;
                Transform next = null;
                for (int j = 0; j < result.childCount; ++j)
                {
                    Transform candidate = result.GetChild(j);
                    if (candidate.name != steps[i].Key) continue;
                    if (occurrence++ == steps[i].Value)
                    { next = candidate; break; }
                }
                if (next == null) return null;
                result = next;
            }
            return result;
        }

        private Mesh PrefabMesh(MeshFilter live)
        {
            Transform original = PrefabTransform(live.transform);
            MeshFilter filter = original != null ? original.GetComponent<MeshFilter>() : null;
            return filter != null ? filter.sharedMesh : null;
        }

        private Mesh PrefabMesh(MeshCollider live)
        {
            Transform original = PrefabTransform(live.transform);
            if (original == null) return null;
            MeshCollider[] onLive = live.GetComponents<MeshCollider>();
            MeshCollider[] onPrefab = original.GetComponents<MeshCollider>();
            int index = Array.IndexOf(onLive, live);
            return index >= 0 && index < onPrefab.Length && onPrefab[index] != null ?
                onPrefab[index].sharedMesh : null;
        }

        // A previously morphed primitive has a disabled Box/Sphere/Capsule
        // collider plus an enabled private MeshCollider proxy. Both components
        // are copied by the editor. Remove the copied proxy and restore the
        // primitive's prefab enabled state before building a new proxy here.
        private static bool IsPrimitiveMorphProxy(MeshCollider collider)
        {
            if (collider == null || !IsMorphInstance(collider.sharedMesh)) return false;
            string name = collider.sharedMesh.name;
            return name.StartsWith(MorphMeshPrefix +
                    "PartSculptor box collision baseline", StringComparison.Ordinal) ||
                name.StartsWith(MorphMeshPrefix +
                    "PartSculptor round collision baseline", StringComparison.Ordinal);
        }

        private void RestoreCopiedPrimitiveColliders()
        {
            foreach (MeshCollider proxy in part.GetComponentsInChildren<MeshCollider>(true))
            {
                if (!IsPrimitiveMorphProxy(proxy) ||
                    proxy.GetComponentInParent<Part>() != part) continue;
                Collider primitive = (Collider)proxy.GetComponent<BoxCollider>() ??
                    (Collider)proxy.GetComponent<SphereCollider>() ??
                    (Collider)proxy.GetComponent<CapsuleCollider>();
                if (primitive == null) continue;
                Transform original = PrefabTransform(proxy.transform);
                if (original == null) continue;
                Collider native = primitive is BoxCollider ?
                    (Collider)original.GetComponent<BoxCollider>() :
                    primitive is SphereCollider ?
                    (Collider)original.GetComponent<SphereCollider>() :
                    original.GetComponent<CapsuleCollider>();
                if (native == null) continue;
                primitive.enabled = native.enabled;
                proxy.enabled = false;
                // Destroy is deferred until the end of the editor frame.
                // The mesh-collection pass below explicitly skips this proxy.
                UnityEngine.Object.Destroy(proxy);
            }
        }

        internal FaceMorphSupport(Part owner, Transform modelTransform)
        {
            part = owner;
            model = modelTransform;
            Error = "Unsupported part geometry";
            // Exact B9 SUBTYPE transform selectors have independent geometry.
            // Do not classify wildcard/regex selectors as literal object names.
            if (part != null && part.partInfo != null &&
                part.partInfo.partConfig != null)
                foreach (ConfigNode moduleNode in part.partInfo.partConfig.GetNodes("MODULE"))
                    if (string.Equals(moduleNode.GetValue("name"), "ModuleB9PartSwitch",
                        StringComparison.Ordinal))
                    {
                        var moduleSelectors = new List<HashSet<string>>();
                        b9SubtypeSelectors.Add(moduleSelectors);
                        foreach (ConfigNode subtype in moduleNode.GetNodes("SUBTYPE"))
                        {
                            var selectors = new HashSet<string>(StringComparer.Ordinal);
                            moduleSelectors.Add(selectors);
                            foreach (ConfigNode.Value entry in subtype.values)
                            {
                                if (!string.Equals(entry.name, "transform",
                                    StringComparison.OrdinalIgnoreCase)) continue;
                                string name = entry.value;
                                if (!string.IsNullOrWhiteSpace(name) &&
                                    name.IndexOfAny(new[] { '*', '?', '[', ']', '(', ')', '^', '$' }) < 0)
                                {
                                    b9MeshVariants.Add(name.Trim());
                                    selectors.Add(name.Trim());
                                }
                            }
                        }
                    }
            if (part == null || model == null) return;
            RemoveCopiedGeneratedColliders();
            RestoreCopiedPrimitiveColliders();
            if (model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != 0)
            { Error = "Skinned/animated meshes are not supported"; return; }
            // Some stock tanks use BoxCollider, SphereCollider or
            // CapsuleCollider instead of MeshCollider. They can be represented
            // by a private convex mesh proxy during deformation. Other collider
            // types are not silently ignored: that would produce bad physics.
            foreach (Collider c in part.GetComponentsInChildren<Collider>(true))
            {
                if (c == null || c.GetComponentInParent<Part>() != part ||
                    !c.enabled || c.isTrigger) continue;
                if (c is MeshCollider || c is BoxCollider ||
                    c is SphereCollider || c is CapsuleCollider) continue;
                Error = "Unsupported collider: " + c.GetType().Name;
                return;
            }
            // Include *inactive* models. B9 SUBTYPE transform = ... selects
            // between existing GameObjects by toggling their active state. If
            // only the currently visible variant were captured, switching to
            // another subtype after morphing would reveal an unmorphed mesh.
            // Never activate/deactivate these objects ourselves; B9 owns that.
            foreach (MeshFilter f in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f == null || f.GetComponentInParent<Part>() != part ||
                    f.sharedMesh == null || f.GetComponent<MeshRenderer>() == null)
                    continue;
                MeshRenderer render = f.GetComponent<MeshRenderer>();
                Material material = render.sharedMaterial;
                if (material != null && material.shader != null &&
                    material.shader.name.IndexOf("Waterfall", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue; // effect meshes are controlled by Waterfall, not the cage
                Mesh native = IsMorphInstance(f.sharedMesh) ? PrefabMesh(f) : f.sharedMesh;
                if (native == null || !native.isReadable || IsMorphInstance(native))
                { Error = "Readable native visual mesh is unavailable"; Dispose(); return; }
                Add(f, null, native);
            }
            // Inactive B9 alternatives can also own their own collision mesh.
            // Record those too, so a later subtype switch has morphed physics.
            foreach (MeshCollider c in part.GetComponentsInChildren<MeshCollider>(true))
            {
                if (c == null || c.GetComponentInParent<Part>() != part ||
                    !c.enabled || c.isTrigger || c.sharedMesh == null ||
                    IsPrimitiveMorphProxy(c)) continue;
                // A duplicated MeshCollider can also point to a private
                // already-morphed mesh. Recover the immutable prefab shape.
                Mesh native = IsMorphInstance(c.sharedMesh) ? PrefabMesh(c) : c.sharedMesh;
                if (native == null || !native.isReadable || IsMorphInstance(native))
                { Error = "Readable native collider mesh is unavailable";
                  LogCollisionInventory(Error + ": " + CollisionPath(c.transform)); Dispose(); return; }
                Add(null, c, native);
            }
            // Construct a temporary mesh representation for primitive stock
            // collision shapes. At identity the native colliders stay enabled;
            // the proxies are only activated when a face is actually morphed.
            foreach (Collider c in part.GetComponentsInChildren<Collider>(true))
            {
                if (c == null || c.GetComponentInParent<Part>() != part ||
                    !c.enabled || c.isTrigger || c is MeshCollider) continue;
                Mesh proxyMesh = PrimitiveMesh(c);
                if (proxyMesh == null)
                { Error = "Unable to model collider: " + c.GetType().Name;
                  Dispose(); return; }
                AddPrimitive(c, proxyMesh);
            }
            if (!meshes.Exists(m => m.filter != null) ||
                (!meshes.Exists(m => m.collider != null) && !Settings.GenerateVertexCollisionFallback))
            { Error = "Readable visual mesh and supported collider are required";
              LogCollisionInventory("No supported native collider or visual mesh");
              Dispose(); return; }
            // All colliders must be represented by a deformed MeshCollider.
            // Do not accept unsupported meshless collision setups.
            RebaseNodes();
            RecalculateBounds();
            if (max.y - min.y < 0.01f || max.x - min.x < 0.01f ||
                max.z - min.z < 0.01f)
            { Error = "Part bounding box is degenerate"; Dispose(); return; }
            valid = true;
            Error = "";
        }

        private string VertexMeshKey(MeshFilter filter, Mesh native)
        {
            var path = new List<string>();
            for (Transform t = filter.transform; t != null && t != model; t = t.parent)
                path.Add(t.GetSiblingIndex() + ":" + Uri.EscapeDataString(t.name));
            path.Reverse();
            return string.Join("/", path.ToArray()) + "#" +
                Array.IndexOf(filter.GetComponents<MeshFilter>(), filter) + ":" +
                Uri.EscapeDataString(native.name) + ":" + native.vertexCount;
        }

        private void Add(MeshFilter f, MeshCollider c, Mesh native)
        {
            MeshRecord r = new MeshRecord();
            r.filter = f;
            r.collider = c;
            r.transform = f != null ? f.transform : c.transform;
            for (Transform walk = r.transform; walk != null && walk != model;
                 walk = walk.parent)
                if (b9MeshVariants.Contains(walk.name))
                { r.b9VariantTransform = walk.name; break; }
            r.native = native;
            r.vertices = native.vertices;
            r.nativeVertices = r.vertices;
            if (f != null) r.vertexMeshKey = VertexMeshKey(f, native);
            r.originalEnabled = c != null && c.enabled;
            r.instance = UnityEngine.Object.Instantiate(native);
            r.instance.name = "PartSculptor face morph " + native.name;
            if (f != null) f.sharedMesh = r.instance;
            if (c != null) c.sharedMesh = r.instance;
            meshes.Add(r);
        }

        // Generated proxy meshes have private ownership and are never assigned
        // to the prefab or the original collider. This avoids mutating KSP's
        // shared part assets and restores the original physics on reset.
        private void AddPrimitive(Collider primitive, Mesh native)
        {
            MeshCollider proxy = primitive.gameObject.AddComponent<MeshCollider>();
            proxy.enabled = false;
            proxy.convex = true;
            proxy.isTrigger = false;
            proxy.sharedMaterial = primitive.sharedMaterial;
            proxy.contactOffset = primitive.contactOffset;
            Add(null, proxy, native);
            MeshRecord r = meshes[meshes.Count - 1];
            r.primitive = primitive;
            r.originalEnabled = primitive.enabled;
            r.ownedNative = true;
        }

        // Eight corner points and twelve triangles for a BoxCollider.
        private static Mesh MakeBox(Vector3 center, Vector3 size)
        {
            Vector3 h = size * 0.5f;
            Vector3[] vertices = new Vector3[8];
            for (int i = 0; i < 8; ++i)
                vertices[i] = center + new Vector3((i & 1) == 0 ? -h.x : h.x,
                    (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
            int[] indices = {
                0,2,3, 0,3,1, 4,5,7, 4,7,6,
                0,1,5, 0,5,4, 2,6,7, 2,7,3,
                0,4,6, 0,6,2, 1,3,7, 1,7,5
            };
            Mesh mesh = new Mesh();
            mesh.name = "PartSculptor box collision baseline";
            mesh.vertices = vertices;
            mesh.triangles = indices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Build a low-poly convex envelope matching a sphere/capsule. The
        // profile contains the original spherical caps and the capsule's
        // straight middle section, not just a bounding box.
        private static Mesh MakeRound(Vector3 center, float radius, float height,
            int direction, bool capsule)
        {
            // Stay below the Unity/PhysX 255-triangle convex-collider limit.
            // 12 sides * 9 bands * 2 triangles = 216 maximum.
            const int sides = 12;
            const int halfRings = 4;
            float bodyHalf = capsule ? Mathf.Max(0f, height * 0.5f - radius) : 0f;
            var rings = new List<Vector2>();
            for (int i = 0; i <= halfRings; ++i)
            {
                float angle = -Mathf.PI * 0.5f + Mathf.PI * 0.5f * i / halfRings;
                rings.Add(new Vector2(Mathf.Max(radius * 0.0001f, radius * Mathf.Cos(angle)),
                    -bodyHalf + radius * Mathf.Sin(angle)));
            }
            for (int i = bodyHalf > 0.00001f ? 0 : 1; i <= halfRings; ++i)
            {
                float angle = Mathf.PI * 0.5f * i / halfRings;
                rings.Add(new Vector2(Mathf.Max(radius * 0.0001f, radius * Mathf.Cos(angle)),
                    bodyHalf + radius * Mathf.Sin(angle)));
            }
            Vector3[] vertices = new Vector3[rings.Count * sides];
            var indices = new List<int>();
            for (int ring = 0; ring < rings.Count; ++ring)
                for (int i = 0; i < sides; ++i)
                {
                    float a = 2f * Mathf.PI * i / sides;
                    float axis = rings[ring].y;
                    float u = rings[ring].x * Mathf.Cos(a);
                    float v = rings[ring].x * Mathf.Sin(a);
                    Vector3 p = direction == 0 ? new Vector3(axis, u, v) :
                        direction == 2 ? new Vector3(u, v, axis) :
                        new Vector3(u, axis, v);
                    vertices[ring * sides + i] = center + p;
                    if (ring == 0) continue;
                    int prev = (i + 1) % sides;
                    int a0 = (ring - 1) * sides + i;
                    int a1 = (ring - 1) * sides + prev;
                    int b0 = ring * sides + i;
                    int b1 = ring * sides + prev;
                    indices.Add(a0); indices.Add(a1); indices.Add(b0);
                    indices.Add(b0); indices.Add(a1); indices.Add(b1);
                }
            Mesh mesh = new Mesh();
            mesh.name = "PartSculptor round collision baseline";
            mesh.vertices = vertices;
            mesh.triangles = indices.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh PrimitiveMesh(Collider c)
        {
            BoxCollider box = c as BoxCollider;
            if (box != null) return MakeBox(box.center, box.size);
            SphereCollider sphere = c as SphereCollider;
            if (sphere != null) return MakeRound(sphere.center, sphere.radius,
                sphere.radius * 2f, 1, false);
            CapsuleCollider capsule = c as CapsuleCollider;
            if (capsule != null) return MakeRound(capsule.center, capsule.radius,
                Mathf.Max(capsule.height, capsule.radius * 2f),
                capsule.direction, true);
            return null;
        }

        private void RecalculateBounds()
        {
            variantYRanges.Clear();
            bool first = true;
            foreach (MeshRecord r in meshes)
            {
                if (r.filter == null) continue; // use visible geometry as bounds
                foreach (Vector3 vertex in r.nativeVertices)
                {
                    Vector3 p = part.transform.InverseTransformPoint(r.transform.TransformPoint(vertex));
                    if (first) { min = max = p; first = false; }
                    else { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
                    if (r.b9VariantTransform != null)
                    {
                        Vector2 span;
                        if (variantYRanges.TryGetValue(r.b9VariantTransform, out span))
                            variantYRanges[r.b9VariantTransform] =
                                new Vector2(Mathf.Min(span.x, p.y), Mathf.Max(span.y, p.y));
                        else
                            variantYRanges[r.b9VariantTransform] = new Vector2(p.y, p.y);
                    }
                }
            }
        }

        private Vector2 RingYRange(MeshRecord r)
        {
            Vector2 variant;
            if (r.b9VariantTransform != null &&
                variantYRanges.TryGetValue(r.b9VariantTransform, out variant) &&
                variant.y - variant.x > 0.01f)
                return variant;
            return new Vector2(min.y, max.y);
        }

        private static bool Visible(MeshRecord r)
        {
            if (r.filter == null || !r.filter.gameObject.activeInHierarchy || r.instance == null) return false;
            MeshRenderer renderer = r.filter.GetComponent<MeshRenderer>();
            return renderer != null && renderer.enabled;
        }

        // Pick the actual rendered triangle, not a simplified convex collider.
        // Map its barycentric point back to the undeformed mesh-local anchors.
        internal bool TryPickProceduralVertex(Ray worldRay, out ProceduralVertex vertex, out string error,
            Camera camera = null, bool snapEdge = false)
        {
            vertex = default(ProceduralVertex);
            error = "Click a visible surface on the selected part";
            float closest = 5000f;
            Vector3 hitWeights = Vector3.zero;
            bool found = false;
            MeshRecord picked = null;
            int[] pickedTriangle = null;
            foreach (MeshRecord r in meshes)
            {
                if (!Visible(r)) continue;
                Vector3 origin = r.transform.InverseTransformPoint(worldRay.origin);
                Vector3 direction = r.transform.InverseTransformVector(worldRay.direction);
                // Convert local ray distance back to world distance when
                // comparing surfaces with different local scale.
                Ray localRay = new Ray(origin, direction.normalized);
                float localPerWorld = direction.magnitude;
                if (localPerWorld <= 1e-8f) continue;
                Vector3[] live = r.currentVertices ?? r.instance.vertices;
                for (int sub = 0; sub < r.instance.subMeshCount; ++sub)
                {
                    if (r.instance.GetTopology(sub) != MeshTopology.Triangles) continue;
                    int[] triangles = r.instance.GetTriangles(sub);
                    for (int i = 0; i + 2 < triangles.Length; i += 3)
                    {
                        int a = triangles[i], b = triangles[i+1], c = triangles[i+2];
                        float distance; Vector3 weights;
                        if (!ProceduralMeshBuilder.RayTriangle(localRay, live[a], live[b], live[c],
                            out distance, out weights)) continue;
                        distance /= localPerWorld;
                        if (distance >= closest) continue;
                        closest = distance; found = true; hitWeights = weights;
                        picked = r; pickedTriangle = new[] { a, b, c };
                        vertex = new ProceduralVertex(r.vertexMeshKey, sub,
                            r.vertices[a] * weights.x + r.vertices[b] * weights.y + r.vertices[c] * weights.z,
                            Vector3.zero);
                    }
                }
            }
            if (!found) return false;
            // Do not insert through a different vessel part in the foreground.
            foreach (RaycastHit hit in Physics.RaycastAll(worldRay, closest))
            {
                Part owner = hit.collider != null ? hit.collider.GetComponentInParent<Part>() : null;
                if (owner != null && owner != part && EditorLogic.fetch != null &&
                    EditorLogic.fetch.ship != null && EditorLogic.fetch.ship.parts.Contains(owner))
                { error = "Another part is in front of the selected surface"; return false; }
            }
            if (snapEdge || !ProceduralMeshBuilder.Interior(hitWeights))
            {
                // Snap on the frontmost rendered triangle only. Ring-cut edges
                // are real triangle edges, so the same operation covers them.
                Vector3[] live = picked.currentVertices ?? picked.instance.vertices;
                Vector3 worldHit = worldRay.GetPoint(closest);
                int edge = -1;
                float fraction = 0f, best = float.PositiveInfinity;
                for (int k = 0; k < 3; ++k)
                {
                    Vector3 a = picked.transform.TransformPoint(live[pickedTriangle[k]]);
                    Vector3 b = picked.transform.TransformPoint(live[pickedTriangle[(k+1)%3]]);
                    Vector3 d = b-a;
                    if (d.sqrMagnitude <= 1e-20f) continue;
                    float u = Mathf.Clamp01(Vector3.Dot(worldHit-a,d)/d.sqrMagnitude);
                    Vector3 q = a+d*u;
                    float distance = (q-worldHit).sqrMagnitude;
                    if (camera != null)
                    {
                        Vector3 screen = camera.WorldToScreenPoint(q) - camera.WorldToScreenPoint(worldHit);
                        distance = screen.x*screen.x + screen.y*screen.y;
                    }
                    if (distance < best) { best = distance; edge = k; fraction = u; }
                }
                if (edge < 0 || (snapEdge && camera != null && best > 14f*14f))
                { error = "Click within 14 pixels of a visible edge or ring"; return false; }
                if (fraction <= ProceduralMeshBuilder.InteriorWeight || fraction >= 1f-ProceduralMeshBuilder.InteriorWeight)
                { error = "Click between existing vertices, not on an endpoint"; return false; }
                vertex.Anchor = Vector3.Lerp(picked.vertices[pickedTriangle[edge]],
                    picked.vertices[pickedTriangle[(edge+1)%3]], fraction);
            }
            error = ""; return true;
        }

        internal bool TryGetProceduralVertexWorld(int editIndex, out Vector3 world, out Transform meshTransform)
        {
            world = Vector3.zero; meshTransform = null;
            foreach (MeshRecord r in meshes)
            {
                if (!Visible(r)) continue;
                foreach (ProceduralVertexBinding binding in r.vertexBindings)
                    if (binding.EditIndex == editIndex)
                    {
                        Vector3[] live = r.currentVertices ?? r.instance.vertices;
                        if (binding.Index >= live.Length) return false;
                        world = r.transform.TransformPoint(live[binding.Index]);
                        meshTransform = r.transform;
                        return true;
                    }
            }
            return false;
        }

        // Reflect immutable anchors in the active part-local native bounds,
        // not the already displaced visual surface. Only active compatible
        // mesh objects participate; asymmetric geometry is never guessed.
        internal bool TryMirrorVertex(ProceduralVertex source, IList<ProceduralVertex> edits,
            int axis, out int existing, out ProceduralVertex mirror, out string error)
        {
            existing = -1; mirror = default(ProceduralVertex);
            error = "No matching surface across the selected mirror plane";
            if (axis < 0 || axis > 2) return false;
            MeshRecord owner = null;
            bool first = true;
            Vector3 lo = Vector3.zero, hi = Vector3.zero;
            foreach (MeshRecord r in meshes)
            {
                if (!Visible(r)) continue;
                if (r.vertexMeshKey == source.MeshKey) owner = r;
                Matrix4x4 toPart = part.transform.worldToLocalMatrix * r.transform.localToWorldMatrix;
                foreach (Vector3 v in r.nativeVertices)
                {
                    Vector3 p = toPart.MultiplyPoint3x4(v);
                    if (first) { lo = hi = p; first = false; }
                    else { lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p); }
                }
            }
            if (owner == null || first) return false;
            float tolerance = Mathf.Max(1e-6f, (hi - lo).magnitude * 0.00005f);
            Vector3 anchor = part.transform.InverseTransformPoint(owner.transform.TransformPoint(source.Anchor));
            Vector3 target = anchor;
            target[axis] = lo[axis] + hi[axis] - anchor[axis];
            // Center-plane anchors must bind to themselves, even when a
            // coincident separate mesh has another editable anchor.
            if ((target - anchor).sqrMagnitude <= tolerance * tolerance)
                for (int i = 0; i < edits.Count; ++i)
                    if (edits[i].MeshKey == source.MeshKey && edits[i].Submesh == source.Submesh &&
                        edits[i].Anchor.Equals(source.Anchor))
                    { existing = i; mirror = edits[i]; error = ""; return true; }
            // Existing counterpart.
            float best = tolerance * tolerance;
            for (int i = 0; i < edits.Count; ++i)
                foreach (MeshRecord r in meshes)
                {
                    if (!Visible(r) || r.vertexMeshKey != edits[i].MeshKey ||
                        !ActiveFallbackCompatible(owner, r)) continue;
                    Vector3 q = part.transform.InverseTransformPoint(r.transform.TransformPoint(edits[i].Anchor));
                    float distance = (q - target).sqrMagnitude;
                    if (distance > best) continue;
                    best = distance; existing = i; mirror = edits[i];
                }
            if (existing >= 0) { error = ""; return true; }
            // Find a point on a native triangle, even if its triangulation
            // differs from the source side. The usual replay path handles edges.
            MeshRecord match = null;
            int submesh = -1;
            Vector3 matchedAnchor = Vector3.zero;
            foreach (MeshRecord r in meshes)
            {
                if (!Visible(r) || !ActiveFallbackCompatible(owner, r)) continue;
                Matrix4x4 toPart = part.transform.worldToLocalMatrix * r.transform.localToWorldMatrix;
                for (int sub = 0; sub < r.native.subMeshCount; ++sub)
                {
                    if (r.native.GetTopology(sub) != MeshTopology.Triangles) continue;
                    int[] tris = r.native.GetTriangles(sub);
                    for (int j = 0; j + 2 < tris.Length; j += 3)
                    {
                        Vector3 a = r.nativeVertices[tris[j]], b = r.nativeVertices[tris[j+1]], c = r.nativeVertices[tris[j+2]];
                        Vector3 pa = toPart.MultiplyPoint3x4(a), pb = toPart.MultiplyPoint3x4(b), pc = toPart.MultiplyPoint3x4(c);
                        if (Vector3.Cross(pb-pa, pc-pa).sqrMagnitude <= 1e-20f) continue;
                        Vector3 w = ProceduralSurfacePhysics.ClosestWeights(target, pa, pb, pc);
                        Vector3 q = pa*w.x + pb*w.y + pc*w.z;
                        float distance = (q-target).sqrMagnitude;
                        if (distance > best || (match != null && distance >= best)) continue;
                        best = distance; match = r; submesh = sub;
                        matchedAnchor = a*w.x + b*w.y + c*w.z;
                    }
                }
            }
            if (match == null) return false;
            Vector3 offset = part.transform.InverseTransformVector(owner.transform.TransformVector(source.Offset));
            offset[axis] = -offset[axis];
            mirror = new ProceduralVertex(match.vertexMeshKey, submesh, matchedAnchor,
                match.transform.InverseTransformVector(part.transform.TransformVector(offset)), source.InfluenceRadius);
            error = ""; return true;
        }

        // The deformation is linear in stored offsets. Compute the response of
        // each selected handle to every selected edit, including hard binding
        // inheritance and overlapping soft fields, so the group moves rigidly.
        internal bool TryGetVertexGroupWeights(IList<int> selection, out double[] weights, IList<float> desired = null)
        {
            int n = selection.Count;
            weights = new double[n];
            if (n == 0 || (desired != null && desired.Count != n)) return false;
            var response = new double[n, n + 1];
            var owners = new MeshRecord[n];
            var indices = new int[n];
            for (int i = 0; i < n; ++i)
            {
                if (selection[i] < 0 || selection[i] >= proceduralVertices.Count) return false;
                foreach (MeshRecord r in meshes)
                {
                    if (!Visible(r) || r.vertexBaselinePart == null) continue;
                    foreach (ProceduralVertexBinding b in r.vertexBindings)
                        if (b.EditIndex == selection[i])
                        { owners[i] = r; indices[i] = b.Index; break; }
                    if (owners[i] != null) break;
                }
                if (owners[i] == null) return false;
                response[i, n] = desired == null ? 1d : desired[i];
            }
            for (int col = 0; col < n; ++col)
            {
                MeshRecord r = owners[col];
                ProceduralVertex edit = proceduralVertices[selection[col]];
                if (edit.InfluenceRadius > 0f)
                {
                    Vector3 anchor = r.vertexBaselinePart[indices[col]];
                    for (int row = 0; row < n; ++row)
                        if (owners[row] == r)
                            response[row, col] = row == col ? 1d :
                                ProceduralMeshBuilder.InfluenceWeight(
                                    (r.vertexBaselinePart[indices[row]] - anchor).magnitude,
                                    edit.InfluenceRadius);
                }
                else
                {
                    var inherited = new double[r.vertices.Length];
                    foreach (ProceduralVertexBinding b in r.vertexBindings)
                        inherited[b.Index] = inherited[b.A] * b.Weights.x +
                            inherited[b.B] * b.Weights.y + inherited[b.C] * b.Weights.z +
                            (b.EditIndex == selection[col] ? 1d : 0d);
                    for (int row = 0; row < n; ++row)
                        if (owners[row] == r) response[row, col] = inherited[indices[row]];
                }
            }
            return TrySolveVertexGroup(response, weights);
        }

        // Partial-pivot elimination. Reject ambiguous/ill-conditioned groups
        // rather than committing huge compensating offsets to a craft.
        internal static bool TrySolveVertexGroup(double[,] response, double[] weights)
        {
            int n = weights.Length;
            for (int col = 0; col < n; ++col)
            {
                int pivot = col;
                for (int row = col + 1; row < n; ++row)
                    if (Math.Abs(response[row, col]) > Math.Abs(response[pivot, col])) pivot = row;
                double divisor = response[pivot, col];
                if (double.IsNaN(divisor) || double.IsInfinity(divisor) || Math.Abs(divisor) < 1e-7d)
                    return false;
                for (int j = col; j <= n; ++j)
                {
                    double temp = response[col, j]; response[col, j] = response[pivot, j];
                    response[pivot, j] = temp;
                }
                for (int j = col; j <= n; ++j) response[col, j] /= divisor;
                for (int row = 0; row < n; ++row)
                {
                    if (row == col) continue;
                    double factor = response[row, col];
                    for (int j = col; j <= n; ++j) response[row, j] -= factor * response[col, j];
                }
            }
            for (int i = 0; i < n; ++i)
            {
                double value = response[i, n];
                if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > 1000d) return false;
                weights[i] = value;
            }
            return true;
        }

        private bool ValidateVertexTargets(IList<ProceduralVertex> edits, out string error)
        {
            error = "";
            if (edits == null || edits.Count > ProceduralMeshBuilder.MaxIndividualVertices)
            { error = "Invalid individual vertex count"; return false; }
            foreach (ProceduralVertex edit in edits)
            {
                if (string.IsNullOrEmpty(edit.MeshKey) || !ProceduralMeshBuilder.Finite(edit.Anchor) ||
                    !ProceduralMeshBuilder.Finite(edit.Offset) ||
                    !ProceduralMeshBuilder.ValidInfluenceRadius(edit.InfluenceRadius))
                { error = "Invalid individual vertex data"; return false; }
                int matches = 0;
                foreach (MeshRecord r in meshes)
                    if (r.filter != null && r.vertexMeshKey == edit.MeshKey) ++matches;
                if (matches != 1)
                { error = "The saved visual mesh is unavailable or ambiguous"; return false; }
            }
            return true;
        }

        private static bool HasVertexEdits(MeshRecord r, IList<ProceduralVertex> edits)
        {
            if (r.filter == null) return false;
            foreach (ProceduralVertex edit in edits)
                if (edit.MeshKey == r.vertexMeshKey) return true;
            return false;
        }

        internal bool CanSetProceduralRings(IList<ProceduralRing> requested, out string error)
        { return CanSetProceduralGeometry(requested, proceduralVertices, out error); }

        internal bool CanSetProceduralGeometry(IList<ProceduralRing> requested,
            IList<ProceduralVertex> edits, out string error)
        {
            error = "";
            if (!valid || !ProceduralMeshBuilder.Valid(requested))
            { error = "Unsupported mesh or cross-section parameters"; return false; }
            if (!ValidateVertexTargets(edits, out error)) return false;
            RecalculateBounds();
            foreach (MeshRecord r in meshes)
            {
                if (r.transform == null || r.instance == null) continue;
                if (requested.Count == 0 && !HasVertexEdits(r, edits)) continue;
                Matrix4x4 toPart = part.transform.worldToLocalMatrix * r.transform.localToWorldMatrix;
                Vector2 bounds = RingYRange(r);
                string key = TopologyKey(requested, toPart, bounds, r, edits);
                if (key == r.topologyKey) continue;
                Mesh test;
                if (!ProceduralMeshBuilder.TryBuild(r.native, toPart, requested,
                    bounds.x, bounds.y, r.collider != null && r.collider.convex,
                    out test, out error, r.vertexMeshKey, edits)) return false;
                if (test != null) UnityEngine.Object.Destroy(test);
            }
            return true;
        }

        internal bool SetProceduralRings(IList<ProceduralRing> requested, out string error)
        { return SetProceduralGeometry(requested, proceduralVertices, out error); }

        // Validate/rebuild all meshes atomically before replacing any of them.
        internal bool SetProceduralGeometry(IList<ProceduralRing> requested,
            IList<ProceduralVertex> edits, out string error)
        {
            error = "";
            if (!valid) { error = Error; return false; }
            if (!ProceduralMeshBuilder.Valid(requested))
            { error = "Cross-section positions must be ordered, separated, and in range"; return false; }
            if (!ValidateVertexTargets(edits, out error)) return false;
            var desired = new List<ProceduralRing>(requested);
            var desiredVertices = new List<ProceduralVertex>(edits);
            RecalculateBounds();
            if (!RebuildTopology(desired, false, out error, desiredVertices)) return false;
            proceduralRings.Clear();
            proceduralRings.AddRange(desired);
            proceduralVertices.Clear();
            proceduralVertices.AddRange(desiredVertices);
            return true;
        }

        // Offsets and ring dimensions/tilt do not change topology. Include
        // anchor order/index because bindings refer to the complete edit list.
        private string TopologyKey(IList<ProceduralRing> rings, Matrix4x4 localToPart,
            Vector2 bounds, MeshRecord r, IList<ProceduralVertex> edits)
        {
            if (rings.Count == 0 && !HasVertexEdits(r, edits)) return "";
            var key = new System.Text.StringBuilder();
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            key.Append(bounds.x.ToString("R", culture));
            key.Append('|').Append(bounds.y.ToString("R", culture));
            for (int i = 0; i < 16; ++i) key.Append('|').Append(localToPart[i].ToString("R", culture));
            foreach (ProceduralRing ring in rings) key.Append('|').Append(ring.Height.ToString("R", culture));
            if (r.filter != null)
                for (int i = 0; i < edits.Count; ++i)
                {
                    ProceduralVertex e = edits[i];
                    if (e.MeshKey != r.vertexMeshKey) continue;
                    key.Append("|V").Append(i).Append(':').Append(e.Submesh).Append(':')
                        .Append(e.Anchor.x.ToString("R", culture)).Append(':')
                        .Append(e.Anchor.y.ToString("R", culture)).Append(':')
                        .Append(e.Anchor.z.ToString("R", culture));
                }
            return key.ToString();
        }

        private bool RebuildTopology(IList<ProceduralRing> rings, bool force,
            out string error, IList<ProceduralVertex> edits)
        {
            error = "";
            var replacements = new List<Mesh>();
            var keys = new List<string>();
            var allBindings = new List<List<ProceduralVertexBinding>>();
            foreach (MeshRecord r in meshes)
            {
                var bindings = new List<ProceduralVertexBinding>();
                allBindings.Add(bindings);
                if (r.transform == null || r.instance == null)
                { replacements.Add(null); keys.Add(""); continue; }
                Matrix4x4 toPart = part.transform.worldToLocalMatrix * r.transform.localToWorldMatrix;
                Vector2 bounds = RingYRange(r);
                string key = TopologyKey(rings, toPart, bounds, r, edits);
                keys.Add(key);
                if (!force && key == r.topologyKey)
                { replacements.Add(null); continue; }
                Mesh prepared;
                if (key.Length != 0)
                {
                    if (!ProceduralMeshBuilder.TryBuild(r.native, toPart, rings,
                        bounds.x, bounds.y, r.collider != null && r.collider.convex,
                        out prepared, out error, r.vertexMeshKey, edits, bindings))
                    {
                        foreach (Mesh mesh in replacements)
                            if (mesh != null) UnityEngine.Object.Destroy(mesh);
                        return false;
                    }
                }
                else prepared = UnityEngine.Object.Instantiate(r.native);
                prepared.name = "PartSculptor face morph " + r.native.name;
                replacements.Add(prepared);
            }
            for (int i = 0; i < meshes.Count; ++i)
            {
                Mesh replacement = replacements[i];
                if (replacement == null) continue;
                MeshRecord r = meshes[i];
                Mesh previous = r.instance;
                r.instance = replacement;
                r.vertices = replacement.vertices;
                r.currentVertices = null;
                r.vertexBaselinePart = null;
                r.vertexBindings = allBindings[i];
                r.topologyKey = keys[i];
                if (r.filter != null) r.filter.sharedMesh = replacement;
                if (r.collider != null)
                {
                    r.collider.sharedMesh = null;
                    r.collider.sharedMesh = replacement;
                }
                if (previous != null) UnityEngine.Object.Destroy(previous);
            }
            return true;
        }

        // Called after PartSculptor resets native attach-node positions on an
        // ordinary X/Y/Z edit. Never capture already-morphed nodes as baseline.
        internal void RebaseNodes()
        {
            nodes.Clear();
            if (part == null) return;
            foreach (AttachNode n in part.attachNodes)
                if (n != null) nodes[n] = new NodeRecord {
                    position = n.position, originalPosition = n.originalPosition,
                    orientation = n.orientation, size = n.size
                };
            if (part.srfAttachNode != null)
            {
                AttachNode n = part.srfAttachNode;
                nodes[n] = new NodeRecord { position = n.position,
                    originalPosition = n.originalPosition, orientation = n.orientation };
            }
        }

        // Use the native (pre-morph) node pose and the undistorted native
        // mesh bounds. Neither the node label nor its CURRENT tilted normal
        // reliably identifies the parent/child end of a morphed part.
        internal bool TryGetEndFace(AttachNode node, out int face)
        {
            face = -1;
            NodeRecord native;
            if (!valid || node == null || !nodes.TryGetValue(node, out native))
                return false;
            float magnitude = native.orientation.magnitude;
            if (magnitude < 0.01f ||
                Mathf.Abs(native.orientation.y) / magnitude < 0.65f)
                return false; // Not an axial/stack end face.
            face = native.orientation.y > 0f ? 1 : 0;
            float endY = face == 0 ? min.y : max.y;
            if (Mathf.Abs(native.position.y - endY) >
                (max.y - min.y) * 0.20f)
            {
                face = -1;
                return false; // Recessed/side/inset connection.
            }
            return true;
        }

        internal bool TryGetNativeNodePosition(AttachNode node,
            out Vector3 nativePosition)
        {
            NodeRecord native;
            if (valid && node != null && nodes.TryGetValue(node, out native))
            {
                nativePosition = native.position;
                return true;
            }
            nativePosition = Vector3.zero;
            return false;
        }

        internal Vector3 Corner(int face, int corner, MorphFace bottom, MorphFace top)
        {
            Vector3 point = new Vector3((corner & 1) == 0 ? min.x : max.x,
                face == 0 ? min.y : max.y,
                (corner & 2) == 0 ? min.z : max.z);
            return FaceMorphMath.Transform(point, min, max, bottom, top);
        }

        private sealed class PreparedSurface
        {
            internal MeshRecord Record;
            internal ProceduralSurfacePhysics Surface;
            internal List<ProceduralVertexBinding> Bindings;
            internal Vector3[] LocalVertices;
            internal ProceduralConvexHull CookedCollision;
            internal ProceduralConvexHull GeneratedCollision;
            internal double GeneratedBaselineVolume;
            internal bool CollisionBaselineChecked, CollisionBaselineUsable;
        }

        private PreparedSurface PrepareSurface(MeshRecord r, Mesh topology, Vector3[] native,
            IList<ProceduralRing> rings, IList<ProceduralVertex> edits,
            List<ProceduralVertexBinding> bindings, MorphFace bottom, MorphFace top)
        {
            var result = new PreparedSurface { Record = r, Surface = new ProceduralSurfacePhysics(),
                Bindings = bindings, LocalVertices = new Vector3[native.Length] };
            Vector3[] transformed = result.LocalVertices;
            Vector3[] baseline = new Vector3[native.Length];
            Vector2 bounds = RingYRange(r);
            for (int i = 0; i < native.Length; ++i)
            {
                Vector3 p = part.transform.InverseTransformPoint(r.transform.TransformPoint(native[i]));
                ProceduralProfileSample profile = ProceduralMeshBuilder.Profile(p.y,
                    bounds.x, bounds.y, rings);
                float centeredX = (p.x - (min.x + max.x) * 0.5f) * profile.Width;
                float centeredZ = (p.z - (min.z + max.z) * 0.5f) * profile.Depth;
                p.x = centeredX + (min.x + max.x) * 0.5f;
                p.z = centeredZ + (min.z + max.z) * 0.5f;
                p.y += centeredX * profile.TiltX + centeredZ * profile.TiltZ;
                Vector3 newP = FaceMorphMath.Transform(p, min, max, bottom, top);
                baseline[i] = transformed[i] = r.transform.InverseTransformPoint(
                    part.transform.TransformPoint(newP));
            }
            foreach (ProceduralVertexBinding binding in bindings)
            {
                Vector3 w = binding.Weights;
                baseline[binding.Index] = baseline[binding.A] * w.x +
                    baseline[binding.B] * w.y + baseline[binding.C] * w.z;
                transformed[binding.Index] = transformed[binding.A] * w.x +
                    transformed[binding.B] * w.y + transformed[binding.C] * w.z +
                    (edits[binding.EditIndex].InfluenceRadius <= 0f ?
                        edits[binding.EditIndex].Offset : Vector3.zero);
            }
            Matrix4x4 toPart = part.transform.worldToLocalMatrix * r.transform.localToWorldMatrix;
            // Add each soft edit once, using immutable ring/morph baseline
            // distances. Equal positions receive equal weights at UV seams.
            // Do not also interpolate its offset through later bindings.
            var softEdits = new HashSet<int>();
            foreach (ProceduralVertexBinding binding in bindings)
            {
                if (!softEdits.Add(binding.EditIndex)) continue;
                ProceduralVertex edit = edits[binding.EditIndex];
                if (edit.InfluenceRadius <= 0f || edit.Offset.sqrMagnitude == 0f) continue;
                Vector3 anchor = toPart.MultiplyPoint3x4(baseline[binding.Index]);
                for (int i = 0; i < native.Length; ++i)
                {
                    float distance = (toPart.MultiplyPoint3x4(baseline[i]) - anchor).magnitude;
                    float weight = i == binding.Index ? 1f :
                        ProceduralMeshBuilder.InfluenceWeight(distance, edit.InfluenceRadius);
                    transformed[i] += edit.Offset * weight;
                }
            }
            for (int i = 0; i < native.Length; ++i)
            {
                result.Surface.Base.Add(toPart.MultiplyPoint3x4(baseline[i]));
                result.Surface.Edited.Add(toPart.MultiplyPoint3x4(transformed[i]));
            }
            for (int sub = 0; sub < topology.subMeshCount; ++sub)
                result.Surface.Triangles.AddRange(topology.GetTriangles(sub));
            return result;
        }

        // First use the original compatibility rule, but do not transfer
        // edits between active and inactive objects, even for equal names.
        private static bool CompatibleSurfaces(MeshRecord visual, MeshRecord collision)
        {
            if (visual.transform.gameObject.activeInHierarchy != collision.transform.gameObject.activeInHierarchy)
                return false;
            return visual.b9VariantTransform == null || collision.b9VariantTransform == null ||
                visual.b9VariantTransform == collision.b9VariantTransform;
        }

        // Different literal B9 transform names need not mean different
        // subtypes. Reject pairs which a common switch never selects together.
        // Inactive alternatives may share physics only under the strict rule.
        private bool ActiveFallbackCompatible(MeshRecord visual, MeshRecord collision)
        {
            if (!visual.transform.gameObject.activeInHierarchy ||
                !collision.transform.gameObject.activeInHierarchy) return false;
            string a = visual.b9VariantTransform, b = collision.b9VariantTransform;
            if (a == null || b == null || a == b) return true;
            foreach (List<HashSet<string>> module in b9SubtypeSelectors)
            {
                bool hasA = false, hasB = false, together = false;
                foreach (HashSet<string> subtype in module)
                {
                    bool sa = subtype.Contains(a), sb = subtype.Contains(b);
                    hasA |= sa; hasB |= sb; together |= sa && sb;
                }
                if (hasA && hasB && !together) return false;
            }
            // Separate independent switches are safe only while both are active.
            return true;
        }

        private static bool EligibleCollider(MeshRecord r)
        {
            // Primitive proxies are disabled at identity; the native primitive
            // is their effective collider. Do not mistake that for a disabled
            // collision surface. Never enable a disabled native collider.
            return r.collider != null && !r.collider.isTrigger && r.originalEnabled &&
                (r.collider.enabled || (r.primitive != null && r.primitive.enabled));
        }

        private static string CollisionPath(Transform t)
        {
            var names = new List<string>();
            for (; t != null; t = t.parent) names.Add(t.name);
            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        private string CollisionLabel(MeshRecord r)
        {
            return CollisionPath(r.transform) + " mesh=" + r.native.name +
                " variant=" + (r.b9VariantTransform ?? "shared") +
                " active=" + r.transform.gameObject.activeInHierarchy;
        }

        // Include unregistered colliders, not just the candidates that made
        // it into meshes. This is essential when the actual problem is a
        // disabled, trigger-only, meshless, or otherwise skipped collider.
        private void LogCollisionInventory(string reason, bool warning = true)
        {
            if (part == null) return;
            string summary = "PG collision fallback: " + part.name + ": " + reason;
            if (warning) Log.Warning(summary); else Log.Info(summary);
            foreach (Collider c in part.GetComponentsInChildren<Collider>(true))
            {
                if (c == null || c.GetComponentInParent<Part>() != part) continue;
                MeshCollider mc = c as MeshCollider;
                bool registered = meshes.Exists(r => r.collider == c || r.primitive == c);
                string state = IsGeneratedVertexCollider(mc) ? "generated fallback (not native candidate)" :
                    registered ? "registered" : c.isTrigger ? "excluded trigger" :
                    !c.enabled ? "excluded disabled" : mc != null && mc.sharedMesh == null ?
                    "excluded missing mesh" : "unregistered/unsupported";
                string entry = "PG collision inventory: " + CollisionPath(c.transform) +
                    " type=" + c.GetType().Name + " active=" + c.gameObject.activeInHierarchy +
                    " enabled=" + c.enabled + " trigger=" + c.isTrigger + " " + state +
                    (mc != null && mc.sharedMesh != null ? " mesh=" + mc.sharedMesh.name : "");
                if (warning) Log.Warning(entry); else Log.Info(entry);
            }
        }

        private static bool UsableCollisionBaseline(PreparedSurface shape)
        {
            if (!shape.CollisionBaselineChecked)
            {
                shape.CollisionBaselineChecked = true;
                if (!shape.Record.collider.convex) shape.CollisionBaselineUsable = true;
                else
                {
                    ProceduralConvexHull low, high;
                    shape.CollisionBaselineUsable = ProceduralConvexHull.TryPair(
                        shape.Surface.Base, shape.Surface.Base, out low, out high);
                }
            }
            return shape.CollisionBaselineUsable;
        }

        private bool TrySelectCollision(List<PreparedSurface> surfaces, PreparedSurface visual,
            Vector3 anchor, out PreparedSurface nearest, out ProceduralSurfacePhysics.Hit best,
            out string report)
        {
            nearest = null;
            best = new ProceduralSurfacePhysics.Hit { Triangle = -1, DistanceSquared = float.PositiveInfinity };
            var notes = new List<string>();
            // Rank strict compatibility first, then other currently active
            // colliders in compatible B9 groups. Distance breaks ties in each tier.
            for (int tier = 0; tier < 2; ++tier)
            {
                foreach (PreparedSurface collision in surfaces)
                {
                    MeshRecord r = collision.Record;
                    if (r.collider == null) continue;
                    string why;
                    bool strict = CompatibleSurfaces(visual.Record, collision.Record);
                    bool fallback = ActiveFallbackCompatible(visual.Record, collision.Record);
                    ProceduralSurfacePhysics.Hit hit;
                    if (!EligibleCollider(r)) why = "disabled/trigger";
                    else if (tier == 0 ? !strict : strict || !fallback)
                        why = !strict && !fallback ? "incompatible B9 variant or activation" : "other tier";
                    else if (!collision.Surface.TryClosest(anchor, out hit)) why = "no valid triangle";
                    else if (!UsableCollisionBaseline(collision)) why = "no closed convex baseline";
                    else
                    {
                        why = "candidate distance=" + Mathf.Sqrt(hit.DistanceSquared).ToString("G5");
                        if (hit.DistanceSquared < best.DistanceSquared)
                        { nearest = collision; best = hit; }
                    }
                    notes.Add("tier " + tier + " " + CollisionLabel(r) + ": " + why);
                }
                if (nearest != null)
                {
                    report = string.Join("; ", notes.ToArray());
                    string message = "visual=" + CollisionLabel(visual.Record) +
                        " selected tier " + tier + " " + CollisionLabel(nearest.Record);
                    if (tier != 0) LogCollisionInventory(message + "; " + report, false);
                    else Log.Debug("PG collision fallback: " + part.name + ": " + message + "; " + report);
                    return true;
                }
            }
            report = notes.Count == 0 ? "no registered collision records" : string.Join("; ", notes.ToArray());
            return false;
        }

        private bool ClosestVisual(List<PreparedSurface> surfaces, MeshRecord collider,
            Vector3 point, out ProceduralSurfacePhysics.Hit best)
        {
            best = new ProceduralSurfacePhysics.Hit { Triangle = -1, DistanceSquared = float.PositiveInfinity };
            foreach (PreparedSurface shape in surfaces)
            {
                MeshRecord r = shape.Record;
                if (r.filter == null || (collider != null ?
                    !(CompatibleSurfaces(r, collider) || ActiveFallbackCompatible(r, collider)) : !Visible(r))) continue;
                ProceduralSurfacePhysics.Hit hit;
                if (shape.Surface.TryClosest(point, out hit) && hit.DistanceSquared < best.DistanceSquared)
                    best = hit;
            }
            return best.Triangle >= 0;
        }

        private bool PrepareVertexPhysics(List<PreparedSurface> surfaces,
            IList<ProceduralVertex> edits, out double volume, out string error)
        {
            volume = 1d; error = "";
            if (edits.Count == 0) return true;
            bool anyOffset = false;
            foreach (ProceduralVertex edit in edits)
                if (edit.Offset.sqrMagnitude > 1e-14f) { anyOffset = true; break; }
            if (!anyOffset) return true;
            foreach (PreparedSurface shape in surfaces)
                foreach (Vector3 point in shape.Surface.Edited)
                    if (!ProceduralMeshBuilder.Finite(point))
                    { error = "Invalid vertex after deformation"; return false; }
            // Move existing collision vertices by interpolating the same
            // displacement field used by the visual surface.
            foreach (PreparedSurface collision in surfaces)
            {
                if (!EligibleCollider(collision.Record)) continue;
                for (int i = 0; i < collision.Surface.Base.Count; ++i)
                {
                    ProceduralSurfacePhysics.Hit hit;
                    if (ClosestVisual(surfaces, collision.Record, collision.Surface.Base[i], out hit) &&
                        hit.Delta.sqrMagnitude > 1e-14f && UsableCollisionBaseline(collision))
                        collision.Surface.Edited[i] += hit.Delta;
                }
            }
            // Coarse collision meshes may have no vertex inside a local edit.
            // Add a support point to the nearest compatible collider triangle.
            foreach (PreparedSurface visual in surfaces)
            {
                if (visual.Record.filter == null) continue;
                var supportEdits = new HashSet<int>();
                foreach (ProceduralVertexBinding binding in visual.Bindings)
                {
                    if (!supportEdits.Add(binding.EditIndex)) continue;
                    Vector3 anchor = visual.Surface.Base[binding.Index];
                    Vector3 delta = visual.Surface.Edited[binding.Index] - anchor;
                    if (delta.sqrMagnitude <= 1e-14f) continue;
                    PreparedSurface nearest;
                    ProceduralSurfacePhysics.Hit best;
                    string report;
                    if (!TrySelectCollision(surfaces, visual, anchor, out nearest, out best, out report))
                    {
                        // An inactive saved B9 edit must not change an active
                        // alternative's collider. Replay on subtype activation.
                        if (!visual.Record.transform.gameObject.activeInHierarchy)
                        {
                            Log.Debug("PG collision fallback deferred inactive visual: " + CollisionLabel(visual.Record));
                            continue;
                        }
                        if (!Settings.GenerateVertexCollisionFallback)
                        {
                            error = "No compatible collision surface for individual vertex: " +
                                (report == "no registered collision records" ? report : "all candidates rejected") +
                                "; see PG collision inventory in KSP.log (visual hull fallback disabled)";
                            LogCollisionInventory(CollisionLabel(visual.Record) + "; " + report);
                            return false;
                        }
                        if (visual.GeneratedCollision == null)
                        {
                            ProceduralConvexHull low, high;
                            if (!ProceduralConvexHull.TryPair(visual.Surface.Base, visual.Surface.Edited, out low, out high))
                            {
                                error = "No usable native collider; visual mesh cannot form a closed convex hull";
                                LogCollisionInventory(CollisionLabel(visual.Record) + "; " + report + "; " + error);
                                return false;
                            }
                            visual.GeneratedCollision = high;
                            visual.GeneratedBaselineVolume = low.Volume();
                            LogCollisionInventory("Using opt-in visual-derived convex hull for " +
                                CollisionLabel(visual.Record) + "; " + report);
                        }
                        continue;
                    }
                    if (!nearest.Surface.AddSupport(best, delta, nearest.Record.collider.convex, out error))
                        return false;
                }
            }
            double before = 0d, after = 0d;
            bool activeDisplacement = false;
            foreach (PreparedSurface shape in surfaces)
            {
                if (shape.GeneratedCollision != null)
                {
                    // Baseline and edited hulls use the same visual point set.
                    // Other native collision shells may overlap this approximation.
                    before += shape.GeneratedBaselineVolume;
                    after += shape.GeneratedCollision.Volume();
                    activeDisplacement = true;
                }
                if (!EligibleCollider(shape.Record)) continue;
                bool active = shape.Record.originalEnabled && shape.Record.transform.gameObject.activeInHierarchy;
                bool displaced = HasCollisionDisplacement(shape.Surface);
                if (!active && !displaced) continue;
                foreach (Vector3 p in shape.Surface.Edited)
                    if (!ProceduralMeshBuilder.Finite(p))
                    { error = "Invalid collision vertex after deformation"; return false; }
                double lowVolume, highVolume;
                if (shape.Record.collider.convex)
                {
                    ProceduralConvexHull low, high;
                    if (!ProceduralConvexHull.TryPair(shape.Surface.Base, shape.Surface.Edited, out low, out high))
                    {
                        // A flat/degenerate untouched auxiliary collider need
                        // not block edits to another solid collider.
                        if (!displaced) continue;
                        error = "Cannot build a closed convex collision hull for " + shape.Record.native.name;
                        return false;
                    }
                    lowVolume = low.Volume(); highVolume = high.Volume();
                    if (displaced) shape.CookedCollision = high;
                }
                else
                {
                    lowVolume = shape.Surface.Volume(false);
                    highVolume = shape.Surface.Volume(true);
                }
                if (!active) continue;
                activeDisplacement |= displaced;
                before += lowVolume; after += highVolume;
            }
            if (!activeDisplacement) return true;
            if (!(before > 1e-12d) || !(after > before * 1e-6d) ||
                double.IsInfinity(after) || double.IsInfinity(before))
            { error = "Cannot calculate a positive enclosed collision volume"; return false; }
            volume = after / before;
            return true;
        }

        // Dry-run the exact same deformation used at commit, including all
        // prospective collision subdivisions. No live meshes are mutated.
        internal bool TryEvaluateVertexVolume(IList<ProceduralRing> rings,
            IList<ProceduralVertex> edits, MorphFace bottom, MorphFace top,
            out double volume, out string error)
        {
            volume = 1d; error = "";
            if (edits.Count == 0) return true;
            if (!valid || !ValidateVertexTargets(edits, out error)) return false;
            RecalculateBounds();
            var surfaces = new List<PreparedSurface>();
            foreach (MeshRecord r in meshes)
            {
                if (r.transform == null || r.instance == null) continue;
                Mesh temporary = null;
                Mesh topology = r.native;
                var bindings = new List<ProceduralVertexBinding>();
                if (rings.Count != 0 || HasVertexEdits(r, edits))
                {
                    Matrix4x4 toPart = part.transform.worldToLocalMatrix * r.transform.localToWorldMatrix;
                    Vector2 bounds = RingYRange(r);
                    if (!ProceduralMeshBuilder.TryBuild(r.native, toPart, rings, bounds.x, bounds.y,
                        r.collider != null && r.collider.convex, out temporary, out error,
                        r.vertexMeshKey, edits, bindings)) return false;
                    topology = temporary;
                }
                try { surfaces.Add(PrepareSurface(r, topology, topology.vertices, rings, edits, bindings, bottom, top)); }
                finally { if (temporary != null) UnityEngine.Object.Destroy(temporary); }
            }
            return PrepareVertexPhysics(surfaces, edits, out volume, out error);
        }

        private void MoveNodeWithVertices(List<PreparedSurface> surfaces, AttachNode node)
        {
            ProceduralSurfacePhysics.Hit hit;
            if (ClosestVisual(surfaces, null, node.position, out hit))
            {
                node.position += hit.Delta;
                if (hit.BeforeNormal.sqrMagnitude > 1e-12f && hit.AfterNormal.sqrMagnitude > 1e-12f)
                    node.orientation = Quaternion.FromToRotation(hit.BeforeNormal, hit.AfterNormal) * node.orientation;
            }
            if (ClosestVisual(surfaces, null, node.originalPosition, out hit))
                node.originalPosition += hit.Delta;
        }

        private static bool HasCollisionDisplacement(ProceduralSurfacePhysics surface)
        {
            for (int i = 0; i < surface.Base.Count; ++i)
                if ((surface.Edited[i] - surface.Base[i]).sqrMagnitude > 1e-14f) return true;
            return false;
        }

        private static void RemoveGeneratedCollision(MeshRecord r)
        {
            if (r.generatedCollider != null)
            {
                r.generatedCollider.enabled = false;
                r.generatedCollider.sharedMesh = null;
                UnityEngine.Object.Destroy(r.generatedCollider);
                r.generatedCollider = null;
            }
            if (r.generatedMesh != null) UnityEngine.Object.Destroy(r.generatedMesh);
            r.generatedMesh = null;
        }

        private void ApplyGeneratedCollision(PreparedSurface shape)
        {
            MeshRecord r = shape.Record;
            if (shape.GeneratedCollision == null)
            { RemoveGeneratedCollision(r); return; }
            // Only called after all dry-run physics and volume checks passed.
            // Attach to the visual object's existing transform: B9 activation
            // naturally controls it, and no sibling indexes change.
            var mesh = new Mesh { name = GeneratedColliderPrefix + r.native.name };
            Matrix4x4 fromPart = r.transform.worldToLocalMatrix * part.transform.localToWorldMatrix;
            var vertices = new List<Vector3>();
            foreach (Vector3 p in shape.GeneratedCollision.Vertices)
                vertices.Add(fromPart.MultiplyPoint3x4(p));
            mesh.SetVertices(vertices);
            mesh.SetTriangles(shape.GeneratedCollision.Triangles, 0);
            mesh.RecalculateBounds();
            if (r.generatedCollider == null)
            {
                r.generatedCollider = r.transform.gameObject.AddComponent<MeshCollider>();
                r.generatedCollider.enabled = false;
                r.generatedCollider.sharedMesh = null;
                r.generatedCollider.convex = true;
                r.generatedCollider.isTrigger = false;
            }
            Mesh previous = r.generatedMesh;
            r.generatedCollider.sharedMesh = null;
            r.generatedCollider.sharedMesh = mesh;
            r.generatedMesh = mesh;
            r.generatedCollider.enabled = true;
            if (previous != null) UnityEngine.Object.Destroy(previous);
        }

        private void ApplyPreparedSurface(PreparedSurface shape, bool identity, bool individual)
        {
            MeshRecord r = shape.Record;
            // Visual geometry keeps the original submeshes and attributes.
            r.currentVertices = r.filter != null ? shape.LocalVertices : null;
            r.vertexBaselinePart = r.filter != null ? shape.Surface.Base.ToArray() : null;
            r.instance.vertices = shape.LocalVertices;
            r.instance.RecalculateNormals();
            r.instance.RecalculateBounds();
            if (r.filter != null) ApplyGeneratedCollision(shape);
            if (r.collider == null) return;
            Mesh previous = r.vertexCollider;
            r.vertexCollider = null;
            if (individual && (shape.Surface.Base.Count != shape.LocalVertices.Length ||
                HasCollisionDisplacement(shape.Surface)))
            {
                Matrix4x4 fromPart = r.transform.worldToLocalMatrix * part.transform.localToWorldMatrix;
                IList<Vector3> collisionPoints = shape.CookedCollision != null ?
                    shape.CookedCollision.Vertices : shape.Surface.Edited;
                List<int> collisionTriangles = shape.CookedCollision != null ?
                    shape.CookedCollision.Triangles : shape.Surface.Triangles;
                var vertices = new List<Vector3>(collisionPoints.Count);
                foreach (Vector3 point in collisionPoints)
                    vertices.Add(fromPart.MultiplyPoint3x4(point));
                var mesh = new Mesh();
                mesh.name = MorphMeshPrefix + r.native.name + " vertex collision";
                mesh.SetVertices(vertices);
                mesh.SetTriangles(collisionTriangles, 0);
                mesh.RecalculateBounds();
                r.vertexCollider = mesh;
            }
            identity = identity && r.vertexCollider == null;
            if (r.primitive != null)
            {
                r.collider.enabled = false;
                r.primitive.enabled = identity && r.originalEnabled;
            }
            r.collider.sharedMesh = null;
            r.collider.sharedMesh = r.vertexCollider != null ? r.vertexCollider : r.instance;
            if (r.primitive != null) r.collider.enabled = !identity && r.originalEnabled;
            if (previous != null) UnityEngine.Object.Destroy(previous);
        }

        internal void Apply(MorphFace bottom, MorphFace top, bool moveAttachments,
            Part stationaryConnectedChild = null)
        {
            if (!valid) return;
            // Base geometry reflects current regular PartSculptor dimensions.
            RecalculateBounds();
            if (proceduralRings.Count != 0 || proceduralVertices.Count != 0)
            {
                string topologyError;
                if (!RebuildTopology(proceduralRings, false, out topologyError, proceduralVertices))
                {
                    Error = topologyError;
                    return; // preserve the last valid mesh and node state
                }
            }
            bool individual = proceduralVertices.Count != 0;
            bool identity = FaceMorphMath.IsIdentity(bottom) &&
                FaceMorphMath.IsIdentity(top) && proceduralRings.Count == 0;
            var surfaces = new List<PreparedSurface>();
            foreach (MeshRecord r in meshes)
            {
                if (r.transform == null || r.instance == null) continue;
                surfaces.Add(PrepareSurface(r, r.instance, r.vertices, proceduralRings,
                    proceduralVertices, r.vertexBindings, bottom, top));
            }
            double volume; string physicsError;
            if (!PrepareVertexPhysics(surfaces, proceduralVertices, out volume, out physicsError))
            { Error = physicsError; Log.Warning("Cannot apply individual vertex physics: " + physicsError); return; }
            VertexVolumeFactor = volume;
            Error = "";
            foreach (PreparedSurface shape in surfaces)
                ApplyPreparedSurface(shape, identity, individual);
            // Calculate the new node poses first, then move the connected
            // subtrees. This ensures a part attached through our parent node
            // can translate the owner before the child connection pivots are
            // evaluated, independent of dictionary enumeration order.
            Vector3 selfMove = Vector3.zero;
            List<AttachedChildMotion> childMotions = moveAttachments ?
                new List<AttachedChildMotion>() : null;
            foreach (KeyValuePair<AttachNode, NodeRecord> pair in nodes)
            {
                AttachNode node = pair.Key;
                if (node == null) continue;
                NodeRecord original = pair.Value;
                Vector3 was = node.position;
                Vector3 previousOrientation = node.orientation;
                node.position = FaceMorphMath.Transform(original.position, min, max, bottom, top);
                node.originalPosition = FaceMorphMath.Transform(original.originalPosition,
                    min, max, bottom, top);
                // Tilt alters the face's surface normal, even when its Y-axis
                // tangent is unchanged. Derive the new normal from the exact
                // deformation instead of transforming a short direction line.
                // Absolute original baselines prevent repeated-drag drift.
                node.orientation = FaceMorphMath.TransformNormal(original.position,
                    original.orientation, min, max, bottom, top);
                if (individual) MoveNodeWithVertices(surfaces, node);
                float t = Mathf.Clamp01((original.position.y - min.y) /
                    Mathf.Max(0.001f, max.y - min.y));
                float width = Mathf.Lerp(bottom.Width, top.Width, t);
                float depth = Mathf.Lerp(bottom.Depth, top.Depth, t);
                node.size = ScaleMath.NodeSizeTier(original.size,
                    original.orientation.x, original.orientation.y, original.orientation.z,
                    width, 1f, depth);
                if (!moveAttachments || node.attachedPart == null) continue;
                if (node.attachedPart == part.parent)
                {
                    // Keep our connection to the parent at the same world
                    // position. Never rotate/move the editor's parent vessel.
                    selfMove -= node.position - was;
                }
                else if (node.attachedPart.parent == part &&
                    node.attachedPart != stationaryConnectedChild)
                {
                    childMotions.Add(new AttachedChildMotion {
                        node = node,
                        child = node.attachedPart,
                        previousPosition = was,
                        previousOrientation = previousOrientation
                    });
                }
            }
            if (!moveAttachments) return;
            if (selfMove.sqrMagnitude > 1e-12f)
                part.transform.position += part.transform.TransformVector(selfMove);

            foreach (AttachedChildMotion motion in childMotions)
            {
                Part child = motion.child;
                if (child == null || motion.node == null || child.parent != part)
                    continue;

                // Use the attach node as the pivot, not the child's origin:
                // engines, adapters and offset child parts can have their
                // transforms a long way from their connection point.
                Vector3 beforeAnchor = part.transform.TransformPoint(motion.previousPosition);
                Vector3 afterAnchor = part.transform.TransformPoint(motion.node.position);
                Vector3 oldNormal = part.transform.TransformDirection(motion.previousOrientation);
                Vector3 newNormal = part.transform.TransformDirection(motion.node.orientation);
                Quaternion turn = Quaternion.identity;
                if (oldNormal.sqrMagnitude > 1e-12f && newNormal.sqrMagnitude > 1e-12f)
                    turn = Quaternion.FromToRotation(oldNormal, newNormal);

                // Assign world rotation and position together. All parts
                // further down this child's hierarchy follow automatically.
                Vector3 offsetFromNode = child.transform.position - beforeAnchor;
                Quaternion previousRotation = child.transform.rotation;
                child.transform.rotation = turn * previousRotation;
                child.transform.position = afterAnchor + turn * offsetFromNode;
            }
        }

        internal void Dispose()
        {
            valid = false;
            foreach (MeshRecord r in meshes)
            {
                RemoveGeneratedCollision(r);
                if (r.filter != null && r.filter.sharedMesh == r.instance)
                    r.filter.sharedMesh = r.native;
                if (r.primitive != null)
                {
                    r.collider.enabled = false;
                    r.primitive.enabled = r.originalEnabled;
                    if (r.collider != null) UnityEngine.Object.Destroy(r.collider);
                }
                else if (r.collider != null && (r.collider.sharedMesh == r.instance ||
                    r.collider.sharedMesh == r.vertexCollider))
                    r.collider.sharedMesh = r.native;
                if (r.vertexCollider != null) UnityEngine.Object.Destroy(r.vertexCollider);
                if (r.instance != null) UnityEngine.Object.Destroy(r.instance);
                if (r.ownedNative && r.native != null)
                    UnityEngine.Object.Destroy(r.native);
            }
            meshes.Clear();
            nodes.Clear();
        }
    }

    // Keep the hull helper in this compilation unit so FaceMorphSupport
    // does not depend on a separate source file being added to older projects.
    internal sealed class ProceduralConvexHull
    {
        internal readonly List<Vector3> Vertices = new List<Vector3>();
        internal readonly List<int> Triangles = new List<int>();
        private struct Face
        {
            internal int A, B, C;
            internal Vector3 Normal;
            internal float Distance(Vector3 p, IList<Vector3> points)
            { return Vector3.Dot(Normal, p - points[A]); }
        }
        private struct Edge { internal int A, B, Count; }
        private const int FaceLimit = 252; // leave headroom below Unity's 255

        private static bool MakeFace(IList<Vector3> p, int a, int b, int c,
            Vector3 inside, float epsilon, out Face face)
        {
            face = default(Face);
            Vector3 ab = p[b] - p[a], ac = p[c] - p[a];
            Vector3 normal = Vector3.Cross(ab, ac);
            float magnitude = normal.magnitude;
            if (!(magnitude > epsilon * Mathf.Max(ab.magnitude, ac.magnitude))) return false;
            normal /= magnitude;
            if (Vector3.Dot(normal, inside - p[a]) > 0f)
            { int t = b; b = c; c = t; normal = -normal; }
            face = new Face { A = a, B = b, C = c, Normal = normal };
            return true;
        }

        private static void CountEdge(Dictionary<long, Edge> edges, int a, int b)
        {
            long key = ((long)Math.Min(a,b) << 32) | (uint)Math.Max(a,b);
            Edge e;
            if (!edges.TryGetValue(key, out e)) e = new Edge { A = a, B = b };
            ++e.Count; edges[key] = e;
        }

        private static bool Seed(IList<Vector3> p, float eps, out int a, out int b,
            out int c, out int d)
        {
            a = 0; b = c = d = -1;
            for (int i = 1; i < p.Count; ++i) if (p[i].x < p[a].x) a = i;
            float best = eps * eps;
            for (int i = 0; i < p.Count; ++i)
            { float q = (p[i]-p[a]).sqrMagnitude; if (q > best) { best=q; b=i; } }
            if (b < 0) return false;
            Vector3 line = (p[b]-p[a]).normalized; best = eps * eps;
            for (int i = 0; i < p.Count; ++i)
            { float q = Vector3.Cross(line,p[i]-p[a]).sqrMagnitude; if (q > best) { best=q; c=i; } }
            if (c < 0) return false;
            Vector3 normal = Vector3.Cross(p[b]-p[a],p[c]-p[a]);
            normal /= normal.magnitude; best = eps;
            for (int i = 0; i < p.Count; ++i)
            { float q = Mathf.Abs(Vector3.Dot(normal,p[i]-p[a])); if (q > best) { best=q; d=i; } }
            return d >= 0;
        }

        private static bool Build(IList<Vector3> p, out ProceduralConvexHull hull)
        {
            hull = null;
            if (p.Count < 4) return false;
            Vector3 min = p[0], max = p[0];
            foreach (Vector3 v in p)
            {
                if (!ProceduralMeshBuilder.Finite(v)) return false;
                min = Vector3.Min(min,v); max = Vector3.Max(max,v);
            }
            float eps = Mathf.Max(1e-8f, (max-min).magnitude * 1e-6f);
            int a,b,c,d;
            if (!Seed(p,eps,out a,out b,out c,out d)) return false;
            Vector3 inside = (p[a]+p[b]+p[c]+p[d]) * 0.25f;
            var faces = new List<Face>();
            int[,] initial = { {a,b,c}, {a,d,b}, {a,c,d}, {b,d,c} };
            for (int i=0;i<4;++i)
            {
                Face f;
                if (!MakeFace(p,initial[i,0],initial[i,1],initial[i,2],inside,eps,out f)) return false;
                faces.Add(f);
            }
            // Add one point at a time; the boundary of the visible faces is
            // the horizon. Only boundary edges get new outward triangles.
            for (int i=0;i<p.Count;++i)
            {
                if (i==a || i==b || i==c || i==d) continue;
                Dictionary<long, Edge> horizon = null;
                for (int j=faces.Count-1;j>=0;--j)
                {
                    Face f=faces[j];
                    if (f.Distance(p[i],p)<=eps) continue;
                    if (horizon == null) horizon = new Dictionary<long, Edge>();
                    CountEdge(horizon,f.A,f.B); CountEdge(horizon,f.B,f.C); CountEdge(horizon,f.C,f.A);
                    faces.RemoveAt(j);
                }
                if (horizon == null) continue;
                foreach (Edge e in horizon.Values)
                {
                    if (e.Count>2) return false;
                    if (e.Count!=1) continue;
                    Face f;
                    if (!MakeFace(p,e.A,e.B,i,inside,eps,out f)) return false;
                    faces.Add(f);
                }
                if (faces.Count>FaceLimit) return false;
            }
            // Verify closure and convexity rather than passing a damaged hull
            // to PhysX or using its volume for resource scaling.
            var closure = new Dictionary<long, Edge>();
            foreach (Face f in faces)
            {
                CountEdge(closure,f.A,f.B); CountEdge(closure,f.B,f.C); CountEdge(closure,f.C,f.A);
                foreach (Vector3 v in p) if (f.Distance(v,p)>eps*4f) return false;
            }
            foreach (Edge e in closure.Values) if (e.Count!=2) return false;
            var result = new ProceduralConvexHull();
            var map = new Dictionary<int,int>();
            foreach (Face f in faces)
                foreach (int index in new[] {f.A,f.B,f.C})
                {
                    int mapped;
                    if (!map.TryGetValue(index,out mapped))
                    { mapped=result.Vertices.Count; map.Add(index,mapped); result.Vertices.Add(p[index]); }
                    result.Triangles.Add(mapped);
                }
            if (!(result.Volume()>0d)) return false;
            hull=result; return true;
        }

        private static void Extreme(IList<Vector3> p, Vector3 direction, HashSet<int> indices)
        {
            int best=0; float score=Vector3.Dot(p[0],direction);
            for (int i=1;i<p.Count;++i)
            { float value=Vector3.Dot(p[i],direction); if (value>score) { score=value; best=i; } }
            indices.Add(best);
        }

        // If a dense hull cannot fit the convex budget, sample the extreme
        // points of BOTH clouds in the same directions. Using common indices
        // avoids inventing a volume change just by changing the sample set.
        private static void Sample(IList<Vector3> p, HashSet<int> indices)
        {
            foreach (Vector3 axis in new[] {Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
                Extreme(p,axis,indices);
            const int count=54; const float golden=2.39996323f;
            for (int i=0;i<count;++i)
            {
                float y=1f-2f*(i+0.5f)/count, r=Mathf.Sqrt(Mathf.Max(0f,1f-y*y)), angle=i*golden;
                Extreme(p,new Vector3(r*Mathf.Cos(angle),y,r*Mathf.Sin(angle)),indices);
            }
        }

        internal static bool TryPair(IList<Vector3> before, IList<Vector3> after,
            out ProceduralConvexHull original, out ProceduralConvexHull edited)
        {
            original=edited=null;
            if (before.Count!=after.Count || before.Count<4) return false;
            if (Build(before,out original) && Build(after,out edited)) return true;
            var indices=new HashSet<int>();
            Sample(before,indices); Sample(after,indices); // at most 120 points
            var low=new List<Vector3>(); var high=new List<Vector3>();
            for (int i=0;i<before.Count;++i)
                if (indices.Contains(i)) { low.Add(before[i]); high.Add(after[i]); }
            return Build(low,out original) && Build(high,out edited);
        }

        internal double Volume()
        {
            Vector3 origin=Vertices[0]; double sum=0d;
            for (int i=0;i<Triangles.Count;i+=3)
            {
                Vector3 a=Vertices[Triangles[i]]-origin,b=Vertices[Triangles[i+1]]-origin,c=Vertices[Triangles[i+2]]-origin;
                sum+=(double)a.x*((double)b.y*c.z-(double)b.z*c.y)+
                    (double)a.y*((double)b.z*c.x-(double)b.x*c.z)+
                    (double)a.z*((double)b.x*c.y-(double)b.y*c.x);
            }
            return Math.Abs(sum/6d);
        }
    }
}
