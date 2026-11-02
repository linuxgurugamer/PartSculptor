// Data-driven scaling presets. Shipped and custom preset files live in
// PluginData and are loaded directly because KSP GameDatabase skips this folder.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace PartSculptor
{
    internal static class ScalePresetRegistry
    {
        static readonly List<ScalePresetDefinition> builtIns = new List<ScalePresetDefinition>();
        static readonly List<ScalePresetDefinition> custom = new List<ScalePresetDefinition>();
        static bool loaded;

        static string BuiltInPath
        {
            get { return Path.Combine(KSPUtil.ApplicationRootPath,
                "GameData/PartSculptor/PluginData/Presets.cfg"); }
        }

        static string CustomPath
        {
            get { return Path.Combine(KSPUtil.ApplicationRootPath,
                "GameData/PartSculptor/PluginData/CustomPresets.cfg"); }
        }

        internal static void EnsureLoaded()
        {
            if (!loaded) Reload();
        }

        internal static void Reload()
        {
            builtIns.Clear();
            custom.Clear();
            loaded = true;
            try
            {
                if (File.Exists(BuiltInPath))
                {
                    ConfigNode root = ConfigNode.Load(BuiltInPath);
                    if (root != null)
                    {
                        if (root.name == "PARTSCULPTOR_PRESETS")
                            ReadDefinitions(root, builtIns, true);
                        else
                            foreach (ConfigNode n in root.GetNodes("PARTSCULPTOR_PRESETS"))
                                ReadDefinitions(n, builtIns, true);
                    }
                }
                else Debug.LogWarning("[PartSculptor] Default presets not found: " + BuiltInPath);
                string legacyCustom = Path.Combine(KSPUtil.ApplicationRootPath,
                    "GameData/StretchScale/PluginData/CustomPresets.cfg");
                string customLoadPath = File.Exists(CustomPath) ? CustomPath : legacyCustom;
                if (File.Exists(customLoadPath))
                {
                    ConfigNode root = ConfigNode.Load(customLoadPath);
                    if (root != null)
                    {
                        // ConfigNode.Load may return a named root or a root
                        // containing STRETCHSCALE_CUSTOM_PRESETS as its child.
                        if (root.name == "PARTSCULPTOR_CUSTOM_PRESETS" ||
                            root.name == "STRETCHSCALE_CUSTOM_PRESETS")
                            ReadDefinitions(root, custom, false);
                        else
                        {
                            foreach (ConfigNode n in root.GetNodes("PARTSCULPTOR_CUSTOM_PRESETS"))
                                ReadDefinitions(n, custom, false);
                            foreach (ConfigNode n in root.GetNodes("STRETCHSCALE_CUSTOM_PRESETS"))
                                ReadDefinitions(n, custom, false);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[PartSculptor] Failed to load scale presets: " + e);
            }
            Debug.Log("[PartSculptor] Loaded " + builtIns.Count + " default and " +
                custom.Count + " custom scale presets");
        }

        static void ReadDefinitions(ConfigNode root, List<ScalePresetDefinition> destination, bool isDefault)
        {
            if (root == null) return;
            foreach (ConfigNode node in root.GetNodes("PRESET"))
            {
                string name = node.GetValue("name");
                bool relative = string.Equals(node.GetValue("mode"), "relative",
                    StringComparison.OrdinalIgnoreCase);
                double x, y, z;
                if (!double.TryParse(node.GetValue("x"), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out x) ||
                    !double.TryParse(node.GetValue("y"), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out y) ||
                    !double.TryParse(node.GetValue("z"), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out z))
                {
                    Debug.LogWarning("[PartSculptor] Invalid preset axes: " + name);
                    continue;
                }
                var item = new ScalePresetDefinition(name != null ? name.Trim() : "",
                    relative, x, y, z, isDefault);
                if (!ScalePresetMath.Valid(item) || Contains(destination, item.Name, item.Relative) ||
                    (isDefault ? Contains(custom, item.Name, item.Relative) :
                        Contains(builtIns, item.Name, item.Relative)))
                {
                    Debug.LogWarning("[PartSculptor] Invalid or duplicate preset: " + name);
                    continue;
                }
                destination.Add(item);
            }
        }

        static bool SameIdentity(ScalePresetDefinition p, string name, bool relative)
        {
            return p.Relative == relative &&
                string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase);
        }

        static bool Contains(List<ScalePresetDefinition> list, string name, bool relative)
        {
            foreach (ScalePresetDefinition p in list)
                if (SameIdentity(p, name, relative)) return true;
            return false;
        }

        // Preset identity includes mode: 'Double Y' can be defined as both an
        // absolute preset and a relative preset without collisions.
        internal static string Key(string name, bool relative)
        {
            return (relative ? "relative:" : "absolute:") + (name ?? "");
        }

        internal static string Key(ScalePresetDefinition preset)
        {
            return preset == null ? null : Key(preset.Name, preset.Relative);
        }

        internal static ScalePresetDefinition FindByKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (key.StartsWith("absolute:", StringComparison.OrdinalIgnoreCase))
                return Find(key.Substring("absolute:".Length), false);
            if (key.StartsWith("relative:", StringComparison.OrdinalIgnoreCase))
                return Find(key.Substring("relative:".Length), true);
            return null;
        }

        internal static ScalePresetDefinition Find(string name, bool relative)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(name)) return null;
            foreach (ScalePresetDefinition p in builtIns)
                if (SameIdentity(p, name, relative)) return p.Copy();
            foreach (ScalePresetDefinition p in custom)
                if (SameIdentity(p, name, relative)) return p.Copy();
            return null;
        }

        // Name-only calls originate from the PAW absolute-only selector or
        // older code; always prefer absolute presets for those callers.
        internal static ScalePresetDefinition Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Find(name, false) ?? Find(name, true);
        }

        internal static List<ScalePresetDefinition> All()
        {
            EnsureLoaded();
            var result = new List<ScalePresetDefinition>();
            foreach (ScalePresetDefinition p in builtIns) result.Add(p.Copy());
            foreach (ScalePresetDefinition p in custom) result.Add(p.Copy());
            return result;
        }

        // The PAW quick selector never presents relative presets, since applying
        // them repeatedly would compound a multiplier.
        internal static List<ScalePresetDefinition> AbsolutePresets()
        {
            var result = new List<ScalePresetDefinition>();
            foreach (ScalePresetDefinition preset in All())
                if (!preset.Relative) result.Add(preset);
            return result;
        }

        internal static string[] AbsoluteOptionNames()
        {
            List<ScalePresetDefinition> definitions = AbsolutePresets();
            var names = new List<string>();
            foreach (ScalePresetDefinition preset in definitions) names.Add(preset.Name);
            // UI_ChooseOption requires at least one valid option; the shipped
            // defaults normally guarantee that 1x uniform is available.
            if (names.Count == 0) names.Add("1x uniform");
            // A display-only state for manual edits and relative presets;
            // OnPresetChanged ignores it because it has no definition.
            names.Add("Custom");
            return names.ToArray();
        }

        internal static bool SaveCustom(ScalePresetDefinition item, string originalName, out string error)
        {
            return SaveCustom(item, originalName, item != null && item.Relative, out error);
        }

        internal static bool SaveCustom(ScalePresetDefinition item, string originalName,
            bool originalRelative, out string error)
        {
            EnsureLoaded();
            error = null;
            if (!ScalePresetMath.Valid(item))
            {
                error = "Enter a name (up to 64 characters) and three positive finite values.";
                return false;
            }
            item.Name = item.Name.Trim();
            foreach (ScalePresetDefinition p in builtIns)
                if (SameIdentity(p, item.Name, item.Relative))
                {
                    error = "Built-in names are reserved. Duplicate the preset under a new name.";
                    return false;
                }
            foreach (ScalePresetDefinition p in custom)
                if (SameIdentity(p, item.Name, item.Relative) &&
                    !(SameIdentity(p, originalName, originalRelative)))
                {
                    error = "A custom preset with this name already exists.";
                    return false;
                }
            var backup = new List<ScalePresetDefinition>(custom);
            if (!string.IsNullOrEmpty(originalName))
                custom.RemoveAll(p => SameIdentity(p, originalName, originalRelative));
            ScalePresetDefinition copy = item.Copy();
            copy.BuiltIn = false;
            custom.Add(copy);
            if (!Save(out error))
            {
                custom.Clear(); custom.AddRange(backup);
                return false;
            }
            ModuleStretchScale.RefreshPresetOptionsAll();
            return true;
        }

        internal static bool DeleteCustom(string name, out string error)
        {
            return DeleteCustom(name, false, out error);
        }

        internal static bool DeleteCustom(string name, bool relative, out string error)
        {
            EnsureLoaded();
            error = null;
            var backup = new List<ScalePresetDefinition>(custom);
            if (custom.RemoveAll(p => SameIdentity(p, name, relative)) == 0)
            {
                error = "Only custom presets can be deleted.";
                return false;
            }
            if (!Save(out error))
            {
                custom.Clear(); custom.AddRange(backup);
                return false;
            }
            ModuleStretchScale.RefreshPresetOptionsAll();
            return true;
        }

        static bool Save(out string error)
        {
            error = null;
            string tmp = null;
            try
            {
                // Save a parent/root and a named child, matching KSP's
                // ConfigNode.Save/Load convention for standalone cfg files.
                var root = new ConfigNode();
                ConfigNode presets = root.AddNode("PARTSCULPTOR_CUSTOM_PRESETS");
                foreach (ScalePresetDefinition p in custom)
                {
                    var node = presets.AddNode("PRESET");
                    node.AddValue("name", p.Name);
                    node.AddValue("mode", p.Relative ? "relative" : "absolute");
                    node.AddValue("x", p.X.ToString("R", CultureInfo.InvariantCulture));
                    node.AddValue("y", p.Y.ToString("R", CultureInfo.InvariantCulture));
                    node.AddValue("z", p.Z.ToString("R", CultureInfo.InvariantCulture));
                }
                string path = CustomPath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                tmp = path + ".tmp";
                root.Save(tmp);
                if (File.Exists(path))
                {
                    // Atomic replace when available; preserve the previous
                    // version if the save fails or KSP terminates mid-write.
                    File.Replace(tmp, path, null);
                }
                else File.Move(tmp, path);
                return true;
            }
            catch (Exception e)
            {
                error = "Unable to save custom presets: " + e.Message;
                Debug.LogError("[PartSculptor] " + error);
                return false;
            }
            finally
            {
                if (tmp != null && File.Exists(tmp))
                    try { File.Delete(tmp); } catch { }
            }
        }
    }
}
