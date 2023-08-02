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
    [HarmonyPatch(typeof(PlayerStats))]
    class KillPlayerPatch
    {
        [HarmonyPatch(nameof(PlayerStats.KillPlayer))]
        public static bool Prefix(PlayerStats __instance, DamageHandlerBase handler)
        {
            if(EventHandler.mode.HasFlag(GameMode.Replication))
            {
                __instance._hub.roleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.RemoteAdmin);
                return false;
            }
            return true;
        }
    }
}
