using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using MEC;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using slocLoader;
using slocLoader.Objects;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using static TheRiptide.Utility;
using static TheRiptide.EventUtility;
using Respawning;
using MapGeneration;
using System.Linq;
using PlayerStatsSystem;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;
    }

    public class EventHandler
    {
        private static Vector3 map_offset = new Vector3(0.0f, 900, 0.0f);
        private static Vector3 spawn_position = new Vector3(-5.0f, 903.0f, 5.0f);
        private static bool found_winner;
        private static HashSet<int> dogs = new HashSet<int>();
        private static bool late_spawn = false;
        private static List<string> names = new List<string>();

        public static void Start()
        {
            found_winner = false;
            WinnerReset();
        }

        public static void Stop()
        {
            found_winner = false;
            WinnerReset();
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + PrincessBanquetEvent.Singleton.EventName + "\n<size=24>" + PrincessBanquetEvent.Singleton.EventDescription + "</size>", 30, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            EndRoom = RoomIdentifier.AllRoomIdentifiers.First((r) => r.Zone == FacilityZone.Surface);
            RoomOffset = new Vector3(40.000f, 14.080f, -32.600f);

            List<Player> players = ReadyPlayers();

            dogs.Clear();
            int dog_count = 3;
            for (int i = 0; i < dog_count; i++)
                if (!players.IsEmpty())
                    dogs.Add(players.PullRandomItem().PlayerId);

            List<slocGameObject> objects;
            if (slocLoader.AutoObjectLoader.AutomaticObjectLoader.TryGetObjects("ariahouse", out objects))
            {
                GameObject root = API.SpawnObjects(objects, map_offset, Quaternion.Euler(Vector3.zero));
            }
            else
            {
                Log.Error("ariahouse.sloc make sure you have this inside the SlocLoader/Objects folder");
            }

            late_spawn = false;
            Timing.CallDelayed(15.0f, () => late_spawn = true);
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted ||
                new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Overwatch)
                return true;

            if (found_winner)
                return HandleGameOverRoleChange(player, new_role);

            if (new_role == RoleTypeId.Scp939 && late_spawn)
                return true;

            if(dogs.Contains(player.PlayerId))
            {
                if(new_role != RoleTypeId.Scp939)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.Scp939);
                    });
                    return false;
                }
            }
            else if (player.PlayerId % 2 == 0)
            {
                if (new_role != RoleTypeId.ClassD)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.ClassD);
                    });
                    return false;
                }
            }
            else
            {
                if (new_role != RoleTypeId.Scientist)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.Scientist);
                    });
                    return false;
                }
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

            if (role == RoleTypeId.Scp939)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role != RoleTypeId.Scp939)
                        return;
                    if (names.IsEmpty())
                    {
                        names = new List<string>
                        {
                            "Daisy",
                            "Cupcakes",
                            "Princess",
                            "Pitbull Gaming",
                            "Lila",
                            "Aurora",
                            "Baby",
                            "Bella",
                            "Luna",
                            "Honey",
                            "Queen",
                            "Angel",
                            "Cookie",
                            "Sugar",
                            "Teddy",
                            "Lulu",
                            "Dashie"
                        };
                    }
                    player.ReferenceHub.nicknameSync.Network_displayName = names.PullRandomItem();
                    if (late_spawn)
                        player.Position = spawn_position;
                    else
                    {
                        player.SendBroadcast("you will teleport in 15 seconds", 15, shouldClearPrevious: true);
                        player.Position = new Vector3(0.0f, 500.0f, 0.0f);
                        Timing.CallDelayed(15.0f, () => player.Position = spawn_position);
                    }
                    SetScale(player, 0.6f);
                });
            }
            else if(role == RoleTypeId.ClassD)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role != RoleTypeId.ClassD)
                        return;
                    player.Position = spawn_position;
                    SetScale(player, 0.6f);
                });
            }
            else if(role == RoleTypeId.Scientist)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role != RoleTypeId.Scientist)
                        return;
                    player.Position = spawn_position;
                    SetScale(player, 0.6f);
                });
            }
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        bool OnRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerDeath)]
        void OnPlayerDeath(Player victim, Player attacker, DamageHandlerBase damageHandler)
        {
            if (victim == null || !Round.IsRoundStarted)
                return;

            if (!found_winner)
            {
                int humans_alive = 0;
                foreach (var p in Player.GetPlayers())
                    if (p.Role.IsHuman())
                        humans_alive++;
                if (humans_alive == 0)
                {
                    found_winner = true;
                    FoundWinner(victim);
                }
                else if (humans_alive == 1)
                {
                    foreach (var p in Player.GetPlayers())
                    {
                        if (p.Role.IsHuman())
                        {
                            found_winner = true;
                            FoundWinner(p);
                            break;
                        }
                    }
                }
            }
        }
    }

    public class PrincessBanquetEvent : IEvent
    {
        public static PrincessBanquetEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Princess Banquet";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription { get; set; } = "todo\n\n";
        public string EventPrefix { get; } = "PB";
        public bool OverrideWinConditions { get; }
        public bool BulletHolesAllowed { get; set; } = false;
        public PluginHandler PluginHandler { get; }
        public IEventConfig Config => EventConfig;

        [PluginConfig]
        public Config EventConfig;

        public void PrepareEvent()
        {
            Log.Info(EventName + " event is preparing");
            IsRunning = true;
            EventHandler.Start();
            Log.Info(EventName + " event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            EventHandler.Stop();
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Princess Banquet Event", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            Singleton = this;
            //PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
            Handler = PluginHandler.Get(this);
        }

        [PluginUnload]
        public void OnDisabled()
        {
            StopEvent();
        }
    }
}
