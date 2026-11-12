// Editor-safe rigid child rotation for mirrored face morphing.
// Uses KSP-style Transform.Rotate, PartRotated events, gizmo refresh and
// optional native symmetry mapping; a Morph drag owns its own undo backup.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PartSculptor
{
    internal static class EditorPartMorphRotation
    {
        private struct Pose
        {
            internal Part Part;
            internal Vector3 Position;
            internal Quaternion Rotation;
        }

        // General editor rotation helper based on the supplied Rotate() code.
        // Use for standalone editor rotations. During Copy, counterparts are
        // handled separately by the source's SetMorphFaces symmetry pass.
        internal static void Rotate(Part part, Vector3 eulerAngles, Space space,
            bool rotateSymmetry = true, bool recordUndo = true)
        {
            if (part == null || !HighLogic.LoadedSceneIsEditor) return;
            if (recordUndo) EditorUndoSupport.RecordBeforeChange();
            part.transform.Rotate(eulerAngles, space);
            NotifyRotation(part);
            if (rotateSymmetry && EditorLogic.fetch != null &&
                EditorLogic.RootPart != null)
            {
                if (part.symMethod == SymmetryMethod.Mirror)
                    RotateMirrorSymmetryCounterparts(part, eulerAngles, space);
                else if (part.symMethod == SymmetryMethod.Radial)
                    RotateRadialSymmetryCounterparts(part, eulerAngles, space);
            }
        }

        // Does not invoke the symmetry helpers: each physical stack joint in
        // SetMorphFaces' symmetry targets has its own snapshot and rotation.
        // Rotating counterparts here too would double-rotate some children.
        internal static bool RotateJointSubtree(Part child, Quaternion turn,
            Vector3 oldJointAnchor, Vector3 newJointAnchor)
        {
            if (child == null || !HighLogic.LoadedSceneIsEditor) return false;
            float angle;
            Vector3 axis;
            turn.ToAngleAxis(out angle, out axis);
            if (float.IsNaN(angle) || float.IsInfinity(angle) ||
                float.IsNaN(axis.x) || float.IsNaN(axis.y) || float.IsNaN(axis.z) ||
                angle > 150f ||
                ((newJointAnchor - oldJointAnchor).sqrMagnitude < 1e-12f && angle < 0.0001f))
                return false;

            var poses = new List<Pose>();
            Capture(child, poses, new HashSet<Part>());
            if (poses.Count == 0) return false;
            bool shouldRotate = angle > 0.0001f && axis.sqrMagnitude > 1e-8f;
            // Reconstruct absolute world poses from a single snapshot: Unity's
            // transform hierarchy and the KSP physical part tree are not
            // guaranteed to be the same, so no descendant may move twice.
            foreach (Pose pose in poses)
            {
                if (pose.Part == null) continue;
                pose.Part.transform.rotation = pose.Rotation;
                if (shouldRotate)
                    pose.Part.transform.Rotate(axis, angle, Space.World);
                pose.Part.transform.position = newJointAnchor +
                    turn * (pose.Position - oldJointAnchor);
            }
            // Match KSP's editor PartRotated event model: one event for the
            // rotated child root, not a storm of events for its descendants.
            // Their world transforms were already updated above.
            NotifyRotation(child);
            return true;
        }

        private static void Capture(Part part, List<Pose> poses, HashSet<Part> seen)
        {
            if (part == null || !seen.Add(part)) return;
            poses.Add(new Pose { Part = part, Position = part.transform.position,
                Rotation = part.transform.rotation });
            if (part.children == null) return;
            foreach (Part descendant in part.children)
                if (descendant != null && descendant.parent == part)
                    Capture(descendant, poses, seen);
        }

        private static void NotifyRotation(Part part)
        {
            ModulePartSculptor module =
                part.FindModuleImplementing<ModulePartSculptor>();
            if (module != null)
            {
                ScaleGizmo.UpdateFor(module);
                // UpdateFor calls the gizmo's Update(); during a live drag
                // that would re-enter SetMorphFaces recursively. Its normal
                // next frame updates the handles in that case.
                if (!FaceMorphGizmo.IsDragging)
                    FaceMorphGizmo.UpdateFor(module);
            }
            GameEvents.onEditorPartEvent.Fire(ConstructionEventType.PartRotated, part);
        }

        private static void RotateMirrorSymmetryCounterparts(Part part,
            Vector3 eulerAngles, Space space)
        {
            if (space == Space.Self)
                eulerAngles = part.transform.TransformDirection(eulerAngles);
            eulerAngles = EditorLogic.RootPart.transform.InverseTransformDirection(eulerAngles);
            Quaternion rotation = Quaternion.Euler(eulerAngles);
            float angle;
            Vector3 axis;
            rotation.ToAngleAxis(out angle, out axis);
            Vector3 mirrorAxis = new Vector3(-axis.x, axis.y, axis.z);
            mirrorAxis = EditorLogic.RootPart.transform.TransformDirection(mirrorAxis);
            foreach (Part counterpart in part.symmetryCounterparts)
            {
                if (counterpart == null) continue;
                counterpart.transform.Rotate(mirrorAxis, -angle, Space.World);
                NotifyRotation(counterpart);
            }
        }

        private static void RotateRadialSymmetryCounterparts(Part part,
            Vector3 eulerAngles, Space space)
        {
            if (space == Space.World)
                eulerAngles = part.transform.InverseTransformDirection(eulerAngles);
            foreach (Part counterpart in part.symmetryCounterparts)
            {
                if (counterpart == null) continue;
                counterpart.transform.Rotate(eulerAngles, Space.Self);
                NotifyRotation(counterpart);
            }
        }
    }
}
