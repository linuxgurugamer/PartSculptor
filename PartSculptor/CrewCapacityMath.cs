using System;

namespace PartSculptor
{
    // Pure production calculation shared with the executable regression tests.
    // No imaginary IVA seats are ever counted or created.
    internal static class CrewCapacityMath
    {
        internal sealed class Result
        {
            internal int Requested;
            internal int Applied;
            internal int SeatLimit;
            internal string Status;
        }

        internal static Result Calculate(int nativeCapacity, double volumeFactor,
            double exponent, int availableIvaSeats, int occupied, int currentCapacity)
        {
            var result = new Result();
            nativeCapacity = Math.Max(0, nativeCapacity);
            occupied = Math.Max(0, occupied);
            // Never turn an originally uncrewed part into a crewed part.
            if (nativeCapacity == 0)
            {
                result.Status = "Not a crewed part";
                result.Applied = currentCapacity;
                return result;
            }
            double multiplier = Math.Pow(Math.Max(1e-12d, volumeFactor), exponent / 3d);
            double proposed = nativeCapacity * multiplier;
            if (double.IsNaN(proposed) || double.IsInfinity(proposed)) proposed = nativeCapacity;
            result.Requested = (int)Math.Max(1d, Math.Min(128d,
                Math.Floor(proposed + 0.5d)));

            // Seat capacity is known only if a real IVA seat layout is available.
            // For unknown/incomplete internals use the ORIGINAL capacity, never
            // invent seats by changing CrewCapacity alone.
            result.SeatLimit = Math.Max(nativeCapacity,
                availableIvaSeats > 0 ? availableIvaSeats : 0);
            int safe = Math.Min(result.Requested, result.SeatLimit);
            if (occupied != 0)
            {
                // Keep seating indices and existing Kerbals unchanged. Live
                // changes wait until the pod is empty in the editor.
                result.Applied = Math.Max(currentCapacity, occupied);
                result.Status = "Crew aboard: capacity unchanged until empty";
            }
            else
            {
                result.Applied = safe;
                result.Status = availableIvaSeats <= 0
                    ? "IVA seat layout unavailable; using original crew limit"
                    : result.Requested > safe
                        ? "Limited by real IVA seats; no synthetic seats created"
                        : "IVA seat capacity verified";
            }
            return result;
        }
    }
}
