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
            if (__instance?.Owner == null || Player.Get(__instance.Owner) == null || Player.Get(__instance.Owner).IsHuman)
                return true;

            float x = UnityEngine.Mathf.Clamp(1.0f - (__instance._hp.CurValue / __instance._hp.MaxValue - 0.2f) * (1.0f / 0.8f), 0.0f, 1.0f);

            switch (__instance.Owner.roleManager.CurrentRole.RoleTypeId)
            {
                case PlayerRoles.RoleTypeId.Scp173:
                    __result = (float)Math.Round((750.0f * (1.0f - x) + 2500.0f * x) / 50.0f) * 50.0f;
                    return false;
                case PlayerRoles.RoleTypeId.Scp106:
                    __result = (float)Math.Round((300.0f * (1.0f - x) + 1900.0f * x) / 50.0f) * 50.0f;
                    return false;
                case PlayerRoles.RoleTypeId.Scp939:
                    __result = (float)Math.Round((350.0f * (1.0f - x) + 1700.0f * x) / 50.0f) * 50.0f;
                    return false;
                case PlayerRoles.RoleTypeId.Scp049:
                    __result = (float)Math.Round((200.0f * (1.0f - x) + 1500.0f * x) / 50.0f) * 50.0f;
                    return false;
                case PlayerRoles.RoleTypeId.Scp096:
                    __result = (float)Math.Round((600.0f * (1.0f - x) + 2200.0f * x) / 50.0f) * 50.0f;
                    return false;
            }
            return true;
        }
    }
}
