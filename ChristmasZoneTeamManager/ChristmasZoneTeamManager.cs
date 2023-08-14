using NWAPIPermissionSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public class ChristmasZoneConfig
    {
        public int TeamLimit { get; set; } = 30;
    }

    public class ChristmasZoneTeamManager
    {
        public static ChristmasZoneTeamManager Singleton { get; private set; }

        public enum Team { None, CZ, CC }

        [PluginConfig]
        public ChristmasZoneConfig config;

        public bool swap_team_spawn = true;

        [PluginEntryPoint("ChristmasZoneTeamManager", "1.0.0", "Christmas Zone Team Manager", "The Riptide")]
        void EntryPoint()
        {
            Singleton = this;
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        public void RandomizeTeamsAssignment()
        {
            swap_team_spawn = Random.value < 0.5f;
        }

        public Team GetTeam(Player player)
        {
            string user_group = ServerStatic.GetPermissionsHandler()._groups.FirstOrDefault(g => g.Value.EqualsTo(player.ReferenceHub.serverRoles.Group)).Key;
            if (user_group.Contains("cz"))
                return Team.CZ;
            if (user_group.Contains("cc"))
                return Team.CC;
            return Team.None;
        }

        public void AssignTeam(Player player, HashSet<int> team_a, HashSet<int> team_b, string info)
        {
            bool team_full = false;
            int count = 0;
            switch(GetTeam(player))
            {
                case Team.CZ:
                    if (swap_team_spawn)
                    {
                        if (team_b.Count >= config.TeamLimit)
                            team_full = true;
                        else
                        {
                            team_b.Add(player.PlayerId);
                            count = team_b.Count;
                        }
                    }
                    else
                    {
                        if (team_a.Count >= config.TeamLimit)
                            team_full = true;
                        else
                        {
                            team_a.Add(player.PlayerId);
                            count = team_a.Count;
                        }
                    }
                    if (!team_full)
                        player.SendBroadcast("<color=#00bbff>Team:</color> <color=#008a70><b>Containment Zone</b></color>\n" + info, 30, shouldClearPrevious: true);
                    else
                        player.SendBroadcast("<color=#FF0000>Could not join </color><color=#00bbff>Team:</color> <color=#09452a><b>Containment Zone</b></color> <color=#FF0000>Because it is full!</color>\n" + info, 30, shouldClearPrevious: true);
                    break;
                case Team.CC:
                    if (swap_team_spawn)
                    {
                        if (team_a.Count >= config.TeamLimit)
                            team_full = true;
                        else
                        {
                            team_a.Add(player.PlayerId);
                            count = team_a.Count;
                        }
                    }
                    else
                    {
                        if (team_b.Count >= config.TeamLimit)
                            team_full = true;
                        else
                        {
                            team_b.Add(player.PlayerId);
                            count = team_b.Count;
                        }
                    }
                    if (!team_full)
                        player.SendBroadcast("<color=#00bbff>Team:</color> <color=#96e9ff><b>Christmas Chaos</b></color>\n" + info, 30, shouldClearPrevious: true);
                    else
                        player.SendBroadcast("<color=#FF0000>Could not join <color=#00bbff>Team:</color> <color=#1d3973><b>Christmas Chaos</b></color> <color=#FF0000>Because it is full!</color>\n" + info, 30, shouldClearPrevious: true);
                    break;
                case Team.None:
                    player.SendBroadcast("<color=#FF0000>[ERROR] Could not assign a team.</color>\n<color=#FFFF00>YOU MUST LINK YOUR STEAM TO DISCORD AND LOG INTO CEDMOD TO JOIN A TEAM. FOLLOW THE INSTRUCTIONS IN SERVER INFO TO JOIN A TEAM</color>\n" + info, 300, shouldClearPrevious: true);
                    break;
            }
        }

        public bool IsTeamACz()
        {
            return !swap_team_spawn;
        }

        public void BroadcastTeamCount(HashSet<int> team_a, HashSet<int> team_b)
        {
            int cz_count, cc_count;
            if (IsTeamACz())
            {
                cz_count = team_a.Count;
                cc_count = team_b.Count;
            }
            else
            {
                cc_count = team_a.Count;
                cz_count = team_b.Count;
            }
            int limit = config.TeamLimit;
            foreach (var p in ReadyPlayers())
                p.SendBroadcast("<color=#008a70><b>Containment Zone " + cz_count + "/" + limit + "</b></color>\n" + "<color=#96e9ff><b>Christmas Chaos " + cc_count + "/" + limit + "</b></color>", 15, shouldClearPrevious: true);
        }
    }
}
