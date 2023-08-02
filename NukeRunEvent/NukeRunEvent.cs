using CedMod.Addons.Events;
using MapGeneration;
using MEC;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using Respawning;
using Interactables.Interobjects.DoorUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TheRiptide.EventUtility;
using CedMod.Addons.Events.Interfaces;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        public string Description { get; set; } = "Everyone spawns as a ClassD and are given 2 colas. The nuke is activated and the first person to escape wins!\n\n";
    }


    public class EventHandler
    {
        private Config config;
        private static bool found_winner = false;

        public EventHandler()
        {
            config = NukeRunEvent.Singleton.EventConfig;
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            EndRoom = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone == FacilityZone.Surface).First();
            RoomOffset = new UnityEngine.Vector3(40.000f, 14.080f, -32.600f);
            Round.IsLocked = true;

            RoomIdentifier spawn = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Name == RoomName.LczClassDSpawn).First();
            FacilityManager.LockRoom(spawn, DoorLockReason.AdminCommand);
            Timing.CallDelayed(10.0f, () =>
            {
                try
                {
                    FacilityManager.UnlockRoom(spawn, DoorLockReason.AdminCommand);
                    AlphaWarheadController.Singleton.enabled = true;
                    AlphaWarheadController.Singleton.StartDetonation(true, true);
                }
                catch (Exception ex)
                {
                    Log.Error("warhear error: " + ex.ToString());
                }
            });
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + NukeRunEvent.Singleton.EventName + "\n<size=32>" + NukeRunEvent.Singleton.EventDescription + "</size>", 20, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        bool OnRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if(player != null)
            {
                if(!found_winner)
                {
                    int player_id = player.PlayerId;
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
                else
                {
                    return HandleGameOverRoleChange(player, new_role);
                }
            }
            return true;
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            if(player !=null)
            {
                if (!found_winner)
                {
                    int player_id = player.PlayerId;
                    if (role == RoleTypeId.ClassD)
                    {
                        Timing.CallDelayed(0.0f, () =>
                        {
                            Player p = Player.Get(player_id);
                            if(p != null)
                            {
                                p.AddItem(ItemType.SCP207);
                                p.AddItem(ItemType.SCP207);
                                p.SendBroadcast("cola granted. check inv.", 3);
                            }
                        });
                    }
                }
                else
                {
                    HandleGameOverSpawn(player);
                }
            }
        }

        [PluginEvent(ServerEventType.WarheadDetonation)]
        void OnWarheadDetonation()
        {
            int alive_players = 0;
            foreach (var p in Player.GetPlayers())
                if (p.IsAlive)
                   alive_players++;

            if (alive_players == 0)
                Round.IsLocked = false;
        }

        [PluginEvent(ServerEventType.PlayerEscape)]
        void OnPlayerEscape(Player player, RoleTypeId role)
        {
            if (!found_winner)
            {
                found_winner = true;
                Timing.CallDelayed(0.0f, () =>
                {
                    FoundWinner(player);
                });
                Timing.CallDelayed(6.0f, () =>
                {
                    Round.IsLocked = false;
                });
            }
        }

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
    }

    public class NukeRunEvent:IEvent
    {
        public static NukeRunEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Nuke Run";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "NR";
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

        [PluginEntryPoint("Nuke Run Event", "1.0.0", "Nuke Run", "The Riptide")]
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
