// Stock KSP docking port compatibility for independently stretched geometry.
// KSP 1.12.x: ModuleDockingNode owns the docking face, nodeType/nodeTypes,
// and capture/acquisition distances. No Harmony or third-party DLL required.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PartSculptor
{
    internal sealed class DockingPortScaling
    {
        sealed class Port
        {
            internal ModuleDockingNode live;
            internal int moduleIndex;
            internal string nativeType;
            internal Vector3 normal;
            internal float acquireRange;
            internal float captureRange;
            internal float reengageDistance;
            internal string chosenType;
            internal float faceFactor;
            internal bool multipleTypes;
        }

        readonly Part owner;
        readonly List<Port> ports = new List<Port>();

        DockingPortScaling(Part part) { owner = part; }

        internal static DockingPortScaling Create(Part part, Part prefab)
        {
            if (part == null || prefab == null || part.Modules == null || prefab.Modules == null)
                return null;
            DockingPortScaling result = new DockingPortScaling(part);
            // Module index, rather than type name, distinguishes multiple
            // ModuleDockingNode instances on the same part.
            for (int i = 0; i < part.Modules.Count; ++i)
            {
                ModuleDockingNode live = part.Modules[i] as ModuleDockingNode;
                if (live == null) continue;
                ModuleDockingNode original = i < prefab.Modules.Count ?
                    prefab.Modules[i] as ModuleDockingNode : null;
                if (original == null) continue;  // Do not guess a baseline.

                Vector3 normal = Vector3.forward;
                // Prefer the prefab's actual dock transform, whose forward
                // vector represents the physical port face orientation.
                Transform dockingTransform = original.nodeTransform;
                if (dockingTransform == null && !string.IsNullOrEmpty(original.nodeTransformName))
                {
                    foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                        if (child.name == original.nodeTransformName)
                        { dockingTransform = child; break; }
                }
                if (dockingTransform != null)
                    normal = prefab.transform.InverseTransformDirection(dockingTransform.forward);
                else if (!string.IsNullOrEmpty(original.referenceAttachNode))
                {
                    foreach (AttachNode attach in prefab.attachNodes)
                        if (attach != null && attach.id == original.referenceAttachNode)
                        { normal = attach.orientation; break; }
                }
                if (normal.sqrMagnitude < 1e-8f) normal = Vector3.forward;

                Port port = new Port {
                    live = live,
                    moduleIndex = i,
                    nativeType = original.nodeType,
                    normal = normal.normalized,
                    acquireRange = original.acquireRange,
                    captureRange = original.captureRange,
                    reengageDistance = original.minDistanceToReEngage,
                    chosenType = live.nodeType,
                    faceFactor = 1f,
                    multipleTypes = (original.nodeTypes != null && original.nodeTypes.Count > 1) ||
                        (live.nodeTypes != null && live.nodeTypes.Count > 1)
                };
                result.ports.Add(port);
            }
            return result.ports.Count > 0 ? result : null;
        }

        internal void Apply(Vector3 axes, bool b9Present)
        {
            foreach (Port port in ports)
            {
                ModuleDockingNode node = port.live;
                if (node == null) continue;
                double face = ScaleMath.DockingFaceScale(port.normal.x, port.normal.y,
                    port.normal.z, axes.x, axes.y, axes.z);
                if (!ScaleMath.PositiveFinite(face) || face > float.MaxValue) continue;
                port.faceFactor = (float)face;
                if (Settings.ScaleDockingCaptureDistances && !b9Present)
                {
                    // All values come from partPrefab, NOT last frame's
                    // already-scaled docking node, to avoid compounding.
                    UpdateDistance(node, "acquireRange", port.acquireRange,
                        (float)(port.acquireRange * face));
                    UpdateDistance(node, "captureRange", port.captureRange,
                        (float)(port.captureRange * face));
                    UpdateDistance(node, "minDistanceToReEngage", port.reengageDistance,
                        (float)(port.reengageDistance * face));
                }

                // Never rewrite docking metadata on a joined or transitioning
                // port: that could disrupt the live docking finite state machine.
                if (!Settings.MatchDockingPortTypes || b9Present || port.multipleTypes ||
                    !CanChangeType(node))
                {
                    port.chosenType = node.nodeType;
                    continue;
                }
                double nominal = NativeDiameter(port.nativeType);
                if (!(nominal > 0d)) continue; // Preserve custom mod types.
                string desired = ScaleMath.DockingNodeType(port.nativeType,
                    nominal * face, Settings.DockingStockSizeTolerance);
                if (node.nodeType != desired)
                {
                    string before = node.nodeType;
                    node.nodeType = desired;
                    ScaleDebug.Change(owner, "docking", "nodeType", before, desired,
                        "native=" + port.nativeType + "; face scale=" +
                        ScaleDebug.Format(face));
                }
                // ModuleDockingNode also caches a set of compatible types.
                // Only touch an unambiguous single-type stock port.
                if (node.nodeTypes != null &&
                    (node.nodeTypes.Count == 0 || node.nodeTypes.Count == 1))
                {
                    if (!node.nodeTypes.Contains(desired))
                    {
                        node.nodeTypes.Clear();
                        node.nodeTypes.Add(desired);
                    }
                }
                port.chosenType = desired;
            }
        }

        static bool CanChangeType(ModuleDockingNode node)
        {
            if (node.otherNode != null || node.dockedPartUId != 0) return false;
            if (HighLogic.LoadedSceneIsEditor) return true;
            // A saved vessel may be in Acquire, Docked, Disengage, etc.
            return HighLogic.LoadedSceneIsFlight &&
                string.Equals(node.state, "Ready", StringComparison.OrdinalIgnoreCase);
        }

        static double NativeDiameter(string type)
        {
            switch (type)
            {
                case "size0": return 0.625d;
                case "size1": return 1.25d;
                case "size2": return 2.5d;
                default: return 0d;
            }
        }

        void UpdateDistance(ModuleDockingNode node, string name, float baseline, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f) return;
            float oldValue;
            switch (name)
            {
                case "acquireRange":
                    oldValue = node.acquireRange;
                    node.acquireRange = value; break;
                case "captureRange":
                    oldValue = node.captureRange;
                    node.captureRange = value; break;
                default:
                    oldValue = node.minDistanceToReEngage;
                    node.minDistanceToReEngage = value; break;
            }
            ScaleDebug.Change(owner, "docking", name, oldValue, value,
                "prefab=" + ScaleDebug.Format(baseline));
        }

        internal void AppendReport(List<ScaledValue> rows)
        {
            foreach (Port port in ports)
            {
                if (port.live == null) continue;
                string prefix = "Port " + port.moduleIndex + " ";
                rows.Add(new ScaledValue("Docking", prefix + "node type",
                    port.nativeType, port.live.nodeType,
                    "stock size type if near nominal diameter; custom otherwise"));
                rows.Add(new ScaledValue("Docking", prefix + "face multiplier",
                    1d, port.faceFactor, "transverse X/Y/Z face scale"));
                rows.Add(new ScaledValue("Docking", prefix + "acquire range",
                    port.acquireRange, port.live.acquireRange, "m"));
                rows.Add(new ScaledValue("Docking", prefix + "capture range",
                    port.captureRange, port.live.captureRange, "m"));
                rows.Add(new ScaledValue("Docking", prefix + "reengage distance",
                    port.reengageDistance, port.live.minDistanceToReEngage, "m"));
            }
        }
    }
}
