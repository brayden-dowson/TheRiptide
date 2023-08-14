using MEC;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public class ChristmasZoneLobby
    {
        private static bool normal_round;

        HashSet<int> team_a = new HashSet<int>();
        HashSet<int> team_b = new HashSet<int>();

        [PluginEntryPoint("Christmas Zone Lobby", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            if (!normal_round)
                return;

            string info = "<color=#ffff00>Normal round</color>";
            ChristmasZoneTeamManager.Singleton.AssignTeam(player, team_a, team_b, info);
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            if (!normal_round)
                return;

            team_a.Remove(player.PlayerId);
            team_b.Remove(player.PlayerId);
        }

        [PluginEvent(ServerEventType.WaitingForPlayers)]
        void OnWaitingForPlayers()
        {
            team_a.Clear();
            team_b.Clear();

            Timing.CallDelayed(0.0f,()=>
            {
                normal_round = true;
                Type event_manager_type = GetType("CedMod.Addons.Events.EventManager");
                if (event_manager_type != null && event_manager_type.GetField("CurrentEvent", BindingFlags.Public | BindingFlags.Static).GetValue(null) != null)
                    normal_round = false;
            });
        }

        [PluginEvent(ServerEventType.RoundStart)]
        public void OnRoundStart()
        {
            if (!normal_round)
                return;

            ChristmasZoneTeamManager.Singleton.BroadcastTeamCount(team_a, team_b);
        }

        public static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
