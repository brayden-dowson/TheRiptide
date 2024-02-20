using Achievements;
using CommandSystem;
using HarmonyLib;
using MEC;
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
        [Description("Duration of broadcast, might need to be increased if round end time set in config is longer")]
        public ushort Duration { get; set; } = 30;
        [Description("Set text format shared between stats with tags and display text you want to appear at the top")]
        public string Start { get; set; } = "<size=31><line-height=0.9em>";
        [Description("Format used for each stat, if set to no text/empty the stat will not be displayed")]
        public string MostKillsAsScp { get; set; } = "<color=#78e2ff><b>{name}</b></color> had the most kills as <color=#ff0000><b>{role}</b></color> with <color=#45ff7a><b>{kills}</b></color> kills";
        public string FirstToKillScp { get; set; } = "<color=#78e2ff><b>{name}</b></color> was the first to kill a <color=#ff0000><b>SCP</b></color>";
        [Description("Replaces first_to_kill_scp if player has more than one kill")]
        public string MostScpsKilled { get; set; } = "<color=#78e2ff><b>{name}</b></color> killed {scps}";
        public string MostScpsKilledListItem { get; set; } = "<color=#ff0000><b>{scp}</b></color>";
        public string MostKillsAsHuman { get; set; } = "<color=#78e2ff><b>{name}</b></color> had the most kills as a human with <color=#45ff7a><b>{kills}</b></color> kills";
        public string FirstToEscape { get; set; } = "<color=#78e2ff><b>{name}</b></color> was the first to escape in <color=#45ff7a><b>{time}</b></color> as a <b>{role}</b>";
        public string BestAchievement { get; set; } = "<color=#78e2ff><b>{name}</b></color> achieved <color=#45ff7a><b>{achievement}</b></color> - {description}";
        [Description("Close out tags from the Start and display text you want to appear at the bottom")]
        public string End { get; set; } = "</line-height></size>";

        [Description("Achievements to be tracked during the round. Order of achievements determine priority, ones closer to the top override lower ones. contains all valid achievements by default(christmas, halloween and They Are Just Resources... are missing due to techinal reasons)")]
        public List<AchievementName> Achievements { get; set; } = new List<AchievementName>
        {
            AchievementName.BePoliteBeEfficient,
            AchievementName.ChangeInCommand,
            AchievementName.Overcurrent,
            AchievementName.Escape207,
            AchievementName.TurnThemAll,
            AchievementName.AccessGranted,
            AchievementName.CrisisAverted,
            AchievementName.DidntEvenFeelThat,
            AchievementName.MicrowaveMeal,
            AchievementName.EscapeArtist,
            AchievementName.Pacified,
            AchievementName.IllPassThanks,
            AchievementName.ForScience,
            AchievementName.FireInTheHole,
            AchievementName.ItsAlwaysLeft,
            AchievementName.SecureContainProtect,
            AchievementName.IsThisThingOn,
            AchievementName.WalkItOff,
            AchievementName.Friendship,
            AchievementName.HeWillBeBack,
            AchievementName.ExecutiveAccess,
            AchievementName.AnomalouslyEfficient,
            AchievementName.MelancholyOfDecay,
            AchievementName.ThatCanBeUseful,
            AchievementName.TMinus,
            AchievementName.ProceedWithCaution,
            AchievementName.DontBlink,
            AchievementName.DeltaCommand,
            AchievementName.LightsOut,

        };

        public Dictionary<AchievementName, string> AchievementDescriptions { get; set; } = new Dictionary<AchievementName, string>
        {
            { AchievementName.LightsOut, "Respawned as Nine-Tailed Fox" },
            { AchievementName.DeltaCommand, "Respawned as Chaos Insurgency" },
            { AchievementName.DontBlink, "Successfully evaded <color=#ff0000>SCP-173</color>" },
            { AchievementName.ProceedWithCaution, "Successfully passed through a Tesla gate that <color=#ff0000>SCP-079</color> was watching" },
            { AchievementName.TMinus, "Survived a successful Alpha Warhead Detonation" },
            { AchievementName.ThatCanBeUseful, "Find any gun as a <color=#FF8000>Class-D</color>" },
            { AchievementName.MelancholyOfDecay, "Captured a player within five seconds of emerging from the ground as <color=#ff0000>SCP-106</color>" },
            { AchievementName.AnomalouslyEfficient, "Killed a player in the first minute of the game as an <color=#ff0000>SCP</color>" },
            { AchievementName.ExecutiveAccess, "Obtained a max-level keycard" },
            { AchievementName.HeWillBeBack, "Successfully escaped from the Pocket Dimension" },
            { AchievementName.Friendship, "As a <color=#FFFF7C>Scientist</color>, successfully upgraded their keycard alongside <color=#FF8000>Class-D's</color>" },
            { AchievementName.WalkItOff, "Survived a fall with less than half of their health remaining" },
            { AchievementName.IsThisThingOn, "Broadcasted a 'helpful' message via the Intercom" },
            { AchievementName.SecureContainProtect, "killed the final <color=#ff0000>SCP</color> in the round as a MTF" },
            { AchievementName.ItsAlwaysLeft, "Escaped as <color=#FF8000>Class-D</color> personnel" },
            { AchievementName.FireInTheHole, "Killed an enemy using a grenade" },
            { AchievementName.ForScience, "Escaped as a <color=#FFFF7C>Scientist</color>" },
            { AchievementName.IllPassThanks, "Killed someone who was actively using the Micro H.I.D as an <color=#ff0000>SCP</color>" },
            { AchievementName.Pacified, "Killed <color=#ff0000>SCP-096</color> while it was entering its rage" },
            { AchievementName.EscapeArtist, "Was the first to escape the Facility" },
            { AchievementName.MicrowaveMeal, "Killed an <color=#ff0000>SCP</color> with the Micro H.I.D" },
            { AchievementName.DidntEvenFeelThat, "Used adrenaline to survive a hit that would otherwise killed them" },
            { AchievementName.CrisisAverted, "Used SCP-500 when they were about to die" },
            { AchievementName.AccessGranted, "Killed a <color=#FFFF7C>Scientist</color> holding a keycard as a <color=#FF8000>Class-D</color>" },
            { AchievementName.TurnThemAll, "Cured ten people as <color=#ff0000>SCP-049</color>" },
            { AchievementName.Escape207, "Escaped while under the effects of SCP-207" },
            { AchievementName.SomethingDoneRight, "Killed an SCP as a <color=#FFFF7C>Scientist</color>" },
            { AchievementName.PropertyOfChaos, "Escaped with more than two SCP objects, as a <color=#FF8000>Class-D</color>" },
            { AchievementName.ThatWasClose, "Canceled the Alpha Warhead detonation in the last 15 seconds"},
            { AchievementName.Overcurrent, "Tried to recharge the Micro H.I.D" },
            { AchievementName.ChangeInCommand, "Disarmed an MTF operative" },
            { AchievementName.BePoliteBeEfficient, "Killed five enemies in less than 30 seconds" }
        };

        public Dictionary<AchievementName, string> AchievementNames { get; set; } = new Dictionary<AchievementName, string>
        {
            { AchievementName.LightsOut, "Lights Out" },
            { AchievementName.DeltaCommand, "We of Delta Command..." },
            { AchievementName.DontBlink, "Don’t Blink" },
            { AchievementName.ProceedWithCaution, "Proceed With Caution" },
            { AchievementName.TMinus, "T-Minus 90 seconds..." },
            { AchievementName.ThatCanBeUseful, "... You Thinking What I'm Thinking?" },
            { AchievementName.MelancholyOfDecay, "Melancholy of Decay" },
            { AchievementName.AnomalouslyEfficient, "Anomalously Efficient" },
            { AchievementName.ExecutiveAccess, "Executive Access" },
            { AchievementName.HeWillBeBack, "He’ll Be Back..." },
            { AchievementName.Friendship, "Friendship" },
            { AchievementName.WalkItOff, "Walk It Off" },
            { AchievementName.IsThisThingOn, "Is This Thing On?" },
            { AchievementName.SecureContainProtect, "Secure. Contain. Protect." },
            { AchievementName.ItsAlwaysLeft, "It's Always Left, Brothers!" },
            { AchievementName.FireInTheHole, "Fire In The Hole!" },
            { AchievementName.ForScience, "For Science!" },
            { AchievementName.IllPassThanks,  "I'll Pass, Thanks" },
            { AchievementName.Pacified, "Pacified" },
            { AchievementName.EscapeArtist, "Escape Artist" },
            { AchievementName.MicrowaveMeal, "Microwave Meal" },
            { AchievementName.DidntEvenFeelThat, "Ha! I didn't even feel that!" },
            { AchievementName.CrisisAverted, "Crisis Averted" },
            { AchievementName.AccessGranted, "Access Granted" },
            { AchievementName.TurnThemAll, "My Cure Is Most Effective..." },
            { AchievementName.Escape207, "High on the Wings of Caffeine" },
            { AchievementName.SomethingDoneRight, "If you want something done right..."},
            { AchievementName.PropertyOfChaos, "Property of the Chaos Insurgency" },
            { AchievementName.ThatWasClose, "That was... close." },
            { AchievementName.Overcurrent, "Overcurrent" },
            { AchievementName.ChangeInCommand, "Change in Command" },
            { AchievementName.BePoliteBeEfficient, "Be Polite. Be Efficient." }
        };
    }

    public class Stats
    {
        public string Name = "";
        public int PlayerId = 0;
        public string UserId = "";
        public bool DNT;

        public int KillsAsScp = 0;
        public RoleTypeId ScpRole = RoleTypeId.None;
        public List<RoleTypeId> ScpsKilled = new List<RoleTypeId>();
        public float ScpKilledTime = -1;
        public int TotalKills = 0;
        public float EscapeTime = -1;
        public RoleTypeId EscapeRole;
        public AchievementName? Achievement = null;

        public Stats(Player player)
        {
            Name = player.Nickname;
            PlayerId = player.PlayerId;
            UserId = player.UserId;
            DNT = player.DoNotTrack;
        }
    }

    public enum Event
    {
        RoundEnd,
        NextJoin,
        NextRoundStart,
    }

    public enum Stat
    {
        MostKillsAsScp,
        FirstToKillScp,
        MostScpsKilled,
        MostKillsAsHuman,
        FirstToEscape,
        BestAchievement
    }

    public class EventCommand
    {
        public Stat Stat { get; set; }
        public Event Event { get; set; }
        public float Delay { get; set; }
        public List<string> Commands { get; set; } = new List<string>();
    }

    public class EventCommandConfig
    {
        [Description("Will log all commands when enabled")]
        public bool Debug { get; set; } = true;

        [Description(@"____________________________________________________________________________________________________________________________________________________________
# A system to execute commands based on the players that were an MVP, Stat represents the MVP message you want to execute the commands for and Event is when you want it to execute
# Each EventCommand must specify the Stat, Event, Delay and a list of Commands to execute
# Valid Stats are MostKillsAsScp, FirstToKillScp, MostScpsKilled, MostKillsAsHuman, FirstToEscape and BestAchievement
# Valid Events are RoundEnd, NextJoin and NextRoundStart
# Delay might be necessary in certain cases when variables are not set yet. Setting the delay to -1 will execute the command with no delay, for a 1 frame delay use 0 for the delay
# Commands are executed in the Server Console by default to execute it as a Remote Admin command you must start your command with a '/'
# Commands support Command Interpolation https://en.scpslgame.com/index.php?title=Command_Interpolation which can be used to perform logic(see example below)
# Additional MVP specific interpolation is provided
#   %name% - get the name of the player. this is the only variable to bypass the Command Interpolation for safety reasons i.e. you cannot perform any logic with the %name% variable unlike all the others. dynamic value which changes when the player leaves/rejoins should always point to the same user.
#   {id} - the player_id of the player. if used in the command the command will not be executed if the player is not in the server. dynamic value which changes when the player leaves/rejoins garunteed to always point to the same user.
#   {user_id} - the user_id of the player. unlike {id}, it will not prevent the command from executing as user_ids are expected to work for offline players
#   {dnt} - true/false value indicating if the player has Do Not Track enabled or not. use this with command interpolation to stay within the VSR. dynamic value which changes when the player leaves/rejoins garunteed to always be up to date.
#   Stat related variables {stat_kills_as_scp} {stat_scp_role} {stat_scps_killed} {stat_scp_killed_time} {stat_kills_as_human} {stat_escape_time} {stat_escape_role} {stat_best_achievement}
#   Player related variables {ip} {role} {team} {faction} {hp} {stamina} {ahp} {alive} {human} {scp}
# You can test commands by using the mvp_event_command in the Remote Admin(requires server console permission) aliases - mec. e.g. ""mec /pbc {id} 5 %name% used command to make a private broadcast""
#
# Example List
# 1. sends a broadcast to everyone doxing the player that had the most kills as a human last round only if they had more than 5 total kills(Dont use this unless you want your server to be delisted, you shouldnt need me to tell you this)
# 2. sends a private broadcast to the player when they join if they had the most kills as an scp last round
# 3. sends a private broadcast and gives scp207 if they spawn as a human and had the most kills as an scp last round
#event_commands:
#- stat: MostKillsAsHuman
#  event: NextRoundStart
#  delay: 10
#  commands:
#  - '{if,{greater,{stat_kills_as_human},5},/bc 1 haha get doxed idiot. <color=#00FF00>%name%</color>: <color=#FF0000>{ip}</color>}'
#- stat: MostKillsAsScp
#  event: NextJoin
#  delay: 1
#  commands:
#  - /pbc {id} 5 You had most kills as SCP last match so will spawn with an item this game if you spawn as a human role
#- stat: MostKillsAsScp
#  event: NextRoundStart
#  delay: 1
#  commands:
#  - '{if,{human},/give {id} 18}'
#  - '{if,{human},/pbc {id} 5 you spawned with scp207!}'
# ____________________________________________________________________________________________________________________________________________________________")]
        public List<EventCommand> EventCommands { get; set; } = new List<EventCommand>
        {
        };
    }

    public class MVP
    {
        public static MVP Instance { get; private set; } = null;

        [PluginConfig]
        public Config config;

        [PluginConfig("commands.yml")]
        public EventCommandConfig commands;

        private static bool normal_round;
        private static Dictionary<int, Stats> player_stats = new Dictionary<int, Stats>();
        private static Stopwatch stopwatch = new Stopwatch();
        private Harmony harmony = new Harmony("TheRiptide.MVP");

        private Dictionary<Event, List<EventCommand>> event_commands = new Dictionary<Event, List<EventCommand>>();
        private Dictionary<Stat, Stats> previous_stats = new Dictionary<Stat, Stats>();

        [PluginEntryPoint("MVP", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            Instance = this;
            PluginAPI.Events.EventManager.RegisterEvents(this);
            harmony.PatchAll();
            foreach(var e in commands.EventCommands)
            {
                if (event_commands.TryGetValue(e.Event, out List<EventCommand> commands))
                    commands.Add(e);
                else
                    event_commands.Add(e.Event, new List<EventCommand> { e });
            }
            RegisterAllValues();
            Log.Info("The Riptide's MVP enabled");
        }

        [PluginUnload]
        public void OnDisabled()
        {
            harmony.UnpatchAll("TheRiptide.MVP");
            PluginAPI.Events.EventManager.UnregisterEvents(this);
            Instance = null;
            event_commands.Clear();
            Log.Info("The Riptide's MVP disabled");
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            if (player_stats.ContainsKey(player.PlayerId))
                player_stats[player.PlayerId] = new Stats(player);
            else
                player_stats.Add(player.PlayerId, new Stats(player));

            var result = previous_stats.FirstOrDefault(p => p.Value.UserId == player.UserId);
            if (result.Value != null)
            {
                result.Value.Name = player.Nickname;
                result.Value.PlayerId = player.PlayerId;
                result.Value.DNT = player.DoNotTrack;
            }
            
            RunEventCommands(Event.NextJoin);
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

            RunEventCommands(Event.NextRoundStart);
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
            Stats top_achievement = null;
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

                if (s.Achievement != null && (top_achievement == null || config.Achievements.IndexOf(s.Achievement.Value) < config.Achievements.IndexOf(top_achievement.Achievement.Value)))
                    top_achievement = s;
            }

            string bc = config.Start;

            if (top_kills_as_scp != null && !string.IsNullOrEmpty(config.MostKillsAsScp))
                bc += config.MostKillsAsScp.Replace("{name}", top_kills_as_scp.Name).Replace("{role}", top_kills_as_scp.ScpRole.ToString()).Replace("{kills}", top_kills_as_scp.KillsAsScp.ToString()) + "\n";

            if (top_scps_killed != null && !(string.IsNullOrEmpty(config.FirstToKillScp) && string.IsNullOrEmpty(config.MostScpsKilled)))
            {
                if ((top_scps_killed.ScpsKilled.Count == 1 || string.IsNullOrEmpty(config.MostScpsKilled)) && top_scp_killed_time != null && !string.IsNullOrEmpty(config.FirstToKillScp))
                    bc += config.FirstToKillScp.Replace("{name}", top_scp_killed_time.Name) + "\n";
                else if(!string.IsNullOrEmpty(config.MostScpsKilled))
                {
                    List<string> roles = new List<string>();
                    foreach (var scp in top_scps_killed.ScpsKilled)
                        roles.Add(config.MostScpsKilledListItem.Replace("{scp}", scp.ToString()));
                    bc += config.MostScpsKilled.Replace("{name}", top_scps_killed.Name).Replace("{scps}", string.Join(", ", roles)) + "\n";
                }
            }

            if (top_total_kills != null && !string.IsNullOrEmpty(config.MostKillsAsHuman))
                bc += config.MostKillsAsHuman.Replace("{name}", top_total_kills.Name).Replace("{kills}", top_total_kills.TotalKills.ToString()) + "\n";

            if (top_escape_time != null && !string.IsNullOrEmpty(config.FirstToEscape))
            {
                TimeSpan ts = new TimeSpan(0, 0, Mathf.RoundToInt(top_escape_time.EscapeTime));
                bc += config.FirstToEscape.
                    Replace("{name}", top_escape_time.Name).
                    Replace("{time}", ts.Minutes + ":" + ts.Seconds.ToString("D2")).
                    Replace("{role}", (top_escape_time.EscapeRole == RoleTypeId.ClassD ? "<color=#ff731c>" : "<color=#fff287>") + top_escape_time.EscapeRole + "</color>") + "\n";
            }

            if (top_achievement != null && !string.IsNullOrEmpty(config.BestAchievement))
                bc += config.BestAchievement.
                    Replace("{name}", top_achievement.Name).
                    Replace("{achievement}", config.AchievementNames.TryGetValue(top_achievement.Achievement.Value, out string name) ? name : $"<color=#FF0000>ERROR ACHIEVEMENT NAME TRANSLATION MISSING FOR {top_achievement.Achievement.Value}</color>").
                    Replace("{description}", config.AchievementDescriptions.TryGetValue(top_achievement.Achievement.Value, out string description) ? description : $"<color=#FF0000>ERROR ACHIEVEMENT DESCRIPTION TRANSLATION MISSING FOR {top_achievement.Achievement.Value}</color>");

            bc += config.End;

            foreach (var p in Player.GetPlayers())
                if (p.IsReady)
                    p.SendBroadcast(bc, config.Duration, shouldClearPrevious: true);

            //command events
            previous_stats.Clear();
            if (top_kills_as_scp != null)
                previous_stats[Stat.MostKillsAsScp] = top_kills_as_scp;

            if(top_scps_killed != null)
            {
                if (top_scps_killed.ScpsKilled.Count == 1 && top_scp_killed_time != null)
                    previous_stats[Stat.FirstToKillScp] = top_scp_killed_time;
                else
                    previous_stats[Stat.MostScpsKilled] = top_scps_killed;
            }

            if (top_total_kills != null)
                previous_stats[Stat.MostKillsAsHuman] = top_total_kills;

            if (top_escape_time != null)
                previous_stats[Stat.FirstToEscape] = top_escape_time;

            if (top_achievement != null)
                previous_stats[Stat.BestAchievement] = top_achievement;

            RunEventCommands(Event.RoundEnd);
        }

        public void OnPlayerAchieve(Player player, AchievementName achievement)
        {
            if (config.Achievements.Contains(achievement))
            {
                Stats stats = player_stats[player.PlayerId];
                if (stats.Achievement == null || config.Achievements.IndexOf(stats.Achievement.Value) > config.Achievements.IndexOf(achievement))
                    stats.Achievement = achievement;
            }
        }

        //mec /pbc {id} 10 {name} {id} {user_id} {dnt} {stat_kills_as_scp} {stat_scp_role} {stat_scps_killed} {stat_scp_killed_time} {stat_kills_as_human} {stat_escape_time} {stat_escape_role} {stat_best_achievement}
        //mec /pbc {id} 10 {ip} {role} {team} {faction} {hp} {stamina} {ahp} {alive} {human} {scp}
        //mec /pbc {id} 10 {round_started} {round_time_elasped} {player_count} {max_players} {scp_count} {human_count} {mtf_count} {chaos_count} {classd_count} {scientist_count} {alive_count} {dead_count}
        public void RegisterAllValues()
        {
            //Register("{name}", s => s.Name);
            Register("{id}", s => s.PlayerId.ToString());
            Register("{user_id}", s => s.UserId);
            Register("{dnt}", s => s.DNT.ToString());

            Register("{stat_kills_as_scp}", s => s.KillsAsScp.ToString());
            Register("{stat_scp_role}", s => s.ScpRole.ToString());
            Register("{stat_scps_killed}", s => string.Join(", ", s.ScpsKilled));
            Register("{stat_scp_killed_time}", s => s.ScpKilledTime.ToString());
            Register("{stat_kills_as_human}", s => s.TotalKills.ToString());
            Register("{stat_escape_time}", s => s.EscapeTime.ToString());
            Register("{stat_escape_role}", s => s.EscapeRole.ToString());
            Register("{stat_best_achievement}", s => s.Achievement == null ? "none" : s.Achievement.Value.ToString());

            Register("{ip}", s => Player.TryGet(s.UserId, out Player p) ? p.IpAddress : "0.0.0.0");
            Register("{role}", s => Player.TryGet(s.UserId, out Player p) ? p.Role.ToString() : RoleTypeId.None.ToString());
            Register("{team}", s => Player.TryGet(s.UserId, out Player p) ? p.Team.ToString() : Team.Dead.ToString());
            Register("{faction}", s => Player.TryGet(s.UserId, out Player p) ? p.Team.GetFaction().ToString() : Faction.Unclassified.ToString());
            Register("{hp}", s => Player.TryGet(s.UserId, out Player p) ? p.Health.ToString() : "0");
            Register("{stamina}", s => Player.TryGet(s.UserId, out Player p) ? p.StaminaRemaining.ToString() : "0");
            Register("{ahp}", s => Player.TryGet(s.UserId, out Player p) ? p.ArtificialHealth.ToString() : "0");
            Register("{alive}", s => Player.TryGet(s.UserId, out Player p) ? p.IsAlive.ToString() : "false");
            Register("{human}", s => Player.TryGet(s.UserId, out Player p) ? p.IsHuman.ToString() : "false");
            Register("{scp}", s => Player.TryGet(s.UserId, out Player p) ? p.IsSCP.ToString() : "false");

            Register("{round_started}", s => Round.IsRoundStarted.ToString());
            //Register("{round_time_elasped}", s => Round.Duration.TotalSeconds.ToString());
            //Register("{player_count}", s => Player.Count.ToString());
            //Register("{max_players}", s => Server.MaxPlayers.ToString());
            //Register("{scp_count}", s => Player.GetPlayers().Count(p => p.IsSCP).ToString());
            //Register("{human_count}", s => Player.GetPlayers().Count(p => p.IsHuman).ToString());
            //Register("{mtf_count}", s => Player.GetPlayers().Count(p => p.IsNTF).ToString());
            //Register("{chaos_count}", s => Player.GetPlayers().Count(p => p.IsChaos).ToString());
            //Register("{classd_count}", s => Player.GetPlayers().Count(p => p.Role == RoleTypeId.ClassD).ToString());
            //Register("{scientist_count}", s => Player.GetPlayers().Count(p => p.Role == RoleTypeId.Scientist).ToString());
            //Register("{alive_count}", s => Player.GetPlayers().Count(p => p.IsAlive).ToString());
            //Register("{dead_count}", s => Player.GetPlayers().Count(p => !p.IsAlive).ToString());
        }

        public void RunEventCommands(Event e)
        {
            if (!normal_round || !event_commands.TryGetValue(e, out List<EventCommand> commands))
                return;

            foreach(var command in commands)
            {
                if (!previous_stats.TryGetValue(command.Stat, out Stats stats))
                    return;

                if (command.Delay < 0.0f)
                    ExecuteEventCommands(command, stats);
                else
                    Timing.CallDelayed(command.Delay, () => ExecuteEventCommands(command, stats));   
            }
        }

        public void ExecuteEventCommands(EventCommand e, Stats s)
        {
            foreach (var cmd in e.Commands)
            {
                string r = "";
                try
                {
                    if (cmd.Contains("{id}") && (!Player.TryGet(s.PlayerId, out Player player) || player.UserId != s.UserId))
                        continue;
                    if (commands.Debug)
                        Log.Info($"Parsing command(do not copy): {cmd.Replace("<", "＜").Replace(">", "＞")}");
                    r = FastStringInterpolation(cmd, s).Replace("%name%", s.Name);
                    if(!string.IsNullOrEmpty(r))
                    {
                        if (commands.Debug)
                            Log.Info($"Executing commmand(do not copy): {r.Replace("<", "＜").Replace(">", "＞")}");
                        Server.RunCommand(r);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"cmd: \"{cmd}\" stat: \"{e.Stat}\" event: \"{e.Event}\" delay: \"{e.Delay}\" error: " + ex.ToString(), "Event Command");
                }
            }
        }

        public class Node
        {
            public Func<Stats, string> func = null;
            public Dictionary<char, Node> children = new Dictionary<char, Node>();
        }

        private Node trie = new Node();

        public void Register(string key, Func<Stats, string> value)
        {
            Node n = trie;
            foreach(var c in key)
            {
                if (n.children.TryGetValue(c, out Node child))
                    n = child;
                else
                {
                    var new_child = new Node();
                    n.children.Add(c, new_child);
                    n = new_child;
                }
            }
            n.func = value;
        }

        public string FastStringInterpolation(string s, Stats stats)
        {
            List<string> split = new List<string>();
            List<string> values = new List<string>();
            int start = 0;
            int i = 0;
            for (; i < s.Length; i++)
            {
                int j = 0;
                Node n = trie;
                while (s.Length > i + j && n.children.TryGetValue(s[i + j], out Node child))
                {
                    n = child;
                    j++;
                }
                if(n.func != null)
                {
                    split.Add(s.Substring(start, i - start));
                    i += j - 1;
                    start = i + 1;
                    values.Add(n.func(stats));
                }
            }
            split.Add(s.Substring(start, i - start));
            List<string> result = new List<string>();
            for(int k = 0; k < split.Count; k ++)
            {
                result.Add(split[k]);
                if (k < values.Count)
                    result.Add(values[k]);
            }
            string cmd = string.Join("", result);
            if (commands.Debug)
                Log.Info($"Post MVP interpolation(do not copy): {cmd.Replace("<", "＜").Replace(">", "＞")}");
            if (!ServerConsole.singleton.NameFormatter.TryProcessExpression(cmd, "MVP Event Command Handler", out cmd))
            {
                Log.Error(cmd);
                return "";
            }
            else if (commands.Debug)
                Log.Info($"Post command interpolation(do not copy): {cmd.Replace("<", "＜").Replace(">", "＞")}");
            return cmd;
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

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class MVPEventCommand : ICommand
    {
        public string Command { get; } = "mvp_event_command";

        public string[] Aliases { get; } = new string[] { "mec" };

        public string Description { get; } = "Test a MVP event command. See commands.yml for all variables. Usage: mec <command>";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if(!Player.TryGet(sender, out Player player))
            {
                response = "You must be a player to execute this command";
                return false;
            }

            if (arguments.Count < 1)
            {
                response = "To execute this command provide at least 1 argument!\nUsage: mec <command>";
                return false;
            }

            if (!sender.CheckPermission(PlayerPermissions.ServerConsoleCommands))
            {
                response = "No permission requires ServerConsoleCommands";
                return false;
            }

            string cmd = string.Join(" ", arguments);
            MVP.Instance.ExecuteEventCommands(new EventCommand
            {
                Stat = Stat.BestAchievement,
                Event = Event.RoundEnd,
                Delay = -1,
                Commands = new List<string> { cmd }
            },
            new Stats(player));

            response = "executing... check server console for errors";
            return true;
        }
    }
}
