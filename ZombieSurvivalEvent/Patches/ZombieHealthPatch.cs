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
            if (__instance?.Hub == null ||
                Player.Get(__instance.Hub) == null ||
                Player.Get(__instance.Hub).IsHuman ||
                __instance.Hub.roleManager.CurrentRole.RoleTypeId != PlayerRoles.RoleTypeId.Scp0492 ||
                EventHandler.config == null)
                return true;

            if (EventHandler.zombies.Contains(__instance.Hub.PlayerId))
                __result = (EventHandler.health_ramping + EventHandler.config.ZombieHealth) * ((float)EventHandler.humans_alive / EventHandler.zombies_alive);
            else
                return true;
            return false;
        }
    }
}
