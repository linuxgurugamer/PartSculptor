// B9PartSwitch may select from existing meshes using SUBTYPE transform values.
// PartSculptor can morph these static alternatives because FaceMorphSupport
// captures BOTH active and inactive model meshes, without changing B9's
// transform activation state. TEXTURE replaces renderer materials only.
// B9 NODE stack-attachment moves are supported using subtype-native baselines
// and are re-applied separately from the face deformation. Reject geometry
// repositioning TRANSFORM nodes and unverified features. Validate EVERY subtype.
using System;
using System.Collections.Generic;
using System.Globalization;
using static PartSculptor.RegisterToolbar;

namespace PartSculptor
{
    internal static class B9FaceMorphCompatibility
    {
        // Verified module fields are always allowed. Unknown scalar or vector
        // numeric values are rejected; unknown nonnumeric values are allowed.
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
        // reposition model geometry and remains unsupported. Unknown numeric
        // subtype values and unknown nested nodes are blocked.
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
        // scale, hide, or replace those transforms. Unknown numeric values
        // are rejected, while unknown nonnumeric values pass AllowedValues.
        // Only B9 attach-node position and size modifiers are validated here.
        // The separate SUBTYPE `node = ...` visibility toggles remain B9-owned
        // and are not the same as a nested NODE { ... } modifier.
        private static readonly HashSet<string> NodeFields = Set(
            "name", "position", "positionOffset", "size");

        private static readonly HashSet<string> TextureFields = Set(
            "texture", "currentTexture", "isNormalMap", "shaderProperty",
            "transform", "baseTransform");

        private static HashSet<string> Set(params string[] fields)
        {
            return new HashSet<string>(fields, StringComparer.OrdinalIgnoreCase);
        }

        // Unrecognized text and Boolean options cannot directly supply a numeric
        // transformation, so allow them. Reject unknown numeric scalar values and
        // comma-separated numeric vectors (such as position/scale coordinates).
        // Parse using the invariant culture used by KSP config files.
        private static bool IsNumericValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            double number;
            if (double.TryParse(value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out number)) return true;

            string[] components = value.Split(',');
            if (components.Length < 2) return false;
            foreach (string component in components)
                if (!double.TryParse(component.Trim(), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out number)) return false;
            return true;
        }

        private static bool AllowedValues(ConfigNode config, HashSet<string> fields,
            string location, out string reason)
        {
            foreach (ConfigNode.Value entry in config.values)
            {
                if (fields.Contains(entry.name) || !IsNumericValue(entry.value))
                    continue;
                reason = location + " has unsupported numeric field '" + entry.name + "'";
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

        private static bool AllowsNodeModifier(Part part, ConfigNode node,
            string location, out string reason)
        {
            if (!AllowedValues(node, NodeFields, location, out reason)) return false;
            if (node.nodes.Count != 0)
            {
                reason = location + " contains unsupported nested nodes";
                return false;
            }
            string selector = node.GetValue("name");
            if (string.IsNullOrWhiteSpace(selector))
            {
                reason = location + " is missing an attachment node name";
                return false;
            }
            if (!node.HasValue("position") && !node.HasValue("positionOffset") &&
                !node.HasValue("size"))
            {
                reason = location + " has no position/positionOffset/size modifier";
                return false;
            }
            UnityEngine.Vector3 vector;
            foreach (string field in new[] { "position", "positionOffset" })
                if (node.HasValue(field) &&
                    !B9PartSwitchSupport.TryReadNodeVector(node.GetValue(field), out vector))
                {
                    reason = location + " has an invalid " + field + " vector";
                    return false;
                }
            if (node.HasValue("size"))
            {
                int size;
                if (!int.TryParse(node.GetValue("size"),
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out size) || size < 0)
                {
                    reason = location + " has an invalid node size";
                    return false;
                }
            }
            Part prefab = part != null && part.partInfo != null ?
                part.partInfo.partPrefab : null;
            bool matches = false;
            if (prefab != null && prefab.attachNodes != null)
                foreach (AttachNode attachNode in prefab.attachNodes)
                    if (attachNode != null &&
                        B9PartSwitchSupport.NodeNameMatches(selector, attachNode.id))
                    { matches = true; break; }
            if (!matches)
            {
                reason = location + " has no matching original stack attachment node";
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
                    // SUBTYPE/MODULE blocks update existing part modules; they
                    // do not select or replace the static model meshes. Leave
                    // their contents to B9PartSwitch rather than rejecting
                    // the entire subtype for face morphing.
                    if (string.Equals(nested.name, "MODULE",
                        StringComparison.OrdinalIgnoreCase))
                        continue;
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
                    if (string.Equals(nested.name, "NODE",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        if (!AllowsNodeModifier(part, nested, where + " / NODE",
                            out reason)) return false;
                        continue;
                    }
                    // TRANSFORM position/rotation/scale modifiers, MATERIAL,
                    // and other unknown nodes still invalidate morph assumptions.
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
