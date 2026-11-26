// Pure, KSP-free preset calculations used by the PAW, editor and regression runner.
using System;
using System.Collections.Generic;

namespace PartSculptor
{
    internal sealed class ScalePresetDefinition
    {
        internal string Name;
        internal bool Relative;
        internal double X, Y, Z;
        internal bool BuiltIn;

        internal ScalePresetDefinition(string name, bool relative, double x, double y, double z, bool builtIn)
        {
            Name = name;
            Relative = relative;
            X = x; Y = y; Z = z;
            BuiltIn = builtIn;
        }

        internal ScalePresetDefinition Copy()
        {
            return new ScalePresetDefinition(Name, Relative, X, Y, Z, BuiltIn);
        }
    }

    internal static class ScalePresetMath
    {
        internal static bool ValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 64 ||
                string.Equals(name.Trim(), "Custom", StringComparison.OrdinalIgnoreCase)) return false;
            foreach (char c in name)
                if (char.IsControl(c) || c == '=' || c == '{' || c == '}' ||
                    c == '[' || c == ']') return false;
            return true;
        }

        internal static bool Valid(ScalePresetDefinition p)
        {
            return p != null && ValidName(p.Name) &&
                ScaleMath.PositiveFinite(p.X) && ScaleMath.PositiveFinite(p.Y) &&
                ScaleMath.PositiveFinite(p.Z);
        }

        // Match the persisted single-precision axes, not total scale (which
        // also includes TweakScale), and never execute a preset during load.
        internal static bool MatchesAbsolute(ScalePresetDefinition preset, float x, float y, float z)
        {
            return Valid(preset) && !preset.Relative &&
                x == (float)preset.X && y == (float)preset.Y && z == (float)preset.Z;
        }

        internal static string ResolveDisplay(IList<ScalePresetDefinition> presets,
            string savedName, float x, float y, float z)
        {
            if (string.Equals(savedName, "Custom", StringComparison.OrdinalIgnoreCase))
                return "Custom";
            bool infer = string.IsNullOrEmpty(savedName);
            foreach (ScalePresetDefinition preset in presets)
                if (MatchesAbsolute(preset, x, y, z) &&
                    (infer || string.Equals(savedName, preset.Name, StringComparison.OrdinalIgnoreCase)))
                    return preset.Name;
            // Registry's emergency option when its files are unavailable.
            if (presets.Count == 0 && x == 1f && y == 1f && z == 1f &&
                (infer || string.Equals(savedName, "1x uniform", StringComparison.OrdinalIgnoreCase)))
                return "1x uniform";
            return "Custom";
        }

        // All components are ABSOLUTE for absolute presets, and multiply the
        // current axes for explicit relative presets (e.g. custom Y x2).
        internal static ScaleMath.Axes Apply(ScalePresetDefinition p, double x, double y, double z)
        {
            if (!Valid(p)) return new ScaleMath.Axes(x, y, z);
            return p.Relative
                ? new ScaleMath.Axes(x * p.X, y * p.Y, z * p.Z)
                : new ScaleMath.Axes(p.X, p.Y, p.Z);
        }
    }
}
