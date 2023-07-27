using MEC;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using Respawning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public class AnomalousFootsoldier : IComparable
    {
        [PluginEntryPoint("Anomalous Footsoldier", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        static int anomalous_footsoldier = -1;
        static CoroutineHandle scp_item_regenerate;
        SpawnableTeamType spawning_team = SpawnableTeamType.None;

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            anomalous_footsoldier = -1;
            spawning_team = SpawnableTeamType.None;
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        public void OnRespawn(SpawnableTeamType team)
        {
            spawning_team = team;
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            Timing.CallDelayed(0.1f, () =>
            {
                if (anomalous_footsoldier == -1 &&
                    ((spawning_team == SpawnableTeamType.NineTailedFox && role.GetTeam() == Team.FoundationForces) ||
                    (spawning_team == SpawnableTeamType.ChaosInsurgency && role.GetTeam() == Team.ChaosInsurgency)) &&
                    !player.TemporaryData.Contains("custom_class"))
                {
                    anomalous_footsoldier = player.PlayerId;
                    player.TemporaryData.Add("custom_class", this);
                    player.SendBroadcast("[Anomalous Footsoldier] check inv.", 15, shouldClearPrevious: true);
                    if (spawning_team == SpawnableTeamType.ChaosInsurgency)
                    {
                        RemoveItem(player, ItemType.GunAK);
                        RemoveItem(player, ItemType.GunShotgun);
                        RemoveItem(player, ItemType.GunRevolver);
                        RemoveItem(player, ItemType.GunLogicer);
                        player.ClearInventory(true, false);
                        if (UnityEngine.Random.value > 0.5)
                            AddOrDropFirearm(player, ItemType.GunRevolver, true);
                        else
                            AddOrDropFirearm(player, ItemType.GunShotgun, true);
                    }
                    else if (spawning_team == SpawnableTeamType.NineTailedFox)
                    {
                        RemoveItem(player, ItemType.GunCrossvec);
                        RemoveItem(player, ItemType.GunE11SR);
                        player.ClearInventory(true, false);
                        AddOrDropFirearm(player, ItemType.GunCrossvec, true);
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        switch (UnityEngine.Random.Range(0, 10))
                        {
                            case 0: AddOrDropItem(player, ItemType.SCP018); break;
                            case 1: AddOrDropItem(player, ItemType.SCP1576); break;
                            case 2: AddOrDropItem(player, ItemType.SCP1853); break;
                            case 3: AddOrDropItem(player, ItemType.SCP207); break;
                            case 4: AddOrDropItem(player, ItemType.SCP2176); break;
                            case 5: AddOrDropItem(player, ItemType.SCP244a); break;
                            case 6: AddOrDropItem(player, ItemType.SCP244b); break;
                            case 7: AddOrDropItem(player, ItemType.SCP268); break;
                            case 8: AddOrDropItem(player, ItemType.SCP330); break;
                            case 9: AddOrDropItem(player, ItemType.SCP500); break;
                        }
                    }
                    Timing.KillCoroutines(scp_item_regenerate);
                    scp_item_regenerate = Timing.RunCoroutine(_RegenSCPItems());
                }
            });
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        void OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId newRole, RoleChangeReason reason)
        {
            if (player != null && player.PlayerId == anomalous_footsoldier && newRole == RoleTypeId.Spectator)
            {
                anomalous_footsoldier = -1;
                player.TemporaryData.Remove("custom_class");
            }
        }

        private static IEnumerator<float> _RegenSCPItems()
        {
            while(anomalous_footsoldier != -1)
            {
                bool found = false;
                foreach(var player in Player.GetPlayers())
                {
                    if(player.PlayerId == anomalous_footsoldier && player.IsAlive)
                    {
                        found = true;
                        player.SendBroadcast("gained new anomalous object", 10, shouldClearPrevious: true);
                        switch (UnityEngine.Random.Range(0, 10))
                        {
                            case 0: AddOrDropItem(player, ItemType.SCP018); break;
                            case 1: AddOrDropItem(player, ItemType.SCP1576); break;
                            case 2: AddOrDropItem(player, ItemType.SCP1853); break;
                            case 3: AddOrDropItem(player, ItemType.SCP207); break;
                            case 4: AddOrDropItem(player, ItemType.SCP2176); break;
                            case 5: AddOrDropItem(player, ItemType.SCP244a); break;
                            case 6: AddOrDropItem(player, ItemType.SCP244b); break;
                            case 7: AddOrDropItem(player, ItemType.SCP268); break;
                            case 8: AddOrDropItem(player, ItemType.SCP330); break;
                            case 9: AddOrDropItem(player, ItemType.SCP500); break;
                        }
                    }
                }
                if (!found)
                    break;

                yield return Timing.WaitForSeconds(150.0f);
            }
        }

        public int CompareTo(object obj)
        {
            return Comparer<AnomalousFootsoldier>.Default.Compare(this, obj as AnomalousFootsoldier);
        }
    }
}
