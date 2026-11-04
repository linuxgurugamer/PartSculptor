// Pure C# version of the deformation arithmetic, shared with the executable
// offline RegressionRunner (no Unity or KSP assembly needed).
using System;
namespace PartSculptor
{
    internal struct MorphPoint
    {
        internal double X, Y, Z;
        internal MorphPoint(double x, double y, double z)
        { X = x; Y = y; Z = z; }
    }
    internal static class FaceMorphCore
    {
        // Edge midpoint handles on either face follow the same X-/X+/Z-/Z+
        // ordering. The axis perpendicular to a selected edge is the only
        // axis changed by a regular unlocked drag (or Alt tilt).
        internal static int EdgeWidthSign(int edge)
        { return edge == 0 ? -1 : edge == 1 ? 1 : 0; }
        internal static int EdgeDepthSign(int edge)
        { return edge == 2 ? -1 : edge == 3 ? 1 : 0; }

        // Convert a mouse movement in pixels to the selected projected
        // handle's parameter movement. Projection onto the handle's screen
        // tangent removes camera-angle foreshortening, unlike a world-plane
        // ray displacement used directly as a parameter delta.
        internal static double ProjectedHandleParameterDelta(
            double mouseX, double mouseY, double probePixelsX,
            double probePixelsY, double probeParameterStep)
        {
            double squared = probePixelsX * probePixelsX +
                probePixelsY * probePixelsY;
            if (squared < 1e-8d || double.IsNaN(squared) ||
                double.IsInfinity(squared)) return 0d;
            return (mouseX * probePixelsX + mouseY * probePixelsY) /
                squared * probeParameterStep;
        }

        // Kept for existing callers and craft migration tests; an older morph
        // contains no offsets, so it still produces the original geometry.
        internal static MorphPoint Transform(MorphPoint p, MorphPoint min, MorphPoint max,
            double bottomX, double bottomZ, double bottomTiltX, double bottomTiltZ,
            double topX, double topZ, double topTiltX, double topTiltZ)
        {
            return Transform(p, min, max, bottomX, bottomZ, bottomTiltX, bottomTiltZ,
                topX, topZ, topTiltX, topTiltZ, 0d, 0d, 0d, 0d);
        }

        // Offset values are normalized to half of the native X/Z bounds.
        // The same offset applies to every corner of a face, so translating
        // it never changes that face's width, depth or tilt.
        internal static MorphPoint Transform(MorphPoint p, MorphPoint min, MorphPoint max,
            double bottomX, double bottomZ, double bottomTiltX, double bottomTiltZ,
            double topX, double topZ, double topTiltX, double topTiltZ,
            double bottomOffsetX, double bottomOffsetZ,
            double topOffsetX, double topOffsetZ)
        {
            double t = Math.Max(0d, Math.Min(1d,
                (p.Y - min.Y) / Math.Max(0.001d, max.Y - min.Y)));
            double x = p.X - (min.X + max.X) * .5d;
            double z = p.Z - (min.Z + max.Z) * .5d;
            double sx = bottomX + (topX - bottomX) * t;
            double sz = bottomZ + (topZ - bottomZ) * t;
            double ax = bottomTiltX + (topTiltX - bottomTiltX) * t;
            double az = bottomTiltZ + (topTiltZ - bottomTiltZ) * t;
            double ox = bottomOffsetX + (topOffsetX - bottomOffsetX) * t;
            double oz = bottomOffsetZ + (topOffsetZ - bottomOffsetZ) * t;
            return new MorphPoint((min.X + max.X) * .5d + x * sx +
                    (max.X - min.X) * .5d * ox,
                p.Y + x * ax + z * az,
                (min.Z + max.Z) * .5d + z * sz +
                    (max.Z - min.Z) * .5d * oz);
        }
        // Transform an attach-node SURFACE NORMAL by the inverse-transpose
        // of the local deformation Jacobian. Transforming a direction as two
        // points is incorrect for a tilted face: the Y-edge can remain vertical
        // even though the end face is no longer perpendicular to Y.
        //
        // Using the cofactor matrix avoids an explicit inverse and gives the
        // correct perpendicular normal for top/bottom AND side attach nodes.
        // At an end face, its normal depends on that face's own width/depth
        // and tilt, not on an opposite-face translation or taper.
        internal static MorphPoint TransformNormal(MorphPoint p, MorphPoint normal,
            MorphPoint min, MorphPoint max,
            double bottomX, double bottomZ, double bottomTiltX, double bottomTiltZ,
            double topX, double topZ, double topTiltX, double topTiltZ,
            double bottomOffsetX, double bottomOffsetZ,
            double topOffsetX, double topOffsetZ)
        {
            double height = Math.Max(0.001d, max.Y - min.Y);
            double t = Math.Max(0d, Math.Min(1d, (p.Y - min.Y) / height));
            double x = p.X - (min.X + max.X) * .5d;
            double z = p.Z - (min.Z + max.Z) * .5d;
            double sx = bottomX + (topX - bottomX) * t;
            double sz = bottomZ + (topZ - bottomZ) * t;
            double ax = bottomTiltX + (topTiltX - bottomTiltX) * t;
            double az = bottomTiltZ + (topTiltZ - bottomTiltZ) * t;

            // Columns of the Jacobian: derivatives of the deformed point
            // with respect to original x, y, z (in the part's local frame).
            MorphPoint dx = new MorphPoint(sx, ax, 0d);
            MorphPoint dy = new MorphPoint(
                (x * (topX - bottomX) +
                    (max.X - min.X) * .5d * (topOffsetX - bottomOffsetX)) / height,
                1d + (x * (topTiltX - bottomTiltX) +
                    z * (topTiltZ - bottomTiltZ)) / height,
                (z * (topZ - bottomZ) +
                    (max.Z - min.Z) * .5d * (topOffsetZ - bottomOffsetZ)) / height);
            MorphPoint dz = new MorphPoint(0d, az, sz);

            MorphPoint nx = Cross(dy, dz);
            MorphPoint ny = Cross(dz, dx);
            MorphPoint nz = Cross(dx, dy);
            MorphPoint result = new MorphPoint(
                normal.X * nx.X + normal.Y * ny.X + normal.Z * nz.X,
                normal.X * nx.Y + normal.Y * ny.Y + normal.Z * nz.Y,
                normal.X * nx.Z + normal.Y * ny.Z + normal.Z * nz.Z);
            double length = Math.Sqrt(result.X * result.X +
                result.Y * result.Y + result.Z * result.Z);
            if (length < 1e-10d || double.IsNaN(length) || double.IsInfinity(length))
                return normal;
            return new MorphPoint(result.X / length, result.Y / length,
                result.Z / length);
        }

        private static MorphPoint Cross(MorphPoint a, MorphPoint b)
        {
            return new MorphPoint(a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        }

        // Snap the change from the initial drag, not the saved starting value.
        // This prevents any jump when Shift is pressed without mouse movement.
        internal static double SnapDelta(double delta, double increment)
        {
            if (increment <= 0d || double.IsNaN(increment) ||
                double.IsInfinity(increment)) return delta;
            return Math.Round(delta / increment, 0, MidpointRounding.AwayFromZero) * increment;
        }

        // Ctrl+Alt linked-radial resize. Mouse movement along the picked
        // corner's ORIGINAL radial direction produces a uniform factor for
        // both face dimensions. Orthogonal movement produces no resize.
        // With Shift, snap the common factor (not each dimension separately)
        // so existing non-square faces keep their original aspect ratio.
        // Clamp the factor so BOTH width and depth remain within morph bounds.
        // Pure C# for use by the offline regression runner.
        internal static double RadialScaleFactor(double radiusX, double radiusZ,
            double deltaX, double deltaZ, double initialWidth, double initialDepth,
            double snapIncrement)
        {
            double radiusSquared = radiusX * radiusX + radiusZ * radiusZ;
            if (radiusSquared < 1e-12d || initialWidth <= 0d || initialDepth <= 0d ||
                double.IsNaN(radiusSquared) || double.IsInfinity(radiusSquared))
                return 1d;
            double delta = (radiusX * deltaX + radiusZ * deltaZ) / radiusSquared;
            if (double.IsNaN(delta) || double.IsInfinity(delta)) return 1d;
            if (snapIncrement > 0d) delta = SnapDelta(delta, snapIncrement);
            double lower = Math.Max(0.25d / initialWidth, 0.25d / initialDepth);
            double upper = Math.Min(4d / initialWidth, 4d / initialDepth);
            return Math.Max(lower, Math.Min(upper, 1d + delta));
        }

        internal static double VolumeFactor(double bx, double bz, double tx, double tz)
        {
            return (2d * bx * bz + bx * tz + tx * bz + 2d * tx * tz) / 6d;
        }
    }
}
