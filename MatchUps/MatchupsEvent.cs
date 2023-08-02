using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using CustomPlayerEffects;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables.Scp330;
using MapGeneration;
using MEC;
using Mirror;
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
using static TheRiptide.Utility;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        [Description("how many player to spawn for each item type and scp type, order: 049, 096, 106, 173, 939")]
        public Dictionary<ItemType, List<float>> MatchupWeights { get; set; } = new Dictionary<ItemType, List<float>>
        {
            { ItemType.MicroHID,            new  List<float>{ 3.0f, 1.5f, 4.0f, 4.0f, 5.0f } },
            { ItemType.Jailbird,            new  List<float>{ 4.0f, 6.0f, 6.0f, 8.0f, 8.0f } },
            { ItemType.GunCom45,            new  List<float>{ 3.0f, 5.0f, 5.0f, 7.0f, 4.0f } },
            { ItemType.ParticleDisruptor,   new  List<float>{ 10.0f, 15.0f, 12.0f, 15.0f, 12.0f } },
            { ItemType.GunLogicer,          new  List<float>{ 6.0f, 8.0f, 8.0f, 8.0f, 8.0f } },
            { ItemType.GunCOM15,            new  List<float>{ 15.0f, 17.0f, 20.0f, 17.0f, 20.0f } },
            { ItemType.GunShotgun,          new  List<float>{ 7.0f, 9.0f, 9.0f, 9.0f, 9.0f } },
            { ItemType.GunCrossvec,         new  List<float>{ 7.0f, 9.0f, 9.0f, 9.0f, 9.0f } },
            { ItemType.GrenadeHE,           new  List<float>{ 10.0f, 15.0f, 20.0f, 15.0f, 15.0f } },
            { ItemType.SCP018,              new  List<float>{ 5.0f, 6.0f, 6.0f, 6.0f, 6.0f } },
            { ItemType.SCP330,              new  List<float>{ 10.0f, 15.0f, 12.0f, 15.0f, 12.0f } },
        };

        //[Description("surface weight")]
        //public Dictionary<ItemType, float> SurfaceWeights { get; set; } = new Dictionary<ItemType, float>
        //{
        //    { ItemType.MicroHID, 2.0f },
        //    { ItemType.Jailbird, 2.0f },
        //    { ItemType.GunCom45, 2.0f },
        //    { ItemType.ParticleDisruptor, 2.0f },
        //    { ItemType.GunLogicer, 1.0f },
        //    { ItemType.GunCOM15, 0.5f },
        //    { ItemType.GunShotgun, 1.5f },
        //    { ItemType.GunCrossvec, 1.5f },
        //    { ItemType.GrenadeHE, 1.0f },
        //    { ItemType.SCP018, 0.2f },
        //};

        public string Description { get; set; } = "[recommended player count 10+] Fight between SCPs and NTF with a random weapon in a random zone. Warning! may be very unbalanced!\n\n";
    }

    public class EventHandler
    {
        private static FacilityZone zone;
        private static ItemType weapon;
        private static RoleTypeId scp;

        Dictionary<RoleTypeId, int> scp_index = new Dictionary<RoleTypeId, int>
        {
            { RoleTypeId.Scp049, 0 },
            { RoleTypeId.Scp096, 1 },
            { RoleTypeId.Scp106, 2 },
            { RoleTypeId.Scp173, 3 },
            { RoleTypeId.Scp939, 4 }
        };

        private static Config config;

        private static Vector3 team_a_surface_offset = new Vector3(131.581f, -11.208f, 27.433f);
        private static Vector3 team_b_surface_offset = new Vector3(-9.543f, 0.960f, 0.410f);
        private static Vector3 entrance_offset = new Vector3(-5.434f, 0.965f, -0.043f);
        private static Vector3 heavy_offset = new Vector3(4.231f, 0.959f, -0.016f);
        private static Vector3 light_offset = new Vector3(15.152f, 0.960f, -0.011f);

        private static HashSet<int> team_a = new HashSet<int>();
        private static HashSet<int> team_b = new HashSet<int>();

        private static RoomIdentifier team_a_room;
        private static RoomIdentifier team_b_room;

        private static Vector3 team_a_offset;
        private static Vector3 team_b_offset;

        private static bool old_ff;

        public static void Start(Config config)
        {
            old_ff = Server.FriendlyFire;
            Server.FriendlyFire = false;
            EventHandler.config = config;
            team_a.Clear();
            team_b.Clear();
            zone = new List<FacilityZone>
            {
                FacilityZone.Surface,
                FacilityZone.Surface,
                FacilityZone.Entrance,
                FacilityZone.Entrance,
                FacilityZone.HeavyContainment,
                FacilityZone.LightContainment,
                FacilityZone.LightContainment
            }.RandomItem();
            weapon = config.MatchupWeights.Keys.ToList().RandomItem();
            scp = new List<RoleTypeId>
            {
                RoleTypeId.Scp049,
                RoleTypeId.Scp096,
                RoleTypeId.Scp106,
                RoleTypeId.Scp173,
                RoleTypeId.Scp939
            }.RandomItem();
        }

        public static void Stop()
        {
            Server.FriendlyFire = old_ff;
            team_a.Clear();
            team_b.Clear();
            team_a_room = null;
            team_b_room = null;
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: Matchups\n<size=32>Fight between SCPs and NTF with a random weapon in a random zone\n<color=#FF9000>Zone: " + zone.ToString().Replace("Containment", "") + ", Weapon: " + weapon.ToString().Replace("Gun", "") + ", SCP: " + scp.ToString().Replace("Scp", "") + ", SCP to Human Ratio: 1.0:" + (config.MatchupWeights[weapon][scp_index[scp]]).ToString("0.0") + "</color></size>", 30, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            RoomIdentifier surface = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone == FacilityZone.Surface).First();
            HashSet<RoomIdentifier> checkpoint_a = RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name == RoomName.HczCheckpointA || r.Name == RoomName.LczCheckpointA).ToHashSet();
            HashSet<RoomIdentifier> checkpoint_b = RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name == RoomName.HczCheckpointB || r.Name == RoomName.LczCheckpointB).ToHashSet();

            var mid_spawns = RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name == RoomName.HczCheckpointToEntranceZone);
            RoomIdentifier team_a_ez = mid_spawns.Where(r => r.Zone == FacilityZone.Entrance).First();
            RoomIdentifier team_b_ez = mid_spawns.Where(r => r.Zone == FacilityZone.Entrance).Last();
            RoomIdentifier team_a_heavy = FacilityManager.GetAdjacent(team_a_ez).Keys.Where(r => r.Zone == FacilityZone.HeavyContainment).First();
            RoomIdentifier team_b_heavy = FacilityManager.GetAdjacent(team_b_ez).Keys.Where(r => r.Zone == FacilityZone.HeavyContainment).First();

            FacilityManager.LockRoom(surface, DoorLockReason.AdminCommand);
            FacilityManager.LockJoinedRooms(new HashSet<RoomIdentifier> { team_a_ez, team_a_heavy }, DoorLockReason.AdminCommand);
            FacilityManager.LockJoinedRooms(new HashSet<RoomIdentifier> { team_b_ez, team_b_heavy }, DoorLockReason.AdminCommand);
            FacilityManager.LockJoinedRooms(checkpoint_a, DoorLockReason.AdminCommand);
            FacilityManager.LockJoinedRooms(checkpoint_b, DoorLockReason.AdminCommand);

            foreach (var door in ElevatorDoor.AllElevatorDoors[ElevatorManager.ElevatorGroup.Nuke])
                FacilityManager.LockDoor(door, DoorLockReason.AdminCommand);

            foreach (var door in ElevatorDoor.AllElevatorDoors[ElevatorManager.ElevatorGroup.Scp049])
                FacilityManager.LockDoor(door, DoorLockReason.AdminCommand);

            switch (zone)
            {
                case FacilityZone.Surface:
                    team_a_room = surface;
                    team_b_room = surface;
                    team_a_offset = team_a_surface_offset;
                    team_b_offset = team_b_surface_offset;
                    break;
                case FacilityZone.Entrance:
                    team_a_room = team_a_ez;
                    team_b_room = team_b_ez;
                    team_a_offset = entrance_offset;
                    team_b_offset = entrance_offset;
                    break;
                case FacilityZone.HeavyContainment:
                    team_a_room = team_a_heavy;
                    team_b_room = team_b_heavy;
                    team_a_offset = heavy_offset;
                    team_b_offset = heavy_offset;
                    break;
                case FacilityZone.LightContainment:
                    team_a_room = checkpoint_a.Where(r => r.Zone == FacilityZone.LightContainment).First();
                    team_b_room = checkpoint_b.Where(r => r.Zone == FacilityZone.LightContainment).First();
                    team_a_offset = light_offset;
                    team_b_offset = light_offset;
                    break;
            }

            foreach (var p in Player.GetPlayers())
                team_a.Add(p.PlayerId);

            int team_b_count = Mathf.RoundToInt(team_a.Count / (1.0f + (config.MatchupWeights[weapon][scp_index[scp]])));
            if (team_b_count == 0)
                team_b_count = 1;
            for (int i = 0; i < team_b_count; i++)
            {
                int id = team_a.ElementAt(Random.Range(0, team_a.Count));
                team_a.Remove(id);
                team_b.Add(id);
            }

            Timing.CallDelayed(3.0f, () =>
            {
                ItemPickupBase[] items = Object.FindObjectsOfType<ItemPickupBase>();
                foreach (var item in items)
                    NetworkServer.Destroy(item.gameObject);

                Cassie.Message("3 . 2 . 1");
            });
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted || new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Overwatch || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Scp0492)
                return true;

            if (team_a.Contains(player.PlayerId))
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
            else if (team_b.Contains(player.PlayerId))
            {
                if (new_role != scp)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(scp);
                    });
                    return false;
                }
            }
            else
            {
                if (new_role != RoleTypeId.Spectator && new_role != RoleTypeId.Overwatch && new_role != RoleTypeId.Tutorial)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.Spectator);
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

            if (team_a.Contains(player.PlayerId))
            {
                if (role == RoleTypeId.NtfSpecialist)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        SetLoadout(player);
                        Teleport.RoomPos(player, team_a_room, team_a_offset);
                        player.EffectsManager.EnableEffect<Ensnared>(10);
                    });
                    Timing.CallDelayed(7.0f, () =>
                    {
                        player.EffectsManager.EnableEffect<Scanned>(10);
                    });
                }
            }
            else if (team_b.Contains(player.PlayerId))
            {
                if (role == scp)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        Teleport.RoomPos(player, team_b_room, team_b_offset);
                        player.EffectsManager.EnableEffect<Ensnared>(10);
                    });
                    Timing.CallDelayed(7.0f, () =>
                    {
                        player.EffectsManager.EnableEffect<Scanned>(10);
                    });
                }
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

        private void SetLoadout(Player player)
        {
            player.ClearInventory();
            player.AddItem(ItemType.KeycardZoneManager);
            player.AddItem(ItemType.ArmorHeavy);
            if (IsGun(weapon))
            {
                if (weapon != ItemType.ParticleDisruptor)
                    AddFirearm(player, weapon, true);
                else
                {
                    for (int i = 0; i < 6; i++)
                    {
                        ParticleDisruptor pd = player.AddItem(weapon) as ParticleDisruptor;
                        pd.Status = new FirearmStatus(5, pd.Status.Flags, pd.Status.Attachments);
                    }
                }
            }
            else if (weapon == ItemType.GrenadeHE || weapon == ItemType.SCP018)
            {
                for(int i = 0; i < 6; i++)
                    player.AddItem(weapon);
            }
            else if(weapon == ItemType.SCP330)
            {
                player.AddItem(weapon);
                Scp330Bag bag;
                if (Scp330Bag.TryGetBag(player.ReferenceHub, out bag))
                    bag.Candies[bag.Candies.Count - 1] = CandyKindID.Pink;
            }
            else
                player.AddItem(weapon);
            player.EffectsManager.EnableEffect<Ensnared>(10);
        }
    }

    public class MatchupsEvent:IEvent
    {
        public static MatchupsEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Matchups";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "MU";
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
            EventHandler.Start(EventConfig);
            Log.Info(EventName + " event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            EventHandler.Stop();
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Matchups Event", "1.0.0", "", "The Riptide")]
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
