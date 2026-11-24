// Inserts real vertices where a horizontal (part-local Y) cross-section
// intersects existing triangles. Preserves submesh/material boundaries, UV
// channels and per-vertex attributes. Operates on private mesh instances only.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PartSculptor
{
    internal struct ProceduralRing
    {
        internal float Height; // 0..1, measured between native part-local Y bounds
        internal float Width;  // local X multiplier at this cross-section
        internal float Depth;  // local Z multiplier at this cross-section
        internal float TiltX;  // local Y displacement per part-local X unit
        internal float TiltZ;  // local Y displacement per part-local Z unit
        internal ProceduralRing(float height, float width, float depth)
            : this(height, width, depth, 0f, 0f) { }
        internal ProceduralRing(float height, float width, float depth, float tiltX, float tiltZ)
        { Height = height; Width = width; Depth = depth; TiltX = tiltX; TiltZ = tiltZ; }
    }

    internal struct ProceduralProfileSample
    {
        internal float Width, Depth, TiltX, TiltZ;
        internal ProceduralProfileSample(float width, float depth, float tiltX, float tiltZ)
        { Width = width; Depth = depth; TiltX = tiltX; TiltZ = tiltZ; }
    }

    // One logical edit, anchored in immutable mesh-local space. The anchor
    // survives ring height changes because it is located again after slicing.
    internal struct ProceduralVertex
    {
        internal string MeshKey;
        internal int Submesh;
        internal Vector3 Anchor, Offset;
        // Radius in part-local metres, evaluated on the undeformed-by-vertices surface.
        internal float InfluenceRadius;
        internal ProceduralVertex(string meshKey, int submesh, Vector3 anchor, Vector3 offset,
            float influenceRadius = 0f)
        { MeshKey = meshKey; Submesh = submesh; Anchor = anchor; Offset = offset;
            InfluenceRadius = influenceRadius; }
    }

    internal struct ProceduralVertexBinding
    {
        internal int EditIndex, Index, A, B, C;
        internal Vector3 Weights;
        internal ProceduralVertexBinding(int editIndex, int index, int a, int b, int c, Vector3 weights)
        { EditIndex = editIndex; Index = index; A = a; B = b; C = c; Weights = weights; }
    }

    internal static class ProceduralMeshBuilder
    {
        internal const int MaxRings = 6;
        internal const int MaxIndividualVertices = 64;
        internal const float InteriorWeight = 0.0001f;

        internal static bool ValidInfluenceRadius(float radius)
        { return radius >= 0f && !float.IsInfinity(radius) && !float.IsNaN(radius); }

        // Smoothstep falloff: full displacement at the anchor, zero at the
        // radius, with zero derivative at both ends.
        internal static float InfluenceWeight(float distance, float radius)
        {
            if (radius <= 0f || distance >= radius) return 0f;
            float t = 1f - Mathf.Clamp01(distance / radius);
            return t * t * (3f - 2f * t);
        }

        internal static bool Finite(Vector3 p)
        {
            return !float.IsNaN(p.x) && !float.IsInfinity(p.x) &&
                !float.IsNaN(p.y) && !float.IsInfinity(p.y) &&
                !float.IsNaN(p.z) && !float.IsInfinity(p.z);
        }

        // Interior classification is separate from edge and endpoint handling.
        internal static bool Interior(Vector3 weights)
        {
            return weights.x > InteriorWeight && weights.y > InteriorWeight &&
                weights.z > InteriorWeight;
        }

        internal static bool OnTriangle(Vector3 weights)
        {
            return weights.x >= -InteriorWeight && weights.y >= -InteriorWeight &&
                weights.z >= -InteriorWeight;
        }

        internal static int SmallestWeight(Vector3 w)
        { return w.x <= w.y && w.x <= w.z ? 0 : w.y <= w.z ? 1 : 2; }

        internal static bool AtEndpoint(Vector3 w)
        { return Mathf.Max(w.x, Mathf.Max(w.y, w.z)) >= 1f - InteriorWeight; }

        internal static bool SamePoint(Vector3 a, Vector3 b)
        { return (a - b).sqrMagnitude <= 1e-12f; }

        internal static bool TryBarycentric(Vector3 p, Vector3 a, Vector3 b, Vector3 c,
            out Vector3 weights)
        {
            weights = Vector3.zero;
            Vector3 ab = b - a, ac = c - a, ap = p - a;
            float aa = Vector3.Dot(ab, ab), bb = Vector3.Dot(ac, ac),
                d = Vector3.Dot(ab, ac), pa = Vector3.Dot(ap, ab), pc = Vector3.Dot(ap, ac);
            double determinant = (double)aa * bb - (double)d * d;
            if (determinant <= (double)aa * bb * 1e-10 || determinant <= 0d) return false;
            float v = (float)(((double)bb * pa - (double)d * pc) / determinant);
            float w = (float)(((double)aa * pc - (double)d * pa) / determinant);
            weights = new Vector3(1f - v - w, v, w);
            float tolerance = Mathf.Max(1e-7f, Mathf.Sqrt(Mathf.Max(aa, bb)) * 0.00005f);
            return Finite(weights) && (a + ab * v + ac * w - p).sqrMagnitude <= tolerance * tolerance;
        }

        // Ray intersection in the supplied coordinate system; the caller
        // converts local distance to world metres for cross-mesh comparisons.
        internal static bool RayTriangle(Ray ray, Vector3 a, Vector3 b, Vector3 c,
            out float distance, out Vector3 weights)
        {
            distance = 0f; weights = Vector3.zero;
            Vector3 e1 = b - a, e2 = c - a;
            Vector3 h = Vector3.Cross(ray.direction, e2);
            float determinant = Vector3.Dot(e1, h);
            if (Mathf.Abs(determinant) <= 1e-10f * e1.magnitude * h.magnitude ||
                determinant == 0f) return false;
            float inv = 1f / determinant;
            Vector3 s = ray.origin - a;
            float u = inv * Vector3.Dot(s, h);
            if (u < 0f || u > 1f) return false;
            Vector3 q = Vector3.Cross(s, e1);
            float v = inv * Vector3.Dot(ray.direction, q);
            if (v < 0f || u + v > 1f) return false;
            distance = inv * Vector3.Dot(e2, q);
            weights = new Vector3(1f - u - v, u, v);
            return distance > 0f && !float.IsInfinity(distance) && Finite(weights);
        }
        internal const int MaxVertices = 65000; // preserve 16-bit mesh compatibility
        internal const int MaxConvexTriangles = 255;

        internal static bool Valid(IList<ProceduralRing> rings)
        {
            if (rings == null) return true;
            if (rings.Count > MaxRings) return false;
            float last = 0f;
            for (int i = 0; i < rings.Count; ++i)
            {
                ProceduralRing r = rings[i];
                if (float.IsNaN(r.Height) || float.IsNaN(r.Width) || float.IsNaN(r.Depth) ||
                    float.IsNaN(r.TiltX) || float.IsNaN(r.TiltZ) ||
                    float.IsInfinity(r.Height) || float.IsInfinity(r.Width) || float.IsInfinity(r.Depth) ||
                    float.IsInfinity(r.TiltX) || float.IsInfinity(r.TiltZ) ||
                    r.Height < 0.01f || r.Height > 0.99f ||
                    r.Height - last < 0.015f ||
                    r.Width < 0.1f || r.Width > 4f || r.Depth < 0.1f || r.Depth > 4f ||
                    Mathf.Abs(r.TiltX) > Settings.MaxFaceTilt ||
                    Mathf.Abs(r.TiltZ) > Settings.MaxFaceTilt)
                    return false;
                last = r.Height;
            }
            return true;
        }

        // Exact integral of a piecewise-linear width*depth cross-section.
        // End faces retain unit multiplier so the attachment nodes do not move.
        internal static double VolumeFactor(IList<ProceduralRing> rings)
        {
            if (rings == null || rings.Count == 0) return 1d;
            float y0 = 0f, w0 = 1f, d0 = 1f;
            double volume = 0d;
            for (int i = 0; i <= rings.Count; ++i)
            {
                float y1 = i == rings.Count ? 1f : rings[i].Height;
                float w1 = i == rings.Count ? 1f : rings[i].Width;
                float d1 = i == rings.Count ? 1f : rings[i].Depth;
                volume += (y1 - y0) * (2d * w0 * d0 + w0 * d1 + w1 * d0 + 2d * w1 * d1) / 6d;
                y0 = y1; w0 = w1; d0 = d1;
            }
            return volume;
        }

        internal static ProceduralProfileSample Profile(float y, float minY, float maxY,
            IList<ProceduralRing> rings)
        {
            if (rings == null || rings.Count == 0 || maxY <= minY)
                return new ProceduralProfileSample(1f, 1f, 0f, 0f);
            float t = Mathf.Clamp01((y - minY) / (maxY - minY));
            float start = 0f, x = 1f, z = 1f, tiltX = 0f, tiltZ = 0f;
            for (int i = 0; i <= rings.Count; ++i)
            {
                float end = i == rings.Count ? 1f : rings[i].Height;
                float nextX = i == rings.Count ? 1f : rings[i].Width;
                float nextZ = i == rings.Count ? 1f : rings[i].Depth;
                float nextTiltX = i == rings.Count ? 0f : rings[i].TiltX;
                float nextTiltZ = i == rings.Count ? 0f : rings[i].TiltZ;
                if (t <= end || i == rings.Count)
                {
                    float u = Mathf.Clamp01((t - start) / Mathf.Max(0.00001f, end - start));
                    return new ProceduralProfileSample(
                        Mathf.Lerp(x, nextX, u), Mathf.Lerp(z, nextZ, u),
                        Mathf.Lerp(tiltX, nextTiltX, u), Mathf.Lerp(tiltZ, nextTiltZ, u));
                }
                start = end; x = nextX; z = nextZ; tiltX = nextTiltX; tiltZ = nextTiltZ;
            }
            return new ProceduralProfileSample(1f, 1f, 0f, 0f);
        }

        // Mutable vertex attributes during a succession of polygon cuts.
        private sealed class VertexData
        {
            internal readonly List<Vector3> positions = new List<Vector3>();
            internal readonly List<float> y = new List<float>();
            internal readonly List<Vector3> normals = new List<Vector3>();
            internal readonly List<Vector4> tangents = new List<Vector4>();
            internal readonly List<Color> colors = new List<Color>();
            internal readonly List<Vector2>[] uv = {
                new List<Vector2>(), new List<Vector2>(), new List<Vector2>(), new List<Vector2>()
            };
            internal bool hasNormals, hasTangents, hasColors;
            internal readonly bool[] hasUv = new bool[4];

            internal int Insert(int a, int b, int c, Vector3 w)
            {
                positions.Add(positions[a] * w.x + positions[b] * w.y + positions[c] * w.z);
                y.Add(y[a] * w.x + y[b] * w.y + y[c] * w.z);
                if (hasNormals) normals.Add((normals[a] * w.x + normals[b] * w.y + normals[c] * w.z).normalized);
                if (hasTangents) tangents.Add(tangents[a] * w.x + tangents[b] * w.y + tangents[c] * w.z);
                if (hasColors) colors.Add(colors[a] * w.x + colors[b] * w.y + colors[c] * w.z);
                for (int i = 0; i < 4; ++i)
                    if (hasUv[i]) uv[i].Add(uv[i][a] * w.x + uv[i][b] * w.y + uv[i][c] * w.z);
                return positions.Count - 1;
            }

            internal int Interpolate(int a, int b, float t)
            {
                positions.Add(Vector3.LerpUnclamped(positions[a], positions[b], t));
                y.Add(Mathf.LerpUnclamped(y[a], y[b], t));
                if (hasNormals) normals.Add(Vector3.LerpUnclamped(normals[a], normals[b], t).normalized);
                if (hasTangents) tangents.Add(Vector4.LerpUnclamped(tangents[a], tangents[b], t));
                if (hasColors) colors.Add(Color.LerpUnclamped(colors[a], colors[b], t));
                for (int i = 0; i < 4; ++i)
                    if (hasUv[i]) uv[i].Add(Vector2.LerpUnclamped(uv[i][a], uv[i][b], t));
                return positions.Count - 1;
            }
        }

        // Unordered source-edge key: adjacent triangles share new vertices,
        // so they cannot open visible gaps along the same cut.
        private static long EdgeKey(int a, int b)
        {
            uint low = (uint)Math.Min(a, b);
            uint high = (uint)Math.Max(a, b);
            return ((long)low << 32) | high;
        }

        private static int Crossing(int a, int b, float plane, VertexData data,
            Dictionary<long, int> edgeCache)
        {
            if (Mathf.Abs(data.y[a] - plane) < 1e-6f) return a;
            if (Mathf.Abs(data.y[b] - plane) < 1e-6f) return b;
            long key = EdgeKey(a, b);
            int found;
            if (edgeCache.TryGetValue(key, out found)) return found;
            float t = (plane - data.y[a]) / (data.y[b] - data.y[a]);
            int index = data.Interpolate(a, b, Mathf.Clamp01(t));
            data.y[index] = plane;
            edgeCache.Add(key, index);
            return index;
        }

        private static void AppendUnique(List<int> polygon, int index)
        {
            if (polygon.Count == 0 || polygon[polygon.Count - 1] != index)
                polygon.Add(index);
        }

        private static List<int> Clip(int a, int b, int c, bool lower,
            float plane, VertexData data, Dictionary<long, int> edgeCache)
        {
            int[] original = { a, b, c };
            var polygon = new List<int>(4);
            for (int i = 0; i < 3; ++i)
            {
                int from = original[i];
                int to = original[(i + 1) % 3];
                bool insideFrom = lower ? data.y[from] <= plane + 1e-6f :
                    data.y[from] >= plane - 1e-6f;
                bool insideTo = lower ? data.y[to] <= plane + 1e-6f :
                    data.y[to] >= plane - 1e-6f;
                if (insideFrom && insideTo) AppendUnique(polygon, to);
                else if (insideFrom && !insideTo)
                    AppendUnique(polygon, Crossing(from, to, plane, data, edgeCache));
                else if (!insideFrom && insideTo)
                {
                    AppendUnique(polygon, Crossing(from, to, plane, data, edgeCache));
                    AppendUnique(polygon, to);
                }
            }
            if (polygon.Count > 1 && polygon[0] == polygon[polygon.Count - 1])
                polygon.RemoveAt(polygon.Count - 1);
            return polygon;
        }

        private static void Triangulate(List<int> polygon, VertexData data, List<int> triangles)
        {
            for (int i = 1; i + 1 < polygon.Count; ++i)
            {
                int a = polygon[0], b = polygon[i], c = polygon[i + 1];
                if (a == b || b == c || a == c) continue;
                if (Vector3.Cross(data.positions[b] - data.positions[a],
                    data.positions[c] - data.positions[a]).sqrMagnitude <= 1e-18f)
                    continue;
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
            }
        }

        // Split every incident triangle, even across submeshes. Reuse an index
        // for a shared indexed edge; retain separate UV/normal copies at seams.
        private static bool SplitEdge(VertexData data, List<List<int>> submeshes,
            Vector3 from, Vector3 to, Vector3 anchor, int editIndex,
            List<ProceduralVertexBinding> bindings, out string error)
        {
            error = "";
            var cache = new Dictionary<long, int>();
            Vector3 axis = to - from;
            if (axis.sqrMagnitude <= 1e-20f)
            { error = "Degenerate edge"; return false; }
            foreach (List<int> triangles in submeshes)
                for (int j = 0; j + 2 < triangles.Count; j += 3)
                {
                    int[] tri = { triangles[j], triangles[j+1], triangles[j+2] };
                    for (int k = 0; k < 3; ++k)
                    {
                        int a = tri[k], b = tri[(k+1)%3], c = tri[(k+2)%3];
                        Vector3 pa = data.positions[a], pb = data.positions[b];
                        if (!((SamePoint(pa, from) && SamePoint(pb, to)) ||
                              (SamePoint(pa, to) && SamePoint(pb, from)))) continue;
                        long key = EdgeKey(a, b);
                        int index;
                        if (!cache.TryGetValue(key, out index))
                        {
                            if (data.positions.Count >= MaxVertices)
                            { error = "Vertex insertion exceeds the mesh vertex limit"; return false; }
                            float u = Mathf.Clamp01(Vector3.Dot(anchor - pa, pb - pa) / (pb - pa).sqrMagnitude);
                            index = data.Interpolate(a, b, u);
                            // Keep geometric seam copies exactly coincident.
                            data.positions[index] = anchor;
                            cache.Add(key, index);
                            bindings.Add(new ProceduralVertexBinding(editIndex, index, a, b, c,
                                new Vector3(1f-u, u, 0f)));
                        }
                        triangles[j] = a; triangles[j+1] = index; triangles[j+2] = c;
                        triangles.InsertRange(j+3, new[] { index, b, c });
                        j += 3; // both replacement triangles are already processed
                        break;
                    }
                }
            if (cache.Count == 0) { error = "Edge no longer exists"; return false; }
            return true;
        }

        internal static bool TryBuild(Mesh source, Matrix4x4 localToPart,
            IList<ProceduralRing> rings, float minY, float maxY,
            bool convexCollider, out Mesh refined, out string error,
            string vertexMeshKey = null, IList<ProceduralVertex> edits = null,
            List<ProceduralVertexBinding> bindings = null)
        {
            refined = null;
            error = "";
            if (source == null || !source.isReadable || !Valid(rings))
            { error = "Invalid source mesh or cross-section parameters"; return false; }
            if (source.subMeshCount == 0)
            { error = "Source mesh has no submeshes"; return false; }
            if (source.blendShapeCount > 0)
            { error = "Blend-shape meshes cannot be subdivided"; return false; }
            Vector3[] native = source.vertices;
            if (native.Length == 0 || native.Length > MaxVertices)
            { error = "Mesh vertex count is outside the supported range"; return false; }
            var data = new VertexData();
            data.positions.AddRange(native);
            foreach (Vector3 v in native) data.y.Add(localToPart.MultiplyPoint3x4(v).y);
            Vector3[] normals = source.normals;
            Vector4[] tangents = source.tangents;
            Color[] colors = source.colors;
            data.hasNormals = normals.Length == native.Length;
            data.hasTangents = tangents.Length == native.Length;
            data.hasColors = colors.Length == native.Length;
            if (data.hasNormals) data.normals.AddRange(normals);
            if (data.hasTangents) data.tangents.AddRange(tangents);
            if (data.hasColors) data.colors.AddRange(colors);
            Vector2[][] uv = { source.uv, source.uv2, source.uv3, source.uv4 };
            for (int c = 0; c < 4; ++c)
            {
                data.hasUv[c] = uv[c].Length == native.Length;
                if (data.hasUv[c]) data.uv[c].AddRange(uv[c]);
            }
            var submeshes = new List<List<int>>();
            for (int s = 0; s < source.subMeshCount; ++s)
            {
                if (source.GetTopology(s) != MeshTopology.Triangles)
                { error = "Non-triangle submeshes cannot be subdivided"; return false; }
                submeshes.Add(new List<int>(source.GetTriangles(s)));
            }
            if (rings != null)
                foreach (ProceduralRing ring in rings)
                {
                    float plane = Mathf.Lerp(minY, maxY, ring.Height);
                    var cache = new Dictionary<long, int>();
                    for (int s = 0; s < submeshes.Count; ++s)
                    {
                        List<int> before = submeshes[s];
                        var after = new List<int>(before.Count * 2);
                        for (int j = 0; j + 2 < before.Count; j += 3)
                        {
                            int a = before[j], b = before[j + 1], c = before[j + 2];
                            float da = data.y[a] - plane, db = data.y[b] - plane,
                                dc = data.y[c] - plane;
                            bool above = da > 1e-6f || db > 1e-6f || dc > 1e-6f;
                            bool below = da < -1e-6f || db < -1e-6f || dc < -1e-6f;
                            if (!above || !below)
                            {
                                after.Add(a); after.Add(b); after.Add(c);
                                continue;
                            }
                            Triangulate(Clip(a, b, c, true, plane, data, cache), data, after);
                            Triangulate(Clip(a, b, c, false, plane, data, cache), data, after);
                        }
                        submeshes[s] = after;
                    }
                    if (data.positions.Count > MaxVertices)
                    { error = "Subdivision would exceed the 65,000-vertex safety limit"; return false; }
                }
            // Rebuild from native topology on every anchor-list change. Retained
            // anchors are classified again after ring cuts and earlier insertions.
            var replayBindings = bindings ?? new List<ProceduralVertexBinding>();
            if (edits != null && vertexMeshKey != null)
                for (int e = 0; e < edits.Count; ++e)
                {
                    ProceduralVertex edit = edits[e];
                    if (edit.MeshKey != vertexMeshKey) continue;
                    if (edit.Submesh < 0 || edit.Submesh >= submeshes.Count ||
                        !Finite(edit.Anchor) || !Finite(edit.Offset))
                    { error = "Invalid saved vertex anchor"; return false; }
                    for (int prev = 0; prev < e; ++prev)
                        if (edits[prev].MeshKey == edit.MeshKey && SamePoint(edits[prev].Anchor, edit.Anchor))
                        { error = "An editable vertex already exists at this point"; return false; }
                    List<int> triangles = submeshes[edit.Submesh];
                    int found = -1, edge = -1, endpoint = -1;
                    Vector3 weights = Vector3.zero, edgeWeights = Vector3.zero;
                    for (int j = 0; j + 2 < triangles.Count; j += 3)
                    {
                        Vector3 w;
                        if (!TryBarycentric(edit.Anchor, data.positions[triangles[j]],
                            data.positions[triangles[j + 1]], data.positions[triangles[j + 2]], out w) ||
                            !OnTriangle(w)) continue;
                        if (Interior(w))
                        {
                            if (found >= 0)
                            { error = "Vertex anchor lies on overlapping triangles"; return false; }
                            found = j; weights = w;
                        }
                        else if (AtEndpoint(w))
                        {
                            int k = w.x >= w.y && w.x >= w.z ? 0 : w.y >= w.z ? 1 : 2;
                            if (SamePoint(data.positions[triangles[j+k]], edit.Anchor))
                                endpoint = triangles[j+k];
                        }
                        else if (edge < 0) { edge = j; edgeWeights = w; }
                    }
                    if (found >= 0 && (edge >= 0 || endpoint >= 0))
                    { error = "Vertex anchor lies on overlapping triangles"; return false; }
                    if (endpoint >= 0)
                    {
                        // A changed ring or removed earlier edit can expose an
                        // existing vertex at a retained anchor. Bind it in place.
                        for (int i = 0; i < data.positions.Count; ++i)
                            if (SamePoint(data.positions[i], data.positions[endpoint]))
                                replayBindings.Add(new ProceduralVertexBinding(e, i, i, i, i, Vector3.right));
                        continue;
                    }
                    if (edge >= 0)
                    {
                        int k = SmallestWeight(edgeWeights);
                        Vector3 a0 = data.positions[triangles[edge+(k+1)%3]],
                            b0 = data.positions[triangles[edge+(k+2)%3]];
                        Vector3 d = b0-a0;
                        if (d.sqrMagnitude <= 1e-20f)
                        { error = "Degenerate edge"; return false; }
                        Vector3 anchor = a0 + d * Mathf.Clamp01(Vector3.Dot(edit.Anchor-a0,d)/d.sqrMagnitude);
                        if (!SplitEdge(data, submeshes, a0, b0, anchor, e, replayBindings, out error)) return false;
                        continue;
                    }
                    if (found < 0)
                    { error = "Vertex anchor is outside its surface or too close to an existing endpoint"; return false; }
                    if (data.positions.Count >= MaxVertices)
                    { error = "Vertex insertion exceeds the mesh vertex limit"; return false; }
                    int a = triangles[found], b = triangles[found + 1], c = triangles[found + 2];
                    int index = data.Insert(a, b, c, weights);
                    triangles[found + 2] = index;
                    triangles.InsertRange(found + 3, new[] { b, c, index, c, a, index });
                    replayBindings.Add(new ProceduralVertexBinding(e, index, a, b, c, weights));
                }
            int triangleCount = 0;
            foreach (List<int> triangles in submeshes) triangleCount += triangles.Count / 3;
            if (convexCollider && triangleCount > MaxConvexTriangles)
            { error = "Subdivision exceeds the convex collider triangle budget (255)"; return false; }
            var mesh = new Mesh();
            mesh.name = "PartSculptor procedural topology " + source.name;
            mesh.SetVertices(data.positions);
            if (data.hasNormals) mesh.SetNormals(data.normals);
            if (data.hasTangents) mesh.SetTangents(data.tangents);
            if (data.hasColors) mesh.SetColors(data.colors);
            for (int i = 0; i < 4; ++i)
                if (data.hasUv[i]) mesh.SetUVs(i, data.uv[i]);
            mesh.subMeshCount = submeshes.Count;
            for (int i = 0; i < submeshes.Count; ++i)
                mesh.SetTriangles(submeshes[i], i, false);
            if (!data.hasNormals) mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            refined = mesh;
            return true;
        }
    }
}
