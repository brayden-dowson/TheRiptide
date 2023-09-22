using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using CommandSystem;
using CustomPlayerEffects;
using HarmonyLib;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Usables.Scp330;
using LightContainmentZoneDecontamination;
using MEC;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using Respawning;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using static TheRiptide.EnumExtensions;
using static TheRiptide.Utility;

//end of game state reset
//log spectators?
//log matches won
//build bracket with log
//gramaphone ghost test
//seeds/bracket order

//bc loadout
//bc zone with loadout
//zone queue
//zone bans
//make bans bc look nice
namespace TheRiptide
{
    public class Matchup
    {
        public string team_a { get; set; }
        public string team_b { get; set; }
    }

    public class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;
        public string Description { get; set; } = "Team v Team Tournament\n\n";
        public int ScoreThreshold { get; set; } = 2;
        public int BanSelectionTime { get; set; } = 90;
        public int LoadoutSelectionTime { get; set; } = 45;

        [Description("percentage that the zone decreases by per minute 0.20 = 5 mins to collapse fully")]
        public float ZoneSizeShrinkRate { get; set; } = 0.20f;
        public float MinZoneSizeSurface { get; set; } = 3.0f;
        public float MinZoneSizeFacility { get; set; } = 7.5f;
        public float ZoneShrinkDelay { get; set; } = 60.0f;

        public RoleTypeId RoleA { get; set; } = RoleTypeId.ClassD;
        public RoleTypeId RoleB { get; set; } = RoleTypeId.Scientist;
        public List<ItemType> GlobalItemBans { get; set; } = new List<ItemType>();
        public Dictionary<ItemType, int> TeamItemLimit { get; set; } = new Dictionary<ItemType, int>
        {
            {ItemType.SCP268, 1 },
            {ItemType.SCP018, 1 },
            {ItemType.SCP1576, 1 },
            {ItemType.SCP244a, 1 },
            {ItemType.SCP244b, 1 }
        };
        public int TeamZoneBanLimit { get; set; } = 1;
        public Dictionary<Category, int> TeamCategoryBansLimit { get; set; } = new Dictionary<Category, int>
        {
            {Category.Weapon, 4 },
            {Category.Medical, 1 },
            {Category.SCP, 1 },
            {Category.Other, 1 },
        };
        public int TeamCandyBanLimit { get; set; } = 1;
        public int BanThreshold { get; set; } = 2;

        public List<Matchup> PredefinedBracket { get; set; } = new List<Matchup>
        {
            new Matchup{team_a = "team_1", team_b = "team_2"},
            new Matchup{team_a = "team_5", team_b = ""},
            new Matchup{team_a = "team_3", team_b = "team_4"},
            new Matchup{team_a = "team_6", team_b = ""},
        };
    }

    public class MatchResult
    {
        public System.DateTime time { get; set; } = System.DateTime.Now;
        public string winner { get; set; }
        public string loser { get; set; }
        public int winner_score { get; set; }
        public int loser_score { get; set; }
        public bool valid { get; set; } = true;
        public string reason { get; set; }
    }

    public class Log
    {
        public List<MatchResult> Results { get; set; } = new List<MatchResult>();
    }

    public class TeamInfo
    {
        public string BadgeName { get; set; } = "";
        public string BadgeColor { get; set; } = "";
    }

    public class TeamCache
    {
        public Dictionary<string, TeamInfo> Teams { get; set; } = new Dictionary<string, TeamInfo>();
    }


    public class EventHandler
    {
        public static EventHandler Singleton { get; private set; }
        public static Config config;
        private static Tournament tournament;

        public EventHandler()
        {
            Singleton = this;
        }

        public static void Start(object plugin, Config config, Log log, TeamCache cache)
        {
            Round.IsLocked = true;
            //foreach(var p in NetworkClient.prefabs)
            //{
            //    PluginAPI.Core.Log.Info(p.Value.name);
            //}    

            EventHandler.config = config;

            StandingDisplay.Start();
            LoadoutRoom.Start();
            tournament = new Tournament();
            tournament.Start(plugin, config, log, cache);
        }

        public static void Stop()
        {
            LoadoutRoom.Stop();
            StandingDisplay.Stop();
            tournament.Stop();
            tournament = null;
        }

        [PluginEvent(ServerEventType.PlayerGetGroup)]
        public void OnPlayerGetGroup(PlayerGetGroupEvent e)
        {
            Team team = tournament.AssignTeam(e.UserId, e.Group);
            Timing.CallDelayed(0.0f, () =>
            {
                Player player = Player.Get(e.UserId);
                if (player != null)
                {
                    if (team != null)
                        player.SendBroadcast("<color=#00bbff>Team:</color> <b>" + team.BadgeColor + team.BadgeName + "</color></b>", 15, shouldClearPrevious: true);
                    else
                    {
                        Timing.CallDelayed(0.0f, () =>
                        {
                            if (tournament.mode == TournamentMode.Predefined)
                                player.SendBroadcast("<b><color=#FF0000>Your team: " + BadgeColors.ColorNameToTag(e.Group.BadgeColor) + e.Group.BadgeText + "</color> is not apart of the predefined bracket for this tournament. If you believe this is an error speak to a tournament organiser", 60, shouldClearPrevious: true);
                        });
                    }
                }
                else
                    PluginAPI.Core.Log.Info("Null player");
            });
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        public void OnPlayerJoined(Player player)
        {
            Team team;
            if(tournament.TryGetTeam(player, out team))
            {
                Team opponent = tournament.GetOpponent(team);
                if (opponent != null)
                    SpectatorVisibility.SetMatchup(player, team, opponent);
            }
            else
            {
                if (tournament.mode == TournamentMode.Predefined)
                    player.SendBroadcast("<b><color=#FF0000>Could not assign a team because you have either\n1. Not linked your dicord to steam\n2. Not logged into Cedmod", 60, shouldClearPrevious: true);
            }
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        public void OnPlayerLeft(Player player)
        {
            if (player == null)
                return;

            tournament.RemovePlayer(player);
            StandingDisplay.RemovePlayer(player);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            Round.IsLocked = true;
            DecontaminationController.Singleton.NetworkDecontaminationOverride = DecontaminationController.DecontaminationStatus.Disabled;
            AlphaWarheadController.Singleton._isAutomatic = false;
            Timing.CallDelayed(1.0f,()=>
            {
                try
                {
                    ArenaManager.Start();
                    ClearItemPickups();
                    ClearRagdolls();
                }
                catch(System.Exception ex)
                {
                    PluginAPI.Core.Log.Error(ex.ToString());
                }
            });

            Server.Instance.SetRole(RoleTypeId.Scp939);
            Server.Instance.ReferenceHub.nicknameSync.SetNick("[Tournament Bracket]");
            Server.Instance.Position = new Vector3(128.8f, 994.0f, 18.0f);
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(PlayerChangeRoleEvent e)
        {
            if (e.Player == null || !Round.IsRoundStarted)
                return true;

            if(e.ChangeReason == RoleChangeReason.RoundStart && e.NewRole != RoleTypeId.Spectator)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (e.Player.Role == RoleTypeId.Spectator)
                        return;
                    e.Player.SetRole(RoleTypeId.Spectator);
                });
                return false;
            }
            //else if(e.NewRole == RoleTypeId.Spectator)
            //{
            //    foreach (var p in ReadyPlayers())
            //        if (p.IsAlive && p.GameObject != e.Player.GameObject && !tournament.CanSpectate(e.Player, p))
            //            e.Player.Connection.Send(new ObjectDestroyMessage { netId = p.ReferenceHub.netId });
            //            //e.Player.Connection.Send(new RoleSyncInfo(p.ReferenceHub, RoleTypeId., e.Player.ReferenceHub));
            //}

            return true;
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(PlayerSpawnEvent e)
        {
            if (e.Player == null || !Round.IsRoundStarted)
                return;

            if (e.Role != RoleTypeId.Spectator)
                StandingDisplay.RemovePlayer(e.Player);

            if (e.Role == RoleTypeId.Tutorial)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    try
                    {
                        if (e.Player.Role != RoleTypeId.Tutorial || !(e.Player.ReferenceHub.roleManager.CurrentRole is IFpcRole currentRole))
                            return;
                        foreach (HitboxIdentity hitbox in currentRole.FpcModule.CharacterModelInstance.Hitboxes)
                            hitbox.SetColliders(false);
                    }
                    catch (System.Exception ex)
                    {
                        PluginAPI.Core.Log.Error(ex.ToString());
                    }
                });
            }
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        bool OnRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerEscape)]
        bool OnPlayerEscape(Player player, RoleTypeId role)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerSearchPickup)]
        bool OnPlayerSearchPickup(PlayerSearchPickupEvent e)
        {
            Team team;
            if(tournament.TryGetTeam(e.Player,out team))
            {
                if (team.State == TeamState.LoadoutSelection)
                {
                    Loadout loadout = Loadout.Get(e.Player);
                    if (!CheckTeamItemLimit(team, e.Item.Info.ItemId))
                    {
                        loadout.Broadcast(e.Player, "\nYour team has reached the limit for " + e.Item.Info.ItemId.ToString().Replace("Gun", "") + " of " + config.TeamItemLimit[e.Item.Info.ItemId]);
                        return false;
                    }
                    loadout.SetItem(e.Item.Info.ItemId);
                    loadout.UpdateInventoy(e.Player, false);
                    loadout.Broadcast(e.Player, "\n<color=#87e8de>Zone: </color><color=#b7eb8f>" + tournament.GetMatch(team).zone + "</color>");
                    return false;
                }
                else if(team.State == TeamState.BanSelection)
                {
                    Bans bans = Bans.Get(e.Player);
                    if(e.Item.Info.ItemId.ItemCategory() == Category.None)
                    {
                        if(bans.SetZoneBan(e.Item.Info.ItemId))
                        {
                            Bans.BroadcastTeam(team);
                        }
                    }
                    else if(bans.TryAddItemBan(e.Item.Info.ItemId))
                    {
                        Bans.BroadcastTeam(team);
                        bans.UpdateInventoy(e.Player);
                    }
                    return false;
                }
            }
            return true;
        }

        [PluginEvent(ServerEventType.PlayerDropItem)]
        bool OnPlayerDropItem(PlayerDropItemEvent e)
        {
            Team team;
            if(tournament.TryGetTeam(e.Player, out team))
            {
                if (team.State == TeamState.LoadoutSelection)
                {
                    if (e.Item.Category != ItemCategory.Armor && e.Item.Category != ItemCategory.Keycard)
                    {
                        Loadout loadout = Loadout.Get(e.Player);
                        if (e.Item.ItemTypeId != ItemType.SCP330)
                            loadout.RemoveItem(e.Item.ItemTypeId);
                        else
                            loadout.RemoveCandy((e.Item as Scp330Bag).Candies.First());
                        loadout.UpdateInventoy(e.Player, false);
                        loadout.Broadcast(e.Player, "\n<color=#87e8de>Zone: </color><color=#b7eb8f>" + tournament.GetMatch(team).zone + "</color>");
                    }
                    return false;
                }
                else if (team.State == TeamState.BanSelection)
                {
                    Bans bans = Bans.Get(e.Player);
                    if (e.Item.ItemTypeId != ItemType.SCP330)
                    {
                        if (bans.TryRemoveItemBan(e.Item.ItemTypeId))
                        {
                            bans.UpdateInventoy(e.Player);
                            Bans.BroadcastTeam(team);
                        }
                    }
                    else
                    {
                        bans.RemoveAllCandyBans();
                        bans.UpdateInventoy(e.Player);
                        Bans.BroadcastTeam(team);
                    }
                    return false;
                }
            }
            return true;
        }

        [PluginEvent(ServerEventType.PlayerInteractDoor)]
        public bool OnPlayerInteractDoor(Player player, DoorVariant door, bool canOpen)
        {
            return !LoadoutRoom.ButtonPressed(player, door);
        }

        [PluginEvent(ServerEventType.PlayerChangeItem)]
        void OnPlayerChangeItem(PlayerChangeItemEvent e)
        {
            Team team;
            if(tournament.TryGetTeam(e.Player,out team))
            {
                ItemBase item;
                if (e.Player.ReferenceHub.inventory.UserInventory.Items.TryGetValue(e.NewItem, out item))
                {
                    if (team.State == TeamState.LoadoutSelection)
                    {
                        if (item.ItemTypeId.ItemCategory() != Category.Weapon)
                            Timing.CallDelayed(0.0f, () => e.Player.CurrentItem = null);
                    }
                    else if (team.State == TeamState.BanSelection)
                        Timing.CallDelayed(0.0f, () => e.Player.CurrentItem = null);
                }
            }
        }

        [PluginEvent(ServerEventType.PlayerChangeSpectator)]
        void OnPlayerChangeSpectator(PlayerChangeSpectatorEvent e)
        {
            if (e.Player == null)
                return;

            //PluginAPI.Core.Log.Info("spectator changed");

            //if (e.NewTarget != null)
            //    PluginAPI.Core.Log.Info(e.NewTarget.Nickname);

            //if (e.OldTarget != null)
            //    PluginAPI.Core.Log.Info(e.OldTarget.Nickname);

            //if (!tournament.CanSpectate(e.Player, e.NewTarget))
            //    e.Player.ReferenceHub.netIdentity.connectionToClient.Send(new RoleSyncInfo(e.NewTarget.ReferenceHub, RoleTypeId.Spectator, e.Player.ReferenceHub));

            if (e.NewTarget != null && e.NewTarget.Nickname == "Dedicated Server")
            {//for some reason when this is run as a Cedmod event you cant check the GameObject
                StandingDisplay.AddPlayer(e.Player);
                //PluginAPI.Core.Log.Info("added player");
            }
            if (e.OldTarget != null && e.OldTarget.Nickname == "Dedicated Server")
            {
                StandingDisplay.RemovePlayer(e.Player);
                //PluginAPI.Core.Log.Info("removed player");
            }
        }

        [PluginEvent(ServerEventType.PlayerReceiveEffect)]
        bool OnPlayerReceiveEffect(PlayerReceiveEffectEvent e)
        {
            if (e.Player.Role == RoleTypeId.Tutorial && e.Effect is Flashed)
                return false;
            return true;
        }

        public bool OnPlayerPickupScp330(Player player, Scp330Pickup pickup)
        {
            Team team;
            if(tournament.TryGetTeam(player,out team))
            {
                if (team.State == TeamState.LoadoutSelection)
                {
                    Loadout loadout = Loadout.Get(player);
                    loadout.SetCandy(pickup.ExposedCandy);
                    loadout.UpdateInventoy(player, false);
                    loadout.Broadcast(player, "\n<color=#87e8de>Zone: </color><color=#b7eb8f>" + tournament.GetMatch(team).zone + "</color>");
                    return false;
                }
                else if (team.State == TeamState.BanSelection)
                {
                    Bans bans = Bans.Get(player);
                    if (bans.TryAddCandyBan(pickup.ExposedCandy))
                    {
                        bans.UpdateInventoy(player);
                        Bans.BroadcastTeam(team);
                    }
                    return false;
                }
            }
            return true;
        }

        public bool TryRunMatch(string team)
        {
            return tournament.TryRunMatch(team);
        }

        public void AutoRunTournament()
        {
            tournament.AutoRun();
        }

        public void SetupScrimmage(int team_count)
        {
            tournament.SetupScrimmage(team_count);
        }

        public void SetupPredefined()
        {
            tournament.SetupPredefined();
        }

        public bool ForceWin(Player sender, string team)
        {
            return tournament.TryForceWin(sender, team);
        }

        public bool UndoWin(Player sender, string team, string reason)
        {
            return tournament.TryUndoWin(sender, team, reason);
        }

        public void SaveLog()
        {
            tournament.SaveLog();
        }

        public Team TryAssignTeam(Player player, string team_name)
        {
            return tournament.TryAssignTeam(player, team_name);
        }

        public bool TryCreateTeam(string team_name)
        {
            return tournament.TryCreateTeam(team_name);
        }

        public bool TryRemoveTeam(string team_name)
        {
            return tournament.TryRemoveTeam(team_name);
        }

        public void UnsetTeam(Player player)
        {
            tournament.RemovePlayer(player);
        }

        public string TeamList()
        {
            return tournament.TeamList();
        }

        //public bool IsOnOppositeTeams(ReferenceHub observer, ReferenceHub target)
        //{
        //    return tournament.CanSpectate(observer, target);
        //}

        private bool CheckTeamItemLimit(Team team, ItemType type)
        {
            if (!config.TeamItemLimit.ContainsKey(type))
                return true;

            int count = 0;
            foreach(var user in team.Users)
            {
                Player p;
                if(Player.TryGet(user, out p))
                {
                    Loadout loadout = Loadout.Get(p);
                    if (loadout.weapon.ToItemType() == type || loadout.medical.ToItemType() == type || loadout.scp.ToItemType() == type || loadout.other.ToItemType() == type)
                        count++;
                }
            }
            if (count >= config.TeamItemLimit[type])
                return false;
            return true;
        }
    }

    public class EliminationTournamentEvent : IEvent, IBulletHoleBehaviour
    {
        public static EliminationTournamentEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Elimination Tournament";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else PluginAPI.Core.Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "ET";
        //public bool OverrideWinConditions { get; }
        //public bool BulletHolesAllowed { get; set; } = false;
        public PluginHandler PluginHandler { get; }
        public IEventConfig Config => EventConfig;

        [PluginConfig]
        public Config EventConfig;

        [PluginConfig("log.yml")]
        public Log log;

        [PluginConfig("team_cache.yml")]
        public TeamCache team_cache;

        private Harmony harmony;

        public void PrepareEvent()
        {
            PluginAPI.Core.Log.Info(EventName + " event is preparing");
            IsRunning = true;
            harmony = new Harmony("EliminationTournamentEvent");
            harmony.PatchAll();
            EventHandler.Start(this, EventConfig, log, team_cache);
            PluginAPI.Core.Log.Info(EventName + " event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            harmony.UnpatchAll("EliminationTournamentEvent");
            harmony = null;
            EventHandler.Stop();
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Elimination Tournament", "1.0.0", "Tournament with teams", "The Riptide")]
        public void OnEnabled()
        {
            Singleton = this;
            Handler = PluginHandler.Get(this);
            //PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        [PluginUnload]
        public void OnDisabled()
        {
            StopEvent();
        }

        public bool CanPlaceBulletHole(StandardHitregBase reg, Ray ray, RaycastHit hit)
        {
            return false;
        }

        //[PluginEvent(ServerEventType.WaitingForPlayers)]
        //public void OnWaitingForPlayers()
        //{
        //    PrepareEvent();
        //}
    }

    //[CommandHandler(typeof(RemoteAdminCommandHandler))]
    //public class TournamentReset : ICommand
    //{
    //    public string Command { get; } = "tour_r";

    //    public string[] Aliases { get; } = new string[] { "tr" };

    //    public string Description { get; } = "Reset arenas";

    //    public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
    //    {
    //        Player player;
    //        if (!sender.CheckPermission(PlayerPermissions.PlayersManagement) || !Player.TryGet(sender, out player))
    //        {
    //            response = "No permission";
    //            return false;
    //        }

    //        foreach (var door in DoorVariant.AllDoors)
    //            door.NetworkTargetState = false;

    //        response = "Success";
    //        return true;
    //    }
    //}

    //[CommandHandler(typeof(GameConsoleCommandHandler))]
    //[CommandHandler(typeof(RemoteAdminCommandHandler))]
    //public class TournamentStanding : ICommand
    //{
    //    public string Command { get; } = "standing";

    //    public string[] Aliases { get; } = new string[] { "s" };

    //    public string Description { get; } = "shows standing as a hint";

    //    public static CoroutineHandle handle;

    //    public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
    //    {
    //        //int lines;
    //        int width;
    //        //if(!int.TryParse(arguments.At(0), out lines))
    //        //{
    //        //    response = "failed";
    //        //    return false;
    //        //}

    //        if (!int.TryParse(arguments.At(0), out width))
    //        {
    //            response = "failed";
    //            return false;
    //        }

    //        Player player;
    //        if (Player.TryGet(sender, out player))
    //        {
    //            int count = 1;
    //            Brackets b = new Brackets();
    //            Timing.KillCoroutines(handle);
    //            handle = Timing.CallPeriodically(300.0f, 0.5f,()=>
    //            {
    //                List<string> teams = new List<string>();
    //                for (int i = 0; i < count; i++)
    //                    teams.Add("team_" + (i + 1).ToString());
    //                b.BuildStanding(teams, null);
    //                player.ReceiveHint(b.GetCurrentStandingHint(0, width), 10);
    //                count++;
    //            });
    //            response = "success";
    //            return true;
    //        }
    //        response = "failed";
    //        return false;
    //    }
    //}
}
