using HarmonyLib;
using PlayerStatsSystem;
using PluginAPI.Core;
using PlayerRoles.PlayableScps.HumeShield;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(DynamicHumeShieldController))]
    class MaxHumeShieldPatch
    {
        [HarmonyPatch(nameof(DynamicHumeShieldController.HsMax), MethodType.Getter)]
        public static bool Prefix(ref float __result, DynamicHumeShieldController __instance)
        {
            if (__instance?.Owner == null || Player.Get(__instance.Owner) == null || Player.Get(__instance.Owner).IsHuman ||
                __instance.Owner.roleManager.CurrentRole.RoleTypeId != PlayerRoles.RoleTypeId.Scp173)
                return true;

            __result = EventHandler.config.Shield;
            return false;
        }
    }
}
