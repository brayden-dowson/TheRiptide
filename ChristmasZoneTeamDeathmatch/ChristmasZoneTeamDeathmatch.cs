﻿using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using CustomPlayerEffects;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Pickups;
using MapGeneration;
using MEC;
using Mirror;
using NWAPIPermissionSystem;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
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
        public string Description { get; set; } = "Scientists vs Class-D in a random zone with a random loadout, last team alive wins!\n\n";
    }

    //1 970 -44
    public class EventHandler
    {
        private static FacilityZone zone;
        private static ItemType weapon;
        private static ItemType armor;
        private static ItemType scp;
        private static ItemType other;

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

        private static CoroutineHandle replenish;

        private static bool old_ff;
        private static bool swap_team_spawn = false;

        public static void Start()
        {
            swap_team_spawn = Random.value < 0.5;
            old_ff = Server.FriendlyFire;
            Server.FriendlyFire = false;
            team_a.Clear();
            team_b.Clear();
            zone = new List<FacilityZone>
            {
                FacilityZone.Surface,
                FacilityZone.Entrance,
                FacilityZone.HeavyContainment,
                FacilityZone.LightContainment
            }.RandomItem();
            weapon = new List<ItemType>
            {
                ItemType.GunCOM15,
                ItemType.GunCom45,
                ItemType.GunFSP9,
                ItemType.GunCrossvec,
                ItemType.GunE11SR,
                ItemType.GunAK,
                ItemType.GunShotgun,
                ItemType.GunRevolver,
                ItemType.GunLogicer,
                ItemType.Jailbird,
                ItemType.MicroHID
            }.RandomItem();
            armor = new List<ItemType>
            {
                ItemType.None,
                ItemType.ArmorLight,
                ItemType.ArmorCombat,
                ItemType.ArmorCombat,
                ItemType.ArmorHeavy
            }.RandomItem();
            scp = new List<ItemType>
            {
                ItemType.SCP018,
                ItemType.SCP1576,
                ItemType.SCP1853,
                ItemType.SCP207,
                ItemType.SCP2176,
                ItemType.SCP244a,
                ItemType.SCP268,
                ItemType.SCP330,
                ItemType.SCP500,
                ItemType.AntiSCP207
            }.RandomItem();
            other = new List<ItemType>
            {
                ItemType.None,
                ItemType.Medkit,
                ItemType.Medkit,
                ItemType.Painkillers,
                ItemType.Painkillers,
                ItemType.Adrenaline,
                ItemType.Adrenaline,
                ItemType.Flashlight,
                ItemType.GrenadeFlash
            }.RandomItem();
        }

        public static void Stop()
        {
            Server.FriendlyFire = old_ff;
            team_a.Clear();
            team_b.Clear();
            Timing.KillCoroutines(replenish);
            team_a_room = null;
            team_b_room = null;
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            string info = "<color=#FF9000>Zone: " + zone.ToString().Replace("Containment", "") + ", Weapon: " + weapon.ToString().Replace("Gun", "") + ", Armor: " + armor.ToString().Replace("Armor", "") + ", SCP: " + scp.ToString().Replace("SCP", "") + ", Other: " + other.ToString() + "</color>";
            ChristmasZoneTeamManager.Singleton.AssignTeam(player, team_a, team_b, info);
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            team_a.Remove(player.PlayerId);
            team_b.Remove(player.PlayerId);
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

            Timing.CallDelayed(3.0f, () =>
            {
                ItemPickupBase[] items = Object.FindObjectsOfType<ItemPickupBase>();
                foreach (var item in items)
                    NetworkServer.Destroy(item.gameObject);

                if (other == ItemType.Flashlight)
                    FacilityManager.SetAllRoomLightStates(false);
                Cassie.Message("3 . 2 . 1");
            });

            replenish = Timing.RunCoroutine(_Replenish());

            ChristmasZoneTeamManager.Singleton.BroadcastTeamCount(team_a, team_b);
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted || new_role == RoleTypeId.Filmmaker || new_role == RoleTypeId.Overwatch || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Spectator)
                return true;

            if (team_a.Contains(player.PlayerId))
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
            else if (team_b.Contains(player.PlayerId))
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
            else
            {
                if (new_role != RoleTypeId.Spectator)
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
                if (role == RoleTypeId.ClassD)
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
                if (role == RoleTypeId.Scientist)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        SetLoadout(player);
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

        [PluginEvent(ServerEventType.RoundEndConditionsCheck)]
        void OnRoundEndConditionsCheck(bool baseGameConditionsSatisfied)
        {
            if (!baseGameConditionsSatisfied)
                return;

            bool? cz_won = null;
            foreach (var p in ReadyPlayers())
            {
                if (p.Role == RoleTypeId.ClassD || p.Role == RoleTypeId.Scientist)
                {
                    string user_group = ServerStatic.GetPermissionsHandler()._groups.FirstOrDefault(g => g.Value.EqualsTo(p.ReferenceHub.serverRoles.Group)).Key;
                    if(user_group.Contains("cz"))
                    {
                        cz_won = true;
                        break;
                    }
                    else if(user_group.Contains("cc"))
                    {
                        cz_won = false;
                        break;
                    }
                }
            }

            if (cz_won == null)
                return;

            foreach (var p in ReadyPlayers())
            {
                if (cz_won.Value)
                    p.SendBroadcast("<size=64><b><color=#008a70>Containment Zone</color> <color=#00bbff>Won!</color></b>", 30, shouldClearPrevious: true);
                else
                    p.SendBroadcast("<size=64><b><color=#96e9ff>Christmas Chaos</color> <color=#00bbff>Won!</color></b>", 30, shouldClearPrevious: true);
            }
        }

        private void SetLoadout(Player player)
        {
            player.ClearInventory();
            player.AddItem(ItemType.KeycardZoneManager);
            player.AddItem(armor);
            if (IsGun(weapon))
            {
                Firearm firearm = AddFirearm(player, weapon, true);
                if (other == ItemType.Flashlight)
                    AddAttachment(firearm, AttachmentName.Flashlight);
            }
            else
                player.AddItem(weapon);
            player.AddItem(other);
            player.AddItem(scp);
            player.EffectsManager.EnableEffect<Ensnared>(10);
        }

        private IEnumerator<float> _Replenish()
        {
            int skip = 0;
            while (true)
            {
                try
                {
                    foreach (var player in Player.GetPlayers())
                    {
                        if (player.IsAlive)
                        {
                            if (IsGun(weapon))
                            {
                                ItemType ammo = GunAmmoType(weapon);
                                if (ammo != ItemType.None)
                                    player.SetAmmo(ammo, GetStandardAmmoLimit(player.ReferenceHub.inventory.TryGetBodyArmor(out BodyArmor armor) ? armor : null, ammo));
                            }
                            else
                            {
                                int count = 0;
                                foreach (var item in player.ReferenceHub.inventory.UserInventory.Items.Values)
                                {
                                    if (item != null && item.ItemTypeId == weapon)
                                        count++;
                                }
                                if ((weapon == ItemType.GrenadeHE || skip == 0) && count < 3)
                                    player.AddItem(weapon);
                            }
                        }
                    }
                    skip++;
                    if (skip == 3)
                        skip = 0;
                }
                catch (System.Exception ex)
                {
                    Log.Error("_Replenish error: " + ex.ToString());
                }
                yield return Timing.WaitForSeconds(10.0f);
            }
        }
    }

    public class TeamDeathmatchEvent : IEvent
    {
        public static TeamDeathmatchEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Christmas Zone Team Deathmatch";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "CZTD";
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

        [PluginEntryPoint("Christmas Zone Team Deathmatch", "1.0.0", "scientists vs class-d in a random zone with a random weapon", "The Riptide")]
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
