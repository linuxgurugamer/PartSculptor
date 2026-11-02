// KSP-independent calculations used by the live scaler and by the automated tests.
// Keep these functions free of Unity/KSP dependencies to test them with dotnet.
using System;

namespace PartSculptor
{
    // Small reference-identity state machine. A craft loaded with a parent
    // is seeded as already attached; only a NEW parent assignment can match.
    // Kept independent of Part so its event/undo decisions are unit-testable.
    internal sealed class NodeAttachWatcher
    {
        private object observedParent;
        private bool pending;
        private int attempts;

        internal void Seed(object initialParent)
        {
            observedParent = initialParent;
            pending = false;
            attempts = 0;
        }

        internal bool Observe(object parent)
        {
            if (!object.ReferenceEquals(parent, observedParent))
            {
                observedParent = parent;
                pending = parent != null;
                attempts = 0;
            }
            return pending;
        }

        internal void Complete() { pending = false; }

        internal void Retry()
        {
            if (++attempts >= 12) pending = false;
        }
    }

    internal static class ScaleMath
    {
        // KSP's simulator may ask for several staging snapshots with different
        // supplied base masses. The returned modifier must cancel *that* base,
        // not the live Part.mass, or total simulated dry mass will oscillate.
        // The caller supplies the prefab-anchored target and independent extras.
        internal static double MassModifierDelta(double suppliedBaseMass,
            double targetDryMass, double tweakScaleDelta,
            double b9ExtraMass, double fuelSwitchExtraMass)
        {
            return targetDryMass - suppliedBaseMass - tweakScaleDelta +
                b9ExtraMass + fuelSwitchExtraMass;
        }

        // B9 baseVolume is a volume, not a linear dimension. Always compute
        // from the native prefab/saved value rather than last frame's value.
        internal static double B9BaseVolume(double native, double stretchVolume)
        {
            if (native < 0d || double.IsNaN(native) || double.IsInfinity(native) ||
                !PositiveFinite(stretchVolume)) return double.NaN;
            return native * stretchVolume;
        }

        // B9's live modifier includes already-stretched volume-derived values,
        // plus fixed subtype addedMass/addedCost. Produce ONLY PartSculptor's
        // remaining delta to reach the desired original-value exponent target.
        internal static double B9ModifierExtra(double current, double fixedComponent,
            double nativeToCurrentVolumeRatio, double exponentFactor)
        {
            return ((current - fixedComponent) * nativeToCurrentVolumeRatio +
                fixedComponent) * exponentFactor - current;
        }
        internal struct Axes
        {
            public readonly double X, Y, Z;
            internal Axes(double x, double y, double z) { X = x; Y = y; Z = z; }
        }

        internal static bool PositiveFinite(double value)
        {
            return value > 0d && !double.IsInfinity(value) && !double.IsNaN(value);
        }

        // Absolute gizmo value based on the START of a drag, never the last
        // displayed scale (prevents incremental feedback/cumulative scaling).
        internal static double GizmoScaleValue(double baseline, double pixelDelta,
            double pixelsPerScale, double min, double max, double step)
        {
            if (!PositiveFinite(baseline) || !PositiveFinite(pixelsPerScale) ||
                double.IsNaN(pixelDelta) || double.IsInfinity(pixelDelta)) return baseline;
            double delta = pixelDelta / pixelsPerScale;
            if (PositiveFinite(step)) delta = Math.Round(delta / step) * step;
            return Math.Max(min, Math.Min(max, baseline + delta));
        }

        // Follow the rendered handle, not a fixed mouse-delta estimate.
        // The current scale is an absolute model value, and the screen error
        // measures the remaining distance between the cursor and that handle.
        // Applying the correction again with zero error is an exact no-op;
        // it cannot compound the part's scale. Snap relative to drag start.
        internal static double GizmoFollowValue(double dragStartScale,
            double currentScale, double handleErrorPixels,
            double pixelsPerUnitScale, double min, double max, double step)
        {
            if (!PositiveFinite(currentScale) ||
                !PositiveFinite(pixelsPerUnitScale) ||
                double.IsNaN(handleErrorPixels) ||
                double.IsInfinity(handleErrorPixels)) return currentScale;
            double offsetFromStart = currentScale - dragStartScale;
            return GizmoScaleValue(dragStartScale,
                offsetFromStart * pixelsPerUnitScale + handleErrorPixels,
                pixelsPerUnitScale, min, max, step);
        }

        // Applies a requested gizmo axis value from the current absolute axes.
        // Locked proportions preserve the before-axis ratios.
        internal static Axes GizmoAxes(double x, double y, double z,
            int axis, double requested, bool locked)
        {
            return GizmoAxes(x, y, z, axis, requested, locked,
                0d, double.MaxValue);
        }

        // For locked proportions, clamp the shared multiplier before applying
        // it to ANY axis; clamping each component afterwards breaks the lock.
        internal static Axes GizmoAxes(double x, double y, double z,
            int axis, double requested, bool locked, double min, double max)
        {
            if (axis < 0 || axis > 2 || !PositiveFinite(requested))
                return new Axes(x, y, z);
            double oldValue = axis == 0 ? x : axis == 1 ? y : z;
            if (!PositiveFinite(oldValue)) return new Axes(x, y, z);
            if (locked && PositiveFinite(x) && PositiveFinite(y) && PositiveFinite(z))
            {
                double factor = requested / oldValue;
                if (PositiveFinite(max))
                    factor = Math.Min(factor, Math.Min(max / x,
                        Math.Min(max / y, max / z)));
                if (PositiveFinite(min))
                    factor = Math.Max(factor, Math.Max(min / x,
                        Math.Max(min / y, min / z)));
                return new Axes(x * factor, y * factor, z * factor);
            }
            if (PositiveFinite(max)) requested = Math.Min(requested, max);
            if (PositiveFinite(min)) requested = Math.Max(requested, min);
            return new Axes(axis == 0 ? requested : x,
                axis == 1 ? requested : y, axis == 2 ? requested : z);
        }

        // Absolute post-load correction for a pair of connected stack nodes.
        // After applying it once the resulting correction is exactly zero;
        // never compound the previous position delta.
        internal static double AttachmentCorrection(double expectedNodeWorld,
            double actualNodeWorld)
        {
            return expectedNodeWorld - actualNodeWorld;
        }

        internal static double Volume(double x, double y, double z)
        {
            return x * y * z;
        }

        // An exponent for uniform scaling must still work for nonuniform axes.
        internal static double ExponentFactor(double volume, double exponent)
        {
            return Math.Pow(volume, exponent / 3d);
        }

        internal static double ScaledValue(double baseline, double volume, double exponent)
        {
            return baseline * ExponentFactor(volume, exponent);
        }

        // Docking uses an actual physical face diameter, not the rounded
        // AttachNode.size tier. This uses the same cross-section definition as
        // attachment node matching, which remains stable under nonuniform axes.
        internal static double DockingFaceScale(double nx, double ny, double nz,
            double sx, double sy, double sz)
        {
            if (!PositiveFinite(sx) || !PositiveFinite(sy) || !PositiveFinite(sz))
                return 1d;
            return NodeDiameter(1, nx, ny, nz, sx, sy, sz) / 1.25d;
        }

        // KSP docking compatibility is based on a string nodeType, not on the
        // connection's physical dimensions. Do NOT label an arbitrary 1.875m
        // face as a 2.5m port: it would dock to an incompatible stock Senior.
        // Intermediate sizes get stable size-specific types; two matching
        // stretched ports can still mate. All mod-defined types are untouched.
        internal static string DockingNodeType(string nativeType, double diameter,
            double stockTolerance = 0.05d)
        {
            if (nativeType != "size0" && nativeType != "size1" && nativeType != "size2")
                return nativeType;
            if (!PositiveFinite(diameter)) return nativeType;
            if (stockTolerance < 0d || stockTolerance > 0.25d)
                stockTolerance = 0.05d;
            double[] sizes = { 0.625d, 1.25d, 2.5d };
            int best = 0;
            double difference = double.MaxValue;
            for (int i = 0; i < sizes.Length; ++i)
            {
                double relative = Math.Abs(diameter / sizes[i] - 1d);
                if (relative < difference) { difference = relative; best = i; }
            }
            if (difference <= stockTolerance)
                return "size" + best;
            // Quantize to the nearest millimeter to avoid floating-point
            // noise from yielding different types for identical nominal sizes.
            long millimeters = (long)Math.Round(diameter * 1000d,
                MidpointRounding.AwayFromZero);
            return "PartSculptorDock_" + millimeters + "mm";
        }

        internal static double NominalNodeDiameter(int tier)
        {
            if (tier < 0) return 0d;
            return tier == 0 ? 0.625d : tier * 1.25d;
        }

        // Cross-sectional diameter of an attachment node under nonuniform scaling.
        // The normal is in the unscaled part-local coordinate system.
        internal static double NodeDiameter(int tier, double nx, double ny, double nz,
            double sx, double sy, double sz)
        {
            double diameter = NominalNodeDiameter(tier);
            if (!PositiveFinite(diameter)) return 0d;
            double nLength = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (!(nLength > 1e-12d)) { nx = 0d; ny = 1d; nz = 0d; }
            else { nx /= nLength; ny /= nLength; nz /= nLength; }
            sx = Math.Abs(sx); sy = Math.Abs(sy); sz = Math.Abs(sz);
            double ax = sy * sz * nx;
            double ay = sx * sz * ny;
            double az = sx * sy * nz;
            return diameter * Math.Sqrt(Math.Sqrt(ax * ax + ay * ay + az * az));
        }

        internal static int NodeSizeTier(int originalTier, double nx, double ny, double nz,
            double sx, double sy, double sz)
        {
            if (originalTier < 0) return originalTier;
            if (sx == 1d && sy == 1d && sz == 1d) return originalTier;
            double diameter = NodeDiameter(originalTier, nx, ny, nz, sx, sy, sz);
            if (diameter < 0.9375d) return 0;
            if (diameter >= 1250d) return Math.Max(originalTier, 1000);
            return Math.Max(1, (int)Math.Round(diameter / 1.25d,
                MidpointRounding.AwayFromZero));
        }

        // For an axial stack node, stretch only the two face axes, retaining
        // its length. Diagonal node normals use uniform scaling instead.
        internal static Axes NodeMatchMultipliers(double nx, double ny, double nz,
            double targetDiameter, double sourceDiameter)
        {
            if (!PositiveFinite(targetDiameter) || !PositiveFinite(sourceDiameter))
                return new Axes(1d, 1d, 1d);
            double ratio = targetDiameter / sourceDiameter;
            if (!PositiveFinite(ratio)) return new Axes(1d, 1d, 1d);
            double norm = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (norm > 1e-10d) { nx /= norm; ny /= norm; nz /= norm; }
            // A 0.98 threshold avoids surprising changes for oblique nodes.
            if (Math.Abs(nx) >= 0.98d) return new Axes(1d, ratio, ratio);
            if (Math.Abs(ny) >= 0.98d) return new Axes(ratio, 1d, ratio);
            if (Math.Abs(nz) >= 0.98d) return new Axes(ratio, ratio, 1d);
            return new Axes(ratio, ratio, ratio);
        }
    }
}
