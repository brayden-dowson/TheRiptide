using GameCore;
using HarmonyLib;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(RoundSummary))]
    class RoundEndOverride
    {
        [HarmonyPatch(nameof(RoundSummary.Start), MethodType.Normal)]
        public static bool Prefix(RoundSummary __instance)
        {
            RoundSummary.singleton = __instance;
            RoundSummary._singletonSet = true;
            if (!NetworkServer.active)
                return false;
            RoundSummary.roundTime = 0;
            __instance.KeepRoundOnOne = !ConfigFile.ServerConfig.GetBool("end_round_on_one_player");

            RoundSummary.KilledBySCPs = 0;
            RoundSummary.EscapedClassD = 0;
            RoundSummary.EscapedScientists = 0;
            RoundSummary.ChangedIntoZombies = 0;
            RoundSummary.Kills = 0;
            PlayerRoleManager.OnServerRoleSet += new PlayerRoleManager.ServerRoleSet(__instance.OnRoleChanged);
            PlayerStats.OnAnyPlayerDied += new Action<ReferenceHub, DamageHandlerBase>(__instance.OnAnyPlayerDied);
            return false;
        }
    }
}
