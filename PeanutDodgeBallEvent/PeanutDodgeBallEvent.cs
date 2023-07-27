using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using CustomPlayerEffects;
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
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
        private static bool found_winner = false;
        private static int peanut;
        private static RoomIdentifier room_173;

        public static void Start()
        {
            found_winner = false;
            WinnerReset();
        }

        public static void Stop()
        {
            found_winner = false;
            WinnerReset();
            room_173 = null;
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + PeanutDodgeBallEvent.Singleton.EventName + "\n<size=32>" + PeanutDodgeBallEvent.Singleton.EventDescription + "</size>", 30, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            if(peanut == player.PlayerId)
            {
                Player selected = Player.GetPlayers().Where(p => p != player).ToList().RandomItem();
                peanut = selected.PlayerId;
                selected.SetRole(RoleTypeId.Scp173);
            }
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        bool OnRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            return false;
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            Round.IsLocked = true;
            Timing.CallDelayed(30.0f, () => Round.IsLocked = false);
            room_173 = RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Lcz173);

            foreach (var door in DoorVariant.DoorsByRoom[room_173])
            {
                FacilityManager.LockDoor(door, DoorLockReason.AdminCommand);
                if(door is Timed173PryableDoor pd)
                {
                    pd._stopwatch.Stop();
                }
            }

            EndRoom = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone == FacilityZone.Surface).First();
            RoomOffset = new Vector3(40.000f, 14.080f, -32.600f);

            peanut = Player.GetPlayers().RandomItem().PlayerId;
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted || new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Overwatch)
                return true;

            if (found_winner)
                return HandleGameOverRoleChange(player, new_role);

            if (player.PlayerId == peanut)
            {
                if (new_role != RoleTypeId.Scp173)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.Scp173);
                    });
                    return false;
                }
            }
            else
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

            if(peanut == player.PlayerId && role == RoleTypeId.Scp173)
            {
                Timing.CallDelayed(0.0f,()=>
                {
                    if (player.Role == RoleTypeId.Scp173)
                        Teleport.RoomPos(player, room_173, new Vector3(7.906f, 12.429f, 8.101f));
                });
                //Timing.CallDelayed(25.0f, () =>
                //{
                //    if (player.Role == RoleTypeId.Scp173)
                //        player.EffectsManager.EnableEffect<Ensnared>(5);
                //});
                Timing.CallDelayed(7.0f, () =>
                {
                    if (player.Role == RoleTypeId.Scp173)
                        Teleport.RoomPos(player, room_173, new Vector3(15.512f, 12.429f, 7.929f));
                });
            }
            else
            {
                //player.SendBroadcast("Event starting in 30 seconds", 27, shouldClearPrevious: true);
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role == RoleTypeId.ClassD)
                        Teleport.RoomPos(player, room_173, new Vector3(15.512f, 12.429f, 7.929f));
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerDeath)]
        void OnPlayerDeath(Player victim, Player attacker, DamageHandlerBase damageHandler)
        {
            if (victim == null || !Round.IsRoundStarted)
                return;

            if (!found_winner)
                found_winner = WinConditionLastClassD(victim);

            if (found_winner)
                return;
        }
    }

    public class PeanutDodgeBallEvent:IEvent
    {
        public static PeanutDodgeBallEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Peanut Dodgeball";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription { get; set; } = "Everyone spawns in 173s room with 173. The last one alive wins!\n\n";
        public string EventPrefix { get; } = "PD";
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

        [PluginEntryPoint("Peanut Dodgeball Event", "1.0.0", "Everyone spawns in 173s room with 173 the last one alive wins", "The Riptide")]
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
