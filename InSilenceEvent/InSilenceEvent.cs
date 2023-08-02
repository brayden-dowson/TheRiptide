using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using CustomPlayerEffects;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using MapGeneration.Distributors;
using MEC;
using PlayerRoles;
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

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;
        public string Description { get; set; } = "All players will spawn in heavy containment zone and both the elevator systems and the entrance zone will be locked down. Everyone will be spawned as a scientist and will have to find some guns to kill the SCP939s that will be trying to find and kill everyone. If a generator is turned on the SCP079 doors will open allowing players access to its armor and pressing the overcharge which will turn the lights back on if they are turned off. The SCP939s will have only 1 hp while having 700 shield\n\n";
    }

    public class EventHandler
    {
        private static HashSet<int> dogs = new HashSet<int>();
        private static int gens_activated = 0; 

        public static void Start()
        {
            gens_activated = 0;
            dogs.Clear();
        }

        public static void Stop()
        {
            gens_activated = 0;
            dogs.Clear();
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + InSilenceEvent.Singleton.EventName + "\n<size=24>" + InSilenceEvent.Singleton.EventDescription + "</size>", 30, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            int attempt = 0;
            int dog_count = Mathf.FloorToInt(Player.Count / 15);
            if (dog_count == 0)
                dog_count = 1;
            while (dogs.Count < dog_count && attempt < 100)
            {
                dogs.Add(Player.GetPlayers().ElementAt(Random.Range(0, Player.GetPlayers().Count)).PlayerId);
                attempt++;
            }

            FacilityManager.LockRoom(RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.LczCheckpointA), DoorLockReason.AdminCommand);
            FacilityManager.LockRoom(RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.LczCheckpointB), DoorLockReason.AdminCommand);
            FacilityManager.LockRooms(RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name == RoomName.HczCheckpointToEntranceZone && r.Zone == FacilityZone.Entrance).ToHashSet(), DoorLockReason.AdminCommand);

            Timing.CallDelayed(3.0f, () =>
            {
                if (Player.Count >= 15)
                    FacilityManager.SetAllRoomLightStates(false);
                Cassie.Message("pitch_0.10 .G7 .");
            });
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

            if (dogs.Contains(player.PlayerId))
            {
                if (new_role != RoleTypeId.Scp939 && new_role != RoleTypeId.Spectator && new_role != RoleTypeId.Overwatch && new_role != RoleTypeId.Tutorial)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.Scp939);
                    });
                    return false;
                }
            }
            else
            {
                if (new_role != RoleTypeId.Scientist && new_role != RoleTypeId.Spectator && new_role != RoleTypeId.Overwatch && new_role != RoleTypeId.Tutorial)
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

            if(role == RoleTypeId.Scp939)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role != RoleTypeId.Scp939)
                        return;
                    player.Health = 1.0f;
                    player.EffectsManager.EnableEffect<Ensnared>(15);
                    player.SendBroadcast("Ensnared for 15 seconds", 15, shouldClearPrevious: true);
                });
            }
            else if (role == RoleTypeId.Scientist)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role != RoleTypeId.Scientist)
                        return;
                    player.AddItem(ItemType.Flashlight);
                    var heavy = RoomIdentifier.AllRoomIdentifiers.Where(r => r.Zone == FacilityZone.HeavyContainment && r.Name != RoomName.HczArmory && r.Name != RoomName.HczCheckpointToEntranceZone);
                    Teleport.Room(player, heavy.ElementAt(Random.Range(0, heavy.Count())));
                });
            }
        }

        [PluginEvent(ServerEventType.GeneratorActivated)]
        void OnGeneratorActivated(Scp079Generator gen)
        {
            gens_activated++;
            if(gens_activated == 1)
            {
                RoomIdentifier scp079 = RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Hcz079);
                foreach(var door in DoorVariant.DoorsByRoom[scp079])
                {
                    if(door is PryableDoor)
                    {
                        FacilityManager.UnlockDoor(door, DoorLockReason.AdminCommand);
                        FacilityManager.OpenDoor(door);
                    }
                }
            }
        }

    }

    public class InSilenceEvent:IEvent
    {
        public static InSilenceEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "In Silence";
        public string EvenAuthor { get; } = "The Riptide. Idea by Guy in Grey";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "IS";
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

        [PluginEntryPoint("In Silence Event", "1.0.0", "", "The Riptide")]
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
