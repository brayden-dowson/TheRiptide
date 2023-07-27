using CedMod.Addons.Events;
using CustomPlayerEffects;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.MicroHID;
using MapGeneration;
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
using HarmonyLib;
using System.Diagnostics;
using Interactables.Interobjects.DoorUtils;
using Interactables.Interobjects;
using CedMod.Addons.Events.Interfaces;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        [Description("how long in seconds does it take to recharge the micro")]
        public float RechargeTime { get; set; } = 15.0f;

        [Description("micro energy lifetime multiplier")]
        public float EnergyMultiplier { get; set; } = 2.0f;
    }

    public class EventHandler
    {
        private static Config config;
        private static HashSet<int> ghost_busters = new HashSet<int>();
        private static bool ff;
        private static CoroutineHandle update;
        private static RoomIdentifier heavy_a;
        private static RoomIdentifier heavy_b;
        private static RoomIdentifier ez_a;
        private static RoomIdentifier ez_b;

        public static void Start(Config config)
        {
            EventHandler.config = config;
            ff = Server.FriendlyFire;
            Server.FriendlyFire = false;
        }

        public static void Stop()
        {
            Server.FriendlyFire = ff;
            ghost_busters.Clear();
            Timing.KillCoroutines(update);
            heavy_a = null;
            heavy_b = null;
            ez_a = null;
            ez_b = null;
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + GhostBustersEvent.Singleton.EventName + "\n<size=32>" + GhostBustersEvent.Singleton.EventDescription + "</size>", 30, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            FacilityManager.LockRoom(RoomIdentifier.AllRoomIdentifiers.First(r => r.Zone == FacilityZone.Surface), DoorLockReason.AdminCommand);

            var mid = RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name == RoomName.HczCheckpointToEntranceZone);
            ez_a = mid.Where(r => r.Zone == FacilityZone.Entrance).First();
            ez_b = mid.Where(r => r.Zone == FacilityZone.Entrance).Last();
            heavy_a = FacilityManager.GetAdjacent(ez_a).Keys.Where(r => r.Zone == FacilityZone.HeavyContainment).First();
            heavy_b = FacilityManager.GetAdjacent(ez_b).Keys.Where(r => r.Zone == FacilityZone.HeavyContainment).First();
            FacilityManager.LockJoinedRooms(new HashSet<RoomIdentifier> { ez_a, heavy_a }, DoorLockReason.AdminCommand);
            FacilityManager.LockJoinedRooms(new HashSet<RoomIdentifier> { ez_b, heavy_b }, DoorLockReason.AdminCommand);

            foreach (var door in DoorVariant.DoorsByRoom[RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.EzGateA)])
                if (door is PryableDoor)
                    FacilityManager.OpenDoor(door);

            foreach (var door in DoorVariant.DoorsByRoom[RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.EzGateB)])
                if (door is PryableDoor)
                    FacilityManager.OpenDoor(door);

            foreach (var door in DoorVariant.DoorsByRoom[RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.EzIntercom)])
                if (door.Rooms.Count() == 1)
                    FacilityManager.OpenDoor(door);

            int ghost_buster_count = Mathf.RoundToInt(Player.Count / 5.0f);
            if (ghost_buster_count == 0)
                ghost_buster_count = 1;

            List<Player> players = Player.GetPlayers().ToList();
            ghost_busters.Clear();
            for (int i = 0; i < ghost_buster_count; i++)
                ghost_busters.Add(players.PullRandomItem().PlayerId);

            update = Timing.RunCoroutine(_Update());
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted ||
                new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Overwatch)
                return true;

            if(ghost_busters.Contains(player.PlayerId))
            {
                if (new_role != RoleTypeId.NtfSpecialist)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.NtfSpecialist);
                    });
                    return false;
                }
            }
            else
            {
                if (new_role != RoleTypeId.Scp106)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.Scp106);
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

            if (role == RoleTypeId.NtfSpecialist)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (role != RoleTypeId.NtfSpecialist)
                        return;
                    Teleport.Room(player, ez_a);
                    player.ClearInventory();
                    player.AddItem(ItemType.Radio);
                    player.AddItem(ItemType.MicroHID);
                    player.AddItem(ItemType.Painkillers);
                    player.AddItem(ItemType.Medkit);
                    player.AddItem(ItemType.Medkit);
                    player.AddItem(ItemType.SCP500);
                    player.AddItem(ItemType.SCP268);
                    player.AddItem(ItemType.SCP207);
                    player.EffectsManager.EnableEffect<Ensnared>(10);
                    player.EffectsManager.EnableEffect<Scanned>(10);
                });
            }
            else if (role == RoleTypeId.Scp106)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (role != RoleTypeId.Scp106)
                        return;
                    Teleport.Room(player, ez_b);
                    player.EffectsManager.EnableEffect<Ensnared>(10);
                    //player.EffectsManager.ChangeState<Disabled>(1);
                });
            }
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        bool OnRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerEnterPocketDimension)]
        public void OnPlayerEnterPocketDimension(Player player)
        {
            Timing.CallDelayed(0.0f, () =>
            {
                if(player.IsAlive)
                {
                    List<RoomIdentifier> ez = RoomIdentifier.AllRoomIdentifiers.Where(r => r.Zone == FacilityZone.Entrance).ToList();
                    Teleport.Room(player, ez.RandomItem());
                    player.EffectsManager.DisableEffect<Traumatized>();
                }
            });
        }

        [PluginEvent(ServerEventType.PlayerReceiveEffect)]
        void OnReceiveEffect(Player player, StatusEffectBase effect, byte intensity, float duration)
        {
            if(effect is Corroding)
                player.EffectsManager.DisableEffect<Corroding>();
        }

        [PluginEvent(ServerEventType.PlayerExitPocketDimension)]
        public void OnPlayerExitPocketDimension(Player player, bool is_succsefull)
        {
            if(is_succsefull)
            {
                List<RoomIdentifier> ez = RoomIdentifier.AllRoomIdentifiers.Where(r => r.Zone == FacilityZone.Entrance).ToList();
                Timing.CallDelayed(0.0f, () =>
                {
                    Teleport.Room(player, ez.RandomItem());
                });
            }
        }

        private static IEnumerator<float> _Update()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (true)
            {
                float delta = (float)stopwatch.Elapsed.TotalSeconds;
                try
                {
                    foreach (var player in Player.GetPlayers())
                    {
                        if (ghost_busters.Contains(player.PlayerId) && player.Role.IsHuman())
                        {
                            foreach (var item in player.ReferenceHub.inventory.UserInventory.Items.Values)
                            {
                                if (item is MicroHIDItem micro)
                                {
                                    if (micro.State == HidState.Idle && micro.RemainingEnergy < 1.0f)
                                    {
                                        micro.RemainingEnergy += delta * (1.0f / config.RechargeTime);
                                        micro.ServerSendStatus(HidStatusMessageType.EnergySync, micro.EnergyToByte);
                                    }
                                    else if (micro.State == HidState.Firing && micro.RemainingEnergy > 0.0f)
                                    {
                                        micro.RemainingEnergy += delta * (MicroHIDItem.FireEnergyConsumption * (1.0f - (1.0f / config.EnergyMultiplier)));
                                        micro.ServerSendStatus(HidStatusMessageType.EnergySync, micro.EnergyToByte);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (player.Role == RoleTypeId.Scp106 && player.Room != null)
                            {
                                if (player.Room == heavy_a || (player.Room == ez_a && ez_a.transform.InverseTransformPoint(player.Position).x < -7.0f))
                                    player.Position = ((25.0f * player.Position) + ez_a.transform.position) / 26.0f;
                                else if (player.Room == heavy_b || (player.Room == ez_b && ez_b.transform.InverseTransformPoint(player.Position).x < -7.0f))
                                    player.Position = ((25.0f * player.Position) + ez_b.transform.position) / 26.0f;
                                else if (player.Room.Zone != FacilityZone.Entrance)
                                    Teleport.Room(player, ez_b);
                            }
                        }
                    }
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                stopwatch.Restart();
                yield return Timing.WaitForOneFrame;
            }
        }
    }

    public class GhostBustersEvent:IEvent
    {
        public static GhostBustersEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Ghost Busters";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription { get; set; } = "All players spawn in entrance. Some players spawn as NTF specialists and the rest as 106. The specialists get a super micro which can fire with very little wind-up, recharges over time and has a longer duration. The specialists also get a coke, 2x medkit, painkillers, radio, scp500 and the hat. The specialists are also immune from the pocket dimention and will take 30 damage on hit. FriendlyFire is disabled for this event. The last team standing wins!\n\n";
        public string EventPrefix { get; } = "GB";
        public bool OverrideWinConditions { get; }
        public bool BulletHolesAllowed { get; set; } = false;
        public PluginHandler PluginHandler { get; }
        public IEventConfig Config => EventConfig;

        [PluginConfig]
        public Config EventConfig;
        private Harmony harmony;

        public void PrepareEvent()
        {
            Log.Info(EventName + " event is preparing");
            IsRunning = true;
            harmony = new Harmony("GhostBustersEvent");
            harmony.PatchAll();
            EventHandler.Start(EventConfig);
            Log.Info(EventName + " event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            harmony.UnpatchAll("GhostBustersEvent");
            EventHandler.Stop();
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Ghost Busters Event", "1.0.0", "", "The Riptide")]
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
