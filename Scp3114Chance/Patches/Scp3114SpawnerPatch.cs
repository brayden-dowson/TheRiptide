using HarmonyLib;
using Mirror;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp3114;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(Scp3114Spawner))]
    class Scp3114SpawnerPatch
    {
        [HarmonyPatch(nameof(Scp3114Spawner.OnPlayersSpawned))]
        public static bool Prefix()
        {
            if (!NetworkServer.active)
                return false;
            Scp3114Spawner._ragdollsSpawned = false;
            if ((double)UnityEngine.Random.value > Scp3114Chance.Scp3114Chance.Singelton.config.SpawnChance)
                return false;
            Scp3114Spawner.SpawnCandidates.Clear();
            PlayerRolesUtils.ForEachRole<HumanRole>(new Action<ReferenceHub>(Scp3114Spawner.SpawnCandidates.Add));
            if (Scp3114Spawner.SpawnCandidates.Count < 2)
                return false;
            Scp3114Spawner.SpawnCandidates.RandomItem().roleManager.ServerSetRole(RoleTypeId.Scp3114, RoleChangeReason.RoundStart);
            return false;
        }
    }
}
