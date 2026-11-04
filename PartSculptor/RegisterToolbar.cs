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

#if DEBUG
            Log = new Log("PartSculptor", Log.LEVEL.INFO);
#else
            Log = new Log("PartSculptor", Log.LEVEL.ERROR);
#endif

        }
    }
}
