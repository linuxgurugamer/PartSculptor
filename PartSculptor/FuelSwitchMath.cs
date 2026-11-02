using System;

namespace PartSculptor
{
    // Dependency-free calculations exercised by the executable regression runner.
    // Module switchers own the native contents; PartSculptor applies an absolute
    // multiplier to those values, never to a previously scaled result.
    internal static class FuelSwitchMath
    {
        internal static bool FinitePositive(double value)
        {
            return value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);
        }

        internal static bool Close(double a, double b)
        {
            return Math.Abs(a - b) <= Math.Max(1e-7, Math.Max(Math.Abs(a), Math.Abs(b)) * 1e-7);
        }

        internal static double AbsoluteCapacity(double native, double tweak, double stretch)
        {
            return native * tweak * stretch;
        }

        // A retained, exactly matching variant baseline wins on reload.
        // New capacities supplied by a fuel switch are typically already
        // TweakScale-adjusted but not PartSculptor-adjusted.
        internal static double NativeCapacity(double live, double tweak,
            double saved, bool hasSaved, double previousStretch,
            double previousNative, bool sameLiveAsLastWritten)
        {
            if (hasSaved && FinitePositive(saved)) return saved;
            if (sameLiveAsLastWritten && FinitePositive(previousNative)) return previousNative;
            if (!FinitePositive(tweak)) tweak = 1;
            if (FinitePositive(previousNative) &&
                Close(live, previousNative * tweak * previousStretch))
                return previousNative;
            return live / tweak;
        }
    }
}
