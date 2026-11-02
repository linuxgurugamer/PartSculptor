// Dedicated crew-seat integration. CrewCapacity is NOT a generic exponent field.
// Stock IVA seats are authored physical locations. Automatically cloning them
// gives overlapping crew, invalid cameras/portraits and broken EVA, so PartSculptor
// only expands into seats actually supplied by the part's IVA configuration.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PartSculptor
{
    internal sealed class CrewCapacityScaling
    {
        readonly Part part;
        readonly int nativeCapacity;
        readonly double exponent;
        readonly bool dynamicallyManaged;
        int verifiedSeatCount;
        bool seatLayoutChecked;
        string lastWarning;
        internal CrewCapacityMath.Result Last { get; private set; }

        internal CrewCapacityScaling(Part part, double exponent)
        {
            this.part = part;
            this.exponent = exponent;
            Part prefab = part != null && part.partInfo != null ? part.partInfo.partPrefab : null;
            nativeCapacity = prefab != null ? prefab.CrewCapacity : (part != null ? part.CrewCapacity : 0);
            // Inflatable/animated crew pods have their own capacity state
            // machine. Never override it with an editor scaling callback.
            if (part != null)
                foreach (PartModule module in part.Modules)
                {
                    ModuleAnimateGeneric animation = module as ModuleAnimateGeneric;
                    if (animation != null && animation.CrewCapacity != 0)
                    {
                        dynamicallyManaged = true;
                        break;
                    }
                }
        }

        internal bool Apply(float x, float y, float z)
        {
            if (part == null || nativeCapacity <= 0 || dynamicallyManaged ||
                !HighLogic.LoadedSceneIsEditor)
                return false;
            int seats = ReadIvaSeatCount();
            int occupied = part.protoModuleCrew != null ? part.protoModuleCrew.Count : 0;
            CrewCapacityMath.Result next = CrewCapacityMath.Calculate(nativeCapacity,
                (double)x * y * z, exponent, seats, occupied, part.CrewCapacity);
            Last = next;
            if (next.Requested > next.Applied && next.Status != lastWarning)
            {
                lastWarning = next.Status;
                Debug.LogWarning("[PartSculptor] " + part.name +
                    ": requested crew capacity " + next.Requested +
                    " but safe capacity is " + next.Applied + ". " + next.Status);
            }
            if (next.Applied == part.CrewCapacity) return false;
            int before = part.CrewCapacity;
            part.CrewCapacity = next.Applied;
            ScaleDebug.Change(part, "crew", "CrewCapacity", before, next.Applied,
                "requested=" + next.Requested + "; IVA seats=" + seats + "; " + next.Status);
            if (part.PartActionWindow != null) part.PartActionWindow.displayDirty = true;
            return true;
        }

        internal void AppendReport(List<ScaledValue> values)
        {
            if (values == null || nativeCapacity <= 0) return;
            if (dynamicallyManaged)
            {
                values.Add(new ScaledValue("Crew", "Crew capacity", nativeCapacity,
                    part.CrewCapacity, "Managed by ModuleAnimateGeneric; PartSculptor does not override"));
                return;
            }
            if (Last == null) return;
            values.Add(new ScaledValue("Crew", "Crew capacity", nativeCapacity,
                part.CrewCapacity, Last.Status));
            values.Add(new ScaledValue("Crew", "Requested scaled capacity", nativeCapacity,
                Last.Requested, "Exponent=" + exponent + "; may be limited by IVA"));
            values.Add(new ScaledValue("Crew", "Available IVA seat definitions",
                nativeCapacity, verifiedSeatCount,
                "0 means an IVA seat count could not be verified"));
        }

        int ReadIvaSeatCount()
        {
            // A live IVA is authoritative when present, including prop seats.
            if (part.internalModel != null && part.internalModel.seats != null &&
                part.internalModel.seats.Count > 0)
                return verifiedSeatCount = part.internalModel.seats.Count;
            if (seatLayoutChecked) return verifiedSeatCount;
            seatLayoutChecked = true;
            try
            {
                Part prefab = part.partInfo != null ? part.partInfo.partPrefab : null;
                if (prefab != null && prefab.internalModel != null &&
                    prefab.internalModel.seats != null && prefab.internalModel.seats.Count > 0)
                    return verifiedSeatCount = prefab.internalModel.seats.Count;

                string internalName = part.InternalModelName;
                if (string.IsNullOrEmpty(internalName) && part.partInfo != null &&
                    part.partInfo.partConfig != null)
                {
                    ConfigNode ivaReference = part.partInfo.partConfig.GetNode("INTERNAL");
                    if (ivaReference != null) internalName = ivaReference.GetValue("name");
                }
                if (string.IsNullOrEmpty(internalName) || GameDatabase.Instance == null)
                    return verifiedSeatCount;

                foreach (ConfigNode iva in GameDatabase.Instance.GetConfigNodes("INTERNAL"))
                {
                    if (!string.Equals(iva.GetValue("name"), internalName,
                        StringComparison.OrdinalIgnoreCase)) continue;
                    int count = CountSeatModules(iva);
                    // Seats may also be hosted in IVA props, not just directly
                    // on the INTERNAL. Count only explicitly authored seat props.
                    foreach (ConfigNode prop in iva.GetNodes("PROP"))
                    {
                        string propName = prop.GetValue("name");
                        if (string.IsNullOrEmpty(propName)) continue;
                        foreach (ConfigNode propDefinition in GameDatabase.Instance.GetConfigNodes("PROP"))
                            if (string.Equals(propDefinition.GetValue("name"), propName,
                                StringComparison.OrdinalIgnoreCase))
                            {
                                count += CountSeatModules(propDefinition);
                                break;
                            }
                    }
                    return verifiedSeatCount = count;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[PartSculptor] IVA seat check failed: " + e.Message);
            }
            return verifiedSeatCount;
        }

        static int CountSeatModules(ConfigNode node)
        {
            int count = 0;
            foreach (ConfigNode module in node.GetNodes("MODULE"))
                if (string.Equals(module.GetValue("name"), "InternalSeat",
                    StringComparison.OrdinalIgnoreCase)) ++count;
            return count;
        }
    }
}
