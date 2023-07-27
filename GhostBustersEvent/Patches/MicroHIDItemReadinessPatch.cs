using HarmonyLib;
using InventorySystem.Items.MicroHID;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(MicroHIDItem))]
    class MicroHIDItemReadinessPatch
    {
        [HarmonyPatch(nameof(MicroHIDItem.Readiness), MethodType.Getter)]
        public static bool Prefix(ref float __result, MicroHIDItem __instance)
        {
            if(__instance.State == HidState.PoweringUp || __instance.State == HidState.Primed || __instance.State == HidState.Firing)
                __result = 1.0f;
            else
                __result = 0.0f;

            return false;
        }
    }
}
