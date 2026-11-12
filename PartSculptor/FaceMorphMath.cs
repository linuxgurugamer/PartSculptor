// Deterministic, absolute face deformation. Corners on each Y end move as a
// group; linear interpolation between the two faces creates a tapered body.
using System;
using UnityEngine;

namespace PartSculptor
{
    internal struct MorphFace
    {
        internal float Width, Depth, TiltX, TiltZ, OffsetX, OffsetZ;
        internal MorphFace(float width, float depth, float tiltX, float tiltZ)
            : this(width, depth, tiltX, tiltZ, 0f, 0f) { }
        internal MorphFace(float width, float depth, float tiltX, float tiltZ,
            float offsetX, float offsetZ)
        {
            Width = width; Depth = depth; TiltX = tiltX; TiltZ = tiltZ;
            OffsetX = offsetX; OffsetZ = offsetZ;
        }
    }

    internal static class FaceMorphMath
    {
        internal static MorphFace Clamp(MorphFace f)
        {
            f.Width = Mathf.Clamp(f.Width, 0.25f, 4f);
            f.Depth = Mathf.Clamp(f.Depth, 0.25f, 4f);
            f.TiltX = Mathf.Clamp(f.TiltX, -Settings.MaxFaceTilt, Settings.MaxFaceTilt);
            f.TiltZ = Mathf.Clamp(f.TiltZ, -Settings.MaxFaceTilt, Settings.MaxFaceTilt);
            f.OffsetX = Mathf.Clamp(f.OffsetX, -4f, 4f);
            f.OffsetZ = Mathf.Clamp(f.OffsetZ, -4f, 4f);
            return f;
        }

        internal static Vector3 Transform(Vector3 point, Vector3 min, Vector3 max,
            MorphFace bottom, MorphFace top)
        {
            MorphPoint result = FaceMorphCore.Transform(
                new MorphPoint(point.x, point.y, point.z),
                new MorphPoint(min.x, min.y, min.z),
                new MorphPoint(max.x, max.y, max.z),
                bottom.Width, bottom.Depth, bottom.TiltX, bottom.TiltZ,
                top.Width, top.Depth, top.TiltX, top.TiltZ,
                bottom.OffsetX, bottom.OffsetZ, top.OffsetX, top.OffsetZ);
            return new Vector3((float)result.X, (float)result.Y, (float)result.Z);
        }

        // Attach nodes are normals to the deformed surface, not tangent
        // vectors. Preserve the original orientation as the absolute baseline.
        internal static Vector3 TransformNormal(Vector3 point, Vector3 normal,
            Vector3 min, Vector3 max, MorphFace bottom, MorphFace top)
        {
            MorphPoint result = FaceMorphCore.TransformNormal(
                new MorphPoint(point.x, point.y, point.z),
                new MorphPoint(normal.x, normal.y, normal.z),
                new MorphPoint(min.x, min.y, min.z),
                new MorphPoint(max.x, max.y, max.z),
                bottom.Width, bottom.Depth, bottom.TiltX, bottom.TiltZ,
                top.Width, top.Depth, top.TiltX, top.TiltZ,
                bottom.OffsetX, bottom.OffsetZ, top.OffsetX, top.OffsetZ);
            return new Vector3((float)result.X, (float)result.Y, (float)result.Z);
        }

        // Integral of the linearly changing X/Z cross-sectional multipliers.
        // Shear alone has no contribution; this is an approximation when the
        // taper and tilt are both strongly nonuniform.
        internal static float SnapDelta(float delta, float increment)
        {
            return (float)FaceMorphCore.SnapDelta(delta, increment);
        }

        internal static double VolumeFactor(MorphFace b, MorphFace t)
        {
            return FaceMorphCore.VolumeFactor(b.Width, b.Depth, t.Width, t.Depth);
        }

        internal static bool IsIdentity(MorphFace face)
        {
            return Mathf.Abs(face.Width - 1f) < 0.00001f &&
                Mathf.Abs(face.Depth - 1f) < 0.00001f &&
                Mathf.Abs(face.TiltX) < 0.00001f && Mathf.Abs(face.TiltZ) < 0.00001f &&
                Mathf.Abs(face.OffsetX) < 0.00001f && Mathf.Abs(face.OffsetZ) < 0.00001f;
        }
    }
}
