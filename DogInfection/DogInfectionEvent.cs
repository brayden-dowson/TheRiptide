using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using MEC;
using PlayerRoles;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using Respawning;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TheRiptide.EventUtility;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;
    }

    public class EventHandler
    {
        public Config config = null;
        public static HashSet<int> infected = new HashSet<int>();
        public static RoomIdentifier scp173_room;
        public static bool found_winner = false;

        public EventHandler()
        {
            config = DogInfectionEvent.Singleton.EventConfig;
        }

        public static void Start()
        {
            infected.Clear();
            found_winner = false;
            WinnerReset();
        }

        public static void Stop()
        {
            infected.Clear();
            found_winner = false;
            WinnerReset();
            scp173_room = null;
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            ClearAllItems();
            LockDownLight();
            EndRoom = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone == FacilityZone.Surface).First();
            RoomOffset = new UnityEngine.Vector3(40.000f, 14.080f, -32.600f);

            scp173_room = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Name == RoomName.Lcz173).First();
            infected.Add(Player.GetPlayers().RandomItem().PlayerId);
            Timing.CallDelayed(3.0f, () =>
            {
                Cassie.Message("pitch_0.10 .G7 .");
            });

        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: Dog Infection\n<size=32>Light is locked down and everyone spawns as ClassD in light with one dog. The dogs only spawn inside 173s room. Dogs can infect ClassD on kill, the last ClassD alive wins!</size>", 30, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            if (!Round.IsRoundStarted)
                return;

            infected.Remove(player.PlayerId);
            if (infected.Count == 0)
            {
                IEnumerable<Player> spectators = Player.GetPlayers().Where((x) => !x.IsAlive && x.PlayerId != player.PlayerId);
                if (spectators.Count() >= 1)
                    infected.Add(spectators.ElementAt(UnityEngine.Random.Range(0, spectators.Count())).PlayerId);
                else
                {
                    IEnumerable<Player> players = Player.GetPlayers().Where((x) => x.PlayerId != player.PlayerId);
                    if (players.Count() >= 1)
                        infected.Add(players.ElementAt(UnityEngine.Random.Range(0, players.Count())).PlayerId);
                }
                if (infected.Count == 1)
                    foreach (var p in Player.GetPlayers())
                        if (infected.First() == p.PlayerId)
                            p.SetRole(RoleTypeId.Scp939);
            }
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

            if(found_winner)
                return HandleGameOverRoleChange(player, new_role);

            int player_id = player.PlayerId;
            if (infected.Contains(player.PlayerId))
            {
                if (new_role != RoleTypeId.Scp939 && new_role != RoleTypeId.Spectator)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        Player p = Player.Get(player_id);
                        if (p != null)
                            p.SetRole(RoleTypeId.Scp939);
                    });
                    return false;
                }
            }
            else
            {
                if (new_role != RoleTypeId.ClassD && new_role != RoleTypeId.Spectator)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        Player p = Player.Get(player_id);
                        if (p != null)
                            p.SetRole(RoleTypeId.ClassD);
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

            if(found_winner)
            {
                HandleGameOverSpawn(player);
                return;
            }

            if (role == RoleTypeId.Scp939)
            {
                UnityEngine.Vector3 offset = new UnityEngine.Vector3(16.000f, 12.429f, 8.000f);
                int player_id = player.PlayerId;
                Timing.CallDelayed(0.0f, () =>
                {
                    Player p = Player.Get(player_id);
                    if (p != null && p.Role == RoleTypeId.Scp939)
                    {
                        p.ClearInventory();
                        Teleport.RoomPos(p, scp173_room, offset);
                    }
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerDeath)]
        void OnPlayerDeath(Player victim, Player attacker, DamageHandlerBase damageHandler)
        {
            if (!Round.IsRoundStarted)
                return;

            if (victim != null)
            {
                if (!found_winner)
                {
                    int player_id = victim.PlayerId;
                    Timing.CallDelayed(1.0f, () =>
                    {
                        Player player = Player.Get(player_id);
                        if(player != null)
                        {
                            infected.Add(victim.PlayerId);
                            player.SetRole(RoleTypeId.Scp939);
                        }
                    });

                    found_winner = WinConditionLastClassD(victim);
                }
            }
        }
    }

    public class DogInfectionEvent:IEvent
    {
        public static DogInfectionEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Dog Infection";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription { get; set; } = "[recommended player count 30+] Light is locked down and everyone spawns as ClassD in light with one dog. Dog can infect Class-Ds on kill, the last ClassD alive wins\n\n";
        public string EventPrefix { get; } = "DI";
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

        [PluginEntryPoint("Dog Infection Event", "1.0.0", "Dogs infect players on kill causing them to become dogs too, everyone is locked in light the last player alive wins", "The Riptide")]
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
