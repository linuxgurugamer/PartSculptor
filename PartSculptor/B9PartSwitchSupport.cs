// Optional B9PartSwitch integration. No B9 DLL reference is required.
// baseVolume is updated from an immutable native baseline. B9 then owns the
// resources created from that volume; PartSculptor must not rescale them again.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

using static PartSculptor.RegisterToolbar;

namespace PartSculptor
{
    internal sealed class B9PartSwitchSupport
    {
        private sealed class VolumeBinding
        {
            internal PartModule Module;
            internal MemberInfo BaseVolumeMember;
            internal MethodInfo UpdateVolumeMethod;
            internal MethodInfo IsManagedResourceMethod;
            internal MemberInfo ManagesResourcesMember;
            internal string Key;
            internal double Native;
        }

        // B9 child switchers may add (or subtract) subtype-specific tank volume
        // from another B9 switcher via parentID. Each subtype needs a native,
        // unscaled baseline, independent of which subtype is selected.
        private sealed class ParentVolumeBinding
        {
            internal PartModule Child;
            internal object Subtype;
            internal MemberInfo ValueMember;
            internal string ParentId;
            internal string Key;
            internal double Native;
        }

        readonly List<PartModule> switchers = new List<PartModule>();
        readonly List<VolumeBinding> volumes = new List<VolumeBinding>();
        readonly List<ParentVolumeBinding> parentVolumes = new List<ParentVolumeBinding>();
        bool queryingMass;
        bool queryingCost;

        private static bool IsB9(PartModule module)
        {
            return module != null &&
                (string.Equals(module.moduleName, "ModuleB9PartSwitch", StringComparison.Ordinal) ||
                 string.Equals(module.GetType().Name, "ModuleB9PartSwitch", StringComparison.Ordinal));
        }

        private static double Number(object target, string name, double fallback = 0d)
        {
            if (target == null) return fallback;
            try
            {
                MemberInfo member = ExponentBinding.FindMember(target.GetType(), name);
                if (member == null) return fallback;
                object value = ExponentBinding.Read(member, target);
                return value == null ? fallback : Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch { return fallback; }
        }

        private static bool FiniteNonNegative(double value)
        {
            return value >= 0d && !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool FiniteSigned(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) &&
                value >= -float.MaxValue && value <= float.MaxValue;
        }

        // The same native signed adjustment must be reused for every morph,
        // stretch, subtype switch, craft reload and undo. Never use an already
        // scaled runtime value as the source for the next adjustment.
        internal static double ScaleParentVolume(double native, double volumeFactor)
        {
            return ScaleMath.B9ParentVolume(native, volumeFactor);
        }

        internal static string ParentVolumeKey(int index, string moduleId,
            string subtypeName)
        {
            return ResourceKey(VolumeKey(index, moduleId), subtypeName);
        }

        private static IEnumerable<object> Subtypes(PartModule module)
        {
            if (module == null) yield break;
            MemberInfo member = ExponentBinding.FindMember(module.GetType(), "subtypes");
            if (member == null) yield break;
            IEnumerable entries = null;
            try { entries = ExponentBinding.Read(member, module) as IEnumerable; }
            catch { }
            if (entries == null) yield break;
            foreach (object entry in entries)
                if (entry != null) yield return entry;
        }

        private static string SubtypeName(object subtype)
        {
            if (subtype == null) return "";
            try
            {
                // Actual B9 PartSubtype exposes Name and subtypeName.
                MemberInfo name = ExponentBinding.FindMember(subtype.GetType(), "Name") ??
                    ExponentBinding.FindMember(subtype.GetType(), "subtypeName");
                object value = name != null ? ExponentBinding.Read(name, subtype) : null;
                return value != null ? Convert.ToString(value, CultureInfo.InvariantCulture) : "";
            }
            catch { return ""; }
        }

        private static bool NativeSubtypeVolume(Part part, int switchIndex,
            int subtypeIndex, string subtypeName, out double native)
        {
            native = 0d;
            ConfigNode config = part != null && part.partInfo != null ?
                part.partInfo.partConfig : null;
            if (config == null) return false;
            int index = 0;
            foreach (ConfigNode moduleNode in config.GetNodes("MODULE"))
            {
                if (!string.Equals(moduleNode.GetValue("name"), "ModuleB9PartSwitch",
                    StringComparison.OrdinalIgnoreCase)) continue;
                if (index++ != switchIndex) continue;
                int subtypeOrdinal = 0;
                foreach (ConfigNode subtypeNode in moduleNode.GetNodes("SUBTYPE"))
                {
                    if (subtypeOrdinal++ != subtypeIndex) continue;
                    if (!string.Equals(subtypeNode.GetValue("name") ?? "", subtypeName,
                        StringComparison.Ordinal)) return false;
                    string source = subtypeNode.GetValue("volumeAddedToParent");
                    if (string.IsNullOrEmpty(source)) { native = 0d; return true; }
                    return double.TryParse(source, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out native) && FiniteSigned(native);
                }
                return false;
            }
            return false;
        }

        private void CaptureParentVolumes(Part part,
            IDictionary<string, double> saved, double currentStretchVolume,
            IList<PartModule> prefabSwitchers)
        {
            for (int i = 0; i < switchers.Count; ++i)
            {
                PartModule module = switchers[i];
                string parentId = Value(module, "parentID");
                if (string.IsNullOrEmpty(parentId)) continue;
                // B9's source has a public `subtypes` List<PartSubtype> and a
                // public float PartSubtype.volumeAddedToParent. Reflect them to
                // avoid a hard B9PartSwitch assembly dependency.
                var prefabSubtypes = new List<object>();
                if (i < prefabSwitchers.Count)
                    prefabSubtypes.AddRange(Subtypes(prefabSwitchers[i]));
                int ordinal = 0;
                foreach (object subtype in Subtypes(module))
                {
                    string name = SubtypeName(subtype);
                    if (string.IsNullOrEmpty(name)) { ++ordinal; continue; }
                    MemberInfo field = ExponentBinding.FindMember(subtype.GetType(),
                        "volumeAddedToParent");
                    if (field == null) { ++ordinal; continue; }
                    double live;
                    try { live = Convert.ToDouble(ExponentBinding.Read(field, subtype),
                        CultureInfo.InvariantCulture); }
                    catch { ++ordinal; continue; }
                    if (!FiniteSigned(live)) { ++ordinal; continue; }
                    string key = ParentVolumeKey(i, Value(module, "moduleID"), name);
                    double native;
                    if (saved != null && saved.TryGetValue(key, out native) &&
                        FiniteSigned(native)) { }
                    else if (NativeSubtypeVolume(part, i, ordinal, name, out native)) { }
                    else if (ordinal < prefabSubtypes.Count &&
                        string.Equals(SubtypeName(prefabSubtypes[ordinal]), name,
                            StringComparison.Ordinal) &&
                        FiniteSigned(Number(prefabSubtypes[ordinal],
                            "volumeAddedToParent", double.NaN)))
                        native = Number(prefabSubtypes[ordinal], "volumeAddedToParent");
                    else
                        native = live / (ScaleMath.PositiveFinite(currentStretchVolume) ?
                            currentStretchVolume : 1d);
                    if (FiniteSigned(native))
                    {
                        if (saved != null) saved[key] = native;
                        parentVolumes.Add(new ParentVolumeBinding {
                            Child = module, Subtype = subtype, ValueMember = field,
                            ParentId = parentId, Key = key, Native = native
                        });
                    }
                    ++ordinal;
                }
            }
        }


        private static double CurrentVolume(VolumeBinding binding)
        {
            try
            {
                object value = ExponentBinding.Read(binding.BaseVolumeMember, binding.Module);
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch { return double.NaN; }
        }

        // The index is the ordinal among B9 switchers (not among all modules).
        // This remains stable across subtype switches and craft reloads.
        internal static string VolumeKey(int index, string moduleId)
        {
            moduleId = moduleId ?? "";
            return index.ToString(CultureInfo.InvariantCulture) + ":" +
                moduleId.Length.ToString(CultureInfo.InvariantCulture) + ":" + moduleId;
        }

        private B9PartSwitchSupport(Part part, IDictionary<string, double> saved,
            IDictionary<string, double> savedParentVolumes, double currentStretchVolume)
        {
            if (part == null || part.Modules == null) return;
            foreach (PartModule module in part.Modules)
                if (IsB9(module)) switchers.Add(module);

            var prefabSwitchers = new List<PartModule>();
            Part prefab = part.partInfo != null ? part.partInfo.partPrefab : null;
            if (prefab != null && prefab.Modules != null)
                foreach (PartModule module in prefab.Modules)
                    if (IsB9(module)) prefabSwitchers.Add(module);

            CaptureParentVolumes(part, savedParentVolumes, currentStretchVolume,
                prefabSwitchers);
            for (int i = 0; i < switchers.Count; ++i)
            {
                PartModule module = switchers[i];
                MemberInfo member = ExponentBinding.FindMember(module.GetType(), "baseVolume");
                if (member == null) continue;
                MethodInfo update = module.GetType().GetMethod("UpdateVolume",
                    BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                string key = VolumeKey(i, Value(module, "moduleID"));
                double native;
                double live;
                try { live = Convert.ToDouble(ExponentBinding.Read(member, module),
                    CultureInfo.InvariantCulture); }
                catch { continue; }
                if (!FiniteNonNegative(live)) continue;

                if (saved != null && saved.TryGetValue(key, out native) &&
                    FiniteNonNegative(native))
                {
                    // Saved native volume wins over a changed prefab / scaled live field.
                }
                else if (i < prefabSwitchers.Count &&
                    FiniteNonNegative(Number(prefabSwitchers[i], "baseVolume", double.NaN)))
                {
                    native = Number(prefabSwitchers[i], "baseVolume");
                }
                else
                {
                    // Legacy craft without snapshots or an available prefab.
                    native = live / (ScaleMath.PositiveFinite(currentStretchVolume) ?
                        currentStretchVolume : 1d);
                }
                if (!FiniteNonNegative(native)) continue;
                if (saved != null) saved[key] = native;
                volumes.Add(new VolumeBinding {
                    Module = module, BaseVolumeMember = member, UpdateVolumeMethod = update,
                    IsManagedResourceMethod = module.GetType().GetMethod("IsManagedResource",
                        BindingFlags.Public | BindingFlags.Instance,
                        null, new[] { typeof(string) }, null),
                    ManagesResourcesMember = ExponentBinding.FindMember(module.GetType(), "ManagesResources"),
                    Key = key, Native = native
                });
            }
        }

        internal static B9PartSwitchSupport Create(Part part,
            IDictionary<string, double> savedVolumes,
            IDictionary<string, double> savedParentVolumes, double currentStretchVolume)
        {
            B9PartSwitchSupport result = new B9PartSwitchSupport(part, savedVolumes,
                savedParentVolumes, currentStretchVolume);
            return result.switchers.Count != 0 ? result : null;
        }

        internal string Signature
        {
            get
            {
                var entries = new List<string>(switchers.Count);
                for (int i = 0; i < switchers.Count; ++i)
                {
                    PartModule module = switchers[i];
                    string id = Value(module, "moduleID");
                    string subtype = SelectedSubtype(module);
                    entries.Add(i.ToString(CultureInfo.InvariantCulture) + ":" +
                        (id ?? "").Length + ":" + id + ":" +
                        (subtype ?? "").Length + ":" + subtype);
                }
                return string.Join("|", entries.ToArray());
            }
        }

        internal string SelectedSubtypes
        {
            get
            {
                var names = new List<string>();
                foreach (PartModule module in switchers)
                {
                    string id = Value(module, "moduleID");
                    names.Add((string.IsNullOrEmpty(id) ? "B9" : id) + ": " +
                        SelectedSubtype(module));
                }
                return string.Join(", ", names.ToArray());
            }
        }

        // B9's serialized NodeData field is a property named
        // CurrentSubtypeName, not a member named currentSubtype. The latter
        // is only a config-node alias. Use the property for change detection.
        private static string SelectedSubtype(PartModule module)
        {
            string subtype = Value(module, "CurrentSubtypeName");
            if (string.IsNullOrEmpty(subtype)) subtype = Value(module, "currentSubtype");
            if (string.IsNullOrEmpty(subtype)) subtype = Value(module, "currentSubtypeIndex");
            return subtype;
        }

        private static string Value(PartModule module, string key)
        {
            try
            {
                MemberInfo member = ExponentBinding.FindMember(module.GetType(), key);
                if (member == null) return "";
                object value = ExponentBinding.Read(member, module);
                return value == null ? "" : Convert.ToString(value, CultureInfo.InvariantCulture);
            }
            catch { return ""; }
        }

        internal static string ResourceKey(string signature, string name)
        {
            return (signature ?? "") + "\u001f" + (name ?? "");
        }

        internal static bool SplitResourceKey(string key, out string signature, out string name)
        {
            signature = name = null;
            if (string.IsNullOrEmpty(key)) return false;
            int index = key.LastIndexOf('\u001f');
            if (index < 0) return false;
            signature = key.Substring(0, index);
            name = key.Substring(index + 1);
            return !string.IsNullOrEmpty(signature) && !string.IsNullOrEmpty(name);
        }

        // A B9 parent tank can have baseVolume=0 and still have tank volume
        // contributed by a child switcher's volumeAddedToParent. It is B9-
        // managed in that case and must not be scaled a second time.
        private bool HasChildVolume(PartModule parent)
        {
            string id = Value(parent, "moduleID");
            if (string.IsNullOrEmpty(id)) return false;
            foreach (ParentVolumeBinding child in parentVolumes)
                if (string.Equals(child.ParentId, id, StringComparison.Ordinal) &&
                    Math.Abs(child.Native) > 1e-9d) return true;
            return false;
        }

        // Only skip PartSculptor's separate PartResource scaling if B9 can
        // actually update resources from a managed positive baseVolume or
        // a subtype contribution from a child switcher.
        internal bool OwnsScaledResource(string resourceName)
        {
            foreach (VolumeBinding binding in volumes)
            {
                if ((binding.Native <= 0d && !HasChildVolume(binding.Module)) ||
                    binding.UpdateVolumeMethod == null ||
                    binding.IsManagedResourceMethod == null ||
                    binding.ManagesResourcesMember == null) continue;
                try
                {
                    object enabled = ExponentBinding.Read(binding.ManagesResourcesMember,
                        binding.Module);
                    if (!(enabled is bool) || !(bool)enabled) continue;
                    object managed = binding.IsManagedResourceMethod.Invoke(binding.Module,
                        new object[] { resourceName });
                    if (managed is bool && (bool)managed) return true;
                }
                catch (Exception e)
                {
                    Log.Warning("[PartSculptor] B9 resource ownership check failed: " +
                        e.Message);
                }
            }
            return false;
        }

        // Invoked before PartSculptor's normal resource pass. On a B9 tank,
        // UpdateVolume changes capacities and fills tanks in the editor, so
        // preserve current fill fractions across an ordinary resize. Never
        // invoke UpdateVolume in flight: B9 zeros resources in that path.
        internal void ApplyBaseVolumes(Part part, double stretchVolume, bool editor)
        {
            if (!ScaleMath.PositiveFinite(stretchVolume)) return;
            var refresh = new HashSet<PartModule>();
            var changedParents = new HashSet<PartModule>();

            // Update ALL child subtypes from native signed values, not merely
            // the currently selected subtype. B9 reads the active child's
            // volumeAddedToParent when the parent calculates its tank volume.
            // Scale negative contributions as well as positive ones.
            foreach (ParentVolumeBinding child in parentVolumes)
            {
                double next = ScaleParentVolume(child.Native, stretchVolume);
                if (!FiniteSigned(next)) continue;
                double previous;
                try { previous = Convert.ToDouble(ExponentBinding.Read(
                    child.ValueMember, child.Subtype), CultureInfo.InvariantCulture); }
                catch { continue; }
                if (Math.Abs(next - previous) <=
                    Math.Max(1e-5d, Math.Abs(next) * 1e-6d)) continue;
                if (!ExponentBinding.Write(child.ValueMember, child.Subtype, (float)next))
                    continue;
                ScaleDebug.Change(part, "B9", child.Key + ".volumeAddedToParent",
                    previous, next, "native=" + ScaleDebug.Format(child.Native) +
                    "; stretch/morph volume=" + ScaleDebug.Format(stretchVolume));
                foreach (PartModule candidate in switchers)
                {
                    if (string.Equals(Value(candidate, "moduleID"), child.ParentId,
                        StringComparison.Ordinal))
                    {
                        changedParents.Add(candidate);
                        break;
                    }
                }
            }

            foreach (VolumeBinding binding in volumes)
            {
                double next = ScaleMath.B9BaseVolume(binding.Native, stretchVolume);
                if (!FiniteNonNegative(next) || next > float.MaxValue) continue;
                double previous = CurrentVolume(binding);
                if (!FiniteNonNegative(previous) ||
                    Math.Abs(next - previous) <= Math.Max(1e-5d, next * 1e-6d)) continue;
                if (!ExponentBinding.Write(binding.BaseVolumeMember, binding.Module,
                        (float)next)) continue;
                ScaleDebug.Change(part, "B9", binding.Key + ".baseVolume", previous,
                    next, "native=" + ScaleDebug.Format(binding.Native) +
                    "; stretch volume=" + ScaleDebug.Format(stretchVolume));
                refresh.Add(binding.Module);
            }

            // In B9 2.20, `VolumeFromChildren` is cached on the parent and is
            // normally recomputed by its private UpdateVolumeFromChildren().
            // Refresh only that cache here, INCLUDING IN FLIGHT. Never call
            // B9.UpdateVolume in flight: it can reset fuel amounts to zero.
            foreach (PartModule parent in changedParents)
            {
                try
                {
                    MethodInfo recompute = parent.GetType().GetMethod(
                        "UpdateVolumeFromChildren", BindingFlags.NonPublic |
                        BindingFlags.Instance, null, Type.EmptyTypes, null);
                    if (recompute != null) recompute.Invoke(parent, null);
                }
                catch (Exception e)
                {
                    Log.Warning("[PartSculptor] B9 parent volume cache refresh failed: " +
                        e.Message);
                }
                refresh.Add(parent);
            }

            if (!editor || refresh.Count == 0) return;
            // A parent can have baseVolume AND child contributions change in
            // one morph gesture: refresh each B9 switcher only once. Preserve
            // editor tank fill fractions across B9.UpdateVolume calls.
            var fillFractions = new Dictionary<string, double>(StringComparer.Ordinal);
            if (part != null && part.Resources != null)
                foreach (PartResource resource in part.Resources)
                    if (resource != null && resource.maxAmount > 0d &&
                        OwnsScaledResource(resource.resourceName))
                        fillFractions[resource.resourceName] = Math.Max(0d,
                            Math.Min(1d, resource.amount / resource.maxAmount));

            foreach (VolumeBinding binding in volumes)
            {
                if (!refresh.Remove(binding.Module) || binding.UpdateVolumeMethod == null)
                    continue;
                try { binding.UpdateVolumeMethod.Invoke(binding.Module, null); }
                catch (Exception e)
                {
                    Log.Warning("[PartSculptor] B9 UpdateVolume failed on " +
                        binding.Key + ": " + e.Message);
                }
            }
            if (part == null || part.Resources == null) return;
            foreach (PartResource resource in part.Resources)
            {
                double fraction;
                if (resource != null && fillFractions.TryGetValue(resource.resourceName,
                    out fraction))
                    resource.amount = Math.Min(resource.maxAmount,
                        resource.maxAmount * fraction);
            }
        }

        // OnStart/Start and subtype initialization in B9 can recreate its
        // PartResources after PartSculptor has already applied the stretched
        // baseVolume.  Its flight UpdateVolume() must NOT be called here:
        // ResourceModifier.UpdateVolumeFlight() sets fill amounts to zero.
        // Instead read the currently selected tank's authoritative total
        // volume and per-resource unitsPerVolume, and repair only maxAmount.
        // This is also safe after a resource subtype switch: do not resurrect
        // resources that the active subtype does not contain.
        internal bool ReconcileManagedCapacities(Part part)
        {
            if (part == null || part.Resources == null) return false;
            bool changed = false;
            foreach (VolumeBinding binding in volumes)
            {
                if ((binding.Native <= 0d && !HasChildVolume(binding.Module)) ||
                    binding.IsManagedResourceMethod == null ||
                    binding.ManagesResourcesMember == null) continue;
                try
                {
                    object manages = ExponentBinding.Read(binding.ManagesResourcesMember,
                        binding.Module);
                    if (!(manages is bool) || !(bool)manages) continue;

                    Type moduleType = binding.Module.GetType();
                    MemberInfo subtypeMember = ExponentBinding.FindMember(moduleType,
                        "CurrentSubtype");
                    MemberInfo tankMember = ExponentBinding.FindMember(moduleType,
                        "CurrentTankType");
                    if (subtypeMember == null || tankMember == null) continue;
                    object subtype = ExponentBinding.Read(subtypeMember, binding.Module);
                    IEnumerable tankResources = ExponentBinding.Read(tankMember,
                        binding.Module) as IEnumerable;
                    if (subtype == null || tankResources == null) continue;

                    // B9's GetTotalVolume accounts for subtype volumeMultiplier,
                    // volumeAdded, child volume and TweakScale's own VolumeScale.
                    // Multiplying just baseVolume would be wrong for those tanks.
                    MethodInfo volumeMethod = moduleType.GetMethod("GetTotalVolume",
                        BindingFlags.Public | BindingFlags.Instance, null,
                        new[] { subtype.GetType() }, null);
                    if (volumeMethod == null) continue;
                    double volume = Convert.ToDouble(volumeMethod.Invoke(binding.Module,
                        new[] { subtype }), CultureInfo.InvariantCulture);
                    if (!FiniteNonNegative(volume)) continue;

                    foreach (object tankResource in tankResources)
                    {
                        if (tankResource == null) continue;
                        MemberInfo resourceNameMember = ExponentBinding.FindMember(
                            tankResource.GetType(), "ResourceName");
                        if (resourceNameMember == null) continue;
                        string name = Convert.ToString(ExponentBinding.Read(
                            resourceNameMember, tankResource), CultureInfo.InvariantCulture);
                        if (string.IsNullOrEmpty(name)) continue;
                        double unitsPerVolume = Number(tankResource, "unitsPerVolume",
                            double.NaN);
                        double expected = ScaleMath.B9ManagedCapacity(volume,
                            unitsPerVolume);
                        if (!FiniteNonNegative(expected)) continue;

                        // B9 owns this resource. Do not add absent resources or
                        // interfere with resources managed by another switcher.
                        object managed = binding.IsManagedResourceMethod.Invoke(
                            binding.Module, new object[] { name });
                        if (!(managed is bool) || !(bool)managed) continue;
                        foreach (PartResource resource in part.Resources)
                        {
                            if (resource == null || !string.Equals(resource.resourceName,
                                name, StringComparison.Ordinal)) continue;
                            if (Math.Abs(resource.maxAmount - expected) <=
                                Math.Max(1e-5d, expected * 1e-6d)) break;
                            double oldMaximum = resource.maxAmount;
                            resource.maxAmount = expected;
                            // Preserve the current amount: never refill a tank
                            // or zero the contents in flight. Clamp only if a
                            // capacity reduction would otherwise overfill it.
                            if (resource.amount > expected) resource.amount = expected;
                            ScaleDebug.Change(part, "B9 resource",
                                name + ".maxAmount", oldMaximum, expected,
                                "active subtype capacity; no refill");
                            changed = true;
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Warning("[PartSculptor] B9 capacity reconciliation failed on " +
                        binding.Key + ": " + e.Message);
                }
            }
            return changed;
        }

        // Native contributions from the currently selected child subtypes.
        // B9 exposes a *scaled* VolumeFromChildren cache, so it must never be
        // used for BOTH sides of the native-to-current mass/cost ratio.
        private double NativeVolumeFromChildren(PartModule parent)
        {
            string id = Value(parent, "moduleID");
            if (string.IsNullOrEmpty(id)) return 0d;
            double total = 0d;
            foreach (ParentVolumeBinding child in parentVolumes)
            {
                if (!string.Equals(child.ParentId, id, StringComparison.Ordinal))
                    continue;
                MemberInfo subtypeMember = ExponentBinding.FindMember(
                    child.Child.GetType(), "CurrentSubtype");
                object selected = null;
                try { if (subtypeMember != null)
                    selected = ExponentBinding.Read(subtypeMember, child.Child); }
                catch { }
                if (object.ReferenceEquals(selected, child.Subtype))
                    total += child.Native;
            }
            return total;
        }

        // B9 already scales its volume-derived mass/cost after baseVolume is
        // written. Correct only the difference between that live contribution
        // and a prefab-anchored exponent target (including child subtype
        // volumeAddedToParent contributions). Never multiply its live total twice.
        private double ExtraForBinding(VolumeBinding binding, double liveModifier,
            double exponentFactor, bool cost)
        {
            if (binding.Native <= 0d && !HasChildVolume(binding.Module))
                return liveModifier * (exponentFactor - 1d);
            object subtype = null;
            try
            {
                MemberInfo member = ExponentBinding.FindMember(binding.Module.GetType(),
                    "CurrentSubtype");
                subtype = member == null ? null : ExponentBinding.Read(member, binding.Module);
            }
            catch { /* No additional subtype information. */ }
            if (subtype == null) return liveModifier * (exponentFactor - 1d);

            double multiplier = Number(subtype, "volumeMultiplier", 1d);
            double added = Number(subtype, "volumeAdded");
            double liveChildren = Number(binding.Module, "VolumeFromChildren");
            double nativeChildren = NativeVolumeFromChildren(binding.Module);
            double volumeScale = Number(binding.Module, "VolumeScale", 1d);
            double fixedModifier = Number(subtype, cost ? "addedCost" : "addedMass") * volumeScale;
            double liveBase = CurrentVolume(binding);
            double currentTotal = liveBase * multiplier + added + liveChildren;
            double nativeTotal = binding.Native * multiplier + added + nativeChildren;
            if (!ScaleMath.PositiveFinite(currentTotal) ||
                !FiniteNonNegative(nativeTotal))
                return liveModifier * (exponentFactor - 1d);
            return ScaleMath.B9ModifierExtra(liveModifier, fixedModifier,
                nativeTotal / currentTotal, exponentFactor);
        }

        internal double ExtraMass(float nativeMass, ModifierStagingSituation situation,
            double exponentFactor)
        {
            if (queryingMass) return 0d;
            queryingMass = true;
            try
            {
                double total = 0d;
                foreach (PartModule module in switchers)
                {
                    if (!module.isEnabled) continue;
                    IPartMassModifier modifier = module as IPartMassModifier;
                    if (modifier == null) continue;
                    try
                    {
                        double live = modifier.GetModuleMass(nativeMass, situation);
                        VolumeBinding binding = volumes.Find(v => v.Module == module);
                        total += binding == null ? live * (exponentFactor - 1d) :
                            ExtraForBinding(binding, live, exponentFactor, false);
                    }
                    catch (Exception e)
                    { Log.Warning("[PartSculptor] B9 mass delta failed: " + e.Message); }
                }
                return total;
            }
            finally { queryingMass = false; }
        }

        internal double ExtraCost(float nativeCost, ModifierStagingSituation situation,
            double exponentFactor)
        {
            if (queryingCost) return 0d;
            queryingCost = true;
            try
            {
                double total = 0d;
                foreach (PartModule module in switchers)
                {
                    if (!module.isEnabled) continue;
                    IPartCostModifier modifier = module as IPartCostModifier;
                    if (modifier == null) continue;
                    try
                    {
                        double live = modifier.GetModuleCost(nativeCost, situation);
                        VolumeBinding binding = volumes.Find(v => v.Module == module);
                        total += binding == null ? live * (exponentFactor - 1d) :
                            ExtraForBinding(binding, live, exponentFactor, true);
                    }
                    catch (Exception e)
                    { Log.Warning("[PartSculptor] B9 cost delta failed: " + e.Message); }
                }
                return total;
            }
            finally { queryingCost = false; }
        }

        internal float GetMassModifier(float nativeMass, ModifierStagingSituation situation)
        {
            if (queryingMass) return 0f;
            queryingMass = true;
            try
            {
                float total = 0f;
                foreach (PartModule module in switchers)
                {
                    if (!module.isEnabled) continue;
                    IPartMassModifier modifier = module as IPartMassModifier;
                    if (modifier == null) continue;
                    try { total += modifier.GetModuleMass(nativeMass, situation); }
                    catch (Exception e)
                    { Log.Warning("[PartSculptor] B9 mass modifier failed: " + e.Message); }
                }
                return total;
            }
            finally { queryingMass = false; }
        }

        internal float GetCostModifier(float nativeCost, ModifierStagingSituation situation)
        {
            if (queryingCost) return 0f;
            queryingCost = true;
            try
            {
                float total = 0f;
                foreach (PartModule module in switchers)
                {
                    if (!module.isEnabled) continue;
                    IPartCostModifier modifier = module as IPartCostModifier;
                    if (modifier == null) continue;
                    try { total += modifier.GetModuleCost(nativeCost, situation); }
                    catch (Exception e)
                    { Log.Warning("[PartSculptor] B9 cost modifier failed: " + e.Message); }
                }
                return total;
            }
            finally { queryingCost = false; }
        }

        internal void CollectVolumeValues(List<ScaledValue> values)
        {
            foreach (VolumeBinding binding in volumes)
                values.Add(new ScaledValue("B9PartSwitch", binding.Key + " baseVolume",
                    binding.Native, CurrentVolume(binding), "B9 tank volume units"));
            foreach (ParentVolumeBinding binding in parentVolumes)
            {
                double current;
                try { current = Convert.ToDouble(ExponentBinding.Read(
                    binding.ValueMember, binding.Subtype), CultureInfo.InvariantCulture); }
                catch { continue; }
                values.Add(new ScaledValue("B9PartSwitch",
                    binding.Key + " volumeAddedToParent", binding.Native, current,
                    "B9 child subtype contribution to parent tank (signed)"));
            }
        }
    }
}
