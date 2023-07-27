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

            switch(__instance.Hub.roleManager.CurrentRole.RoleTypeId)
            {
                case PlayerRoles.RoleTypeId.Scp173:
                    __result = 5000;
                    return false;
                case PlayerRoles.RoleTypeId.Scp106:
                    __result = 3000;
                    return false;
                case PlayerRoles.RoleTypeId.Scp939:
                    __result = 3400;
                    return false;
                case PlayerRoles.RoleTypeId.Scp049:
                    __result = 3000;
                    return false;
                case PlayerRoles.RoleTypeId.Scp096:
                    __result = 3500;
                    return false;
            }
            return true;
        }
    }
}
