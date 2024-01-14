using CustomPlayerEffects;
using InventorySystem.Items.Usables.Scp330;
using MEC;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PluginAPI.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheRiptide.Utility;
using static TheRiptide.StaticTranslation;

namespace TheRiptide
{
    public class Match
    {
        public Bracket.Node Bracket { get; private set; }
        public bool Finished { get; private set; } = false;
        public Team Winner { get; private set; } = new Team { BadgeName = "Error", TeamName = "error" };
        public Team Loser { get; private set; } = new Team { BadgeName = "Error", TeamName = "error" };
        public int WinnerScore { get; private set; } = 0;
        public int LoserScore { get; private set; } = 0;

        private CoroutineHandle ban_handle;
        private CoroutineHandle loadout_handle;
        private CoroutineHandle match_handle;
        private CoroutineHandle zone_handle;
        private CoroutineHandle round_handle;
        private CoroutineHandle reset_handle;

        public Zone zone { get; private set; }
        private Team team_a;
        private Team team_b;

        private int score_threshold;
        private int team_a_score = 0;
        private int team_b_score = 0;

        private LoadoutRoom.RoomPair rooms;

        private bool team_a_ready = false;
        private bool team_b_ready = false;

        private int elapsed = 0;
        private HashSet<int> team_a_ready_players = new HashSet<int>();
        private HashSet<int> team_b_ready_players = new HashSet<int>();

        public Match(Bracket.Node node, int score_threshold)
        {
            Bracket = node;
            //this.zone = zone;
            //ArenaManager.OccupyZone(zone);
            this.score_threshold = score_threshold;
            team_a = node.team_a.winner;
            team_b = node.team_b.winner;
        }

        //public Match(Team team_a, Team team_b, int score_threshold, Bracket.Node bracket)
        //{
        //    this.Bracket = bracket;
        //    //this.zone = zone;
        //    //ArenaManager.OccupyZone(zone);
        //    this.score_threshold = score_threshold;
        //    this.team_a = team_a;
        //    this.team_b = team_b;
        //}

        public void Run()
        {
            match_handle = Timing.RunCoroutine(_MatchLogic());
        }

        //public bool InMatch(Team a, Team b)
        //{
        //    return (a == team_a && b == team_b) || (a == team_b && b == team_a);
        //}

        //public bool OnOppositeTeams(ReferenceHub target, ReferenceHub observer)
        //{
        //    return (team_a.Users.Contains(target.characterClassManager.UserId) && team_b.Users.Contains(observer.characterClassManager.UserId)) ||
        //        (team_b.Users.Contains(target.characterClassManager.UserId) && team_a.Users.Contains(observer.characterClassManager.UserId));
        //}

        public Team GetOpponent(Team team)
        {
            if (team_a == team)
                return team_b;
            else if (team_b == team)
                return team_a;
            else
                return null;
        }

        private IEnumerator<float> _MatchLogic()
        {
            rooms = LoadoutRoom.BorrowLoadoutRoomPair();
            ResetBans();
            while (team_a_score == team_b_score || Mathf.Max(team_a_score, team_b_score) < score_threshold)
            {
                ban_handle = Timing.RunCoroutine(_BanSelection());
                while (ban_handle.IsAliveAndPaused || ban_handle.IsRunning)
                    yield return Timing.WaitForOneFrame;

                zone_handle = Timing.RunCoroutine(_ZoneHandler());
                while (zone_handle.IsAliveAndPaused || zone_handle.IsRunning)
                    yield return Timing.WaitForOneFrame;

                loadout_handle = Timing.RunCoroutine(_LoadoutSelection());
                while (loadout_handle.IsAliveAndPaused || loadout_handle.IsRunning)
                    yield return Timing.WaitForOneFrame;

                StartRound();
                yield return Timing.WaitForSeconds(10.0f);
                round_handle = Timing.RunCoroutine(_RoundLogic());
                while (round_handle.IsAliveAndPaused || round_handle.IsRunning)
                    yield return Timing.WaitForOneFrame;

                reset_handle = Timing.RunCoroutine(_ResetRound());
                while (reset_handle.IsAliveAndPaused || reset_handle.IsRunning)
                    yield return Timing.WaitForOneFrame;
            }

            EndMatch();
        }

        private void EndMatch()
        {
            team_a.State = TeamState.None;
            foreach (var user in team_a.Users)
            {
                Player p;
                if (!Player.TryGet(user, out p))
                    continue;
                if (p.IsAlive)
                    p.SetRole(RoleTypeId.Spectator);
                SpectatorVisibility.RemoveMatchup(p);
            }

            team_b.State = TeamState.None;
            foreach (var user in team_b.Users)
            {
                Player p;
                if (!Player.TryGet(user, out p))
                    continue;
                if (p.IsAlive)
                    p.SetRole(RoleTypeId.Spectator);
                SpectatorVisibility.RemoveMatchup(p);
            }

            if (team_a_score > team_b_score)
            {
                Winner = team_a;
                Loser = team_b;
                WinnerScore = team_a_score;
                LoserScore = team_b_score;
            }
            else if (team_b_score > team_a_score)
            {
                Winner = team_b;
                Loser = team_a;
                WinnerScore = team_b_score;
                LoserScore = team_a_score;
            }
            LoadoutRoom.ReturnRoomPair(rooms);
            //ArenaManager.FreeZone(zone);
            Finished = true;
        }

        private IEnumerator<float> _ZoneHandler()
        {
            HashSet<Zone> zone_bans = team_a.ZoneBans.Union(team_b.ZoneBans).ToHashSet();
            List<Zone> available_zones = System.Enum.GetValues(typeof(Zone)).ToArray<Zone>().Except(zone_bans).ToList();
            zone = available_zones.RandomItem();

            while(!ArenaManager.AvailableZones().Contains(zone))
            {
                foreach(var user in team_a.Users)
                {
                    Player p;
                    if (Player.TryGet(user, out p))
                        p.SendBroadcast(Translation.WaitingForZone.Replace("{zone}", zone.ToString()), 2, shouldClearPrevious: true);
                }
                foreach (var user in team_b.Users)
                {
                    Player p;
                    if (Player.TryGet(user, out p))
                        p.SendBroadcast(Translation.WaitingForZone.Replace("{zone}", zone.ToString()), 2, shouldClearPrevious: true);
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
            ArenaManager.OccupyZone(zone);
        }

        private void StartRound()
        {
            bool randomize_spawn = Random.value < 0.5;
            bool randomize_role = Random.value < 0.5;
            RoomSpawn team_a_spawn = randomize_spawn ? RoomSpawn.SpawnA : RoomSpawn.SpawnB;
            RoleTypeId team_a_role = randomize_role ? EventHandler.config.RoleA : EventHandler.config.RoleB;
            team_a.State = TeamState.Fighting;
            string hint = "\n\n\n" + team_a.BadgeColor + team_a.BadgeName + "</color> " + team_a_score + "\n" + team_b.BadgeColor + team_b.BadgeName + "</color> " + team_b_score;
            foreach (var user in team_a.Users)
            {
                Player p;
                if (Player.TryGet(user, out p))
                {
                    p.SetRole(team_a_role);
                    Timing.CallDelayed(0.1f, () =>
                    {
                        if (p.Role != team_a_role)
                            return;
                        ArenaManager.TeleportSpawn(p, zone, team_a_spawn);
                        ApplySpawnEffects(p, hint);
                        SpectatorVisibility.SetMatchup(p, team_a, team_b);
                        //(p.RoleBase as FpcStandardRoleBase).VisibilityController = new CustomVisibilityController(this, p.ReferenceHub);
                    });
                }
            }
            RoomSpawn team_b_spawn = randomize_spawn ? RoomSpawn.SpawnB : RoomSpawn.SpawnA;
            RoleTypeId team_b_role = randomize_role ? EventHandler.config.RoleB : EventHandler.config.RoleA;
            team_b.State = TeamState.Fighting;
            foreach (var user in team_b.Users)
            {
                Player p;
                if (Player.TryGet(user, out p))
                {
                    p.SetRole(team_b_role);
                    Timing.CallDelayed(0.1f, () =>
                    {
                        if (p.Role != team_b_role)
                            return;
                        ArenaManager.TeleportSpawn(p, zone, team_b_spawn);
                        ApplySpawnEffects(p, hint);
                        SpectatorVisibility.SetMatchup(p, team_b, team_a);
                        //(p.RoleBase as FpcStandardRoleBase).VisibilityController = new CustomVisibilityController(this, p.ReferenceHub);
                    });
                }
            }
            ArenaManager.StartRound(zone);
        }

        private void ApplySpawnEffects(Player player,string hint)
        {
            player.ClearInventory();
            player.AddItem(ItemType.KeycardZoneManager);
            player.AddItem(ItemType.ArmorCombat);
            Loadout.Get(player).UpdateInventoy(player, true);
            player.EffectsManager.EnableEffect<Ensnared>(7);
            player.ReceiveHint(hint, 5);
            Timing.CallDelayed(4.25f, () =>
            {
                player.EffectsManager.EnableEffect<Scanned>(10);
                player.ReceiveHint(hint, 2);
            });
        }

        private IEnumerator<float> _ResetRound()
        {
            yield return Timing.WaitForSeconds(7.0f);
            foreach (var user in team_a.Users)
            {
                Player p;
                if (Player.TryGet(user, out p) && p.IsAlive)
                    p.SetRole(RoleTypeId.Spectator);
            }
            foreach (var user in team_b.Users)
            {
                Player p;
                if (Player.TryGet(user, out p) && p.IsAlive)
                    p.SetRole(RoleTypeId.Spectator);
            }
            yield return Timing.WaitForSeconds(3.0f);
            ArenaManager.Reset(zone);
            ArenaManager.FreeZone(zone);
        }

        private void BroadcastTeam(Team team, string msg, ushort duration)
        {
            foreach (var user in team.Users)
            {
                Player p;
                if (Player.TryGet(user, out p) && p.IsReady)
                    p.SendBroadcast(msg, duration, shouldClearPrevious: true);
            }
        }

        private IEnumerator<float> _RoundLogic()
        {
            string team_a_msg = "";
            string team_b_msg = "";
            while (true)
            {
                int team_a_alive = 0;
                int team_b_alive = 0;

                //team_a_alive = 1;//REMOVE todo
                //team_b_alive = 1;

                foreach (var user in team_a.Users)
                {
                    Player p;
                    if (Player.TryGet(user, out p) && p.IsHuman && p.Role != RoleTypeId.Tutorial)
                        team_a_alive++;
                }

                foreach (var user in team_b.Users)
                {
                    Player p;
                    if (Player.TryGet(user, out p) && p.IsHuman && p.Role != RoleTypeId.Tutorial)
                        team_b_alive++;
                }

                if (team_a_alive == 0 && team_b_alive == 0)
                {
                    team_a_score++;
                    team_b_score++;
                    team_a_msg = Translation.Draw;
                    team_b_msg = Translation.Draw;
                    break;
                }
                if (team_a_alive == 0)
                {
                    team_b_score++;
                    team_a_msg = Translation.Lost;
                    team_b_msg = Translation.Won;
                    break;
                }
                if (team_b_alive == 0)
                {
                    team_a_score++;
                    team_a_msg = Translation.Won;
                    team_b_msg = Translation.Lost;
                    break;
                }
                yield return Timing.WaitForOneFrame;
            }
            string line_2 = team_a.BadgeColor + team_a.BadgeName + "</color> " + team_a_score + "\n" + team_b.BadgeColor + team_b.BadgeName + "</color> " + team_b_score;
            BroadcastTeam(team_a, team_a_msg + line_2, 10);
            BroadcastTeam(team_b, team_b_msg + line_2, 10);
            foreach (var p in ReadyPlayers())
                if (!team_a.Users.Contains(p.UserId) && !team_b.Users.Contains(p.UserId))
                    p.SendBroadcast(line_2, 15, shouldClearPrevious: true);
        }

        private void ResetBans()
        {
            foreach (var user in team_a.Users)
            {
                Player p;
                if (Player.TryGet(user, out p))
                    Bans.Get(p).Reset();
            }

            foreach (var user in team_b.Users)
            {
                Player p;
                if (Player.TryGet(user, out p))
                    Bans.Get(p).Reset();
            }
        }

        private IEnumerator<float> _BanSelection()
        {
            team_a.State = TeamState.BanSelection;
            team_b.State = TeamState.BanSelection;
            rooms.team_a.Spawn(new HashSet<ItemType>(), new HashSet<CandyKindID>(), (player) => { team_a_ready_players.Add(player.PlayerId); SendReadyStateHint(player, true, EventHandler.config.BanSelectionTime); });
            rooms.team_b.Spawn(new HashSet<ItemType>(), new HashSet<CandyKindID>(), (player) => { team_b_ready_players.Add(player.PlayerId); SendReadyStateHint(player, true, EventHandler.config.BanSelectionTime); });

            team_a_ready = false;
            team_b_ready = false;
            team_a_ready_players = new HashSet<int>();
            team_b_ready_players = new HashSet<int>();
            elapsed = 0;
            while (elapsed < EventHandler.config.BanSelectionTime && (!team_a_ready || !team_b_ready))
            {
                try
                {
                    team_a_ready = RunBanLogic(rooms.team_a, team_a_ready_players, team_a, team_b, EventHandler.config.BanSelectionTime - elapsed);
                    team_b_ready = RunBanLogic(rooms.team_b, team_b_ready_players, team_b, team_a, EventHandler.config.BanSelectionTime - elapsed);
                }
                catch (System.Exception ex)
                {
                    PluginAPI.Core.Log.Error(ex.ToString());
                }

                if (team_a_ready && team_b_ready)
                    break;

                yield return Timing.WaitForSeconds(1.0f);
                elapsed++;
            }

            team_a.ItemBans = Bans.GetTeamItemBans(team_a).ToList();
            team_a.CandyBans = Bans.GetTeamCandyBans(team_a).ToList();
            team_a.ZoneBans = Bans.GetTeamZoneBan(team_a).ToList();

            team_b.ItemBans = Bans.GetTeamItemBans(team_b).ToList();
            team_b.CandyBans = Bans.GetTeamCandyBans(team_b).ToList();
            team_b.ZoneBans = Bans.GetTeamZoneBan(team_b).ToList();

            foreach (var user in team_a.Users)
            {
                Player p = null;
                if (Player.TryGet(user, out p))
                {
                    p.ClearInventory();
                    p.SetRole(RoleTypeId.Spectator);
                }
            }
            foreach (var user in team_b.Users)
            {
                Player p = null;
                if (Player.TryGet(user, out p))
                {
                    p.ClearInventory();
                    p.SetRole(RoleTypeId.Spectator);
                }
            }

            team_a.State = TeamState.None;
            team_b.State = TeamState.None;
            rooms.team_a.Unspawn();
            rooms.team_b.Unspawn();
            BroadcastTeam(team_a, "", 1);
            BroadcastTeam(team_b, "", 1);
        }

        private bool RunBanLogic(LoadoutRoom room, HashSet<int> ready_players, Team team, Team opponent, int time_left)
        {
            bool all_ready = !ready_players.IsEmpty();
            foreach (var user in team.Users)
            {
                Player p = Player.Get(user);
                if (p == null || !p.IsReady)
                {
                    all_ready = false;
                    continue;
                }

                if (p.Role != RoleTypeId.Tutorial)
                {
                    p.SetRole(RoleTypeId.Tutorial);
                    Timing.CallDelayed(0.1f, () =>
                    {
                        if (p.Role != RoleTypeId.Tutorial)
                            return;
                        p.ClearInventory();
                        Bans.Get(p).UpdateInventoy(p);
                        Bans.Get(p).Broadcast(p, Bans.GetTeamVotes(team), Bans.GetTeamBans(team), Bans.GetTeamZoneVotes(team), Bans.GetTeamZoneBanStr(team));
                        room.TeleportPlayer(p);
                        SpectatorVisibility.SetMatchup(p, team, opponent);
                    });
                }

                if (!ready_players.Contains(p.PlayerId))
                    all_ready = false;

                SendReadyStateHint(p, ready_players.Contains(p.PlayerId), EventHandler.config.BanSelectionTime);
            }
            return all_ready;
        }

        private void ResetLoadouts()
        {
            foreach (var user in team_a.Users)
            {
                Player p;
                if (Player.TryGet(user, out p))
                    Loadout.Get(p).Reset();
            }

            foreach (var user in team_b.Users)
            {
                Player p;
                if (Player.TryGet(user, out p))
                    Loadout.Get(p).Reset();
            }
        }

        private IEnumerator<float> _LoadoutSelection()
        {
            ResetLoadouts();
            HashSet<ItemType> item_bans = new HashSet<ItemType>();
            HashSet<CandyKindID> candy_bans = new HashSet<CandyKindID>();
            item_bans.UnionWith(team_a.ItemBans);
            item_bans.UnionWith(team_b.ItemBans);
            candy_bans.UnionWith(team_a.CandyBans);
            candy_bans.UnionWith(team_b.CandyBans);
            rooms.team_a.Spawn(item_bans, candy_bans, (player) => { team_a_ready_players.Add(player.PlayerId); SendReadyStateHint(player, true, EventHandler.config.LoadoutSelectionTime); });
            rooms.team_b.Spawn(item_bans, candy_bans, (player) => { team_b_ready_players.Add(player.PlayerId); SendReadyStateHint(player, true, EventHandler.config.LoadoutSelectionTime); });
            team_a.State = TeamState.LoadoutSelection;
            team_b.State = TeamState.LoadoutSelection;

            team_a_ready = false;
            team_b_ready = false;
            team_a_ready_players = new HashSet<int>();
            team_b_ready_players = new HashSet<int>();
            elapsed = 0;
            while (elapsed < EventHandler.config.LoadoutSelectionTime && (!team_a_ready || !team_b_ready))
            {
                try
                {
                    team_a_ready = RunLoadoutLogic(rooms.team_a, team_a_ready_players, team_a, team_b, EventHandler.config.LoadoutSelectionTime - elapsed);
                    team_b_ready = RunLoadoutLogic(rooms.team_b, team_b_ready_players, team_b, team_a, EventHandler.config.LoadoutSelectionTime - elapsed);
                }
                catch (System.Exception ex)
                {
                    PluginAPI.Core.Log.Error(ex.ToString());
                }

                if (team_a_ready && team_b_ready)
                    break;

                yield return Timing.WaitForSeconds(1.0f);
                elapsed++;
            }
            team_a.State = TeamState.None;
            team_b.State = TeamState.None;
            rooms.team_a.Unspawn();
            rooms.team_b.Unspawn();
            BroadcastTeam(team_a, "", 1);
            BroadcastTeam(team_b, "", 1);
        }

        private bool RunLoadoutLogic(LoadoutRoom room, HashSet<int> ready_players, Team team, Team opponent, int time_left)
        {
            bool all_ready = !ready_players.IsEmpty();
            foreach (var user in team.Users)
            {
                Player p = Player.Get(user);
                if (p == null || !p.IsReady)
                {
                    all_ready = false;
                    continue;
                }

                if (p.Role != RoleTypeId.Tutorial)
                {
                    p.SetRole(RoleTypeId.Tutorial);
                    Timing.CallDelayed(0.1f, () =>
                    {
                        if (p.Role != RoleTypeId.Tutorial)
                            return;
                        p.ClearInventory();
                        p.AddItem(ItemType.ArmorCombat);
                        Loadout.Get(p).UpdateInventoy(p, false);
                        Loadout.Get(p).Broadcast(p, Translation.ExtraStringZone.Replace("{zone}", zone.ToString()));
                        room.TeleportPlayer(p);
                        SpectatorVisibility.SetMatchup(p, team, opponent);
                    });
                }

                if (!ready_players.Contains(p.PlayerId))
                    all_ready = false;

                SendReadyStateHint(p, ready_players.Contains(p.PlayerId), EventHandler.config.LoadoutSelectionTime);
            }
            return all_ready;
        }

        private void SendReadyStateHint(Player player, bool ready, int total_time)
        {
            player.ReceiveHint("<b><size=48>" + (ready ? Translation.YouAreReady : Translation.YouAreNotReady) + Translation.TimeLeftFormat.Replace("{time}", (total_time - elapsed).ToString("0")) + "</size></b>", 2);
            //player.ReceiveHint("<b><size=48>" + (ready ? "<color=#00FF00>YOU ARE READY</color>" : "<color=#FF0000>YOU ARE NOT READY</color>") + "\n<color=#87ceeb>" + (total_time - elapsed).ToString("0") + "</color></size></b>", 2);
        }
    }
}
