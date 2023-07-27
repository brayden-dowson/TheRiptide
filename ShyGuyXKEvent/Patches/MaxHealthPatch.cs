using HarmonyLib;
using PlayerStatsSystem;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(HealthStat))]
    public static class MaxHealthPatch
    {
        [HarmonyPatch(nameof(HealthStat.MaxValue), MethodType.Getter)]
        public static bool Prefix(ref float __result, HealthStat __instance)
        {
            if (__instance?.Hub == null || Player.Get(__instance.Hub) == null || Player.Get(__instance.Hub).IsHuman)
                return true;

            if(__instance.Hub.roleManager.CurrentRole.RoleTypeId == PlayerRoles.RoleTypeId.Scp096)
            {
                __result = EventHandler.player_count * EventHandler.config.HealthScaling;
                return false;
            }
            return true;
        }
    }
}
