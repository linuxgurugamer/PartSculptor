// Config-driven stretching of KSP part and PartModule properties.
// Reads the *processed* GameDatabase; no dependency on TweakScale is required.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace PartSculptor
{
    internal sealed class ExponentRule
    {
        internal readonly Dictionary<string, double> Fields =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        internal readonly Dictionary<string, ExponentRule> Children =
            new Dictionary<string, ExponentRule>(StringComparer.OrdinalIgnoreCase);
        internal readonly List<string> IgnoreModules = new List<string>();
        internal string Selector;

        internal ExponentRule Child(string name)
        {
            ExponentRule result;
            if (!Children.TryGetValue(name, out result))
                Children[name] = result = new ExponentRule();
            return result;
        }

        internal void Merge(ConfigNode source)
        {
            if (source == null) return;
            string selector = source.GetValue("name");
            if (!string.IsNullOrEmpty(selector)) Selector = selector;
            foreach (ConfigNode.Value value in source.values)
            {
                if (value.name == "name" || string.IsNullOrEmpty(value.name)) continue;
                if (value.name == "-ignore")
                {
                    foreach (string name in value.value.Split(','))
                    {
                        string trimmed = name.Trim();
                        if (trimmed.Length != 0 && !IgnoreModules.Contains(trimmed))
                            IgnoreModules.Add(trimmed);
                    }
                    continue;
                }
                double exponent;
                if (double.TryParse(value.value, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out exponent) &&
                    !double.IsNaN(exponent) && !double.IsInfinity(exponent))
                {
                    // !amount and !maxAmount are TweakScale resource-specific
                    // field selectors, NOT a ModuleManager deletion operation.
                    string field = value.name.TrimStart('!', '@', '%');
                    if (field.Length > 0) Fields[field] = exponent;
                }
            }
            foreach (ConfigNode child in source.GetNodes())
            {
                if (string.IsNullOrEmpty(child.name)) continue;
                Child(child.name).Merge(child);
            }
        }

        internal void Merge(ExponentRule source)
        {
            if (source == null) return;
            if (!string.IsNullOrEmpty(source.Selector)) Selector = source.Selector;
            foreach (KeyValuePair<string, double> v in source.Fields) Fields[v.Key] = v.Value;
            foreach (KeyValuePair<string, ExponentRule> v in source.Children)
                Child(v.Key).Merge(v.Value);
            foreach (string ignored in source.IgnoreModules)
                if (!IgnoreModules.Contains(ignored)) IgnoreModules.Add(ignored);
        }

        internal double Get(string key, double fallback)
        {
            double value;
            return Fields.TryGetValue(key, out value) ? value : fallback;
        }

        internal bool Matches(object instance)
        {
            if (string.IsNullOrEmpty(Selector) || Selector == "*") return true;
            if (instance == null) return false;
            MemberInfo nameMember = ExponentBinding.FindMember(instance.GetType(), "name") ??
                ExponentBinding.FindMember(instance.GetType(), "resourceName");
            if (nameMember == null) return false;
            object value = ExponentBinding.Read(nameMember, instance);
            return value != null && string.Equals(value.ToString(), Selector,
                StringComparison.OrdinalIgnoreCase);
        }

        internal bool Ignored(Part part)
        {
            if (part == null) return false;
            foreach (string ignored in IgnoreModules)
                foreach (PartModule module in part.Modules)
                    if (module != null && (string.Equals(module.moduleName, ignored,
                        StringComparison.OrdinalIgnoreCase) ||
                        IsNamedType(module.GetType(), ignored))) return true;
            return false;
        }

        internal static bool IsNamedType(Type type, string name)
        {
            for (Type current = type; current != null; current = current.BaseType)
                if (string.Equals(current.Name, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }

    // Resolved per-part scaling configuration. Parent behaviors are processed
    // first; later selected behaviors and the local ModulePartSculptor win.
    internal sealed class ResolvedScaleBehavior
    {
        internal readonly List<ConfigNode> TypeExponents = new List<ConfigNode>();
        internal readonly List<ConfigNode> BehaviorExponents = new List<ConfigNode>();
        internal readonly List<ConfigNode> LegacyExponents = new List<ConfigNode>();
        internal readonly Dictionary<string, string> Values =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        internal readonly List<string> Applied = new List<string>();
        internal bool FoundScaleType;

        internal string Get(string key)
        {
            string value;
            return Values.TryGetValue(key, out value) ? value : null;
        }
    }

    internal static class ExponentDatabase
    {
        static readonly Dictionary<string, ExponentRule> rules =
            new Dictionary<string, ExponentRule>(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string, ConfigNode> behaviors =
            new Dictionary<string, ConfigNode>(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string, ConfigNode> scaleTypes =
            new Dictionary<string, ConfigNode>(StringComparer.OrdinalIgnoreCase);
        static bool loaded;

        internal static void Load()
        {
            if (loaded || GameDatabase.Instance == null) return;
            loaded = true;
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("TWEAKSCALEEXPONENTS"))
            {
                string name = node.GetValue("name");
                if (string.IsNullOrEmpty(name)) continue;
                ExponentRule rule;
                if (!rules.TryGetValue(name, out rule)) rules[name] = rule = new ExponentRule();
                rule.Merge(node);
            }
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("TWEAKSCALEBEHAVIOR"))
            {
                string name = node.GetValue("name");
                if (!string.IsNullOrEmpty(name)) behaviors[name] = node;
            }
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("SCALETYPE"))
            {
                string name = node.GetValue("name");
                if (!string.IsNullOrEmpty(name)) scaleTypes[name] = node;
            }
            Debug.Log("[PartSculptor] Loaded " + rules.Count + " exponent rules, " +
                behaviors.Count + " behaviors and " + scaleTypes.Count + " scale types from GameData.");
        }

        internal static ExponentRule ForName(string name)
        {
            ExponentRule result = new ExponentRule();
            ExponentRule rule;
            if (name != null && rules.TryGetValue(name, out rule)) result.Merge(rule);
            return result;
        }

        // Resolve EVERY global TWEAKSCALEEXPONENTS definition whose name
        // matches this module. This does not require a behavior selection or
        // a TweakScale PartModule. Base classes (e.g. ModuleEngines) are
        // processed first, followed by more specific derived classes and any
        // registered KSP moduleName alias. More specific rules win per field.
        internal static List<string> ModuleRuleNames(PartModule module)
        {
            var names = new List<string>();
            if (module == null) return names;
            for (Type type = module.GetType(); type != null && type != typeof(object);
                type = type.BaseType)
                names.Insert(0, type.Name);
            if (!string.IsNullOrEmpty(module.moduleName) &&
                !names.Exists(n => string.Equals(n, module.moduleName,
                    StringComparison.OrdinalIgnoreCase)))
                names.Add(module.moduleName);
            return names;
        }

        internal static ExponentRule ForModule(PartModule module)
        {
            ExponentRule result = new ExponentRule();
            foreach (string name in ModuleRuleNames(module))
            {
                ExponentRule rule;
                if (rules.TryGetValue(name, out rule)) result.Merge(rule);
            }
            return result;
        }

        // Identify the specific ModulePartSculptor definition, rather than
        // confusing it with a sibling TweakScale module or another instance.
        internal static ConfigNode OwnModuleConfig(Part part, ModulePartSculptor owner)
        {
            ConfigNode partConfig = part != null && part.partInfo != null ?
                part.partInfo.partConfig : null;
            if (partConfig == null || owner == null) return null;
            int ordinal = 0;
            foreach (PartModule instance in part.Modules)
            {
                if (instance is ModulePartSculptor)
                {
                    if (object.ReferenceEquals(instance, owner)) break;
                    ++ordinal;
                }
            }
            foreach (ConfigNode node in partConfig.GetNodes("MODULE"))
            {
                string moduleName = node.GetValue("name");
                if (!string.Equals(moduleName, "ModulePartSculptor",
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(moduleName, "ModulePartSculptor",
                        StringComparison.OrdinalIgnoreCase)) continue;
                if (ordinal-- == 0) return node;
            }
            return null;
        }

        static IEnumerable<string> Names(string names)
        {
            if (string.IsNullOrEmpty(names)) yield break;
            foreach (string name in names.Split(','))
                if (name.Trim().Length != 0) yield return name.Trim();
        }

        static void ReadValues(ConfigNode node, IDictionary<string, string> values)
        {
            if (node == null) return;
            foreach (ConfigNode.Value value in node.values)
            {
                if (string.IsNullOrEmpty(value.name) || value.name == "name" ||
                    value.name.Equals("behavior", StringComparison.OrdinalIgnoreCase) ||
                    value.name.Equals("inheritBehavior", StringComparison.OrdinalIgnoreCase) ||
                    value.name.Equals("inherits", StringComparison.OrdinalIgnoreCase) ||
                    value.name.Equals("inherit", StringComparison.OrdinalIgnoreCase)) continue;
                values[value.name] = value.value;
            }
        }

        static void AddBehavior(string name, ResolvedScaleBehavior result,
            HashSet<string> visiting)
        {
            ConfigNode node;
            if (!behaviors.TryGetValue(name, out node))
            {
                Debug.LogWarning("[PartSculptor] Unknown TWEAKSCALEBEHAVIOR '" + name + "'.");
                return;
            }
            if (!visiting.Add(name))
            {
                Debug.LogWarning("[PartSculptor] Circular TWEAKSCALEBEHAVIOR inheritance at '" + name + "'.");
                return;
            }
            // A behavior may inherit from one or more other behavior names.
            // This is a PartSculptor extension: stock presets need no parents.
            foreach (string parent in Names(node.GetValue("inherits") ?? node.GetValue("inherit")))
                AddBehavior(parent, result, visiting);
            foreach (ConfigNode module in node.GetNodes("MODULE"))
            {
                string moduleName = module.GetValue("name");
                if (!string.Equals(moduleName, "TweakScale", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(moduleName, "ModulePartSculptor", StringComparison.OrdinalIgnoreCase))
                    continue;
                foreach (string parent in Names(module.GetValue("inherits") ?? module.GetValue("inherit")))
                    AddBehavior(parent, result, visiting);
                ReadValues(module, result.Values);
                foreach (ConfigNode exponent in module.GetNodes("TWEAKSCALEEXPONENTS"))
                    result.BehaviorExponents.Add(exponent);
            }
            foreach (ConfigNode exponent in node.GetNodes("TWEAKSCALEEXPONENTS"))
                result.BehaviorExponents.Add(exponent);
            result.Applied.Add(name);
            visiting.Remove(name);
        }

        static void AddScaleType(string name, IDictionary<string, string> values,
            List<ConfigNode> exponentNodes, HashSet<string> visiting)
        {
            ConfigNode node;
            if (!scaleTypes.TryGetValue(name, out node)) return;
            if (!visiting.Add(name))
            {
                Debug.LogWarning("[PartSculptor] Circular SCALETYPE inheritance at '" + name + "'.");
                return;
            }
            // In a SCALETYPE, 'type' names its parent SCALETYPE.
            string parent = node.GetValue("type");
            if (!string.IsNullOrEmpty(parent))
                AddScaleType(parent, values, exponentNodes, visiting);
            ReadValues(node, values);
            foreach (ConfigNode exponent in node.GetNodes("TWEAKSCALEEXPONENTS"))
                exponentNodes.Add(exponent);
            visiting.Remove(name);
        }

        internal static ResolvedScaleBehavior Resolve(Part part,
            ModulePartSculptor owner, string chosenBehavior, string inheritedBehavior)
        {
            Load();
            var resolved = new ResolvedScaleBehavior();
            ConfigNode own = OwnModuleConfig(part, owner);
            // A saved craft's explicit selection (KSPField) takes precedence
            // over the part prefab. A blank selection uses the prefab value.
            string selected = !string.IsNullOrWhiteSpace(chosenBehavior) ? chosenBehavior :
                own != null ? own.GetValue("behavior") : null;
            string inherited = !string.IsNullOrWhiteSpace(inheritedBehavior) ? inheritedBehavior :
                own != null ? own.GetValue("inheritBehavior") : null;
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in Names(inherited)) AddBehavior(name, resolved, visiting);
            foreach (string name in Names(selected)) AddBehavior(name, resolved, visiting);

            // Backwards compatibility with 0.0.4.4: if no behavior has been
            // explicitly selected on PartSculptor, copied behavior exponent
            // patches in a sibling TweakScale MODULE still work. Once the
            // author selects a PartSculptor behavior, sibling settings are
            // deliberately ignored to avoid ambiguous precedence.
            if (string.IsNullOrWhiteSpace(selected) && string.IsNullOrWhiteSpace(inherited))
            {
                ConfigNode partConfig = part != null && part.partInfo != null ?
                    part.partInfo.partConfig : null;
                if (partConfig != null)
                    foreach (ConfigNode other in partConfig.GetNodes("MODULE"))
                    {
                        if (!string.Equals(other.GetValue("name"), "TweakScale",
                            StringComparison.OrdinalIgnoreCase)) continue;
                        foreach (string name in Names(other.GetValue("behavior")))
                            AddBehavior(name, resolved, visiting);
                        foreach (ConfigNode exponent in other.GetNodes("TWEAKSCALEEXPONENTS"))
                            resolved.LegacyExponents.Add(exponent);
                    }
            }

            // Module-local values override the chosen behavior's values.
            ReadValues(own, resolved.Values);
            string typeName = resolved.Get("type");
            if (!string.IsNullOrEmpty(typeName))
            {
                ConfigNode typeNode;
                if (scaleTypes.TryGetValue(typeName, out typeNode))
                {
                    resolved.FoundScaleType = true;
                    var typeValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    AddScaleType(typeName, typeValues, resolved.TypeExponents,
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                    foreach (KeyValuePair<string, string> entry in typeValues)
                        if (!resolved.Values.ContainsKey(entry.Key))
                            resolved.Values[entry.Key] = entry.Value;
                }
            }
            return resolved;
        }
    }

    // A typed, cached binding. Each new scale is recomputed from an un-stretched
    // baseline, never from the previous stretched value (no cumulative drift).
    internal sealed class ExponentBinding
    {
        internal object Target;
        internal MemberInfo Member;
        internal double Exponent;
        internal double Native;
        internal Vector3 NativeVector;
        internal bool Vector;
        internal Keyframe[] NativeKeys;
        internal bool Curve;
        internal bool IsCrewCapacity;
        internal bool IsPartOffset;
        internal string Description;

        internal static object Read(MemberInfo member, object target)
        {
            FieldInfo field = member as FieldInfo;
            if (field != null) return field.GetValue(target);
            PropertyInfo property = member as PropertyInfo;
            return property != null ? property.GetValue(target, null) : null;
        }

        internal static bool Write(MemberInfo member, object target, object value)
        {
            try
            {
                FieldInfo field = member as FieldInfo;
                if (field != null)
                {
                    if (field.IsInitOnly || field.IsLiteral) return false;
                    field.SetValue(target, value);
                    return true;
                }
                PropertyInfo property = member as PropertyInfo;
                if (property == null || !property.CanWrite) return false;
                property.SetValue(target, value, null);
                return true;
            }
            catch { return false; }
        }

        internal static MemberInfo FindMember(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.IgnoreCase;
            for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                FieldInfo f = t.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (f != null) return f;
                PropertyInfo p = t.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (p != null && p.GetIndexParameters().Length == 0) return p;
            }
            return null;
        }

        internal static Type MemberType(MemberInfo member)
        {
            FieldInfo f = member as FieldInfo;
            if (f != null) return f.FieldType;
            PropertyInfo p = member as PropertyInfo;
            return p != null ? p.PropertyType : null;
        }

        internal static bool Number(object obj, out double value)
        {
            value = 0d;
            if (obj == null || obj is bool || obj is Enum) return false;
            TypeCode code = Type.GetTypeCode(obj.GetType());
            if (code != TypeCode.Byte && code != TypeCode.SByte &&
                code != TypeCode.Int16 && code != TypeCode.UInt16 &&
                code != TypeCode.Int32 && code != TypeCode.UInt32 &&
                code != TypeCode.Int64 && code != TypeCode.UInt64 &&
                code != TypeCode.Single && code != TypeCode.Double &&
                code != TypeCode.Decimal) return false;
            try { value = Convert.ToDouble(obj, CultureInfo.InvariantCulture); }
            catch { return false; }
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        internal static object ConvertNumber(double value, Type type)
        {
            // Integral fields (especially CrewCapacity / InventorySlots) must
            // round predictably rather than truncate or overflow.
            if (type == typeof(float)) return (float)value;
            if (type == typeof(double)) return value;
            if (type == typeof(int)) return (int)Math.Max(int.MinValue,
                Math.Min(int.MaxValue, Math.Round(value, MidpointRounding.AwayFromZero)));
            if (type == typeof(uint)) return (uint)Math.Max(0d,
                Math.Min(uint.MaxValue, Math.Round(value, MidpointRounding.AwayFromZero)));
            if (type == typeof(long)) return (long)Math.Max(long.MinValue,
                Math.Min(long.MaxValue, Math.Round(value, MidpointRounding.AwayFromZero)));
            if (type == typeof(short)) return (short)Math.Max(short.MinValue,
                Math.Min(short.MaxValue, Math.Round(value, MidpointRounding.AwayFromZero)));
            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        internal static bool ReadDouble(ConfigNode node, string key, out double value)
        {
            value = 0d;
            return node != null && double.TryParse(node.GetValue(key),
                NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                !double.IsNaN(value) && !double.IsInfinity(value);
        }

        // B9's SUBTYPE/MODULE/DATA node can replace module fields after
        // OnStartFinished. Only on an observed subtype change may the native
        // baseline be refreshed. The routine refuses to rebase an unchanged
        // value that is exactly what PartSculptor last wrote.
        internal bool RebaseExternalValue(float linear, Vector3 axes)
        {
            if (Target == null || Member == null) return false;
            double factor = Math.Pow(linear, Exponent);
            if (double.IsNaN(factor) || double.IsInfinity(factor)) return false;
            try
            {
                object live = Read(Member, Target);
                if (Vector && live is Vector3)
                {
                    Vector3 expected = IsPartOffset ? Vector3.Scale(NativeVector, axes) :
                        NativeVector * (float)factor;
                    Vector3 current = (Vector3)live;
                    if ((current - expected).sqrMagnitude < 1e-8f) return false;
                    NativeVector = current; // B9 has just loaded an unscaled native vector
                    return true;
                }
                if (Curve && live is FloatCurve && NativeKeys != null)
                {
                    FloatCurve curve = (FloatCurve)live;
                    if (curve.Curve == null) return false;
                    Keyframe[] current = curve.Curve.keys;
                    if (current.Length == NativeKeys.Length)
                    {
                        bool equal = true;
                        for (int i = 0; i < current.Length; ++i)
                        {
                            Keyframe k = current[i], n = NativeKeys[i];
                            if (Math.Abs(k.time - n.time) > 1e-5f ||
                                Math.Abs(k.value - n.value * factor) > 1e-4f ||
                                Math.Abs(k.inTangent - n.inTangent * factor) > 1e-4f ||
                                Math.Abs(k.outTangent - n.outTangent * factor) > 1e-4f)
                            { equal = false; break; }
                        }
                        if (equal) return false;
                    }
                    NativeKeys = (Keyframe[])current.Clone();
                    return true;
                }
                double actual;
                if (Number(live, out actual))
                {
                    double expected = Native * factor;
                    if (Math.Abs(actual - expected) <=
                        Math.Max(1e-5d, Math.Abs(expected) * 1e-5d)) return false;
                    Native = actual; // B9 replaced the module's native field
                    return true;
                }
            }
            catch { /* an optional third-party module may invalidate bindings */ }
            return false;
        }

        internal void SaveNative(ConfigNode root, string nodeName)
        {
            if (root == null || string.IsNullOrEmpty(Description) ||
                (Curve && NativeKeys == null)) return;
            ConfigNode record = root.AddNode(nodeName);
            record.AddValue("path", Description);
            if (Curve)
            {
                record.AddValue("kind", "curve");
                if (NativeKeys != null)
                    foreach (Keyframe key in NativeKeys)
                    {
                        ConfigNode k = record.AddNode("KEY");
                        k.AddValue("time", key.time.ToString("R", CultureInfo.InvariantCulture));
                        k.AddValue("value", key.value.ToString("R", CultureInfo.InvariantCulture));
                        k.AddValue("inTangent", key.inTangent.ToString("R", CultureInfo.InvariantCulture));
                        k.AddValue("outTangent", key.outTangent.ToString("R", CultureInfo.InvariantCulture));
                    }
            }
            else if (Vector)
            {
                record.AddValue("kind", "vector");
                record.AddValue("x", NativeVector.x.ToString("R", CultureInfo.InvariantCulture));
                record.AddValue("y", NativeVector.y.ToString("R", CultureInfo.InvariantCulture));
                record.AddValue("z", NativeVector.z.ToString("R", CultureInfo.InvariantCulture));
            }
            else
            {
                record.AddValue("kind", "number");
                record.AddValue("value", Native.ToString("R", CultureInfo.InvariantCulture));
            }
        }

        internal static bool ReadNativeVector(ConfigNode record, out Vector3 result)
        {
            result = Vector3.zero;
            double x, y, z;
            if (record == null || record.GetValue("kind") != "vector" ||
                !ReadDouble(record, "x", out x) ||
                !ReadDouble(record, "y", out y) ||
                !ReadDouble(record, "z", out z) ||
                Math.Abs(x) > float.MaxValue || Math.Abs(y) > float.MaxValue ||
                Math.Abs(z) > float.MaxValue) return false;
            result = new Vector3((float)x, (float)y, (float)z);
            return true;
        }

        internal static bool ReadNativeCurve(ConfigNode record, out Keyframe[] keys)
        {
            keys = null;
            if (record == null || record.GetValue("kind") != "curve") return false;
            var result = new List<Keyframe>();
            foreach (ConfigNode k in record.GetNodes("KEY"))
            {
                double t, v, i, o;
                if (!ReadDouble(k, "time", out t) || !ReadDouble(k, "value", out v) ||
                    !ReadDouble(k, "inTangent", out i) || !ReadDouble(k, "outTangent", out o) ||
                    Math.Abs(t) > float.MaxValue || Math.Abs(v) > float.MaxValue ||
                    Math.Abs(i) > float.MaxValue || Math.Abs(o) > float.MaxValue)
                    return false;
                result.Add(new Keyframe((float)t, (float)v, (float)i, (float)o));
            }
            keys = result.ToArray();
            return true;
        }

        internal void CollectScaledValues(List<ScaledValue> values)
        {
            if (Target == null || Member == null || values == null) return;
            try
            {
                object current = Read(Member, Target);
                string details = "exponent " + Exponent.ToString("0.###", CultureInfo.InvariantCulture);
                if (Vector)
                    values.Add(new ScaledValue("Module fields", Description,
                        NativeVector, current, details));
                else if (Curve)
                {
                    if (NativeKeys == null) return;
                    FloatCurve original = new FloatCurve();
                    original.Curve.keys = (Keyframe[])NativeKeys.Clone();
                    values.Add(new ScaledValue("Module curves", Description,
                        original, current, details));
                }
                else
                    values.Add(new ScaledValue("Module fields", Description,
                        Native, current, details));
            }
            catch { /* A third-party module may destroy or replace a target. */ }
        }

        internal void Apply(Part part, float stretchLinear, Vector3 axes, bool allowCrewChange)
        {
            if (Target == null || Member == null) return;
            double multiplier = Math.Pow(stretchLinear, Exponent);
            if (double.IsNaN(multiplier) || double.IsInfinity(multiplier)) return;
            try
            {
                if (Vector)
                {
                    Vector3 next = IsPartOffset ? Vector3.Scale(NativeVector, axes) :
                        NativeVector * (float)multiplier;
                    object vectorBefore = ScaleDebug.Enabled ? Read(Member, Target) : null;
                    if (Write(Member, Target, next) && ScaleDebug.Enabled)
                        ScaleDebug.Change(part, "exponent", Description, vectorBefore, next,
                            "native=" + ScaleDebug.Format(NativeVector) +
                            "; exponent=" + ScaleDebug.Format(Exponent) +
                            "; factor=" + ScaleDebug.Format(multiplier));
                    return;
                }
                if (Curve)
                {
                    if (NativeKeys == null) return;
                    FloatCurve duplicate = new FloatCurve();
                    Keyframe[] keys = new Keyframe[NativeKeys.Length];
                    for (int i = 0; i < keys.Length; ++i)
                    {
                        Keyframe k = NativeKeys[i];
                        keys[i] = new Keyframe(k.time, (float)(k.value * multiplier),
                            (float)(k.inTangent * multiplier), (float)(k.outTangent * multiplier));
                    }
                    duplicate.Curve.keys = keys;
                    object curveBefore = ScaleDebug.Enabled ? Read(Member, Target) : null;
                    if (Write(Member, Target, duplicate) && ScaleDebug.Enabled)
                        ScaleDebug.Change(part, "exponent", Description, curveBefore, duplicate,
                            "curve keys=" + NativeKeys.Length +
                            "; exponent=" + ScaleDebug.Format(Exponent) +
                            "; factor=" + ScaleDebug.Format(multiplier));
                    return;
                }
                if (IsCrewCapacity && !allowCrewChange) return;
                // Native is a fixed, unscaled baseline for the life of this
                // binding. Never derive it from the current (possibly already
                // scaled) value: that created geometric growth each time a
                // PAW edit or another module touched a field.
                double desired = Native * multiplier;
                if (double.IsNaN(desired) || double.IsInfinity(desired) ||
                    (MemberType(Member) == typeof(float) && Math.Abs(desired) > float.MaxValue)) return;
                object converted = ConvertNumber(desired, MemberType(Member));
                object scalarBefore = ScaleDebug.Enabled ? Read(Member, Target) : null;
                if (Write(Member, Target, converted) && ScaleDebug.Enabled)
                    ScaleDebug.Change(part, "exponent", Description, scalarBefore, converted,
                        "native=" + ScaleDebug.Format(Native) +
                        "; exponent=" + ScaleDebug.Format(Exponent) +
                        "; factor=" + ScaleDebug.Format(multiplier));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[PartSculptor] Could not scale " + Description + ": " + e.Message);
            }
        }
    }

    internal sealed class PartExponentScaler
    {
        readonly Part part;
        readonly List<ExponentBinding> bindings = new List<ExponentBinding>();
        readonly IDictionary<string, ConfigNode> savedBaselines;
        readonly Vector3 initialAxes;
        internal readonly double CrewExponent;
        internal readonly bool HasCrewRule;
        internal readonly double MassExponent;
        internal readonly double CostExponent;
        internal readonly double CapacityExponent;
        internal readonly double AmountExponent;
        internal readonly bool ScaleResources;
        internal readonly bool AllowGrow;
        internal readonly string ActiveBehaviors;
        internal float LastAppliedLinear = 1f;

        internal PartExponentScaler(Part part, ModulePartSculptor owner,
            float initialStretchLinear, float tweakFactor)
        {
            this.part = part;
            savedBaselines = owner.SavedExponentBaselines;
            initialAxes = new Vector3(owner.scaleX, owner.scaleY, owner.scaleZ);
            ExponentDatabase.Load();
            ConfigNode config = part.partInfo != null ? part.partInfo.partConfig : null;
            ExponentRule partRule = ExponentDatabase.ForName("Part");
            ExponentRule tweakRule = ExponentDatabase.ForName("TweakScale");
            var custom = new Dictionary<string, ExponentRule>(StringComparer.OrdinalIgnoreCase);

            ResolvedScaleBehavior selected = ExponentDatabase.Resolve(part, owner,
                owner.behavior, owner.inheritBehavior);
            ActiveBehaviors = string.Join(", ", selected.Applied.ToArray());
            bool canGrow;
            AllowGrow = !bool.TryParse(selected.Get("allowGrow"), out canGrow) || canGrow;

            // Type defaults (such as stock stack_square's mass = 2), then
            // inherited behaviors, then per-part overrides, then our own
            // ModulePartSculptor overrides. Sibling TweakScale inline rules
            // are read only as a compatibility fallback when no behavior is set.
            foreach (ConfigNode inline in selected.TypeExponents)
                MergeScaleTypeInline(inline, partRule, tweakRule, custom);
            if (!selected.FoundScaleType)
            {
                // When only the behavior definitions were supplied without
                // TweakScale's SCALETYPE file, retain the stock square semantics.
                string typeName = selected.Get("type");
                if (string.Equals(typeName, "stack_square", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(typeName, "free_square", StringComparison.OrdinalIgnoreCase))
                    partRule.Fields["mass"] = 2d;
            }
            foreach (ConfigNode inline in selected.BehaviorExponents)
                MergeInline(inline, partRule, tweakRule, custom);
            foreach (ConfigNode inline in selected.LegacyExponents)
                MergeInline(inline, partRule, tweakRule, custom);
            if (config != null)
                foreach (ConfigNode inline in config.GetNodes("TWEAKSCALEEXPONENTS"))
                    MergeInline(inline, partRule, tweakRule, custom);
            ConfigNode ownModule = ExponentDatabase.OwnModuleConfig(part, owner);
            if (ownModule != null)
                foreach (ConfigNode inline in ownModule.GetNodes("TWEAKSCALEEXPONENTS"))
                    MergeInline(inline, partRule, tweakRule, custom);
            if (selected.Applied.Count > 0)
                Debug.Log("[PartSculptor] " + part.name + " uses behavior(s): " + ActiveBehaviors +
                    "; scale type: " + (selected.Get("type") ?? "none") + ".");
            // MassScale and DryCost in the TweakScale config govern our dry
            // IPartMassModifier/IPartCostModifier, never the TweakScale module's
            // live fields (which would break its own bookkeeping).
            MassExponent = partRule.Get("mass", tweakRule.Get("MassScale", 3d));
            CostExponent = tweakRule.Get("DryCost", 3d);
            ExponentRule resource = partRule.Child("Resources");
            CapacityExponent = resource.Get("maxAmount", 3d);
            AmountExponent = resource.Get("amount", CapacityExponent);
            ScaleResources = !resource.Ignored(part);
            Part prefab = part.partInfo != null ? part.partInfo.partPrefab : null;
            // Crew capacity uses a dedicated IVA-aware handler, never a raw field binding.
            double crewExponent;
            HasCrewRule = partRule.Fields.TryGetValue("CrewCapacity", out crewExponent);
            CrewExponent = HasCrewRule ? crewExponent : 0d;
            BindProperties(part, prefab, partRule, "Part", initialStretchLinear, tweakFactor, true);
            var used = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (PartModule module in part.Modules)
            {
                // B9 owns its own baseVolume/subtype data. Applying a generic
                // TWEAKSCALEEXPONENTS rule to those live fields *as well as*
                // PartSculptor's resource and modifier bridge would scale a
                // fuel tank twice. Bind affected stock modules normally, but
                // never scale ModuleB9PartSwitch's internal bookkeeping.
                if (module == null || module is ModulePartSculptor ||
                    ExponentRule.IsNamedType(module.GetType(), "TweakScale") ||
                    string.Equals(module.moduleName, "ModuleB9PartSwitch",
                        StringComparison.Ordinal) ||
                    string.Equals(module.GetType().Name, "ModuleB9PartSwitch",
                        StringComparison.Ordinal)) continue;
                Type type = module.GetType();
                string name = type.Name;
                int ordinal;
                if (!used.TryGetValue(name, out ordinal)) ordinal = 0;
                used[name] = ordinal + 1;
                PartModule prefabModule = null;
                if (prefab != null)
                {
                    int n = 0;
                    foreach (PartModule other in prefab.Modules)
                        if (other != null && other.GetType() == type &&
                            n++ == ordinal) { prefabModule = other; break; }
                }
                // Global defaults ALWAYS apply to matching PartModules,
                // regardless of whether ModulePartSculptor.behavior is set.
                // Apply explicit part/behavior/ModulePartSculptor overrides
                // afterwards, in base-to-derived order. In 0.0.4.5 the
                // reverse walk allowed a base-class override to clobber a
                // more-specific override on some derived modules.
                ExponentRule rule = ExponentDatabase.ForModule(module);
                foreach (string ruleName in ExponentDatabase.ModuleRuleNames(module))
                {
                    ExponentRule extra;
                    if (custom.TryGetValue(ruleName, out extra)) rule.Merge(extra);
                }
                if (rule.Ignored(part)) continue;
                // A solar panel initializes its production resource in OnLoad,
                // so scaling only chargeRate in OnStartFinished isn't enough.
                // Update the cached ResourceHandler rate as well.
                if (ExponentRule.IsNamedType(type, "ModuleDeployableSolarPanel") &&
                    rule.Fields.ContainsKey("chargeRate") &&
                    !rule.Children.ContainsKey("outputResources"))
                    rule.Child("outputResources").Fields["rate"] = rule.Fields["chargeRate"];
                // Stable, unique path for multiple instances of the same module.
                BindProperties(module, prefabModule, rule, name + "[" + ordinal + "]",
                    initialStretchLinear, tweakFactor, false);
            }
            ScaleDebug.Info(part, "exponent", "resolved " + bindings.Count +
                " property bindings; behavior(s)=" +
                (string.IsNullOrEmpty(ActiveBehaviors) ? "none" : ActiveBehaviors) +
                "; mass=" + ScaleDebug.Format(MassExponent) +
                "; cost=" + ScaleDebug.Format(CostExponent) +
                "; capacity=" + ScaleDebug.Format(CapacityExponent) +
                "; amount=" + ScaleDebug.Format(AmountExponent) +
                "; resources enabled=" + ScaleResources);
        }

        // SCALETYPE exponents without a name apply to part mass (stock
        // stack_square/free_square); named entries retain their normal target.
        static void MergeScaleTypeInline(ConfigNode node, ExponentRule part,
            ExponentRule tweak, IDictionary<string, ExponentRule> custom)
        {
            if (!string.IsNullOrEmpty(node.GetValue("name")))
            {
                MergeInline(node, part, tweak, custom);
                return;
            }
            ExponentRule values = new ExponentRule();
            values.Merge(node);
            double mass;
            if (values.Fields.TryGetValue("mass", out mass))
            {
                part.Fields["mass"] = mass;
                values.Fields.Remove("mass");
            }
            tweak.Merge(values);
        }

        static void MergeInline(ConfigNode node, ExponentRule part, ExponentRule tweak,
            IDictionary<string, ExponentRule> custom)
        {
            string name = node.GetValue("name");
            if (string.IsNullOrEmpty(name)) name = "TweakScale";
            if (string.Equals(name, "Part", StringComparison.OrdinalIgnoreCase)) part.Merge(node);
            else if (string.Equals(name, "TweakScale", StringComparison.OrdinalIgnoreCase)) tweak.Merge(node);
            else
            {
                ExponentRule rule;
                if (!custom.TryGetValue(name, out rule)) custom[name] = rule = new ExponentRule();
                rule.Merge(node);
            }
        }

        void BindProperties(object target, object prefab, ExponentRule rule, string path,
            float oldStretch, float tweakFactor, bool partRoot)
        {
            if (target == null || rule.Ignored(part)) return;
            foreach (KeyValuePair<string, double> entry in rule.Fields)
            {
                string key = entry.Key;
                if (partRoot && (key.Equals("mass", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("cost", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("CrewCapacity", StringComparison.OrdinalIgnoreCase))) continue;
                MemberInfo member = ExponentBinding.FindMember(target.GetType(), key);
                if (member == null) continue;
                Type type = ExponentBinding.MemberType(member);
                object value, baseline;
                try
                {
                    value = ExponentBinding.Read(member, target);
                    baseline = prefab != null ? ExponentBinding.Read(member, prefab) : null;
                }
                catch { continue; }
                string bindingPath = path + "." + key;
                ConfigNode saved;
                if (!savedBaselines.TryGetValue(bindingPath, out saved)) saved = null;
                var binding = new ExponentBinding { Target = target, Member = member,
                    Exponent = entry.Value, Description = bindingPath,
                    IsCrewCapacity = partRoot && key.Equals("CrewCapacity", StringComparison.OrdinalIgnoreCase) };
                double currentNumber, originalNumber;
                if (ExponentBinding.Number(value, out currentNumber))
                {
                    // Prefer the unscaled baseline persisted with the craft.
                    // This also handles fields that are not KSP-persistent and
                    // are reinitialized from a module's prefab upon reload.
                    double persistedNative;
                    if (saved != null && saved.GetValue("kind") == "number" &&
                        ExponentBinding.ReadDouble(saved, "value", out persistedNative))
                        binding.Native = persistedNative;
                    else
                    {
                        double scale = Math.Pow(oldStretch, entry.Value);
                        binding.Native = currentNumber;
                        if (oldStretch != 1f && scale > 0d && !double.IsInfinity(scale))
                        {
                            if (ExponentBinding.Number(baseline, out originalNumber))
                            {
                                double nativePrefab = originalNumber * Math.Pow(tweakFactor, entry.Value);
                                if (Near(currentNumber, nativePrefab) ||
                                    Near(currentNumber, originalNumber))
                                    binding.Native = currentNumber; // KSP restored an unscaled field
                                else if (Near(currentNumber, nativePrefab * scale))
                                    binding.Native = nativePrefab; // legacy craft, already scaled
                                else binding.Native = currentNumber / scale; // unknown legacy source
                            }
                            else binding.Native = currentNumber / scale;
                        }
                    }
                    bindings.Add(binding);
                }
                else if (value is IEnumerable && !(value is string))
                {
                    // TweakScale shorthand: inputList = 3 (and outputList/
                    // reqList) scales Ratio in every ResourceRatio entry.
                    // Some custom converters use rate instead.
                    int index = 0;
                    foreach (object item in (IEnumerable)value)
                    {
                        if (item == null) { ++index; continue; }
                        var element = new ExponentRule();
                        if (ExponentBinding.FindMember(item.GetType(), "Ratio") != null)
                            element.Fields["Ratio"] = entry.Value;
                        else if (ExponentBinding.FindMember(item.GetType(), "rate") != null)
                            element.Fields["rate"] = entry.Value;
                        if (element.Fields.Count > 0)
                            BindProperties(item, null, element, path + "." + key +
                                "[" + index + "]", oldStretch, tweakFactor, false);
                        ++index;
                    }
                }
                else if (type == typeof(Vector3) && value is Vector3)
                {
                    binding.Vector = true;
                    binding.NativeVector = (Vector3)value;
                    binding.IsPartOffset = partRoot &&
                        (key.Equals("CoMOffset", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("CoPOffset", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("CoLOffset", StringComparison.OrdinalIgnoreCase));
                    Vector3 persistedVector;
                    if (ExponentBinding.ReadNativeVector(saved, out persistedVector))
                        binding.NativeVector = persistedVector;
                    else if (oldStretch != 1f)
                    {
                        Vector3 currentVector = (Vector3)value;
                        Vector3 nativePrefab = baseline is Vector3 ? (Vector3)baseline : Vector3.zero;
                        if (baseline is Vector3 &&
                            (Near((currentVector - nativePrefab).sqrMagnitude, 0d) ||
                             Near((currentVector - nativePrefab * tweakFactor).sqrMagnitude, 0d)))
                            binding.NativeVector = currentVector; // unscaled at load
                        else if (binding.IsPartOffset)
                            binding.NativeVector = baseline is Vector3 ?
                                (Vector3)baseline * tweakFactor :
                                new Vector3(currentVector.x / Math.Max(1e-8f, initialAxes.x),
                                    currentVector.y / Math.Max(1e-8f, initialAxes.y),
                                    currentVector.z / Math.Max(1e-8f, initialAxes.z));
                        else binding.NativeVector = currentVector / (float)Math.Pow(oldStretch, entry.Value);
                    }
                    bindings.Add(binding);
                }
                else if (value is FloatCurve)
                {
                    binding.Curve = true;
                    FloatCurve curve = (FloatCurve)value;
                    Keyframe[] persistedKeys;
                    if (ExponentBinding.ReadNativeCurve(saved, out persistedKeys))
                        binding.NativeKeys = persistedKeys;
                    else if (curve.Curve != null)
                    {
                        Keyframe[] keys = curve.Curve.keys;
                        if (oldStretch != 1f && baseline is FloatCurve &&
                            ((FloatCurve)baseline).Curve != null &&
                            CurvesMatch(keys, ((FloatCurve)baseline).Curve.keys))
                            binding.NativeKeys = keys; // KSP reset this to prefab values
                        else if (oldStretch != 1f)
                            binding.NativeKeys = UnscaleCurve(keys, Math.Pow(oldStretch, entry.Value));
                        else binding.NativeKeys = keys;
                    }
                    bindings.Add(binding);
                }
            }
            foreach (KeyValuePair<string, ExponentRule> child in rule.Children)
            {
                if (partRoot && child.Key.Equals("Resources", StringComparison.OrdinalIgnoreCase))
                    continue; // Managed by the existing tank-switcher-aware resource code.
                MemberInfo member = ExponentBinding.FindMember(target.GetType(), child.Key);
                object childTarget = target;
                object childPrefab = prefab;
                if (member == null && (child.Key.Equals("inputResources", StringComparison.OrdinalIgnoreCase) ||
                    child.Key.Equals("outputResources", StringComparison.OrdinalIgnoreCase)))
                {
                    // KSP resource handlers keep their lists under resHandler,
                    // although TweakScale names them as top-level fields.
                    MemberInfo handlerMember = ExponentBinding.FindMember(target.GetType(), "resHandler");
                    if (handlerMember != null)
                    {
                        childTarget = ExponentBinding.Read(handlerMember, target);
                        childPrefab = prefab != null ? ExponentBinding.Read(handlerMember, prefab) : null;
                        if (childTarget != null)
                            member = ExponentBinding.FindMember(childTarget.GetType(), child.Key);
                    }
                }
                if (member == null || childTarget == null) continue;
                object live, source;
                try
                {
                    live = ExponentBinding.Read(member, childTarget);
                    source = childPrefab != null ? ExponentBinding.Read(member, childPrefab) : null;
                }
                catch { continue; }
                if (live == null) continue;
                if (live is IEnumerable && !(live is string))
                {
                    IList sourceList = source as IList;
                    int i = 0;
                    foreach (object item in (IEnumerable)live)
                    {
                        object native = sourceList != null && i < sourceList.Count ? sourceList[i] : null;
                        if (item != null && child.Value.Matches(item)) BindProperties(item, native, child.Value,
                            path + "." + child.Key + "[" + i + "]", oldStretch, tweakFactor, false);
                        ++i;
                    }
                }
                else BindProperties(live, source, child.Value,
                    path + "." + child.Key, oldStretch, tweakFactor, false);
            }
        }

        static bool CurvesMatch(Keyframe[] a, Keyframe[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; ++i)
                if (!Near(a[i].time, b[i].time) || !Near(a[i].value, b[i].value) ||
                    !Near(a[i].inTangent, b[i].inTangent) ||
                    !Near(a[i].outTangent, b[i].outTangent)) return false;
            return true;
        }

        static Keyframe[] UnscaleCurve(Keyframe[] keys, double factor)
        {
            if (factor == 0d || double.IsInfinity(factor) || double.IsNaN(factor)) return keys;
            var native = new Keyframe[keys.Length];
            for (int i = 0; i < keys.Length; ++i)
            {
                Keyframe k = keys[i];
                native[i] = new Keyframe(k.time, (float)(k.value / factor),
                    (float)(k.inTangent / factor), (float)(k.outTangent / factor));
            }
            return native;
        }

        internal void SaveBaselines(ConfigNode node, string nodeName)
        {
            if (node == null) return;
            foreach (ExponentBinding binding in bindings)
                binding.SaveNative(node, nodeName);
        }

        static bool Near(double a, double b)
        {
            return Math.Abs(a - b) <= Math.Max(1e-6d,
                Math.Max(Math.Abs(a), Math.Abs(b)) * 1e-4d);
        }

        internal void CollectScaledValues(List<ScaledValue> values)
        {
            foreach (ExponentBinding binding in bindings)
                binding.CollectScaledValues(values);
        }

        internal void RebaseExternallyChangedFields(float x, float y, float z)
        {
            float linear = (float)Math.Pow(Math.Max(1e-12d, (double)x * y * z), 1d / 3d);
            Vector3 axes = new Vector3(x, y, z);
            foreach (ExponentBinding binding in bindings)
                if (binding.RebaseExternalValue(linear, axes))
                    ScaleDebug.Info(part, "B9", "external subtype replaced " + binding.Description);
        }

        internal void Apply(float x, float y, float z)
        {
            float linear = (float)Math.Pow(Math.Max(1e-12d, (double)x * y * z), 1d / 3d);
            bool allowCrew = HighLogic.LoadedSceneIsEditor &&
                (part.protoModuleCrew == null || part.protoModuleCrew.Count == 0);
            Vector3 axes = new Vector3(x, y, z);
            foreach (ExponentBinding binding in bindings)
                binding.Apply(part, linear, axes, allowCrew);
            LastAppliedLinear = linear;
        }
    }
}
