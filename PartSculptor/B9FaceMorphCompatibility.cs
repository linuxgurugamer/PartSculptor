// B9PartSwitch is not inherently a geometry switcher. In particular, the
// common fuelSwitch uses tankType/addedMass and leaves meshes/nodes alone.
// Only allow face morphing when the final ModuleManager-applied configuration
// confirms that EVERY B9 subtype on this specific switcher is resource-only.
// Unknown configuration is deliberately rejected rather than risking a mesh
// or attachment-node mutation while a morph is active.
using System;
using System.Collections.Generic;

namespace PartSculptor
{
    internal static class B9FaceMorphCompatibility
    {
        // Valid top-level ModuleB9PartSwitch fields that cannot change the
        // model geometry. Unknown fields are blocked pending review.
        private static readonly HashSet<string> ModuleFields = Set(
            "name", "moduleID", "baseVolume", "switcherDescription",
            "switcherDescriptionPlural", "switchInFlight", "affectDragCubes",
            "uiGroupName", "uiGroupDisplayName", "currentSubtype",
            "currentSubtypeIndex", "defaultSubtype", "showSubtypeTitle",
            "showCurrentSubtype", "showInfo", "showCurrentSubtypeInfo");

        // Resource, capacity, UI and physical-property switches are compatible
        // with static shape morphing; transform/node/material/module switches
        // are not. Unknown subtype fields and child nodes are blocked.
        private static readonly HashSet<string> SubtypeFields = Set(
            "name", "title", "description", "tankType", "addedMass", "addedCost",
            "volumeAdded", "volumeMultiplier", "percentFilled",
            "resourcesTweakable", "defaultSubtypePriority", "upgradeRequired",
            "techRequired", "primaryColor", "secondaryColor", "infoDetail",
            "crashTolerance", "maxTemp", "skinMaxTemp", "skinMassPerArea",
            "skinInternalConductionMult", "costMultiplier", "massMultiplier");

        private static readonly HashSet<string> ResourceFields = Set(
            "name", "unitsPerVolume", "percentFilled", "resourcesTweakable",
            "amount", "maxAmount");

        private static HashSet<string> Set(params string[] fields)
        {
            return new HashSet<string>(fields, StringComparer.OrdinalIgnoreCase);
        }

        private static bool AllowedValues(ConfigNode config, HashSet<string> fields,
            string location, out string reason)
        {
            foreach (ConfigNode.Value entry in config.values)
            {
                if (fields.Contains(entry.name)) continue;
                reason = location + " has unsupported field '" + entry.name + "'";
                return false;
            }
            reason = "";
            return true;
        }

        internal static bool IsB9(PartModule module)
        {
            return module != null &&
                (string.Equals(module.moduleName, "ModuleB9PartSwitch",
                    StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(module.GetType().Name, "ModuleB9PartSwitch",
                    StringComparison.OrdinalIgnoreCase));
        }

        private static ConfigNode FindSwitchConfig(Part part, PartModule switcher)
        {
            ConfigNode partConfig = part != null && part.partInfo != null ?
                part.partInfo.partConfig : null;
            if (partConfig == null || part.Modules == null) return null;

            // KSP Modules and PART/MODULE declarations follow the same order.
            // Compare moduleID as well, so another B9 instance cannot make a
            // geometry switcher appear safe if config/module order differs.
            int ordinal = 0;
            foreach (PartModule module in part.Modules)
            {
                if (!IsB9(module)) continue;
                if (object.ReferenceEquals(module, switcher)) break;
                ++ordinal;
            }
            int index = 0;
            foreach (ConfigNode node in partConfig.GetNodes("MODULE"))
            {
                if (!string.Equals(node.GetValue("name"), "ModuleB9PartSwitch",
                    StringComparison.OrdinalIgnoreCase)) continue;
                if (index++ != ordinal) continue;
                string runtimeId = "";
                try
                {
                    System.Reflection.MemberInfo member =
                        ExponentBinding.FindMember(switcher.GetType(), "moduleID");
                    object value = member != null ?
                        ExponentBinding.Read(member, switcher) : null;
                    if (value != null) runtimeId = Convert.ToString(value);
                }
                catch { return null; }
                if (!string.Equals(runtimeId, node.GetValue("moduleID") ?? "",
                    StringComparison.Ordinal)) return null;
                return node;
            }
            return null;
        }

        internal static bool AllowsFaceMorph(Part part, PartModule switcher,
            out string reason)
        {
            ConfigNode config = FindSwitchConfig(part, switcher);
            if (config == null)
            {
                reason = "B9 switcher configuration missing or moduleID mismatch";
                return false;
            }
            string id = config.GetValue("moduleID") ?? "";
            string label = "ModuleB9PartSwitch" +
                (id.Length != 0 ? " (" + id + ")" : "");
            if (!AllowedValues(config, ModuleFields, label, out reason)) return false;

            int count = 0;
            foreach (ConfigNode child in config.nodes)
            {
                if (!string.Equals(child.name, "SUBTYPE",
                    StringComparison.OrdinalIgnoreCase))
                {
                    reason = label + " contains unsupported node '" + child.name + "'";
                    return false;
                }
                ++count;
                string subtype = child.GetValue("name") ?? "(unnamed)";
                string where = label + " / SUBTYPE " + subtype;
                if (!AllowedValues(child, SubtypeFields, where, out reason)) return false;
                foreach (ConfigNode nested in child.nodes)
                {
                    if (!string.Equals(nested.name, "RESOURCE",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        reason = where + " contains geometry/unknown node '" +
                            nested.name + "'";
                        return false;
                    }
                    if (!AllowedValues(nested, ResourceFields,
                        where + " / RESOURCE", out reason)) return false;
                    if (nested.nodes.Count != 0)
                    {
                        reason = where + " / RESOURCE contains nested nodes";
                        return false;
                    }
                }
            }
            if (count == 0)
            {
                reason = label + " has no verified resource-only SUBTYPE nodes";
                return false;
            }
            reason = "";
            return true;
        }
    }
}
