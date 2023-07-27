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
    //from https://github.com/Fondation-Azarus/CustomSpawn/blob/main/CustomSpawn/Patches/HealthStatPatch.cs
    [HarmonyPatch(typeof(HealthStat))]
    public static class MaxHealthPatch
    {
        [HarmonyPatch(nameof(HealthStat.MaxValue), MethodType.Getter)]
        public static bool Prefix(ref float __result, HealthStat __instance)
        {
            if (__instance?.Hub == null || Player.Get(__instance.Hub) == null || !EventHandler.IsPlayerChild[__instance.Hub.PlayerId])
                return true;

            __result = 75.0f;
            return false;
        }
    }
}
