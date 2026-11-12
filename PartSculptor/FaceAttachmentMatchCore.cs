// Pure C# geometry fitting for the newly attached part's Y end face.
// Input target corners are relative to the child's existing attachment node
// in the CHILD'S local frame. This makes the result independent of the part's
// pivot, and matches the anchor-preserving translation in FaceMorphSupport.Apply.
using System;

namespace PartSculptor
{
    internal struct AttachmentFaceShape
    {
        internal double Width, Depth, TiltX, TiltZ, OffsetX, OffsetZ;
        internal AttachmentFaceShape(double width, double depth, double tiltX,
            double tiltZ, double offsetX, double offsetZ)
        {
            Width = width; Depth = depth; TiltX = tiltX; TiltZ = tiltZ;
            OffsetX = offsetX; OffsetZ = offsetZ;
        }
    }

    internal static class FaceAttachmentMatchCore
    {
        internal static MorphPoint Transform(MorphPoint p, MorphPoint min,
            MorphPoint max, AttachmentFaceShape b, AttachmentFaceShape t)
        {
            return FaceMorphCore.Transform(p, min, max,
                b.Width, b.Depth, b.TiltX, b.TiltZ,
                t.Width, t.Depth, t.TiltX, t.TiltZ,
                b.OffsetX, b.OffsetZ, t.OffsetX, t.OffsetZ);
        }

        private static bool Finite(double x)
        { return !double.IsNaN(x) && !double.IsInfinity(x); }

        private static double Distance(MorphPoint a, MorphPoint b)
        {
            double x = a.X - b.X, y = a.Y - b.Y, z = a.Z - b.Z;
            return Math.Sqrt(x * x + y * y + z * z);
        }

        // Permutations naturally handle 0/90/180/270-degree part rotations
        // (and the sign flip caused by opposing stack-node directions).
        // Arbitrary yaw/skew cannot be represented exactly by the existing
        // X/Z-only planar faces. An optional CLOSE fit permits small residuals
        // caused by KSP rotating a newly placed child to the tilted parent
        // node's surface normal. Large skew still fails the error check.
        // Existing offsets are intentionally preserved. When the stack node
        // is on the face, a face offset moves BOTH the face and its attachment
        // node; it cannot change the shape relative to the fixed anchor.
        internal static bool TryFit(MorphPoint[] targetRelative,
            MorphPoint min, MorphPoint max, MorphPoint nativeNode,
            int connectingFace, AttachmentFaceShape bottom, AttachmentFaceShape top,
            out AttachmentFaceShape fitted, out double bestError,
            bool allowCloseFit = false, double maxFaceTilt = 2.5d)
        {
            fitted = connectingFace == 0 ? bottom : top;
            bestError = double.MaxValue;
            if (targetRelative == null || targetRelative.Length != 4 ||
                (connectingFace != 0 && connectingFace != 1) ||
                !Finite(maxFaceTilt) || maxFaceTilt <= 0d || maxFaceTilt > 4d) return false;
            double dx = max.X - min.X, dz = max.Z - min.Z;
            double dy = max.Y - min.Y;
            if (!Finite(dx) || !Finite(dy) || !Finite(dz) ||
                dx < 0.02d || dz < 0.02d || dy < 0.02d) return false;
            for (int i = 0; i < 4; ++i)
                if (!Finite(targetRelative[i].X) ||
                    !Finite(targetRelative[i].Y) || !Finite(targetRelative[i].Z))
                    return false;

            double faceY = connectingFace == 0 ? min.Y : max.Y;
            // Only exterior stack faces, not interior/side attachment nodes.
            if (Math.Abs(nativeNode.Y - faceY) > dy * 0.20d) return false;
            double cornerTolerance = Math.Max(0.005d,
                0.015d * Math.Min(dx, dz));
            // A tilted parent node may rotate the child's local frame by a
            // few degrees. Its corners then form a slightly skewed rectangle
            // in child X/Z and cannot be represented *exactly* by face controls
            // that support width/depth/tilt but no XZ shear. Fit the nearest
            // representable rectangle when all corners are within 7.5% of the
            // smaller target face span. Never accept major skew (e.g. 45 deg).
            if (allowCloseFit)
            {
                double minX = double.MaxValue, maxX = double.MinValue;
                double minZ = double.MaxValue, maxZ = double.MinValue;
                for (int i = 0; i < 4; ++i)
                {
                    minX = Math.Min(minX, targetRelative[i].X);
                    maxX = Math.Max(maxX, targetRelative[i].X);
                    minZ = Math.Min(minZ, targetRelative[i].Z);
                    maxZ = Math.Max(maxZ, targetRelative[i].Z);
                }
                double targetSpan = Math.Min(maxX - minX, maxZ - minZ);
                if (Finite(targetSpan) && targetSpan > 0d)
                    cornerTolerance = Math.Max(cornerTolerance, 0.075d * targetSpan);
            }
            bool found = false;
            for (int a = 0; a < 4; ++a)
            for (int b = 0; b < 4; ++b)
            for (int c = 0; c < 4; ++c)
            for (int d = 0; d < 4; ++d)
            {
                if (a == b || a == c || a == d || b == c || b == d || c == d)
                    continue;
                MorphPoint p0 = targetRelative[a], p1 = targetRelative[b];
                MorphPoint p2 = targetRelative[c], p3 = targetRelative[d];
                double width = ((p1.X + p3.X) - (p0.X + p2.X)) / (2d * dx);
                double depth = ((p2.Z + p3.Z) - (p0.Z + p1.Z)) / (2d * dz);
                double tiltX = ((p1.Y + p3.Y) - (p0.Y + p2.Y)) / (2d * dx);
                double tiltZ = ((p2.Y + p3.Y) - (p0.Y + p1.Y)) / (2d * dz);
                if (!Finite(width) || !Finite(depth) ||
                    !Finite(tiltX) || !Finite(tiltZ) ||
                    width < 0.25d || width > 4d ||
                    depth < 0.25d || depth > 4d ||
                    Math.Abs(tiltX) > maxFaceTilt || Math.Abs(tiltZ) > maxFaceTilt)
                    continue;
                AttachmentFaceShape candidate = new AttachmentFaceShape(width,
                    depth, tiltX, tiltZ, fitted.OffsetX, fitted.OffsetZ);
                AttachmentFaceShape low = connectingFace == 0 ? candidate : bottom;
                AttachmentFaceShape high = connectingFace == 1 ? candidate : top;
                MorphPoint anchor = Transform(nativeNode, min, max, low, high);
                int[] permutation = { a, b, c, d };
                double worst = 0d;
                for (int corner = 0; corner < 4; ++corner)
                {
                    MorphPoint baseCorner = new MorphPoint(
                        (corner & 1) == 0 ? min.X : max.X,
                        faceY, (corner & 2) == 0 ? min.Z : max.Z);
                    MorphPoint actual = Transform(baseCorner, min, max, low, high);
                    MorphPoint relative = new MorphPoint(actual.X - anchor.X,
                        actual.Y - anchor.Y, actual.Z - anchor.Z);
                    worst = Math.Max(worst, Distance(relative,
                        targetRelative[permutation[corner]]));
                    if (worst >= bestError) break;
                }
                if (worst < bestError)
                {
                    bestError = worst;
                    fitted = candidate;
                    found = true;
                }
            }
            return found && bestError <= cornerTolerance;
        }
    }
}
