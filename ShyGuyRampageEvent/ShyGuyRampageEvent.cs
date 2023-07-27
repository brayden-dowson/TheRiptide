using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using CedMod.Addons.Events;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using MEC;

using PlayerRoles;
using PlayerRoles.PlayableScps.Scp096;
using MapGeneration;
using Interactables.Interobjects.DoorUtils;
using PlayerStatsSystem;

using static TheRiptide.EventUtility;
using Respawning;
using CedMod.Addons.Events.Interfaces;
using Mirror;
using HarmonyLib;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        public bool LockSurface { get; set; } = true;
    }

    public class EventHandler
    {
        public static int selected = 0;
        public static bool late_join = false;
        public static bool found_winner = false;
        private static CoroutineHandle warhead;
        private static RoomIdentifier surface;
        private static bool round_started = false;

        public static void Start()
        {
            selected = 0;
            late_join = false;
            found_winner = false;
            round_started = false;
            WinnerReset();
        }

        public static void Stop()
        {
            selected = 0;
            late_join = false;
            found_winner = false;
            WinnerReset();
            Timing.KillCoroutines(warhead);
            surface = null;
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: Shy Guy Rampage\n<size=32>Everyone has triggered Shy Guy and there is no escape. Shy Guy spawns in entrance and enrages automaticaly after 30 seconds everyone else becomes a ClassD and spawns in light\nThe last one alive wins!</size>", 30, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            if (!Round.IsRoundStarted)
                return;

            if (player.PlayerId == selected)
            {
                IEnumerable<Player> spectators = Player.GetPlayers().Where((x) => !x.IsAlive && x.PlayerId != player.PlayerId);
                if (spectators.Count() >= 1)
                    selected = spectators.ElementAt(UnityEngine.Random.Range(0, spectators.Count())).PlayerId;
                else
                {
                    IEnumerable<Player> players = Player.GetPlayers().Where((x) => x.PlayerId != player.PlayerId);
                    if (players.Count() >= 1)
                        selected = players.ElementAt(UnityEngine.Random.Range(0, players.Count())).PlayerId;
                }
                if (selected != player.PlayerId)
                    Player.Get(selected).SetRole(RoleTypeId.Scp096);
            }
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            LightContainmentZoneDecontamination.DecontaminationController.Singleton.NetworkRoundStartTime = NetworkTime.time - (60.0f * 10.0f);
            warhead = Timing.CallDelayed(5.0f * 60.0f, () =>
            {
                try
                {
                    FacilityManager.UnlockRoom(surface, DoorLockReason.AdminCommand);
                    AlphaWarheadController.Singleton.enabled = true;
                    AlphaWarheadController.Singleton.StartDetonation(true, true);
                }
                catch (Exception ex)
                {
                    Log.Error("warhear error: " + ex.ToString());
                }
            });
            EndRoom = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone == FacilityZone.Surface).First();
            RoomOffset = new UnityEngine.Vector3(40.000f, 14.080f, -32.600f);

            surface = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone == FacilityZone.Surface).First();
            if (ShyGuyRampageEvent.Singleton.EventConfig.LockSurface)
                FacilityManager.LockRoom(surface, DoorLockReason.AdminCommand);

            selected = Player.GetPlayers().Where(p => p.IsReady).ToList().RandomItem().PlayerId;
            Timing.CallDelayed(1.0f, () => late_join = true);
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        bool OnRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted)
                return true;

            if (found_winner)
                return HandleGameOverRoleChange(player, new_role);

            int player_id = player.PlayerId;
            if (player.PlayerId == selected)
            {
                if (new_role != RoleTypeId.Scp096)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        Player p = Player.Get(player_id);
                        if (p != null)
                            p.SetRole(RoleTypeId.Scp096);
                    });
                    return false;
                }
            }
            else if (new_role != RoleTypeId.ClassD && new_role != RoleTypeId.Spectator)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    Player p = Player.Get(player_id);
                    if (p != null)
                        player.SetRole(RoleTypeId.ClassD);
                });
                return false;
            }
            return true;
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            if (player == null || !Round.IsRoundStarted)
                return;

            if (found_winner)
            {
                HandleGameOverSpawn(player);
                return;
            }

            int player_id = player.PlayerId;
            if (role == RoleTypeId.Scp096)
            {
                if (!round_started)
                {
                    round_started = true;
                    player.SendBroadcast("you will forcefully enrage in 30 seconds.", 25, shouldClearPrevious: true);
                    Timing.CallDelayed(25.0f, () =>
                    {
                        Player p = Player.Get(player_id);
                        if (p != null && p.Role == RoleTypeId.Scp096)
                            ForceRage(p);
                    });
                }
                else
                {
                    ForceRage(player);
                }
                Timing.CallDelayed(0.0f, () =>
                {
                    Player p = Player.Get(player_id);
                    if (p != null && p.Role == RoleTypeId.Scp096)
                    {
                        if (!Warhead.IsDetonated)
                            Teleport.Room(player, RoomIdentifier.AllRoomIdentifiers.First((r) => r.Name == RoomName.EzGateB));
                        else
                            Teleport.RoomRandom(player, RoomIdentifier.AllRoomIdentifiers.First(r => r.Zone == FacilityZone.Surface));
                    }
                });
            }
            else if (role == RoleTypeId.ClassD)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    Player p = Player.Get(player_id);
                    if (p == null || p.Role != RoleTypeId.ClassD)
                        return;

                    p.AddItem(ItemType.SCP330);
                    p.AddItem(ItemType.SCP330);
                    p.AddItem(ItemType.SCP330);
                    p.AddItem(ItemType.SCP330);
                    p.AddItem(ItemType.SCP330);
                    p.AddItem(ItemType.SCP330);
                    p.AddItem(ItemType.SCP207);
                    p.AddItem(ItemType.SCP207);
                    p.AddItem(ItemType.SCP207);
                    p.AddItem(ItemType.SCP207);
                    p.AddItem(ItemType.Medkit);
                    p.AddItem(ItemType.Painkillers);
                    p.AddItem(ItemType.KeycardFacilityManager);
                    p.SendBroadcast("Coke granted, check inv.", 20, shouldClearPrevious: true);

                    if (!late_join)
                        return;

                    foreach (var shyguy in Player.GetPlayers())
                    {
                        if (shyguy.RoleBase is Scp096Role scp096 && IsRaging(scp096.StateController.RageState))
                        {
                            Scp096TargetsTracker tracker;
                            if (scp096.SubroutineModule.TryGetSubroutine(out tracker))
                                tracker.AddTarget(player.ReferenceHub, false);
                        }
                    }
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerDeath)]
        void OnPlayerDeath(Player victim, Player attacker, DamageHandlerBase damageHandler)
        {
            if (victim != null)
                if (!found_winner)
                    found_winner = WinConditionLastClassD(victim);

            foreach (var p in Player.GetPlayers())
            {
                if (p.RoleBase is Scp096Role scp096)
                {
                    Scp096TargetsTracker tracker;
                    if (scp096.SubroutineModule.TryGetSubroutine(out tracker))
                        tracker.RemoveTarget(victim.ReferenceHub);
                }
            }
        }

        public static bool IsRaging(Scp096RageState rageState)
        {
            return (rageState == Scp096RageState.Distressed || rageState == Scp096RageState.Enraged);
        }

        [PluginEvent(ServerEventType.Scp096ChangeState)]
        public void OnScp096ChangeState(Player player, Scp096RageState rageState)
        {
            if (!IsRaging(rageState))
            {
                ForceRage(player);
            }
        }

        [PluginEvent(ServerEventType.PlayerEscape)]
        bool OnPlayerEscape(Player player, RoleTypeId role)
        {
            return false;
        }

        private static void ForceRage(Player player)
        {
            player.SendBroadcast("you will forcefully enrage in 5 seconds.", 5, shouldClearPrevious: true);
            Timing.CallDelayed(5.0f, () =>
            {
                if (player.RoleBase is Scp096Role scp096)
                {
                    if (!IsRaging(scp096.StateController.RageState))
                    {
                        scp096.StateController.SetRageState(Scp096RageState.Distressed);
                        Scp096TargetsTracker tracker;
                        if (scp096.SubroutineModule.TryGetSubroutine(out tracker))
                        {
                            foreach (var p in Player.GetPlayers())
                                if (p.IsAlive && p.Role == RoleTypeId.ClassD)
                                    tracker.AddTarget(p.ReferenceHub, false);
                        }
                        else
                            Log.Error("no TargetTracker");
                        Scp096RageManager rage;
                        if (scp096.SubroutineModule.TryGetSubroutine(out rage))
                        {
                            rage.EnragedTimeLeft = 60.0f * 60.0f;
                            rage.ServerSendRpc(true);
                        }
                        else
                            Log.Error("no RageManager");
                    }
                }
            });
        }
    }

    public class ShyGuyRampageEvent : IEvent
    {
        public static ShyGuyRampageEvent Singleton { get; private set; }

        public static bool IsRunning = false;

        public PluginHandler Handler;
        public string EventName { get; } = "Shy Guy Rampage";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription { get; set; } = "A Shy Guy spawns in entrance and is triggered by everyone. All other players are Class-D and spawn in their cells. Class-Ds get 6x SCP330, 4x SCP207, medkit, painkillers and a facility manager card. Surface is locked down. The last one alive win!\n\n";
        public string EventPrefix { get; } = "SGR";
        public bool OverrideWinConditions { get; }
        public bool BulletHolesAllowed { get; set; } = false;
        public PluginHandler PluginHandler { get; }
        public IEventConfig Config => EventConfig;

        [PluginConfig]
        public Config EventConfig;

        private Harmony harmony;

        public void PrepareEvent()
        {
            try
            {
                Log.Info("Rampage is preparing");
                IsRunning = true;
                EventHandler.Start();
                harmony = new Harmony("ShyGuyRampageEvent");
                harmony.PatchAll();
                Log.Info("Rampage is prepared");
                PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
            }
            catch(Exception ex)
            {
                Log.Error("prepare error: " + ex.ToString());
            }
        }

        public void StopEvent()
        {
            IsRunning = false;
            EventHandler.Stop();
            harmony.UnpatchAll("ShyGuyRampageEvent");
            harmony = null;
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Shy Guy Rampage", "1.0.0", "Everyone has triggered Shy Guy, last person to survive wins", "The Riptide")]
        public void OnEnabled()
        {
            Singleton = this;
            Handler = PluginHandler.Get(this);
        }

        [PluginUnload]
        public void OnDisabled()
        {
            StopEvent();
        }
    }
}