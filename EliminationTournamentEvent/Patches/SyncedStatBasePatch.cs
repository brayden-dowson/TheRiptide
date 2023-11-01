using HarmonyLib;
using PlayerRoles;
using PlayerStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(SyncedStatBase))]
    class SyncedStatBasePatch
    {
        [HarmonyPatch(nameof(SyncedStatBase.CanReceive))]
        public static bool Prefix(SyncedStatBase __instance, ref bool __result, ReferenceHub hub)
        {
            if (hub.isLocalPlayer)
            {
                __result = false;
                return false;
            }
            if(!SpectatorVisibility.AllowSpectating(hub, __instance.Hub))
            {
                __result = false;
                return false;
            }
            switch (__instance.Mode)
            {
                case SyncedStatBase.SyncMode.Private:
                    __result = hub == __instance.Hub;
                    break;
                case SyncedStatBase.SyncMode.PrivateAndSpectators:
                    __result = !hub.IsAlive() || hub == __instance.Hub;
                    break;
                case SyncedStatBase.SyncMode.Public:
                    __result = true;
                    break;
                default:
                    __result = false;
                    break;
            }
            return false;
        }
    }
}
