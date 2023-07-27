using HarmonyLib;
using PlayerRoles.PlayableScps.Scp096;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(Scp096RageManager))]
    class RageEndPatch
    {
        [HarmonyPatch(nameof(Scp096RageManager.ServerEndEnrage), MethodType.Normal)]
        public static bool Prefix(Scp096RageManager __instance, bool clearTime)
        {
            return false;
        }
    }
}
