// Non-compounding visual scaling for stock engine FX and optional Waterfall effects.
// No Waterfall assembly reference is required. Waterfall uses mesh renderers with
// Waterfall shaders; stock effects can use ParticleSystem or KSPParticleEmitter.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PartSculptor
{
    internal sealed class EngineEffectScaling
    {
        sealed class EffectRoot
        {
            internal Transform source;
            internal Transform wrapper;
            internal string category;
        }

        readonly Part part;
        readonly Transform model;
        readonly List<EffectRoot> roots = new List<EffectRoot>();
        readonly HashSet<Transform> observed = new HashSet<Transform>();
        readonly bool enginePresent;
        readonly bool waterfallPresent;
        Vector3 axes = Vector3.one;
        float nextScan;

        internal EngineEffectScaling(Part part, Transform model)
        {
            this.part = part;
            this.model = model;
            if (part == null || part.Modules == null) return;
            foreach (PartModule module in part.Modules)
            {
                if (module == null) continue;
                string name = module.moduleName ?? module.GetType().Name;
                if (module is ModuleEngines || module is ModuleRCS ||
                    name.StartsWith("ModuleEngines", StringComparison.Ordinal) ||
                    name.StartsWith("ModuleRCS", StringComparison.Ordinal))
                    enginePresent = true;
                if (name == "ModuleWaterfallFX" || module.GetType().Name == "ModuleWaterfallFX")
                    waterfallPresent = true;
            }
        }

        // All particle/effect children under the resized model already inherit
        // the model's absolute X/Y/Z scale; altering them individually would
        // multiply the scale twice. Only external effect roots need wrappers.
        static bool IsUnder(Transform t, Transform ancestor)
        {
            if (t == null || ancestor == null) return false;
            for (Transform p = t; p != null; p = p.parent)
                if (p == ancestor) return true;
            return false;
        }

        bool IsOwnExternalEffect(Transform t)
        {
            if (t == null || part == null || t == part.transform || IsUnder(t, model))
                return false;
            for (Transform p = t; p != null; p = p.parent)
            {
                Part other = p.GetComponent<Part>();
                if (other != null && other != part) return false;
                if (p == part.transform) return true;
            }
            return false;
        }

        static int Depth(Transform t)
        {
            int depth = 0;
            for (; t != null; t = t.parent) depth++;
            return depth;
        }

        static bool UsesWaterfallShader(Renderer renderer)
        {
            if (renderer == null) return false;
            try
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null) continue;
                    if (material.shader.name.IndexOf("Waterfall", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            catch (Exception) { /* An effect can be destroyed while switching engines. */ }
            return false;
        }

        void Candidate(Transform transform, string category,
            Dictionary<Transform, string> candidates)
        {
            if (IsOwnExternalEffect(transform) && !observed.Contains(transform))
            {
                // Prefer stock FX if a transform has both particle and mesh FX.
                if (!candidates.ContainsKey(transform)) candidates.Add(transform, category);
            }
        }

        void Discover()
        {
            if (part == null || part.transform == null) return;
            // Remove dead wrappers on engine-mode changes, vessel unload, etc.
            for (int i = roots.Count - 1; i >= 0; --i)
            {
                EffectRoot entry = roots[i];
                if (entry.source == null || entry.wrapper == null)
                {
                    if (entry.wrapper != null) UnityEngine.Object.Destroy(entry.wrapper.gameObject);
                    observed.Remove(entry.source);
                    roots.RemoveAt(i);
                }
            }

            var candidates = new Dictionary<Transform, string>();
            if (enginePresent && Settings.ScaleEngineEffects)
            {
                foreach (ParticleSystem particle in part.GetComponentsInChildren<ParticleSystem>(true))
                    if (particle != null) Candidate(particle.transform, "Stock FX", candidates);

                // Stock KSP engines also use legacy KSPParticleEmitter objects.
                // Look them up by component type to avoid a hard assembly reference.
                foreach (Component component in part.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) continue;
                    string typeName = component.GetType().Name;
                    if (typeName == "KSPParticleEmitter" || typeName == "ParticleEmitter")
                        Candidate(component.transform, "Stock FX", candidates);
                }
            }

            if (waterfallPresent && Settings.ScaleWaterfallEffects)
            {
                // Waterfall templates instantiate their own meshes, normally
                // below thrustTransform (and therefore below `model`). Those
                // automatically follow model scaling. Standalone FX meshes
                // using Waterfall shaders need an independent parent wrapper.
                foreach (Renderer renderer in part.GetComponentsInChildren<Renderer>(true))
                    if (UsesWaterfallShader(renderer))
                        Candidate(renderer.transform, "Waterfall", candidates);
            }

            var sorted = new List<Transform>(candidates.Keys);
            sorted.Sort((a, b) => Depth(a).CompareTo(Depth(b)));
            foreach (Transform source in sorted)
            {
                if (source == null || source.parent == null || observed.Contains(source)) continue;
                // Don't wrap both a particle system and one of its child
                // renderers: that would square the effect scale.
                bool alreadyCovered = false;
                foreach (EffectRoot entry in roots)
                    if (entry.wrapper != null && IsUnder(source, entry.wrapper))
                    { alreadyCovered = true; break; }
                if (alreadyCovered) continue;

                string category = candidates[source];
                Transform parent = source.parent;
                GameObject holder = new GameObject("PartSculptor FX Scale (" + category + ")");
                Transform wrapper = holder.transform;
                wrapper.SetParent(parent, false);
                wrapper.localPosition = Vector3.zero;
                wrapper.localRotation = Quaternion.identity;
                wrapper.localScale = Vector3.one;
                // Keep source-local offsets and runtime modifiers unchanged.
                // Waterfall controllers can continue animating the source's
                // position/scale inside this absolute-scale wrapper.
                source.SetParent(wrapper, false);
                var root = new EffectRoot { source = source, wrapper = wrapper,
                    category = category };
                roots.Add(root);
                observed.Add(source);
                ScaleDebug.Info(part, "effects", "wrapped external " + category +
                    " root '" + source.name + "'; native parent=" + parent.name);
            }
            // Already-parented effects are intentionally never directly
            // changed; they're scaled by model.localScale.
        }

        internal void Apply(Vector3 stretch)
        {
            if (!FinitePositive(stretch)) return;
            axes = stretch;
            if (!enginePresent && !waterfallPresent) return;
            Discover();
            foreach (EffectRoot entry in roots)
            {
                if (entry.wrapper == null) continue;
                Vector3 before = entry.wrapper.localScale;
                entry.wrapper.localScale = axes;
                ScaleDebug.Change(part, "effects", entry.category + " '" +
                    (entry.source != null ? entry.source.name : "FX") + "'",
                    before, axes, "absolute scale, not cumulative");
            }
            // Stagger checks across multi-engine craft to avoid update spikes.
            nextScan = Time.realtimeSinceStartup + Settings.EffectRefreshSeconds +
                (part.GetInstanceID() & 15) * 0.03f;
        }

        static bool FinitePositive(Vector3 scale)
        {
            return scale.x > 0f && scale.y > 0f && scale.z > 0f &&
                !float.IsNaN(scale.x) && !float.IsNaN(scale.y) && !float.IsNaN(scale.z) &&
                !float.IsInfinity(scale.x) && !float.IsInfinity(scale.y) && !float.IsInfinity(scale.z);
        }

        // Effects can be instantiated in flight after OnStartFinished or
        // recreated when B9 / engine modes switch. Check only parts with FX,
        // at a low frequency, without generating ship-modified events.
        internal void Update()
        {
            if ((enginePresent && Settings.ScaleEngineEffects) ||
                (waterfallPresent && Settings.ScaleWaterfallEffects))
                if (Time.realtimeSinceStartup >= nextScan)
                    Apply(axes);
        }

        internal void AppendReport(List<ScaledValue> values)
        {
            if (values == null || (!enginePresent && !waterfallPresent)) return;
            values.Add(new ScaledValue("Visual effects", "FX scaling axes",
                Vector3.one, axes,
                "Stock / Waterfall effects beneath the model inherit model scale"));
            int stockExternal = 0, waterfallExternal = 0;
            foreach (EffectRoot entry in roots)
            {
                if (entry.wrapper == null || entry.source == null) continue;
                if (entry.category == "Waterfall") waterfallExternal++;
                else stockExternal++;
            }
            if (stockExternal > 0)
                values.Add(new ScaledValue("Visual effects", "External stock FX roots",
                    0d, stockExternal, "Non-model emitters with scale wrappers"));
            if (waterfallExternal > 0)
                values.Add(new ScaledValue("Visual effects", "External Waterfall FX roots",
                    0d, waterfallExternal, "Waterfall-shader meshes with scale wrappers"));
        }
    }
}
