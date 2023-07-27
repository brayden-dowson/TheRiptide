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
    public class NtfDemolitionist:IComparable
    {
        [PluginEntryPoint("Ntf Demolitionist", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        static HashSet<int> demolitionists = new HashSet<int>();
        static bool spawning_ntf = false;

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            demolitionists.Clear();
            spawning_ntf = false;
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        public void OnRespawn(SpawnableTeamType team)
        {
            spawning_ntf = team == SpawnableTeamType.NineTailedFox;
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            Timing.CallDelayed(0.1f, () =>
            {
                if (spawning_ntf && role.GetTeam() == Team.FoundationForces && UnityEngine.Random.value < 0.1)
                {
                    demolitionists.Add(player.PlayerId);
                    player.TemporaryData.Add("custom_class", this);
                    player.SendBroadcast("[Demolitionist] check inv.", 15, shouldClearPrevious: true);
                    RemoveItem(player, ItemType.Medkit);
                    AddOrDropItem(player, ItemType.GrenadeHE);
                    AddOrDropItem(player, ItemType.GrenadeHE);
                    AddOrDropItem(player, ItemType.GrenadeHE);
                }
            });
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        void OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId newRole, RoleChangeReason reason)
        {
            if (player != null && demolitionists.Contains(player.PlayerId) && newRole.GetTeam() != Team.FoundationForces)
            {
                demolitionists.Remove(player.PlayerId);
                player.TemporaryData.Remove("custom_class");
            }
        }

        public int CompareTo(object obj)
        {
            return Comparer<NtfDemolitionist>.Default.Compare(this, obj as NtfDemolitionist);
        }
    }
}
