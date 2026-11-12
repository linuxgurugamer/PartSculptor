// Snap two directly attached KSP1 parts using world-space face centers and
// OUTWARD face normals. No mesh data is changed: this is a rigid placement
// operation for the physical child and every descendant in the KSP part tree.
// Based on PartFaceSnap's SnapAttachedFaces / SnapFaces approach.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PartSculptor
{
    public static class PartFaceSnap
    {
        private struct PartPose
        {
            internal Part Part;
            internal Vector3 Position;
            internal Quaternion Rotation;
        }

        // Standalone node-based entry point, useful for ordinary attached
        // parts without morph geometry. For PartSculptor's tilted mesh faces,
        // prefer SnapFaces with actual deformed face centers/normals instead.
        public static bool SnapAttachedFaces(Part partA, Part partB)
        {
            if (!HighLogic.LoadedSceneIsEditor || partA == null || partB == null)
                return false;
            Part parent = partB.parent == partA ? partA :
                partA.parent == partB ? partB : null;
            if (parent == null) return false;
            Part child = parent == partA ? partB : partA;
            if (parent.attachNodes == null || child.attachNodes == null) return false;
            AttachNode parentNode = null, childNode = null;
            float best = float.MaxValue;
            foreach (AttachNode pn in parent.attachNodes)
            {
                if (pn == null || pn.attachedPart != child) continue;
                Vector3 p = parent.transform.TransformPoint(pn.position);
                foreach (AttachNode cn in child.attachNodes)
                {
                    if (cn == null || cn.attachedPart != parent) continue;
                    float distance = (p - child.transform.TransformPoint(cn.position)).sqrMagnitude;
                    if (distance >= best) continue;
                    best = distance;
                    parentNode = pn;
                    childNode = cn;
                }
            }
            if (parentNode == null || childNode == null) return false;
            return SnapFaces(parent, child,
                parent.transform.TransformPoint(parentNode.position),
                parent.transform.TransformDirection(parentNode.orientation),
                child.transform.TransformPoint(childNode.position),
                child.transform.TransformDirection(childNode.orientation));
        }

        // Match the four corners of a stack connection at the START of the
        // morph. Brute-force all 24 one-to-one assignments; choosing the
        // minimum total squared distance also handles 90/180/270-degree rolls
        // and preserves the same visible side during the rest of the drag.
        public static bool TryBuildCornerMap(Vector3[] parentCorners,
            Vector3[] childCorners, out int[] parentToChild)
        {
            parentToChild = null;
            if (!ValidCorners(parentCorners) || !ValidCorners(childCorners))
                return false;
            float best = float.MaxValue;
            for (int a = 0; a < 4; ++a)
                for (int b = 0; b < 4; ++b)
                    if (b != a)
                        for (int c = 0; c < 4; ++c)
                            if (c != a && c != b)
                                for (int d = 0; d < 4; ++d)
                                    if (d != a && d != b && d != c)
                                    {
                                        float error =
                                            (parentCorners[0] - childCorners[a]).sqrMagnitude +
                                            (parentCorners[1] - childCorners[b]).sqrMagnitude +
                                            (parentCorners[2] - childCorners[c]).sqrMagnitude +
                                            (parentCorners[3] - childCorners[d]).sqrMagnitude;
                                        if (error >= best) continue;
                                        best = error;
                                        parentToChild = new[] { a, b, c, d };
                                    }
            return parentToChild != null;
        }

        // Check ALL intrinsic edge/diagonal lengths before a drag. Only
        // previously congruent touching faces may receive the physical Copy
        // correction; intentionally unequal faces keep incremental behavior.
        public static bool HaveMatchingEdgeGeometry(Vector3[] parentCorners,
            Vector3[] childCorners, int[] parentToChild)
        {
            if (!ValidCorners(parentCorners) || !ValidCorners(childCorners) ||
                !ValidMap(parentToChild)) return false;
            for (int i = 0; i < 4; ++i)
                for (int j = i + 1; j < 4; ++j)
                {
                    float p = (parentCorners[j] - parentCorners[i]).magnitude;
                    float c = (childCorners[parentToChild[j]] -
                        childCorners[parentToChild[i]]).magnitude;
                    if (Mathf.Abs(p - c) > Mathf.Max(0.003f,
                            Mathf.Max(p, c) * 0.01f)) return false;
                }
            return true;
        }

        // Constrain the actual morphed X and Z edge directions as well as the
        // face normal. A normal-only snap can match planes but retain a small
        // roll about that normal, producing a visible lip/shortened edge.
        // This is strictly a RIGID pose adjustment: it cannot reduce the
        // child's local morph tilt or resize a non-congruent end face.
        public static bool SnapCorners(Part parent, Part child,
            Vector3[] parentCorners, Vector3[] childCorners, int[] parentToChild)
        {
            if (!HighLogic.LoadedSceneIsEditor || parent == null || child == null ||
                child.parent != parent || parent.transform == null ||
                child.transform == null ||
                !ValidCorners(parentCorners) || !ValidCorners(childCorners) ||
                !ValidMap(parentToChild)) return false;

            Vector3 sourceX, sourceZ, childX, childZ;
            if (!TryFrame(parentCorners, 0, 1, 2, 3, out sourceX, out sourceZ) ||
                !TryFrame(childCorners, parentToChild[0], parentToChild[1],
                    parentToChild[2], parentToChild[3], out childX, out childZ))
                return false;
            Vector3 sourceUp = Vector3.Cross(sourceZ, sourceX).normalized;
            Vector3 childUp = Vector3.Cross(childZ, childX).normalized;
            Quaternion sourceFrame = Quaternion.LookRotation(sourceZ, sourceUp);
            Quaternion childFrame = Quaternion.LookRotation(childZ, childUp);
            Quaternion turn = sourceFrame * Quaternion.Inverse(childFrame);
            if (Quaternion.Angle(Quaternion.identity, turn) > 150f)
                return false;

            Vector3 parentCenter = Center(parentCorners);
            Vector3 childCenter = Center(childCorners);
            // For identical FL-T400 ends the four points should align within
            // floating point tolerance. Reject a non-rigid fit rather than
            // quietly treating a changed edge LENGTH as a body rotation.
            float span = Mathf.Min(
                (parentCorners[1] - parentCorners[0]).magnitude,
                (parentCorners[2] - parentCorners[0]).magnitude);
            float tolerance = Mathf.Max(0.003f, span * 0.015f);
            for (int i = 0; i < 4; ++i)
                if ((parentCenter + turn *
                    (childCorners[parentToChild[i]] - childCenter) -
                    parentCorners[i]).magnitude > tolerance)
                    return false;
            return ApplyRigidChildPose(parent, child, parentCenter, childCenter, turn);
        }

        private static bool ValidCorners(Vector3[] corners)
        {
            if (corners == null || corners.Length < 4) return false;
            for (int i = 0; i < 4; ++i)
                if (!Finite(corners[i])) return false;
            return (corners[1] - corners[0]).sqrMagnitude > 1e-8f &&
                   (corners[2] - corners[0]).sqrMagnitude > 1e-8f;
        }

        private static bool ValidMap(int[] map)
        {
            if (map == null || map.Length != 4) return false;
            int used = 0;
            for (int i = 0; i < 4; ++i)
            {
                if (map[i] < 0 || map[i] >= 4 || (used & (1 << map[i])) != 0)
                    return false;
                used |= 1 << map[i];
            }
            return used == 15;
        }

        private static bool TryFrame(Vector3[] corners,
            int a, int b, int c, int d, out Vector3 x, out Vector3 z)
        {
            x = (corners[b] - corners[a]) + (corners[d] - corners[c]);
            z = (corners[c] - corners[a]) + (corners[d] - corners[b]);
            if (x.sqrMagnitude < 1e-10f || z.sqrMagnitude < 1e-10f)
                return false;
            x.Normalize();
            // Gram-Schmidt ensures a proper rigid rotation even for a doubly
            // tilted face whose X and Z edges aren't perfectly orthogonal.
            z -= Vector3.Dot(z, x) * x;
            if (z.sqrMagnitude < 1e-10f) return false;
            z.Normalize();
            return true;
        }

        private static Vector3 Center(Vector3[] corners)
        {
            return (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f;
        }

        // The caller must provide the CURRENT deformed face planes in world
        // coordinates, after both source and recipient morph calculations.
        // Child normal faces OUT of the child, therefore it must point in the
        // OPPOSITE direction to the parent's outward face normal.
        public static bool SnapFaces(Part parent, Part child,
            Vector3 parentFacePoint, Vector3 parentFaceNormal,
            Vector3 childFacePoint, Vector3 childFaceNormal)
        {
            if (!HighLogic.LoadedSceneIsEditor || parent == null || child == null ||
                child.parent != parent || parent.transform == null ||
                child.transform == null || !Finite(parentFacePoint) ||
                !Finite(childFacePoint) || !Finite(parentFaceNormal) ||
                !Finite(childFaceNormal) ||
                parentFaceNormal.sqrMagnitude < 1e-10f ||
                childFaceNormal.sqrMagnitude < 1e-10f)
                return false;

            // Use the minimum correction from the current pose. Unlike a
            // fixed 2x angle, this works with tilted or rolled parts and when
            // the child itself is the morph driver.
            Quaternion turn = Quaternion.FromToRotation(childFaceNormal.normalized,
                -parentFaceNormal.normalized);
            if (Quaternion.Angle(Quaternion.identity, turn) > 150f) return false;

            return ApplyRigidChildPose(parent, child,
                parentFacePoint, childFacePoint, turn);
        }

        // Move the *whole physical KSP child subtree* using absolute world
        // poses to avoid depending on Unity's sometimes different hierarchy.
        private static bool ApplyRigidChildPose(Part parent, Part child,
            Vector3 targetCenter, Vector3 childCenter, Quaternion turn)
        {
            if (child.parent != parent) return false;
            var poses = new List<PartPose>();
            var visited = new HashSet<Part>();
            var stack = new Stack<Part>();
            stack.Push(child);
            while (stack.Count > 0)
            {
                Part p = stack.Pop();
                if (p == null || !visited.Add(p)) continue;
                poses.Add(new PartPose { Part = p,
                    Position = p.transform.position, Rotation = p.transform.rotation });
                if (p.children == null) continue;
                foreach (Part descendant in p.children)
                    if (descendant != null && descendant.parent == p)
                        stack.Push(descendant);
            }
            foreach (PartPose pose in poses)
            {
                if (pose.Part == null) continue;
                pose.Part.transform.rotation = turn * pose.Rotation;
                pose.Part.transform.position = targetCenter +
                    turn * (pose.Position - childCenter);
            }
            return true;
        }

        private static bool Finite(Vector3 v)
        {
            return !float.IsNaN(v.x) && !float.IsInfinity(v.x) &&
                   !float.IsNaN(v.y) && !float.IsInfinity(v.y) &&
                   !float.IsNaN(v.z) && !float.IsInfinity(v.z);
        }
    }
}
