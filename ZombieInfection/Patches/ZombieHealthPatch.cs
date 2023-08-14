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
    //[HarmonyPatch(typeof(HealthStat))]
    //public static class MaxHealthPatch
    //{
    //    [HarmonyPatch(nameof(HealthStat.MaxValue), MethodType.Getter)]
    //    public static bool Prefix(ref float __result, HealthStat __instance)
    //    {
    //        if (__instance?.Hub == null ||
    //            Player.Get(__instance.Hub) == null ||
    //            Player.Get(__instance.Hub).IsHuman ||
    //            __instance.Hub.roleManager.CurrentRole.RoleTypeId != PlayerRoles.RoleTypeId.Scp0492)
    //            return true;

    //        if (EventHandler.zombies.ContainsKey(__instance.Hub.PlayerId))
    //            __result = ZombieInfectionEvent.Singleton.EventConfig.MotherZombieHealth;
    //        else if (EventHandler.zombies.Any(c => c.Value.Contains(__instance.Hub.PlayerId)))
    //            __result = ZombieInfectionEvent.Singleton.EventConfig.ChildrenHealthPool / EventHandler.zombies.First(c => c.Value.Contains(__instance.Hub.PlayerId)).Value.Count;
    //        else
    //            return true;
    //        return false;
    //    }
    //}
}
