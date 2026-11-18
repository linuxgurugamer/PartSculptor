// Attachment-node history adapted from StretchScale 1.3 for PartSculptor.
// A node has three distinct poses: native (before stretching), stretched
// (before face morphing), and the final pose last written by PartSculptor.
// Only a change from that FINAL pose is evidence of a foreign module write.
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PartSculptor
{
    internal sealed class NodeGeometryHistory
    {
        internal const string SurfaceId = "$surface";
        private const string SaveNode = "PARTSCULPTOR_NODE_HISTORY";
        private const float EpsilonSquared = 1e-6f;

        internal sealed class Record
        {
            internal Vector3 NativePosition, NativeOriginal, NativeOrientation;
            internal int NativeSize;
            internal Vector3 StretchedPosition;
            internal Vector3 WrittenPosition, WrittenOriginal, WrittenOrientation;
            internal int WrittenSize;
            internal bool HasWritten;
        }

        private readonly Dictionary<string, Record> records =
            new Dictionary<string, Record>(StringComparer.Ordinal);

        internal static bool Near(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude <= EpsilonSquared;
        }

        internal static bool Near(Vector3 a, Vector3 b, Vector3 c)
        {
            return Near(a, b) && Near(b, c);
        }

        internal Record Find(string id)
        {
            Record record;
            return !string.IsNullOrEmpty(id) && records.TryGetValue(id, out record)
                ? record : null;
        }

        internal Record Seed(string id, Vector3 position, Vector3 original,
            Vector3 orientation, int size)
        {
            Record record = Find(id);
            if (record != null) return record;
            record = new Record {
                NativePosition = position, NativeOriginal = original,
                NativeOrientation = orientation, NativeSize = size,
                StretchedPosition = position
            };
            records[id] = record;
            return record;
        }

        internal bool WasWritten(string id, AttachNode node)
        {
            Record record = Find(id);
            return record != null && record.HasWritten && node != null &&
                Near(node.position, record.WrittenPosition) &&
                Near(node.originalPosition, record.WrittenOriginal) &&
                Near(node.orientation, record.WrittenOrientation) &&
                node.size == record.WrittenSize;
        }

        internal bool ChangedExternally(Part part)
        {
            if (part == null) return false;
            foreach (AttachNode node in part.attachNodes)
                if (node != null && !string.IsNullOrEmpty(node.id) &&
                    (Find(node.id) == null || !WasWritten(node.id, node)))
                    return true;
            AttachNode surface = part.srfAttachNode;
            return surface != null && Find(SurfaceId) != null &&
                !WasWritten(SurfaceId, surface);
        }

        // The external module has supplied a NEW native node. Existing records
        // are replaced only when a genuinely foreign write was detected.
        internal Record Adopt(string id, AttachNode node, float baseFactor)
        {
            Record record = Find(id);
            if (record == null || node == null) return record;
            float factor = baseFactor > 1e-6f ? baseFactor : 1f;
            // A foreign module can update only ONE attribute (for example
            // node size or normal). Never learn our previously stretched or
            // morphed values as the new native values for the other fields.
            if (!record.HasWritten || !Near(node.position, record.WrittenPosition))
                record.NativePosition = node.position / factor;
            if (!record.HasWritten || !Near(node.originalPosition, record.WrittenOriginal))
                record.NativeOriginal = node.originalPosition / factor;
            if (!record.HasWritten || !Near(node.orientation, record.WrittenOrientation))
                record.NativeOrientation = node.orientation;
            if (!record.HasWritten || node.size != record.WrittenSize)
                record.NativeSize = node.size;
            return record;
        }

        internal void RecordStretched(string id, Vector3 stretched)
        {
            Record record = Find(id);
            if (record != null) record.StretchedPosition = stretched;
        }

        internal void RecordFinal(Part part)
        {
            if (part == null) return;
            foreach (AttachNode node in part.attachNodes)
                if (node != null && !string.IsNullOrEmpty(node.id))
                    RecordFinal(node.id, node);
            if (part.srfAttachNode != null && Find(SurfaceId) != null)
                RecordFinal(SurfaceId, part.srfAttachNode);
        }

        private void RecordFinal(string id, AttachNode node)
        {
            Record record = Find(id);
            if (record == null || node == null) return;
            record.WrittenPosition = node.position;
            record.WrittenOriginal = node.originalPosition;
            record.WrittenOrientation = node.orientation;
            record.WrittenSize = node.size;
            record.HasWritten = true;
        }

        private static void WriteVector(ConfigNode node, string key, Vector3 vector)
        {
            node.AddValue(key + "X", vector.x.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(key + "Y", vector.y.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(key + "Z", vector.z.ToString("R", CultureInfo.InvariantCulture));
        }

        private static bool ReadVector(ConfigNode node, string key, out Vector3 result)
        {
            result = Vector3.zero;
            float x, y, z;
            if (!float.TryParse(node.GetValue(key + "X"), NumberStyles.Float,
                CultureInfo.InvariantCulture, out x) ||
                !float.TryParse(node.GetValue(key + "Y"), NumberStyles.Float,
                CultureInfo.InvariantCulture, out y) ||
                !float.TryParse(node.GetValue(key + "Z"), NumberStyles.Float,
                CultureInfo.InvariantCulture, out z)) return false;
            if (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(z) ||
                float.IsInfinity(x) || float.IsInfinity(y) || float.IsInfinity(z)) return false;
            result = new Vector3(x, y, z);
            return true;
        }

        internal void Load(ConfigNode owner)
        {
            records.Clear();
            if (owner == null) return;
            foreach (ConfigNode entry in owner.GetNodes(SaveNode))
            {
                string id = entry.GetValue("id");
                if (string.IsNullOrEmpty(id) || records.ContainsKey(id)) continue;
                Vector3 native, original, normal, stretched, written, writtenOriginal, writtenNormal;
                int nativeSize, writtenSize;
                if (!ReadVector(entry, "native", out native) ||
                    !ReadVector(entry, "nativeOriginal", out original) ||
                    !ReadVector(entry, "nativeNormal", out normal) ||
                    !ReadVector(entry, "stretched", out stretched) ||
                    !ReadVector(entry, "written", out written) ||
                    !ReadVector(entry, "writtenOriginal", out writtenOriginal) ||
                    !ReadVector(entry, "writtenNormal", out writtenNormal) ||
                    !int.TryParse(entry.GetValue("nativeSize"), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out nativeSize) ||
                    !int.TryParse(entry.GetValue("writtenSize"), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out writtenSize)) continue;
                records[id] = new Record {
                    NativePosition = native, NativeOriginal = original,
                    NativeOrientation = normal, NativeSize = nativeSize,
                    StretchedPosition = stretched, WrittenPosition = written,
                    WrittenOriginal = writtenOriginal, WrittenOrientation = writtenNormal,
                    WrittenSize = writtenSize, HasWritten = true
                };
            }
        }

        internal void Save(ConfigNode owner)
        {
            if (owner == null) return;
            owner.RemoveNodes(SaveNode);
            foreach (KeyValuePair<string, Record> pair in records)
            {
                Record record = pair.Value;
                if (!record.HasWritten) continue;
                ConfigNode entry = owner.AddNode(SaveNode);
                entry.AddValue("id", pair.Key);
                WriteVector(entry, "native", record.NativePosition);
                WriteVector(entry, "nativeOriginal", record.NativeOriginal);
                WriteVector(entry, "nativeNormal", record.NativeOrientation);
                entry.AddValue("nativeSize", record.NativeSize.ToString(CultureInfo.InvariantCulture));
                WriteVector(entry, "stretched", record.StretchedPosition);
                WriteVector(entry, "written", record.WrittenPosition);
                WriteVector(entry, "writtenOriginal", record.WrittenOriginal);
                WriteVector(entry, "writtenNormal", record.WrittenOrientation);
                entry.AddValue("writtenSize", record.WrittenSize.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
