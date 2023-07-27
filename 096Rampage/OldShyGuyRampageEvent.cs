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
        static Player selected_096 = null;
        static bool late_join = false;
        static bool found_winner = false;
        static Player winner = null;
        static RoomIdentifier winner_room = null;
        static CoroutineHandle restart_handler = new CoroutineHandle();

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: Shy Guy Rampage\n<size=32>Everyone has triggered Shy Guy and there is no escape. Shy Guy spawns in entrance and enrages automaticaly after 30 seconds everyone else becomes a ClassD and spawns in light\nThe last one alive wins!</size>", 30, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            if (player.PlayerId == selected_096.PlayerId)
            {
                IEnumerable<Player> spectators = Player.GetPlayers().Where((x) => !x.IsAlive && x.PlayerId != player.PlayerId);
                if (spectators.Count() >= 1)
                    selected_096 = spectators.ElementAt(UnityEngine.Random.Range(0, spectators.Count()));
                else
                {
                    IEnumerable<Player> players = Player.GetPlayers().Where((x) => x.PlayerId != player.PlayerId);
                    if (players.Count() >= 1)
                        selected_096 = players.ElementAt(UnityEngine.Random.Range(0, players.Count()));
                }
                if (selected_096.PlayerId != player.PlayerId)
                    selected_096.SetRole(RoleTypeId.Scp096);
            }
        }

        [PluginEvent(ServerEventType.WaitingForPlayers)]
        void OnWaitingForPlayers()
        {
            //found_096 = false;
            late_join = false;
            selected_096 = null;
            found_winner = false;
            winner = null;
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            Log.Info("round start");
            winner_room = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Name == RoomName.HczCheckpointToEntranceZone && r.Zone == FacilityZone.HeavyContainment).First();
            if (OldShyGuyRampageEvent.Singleton.EventConfig.LockSurface)
                FacilityManager.LockRoom(RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone == FacilityZone.Surface).First(), DoorLockReason.AdminCommand);

            selected_096 = Player.GetPlayers().RandomItem();
            Timing.CallDelayed(1.0f, () => late_join = true);
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId newRole, RoleChangeReason reason)
        {
            Log.Info("player spawn");
            if (player != null)
            {
                if (!found_winner)
                {
                    HashSet<RoleTypeId> invalid_roles = new HashSet<RoleTypeId>
                    {
                        RoleTypeId.NtfPrivate,
                        RoleTypeId.NtfSergeant,
                        RoleTypeId.NtfSpecialist,
                        RoleTypeId.NtfCaptain,
                        RoleTypeId.ChaosConscript,
                        RoleTypeId.ChaosMarauder,
                        RoleTypeId.ChaosRepressor,
                        RoleTypeId.ChaosRifleman
                    };

                    if (invalid_roles.Contains(newRole))
                    {
                        Timing.CallDelayed(0.0f, () =>
                        {
                            player.SetRole(RoleTypeId.Spectator);
                        });
                        return false;
                    }

                    HashSet<RoleTypeId> valid_roles = new HashSet<RoleTypeId>
                    {
                        RoleTypeId.ClassD,
                        RoleTypeId.Spectator,
                        RoleTypeId.Tutorial,
                        RoleTypeId.Overwatch
                    };

                    if(player.PlayerId == selected_096.PlayerId)
                    {
                        if (newRole != RoleTypeId.Scp096)
                        {
                            Timing.CallDelayed(0.0f, () =>
                            {
                                player.SetRole(RoleTypeId.Scp096);
                            });
                            return false;
                        }
                    }
                    else if (!valid_roles.Contains(newRole))
                    {
                        Timing.CallDelayed(0.0f, () =>
                        {
                            player.SetRole(RoleTypeId.ClassD);
                        });
                        return false;
                    }
                    return true;
                }
                else
                {
                    if (player.PlayerId == winner.PlayerId)
                    {
                        if (newRole != RoleTypeId.ClassD)
                        {
                            Timing.CallDelayed(0.0f, () =>
                            {
                                player.SetRole(RoleTypeId.ClassD);
                            });
                            return false;
                        }
                    }
                    else if (newRole != RoleTypeId.NtfCaptain && newRole != RoleTypeId.Spectator)
                    {
                        Timing.CallDelayed(0.0f, () =>
                        {
                            player.SetRole(RoleTypeId.NtfCaptain);
                        });
                        return false;
                    }
                    return true;
                }
            }
            return true;
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            if (player != null)
            {
                if (!found_winner)
                {
                    if (role == RoleTypeId.Scp096)
                    {
                        player.SendBroadcast("you will forcefully enrage in 30 seconds.", 25, shouldClearPrevious: true);
                        Timing.CallDelayed(25.0f, () =>
                        {
                            ForceRage(player);
                        });
                        Timing.CallDelayed(0.0f, () =>
                        {
                            try
                            {
                                Teleport.Room(player, RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Name == RoomName.EzGateB).First());
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.ToString());
                            }
                        });
                    }
                    else if (role == RoleTypeId.ClassD)
                    {
                        Timing.CallDelayed(0.0f, () =>
                        {
                            if (player.Role == RoleTypeId.ClassD)
                            {
                                player.AddItem(ItemType.SCP207);
                                player.AddItem(ItemType.SCP207);
                                player.AddItem(ItemType.SCP207);
                                player.AddItem(ItemType.SCP207);
                                player.AddItem(ItemType.Medkit);
                                player.AddItem(ItemType.Painkillers);
                                player.AddItem(ItemType.SCP330);
                                player.AddItem(ItemType.SCP330);
                                player.AddItem(ItemType.SCP330);
                                player.AddItem(ItemType.SCP330);
                                player.AddItem(ItemType.SCP330);
                                player.AddItem(ItemType.SCP330);
                                player.AddItem(ItemType.Adrenaline);
                                player.SendBroadcast("Coke granted, check inv.", 20, shouldClearPrevious: true);
                                if (late_join)
                                {
                                    foreach (var p in Player.GetPlayers())
                                    {
                                        if (p.RoleBase is Scp096Role scp096 && IsRaging(scp096.StateController.RageState))
                                        {
                                            Scp096TargetsTracker tracker;
                                            if (scp096.SubroutineModule.TryGetSubroutine(out tracker))
                                                tracker.AddTarget(player.ReferenceHub, false);
                                        }
                                    }
                                }
                            }
                        });
                    }
                }
                else
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        try
                        {
                            Teleport.Room(player, winner_room);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    });

                    if (player.PlayerId == winner.PlayerId)
                    {
                        Timing.CallDelayed(0.0f, () =>
                        {
                            GrantWinnerReward(winner);
                        });
                    }
                    else
                    {
                        Timing.CallDelayed(1.0f, () =>
                        {
                            player.ClearInventory();
                            player.EffectsManager.EnableEffect<CustomPlayerEffects.Ensnared>(0, false);
                        });
                    }
                }
            }
        }

        [PluginEvent(ServerEventType.PlayerDeath)]
        void OnPlayerDeath(Player victim, Player attacker, DamageHandlerBase damageHandler)
        {
            if (victim != null)
            {
                if (!found_winner)
                {
                    int dclass_alive = 0;
                    foreach (var p in Player.GetPlayers())
                        if (p.Role == RoleTypeId.ClassD)
                            dclass_alive++;
                    if (dclass_alive == 0)
                    {
                        found_winner = true;
                        winner = victim;
                    }
                    else if (dclass_alive == 1)
                    {
                        found_winner = true;
                        foreach (var p in Player.GetPlayers())
                            if (p.Role == RoleTypeId.ClassD)
                                winner = p;
                    }

                    if (found_winner)
                    {
                        winner.EffectsManager.ChangeState<CustomPlayerEffects.DamageReduction>(255);
                        winner.SendBroadcast("You Won!", 5, shouldClearPrevious:true);
                        Timing.CallDelayed(5.0f, () =>
                        {
                            foreach (var p in Player.GetPlayers())
                            {
                                if (p.PlayerId != winner.PlayerId)
                                    p.SetRole(RoleTypeId.NtfCaptain);
                                else
                                {
                                    if (p.IsAlive)
                                        GrantWinnerReward(p);
                                    else
                                        p.SetRole(RoleTypeId.ClassD);
                                }
                            }
                        });
                        restart_handler = Timing.CallDelayed(45.0f, () =>
                        {
                            Round.Restart(false);
                        });
                    }
                }
            }

            foreach(var p in Player.GetPlayers())
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

        [PluginEvent(ServerEventType.RoundEnd)]
        void OnRoundEnd(RoundSummary.LeadingTeam leadingTeam)
        {
            Stop();
        }

        [PluginEvent(ServerEventType.RoundRestart)]
        void OnRoundRestart()
        {
            Stop();
        }

        [PluginEvent(ServerEventType.PlayerEscape)]
        bool OnPlayerEscape(Player player, RoleTypeId role)
        {
            player.ReceiveHint("there is no escape");
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
                        Scp096RageManager rage;
                        if (scp096.SubroutineModule.TryGetSubroutine(out rage))
                        {
                            rage.EnragedTimeLeft = 60.0f * 60.0f;
                        }
                    }
                }
            });
        }

        public static void GrantWinnerReward(Player winner)
        {
            Teleport.Room(winner, winner_room);
            winner.ClearInventory();
            winner.AddItem(ItemType.MicroHID);
            winner.AddItem(ItemType.GrenadeHE);
            winner.AddItem(ItemType.Jailbird);
            winner.AddItem(ItemType.ParticleDisruptor);
            winner.AddItem(ItemType.SCP018);
            winner.AddItem(ItemType.SCP244a);
            winner.AddItem(ItemType.GunLogicer);
            winner.AddAmmo(ItemType.Ammo762x39, 200);
            winner.SendBroadcast("Now destroy the losers.\ncheck inv.", 20, shouldClearPrevious: true);
            winner.EffectsManager.ChangeState<CustomPlayerEffects.DamageReduction>(255);
        }

        public static void Start()
        {
            late_join = false;
            selected_096 = null;
            found_winner = false;
        }

        public static void Stop()
        {
            Timing.KillCoroutines(restart_handler);
            selected_096 = null;
            late_join = false;
            found_winner = false;
            winner = null;
            winner_room = null;
        }
    }

    public class OldShyGuyRampageEvent : IEvent
    {
        public static OldShyGuyRampageEvent Singleton { get; private set; }

        public static bool IsRunning = false;

        public PluginHandler Handler;
        public string EventName { get; } = "Shy Guy Rampage";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription { get; set; } = "Everyone has triggered Shy Guy, last person to survive wins\n";
        public string EventPrefix { get; } = "Rampage";
        public bool OverrideWinConditions { get; }
        public bool BulletHolesAllowed { get; set; } = false;
        public PluginHandler PluginHandler { get; }
        public IEventConfig Config => EventConfig;

        [PluginConfig]
        public Config EventConfig;

        public OldShyGuyRampageEvent()
        {
            Singleton = this;
        }

        public void PrepareEvent()
        {
            Log.Info("Rampage is preparing");
            IsRunning = true;
            EventHandler.Start();
            Log.Info("Rampage is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            EventHandler.Stop();
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Shy Guy Rampage", "1.0.0", "Everyone has triggered Shy Guy, last person to survive wins", "The Riptide")]
        public void OnEnabled()
        {
            Handler = PluginHandler.Get(this);
        }

        [PluginUnload]
        public void OnDisabled()
        {
            StopEvent();
        }
    }
}
