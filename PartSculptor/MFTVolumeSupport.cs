// Optional Modular Fuel Tanks / RealFuels integration. No hard DLL reference.
// MFT owns tank volume and allocations; scaling PartResource alone leaves its
// own free/used volume unchanged. RealFuels 15.16.1 uses ChangeTotalVolume;
// older MFT variants may use ChangeVolume or an equivalent message receiver.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

using static PartSculptor.RegisterToolbar;

namespace PartSculptor
{
    internal sealed class MFTVolumeSupport
    {
        sealed class VolumeBinding
        {
            internal string Id;
            internal PartModule Module;
            internal MemberInfo Volume;
            internal MethodInfo ChangeVolume;
            internal MethodInfo ChangeTotalVolume;
            internal MemberInfo TotalVolume;
            internal MemberInfo Utilization;
            internal double Native;
            internal bool Warned;
        }

        readonly List<VolumeBinding> bindings = new List<VolumeBinding>();

        static double ReadVolume(PartModule module, MemberInfo member)
        {
            if (module == null || member == null) return double.NaN;
            try
            {
                object value = ExponentBinding.Read(member, module);
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch { return double.NaN; }
        }

        static bool IsMFT(PartModule module)
        {
            if (module == null) return false;
            if (string.Equals(module.moduleName, "ModuleFuelTanks",
                StringComparison.OrdinalIgnoreCase)) return true;
            // RealFuels and MFT-derived modules need not use the concrete
            // class name ModuleFuelTanks. Look at the inheritance chain too.
            for (Type type = module.GetType(); type != null && type != typeof(object);
                type = type.BaseType)
                if (string.Equals(type.Name, "ModuleFuelTanks",
                    StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // RealFuels 15.16.1 (Source/Tanks/ModuleFuelTanks.cs) no longer has
        // a ChangeVolume receiver. Its supported API is:
        // public void ChangeTotalVolume(double newTotalVolume, bool propagate = false)
        // This updates totalVolume, computes usable volume with utilization,
        // and resizes FuelTank allocations. Find it without a DLL dependency.
        static MethodInfo FindChangeTotalVolume(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (Type current = type; current != null && current != typeof(object);
                current = current.BaseType)
                foreach (MethodInfo method in current.GetMethods(flags))
                {
                    if (method.Name != "ChangeTotalVolume") continue;
                    ParameterInfo[] args = method.GetParameters();
                    if (args.Length < 1 || args.Length > 2) continue;
                    if (args[0].ParameterType != typeof(double) &&
                        args[0].ParameterType != typeof(float)) continue;
                    if (args.Length == 2 && args[1].ParameterType != typeof(bool))
                        continue;
                    return method;
                }
            return null;
        }

        static bool ApplyTotalVolume(VolumeBinding binding, double wanted,
            double live)
        {
            if (binding.ChangeTotalVolume == null) return false;
            double total = ReadVolume(binding.Module, binding.TotalVolume);
            double utilization = ReadVolume(binding.Module, binding.Utilization);
            double targetTotal = ScaleMath.MFTTotalVolumeTarget(wanted, live,
                total, utilization);
            if (!ScaleMath.PositiveFinite(targetTotal)) return false;
            ParameterInfo[] parameters = binding.ChangeTotalVolume.GetParameters();
            object newTotal = Convert.ChangeType(targetTotal,
                parameters[0].ParameterType, CultureInfo.InvariantCulture);
            object[] args = parameters.Length == 2 ?
                new object[] { newTotal, false } : new object[] { newTotal };
            binding.ChangeTotalVolume.Invoke(binding.Module, args);
            return FuelSwitchMath.Close(
                ReadVolume(binding.Module, binding.Volume), wanted);
        }

        static MethodInfo FindChangeVolume(Type type)
        {
            // Some RealFuels versions inherit a PRIVATE ChangeVolume handler
            // from a base class. GetMethods on the derived class does not
            // return private methods from its ancestors, even with NonPublic.
            // Search each level explicitly, preferring double over float.
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            // The most-derived receiver takes precedence over a base-class
            // overload; within each level prefer double, then float.
            for (Type current = type; current != null && current != typeof(object);
                current = current.BaseType)
                for (int preference = 0; preference < 2; ++preference)
                    foreach (MethodInfo method in current.GetMethods(flags))
                    {
                        if (method.Name != "ChangeVolume") continue;
                        ParameterInfo[] args = method.GetParameters();
                        if (args.Length != 1) continue;
                        Type target = preference == 0 ? typeof(double) : typeof(float);
                        if (args[0].ParameterType == target) return method;
                    }
            return null;
        }

        // Unity SendMessage is the legacy volume-change entrypoint used by
        // procedural tank integrations. This can reach a receiver when a
        // RealFuels variant doesn't expose a reflection-callable method.
        // It is a fallback only; never send a second change when the first
        // callback already updated the module's reported volume.
        static bool SendLegacyVolumeChange(VolumeBinding binding, double wanted)
        {
            try
            {
                binding.Module.SendMessage("ChangeVolume", (float)wanted,
                    SendMessageOptions.DontRequireReceiver);
                if (FuelSwitchMath.Close(ReadVolume(binding.Module, binding.Volume), wanted))
                    return true;
            }
            catch (Exception) { /* Try the double receiver used by some forks. */ }
            try
            {
                binding.Module.SendMessage("ChangeVolume", wanted,
                    SendMessageOptions.DontRequireReceiver);
                return FuelSwitchMath.Close(ReadVolume(binding.Module, binding.Volume), wanted);
            }
            catch (Exception) { return false; }
        }

        private MFTVolumeSupport(Part part, IDictionary<string, double> saved,
            double currentTotalVolume)
        {
            Part prefab = part.partInfo != null ? part.partInfo.partPrefab : null;
            var prefabs = new List<PartModule>();
            if (prefab != null && prefab.Modules != null)
                foreach (PartModule module in prefab.Modules)
                    if (IsMFT(module)) prefabs.Add(module);

            int ordinal = 0;
            foreach (PartModule module in part.Modules)
            {
                if (!IsMFT(module)) continue;
                string key = ordinal.ToString(CultureInfo.InvariantCulture);
                MemberInfo member = ExponentBinding.FindMember(module.GetType(), "volume");
                double native;
                if (!saved.TryGetValue(key, out native) || !ScaleMath.PositiveFinite(native))
                {
                    native = ordinal < prefabs.Count ? ReadVolume(prefabs[ordinal],
                        ExponentBinding.FindMember(prefabs[ordinal].GetType(), "volume")) :
                        double.NaN;
                    if (!ScaleMath.PositiveFinite(native))
                    {
                        double live = ReadVolume(module, member);
                        native = ScaleMath.PositiveFinite(currentTotalVolume) ?
                            live / currentTotalVolume : live;
                    }
                }
                if (ScaleMath.PositiveFinite(native)) saved[key] = native;
                bindings.Add(new VolumeBinding {
                    Id = key, Module = module, Volume = member,
                    ChangeVolume = FindChangeVolume(module.GetType()),
                    ChangeTotalVolume = FindChangeTotalVolume(module.GetType()),
                    TotalVolume = ExponentBinding.FindMember(module.GetType(), "totalVolume"),
                    Utilization = ExponentBinding.FindMember(module.GetType(), "utilization"),
                    Native = native
                });
                ++ordinal;
            }
        }

        internal static MFTVolumeSupport Create(Part part,
            IDictionary<string, double> saved, double currentTotalVolume)
        {
            if (part == null || part.Modules == null) return null;
            var support = new MFTVolumeSupport(part, saved, currentTotalVolume);
            return support.bindings.Count > 0 ? support : null;
        }

        internal bool Apply(Part part, double stretchVolume, double tweakVolume,
            bool editor)
        {
            if (part == null) return false;
            bool changed = false;
            foreach (VolumeBinding binding in bindings)
            {
                if (binding.Module == null || !binding.Module.isEnabled) continue;
                double wanted = ScaleMath.MFTManagedVolume(binding.Native,
                    stretchVolume, tweakVolume);
                if (!ScaleMath.PositiveFinite(wanted)) continue;
                double live = ReadVolume(binding.Module, binding.Volume);
                if (ScaleMath.PositiveFinite(live) && FuelSwitchMath.Close(live, wanted))
                    continue;
                if (binding.Volume == null)
                {
                    if (!binding.Warned)
                    {
                        Log.Warning("[PartSculptor] Cannot read ModuleFuelTanks.volume on " +
                            binding.Module.GetType().FullName + "; unable to resize tank.");
                        binding.Warned = true;
                    }
                    continue;
                }
                // Keep fuel state rather than allowing an external volume
                // update to refill in flight. In the editor preserve fill %.
                var amounts = new Dictionary<string, double>(StringComparer.Ordinal);
                var fractions = new Dictionary<string, double>(StringComparer.Ordinal);
                foreach (PartResource resource in part.Resources)
                {
                    if (resource == null) continue;
                    amounts[resource.resourceName] = resource.amount;
                    if (resource.maxAmount > 0d)
                        fractions[resource.resourceName] = resource.amount / resource.maxAmount;
                }
                try
                {
                    Exception callbackError = null;
                    bool applied = false;
                    // RealFuels 15.16.1's supported entrypoint. ChangeVolume()
                    // was removed; importantly the input here is TOTAL physical
                    // volume, not the utilization-adjusted usable volume.
                    if (binding.ChangeTotalVolume != null)
                    {
                        try { applied = ApplyTotalVolume(binding, wanted, live); }
                        catch (Exception e) { callbackError = e.InnerException ?? e; }
                    }
                    // Older MFT forks may still expose ChangeVolume instead.
                    if (!applied && binding.ChangeVolume != null)
                    {
                        try
                        {
                            Type arg = binding.ChangeVolume.GetParameters()[0].ParameterType;
                            object value = Convert.ChangeType(wanted, arg,
                                CultureInfo.InvariantCulture);
                            binding.ChangeVolume.Invoke(binding.Module, new object[] { value });
                            applied = FuelSwitchMath.Close(
                                ReadVolume(binding.Module, binding.Volume), wanted);
                        }
                        catch (Exception e) { callbackError = e.InnerException ?? e; }
                    }
                    if (!applied)
                        applied = SendLegacyVolumeChange(binding, wanted);
                    if (!applied)
                    {
                        if (!binding.Warned)
                        {
                            Log.Warning("[PartSculptor] ModuleFuelTanks volume update " +
                                "did not take effect on " + binding.Module.GetType().FullName +
                                " (assembly " + binding.Module.GetType().Assembly.GetName().Name +
                                "): requested " + wanted.ToString("R", CultureInfo.InvariantCulture) +
                                ", current " + ReadVolume(binding.Module, binding.Volume)
                                    .ToString("R", CultureInfo.InvariantCulture) +
                                (callbackError != null ? "; error: " + callbackError.Message : "") +
                                ". RealFuels 15.16.1 requires ChangeTotalVolume(totalVolume, false); check utilization and module state.");
                            binding.Warned = true;
                        }
                        continue;
                    }
                    binding.Warned = false;
                    changed = true;
                    ScaleDebug.Change(part, "MFT", "ModuleFuelTanks[" + binding.Id +
                        "].volume", live, ReadVolume(binding.Module, binding.Volume),
                        "native=" + ScaleDebug.Format(binding.Native) +
                        "; stretch=" + ScaleDebug.Format(stretchVolume));
                }
                catch (Exception e)
                {
                    if (!binding.Warned)
                    {
                        Log.Warning("[PartSculptor] MFT/RealFuels volume update failed: " +
                            (e.InnerException ?? e).Message);
                        binding.Warned = true;
                    }
                }
                finally
                {
                    // A failed/unsupported RealFuels callback may still touch
                    // fuel amounts. Always restore them, even on failure.
                    foreach (PartResource resource in part.Resources)
                    {
                        if (resource == null) continue;
                        double previous;
                        if (!amounts.TryGetValue(resource.resourceName, out previous)) continue;
                        double fraction;
                        double updated = editor && fractions.TryGetValue(resource.resourceName,
                            out fraction) ? fraction * resource.maxAmount : previous;
                        resource.amount = Math.Max(0d, Math.Min(resource.maxAmount, updated));
                    }
                }
            }
            if (changed && part.PartActionWindow != null)
                part.PartActionWindow.displayDirty = true;
            return changed;
        }

        internal void CollectVolumeValues(List<ScaledValue> values)
        {
            foreach (VolumeBinding binding in bindings)
                values.Add(new ScaledValue("ModularFuelTanks",
                    "Tank[" + binding.Id + "] volume", binding.Native,
                    ReadVolume(binding.Module, binding.Volume), "MFT volume units"));
        }
    }
}
