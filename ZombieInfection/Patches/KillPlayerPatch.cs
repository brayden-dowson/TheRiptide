using HarmonyLib;
using PlayerRoles;
using PlayerStatsSystem;
using PluginAPI.Core;
using Mirror;
using static TheRiptide.Utility;
using PlayerRoles.Ragdolls;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(PlayerStats))]
    class KillPlayerPatch
    {
        [HarmonyPatch(nameof(PlayerStats.KillPlayer))]
        public static bool Prefix(PlayerStats __instance, DamageHandlerBase handler)
        {
            if(__instance._hub.GetRoleId() == RoleTypeId.Scp0492 || handler is Scp049DamageHandler scp_handler)
            {
                BasicRagdoll ragdoll = CreateRagdoll(Server.Instance.ReferenceHub, __instance._hub.GetRoleId(), __instance._hub.gameObject.transform.position, __instance._hub.gameObject.transform.rotation, __instance._hub.nicknameSync.MyNick);
                NetworkServer.Spawn(ragdoll.gameObject);
                __instance._hub.roleManager.ServerSetRole(RoleTypeId.Scp0492, RoleChangeReason.RemoteAdmin);
                return false;
            }
            return true;
        }
    }
}
