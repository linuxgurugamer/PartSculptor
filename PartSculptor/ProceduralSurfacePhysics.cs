// A triangle surface in part-local coordinates, before and after individual
// vertex edits. The same mapping is used for collision and node transfer.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PartSculptor
{
    internal sealed class ProceduralSurfacePhysics
    {
        internal readonly List<Vector3> Base = new List<Vector3>();
        internal readonly List<Vector3> Edited = new List<Vector3>();
        internal readonly List<int> Triangles = new List<int>();

        internal struct Hit
        {
            internal int Triangle;
            internal Vector3 Weights, Point, Delta, BeforeNormal, AfterNormal;
            internal float DistanceSquared;
        }

        private static Vector3 Weighted(Vector3 a, Vector3 b, Vector3 c, Vector3 w)
        { return a * w.x + b * w.y + c * w.z; }

        // Orthogonal projection onto a triangle, falling back to its nearest
        // edge when outside. Degenerate triangles are skipped by the caller.
        internal static Vector3 ClosestWeights(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a, ac = c - a;
            Vector3 normal = Vector3.Cross(ab, ac);
            float ns = normal.sqrMagnitude;
            if (ns > 1e-20f)
            {
                Vector3 q = p - normal * (Vector3.Dot(p - a, normal) / ns);
                Vector3 w;
                if (ProceduralMeshBuilder.TryBarycentric(q, a, b, c, out w) &&
                    w.x >= 0f && w.y >= 0f && w.z >= 0f) return w;
            }
            float best = float.PositiveInfinity;
            Vector3 answer = Vector3.right;
            ClosestEdge(p, a, b, 0, 1, ref best, ref answer);
            ClosestEdge(p, b, c, 1, 2, ref best, ref answer);
            ClosestEdge(p, c, a, 2, 0, ref best, ref answer);
            return answer;
        }

        private static void ClosestEdge(Vector3 p, Vector3 a, Vector3 b, int i, int j,
            ref float best, ref Vector3 answer)
        {
            Vector3 d = b - a;
            float t = d.sqrMagnitude > 1e-20f ?
                Mathf.Clamp01(Vector3.Dot(p - a, d) / d.sqrMagnitude) : 0f;
            float ds = (p - a - d * t).sqrMagnitude;
            if (ds >= best) return;
            best = ds;
            answer = Vector3.zero; answer[i] = 1f - t; answer[j] = t;
        }

        internal bool TryClosest(Vector3 p, out Hit hit)
        {
            hit = new Hit { Triangle = -1, DistanceSquared = float.PositiveInfinity };
            for (int i = 0; i + 2 < Triangles.Count; i += 3)
            {
                int a = Triangles[i], b = Triangles[i + 1], c = Triangles[i + 2];
                Vector3 n = Vector3.Cross(Base[b] - Base[a], Base[c] - Base[a]);
                if (n.sqrMagnitude <= 1e-20f) continue;
                Vector3 w = ClosestWeights(p, Base[a], Base[b], Base[c]);
                Vector3 q = Weighted(Base[a], Base[b], Base[c], w);
                float ds = (p - q).sqrMagnitude;
                if (ds >= hit.DistanceSquared) continue;
                hit = new Hit { Triangle = i, Weights = w, Point = q, DistanceSquared = ds,
                    Delta = Weighted(Edited[a], Edited[b], Edited[c], w) - q,
                    BeforeNormal = n.normalized,
                    AfterNormal = Vector3.Cross(Edited[b] - Edited[a], Edited[c] - Edited[a]).normalized };
            }
            return hit.Triangle >= 0;
        }

        // Preserve original collider topology except for one support point
        // per projected edit. A point on an edge is nudged into one triangle
        // so no T-junction is introduced into the shared edge. If it is on
        // an existing vertex, update all coincident vertices instead.
        internal bool AddSupport(Hit hit, Vector3 displacement, bool convex, out string error)
        {
            error = "";
            int i = hit.Triangle;
            int a = Triangles[i], b = Triangles[i + 1], c = Triangles[i + 2];
            float span = Mathf.Max((Base[b] - Base[a]).magnitude,
                Mathf.Max((Base[c] - Base[b]).magnitude, (Base[a] - Base[c]).magnitude));
            float tolerance = Mathf.Max(1e-7f, span * 1e-5f);
            bool coincident = false;
            for (int j = 0; j < Base.Count; ++j)
                if ((Base[j] - hit.Point).sqrMagnitude <= tolerance * tolerance)
                { Edited[j] = Base[j] + displacement; coincident = true; }
            if (coincident) return true;
            // The raw support triangulation is NOT the final convex hull.
            // A dense but flat source can cook to only a handful of faces.
            if (Base.Count >= ProceduralMeshBuilder.MaxVertices)
            { error = "Individual vertex edit exceeds collider vertex budget"; return false; }
            Vector3 w = hit.Weights;
            // Convex cooking only needs the exact point cloud. Nonconvex
            // colliders still require a strictly interior triangle split.
            if (!convex)
            {
                const float inset = 0.001f;
                w = new Vector3(Mathf.Max(inset, w.x), Mathf.Max(inset, w.y), Mathf.Max(inset, w.z));
                w /= w.x + w.y + w.z;
            }
            Vector3 point = Weighted(Base[a], Base[b], Base[c], w);
            int index = Base.Count;
            Base.Add(point); Edited.Add(point + displacement);
            Triangles[i + 2] = index;
            Triangles.InsertRange(i + 3, new[] { b, c, index, c, a, index });
            return true;
        }

        // Signed tetrahedra: subtract a common origin for numerical stability.
        // Absolute volume is taken per collision shell, not per triangle.
        internal double Volume(bool edited)
        {
            List<Vector3> v = edited ? Edited : Base;
            if (v.Count == 0) return 0d;
            Vector3 origin = v[0];
            double sum = 0d;
            for (int i = 0; i + 2 < Triangles.Count; i += 3)
            {
                Vector3 a = v[Triangles[i]] - origin, b = v[Triangles[i + 1]] - origin,
                    c = v[Triangles[i + 2]] - origin;
                sum += (double)a.x * ((double)b.y * c.z - (double)b.z * c.y) +
                    (double)a.y * ((double)b.z * c.x - (double)b.x * c.z) +
                    (double)a.z * ((double)b.x * c.y - (double)b.y * c.x);
            }
            return Math.Abs(sum / 6d);
        }
    }
}
