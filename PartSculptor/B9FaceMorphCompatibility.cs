// B9PartSwitch may select from existing meshes using SUBTYPE transform values.
// PartSculptor can morph these static alternatives because FaceMorphSupport
// captures BOTH active and inactive model meshes, without changing B9's
// transform activation state. TEXTURE replaces renderer materials only.
// Reject geometry *repositioning* (TRANSFORM nodes), attach-node changes, and
// unknown features. Validate EVERY subtype after ModuleManager patches.
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
            "showCurrentSubtype", "showInfo", "showCurrentSubtypeInfo",
            "affectFARVoxels", "advancedTweakablesOnly", "parentID");

        // B9's SUBTYPE transform = ... selects existing GameObjects (turns them
        // on/off). FaceMorphSupport captures all static meshes, including those
        // in inactive GameObjects, so these alternatives are supported. This is
        // NOT the same as SUBTYPE/TRANSFORM { positionOffset, etc. }, which can
        // reposition model geometry and remains unsupported. Node changes and
        // unknown subtype fields/child nodes are blocked.
        private static readonly HashSet<string> SubtypeFields = Set(
            "name", "title", "description", "tankType", "addedMass", "addedCost",
            "volumeAdded", "volumeMultiplier", "volumeAddedToParent", "percentFilled",
            "resourcesTweakable", "defaultSubtypePriority", "upgradeRequired",
            "techRequired", "primaryColor", "secondaryColor", "infoDetail",
            "crashTolerance", "maxTemp", "skinMaxTemp", "skinMassPerArea",
            "skinInternalConductionMult", "costMultiplier", "massMultiplier",
            "transform");

        private static readonly HashSet<string> ResourceFields = Set(
            "name", "unitsPerVolume", "percentFilled", "resourcesTweakable",
            "amount", "maxAmount");

        // B9 SUBTYPE/TEXTURE changes *materials*, not meshes. The optional
        // transform/baseTransform fields identify which renderers get the new
        // texture; unlike a SUBTYPE/TRANSFORM block they do not move, rotate,
        // scale, hide, or replace those transforms. Reject other fields so a
        // future B9 geometry-changing extension is not silently accepted.
        private static readonly HashSet<string> TextureFields = Set(
            "texture", "currentTexture", "isNormalMap", "shaderProperty",
            "transform", "baseTransform");

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

        // A malformed texture block cannot be counted as a verified safe
        // subtype. This also keeps the conservative all-subtypes policy when
        // another mod rewrites B9 configurations through ModuleManager.
        private static bool AllowsTextureNode(ConfigNode textureNode,
            string location, out string reason)
        {
            if (!AllowedValues(textureNode, TextureFields, location, out reason))
                return false;
            if (textureNode.nodes.Count != 0)
            {
                reason = location + " contains unsupported nested nodes";
                return false;
            }
            if (string.IsNullOrWhiteSpace(textureNode.GetValue("texture")))
            {
                reason = location + " is missing a texture path";
                return false;
            }
            reason = "";
            return true;
        }

        // B9 permits multiple transform = ... declarations on a SUBTYPE, plus
        // exact names, wildcards and regex patterns. Validate only that each
        // declaration is nonempty; B9 owns matching/activation semantics. Do
        // not confuse these with TEXTURE.transform renderer selectors or the
        // unsupported nested TRANSFORM geometry-modifier node.
        private static bool AllowsSubtypeTransformToggles(ConfigNode subtype,
            string location, out string reason)
        {
            foreach (ConfigNode.Value entry in subtype.values)
            {
                if (!string.Equals(entry.name, "transform",
                    StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrWhiteSpace(entry.value))
                {
                    reason = location + " contains an empty transform selector";
                    return false;
                }
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
                if (!AllowsSubtypeTransformToggles(child, where, out reason)) return false;
                foreach (ConfigNode nested in child.nodes)
                {
                    if (string.Equals(nested.name, "RESOURCE",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        if (!AllowedValues(nested, ResourceFields,
                            where + " / RESOURCE", out reason)) return false;
                        if (nested.nodes.Count != 0)
                        {
                            reason = where + " / RESOURCE contains nested nodes";
                            return false;
                        }
                        continue;
                    }
                    if (string.Equals(nested.name, "TEXTURE",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        if (!AllowsTextureNode(nested, where + " / TEXTURE",
                            out reason)) return false;
                        continue;
                    }
                    // TRANSFORM position/rotation/scale modifiers, MATERIAL,
                    // NODE and MODULE nodes can invalidate morph assumptions.
                    // SUBTYPE transform = ... toggles are handled above.
                    reason = where + " contains geometry/unknown node '" +
                        nested.name + "'";
                    return false;
                }
            }
            if (count == 0)
            {
                reason = label + " has no verified morph-safe SUBTYPE nodes";
                return false;
            }
            reason = "";
            return true;
        }
    }
}
