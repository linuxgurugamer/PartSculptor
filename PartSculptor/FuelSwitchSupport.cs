// Optional compatibility with ModularFuelTanks, Firespitter and
// InterstellarFuelSwitch. Reflection and PartModule interfaces only: none of
// those DLLs is needed for PartSculptor to load or compile.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

using static PartSculptor.RegisterToolbar;

namespace PartSculptor
{
    internal sealed class FuelSwitchSupport
    {
        enum ProviderKind { ModularFuelTanks, Firespitter, InterstellarFuelSwitch }
        sealed class Provider
        {
            internal PartModule Module;
            internal ProviderKind Kind;
            internal int Index;
        }

        readonly List<Provider> providers = new List<Provider>();
        bool queryingMass;
        bool queryingCost;

        FuelSwitchSupport(Part part)
        {
            if (part == null || part.Modules == null) return;
            int index = 0;
            foreach (PartModule module in part.Modules)
            {
                if (module == null) { ++index; continue; }
                string name = module.moduleName ?? module.GetType().Name;
                ProviderKind kind;
                if (string.Equals(name, "ModuleFuelTanks", StringComparison.OrdinalIgnoreCase))
                    kind = ProviderKind.ModularFuelTanks;
                else if (string.Equals(name, "FSfuelSwitch", StringComparison.OrdinalIgnoreCase))
                    kind = ProviderKind.Firespitter;
                else if (string.Equals(name, "InterstellarFuelSwitch", StringComparison.OrdinalIgnoreCase))
                    kind = ProviderKind.InterstellarFuelSwitch;
                else { ++index; continue; }
                providers.Add(new Provider { Module = module, Kind = kind, Index = index });
                ++index;
            }
        }

        internal static FuelSwitchSupport Create(Part part)
        {
            FuelSwitchSupport result = new FuelSwitchSupport(part);
            return result.providers.Count > 0 ? result : null;
        }

        static string Read(PartModule module, string name)
        {
            try
            {
                MemberInfo m = ExponentBinding.FindMember(module.GetType(), name);
                if (m == null) return "";
                object v = ExponentBinding.Read(m, module);
                return v == null ? "" : Convert.ToString(v, CultureInfo.InvariantCulture);
            }
            catch { return ""; }
        }

        static string Mode(Provider p)
        {
            switch (p.Kind)
            {
                // A change of tank definition, rather than a change of total
                // allocated volume, is a new MFT variant. User-edited volumes
                // within a tank type are handled through resource change events.
                case ProviderKind.ModularFuelTanks:
                    return Read(p.Module, "type") + ":" + Read(p.Module, "utilization");
                case ProviderKind.Firespitter:
                    return Read(p.Module, "selectedTankSetup");
                default:
                    return Read(p.Module, "selectedTankSetup");
            }
        }

        internal string Signature
        {
            get
            {
                var items = new List<string>();
                foreach (Provider p in providers)
                {
                    string mode = Mode(p) ?? "";
                    items.Add(p.Index.ToString(CultureInfo.InvariantCulture) + ":" +
                        p.Kind.ToString() + ":" + mode.Length + ":" + mode);
                }
                return string.Join("|", items.ToArray());
            }
        }

        internal string Description
        {
            get
            {
                var items = new List<string>();
                foreach (Provider p in providers)
                    items.Add(p.Kind + " (" + Mode(p) + ")");
                return string.Join(", ", items.ToArray());
            }
        }

        // A switcher's own IPartMassModifier/IPartCostModifier is already
        // included by KSP. Return only its native contribution so the caller
        // can provide the EXTRA stretch delta, as for B9PartSwitch.
        internal float NativeMass(float native, ModifierStagingSituation sit)
        {
            if (queryingMass) return 0;
            queryingMass = true;
            try
            {
                float total = 0;
                foreach (Provider p in providers)
                {
                    IPartMassModifier modifier = p.Module as IPartMassModifier;
                    if (modifier == null || !p.Module.isEnabled) continue;
                    try { total += modifier.GetModuleMass(native, sit); }
                    catch (Exception e)
                    {
                        Log.Warning("[PartSculptor] " + p.Kind +
                            " mass modifier: " + e.Message);
                    }
                }
                return total;
            }
            finally { queryingMass = false; }
        }

        internal float NativeCost(float native, ModifierStagingSituation sit)
        {
            if (queryingCost) return 0;
            queryingCost = true;
            try
            {
                float total = 0;
                foreach (Provider p in providers)
                {
                    IPartCostModifier modifier = p.Module as IPartCostModifier;
                    if (modifier == null || !p.Module.isEnabled) continue;
                    try { total += modifier.GetModuleCost(native, sit); }
                    catch (Exception e)
                    {
                        Log.Warning("[PartSculptor] " + p.Kind +
                            " cost modifier: " + e.Message);
                    }
                }
                return total;
            }
            finally { queryingCost = false; }
        }

        // Use the same unambiguous persisted key format as B9 (the separator
        // cannot appear in a valid resource name). The external B9 signature
        // is included by the caller when both kinds of switcher are installed.
        internal static string Key(string signature, string name)
        {
            return B9PartSwitchSupport.ResourceKey(signature, name);
        }

        internal static bool SplitKey(string key, out string signature, out string name)
        {
            return B9PartSwitchSupport.SplitResourceKey(key, out signature, out name);
        }
    }
}
