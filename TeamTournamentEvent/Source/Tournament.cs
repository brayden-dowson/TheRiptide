using InventorySystem.Items.Usables.Scp330;
using MEC;
using PlayerRoles;
using PluginAPI.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public enum TeamState
    {
        None,
        BanSelection,
        LoadoutSelection,
        Fighting
    }

    public class Team
    {
        public string TeamName;
        public string BadgeName;
        public string BadgeColor = "<color=#808080>";
        public HashSet<string> Users = new HashSet<string>();

        public List<ItemType> ItemBans = new List<ItemType>();
        public List<CandyKindID> CandyBans = new List<CandyKindID>();
        public List<Zone> ZoneBans = new List<Zone>();

        public TeamState State = TeamState.None;
    }

    enum TournamentMode
    {
        Role,
        Predefined,
        Scrimmage
    }

    class Tournament
    {
        private Config config;
        private Log log;
        private TeamCache cache;
        private Dictionary<string, Team> teams = new Dictionary<string, Team>();
        private List<Match> matches = new List<Match>();
        private Bracket bracket = new Bracket();
        private CoroutineHandle auto_run;
        private CoroutineHandle update;
        public TournamentMode mode { get; private set; } = TournamentMode.Role;
        private object plugin;

        public void Start(object plugin, Config config, Log log, TeamCache cache)
        {
            this.plugin = plugin;
            this.config = config;
            this.log = log;
            this.cache = cache;
            //brackets.BuildStanding(config.Teams.Keys.ToList(), log);
            StandingDisplay.UpdateStanding(bracket.GetCurrentStandingHint());
            update = Timing.RunCoroutine(_Update());
        }

        public void Stop()
        {
            plugin = null;
            config = null;
            cache = null;
            StandingDisplay.Stop();
            Timing.KillCoroutines(auto_run, update);
        }

        public void SaveLog()
        {
            PluginHandler.Get(plugin).SaveConfig(plugin, "log");
        }

        public void SaveTeamCache()
        {
            PluginHandler.Get(plugin).SaveConfig(plugin, "team_cache");
        }

        public void AutoRun()
        {
            auto_run = Timing.RunCoroutine(_AutoRun());
        }

        public bool TryRunMatch(string team)
        {
            if (matches.Count >= 4)
                return false;

            Bracket.Node node = bracket.FindMatch(team);
            if (node == null)
                return false;

            StartMatch(node);
            return true;
        }

        public bool TryForceWin(Player player, string team_name)
        {
            Team winner;
            if (!teams.TryGetValue(team_name, out winner))
                return false;

            Bracket.Node node = bracket.FindMatch(team_name);
            if (node == null)
                return false;

            Team loser = node.team_a.winner == winner ? node.team_b.winner : node.team_a.winner;
            OnTeamWon(winner, loser, 0, 0, "\nForced win by admin " + player.Nickname);
            return true;
        }

        public bool TryUndoWin(Player player, string team, string reason)
        {
            Bracket.Node node = bracket.FindMatch(team);
            if (node == null || (node.team_a == null && node.team_b == null))
                return false;

            MatchResult result = log.Results.LastOrDefault(m => m.valid && m.winner == team);
            if (result == null)
                return false;

            result.valid = false;
            result.reason += ". Invalidated by " + player.Nickname + " reason: " + reason;
            if (node.winner != null && node.winner.TeamName == team)
                node.winner = null;
            else if (node.team_a.winner.TeamName == team)
                node.team_a.winner = null;
            else if (node.team_b.winner.TeamName == team)
                node.team_b.winner = null;
            StandingDisplay.UpdateStanding(bracket.GetCurrentStandingHint());
            SaveLog();
            return true;
        }

        public void SetupPredefined()
        {
            mode = TournamentMode.Predefined;
            teams.Clear();
            List<Team> new_teams = new List<Team>();
            System.Globalization.TextInfo text_info = new System.Globalization.CultureInfo("en-US", false).TextInfo;
            foreach (var matchup in config.PredefinedBracket)
            {
                if (matchup.team_a != "")
                {
                    Team team_a = new Team
                    {
                        TeamName = matchup.team_a,
                        State = TeamState.None
                    };
                    if(cache.Teams.ContainsKey(team_a.TeamName))
                    {
                        team_a.BadgeName = cache.Teams[team_a.TeamName].BadgeName;
                        team_a.BadgeColor = cache.Teams[team_a.TeamName].BadgeColor;
                    }
                    else
                    {
                        team_a.BadgeName = text_info.ToTitleCase(matchup.team_a.Replace("_", " "));
                        team_a.BadgeColor = "<color=#808080>";
                    }
                    new_teams.Add(team_a);
                }
                if (matchup.team_b != "")
                {
                    Team team_b = new Team
                    {
                        TeamName = matchup.team_b,
                        State = TeamState.None
                    };
                    if (cache.Teams.ContainsKey(team_b.TeamName))
                    {
                        team_b.BadgeName = cache.Teams[team_b.TeamName].BadgeName;
                        team_b.BadgeColor = cache.Teams[team_b.TeamName].BadgeColor;
                    }
                    else
                    {
                        team_b.BadgeName = text_info.ToTitleCase(matchup.team_b.Replace("_", " "));
                        team_b.BadgeColor = "<color=#808080>";
                    }
                    new_teams.Add(team_b);
                }
            }

            teams = new_teams.ToDictionary(x => x.TeamName, x => x);
            foreach (var p in ReadyPlayers())
            {
                string user = p.UserId;
                string local_group;
                if (ServerStatic.GetPermissionsHandler()._members.TryGetValue(user, out local_group))
                {
                    UserGroup group;
                    if (ServerStatic.GetPermissionsHandler()._groups.TryGetValue(local_group, out group))
                    {
                        Team team;
                        if (teams.TryGetValue(local_group, out team))
                        {
                            team.Users.Add(user);
                            team.BadgeColor = BadgeColors.ColorNameToTag(group.BadgeColor);
                            team.BadgeName = group.BadgeText;
                        }
                        else
                        {
                            p.SendBroadcast("<b><color=#FF0000>Your team: " + local_group + " is not apart of the predefined bracket for this tournament if you believe this is an error speak to a tournament organiser", 60, shouldClearPrevious: true);
                        }
                    }
                    else
                    {
                        p.SendBroadcast("Error could not get UserGroup from local_group try rejoining server and contact a tournament organiser", 60, shouldClearPrevious: true);
                    }
                }
                else
                {
                    p.SendBroadcast("<b><color=#FF0000>Could not assign a team because you have either\n1. Not linked your dicord to steam\n2. Not logged into Cedmod", 60, shouldClearPrevious: true);
                }
            }

            bracket.BuildFromPredefined(teams, config.PredefinedBracket, log);
            StandingDisplay.UpdateStanding(bracket.GetCurrentStandingHint());
        }

        public void SetupScrimmage(int team_count)
        {
            mode = TournamentMode.Scrimmage;

            teams.Clear();
            List<Team> new_teams = new List<Team>();
            for (int i = 0; i < team_count; i++)
            {
                Color color = Random.ColorHSV(0.0f, 1.0f, 0.25f, 0.75f, 0.5f, 1.0f);
                Team team = new Team
                {
                    TeamName = "team_" + i,
                    BadgeName = "Team " + i,
                    BadgeColor = "<color=" + color.ToHex() + ">",
                    Users = new HashSet<string> { },
                    ItemBans = new List<ItemType> { },
                    CandyBans = new List<CandyKindID> { },
                    State = TeamState.None, 
                    ZoneBans = new List<Zone> { } 
                };
                new_teams.Add(team);
            }

            int index = 0;
            List<Player> players = ReadyPlayers().OrderBy(x => Random.value).ToList();
            foreach (var p in players)
            {
                PluginAPI.Core.Log.Info(index + " | " + p.UserId);
                Team team = new_teams[index % team_count];
                team.Users.Add(p.UserId);
                p.ReferenceHub.serverRoles.Network_myColor = "silver";
                p.ReferenceHub.serverRoles.Network_myText = team.BadgeName;
                p.SendBroadcast("Scrimmage Mode - You have been assigned to: <b>" + team.BadgeColor + team.BadgeName + "</b></color>", 15);
                index++;
            }

            teams = new_teams.ToDictionary(x => x.TeamName, x => x);

            bracket.BuildStanding(teams.Values.ToList(), log);
            StandingDisplay.UpdateStanding(bracket.GetCurrentStandingHint());
        }

        public Team AssignTeam(string user_id, UserGroup group)
        {
            string local_group;
            switch(mode)
            {
                case TournamentMode.Scrimmage:
                    Team prev = teams.FirstOrDefault(t => t.Value.Users.Contains(user_id)).Value;
                    if (prev != null)
                        return prev;
                    int min = teams.Min(t => t.Value.Users.Count);
                    List<Team> valid_teams = teams.Values.Where(t => t.Users.Count == min).ToList();
                    Team selected = valid_teams.RandomItem();
                    selected.Users.Add(user_id);
                    return selected;
                case TournamentMode.Predefined:
                    if (ServerStatic.GetPermissionsHandler()._members.TryGetValue(user_id, out local_group))
                    {
                        string team_name = local_group;
                        if (teams.ContainsKey(team_name))
                        {
                            bool changed = false;
                            if (!cache.Teams.ContainsKey(team_name))
                            {
                                cache.Teams.Add(team_name, new TeamInfo { BadgeName = group.BadgeText, BadgeColor = BadgeColors.ColorNameToTag(group.BadgeColor) });
                                changed = true;
                            }
                            else
                            {
                                changed = (cache.Teams[team_name].BadgeName != group.BadgeText || cache.Teams[team_name].BadgeColor != BadgeColors.ColorNameToTag(group.BadgeColor));
                                cache.Teams[team_name].BadgeName = group.BadgeText;
                                cache.Teams[team_name].BadgeColor = BadgeColors.ColorNameToTag(group.BadgeColor);
                            }
                            if (changed)
                                SaveTeamCache();
                            Team team = teams[team_name];
                            team.Users.Add(user_id);
                            team.BadgeColor = BadgeColors.ColorNameToTag(group.BadgeColor);
                            team.BadgeName = group.BadgeText;
                            StandingDisplay.UpdateStanding(bracket.GetCurrentStandingHint());
                            return team;
                        }
                    }
                    return null;
                case TournamentMode.Role:
                    if (ServerStatic.GetPermissionsHandler()._members.TryGetValue(user_id, out local_group))
                    {
                        string team_name = local_group;
                        if (teams.ContainsKey(team_name))
                            teams[team_name].Users.Add(user_id);
                        else
                        {
                            Team team = new Team
                            {
                                TeamName = team_name,
                                BadgeName = group.BadgeText,
                                BadgeColor = BadgeColors.ColorNameToTag(group.BadgeColor),
                                Users = new HashSet<string> { user_id },
                                State = TeamState.None
                            };
                            teams.Add(team_name, team);
                            bracket.BuildStanding(teams.Values.ToList(), log);
                            StandingDisplay.UpdateStanding(bracket.GetCurrentStandingHint());
                        }
                        return teams[team_name];
                    }
                    return null;
            }
            return null;
        }

        public Team TryAssignTeam(Player player, string team_name)
        {
            if (teams.ContainsKey(team_name))
            {
                RemovePlayer(player);
                teams[team_name].Users.Add(player.UserId);
                player.ReferenceHub.serverRoles.Network_myText = teams[team_name].BadgeName;
                return teams[team_name];
            }
            return null;
        }

        public bool TryCreateTeam(string team_name)
        {
            if (!teams.ContainsKey(team_name))
            {
                System.Globalization.TextInfo text_info = new System.Globalization.CultureInfo("en-US", false).TextInfo;
                teams.Add(team_name, new Team { TeamName = team_name, BadgeName = text_info.ToTitleCase(team_name.Replace("_", " ")), BadgeColor = "<color=#808080>", State = TeamState.None, Users = new HashSet<string>() });
                bracket.BuildStanding(teams.Values.ToList(), log);
                StandingDisplay.UpdateStanding(bracket.GetCurrentStandingHint());
                return true;
            }
            return false;
        }

        public bool TryRemoveTeam(string team_name)
        {
            if(teams.Remove(team_name))
            {
                bracket.BuildStanding(teams.Values.ToList(), log);
                StandingDisplay.UpdateStanding(bracket.GetCurrentStandingHint());
                return true;
            }
            return false;
        }

        public string TeamList()
        {
            string list = "";
            foreach (var team in teams.Values)
                list += team.TeamName + " | " + team.BadgeName + "\n"; 
            return list;
        }

        public void RemovePlayer(Player player)
        {
            foreach (var team in teams)
                team.Value.Users.Remove(player.UserId);
        }

        public Team GetPlayerTeam(Player player)
        {
            foreach (var team in teams)
                if (team.Value.Users.Contains(player.UserId))
                    return team.Value;
            return null;
        }

        public Team GetOpponent(Team team)
        {
            foreach(var match in matches)
            {
                Team result = match.GetOpponent(team);
                if (result != null)
                    return result;
            }
            return null;
        }

        public Match GetMatch(Team team)
        {
            foreach(var match in matches)
            {
                if (match.GetOpponent(team) != null)
                    return match;
            }
            return null;
        }

        public bool TryGetTeam(Player player, out Team team)
        {
            team = GetPlayerTeam(player);
            return team != null;
        }

        //public bool CanSpectate(Player player, Player target)
        //{
        //    Team spectator_team = GetPlayerTeam(player);
        //    Team target_team = GetPlayerTeam(target);
        //    if (spectator_team != null && target_team != null)
        //        foreach (var match in matches)
        //            if (!match.Finished && match.InMatch(spectator_team, target_team))
        //                return false;
        //    return true;
        //}

        public Team GetTeam(string team_name)
        {
            Team team;
            teams.TryGetValue(team_name, out team);
            return team;
        }

        //private class MatchInfo
        //{
        //    public int stage;
        //    public Team team_a;
        //    public Team team_b;
        //    public Bracket.Node bracket;
        //}

        private IEnumerator<float> _Update()
        {
            while (true)
            {
                try
                {
                    foreach (var match in matches.ToList())
                    {
                        if (match.Finished)
                        {
                            OnTeamWon(match.Winner, match.Loser, match.WinnerScore, match.LoserScore, "");
                            matches.Remove(match);
                        }
                    }
                }
                catch(System.Exception ex)
                {
                    PluginAPI.Core.Log.Error(ex.ToString());
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }

        private IEnumerator<float> _AutoRun()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(1.0f);

                try
                {
                    if (bracket.Finished)
                        break;

                    if (matches.Count >= 4)
                        continue;

                    Dictionary<Bracket.Node, int> available_matches = bracket.GetAvailable();
                    List<KeyValuePair<Bracket.Node, int>> valid_matches = available_matches.Where(match => matches.All(m => m.Bracket != match.Key)).ToList();
                    if(!valid_matches.IsEmpty())
                    {
                        valid_matches.Sort((x, y) => y.Value - x.Value);//desending
                        int max = valid_matches.First().Value;
                        valid_matches.RemoveAll(m => m.Value != max);
                        StartMatch(valid_matches.RandomItem().Key);
                    }


                    ////List<Zone> available_zones = ArenaManager.AvailableZones();
                    //List<MatchInfo> valid_matches = new List<MatchInfo>();
                    //if (!available_matches.IsEmpty() /*&& !available_zones.IsEmpty()*/)
                    //{
                    //    foreach (var match in available_matches)
                    //    {
                    //        if (matches.Any(m => m.Bracket == match.Key))
                    //            continue;

                    //        MatchInfo info = new MatchInfo { stage = match.Value };
                    //        info.team_a = match.Key.team_a.winner;
                    //        info.team_b = match.Key.team_b.winner;
                    //        //info.zones = available_zones.Except(info.team_a.ZoneBans).Except(info.team_b.ZoneBans).ToList();
                    //        //if (info.zones.IsEmpty())
                    //        //    continue;
                    //        info.bracket = match.Key;
                    //        valid_matches.Add(info);
                    //    }
                    //}
                    //if (!valid_matches.IsEmpty())
                    //{
                    //    valid_matches.Sort((x, y) => x.stage - y.stage);
                    //    valid_matches.Reverse();
                    //    int max = valid_matches.First().stage;
                    //    foreach (var info in valid_matches.ToList())
                    //        if (info.stage != max)
                    //            valid_matches.Remove(info);

                    //    MatchInfo selected = valid_matches.RandomItem();
                    //    bool team_a_empty = selected.team_a.Users.IsEmpty() || selected.team_a.Users.All(u => Player.Get(u) == null);
                    //    bool team_b_empty = selected.team_b.Users.IsEmpty() || selected.team_b.Users.All(u => Player.Get(u) == null);
                    //    if (team_a_empty && team_b_empty)
                    //    {
                    //        if (Random.value < 0.5f)
                    //            OnTeamWon(selected.team_a, selected.team_b, 0, 0, "\n<b>" + selected.team_b.BadgeColor + selected.team_b.BadgeName + "</b></color> Surrendered");
                    //        else
                    //            OnTeamWon(selected.team_b, selected.team_a, 0, 0, "\n<b>" + selected.team_a.BadgeColor + selected.team_a.BadgeName + "</b></color> Surrendered");
                    //        continue;
                    //    }
                    //    else if (team_a_empty)
                    //    {
                    //        OnTeamWon(selected.team_b, selected.team_a, 0, 0, "\n<b>" + selected.team_a.BadgeColor + selected.team_a.BadgeName + "</b></color> Surrendered");
                    //        continue;
                    //    }
                    //    else if (team_b_empty)
                    //    {
                    //        OnTeamWon(selected.team_a, selected.team_b, 0, 0, "\n<b>" + selected.team_b.BadgeColor + selected.team_b.BadgeName + "</b></color> Surrendered");
                    //        continue;
                    //    }
                    //    AddMatch(selected.bracket);
                    //    //Match match = new Match(/*selected.zones.RandomItem(), */selected.team_a, selected.team_b, config.ScoreThreshold, selected.bracket);
                    //    //match.Run();
                    //    //matches.Add(match);
                    //    //Timing.CallDelayed(0.5f,()=> StandingDisplay.UpdateStanding(bracket.GetCurrentStandingHint()));
                    //}
                }
                catch (System.Exception ex)
                {
                    PluginAPI.Core.Log.Error(ex.ToString());
                }
            }

            PluginAPI.Core.Log.Info("Winner found");

            foreach(var p in ReadyPlayers())
                p.ReceiveHint("<size=64><b>" + bracket.Winner.BadgeColor + bracket.Winner.BadgeName + " Won!</color></b></size>", 15);

            yield return Timing.WaitForSeconds(15.0f);

            foreach (var p in ReadyPlayers())
                StandingDisplay.AddPlayer(p);
        }

        private void StartMatch(Bracket.Node node)
        {
            Team team_a = node.team_a.winner;
            Team team_b = node.team_b.winner;
            bool team_a_empty = team_a.Users.IsEmpty() || team_a.Users.All(u => Player.Get(u) == null);
            bool team_b_empty = team_b.Users.IsEmpty() || team_b.Users.All(u => Player.Get(u) == null);
            if (team_a_empty && team_b_empty)
            {
                if (Random.value < 0.5f)
                    OnTeamWon(team_a, team_b, 0, 0, "\n<b>" + team_b.BadgeColor + team_b.BadgeName + "</b></color> Surrendered");
                else
                    OnTeamWon(team_b, team_a, 0, 0, "\n<b>" + team_a.BadgeColor + team_a.BadgeName + "</b></color> Surrendered");
                return;
            }
            else if (team_a_empty)
            {
                OnTeamWon(team_b, team_a, 0, 0, "\n<b>" + team_a.BadgeColor + team_a.BadgeName + "</b></color> Surrendered");
                return;
            }
            else if (team_b_empty)
            {
                OnTeamWon(team_a, team_b, 0, 0, "\n<b>" + team_b.BadgeColor + team_b.BadgeName + "</b></color> Surrendered");
                return;
            }

            Match match = new Match(node, config.ScoreThreshold);
            match.Run();
            matches.Add(match);
            Timing.CallDelayed(0.5f, () => StandingDisplay.UpdateStanding(bracket.GetCurrentStandingHint()));
        }

        private void OnTeamWon(Team winner, Team loser, int winner_score, int loser_score, string reason)
        {
            bracket.AddMatchResults(winner, loser);
            StandingDisplay.UpdateStanding(bracket.GetCurrentStandingHint());
            log.Results.Add(new MatchResult { winner = winner.TeamName, loser = loser.TeamName, winner_score = winner_score, loser_score = loser_score, valid = true, reason = reason.Replace("\n", "") });

            foreach (var p in ReadyPlayers())
                if (p.Role == RoleTypeId.Spectator)
                    p.SendBroadcast("<b>" + winner.BadgeColor + winner.BadgeName + "</b></color> Defeated <b>" + loser.BadgeColor + loser.BadgeName + "</b></color> " + winner_score + " - " + loser_score + reason, 15);
            SaveLog();
        }

    }

    public static class BadgeColors
    {
        public static readonly Dictionary<string, string> ColorNameToHex = new Dictionary<string, string>
        {
            {"pink", "#FF96DE"},
            {"red", "#C50000"},
            {"brown", "#944710"},
            {"silver", "#A0A0A0"},
            {"light_green", "#32CD32"},
            {"crimson", "#DC143C"},
            {"cyan", "#00B7EB"},
            {"aqua", "#00FFFF"},
            {"deep_pink", "#FF1493"},
            {"tomato", "#FF6448"},
            {"yellow", "#FAFF86"},
            {"magenta", "#FF0090"},
            {"blue_green", "#4DFFB8"},
            {"orange", "#FF9966"},
            {"lime", "#BFFF00"},
            {"green", "#228B22"},
            {"emerald", "#50C878"},
            {"carmine", "#960018"},
            {"nickel", "#727472"},
            {"mint", "#98FB98"},
            {"army_green", "#4B5320"},
            {"pumpkin", "#EE7600"},
            {"gold", "#EFC01A"},
            {"teal", "#008080"},
            {"blue", "#005EBC"},
            {"purple", "#8137CE"},
            {"light_red", "#FD8272"},
            {"silver_blue", "#666699"},
            {"police_blue", "#002DB3"}

        };

        public static string ColorNameToTag(string color)
        {
            ServerRoles.NamedColor named_color;
            if (ServerRoles.DictionarizedColorsCache.TryGetValue(color, out named_color))
                return "<color=#" + named_color.ColorHex + ">";
            else
            {
                PluginAPI.Core.Log.Info("couldnt get color hex from ServerRoles.DictionarizedColorsCache");
                string hex;
                if (ColorNameToHex.TryGetValue(color, out hex))
                    return "<color=" + hex + ">";
                else
                {
                    PluginAPI.Core.Log.Info("color doesnt exist: " + color);
                    return "<color=#FFFFFF>";
                }
            }
        }
    }
}
