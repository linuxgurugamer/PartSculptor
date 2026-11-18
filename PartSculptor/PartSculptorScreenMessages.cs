// Consistent KSP screen messages with unconditional, searchable part provenance.
// Screen messages are transient; their full context is always retained in KSP.log.
using System;
using UnityEngine;
using static PartSculptor.RegisterToolbar;

namespace PartSculptor
{
    internal static class PartSculptorScreenMessages
    {
        internal static void Post(Part part, string message, float duration,
            ScreenMessageStyle style, string source)
        {
            // Try to collect metadata without depending on a part remaining alive.
            string title = "<unknown>";
            string name = "<unknown>";
            string instance = "<none>";
            string path = "<unavailable>";
            string configFile = "<unavailable>";
            try
            {
                if (part != null)
                {
                    name = part.name ?? "<unnamed>";
                    instance = part.GetInstanceID().ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if (part.partInfo != null)
                    {
                        title = string.IsNullOrEmpty(part.partInfo.title) ? name : part.partInfo.title;
                        // AvailablePart.partUrl carries the original config's
                        // relative GameData source path, identifying the mod.
                        string config = part.partInfo.configFileFullName;
                        if (!string.IsNullOrEmpty(config)) configFile = config;
                        string url = part.partInfo.partUrl;
                        if (!string.IsNullOrEmpty(url))
                        {
                            url = url.Replace('\\', '/').TrimStart('/');
                            path = url.StartsWith("GameData/", StringComparison.OrdinalIgnoreCase)
                                ? url : "GameData/" + url;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Even if metadata lookup fails, still log the actual message.
                Log.Warning("[ScreenMessage] Part context lookup failed: " + e);
            }
            // Never conditioned on debugLogChanges: every on-screen message
            // leaves a searchable source/part/message entry in KSP.log.
            Log.Info("[ScreenMessage] source=" + Safe(source) +
                " partTitle=\"" + Safe(title) + "\" partName=\"" + Safe(name) +
                "\" instanceId=" + instance + " partPath=\"" + Safe(path) +
                "\" configFile=\"" + Safe(configFile) +
                "\" message=\"" + Safe(message) + "\"");
            ScreenMessages.PostScreenMessage(message, duration, style);
        }

        private static string Safe(string value)
        {
            return (value ?? "<null>").Replace("\\", "\\\\")
                .Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }
}
