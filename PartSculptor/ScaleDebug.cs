// Change-based logging for PartSculptor; also available in Release when enabled in Settings.cfg.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

using static PartSculptor.RegisterToolbar;

namespace PartSculptor
{
    internal static class ScaleDebug
    {
        // Weak keys prevent entries for deleted/undo-replaced parts from leaking.
        static readonly ConditionalWeakTable<object, Dictionary<string, string>> computed =
            new ConditionalWeakTable<object, Dictionary<string, string>>();

        internal static bool Enabled { get { return Settings.DebugLogChanges; } }

        static string PartLabel(Part part)
        {
            return part == null ? "<null part>" :
                (part.name ?? "<unnamed>") + "#" + part.GetInstanceID().ToString(CultureInfo.InvariantCulture);
        }

        internal static string Format(object value)
        {
            if (value == null) return "<null>";
            if (value is float) return ((float)value).ToString("R", CultureInfo.InvariantCulture);
            if (value is double) return ((double)value).ToString("R", CultureInfo.InvariantCulture);
            if (value is Vector3)
            {
                Vector3 v = (Vector3)value;
                return "(" + Format(v.x) + ", " + Format(v.y) + ", " + Format(v.z) + ")";
            }
            FloatCurve curve = value as FloatCurve;
            if (curve != null)
            {
                if (curve.Curve == null) return "FloatCurve <null>";
                var sb = new StringBuilder("FloatCurve[");
                Keyframe[] keys = curve.Curve.keys;
                for (int i = 0; i < keys.Length; i++)
                {
                    if (i > 0) sb.Append("; ");
                    sb.Append(Format(keys[i].time)).Append(":")
                        .Append(Format(keys[i].value)).Append(" (in=")
                        .Append(Format(keys[i].inTangent)).Append(",out=")
                        .Append(Format(keys[i].outTangent)).Append(")");
                }
                return sb.Append("]").ToString();
            }
            IFormattable formatted = value as IFormattable;
            return formatted != null ? formatted.ToString(null, CultureInfo.InvariantCulture) : value.ToString();
        }

        internal static void Change(Part part, string area, string name,
            object before, object after, string details = null)
        {
            if (!Enabled) return;
            string oldValue = Format(before);
            string newValue = Format(after);
            if (string.Equals(oldValue, newValue, StringComparison.Ordinal)) return;
            Log.Info("[PartSculptor][DEBUG] " + PartLabel(part) + " [" + area + "] " +
                name + ": " + oldValue + " -> " + newValue +
                (string.IsNullOrEmpty(details) ? "" : "; " + details));
        }

        internal static void Info(Part part, string area, string message)
        {
            if (!Enabled) return;
            Log.Info("[PartSculptor][DEBUG] " + PartLabel(part) + " [" + area + "] " + message);
        }

        // KSP queries IPartMassModifier/IPartCostModifier frequently (possibly
        // many times per frame). Only emit when the answer or its inputs change.
        internal static void Computed(object owner, Part part, string area,
            string name, double value, string details)
        {
            if (!Enabled || owner == null) return;
            Dictionary<string, string> previous = computed.GetValue(owner,
                _ => new Dictionary<string, string>(StringComparer.Ordinal));
            string current = Format(value) + " | " + (details ?? "");
            string old;
            if (previous.TryGetValue(area + "/" + name, out old) && old == current) return;
            previous[area + "/" + name] = current;
            Log.Info("[PartSculptor][DEBUG] " + PartLabel(part) + " [" + area +
                "] " + name + ": " + (old == null ? "<first>" : old) +
                " -> " + current);
        }
    }
}
