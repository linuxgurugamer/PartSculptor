// Local-axis mapping for incremental attached-face morph steps.
// Copy mirrors longitudinal/Y tilt across the joint while keeping each
// radial X/Z change on the same visible side of the two parts.
// Pure C# so the sign convention can be tested without KSP.
using System;

namespace PartSculptor
{
    internal struct MorphStep
    {
        internal double Width, Depth, TiltX, TiltZ, OffsetX, OffsetZ;
        internal MorphStep(double width, double depth, double tiltX, double tiltZ,
            double offsetX, double offsetZ)
        {
            Width = width; Depth = depth; TiltX = tiltX; TiltZ = tiltZ;
            OffsetX = offsetX; OffsetZ = offsetZ;
        }
        internal static MorphStep Subtract(MorphStep end, MorphStep start)
        {
            return new MorphStep(end.Width - start.Width, end.Depth - start.Depth,
                end.TiltX - start.TiltX, end.TiltZ - start.TiltZ,
                end.OffsetX - start.OffsetX, end.OffsetZ - start.OffsetZ);
        }
        internal static MorphStep Add(MorphStep current, MorphStep delta)
        {
            return new MorphStep(current.Width + delta.Width, current.Depth + delta.Depth,
                current.TiltX + delta.TiltX, current.TiltZ + delta.TiltZ,
                current.OffsetX + delta.OffsetX, current.OffsetZ + delta.OffsetZ);
        }
        // Use the same tilt-slope limit as the Morph gizmo and face fitter.
        // The caller supplies Settings.MaxFaceTilt so this pure-math type
        // remains independent of KSP settings and can be tested offline.
        internal static bool Valid(MorphStep s, double maxFaceTilt)
        {
            if (!Finite(maxFaceTilt) || maxFaceTilt <= 0d) return false;
            return Finite(s.Width) && Finite(s.Depth) && Finite(s.TiltX) &&
                Finite(s.TiltZ) && Finite(s.OffsetX) && Finite(s.OffsetZ) &&
                s.Width >= .25d && s.Width <= 4d &&
                s.Depth >= .25d && s.Depth <= 4d &&
                Math.Abs(s.TiltX) <= maxFaceTilt && Math.Abs(s.TiltZ) <= maxFaceTilt &&
                Math.Abs(s.OffsetX) <= 4d && Math.Abs(s.OffsetZ) <= 4d;
        }
        private static bool Finite(double x)
        { return !double.IsNaN(x) && !double.IsInfinity(x); }
    }

    internal static class FaceMorphStepCore
    {
        // Copy angular deformation in PHYSICAL slopes (rise/run), not in
        // raw TiltX/TiltZ parameters.  The readout's actual face angle is
        // atan(hypot(TiltX * scaleY / (Width * scaleX),
        //            TiltZ * scaleY / (Depth * scaleZ))).
        //
        // A neighboring face can have a different width, depth or scale, so
        // copying its raw TiltX/TiltZ can produce a visibly smaller angle.
        // Treat this as an INCREMENTAL copy: map the change in signed slopes
        // onto the same visible side, reversing the longitudinal/Y direction,
        // then retain the receiver's existing inclination difference.
        internal static bool TryMirrorInclinationStep(
            MorphStep sourceBefore, MorphStep sourceAfter,
            MorphStep receiverBefore, MorphStep receiverProposed,
            double sourceScaleX, double sourceScaleY, double sourceScaleZ,
            double receiverScaleX, double receiverScaleY, double receiverScaleZ,
            double recipientXSourceX, double recipientXSourceZ,
            double recipientZSourceX, double recipientZSourceZ,
            double recipientYSourceY, out MorphStep corrected)
        {
            corrected = receiverProposed;
            if (!ValidScales(sourceBefore, sourceScaleX, sourceScaleY, sourceScaleZ) ||
                !ValidScales(sourceAfter, sourceScaleX, sourceScaleY, sourceScaleZ) ||
                !ValidScales(receiverBefore, receiverScaleX, receiverScaleY, receiverScaleZ) ||
                !ValidScales(receiverProposed, receiverScaleX, receiverScaleY, receiverScaleZ))
                return false;

            double sourceXDelta =
                SlopeX(sourceAfter, sourceScaleX, sourceScaleY) -
                SlopeX(sourceBefore, sourceScaleX, sourceScaleY);
            double sourceZDelta =
                SlopeZ(sourceAfter, sourceScaleZ, sourceScaleY) -
                SlopeZ(sourceBefore, sourceScaleZ, sourceScaleY);
            MorphStep mirroredSlopes;
            if (!TryMapMirrored(new MorphStep(0d, 0d, sourceXDelta, sourceZDelta, 0d, 0d),
                recipientXSourceX, recipientXSourceZ,
                recipientZSourceX, recipientZSourceZ, recipientYSourceY,
                out mirroredSlopes)) return false;

            double newSlopeX = SlopeX(receiverBefore, receiverScaleX, receiverScaleY) +
                mirroredSlopes.TiltX;
            double newSlopeZ = SlopeZ(receiverBefore, receiverScaleZ, receiverScaleY) +
                mirroredSlopes.TiltZ;
            corrected.TiltX = newSlopeX * receiverProposed.Width *
                receiverScaleX / receiverScaleY;
            corrected.TiltZ = newSlopeZ * receiverProposed.Depth *
                receiverScaleZ / receiverScaleY;
            return Finite(corrected.TiltX) && Finite(corrected.TiltZ);
        }

        private static double SlopeX(MorphStep face, double sx, double sy)
        { return face.TiltX * sy / (face.Width * sx); }

        private static double SlopeZ(MorphStep face, double sz, double sy)
        { return face.TiltZ * sy / (face.Depth * sz); }

        private static bool ValidScales(MorphStep face, double sx, double sy, double sz)
        {
            return Finite(face.Width) && Finite(face.Depth) &&
                Finite(face.TiltX) && Finite(face.TiltZ) &&
                Finite(sx) && Finite(sy) && Finite(sz) &&
                Math.Abs(face.Width * sx) > 1e-8d &&
                Math.Abs(face.Depth * sz) > 1e-8d &&
                Math.Abs(sy) > 1e-8d;
        }

        // An attached face is viewed from the opposite side of the stack
        // joint. Keep X/Z displacement on the SAME visible side, but reverse
        // the longitudinal (Y) motion: right down becomes right up. This is
        // deliberately not the geometry-fitting / flush-face Match mode.
        // TryMap accounts for native X/Z reversals and 90-degree rolls;
        // flipping only the tilt components creates the mirror across Y.
        internal static bool TryMapMirrored(MorphStep delta,
            double recipientXSourceX, double recipientXSourceZ,
            double recipientZSourceX, double recipientZSourceZ,
            double recipientYSourceY, out MorphStep mirrored)
        {
            mirrored = new MorphStep(0, 0, 0, 0, 0, 0);
            MorphStep mapped;
            if (!TryMap(delta, recipientXSourceX, recipientXSourceZ,
                recipientZSourceX, recipientZSourceZ,
                recipientYSourceY, out mapped)) return false;
            mirrored = new MorphStep(mapped.Width, mapped.Depth,
                -mapped.TiltX, -mapped.TiltZ,
                mapped.OffsetX, mapped.OffsetZ);
            return Finite(mirrored.Width) && Finite(mirrored.Depth) &&
                Finite(mirrored.TiltX) && Finite(mirrored.TiltZ) &&
                Finite(mirrored.OffsetX) && Finite(mirrored.OffsetZ);
        }

        // Dot products of the recipient's local axes with the source's
        // LOCAL X/Z/Y axes, expressed in WORLD directions. This maps roll
        // and reversed axes when the two connected stack ends face opposite
        // directions. Face parameters are unitless; native sizes may differ.
        // Mild skew caused by a tilted stack-node normal is accepted, but
        // a diagonal/ambiguous 45-degree roll is not guessed.
        internal static bool TryMap(MorphStep delta,
            double recipientXSourceX, double recipientXSourceZ,
            double recipientZSourceX, double recipientZSourceZ,
            double recipientYSourceY, out MorphStep result)
        {
            result = new MorphStep(0, 0, 0, 0, 0, 0);
            if (!Finite(recipientXSourceX) || !Finite(recipientXSourceZ) ||
                !Finite(recipientZSourceX) || !Finite(recipientZSourceZ) ||
                !Finite(recipientYSourceY) ||
                Math.Abs(recipientYSourceY) < .70d) return false;
            bool xFromX = Math.Abs(recipientXSourceX) >= Math.Abs(recipientXSourceZ);
            bool zFromX = Math.Abs(recipientZSourceX) >= Math.Abs(recipientZSourceZ);
            if (xFromX == zFromX) return false; // Both mapping to one axis
            double xMain = xFromX ? recipientXSourceX : recipientXSourceZ;
            double xOther = xFromX ? recipientXSourceZ : recipientXSourceX;
            double zMain = zFromX ? recipientZSourceX : recipientZSourceZ;
            double zOther = zFromX ? recipientZSourceZ : recipientZSourceX;
            if (Math.Abs(xMain) < .72d || Math.Abs(zMain) < .72d ||
                Math.Abs(xOther) > .65d || Math.Abs(zOther) > .65d)
                return false;
            double xSign = Math.Sign(xMain), zSign = Math.Sign(zMain);
            double ySign = Math.Sign(recipientYSourceY);
            result = new MorphStep(
                xFromX ? delta.Width : delta.Depth,
                zFromX ? delta.Width : delta.Depth,
                xSign * ySign * (xFromX ? delta.TiltX : delta.TiltZ),
                zSign * ySign * (zFromX ? delta.TiltX : delta.TiltZ),
                xSign * (xFromX ? delta.OffsetX : delta.OffsetZ),
                zSign * (zFromX ? delta.OffsetX : delta.OffsetZ));
            return Finite(result.Width) && Finite(result.Depth) &&
                Finite(result.TiltX) && Finite(result.TiltZ) &&
                Finite(result.OffsetX) && Finite(result.OffsetZ);
        }
        private static bool Finite(double x)
        { return !double.IsNaN(x) && !double.IsInfinity(x); }
    }
}
