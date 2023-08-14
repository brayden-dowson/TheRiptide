using Mirror;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public class Config
    {
        public ushort Duration { get; set; } = 20;
        public string StartTag { get; set; } = "<b><size=32>";
        public string MostKillsAsScp { get; set; } = "<color=#00b57f>{name}</color> had the most kills as <color=#d11919>{role}</color> with <color=#d11919>{kills}</color> kills";
        public string FirstToKillScp { get; set; } = "<color=#00b57f>{name}</color> was the first to kill a <color=#d11919>SCP</color>";
        public string MostScpsKilled { get; set; } = "<color=#00b57f>{name}</color> killed {scps}";
        public string MostScpsKilledListItem { get; set; } = "<color=#d11919>{scp}</color>";
        public string MostKillsAsHuman { get; set; } = "<color=#00b57f>{name}</color> had the most kills as a human with <color=#d11919>{kills}</color> kills";
        public string FirstToEscape { get; set; } = "<color=#00b57f>{name}</color> was the first to escape in <color=#d11919>{time}</color> as a {role}";
    }

    public class Stats
    {
        public string Name = "";
        public int KillsAsScp = 0;
        public RoleTypeId ScpRole = RoleTypeId.None;
        public List<RoleTypeId> ScpsKilled = new List<RoleTypeId>();
        public float ScpKilledTime = -1;
        public int TotalKills = 0;
        public float EscapeTime = -1;
        public RoleTypeId EscapeRole;

        public Stats(string name)
        {
            Name = name;
        }
    }

    public class MVP
    {
        [PluginConfig]
        public Config config;

        private static bool normal_round;
        private static Dictionary<int,Stats> player_stats = new Dictionary<int, Stats>();
        private static Stopwatch stopwatch = new Stopwatch();

        [PluginEntryPoint("MVP", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player_stats.Add(player.PlayerId, new Stats(player.Nickname));
        }

        [PluginEvent(ServerEventType.WaitingForPlayers)]
        void OnWaitingForPlayers()
        {
            player_stats.Clear();
        }

        [PluginEvent(ServerEventType.RoundStart)]
        public void OnRoundStart()
        {
            normal_round = true;
            Type event_manager_type = GetType("CedMod.Addons.Events.EventManager");
            if (event_manager_type != null && event_manager_type.GetField("CurrentEvent", BindingFlags.Public | BindingFlags.Static).GetValue(null) != null)
                normal_round = false;
            stopwatch.Restart();
        }

        [PluginEvent(ServerEventType.Scp079GainExperience)]
        public void OnScp079GainExperience(Player player, int amount, Scp079HudTranslation reason)
        {
            if (!normal_round)
                return;

            if(reason == Scp079HudTranslation.ExpGainTerminationAssist || reason == Scp079HudTranslation.ExpGainTerminationDirect)
            {
                Stats stats = player_stats[player.PlayerId];
                stats.KillsAsScp++;
                if (stats.ScpRole == RoleTypeId.None)
                    stats.ScpRole = RoleTypeId.Scp079;
            }
        }

        [PluginEvent(ServerEventType.PlayerDying)]
        void OnPlayerDying(Player victim, Player attacker, DamageHandlerBase handler)
        {
            if (!normal_round || victim == null)
                return;

            if(Vector3.Distance(victim.Position, new Vector3(0.0f, -2000.0f, 0.0f)) < 15.0f)
            {
                Player scp106 = null;
                foreach (var p in Player.GetPlayers())
                    if (p.Role == RoleTypeId.Scp106)
                        scp106 = p;
                if(scp106 != null)
                {
                    Stats stats = player_stats[scp106.PlayerId];
                    stats.KillsAsScp++;
                    if (stats.ScpRole == RoleTypeId.None)
                        stats.ScpRole = RoleTypeId.Scp106;
                }
            }

            if(attacker != null)
            {
                Stats stats = player_stats[attacker.PlayerId];
                if (victim.Role.GetTeam() == Team.SCPs && victim.Role != RoleTypeId.Scp0492)
                {
                    stats.ScpsKilled.Add(victim.Role);
                    if (stats.ScpKilledTime < 0)
                        stats.ScpKilledTime = (float)NetworkTime.time;
                }
                if (attacker.Role.GetTeam() != Team.SCPs && attacker.Role.GetFaction() != victim.Role.GetFaction())
                    stats.TotalKills++;
            }
        }

        [PluginEvent(ServerEventType.PlayerDeath)]
        void OnPlayerDied(Player victim, Player attacker, DamageHandlerBase handler)
        {
            if (!normal_round || victim == null)
                return;

            if(attacker != null)
            {
                Stats stats = player_stats[attacker.PlayerId];
                if(attacker.Role.GetTeam() == Team.SCPs)
                {
                    stats.KillsAsScp++;
                    if (stats.ScpRole == RoleTypeId.None)
                        stats.ScpRole = attacker.Role;
                }
            }
        }

        [PluginEvent(ServerEventType.PlayerEscape)]
        void OnPlayerEscape(Player player, RoleTypeId role)
        {
            if (!normal_round)
                return;

            Stats stats = player_stats[player.PlayerId];
            stats.EscapeTime = (float)stopwatch.Elapsed.TotalSeconds;
            stats.EscapeRole = player.Role;
        }

        [PluginEvent(ServerEventType.RoundEnd)]
        void OnRoundEnded(RoundSummary.LeadingTeam leadingTeam)
        {
            if (!normal_round)
                return;

            Stats top_kills_as_scp = null;
            Stats top_scps_killed = null;
            Stats top_scp_killed_time = null;
            Stats top_total_kills = null;
            Stats top_escape_time = null;
            foreach(var s in player_stats.Values)
            {
                if (s.KillsAsScp != 0 && (top_kills_as_scp == null || s.KillsAsScp > top_kills_as_scp.KillsAsScp))
                    top_kills_as_scp = s;

                if (!s.ScpsKilled.IsEmpty() && (top_scps_killed == null || s.ScpsKilled.Count > top_scps_killed.ScpsKilled.Count))
                    top_scps_killed = s;

                if (s.ScpKilledTime > 0 && (top_scp_killed_time == null || s.ScpKilledTime < top_scp_killed_time.ScpKilledTime))
                    top_scp_killed_time = s;

                if (s.TotalKills != 0 && (top_total_kills == null || s.TotalKills > top_total_kills.TotalKills))
                    top_total_kills = s;

                if (s.EscapeTime > 0 && (top_escape_time == null || s.EscapeTime < top_escape_time.EscapeTime))
                    top_escape_time = s;
            }

            string bc = config.StartTag;

            if (top_kills_as_scp != null)
                bc += config.MostKillsAsScp.Replace("{name}", top_kills_as_scp.Name).Replace("{role}", top_kills_as_scp.ScpRole.ToString()).Replace("{kills}", top_kills_as_scp.KillsAsScp.ToString()) + "\n";

            if (top_scps_killed != null)
            {
                if (top_scps_killed.ScpsKilled.Count == 1 && top_scp_killed_time != null)
                    bc += config.FirstToKillScp.Replace("{name}", top_scp_killed_time.Name) + "\n";
                else
                {
                    List<string> roles = new List<string>();
                    foreach (var scp in top_scps_killed.ScpsKilled)
                        roles.Add(config.MostScpsKilledListItem.Replace("{scp}", scp.ToString()));
                    bc += config.MostScpsKilled.Replace("{name}", top_scps_killed.Name).Replace("{scps}", string.Join(", ", roles)) + "\n";
                }
            }

            if (top_total_kills != null)
                bc += config.MostKillsAsHuman.Replace("{name}", top_total_kills.Name).Replace("{kills}", top_total_kills.TotalKills.ToString()) + "\n";

            if (top_escape_time != null)
            {
                TimeSpan ts = new TimeSpan(0, 0, Mathf.RoundToInt(top_escape_time.EscapeTime));
                bc += config.FirstToEscape.
                    Replace("{name}", top_escape_time.Name).
                    Replace("{time}", ts.Minutes + ":" + ts.Seconds.ToString("D2")).
                    Replace("{role}", (top_escape_time.EscapeRole == RoleTypeId.ClassD ? "<color=#ff731c>" : "<color=#fff287>") + top_escape_time.EscapeRole + "</color>");
            }

            //string bc = "<b><size=32>";
            ////int lines = 0;
            //if (top_kills_as_scp != null)
            //    bc += "<color=#00b57f>" + top_kills_as_scp.Name + "</color> had the most kills as <color=#d11919>" + top_kills_as_scp.ScpRole + "</color> with <color=#d11919>" + top_kills_as_scp.KillsAsScp + "</color> kills\n";

            //if(top_scps_killed != null)
            //{
            //    if (top_scps_killed.ScpsKilled.Count == 1 && top_scp_killed_time != null)
            //        bc += "<color=#00b57f>" + top_scp_killed_time.Name + "</color> was the first to kill a <color=#d11919>SCP</color>\n";
            //    else
            //    {
            //        bc += "<color=#00b57f>" + top_scps_killed.Name + "</color> killed ";
            //        List<string> roles = new List<string>();
            //        foreach (var scp in top_scps_killed.ScpsKilled)
            //            roles.Add("<color=#d11919>" + scp + "</color>");
            //        bc += string.Join(", ", roles) + "\n";
            //    }
            //}

            //if(top_total_kills != null)
            //    bc += "<color=#00b57f>" + top_total_kills.Name + "</color> had the most kills as a human with <color=#d11919>" + top_total_kills.TotalKills + "</color> kills\n";

            //if(top_escape_time != null)
            //{
            //    TimeSpan ts = new TimeSpan(0, 0, Mathf.RoundToInt(top_escape_time.EscapeTime));
            //    bc += "<color=#00b57f>" + top_escape_time.Name + "</color> was the first to escape in <color=#d11919>" + ts.Minutes + ":" + ts.Seconds.ToString("D2") + "</color> as a " + (top_escape_time.EscapeRole == RoleTypeId.ClassD ? "<color=#ff731c>" : "<color=#fff287>") + top_escape_time.EscapeRole + "</color>";
            //}

            foreach (var p in Player.GetPlayers())
                if (p.Role != RoleTypeId.None)
                    p.SendBroadcast(bc, config.Duration, shouldClearPrevious: true);
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
