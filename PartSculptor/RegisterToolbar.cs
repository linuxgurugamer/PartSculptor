using ClickThroughFix;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using ToolbarControl_NS;
using UnityEngine;
using UnityEngine.EventSystems;
using KSP_Log;

namespace PartSculptor
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    internal  class RegisterToolbar : MonoBehaviour
    {
        public static Log Log;

        void Start()
        {
            ToolbarControl.RegisterMod(VesselInspectorHost.ModId, "PartSculptor");

            // Settings.Load may log warnings or errors, so provide a bootstrap logger.
            // Read Settings.cfg before choosing the final log level.
            Log = new Log("PartSculptor", Log.LEVEL.ERROR);
            Settings.Load();
            if (Settings.DebugLogChanges)
            {
                Log = new Log("PartSculptor", Log.LEVEL.DETAIL);
            }
            else
            {
                Log = new Log("PartSculptor", Log.LEVEL.ERROR);
            }
        }
    }
}
