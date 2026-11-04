// KSP editor undo integration shared by the Stretch and Morph handles.
// One pre-edit backup per gesture lets KSP restore the serialized KSPFields
// (including all twelve morph values) as a single undo/redo operation.
using System;
using UnityEngine;

using static PartSculptor.RegisterToolbar;

namespace PartSculptor
{
    internal static class EditorUndoSupport
    {
        internal static bool RecordBeforeChange()
        {
            if (!HighLogic.LoadedSceneIsEditor || EditorLogic.fetch == null ||
                EditorLogic.fetch.ship == null) return false;
            try
            {
                EditorLogic.fetch.SetBackup();
                return true;
            }
            catch (Exception e)
            {
                Log.Warning("[PartSculptor] Could not register editor undo backup: " + e.Message);
                return false;
            }
        }

        // Observe KSP's native shortcuts without consuming the key event or
        // implementing a second undo stack. The stock editor performs undo/redo.
        internal static bool UndoRedoShortcutPressed()
        {
            bool control = Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);
            return control && (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Y));
        }
    }
}
