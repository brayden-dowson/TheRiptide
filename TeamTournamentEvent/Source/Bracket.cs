using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public class Bracket
    {
        public class Node
        {
            public Node team_a = null;
            public Node team_b = null;

            public Team winner = null;
        }

        private Node root = new Node();
        public bool Finished { get; private set; } = false;
        public Team Winner { get; private set; } = null;

        public void BuildFromPredefined(Dictionary<string, Team> teams, List<Matchup> matchups, Log log)
        {
            List<Node> nodes = new List<Node>();
            foreach(var matchup in matchups)
            {
                Team team_a = null;
                Team team_b = null;
                teams.TryGetValue(matchup.team_a, out team_a);
                teams.TryGetValue(matchup.team_b, out team_b);

                if (team_a == null && team_b == null)
                    PluginAPI.Core.Log.Error("Empty matchup found in predefined bracket in the config");
                else if (team_a == null)
                    nodes.Add(new Node { winner = team_b });
                else if (team_b == null)
                    nodes.Add(new Node { winner = team_a });
                else
                    nodes.Add(new Node { team_a = new Node { winner = team_a }, team_b = new Node { winner = team_b } });
            }

            while(nodes.Count > 1)
            {
                List<Node> next_stage = new List<Node>();
                for (int i = 0; i < nodes.Count; i += 2)
                    next_stage.Add(new Node { team_a = nodes[i], team_b = nodes[i + 1] });

                if (nodes.Count % 2 == 1)
                    next_stage.Add(nodes.Last());

                nodes = next_stage;
            }
            root = nodes.First();

            LoadFromLog(teams.Values.ToList(), log);
        }

        public void BuildStanding(List<Team> teams, Log log)
        {
            root = new Node();
            foreach (var team in teams)
                Insert(root, team);

            LoadFromLog(teams, log);
        }

        private void LoadFromLog(List<Team> teams, Log log)
        {
            foreach (var result in log.Results)
            {
                if (!result.valid)
                    continue;
                Team winner = teams.FirstOrDefault(t => t.TeamName == result.winner);
                Team loser = teams.FirstOrDefault(t => t.TeamName == result.loser);

                if (winner == null || loser == null)
                    PluginAPI.Core.Log.Error("log had invalid teams: " + result.winner + ", " + result.loser);

                AddMatchResults(root, winner, loser);
            }
        }

        public void AddMatchResults(Team winner, Team loser)
        {
            AddMatchResults(root, winner, loser);
            if (root.winner != null)
            {
                Finished = true;
                Winner = root.winner;
            }
        }

        public Dictionary<Node, int> GetAvailable()
        {
            return GetAvailable(root, 0);
        }

        public Node FindMatch(string team)
        {
            return Find(root, team);
        }

        public Node FindMatch(string team_a, string team_b)
        {
            return Find(root, team_a, team_b);
        }

        private Node Find(Node node, string team)
        {
            if(node.team_a != null && node.team_b != null)
            {
                if (node.team_a.winner != null && node.team_b.winner != null)
                {
                    if (node.team_a.winner.TeamName == team)
                        return node;
                    if (node.team_b.winner.TeamName == team)
                        return node;
                }
                else
                {
                    Node result = Find(node.team_a, team);
                    if (result != null)
                        return result;
                    result = Find(node.team_b, team);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }

        private Node Find(Node node, string team_a, string team_b)
        {
            if(node.team_a != null && node.team_b != null)
            {
                if (node.team_a.winner != null && node.team_b.winner != null)
                {
                    if (node.team_a.winner.TeamName == team_a && node.team_b.winner.TeamName == team_b)
                        return node;
                    if (node.team_a.winner.TeamName == team_b && node.team_b.winner.TeamName == team_a)
                        return node;
                }
                else
                {
                    Node result = Find(node.team_a, team_a, team_b);
                    if (result != null)
                        return result;
                    result = Find(node.team_b, team_a, team_b);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }

        private Dictionary<Node, int> GetAvailable(Node node, int stage)
        {
            if (node.team_a == null || node.team_b == null)
                return new Dictionary<Node, int>();

            if (IsLeaf(node.team_a) && IsLeaf(node.team_b))
                return new Dictionary<Node, int> { { node, stage } };

            Dictionary<Node, int> avaliable = new Dictionary<Node, int>();
            if (node.team_a != null && node.team_a.winner == null)
                avaliable = avaliable.Union(GetAvailable(node.team_a, stage + 1)).ToDictionary(x => x.Key, x => x.Value);
            if (node.team_b != null && node.team_b.winner == null)
                avaliable = avaliable.Union(GetAvailable(node.team_b, stage + 1)).ToDictionary(x => x.Key, x => x.Value);
            return avaliable;
        }

        private void AddMatchResults(Node node, Team winner, Team loser)
        {
            if (node.team_a == null || node.team_b == null)
                return;
            if ((node.team_a.winner == winner && node.team_b.winner == loser) ||
                (node.team_a.winner == loser && node.team_b.winner == winner))
                node.winner = winner;
            else
            {
                AddMatchResults(node.team_a, winner, loser);
                AddMatchResults(node.team_b, winner, loser);
            }
        }

        private void Insert(Node node, Team team)
        {
            if (node.team_a == null)
                node.team_a = new Node { winner = team };
            else if (node.team_b == null)
                node.team_b = new Node { winner = team };
            else if (IsLeaf(node.team_a))
            {
                node.team_a = new Node { team_a = node.team_a, team_b = node.team_b, };
                node.team_b = new Node { winner = team };
            }
            else if (IsLeaf(node.team_b))
                node.team_b = new Node { team_a = node.team_b, team_b = new Node { winner = team } };
            else if (NodeCount(node.team_a) <= NodeCount(node.team_b))
                Insert(node.team_a, team);
            else
                Insert(node.team_b, team);
        }

        private bool IsLeaf(Node node)
        {
            return node.winner != null;
        }

        private int NodeCount(Node node)
        {
            if (node.winner != null)
                return 1;

            int count = 0;
            if (node.team_a != null)
                count += NodeCount(node.team_a);

            if (node.team_b != null)
                count += NodeCount(node.team_b);

            return count + 1;
        }

        private int MaxDepth(Node node)
        {
            if (node.team_a == null && node.team_b == null)
                return 0;

            int depth_a = 0;
            if (node.team_a != null)
                depth_a = MaxDepth(node.team_a);

            int depth_b = 0;
            if (node.team_b != null)
                depth_b = MaxDepth(node.team_b);

            return Mathf.Max(depth_a, depth_b) + 1;
        }

        private int MinDepth(Node node)
        {
            if (node.winner != null)
                return 0;

            int depth_a = 0;
            if (node.team_a != null)
                depth_a = MinDepth(node.team_a);

            int depth_b = 0;
            if (node.team_b != null)
                depth_b = MinDepth(node.team_b);

            return Mathf.Min(depth_a, depth_b) + 1;
        }

        public string GetCurrentStandingHint()
        {
            const int line_width = 120;

            Dictionary<int, Dictionary<int, string>> line_stages = new Dictionary<int, Dictionary<int, string>>();
            int stages = MaxDepth(root);
            int stage_width = line_width / (stages + 1);
            BuildStageLines(root, 0, MaxDepth(root), false, stage_width, line_stages);
            int max_line = line_stages.Max(x => x.Key);
            int min_line = line_stages.Min(x => x.Key);

            string hint = "<align=left><voffset=4em><size=18><line-height=50%><mspace=0.5em>\n";
            for (int l = min_line; l <= max_line; l++)
            {
                string line = "";
                Dictionary<int, string> stage;
                if (line_stages.TryGetValue(l, out stage))
                {
                    for (int s = 0; s <= stages; s++)
                    {
                        string name;
                        if (stage.TryGetValue(s, out name))
                            line += name;
                        else
                            line += new string(' ', stage_width);
                    }
                }
                hint += line.PadLeft(stage_width * (stages + 1)) + "\n";
            }

            return hint;
        }

        private void BuildStageLines(Node node, int line, int stage, bool is_a, int max_char_width, Dictionary<int, Dictionary<int, string>> line_stages)
        {
            if (!line_stages.ContainsKey(line))
                line_stages.Add(line, new Dictionary<int, string>());

            string prefix = node.team_a == null ? " " : "━";
            if (node.team_a != null && node.team_a.winner != null && node.team_a.winner.State != TeamState.None && node.team_b != null && node.team_b.winner != null && node.team_b.winner.State != TeamState.None)
                prefix = "<b><color=#FF0000>⚔</color></b>";
            char pad_name = (node.team_a == null && node.team_b == null) ? ' ' : '╸';
            string sufix = node == root ? " " : (is_a ? "╻" : "╹");

            string segment;
            if(node.winner == null)
            {
                segment = prefix + "[" + new string('_', max_char_width - 4) + "] ";
                if(node != root)
                {
                    for (int i = 1; i < (1 << stage); i++)
                    {
                        if (is_a)
                        {
                            if (!line_stages.ContainsKey(line + i))
                                line_stages.Add(line + i, new Dictionary<int, string>());
                            line_stages[line + i].Add(stage, new string(' ', max_char_width - 1) + '┃');
                        }
                        else
                        {
                            if (!line_stages.ContainsKey(line - i))
                                line_stages.Add(line - i, new Dictionary<int, string>());
                            line_stages[line - i].Add(stage, new string(' ', max_char_width - 1) + '┃');
                        }
                    }
                }
            }
            else
            {
                segment = node.winner.BadgeColor + prefix + node.winner.BadgeName.PadLeft(max_char_width - 2, pad_name).Substring(0, max_char_width - 2) + sufix + "</color>";
                if (node != root)
                {
                    for (int i = 1; i < (1 << stage); i++)
                    {
                        if (is_a)
                        {
                            if (!line_stages.ContainsKey(line + i))
                                line_stages.Add(line + i, new Dictionary<int, string>());
                            line_stages[line + i].Add(stage, new string(' ', max_char_width - 1) + node.winner.BadgeColor + "┃</color>");
                        }
                        else
                        {
                            if (!line_stages.ContainsKey(line - i))
                                line_stages.Add(line - i, new Dictionary<int, string>());
                            line_stages[line - i].Add(stage, new string(' ', max_char_width - 1) + node.winner.BadgeColor + "┃</color>");
                        }
                    }
                }
            }
            line_stages[line].Add(stage, segment);

            int delta = (1 << stage) / 2;
            if (node.team_a != null)
                BuildStageLines(node.team_a, line - delta, stage - 1, true, max_char_width, line_stages);
            if (node.team_b != null)
                BuildStageLines(node.team_b, line + delta, stage - 1, false, max_char_width, line_stages);
        }


    }
}
