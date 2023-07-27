using MapGeneration;
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
    public class FBI : IComparable
    {
        [PluginEntryPoint("FBI", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        static int respawn_count = 0;
        static HashSet<int> fbi = new HashSet<int>();
        static UnityEngine.Vector3 offset = new UnityEngine.Vector3(-40.021f, -8.119f, -36.140f);
        SpawnableTeamType spawning_team = SpawnableTeamType.None;

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            respawn_count = 0;
            fbi.Clear();
            spawning_team = SpawnableTeamType.None;
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        public void OnRespawn(SpawnableTeamType team)
        {
            spawning_team = team;
            respawn_count++;
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            if (respawn_count >= 3)
            {
                Timing.CallDelayed(0.1f, () =>
                {
                    if (UnityEngine.Random.value < 0.10 &&
                        spawning_team == SpawnableTeamType.NineTailedFox && role.GetTeam() == Team.FoundationForces &&
                        !player.TemporaryData.Contains("custom_class"))
                    {
                        fbi.Add(player.PlayerId);
                        player.TemporaryData.Add("custom_class", this);
                        player.SendBroadcast("[FBI] check inv.", 15, shouldClearPrevious: true);
                        Teleport.RoomPos(player, RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone == FacilityZone.Surface).First(), offset);
                        player.ClearInventory();
                        AddOrDropItem(player, ItemType.KeycardFacilityManager);
                        AddOrDropFirearm(player, ItemType.GunCOM15, true);
                    }
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        void OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId newRole, RoleChangeReason reason)
        {
            if (player != null && fbi.Contains(player.PlayerId) && newRole.GetTeam() != Team.FoundationForces)
            {
                fbi.Remove(player.PlayerId);
                player.TemporaryData.Remove("custom_class");
            }
        }

        public int CompareTo(object obj)
        {
            return Comparer<FBI>.Default.Compare(this, obj as FBI);
        }
    }
}
