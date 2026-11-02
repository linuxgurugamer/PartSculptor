// Mesh/collider deformation for static KSP1 parts. Never edit prefab/shared
// meshes. Rebuild each instance from unmodified vertex snapshots on EVERY edit.
using System;
using System.Collections.Generic;
using UnityEngine;

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
            internal Vector3[] vertices;
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

        private readonly Part part;
        private readonly Transform model;
        private readonly List<MeshRecord> meshes = new List<MeshRecord>();
        private readonly Dictionary<AttachNode, NodeRecord> nodes =
            new Dictionary<AttachNode, NodeRecord>();
        private Vector3 min, max;
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

        private static bool IsMorphInstance(Mesh mesh)
        {
            return mesh != null && mesh.name != null &&
                (mesh.name.StartsWith(MorphMeshPrefix, StringComparison.Ordinal) );
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
            if (part == null || model == null) return;
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
            foreach (MeshFilter f in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f == null || f.sharedMesh == null || f.GetComponent<MeshRenderer>() == null)
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
            foreach (MeshCollider c in part.GetComponentsInChildren<MeshCollider>(true))
            {
                if (c == null || c.GetComponentInParent<Part>() != part ||
                    !c.enabled || c.isTrigger || c.sharedMesh == null ||
                    IsPrimitiveMorphProxy(c)) continue;
                // A duplicated MeshCollider can also point to a private
                // already-morphed mesh. Recover the immutable prefab shape.
                Mesh native = IsMorphInstance(c.sharedMesh) ? PrefabMesh(c) : c.sharedMesh;
                if (native == null || !native.isReadable || IsMorphInstance(native))
                { Error = "Readable native collider mesh is unavailable"; Dispose(); return; }
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
                !meshes.Exists(m => m.collider != null))
            { Error = "Readable visual mesh and supported collider are required";
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

        private void Add(MeshFilter f, MeshCollider c, Mesh native)
        {
            MeshRecord r = new MeshRecord();
            r.filter = f;
            r.collider = c;
            r.transform = f != null ? f.transform : c.transform;
            r.native = native;
            r.vertices = native.vertices;
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
            bool first = true;
            foreach (MeshRecord r in meshes)
            {
                if (r.filter == null) continue; // use visible geometry as bounds
                foreach (Vector3 vertex in r.vertices)
                {
                    Vector3 p = part.transform.InverseTransformPoint(r.transform.TransformPoint(vertex));
                    if (first) { min = max = p; first = false; }
                    else { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
                }
            }
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

        internal Vector3 Corner(int face, int corner, MorphFace bottom, MorphFace top)
        {
            Vector3 point = new Vector3((corner & 1) == 0 ? min.x : max.x,
                face == 0 ? min.y : max.y,
                (corner & 2) == 0 ? min.z : max.z);
            return FaceMorphMath.Transform(point, min, max, bottom, top);
        }

        internal void Apply(MorphFace bottom, MorphFace top, bool moveAttachments)
        {
            if (!valid) return;
            // Base geometry reflects current regular PartSculptor dimensions.
            RecalculateBounds();
            bool identity = FaceMorphMath.IsIdentity(bottom) &&
                FaceMorphMath.IsIdentity(top);
            foreach (MeshRecord r in meshes)
            {
                if (r.transform == null || r.instance == null) continue;
                Vector3[] transformed = new Vector3[r.vertices.Length];
                for (int i = 0; i < transformed.Length; i++)
                {
                    Vector3 p = part.transform.InverseTransformPoint(
                        r.transform.TransformPoint(r.vertices[i]));
                    Vector3 newP = FaceMorphMath.Transform(p, min, max, bottom, top);
                    transformed[i] = r.transform.InverseTransformPoint(
                        part.transform.TransformPoint(newP));
                }
                r.instance.vertices = transformed;
                r.instance.RecalculateNormals();
                r.instance.RecalculateBounds();
                if (r.collider != null)
                {
                    // Unity requires reassignment to recook the deformed collider.
                    // For generated proxies switch back to the exact native
                    // collider whenever the morph returns to identity.
                    if (r.primitive != null)
                    {
                        r.collider.enabled = false;
                        r.primitive.enabled = identity && r.originalEnabled;
                    }
                    r.collider.sharedMesh = null;
                    r.collider.sharedMesh = r.instance;
                    if (r.primitive != null)
                        r.collider.enabled = !identity && r.originalEnabled;
                }
            }
            foreach (KeyValuePair<AttachNode, NodeRecord> pair in nodes)
            {
                AttachNode node = pair.Key;
                if (node == null) continue;
                NodeRecord original = pair.Value;
                Vector3 was = node.position;
                node.position = FaceMorphMath.Transform(original.position, min, max, bottom, top);
                node.originalPosition = FaceMorphMath.Transform(original.originalPosition,
                    min, max, bottom, top);
                // Tilt alters the face's surface normal, even when its Y-axis
                // tangent is unchanged. Derive the new normal from the exact
                // deformation instead of transforming a short direction line.
                // Absolute original baselines prevent repeated-drag drift.
                node.orientation = FaceMorphMath.TransformNormal(original.position,
                    original.orientation, min, max, bottom, top);
                float t = Mathf.Clamp01((original.position.y - min.y) /
                    Mathf.Max(0.001f, max.y - min.y));
                float width = Mathf.Lerp(bottom.Width, top.Width, t);
                float depth = Mathf.Lerp(bottom.Depth, top.Depth, t);
                node.size = ScaleMath.NodeSizeTier(original.size,
                    original.orientation.x, original.orientation.y, original.orientation.z,
                    width, 1f, depth);
                if (!moveAttachments || node.attachedPart == null) continue;
                Vector3 change = node.position - was;
                if (node.attachedPart == part.parent)
                {
                    part.transform.position -= part.transform.TransformVector(change);
                }
                else if (node.attachedPart.parent == part)
                {
                    Part child = node.attachedPart;
                    child.transform.position += part.transform.TransformVector(change);
                }
            }
        }

        internal void Dispose()
        {
            valid = false;
            foreach (MeshRecord r in meshes)
            {
                if (r.filter != null && r.filter.sharedMesh == r.instance)
                    r.filter.sharedMesh = r.native;
                if (r.primitive != null)
                {
                    r.collider.enabled = false;
                    r.primitive.enabled = r.originalEnabled;
                    if (r.collider != null) UnityEngine.Object.Destroy(r.collider);
                }
                else if (r.collider != null && r.collider.sharedMesh == r.instance)
                    r.collider.sharedMesh = r.native;
                if (r.instance != null) UnityEngine.Object.Destroy(r.instance);
                if (r.ownedNative && r.native != null)
                    UnityEngine.Object.Destroy(r.native);
            }
            meshes.Clear();
            nodes.Clear();
        }
    }
}
