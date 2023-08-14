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
using static TheRiptide.Utility;
using static TheRiptide.EventUtility;
using InventorySystem.Items.Usables.Scp330;
using HarmonyLib;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        [Description("Chance to enable speedy mode (all players spawn with 6 colas and 6 yellow candy) 0.0 = 0%, 1.0 = 100%")]
        public float SpeedyChance { get; set; } = 0.25f;

        [Description("Chance to enable spooky mode (players are given ghost lights at random). will override lights out mode. 0.0 = 0%, 1.0 = 100%")]
        public float SpookyChance { get; set; } = 0.3f;

        [Description("Chance to enable invis mode (all players given SCP268) 0.0 = 0%, 1.0 = 100%")]
        public float InvisChance { get; set; } = 0.25f;

        [Description("Chance to enable lights out mode (facility light out and all players are given a flashlight). will override spooky mode. 0.0 = 0%, 1.0 = 100%")]
        public float LightsOutChance { get; set; } = 0.2f;

        [Description("Chance to enable replication mode (peanuts replicate on kill) 0.0 = 0%, 1.0 = 100%")]
        public float ReplicationChance { get; set; } = 0.05f;

        [Description("Chance to enable double nut mode (start with two peanuts) 0.0 = 0%, 1.0 = 100%")]
        public float DoubleNutChance { get; set; } = 0.0f;

        [Description("Chance to enable child mode (all humans are small) 0.0 = 0%, 1.0 = 100%")]
        public float ChildChance { get; set; } = 0.10f;

        [Description("Chance to enable baby173 mode (scp173 is very small) 0.0 = 0%, 1.0 = 100%")]
        public float Baby173Chance { get; set; } = 0.10f;

        [Description("chance a player will be given a ghost light every minute when in spooky mode. 0.0 = 0%, 1.0 = 100%")]
        public float SpookyModeChancePerMinute { get; set; } = 0.5f;

        public float ChildSize { get; set; } = 0.5f;
        public float Baby173Size { get; set; } = 0.25f;

        [Description("Chance an unnamed room will be used e.g. Zone: Lcz, Shape: Curve. 0.0 = 0%, 1.0 = 100%")]
        public float UnnamedRoomChance { get; set; } = 0.075f;

        [Description("Forces replication mode on harder rooms")]
        public List<RoomName> ForceReplication { get; set; } = new List<RoomName>
        {
            RoomName.Outside,
            RoomName.Hcz049,
        };

        [Description("Forces double nut mode on harder rooms(disabled if replication is enabled)")]
        public List<RoomName> ForceDoubleNut { get; set; } = new List<RoomName>
        {
            RoomName.EzOfficeSmall,
            RoomName.HczTestroom,
            RoomName.Hcz079,
            RoomName.Hcz106,
            RoomName.Lcz173,
            RoomName.LczClassDSpawn,
        };

        [Description("Room blacklist")]
        public List<RoomName> RoomBlacklist { get; set; } = new List<RoomName>
        {
            RoomName.Unnamed,
            RoomName.Pocket,
            RoomName.EzCollapsedTunnel,
            RoomName.EzEvacShelter
        };

        public string Description { get; set; } = "Everyone spawns in a random room with 173. Round may be modified with many random gamemodes. The last one alive wins!\n\n";

        [Description("Rooms (do not edit)")]
        public List<RoomName> Rooms { get; set; } = System.Enum.GetValues(typeof(RoomName)).ToArray<RoomName>().ToList();
    }

    [System.Flags]
    public enum GameMode
    {
        None = 0,
        Speedy = 1,
        Spooky = 2,
        Invis = 4,
        LightsOut = 8,
        Replication = 16,
        Double = 32,
        Child = 64,
        Baby173 = 128
    }

    public class EventHandler
    {
        private static Config config;
        private static bool found_winner = false;
        private static HashSet<int> peanuts = new HashSet<int>();

        public static GameMode mode;
        private static RoomName room_name;
        private static FacilityZone room_zone;
        private static RoomShape room_shape;
        private static RoomIdentifier room;
        private static CoroutineHandle spooky_update;
        private static Dictionary<RoomName, Vector3> custom_offset = new Dictionary<RoomName, Vector3>
        {
            { RoomName.HczWarhead, new Vector3(-6.265f, 291.940f, 5.38f) },
            { RoomName.Hcz049, new Vector3(-5.069f, 193.400f, -10.025f) }
        };
        private static bool late_spawn = false;
        private static string mode_str = "";

        public static void Start(Config config)
        {
            EventHandler.config = config;
            found_winner = false;
            WinnerReset();

            mode = GameMode.None;
            if (Random.value < config.SpeedyChance)
                mode |= GameMode.Speedy;
            if (Random.value < config.SpookyChance)
                mode |= GameMode.Spooky;
            if (Random.value < config.InvisChance)
                mode |= GameMode.Invis;
            if (Random.value < config.LightsOutChance)
                mode |= GameMode.LightsOut;
            if (Random.value < config.ReplicationChance)
                mode |= GameMode.Replication;
            if (Random.value < config.DoubleNutChance)
                mode |= GameMode.Double;
            if (Random.value < config.ChildChance)
                mode |= GameMode.Child;
            if (Random.value < config.Baby173Chance)
                mode |= GameMode.Baby173;

            int attempt = 0;
            room = null;
            if (Random.value < config.UnnamedRoomChance)
            {
                while (room == null && attempt < 1000)
                {
                    room_name = RoomName.Unnamed;
                    room_zone = (FacilityZone)Random.Range(1, 4);
                    room_shape = (RoomShape)Random.Range(2, 6);

                    var rooms = RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name == room_name && r.Zone == room_zone && r.Shape == room_shape);
                    if (!rooms.IsEmpty())
                        room = rooms.First();
                    attempt++;
                }
            }
            else
            {
                while (room == null && attempt < 1000)
                {
                    room_name = System.Enum.GetValues(typeof(RoomName)).ToArray<RoomName>().RandomItem();
                    while (config.RoomBlacklist.Contains(room_name))
                        room_name = System.Enum.GetValues(typeof(RoomName)).ToArray<RoomName>().RandomItem();

                    var rooms = RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name == room_name);
                    if (!rooms.IsEmpty())
                        room = rooms.First();
                    attempt++;
                }
            }
            if(room == null)
            {
                Log.Error("Could not find a valid room, check your blacklist");
                mode_str = "[Error] Check plugin configuration";
                return;
            }    

            if (config.ForceReplication.Contains(room_name))
                mode |= GameMode.Replication;
            if (config.ForceDoubleNut.Contains(room_name))
                mode |= GameMode.Double;
            if (mode.HasFlag(GameMode.Replication))
                mode &= ~GameMode.Double;

            if (mode.HasFlag(GameMode.Spooky) && mode.HasFlag(GameMode.LightsOut))
            {
                if (Random.value < 0.5)
                    mode &= ~GameMode.Spooky;
                else
                    mode &= ~GameMode.LightsOut;
            }

            List<string> modes = new List<string>();
            foreach (var gm in System.Enum.GetValues(typeof(GameMode)).ToArray<GameMode>())
                if (gm != GameMode.None && mode.HasFlag(gm))
                    modes.Add("[" + gm.ToString() + "]");
            mode_str = "<color=#FFFF00>" + (room_name != RoomName.Unnamed ? "Room: " + room_name.ToString() : "Zone: " + room_zone.ToString() + ", Shape: " + room_shape.ToString()) + " " + string.Join(", ", modes) + "</color>";
            late_spawn = false;
        }

        public static void Stop()
        {
            found_winner = false;
            WinnerReset();
            room = null;
            Timing.KillCoroutines(spooky_update);
            late_spawn = false;
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + PeanutDodgeBallEvent.Singleton.EventName + "\n<size=32>" + PeanutDodgeBallEvent.Singleton.EventDescription.Replace("\n", "") + "\n" + mode_str + "</size>", 30, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            peanuts.Remove(player.PlayerId);
            if (peanuts.IsEmpty() || (peanuts.Count == 1 && mode.HasFlag(GameMode.Double)))
            {
                List<Player> valid = ReadyPlayers().Where(p => p.Role == RoleTypeId.Spectator && p != player).ToList();
                if (valid.IsEmpty())
                    valid = ReadyPlayers().Where(p => p != player).ToList();
                if (valid.IsEmpty())
                    return;
                Player selected = valid.RandomItem();
                peanuts.Add(selected.PlayerId);
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
            EndRoom = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone == FacilityZone.Surface).First();
            RoomOffset = new Vector3(40.000f, 14.080f, -32.600f);

            FacilityManager.LockAllRooms(DoorLockReason.AdminCommand);
            foreach (var group in ElevatorDoor.AllElevatorDoors)
                foreach (var door in group.Value)
                    FacilityManager.LockDoor(door, DoorLockReason.AdminCommand);

            foreach (var door in DoorVariant.DoorsByRoom[RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Hcz079)])
                door.UnlockLater(0.0f, DoorLockReason.SpecialDoorFeature);

            List<Player> ready = ReadyPlayers();
            peanuts.Add(ready.PullRandomItem().PlayerId);

            if (mode.HasFlag(GameMode.Double))
                if (!ready.IsEmpty())
                    peanuts.Add(ready.PullRandomItem().PlayerId);

            if (mode.HasFlag(GameMode.LightsOut))
                FacilityManager.SetAllRoomLightStates(false);

            if (mode.HasFlag(GameMode.Spooky))
                spooky_update = Timing.RunCoroutine(_SpookyUpdate());

            late_spawn = false;
            Timing.CallDelayed(7.0f, () => late_spawn = true);
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted || new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Overwatch || new_role == RoleTypeId.Filmmaker)
                return true;

            if (found_winner)
                return HandleGameOverRoleChange(player, new_role);

            if (late_spawn && new_role == RoleTypeId.Scp173)
            {
                peanuts.Add(player.PlayerId);
                return true;
            }

            if (peanuts.Contains(player.PlayerId))
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

            if (peanuts.Contains(player.PlayerId) && role == RoleTypeId.Scp173)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role != RoleTypeId.Scp173)
                        return;

                    if (mode.HasFlag(GameMode.Baby173))
                        SetScale(player, config.Baby173Size);
                });
                if (!(late_spawn && mode.HasFlag(GameMode.Replication)))
                {
                    if (!late_spawn)
                        player.SendBroadcast("you will teleport in 7 seconds", 7, shouldClearPrevious: true);
                    Timing.CallDelayed(late_spawn ? 0.0f : 7.0f, () =>
                    {
                        if (player.Role != RoleTypeId.Scp173)
                            return;

                        if (custom_offset.ContainsKey(room_name))
                            Teleport.RoomPos(player, room, custom_offset[room_name]);
                        else
                            Teleport.Room(player, room);
                    });
                }
                else
                {
                    Timing.CallDelayed(1.0f, () =>
                    {
                        if (player.Role != RoleTypeId.Scp173)
                            return;

                        if(player.Room == null || player.Room != room)
                        {
                            if (custom_offset.ContainsKey(room_name))
                                Teleport.RoomPos(player, room, custom_offset[room_name]);
                            else
                                Teleport.Room(player, room);
                        }
                    });
                }
            }
            else
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role != RoleTypeId.ClassD)
                        return;

                    if (custom_offset.ContainsKey(room_name))
                        Teleport.RoomPos(player, room, custom_offset[room_name]);
                    else
                        Teleport.Room(player, room);

                    if (mode.HasFlag(GameMode.Child))
                        SetScale(player, config.ChildSize);
                    if (mode.HasFlag(GameMode.Invis))
                        player.AddItem(ItemType.SCP268);
                    if ((mode.HasFlag(GameMode.LightsOut)))
                        player.AddItem(ItemType.Flashlight);
                    if (mode.HasFlag(GameMode.Speedy))
                    {
                        Scp330Bag bag = player.AddItem(ItemType.SCP330) as Scp330Bag;
                        bag.TryAddSpecific(CandyKindID.Yellow);
                        bag.TryRemove(0);
                        for (int i = 0; i < 5; i++)
                            bag.TryAddSpecific(CandyKindID.Yellow);
                        bag.ServerRefreshBag();
                        for (int i = 0; i < 6; i++)
                            player.AddItem(ItemType.SCP207);
                    }
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerInteractDoor)]
        bool OnPlayerInteractDoor(Player player, DoorVariant door, bool can_open)
        {
            if (door.ActiveLocks > 0 && !player.IsBypassEnabled)
                return true;

            if (door.AllowInteracting(player.ReferenceHub, 0))
            {
                door.NetworkTargetState = !door.TargetState;
                door._triggerPlayer = player.ReferenceHub;
                switch (door.NetworkTargetState)
                {
                    case false:
                        DoorEvents.TriggerAction(door, DoorAction.Closed, player.ReferenceHub);
                        break;
                    case true:
                        DoorEvents.TriggerAction(door, DoorAction.Opened, player.ReferenceHub);
                        break;
                }
            }
            return false;
        }

        [PluginEvent(ServerEventType.PlayerDying)]
        void OnPlayerDying(Player victim, Player attacker, DamageHandlerBase damageHandler)
        {
            if (victim == null || !Round.IsRoundStarted || found_winner)
                return;

            Vector3 pos = victim.Position;
            if (mode.HasFlag(GameMode.Replication) && attacker != null && attacker.Role == RoleTypeId.Scp173)
            {
                peanuts.Add(victim.PlayerId);
                Timing.CallDelayed(0.0f, () =>
                {
                    if (victim.Role != RoleTypeId.Spectator)
                        return;
                    victim.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Scp173, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);
                    victim.Position = pos;
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

        private IEnumerator<float> _SpookyUpdate()
        {
            while(true)
            {
                try
                {
                    foreach(var p in ReadyPlayers())
                    {
                        if (p.Role != RoleTypeId.ClassD || p.IsInventoryFull)
                            continue;
                        if (Random.value < (config.SpookyModeChancePerMinute / 60.0f))
                            p.AddItem(ItemType.SCP2176);
                    }
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }
    }

    public class PeanutDodgeBallEvent:IEvent
    {
        public static PeanutDodgeBallEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Peanut Dodgeball";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "PD";
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
            EventHandler.Start(EventConfig);
            harmony = new Harmony("PeanutDodgeBallEvent");
            harmony.PatchAll();
            Log.Info(EventName + " event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            EventHandler.Stop();
            harmony.UnpatchAll("PeanutDodgeBallEvent");
            harmony = null;
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
