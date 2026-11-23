// Restore stack connections after KSP reconstructs an editor craft at prefab
// node positions, before the saved PartSculptor node offsets are available.
// This does not run during normal resizing, nor on flight vessel loading.
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

using static PartSculptor.RegisterToolbar;

namespace PartSculptor
{
    internal sealed class AttachmentLoadFix
    {
        const string SaveNode = "PARTSCULPTOR_STACK_GAP";
        const float PositionEpsilon = 0.002f;
        const int StartupDelayFrames = 10;
        // B9 and other switchers can rewrite nodes after OnStartFinished and
        // after the first editor placement pass. Watch only during craft load.
        const int RecoveryWatchFrames = 150;
        const int ReadyTimeoutFrames = 90;
        static int scheduledFrame = -1;
        static int latestScheduleFrame = -1;
        static int lastPartsCount = -1;
        static int stableSinceFrame = -1;
        static int lastPollFrame = -1;
        static bool recoveryStarted;
        static readonly HashSet<Part> initialParts = new HashSet<Part>();
        static readonly Dictionary<Part, ObservedJoin> observed =
            new Dictionary<Part, ObservedJoin>();

        sealed class ObservedJoin
        {
            internal AttachNode ChildNode, ParentNode;
            internal Vector3 ChildPosition, ParentPosition;
        }

        // A gap is saved in the owning module's LOCAL coordinates, rather
        // than in world coordinates, so rotated craft can be reloaded safely.
        readonly Dictionary<string, Vector3> savedGaps =
            new Dictionary<string, Vector3>(StringComparer.Ordinal);

        static string Key(string relation, string ownNode, string peerNode)
        {
            return relation + "\u001f" + ownNode + "\u001f" + peerNode;
        }

        internal void Load(ConfigNode node)
        {
            savedGaps.Clear();
            if (node == null) return;
            foreach (ConfigNode record in node.GetNodes(SaveNode))
            {
                string relation = record.GetValue("relation");
                string ownNode = record.GetValue("ownNode");
                string peerNode = record.GetValue("peerNode");
                float x, y, z;
                if ((relation != "parent" && relation != "child") ||
                    string.IsNullOrEmpty(ownNode) || string.IsNullOrEmpty(peerNode) ||
                    !ReadFloat(record, "x", out x) ||
                    !ReadFloat(record, "y", out y) ||
                    !ReadFloat(record, "z", out z)) continue;
                savedGaps[Key(relation, ownNode, peerNode)] = new Vector3(x, y, z);
            }
        }

        static bool ReadFloat(ConfigNode record, string key, out float value)
        {
            return float.TryParse(record.GetValue(key), NumberStyles.Float,
                CultureInfo.InvariantCulture, out value) &&
                !float.IsNaN(value) && !float.IsInfinity(value);
        }

        static void WriteGap(ConfigNode node, string relation, string ownNode,
            string peerNode, Vector3 gap)
        {
            ConfigNode record = node.AddNode(SaveNode);
            record.AddValue("relation", relation);
            record.AddValue("ownNode", ownNode);
            record.AddValue("peerNode", peerNode);
            record.AddValue("x", gap.x.ToString("R", CultureInfo.InvariantCulture));
            record.AddValue("y", gap.y.ToString("R", CultureInfo.InvariantCulture));
            record.AddValue("z", gap.z.ToString("R", CultureInfo.InvariantCulture));
        }

        internal void Save(ConfigNode node, Part owner, bool initialized)
        {
            if (node == null) return;
            // A part can be saved before initialization. Keep the previously
            // loaded gaps then, rather than serializing incomplete connections.
            if (initialized && owner != null)
            {
                Dictionary<string, Vector3> current = new Dictionary<string, Vector3>(StringComparer.Ordinal);
                AttachNode childNode, parentNode;
                if (owner.parent != null &&
                    (TryConnection(owner, owner.parent, out childNode, out parentNode) ||
                     TrySavedConnection("parent", owner, owner.parent,
                         out childNode, out parentNode)))
                {
                    Vector3 worldGap = owner.transform.TransformPoint(childNode.position) -
                        owner.parent.transform.TransformPoint(parentNode.position);
                    current[Key("parent", childNode.id, parentNode.id)] =
                        owner.transform.InverseTransformVector(worldGap);
                }
                foreach (Part child in owner.children)
                {
                    if (child == null ||
                        !(TryConnection(child, owner, out childNode, out parentNode) ||
                          TrySavedConnection("child", owner, child,
                              out parentNode, out childNode)))
                        continue;
                    Vector3 worldGap = child.transform.TransformPoint(childNode.position) -
                        owner.transform.TransformPoint(parentNode.position);
                    current[Key("child", parentNode.id, childNode.id)] =
                        owner.transform.InverseTransformVector(worldGap);
                }
                savedGaps.Clear();
                foreach (KeyValuePair<string, Vector3> entry in current)
                    savedGaps[entry.Key] = entry.Value;
            }
            node.RemoveNodes(SaveNode);
            foreach (KeyValuePair<string, Vector3> entry in savedGaps)
            {
                string[] fields = entry.Key.Split('\u001f');
                if (fields.Length == 3)
                    WriteGap(node, fields[0], fields[1], fields[2], entry.Value);
            }
        }

        internal bool FindSavedGap(string relation, string ownNode, string peerNode,
            Part owner, out Vector3 worldGap)
        {
            Vector3 local;
            if (owner != null && savedGaps.TryGetValue(Key(relation, ownNode, peerNode), out local))
            {
                worldGap = owner.transform.TransformVector(local);
                return true;
            }
            worldGap = Vector3.zero;
            return false;
        }

        // KSP sometimes reconstructs the child->parent relationship before
        // both AttachNode.attachedPart references are populated. The saved
        // connection IDs are unambiguous for the owning part, and allow a
        // post-load recovery without guessing by nearest visual node.
        internal bool TrySavedConnection(string relation, Part owner, Part peer,
            out AttachNode ownNode, out AttachNode peerNode)
        {
            ownNode = null;
            peerNode = null;
            if (owner == null || peer == null) return false;
            foreach (KeyValuePair<string, Vector3> entry in savedGaps)
            {
                string[] ids = entry.Key.Split('\u001f');
                if (ids.Length != 3 || ids[0] != relation) continue;
                AttachNode own = FindStackNode(owner, ids[1]);
                AttachNode other = FindStackNode(peer, ids[2]);
                if (own == null || other == null ||
                    (own.attachedPart != null && own.attachedPart != peer) ||
                    (other.attachedPart != null && other.attachedPart != owner)) continue;
                if (ownNode != null) return false; // ambiguous saved records
                ownNode = own;
                peerNode = other;
            }
            return ownNode != null;
        }

        static AttachNode FindStackNode(Part owner, string id)
        {
            if (owner == null || string.IsNullOrEmpty(id)) return null;
            foreach (AttachNode node in owner.attachNodes)
                if (node != null && string.Equals(node.id, id, StringComparison.Ordinal))
                    return node;
            return null;
        }

        internal static void Schedule()
        {
            if (!HighLogic.LoadedSceneIsEditor) return;
            // A number of PartModules schedule the same newly loaded craft.
            // The latest schedule is used, but a completed recovery can never
            // be restarted by a normal editor scale or B9 subtype change.
            latestScheduleFrame = Time.frameCount;
            scheduledFrame = Time.frameCount + StartupDelayFrames;
            lastPartsCount = -1;
            stableSinceFrame = -1;
            lastPollFrame = -1;
            recoveryStarted = false;
            initialParts.Clear();
            observed.Clear();
        }

        internal static void Poll()
        {
            if (scheduledFrame < 0 || !HighLogic.LoadedSceneIsEditor ||
                Time.frameCount < scheduledFrame || lastPollFrame == Time.frameCount)
                return;
            lastPollFrame = Time.frameCount;
            ShipConstruct ship = EditorLogic.fetch != null ? EditorLogic.fetch.ship : null;
            if (ship == null || ship.parts == null || ship.parts.Count == 0) return;
            if (ship.parts.Count != lastPartsCount)
            {
                // After recovery begins, a part-count change means the user is
                // editing the craft, not KSP finishing its initial load.
                if (recoveryStarted) { StopRecovery(); return; }
                lastPartsCount = ship.parts.Count;
                stableSinceFrame = Time.frameCount;
                return;
            }
            if (Time.frameCount - stableSinceFrame < 2) return;
            foreach (Part p in ship.parts)
            {
                ModulePartSculptor m = p != null ? p.FindModuleImplementing<ModulePartSculptor>() : null;
                if (m != null && !m.AttachmentReady &&
                    Time.frameCount - latestScheduleFrame < ReadyTimeoutFrames)
                    return;
            }
            try
            {
                if (!recoveryStarted)
                {
                    initialParts.Clear();
                    foreach (Part p in ship.parts)
                        if (p != null) initialParts.Add(p);
                    recoveryStarted = true;
                }
                HashSet<Part> visited = new HashSet<Part>();
                foreach (Part p in ship.parts)
                    if (p != null && p.parent == null)
                        RestoreSubtree(p, visited);
            }
            catch (Exception e)
            {
                Log.Error("Post-load attachment alignment failed: " + e);
                StopRecovery();
                return;
            }
            // Keep a bounded node-change watcher. It corrects B9's late
            // subtype/node writes but does not react to changes in part world
            // position from normal editor movement, and does not run later.
            if (Time.frameCount - latestScheduleFrame >= RecoveryWatchFrames)
                StopRecovery();
        }

        static void StopRecovery()
        {
            scheduledFrame = -1;
            recoveryStarted = false;
            initialParts.Clear();
            observed.Clear();
        }

        // KSP's node.attachedPart is the authority. Skipping surface attachments
        // avoids breaking off-axis radial placement or intentionally offset parts.
        internal static bool TryConnection(Part child, Part parent, out AttachNode source, out AttachNode destination)
        {
            source = null;
            destination = null;
            if (child == null || parent == null) return false;
            foreach (AttachNode node in child.attachNodes)
                if (node != null && node.attachedPart == parent && !string.IsNullOrEmpty(node.id))
                {
                    source = node;
                    break;
                }
            if (source == null)
            {
                // One-sided parent link: B9 may have recreated the child's
                // stack-node object without repopulating attachedPart yet.
                // Only accept an unambiguous opposite-facing child node.
                AttachNode linkedParent = null;
                foreach (AttachNode node in parent.attachNodes)
                    if (node != null && node.attachedPart == child)
                    {
                        if (linkedParent != null) return false;
                        linkedParent = node;
                    }
                if (linkedParent == null) return false;
                AttachNode match = null;
                foreach (AttachNode node in child.attachNodes)
                {
                    if (node == null || node.attachedPart != null ||
                        string.IsNullOrEmpty(node.id) ||
                        !OppositeNodeNormals(child, node, parent, linkedParent)) continue;
                    if (match != null) return false;
                    match = node;
                }
                if (match == null) return false;
                source = match;
                destination = linkedParent;
                return true;
            }
            float closest = float.MaxValue;
            Vector3 childWorld = child.transform.TransformPoint(source.position);
            foreach (AttachNode node in parent.attachNodes)
            {
                if (node == null || node.attachedPart != child || string.IsNullOrEmpty(node.id)) continue;
                float distance = (parent.transform.TransformPoint(node.position) - childWorld).sqrMagnitude;
                if (distance < closest)
                {
                    closest = distance;
                    destination = node;
                }
            }
            if (destination != null) return true;
            // One-sided child link: prefer a unique opposite-facing free
            // parent node. Never guess between multiple compatible nodes.
            AttachNode unlinked = null;
            foreach (AttachNode node in parent.attachNodes)
            {
                if (node == null || node.attachedPart != null ||
                    string.IsNullOrEmpty(node.id) ||
                    !OppositeNodeNormals(child, source, parent, node)) continue;
                if (unlinked != null) return false;
                unlinked = node;
            }
            destination = unlinked;
            return destination != null;
        }

        static bool OppositeNodeNormals(Part child, AttachNode childNode,
            Part parent, AttachNode parentNode)
        {
            Vector3 childNormal = child.transform.TransformDirection(childNode.orientation);
            Vector3 parentNormal = parent.transform.TransformDirection(parentNode.orientation);
            if (childNormal.sqrMagnitude < 1e-8f || parentNormal.sqrMagnitude < 1e-8f)
                return false;
            return Vector3.Dot(childNormal.normalized, parentNormal.normalized) < -0.8f;
        }

        static void RestoreSubtree(Part parent, HashSet<Part> visited)
        {
            if (parent == null || !visited.Add(parent)) return;
            foreach (Part child in parent.children)
            {
                if (child == null || !initialParts.Contains(child)) continue;
                RestoreConnection(child, parent);
                RestoreSubtree(child, visited); // top-down; child nodes are now final
            }
        }

        static void RestoreConnection(Part child, Part parent)
        {
            ModulePartSculptor childModule = child.FindModuleImplementing<ModulePartSculptor>();
            ModulePartSculptor parentModule = parent.FindModuleImplementing<ModulePartSculptor>();
            bool relevant = (childModule != null && childModule.HasStretchedGeometry) ||
                (parentModule != null && parentModule.HasStretchedGeometry);
            if (!relevant) return;
            AttachNode childNode, parentNode;
            if (!TryConnection(child, parent, out childNode, out parentNode))
            {
                // KSP/B9 can restore reciprocal AttachNode.attachedPart links
                // later than the parent tree. Prefer the persisted IDs over
                // guessing which node on a multi-node adapter is connected.
                AttachNode own, peer;
                if (childModule != null && childModule.AttachmentFix.TrySavedConnection(
                    "parent", child, parent, out own, out peer))
                { childNode = own; parentNode = peer; }
                else if (parentModule != null && parentModule.AttachmentFix.TrySavedConnection(
                    "child", parent, child, out own, out peer))
                { childNode = peer; parentNode = own; }
                else return;
            }

            ObservedJoin previous;
            bool wasObserved = observed.TryGetValue(child, out previous);
            if (wasObserved && previous.ChildNode == childNode &&
                previous.ParentNode == parentNode &&
                NodeGeometryHistory.Near(previous.ChildPosition, childNode.position) &&
                NodeGeometryHistory.Near(previous.ParentPosition, parentNode.position))
                return;
            observed[child] = new ObservedJoin {
                ChildNode = childNode, ParentNode = parentNode,
                ChildPosition = childNode.position, ParentPosition = parentNode.position
            };

            Vector3 intendedGap = Vector3.zero;
            bool saved = childModule != null && childModule.AttachmentFix.FindSavedGap(
                "parent", childNode.id, parentNode.id, child, out intendedGap);
            if (!saved)
                saved = parentModule != null && parentModule.AttachmentFix.FindSavedGap(
                    "child", parentNode.id, childNode.id, parent, out intendedGap);
            // Older 0.0.4.35 craft files have no gap record. Their stack nodes
            // were aligned during the edit, so zero is the safe legacy default.
            if (!saved) intendedGap = Vector3.zero;

            Vector3 actual = child.transform.TransformPoint(childNode.position);
            Vector3 expected = parent.transform.TransformPoint(parentNode.position) + intendedGap;
            Vector3 delta = new Vector3(
                (float)ScaleMath.AttachmentCorrection(expected.x, actual.x),
                (float)ScaleMath.AttachmentCorrection(expected.y, actual.y),
                (float)ScaleMath.AttachmentCorrection(expected.z, actual.z));
            if (delta.sqrMagnitude < PositionEpsilon * PositionEpsilon) return;
            Vector3 oldPosition = child.transform.position;
            MoveSubtree(child, delta, null, new HashSet<Part>());
            ScaleDebug.Change(child, "load attach", "stack position", oldPosition,
                child.transform.position, "parent=" + parent.name +
                "; child node=" + childNode.id + "; parent node=" + parentNode.id +
                "; saved offset=" + saved);
        }

        // Some KSP constructs Unity-parent the connected parts, others do not.
        // Move the entire subassembly exactly once, without moving twice when
        // the parent's Transform already carries descendants along.
        static void MoveSubtree(Part part, Vector3 delta, Transform movedAncestor,
            HashSet<Part> visited)
        {
            if (part == null || !visited.Add(part)) return;
            if (movedAncestor == null || !part.transform.IsChildOf(movedAncestor))
            {
                part.transform.position += delta;
                movedAncestor = part.transform;
            }
            foreach (Part child in part.children)
                MoveSubtree(child, delta, movedAncestor, visited);
        }
    }
}
