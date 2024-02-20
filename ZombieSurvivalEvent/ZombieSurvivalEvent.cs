using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using CustomPlayerEffects;
using HarmonyLib;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items;
using InventorySystem.Items.Armor;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Usables.Scp330;
using MapGeneration;
using MEC;
using Mirror.LiteNetLib4Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using Respawning;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheRiptide.EventUtility;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        [Description("scales with the ratio of humans to zombies. this is the value for a 1:1 ratio")]
        public int ZombieHealth { get; set; } = 400;
        public float KnockBackMultiple { get; set; } = 2.00f;
        public int SpeedRampingDelay { get; set; } = 2;
        public float SpeedRampingRate { get; set; } = 10.0f;
        public int HealthRampingDelay { get; set; } = 4;
        public float HealthRampingRate { get; set; } = 50.0f;

        public string Description { get; set; } = "Light is locked down and all players spawn in light as Class-D with one zombie. Zombies infect players on kill. Guns deal knock back based on the ratio of zombies to humans alive. Less humans more zombies = more knockback. The more zombies there are the less health each one has. After a certain amount of time zombies speed will start to ramp(increase) and after more time their health aswell. Last one alive wins!\n\n";
    }

    public class EventHandler
    {
        public static Config config;

        private static FacilityZone zone;

        private static bool found_winner = false;
        public static HashSet<int> zombies = new HashSet<int>();
        public static int humans_alive = 1;
        public static int zombies_alive = 1;
        private static CoroutineHandle update;
        private static CoroutineHandle speed_handle;
        private static CoroutineHandle health_handle;
        private static CoroutineHandle knock_back_handle;
        private static Stopwatch stopwatch = new Stopwatch();

        public static int health_ramping = 0;
        public static byte speed_ramping = 0;

        private static Dictionary<int, Vector3> knock_back = new Dictionary<int, Vector3>();

        public static void Start(Config config)
        {
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

            EventHandler.config = config;
            found_winner = false;
            WinnerReset();
            zombies.Clear();
            Timing.KillCoroutines(update);
            Timing.KillCoroutines(speed_handle);
            Timing.KillCoroutines(health_handle);
            health_ramping = 0;
            speed_ramping = 0;
        }

        public static void Stop()
        {
            found_winner = false;
            WinnerReset();
            zombies.Clear();
            Timing.KillCoroutines(update);
            Timing.KillCoroutines(speed_handle);
            Timing.KillCoroutines(health_handle);
            Timing.KillCoroutines(knock_back_handle);
            health_ramping = 0;
            speed_ramping = 0;
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + ZombieSurvivalEvent.Singleton.EventName + "\n<size=32>" + ZombieSurvivalEvent.Singleton.EventDescription + "</size>", 30, shouldClearPrevious: true);

            if(Round.IsRoundStarted)
            {
                Timing.CallDelayed(1.0f,()=>
                {
                    zombies.Add(player.PlayerId);
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            if (player == null || !Round.IsRoundStarted)
                return;

            if (zombies.Contains(player.PlayerId))
                zombies.Remove(player.PlayerId);

            if(zombies.Count == 0)
            {
                HashSet<int> spectators = new HashSet<int>();
                foreach (var p in Player.GetPlayers())
                    if (p.Role == RoleTypeId.Spectator)
                        spectators.Add(p.PlayerId);

                if (!spectators.IsEmpty())
                    zombies.Add(spectators.ElementAt(Random.Range(0, spectators.Count)));
                else
                    zombies.Add(Player.GetPlayers().RandomItem().PlayerId);
                Player.Get(zombies.First()).SetRole(RoleTypeId.Scp0492);
            }
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            if (Player.Count < 30)
            {
                RoomIdentifier surface = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone == FacilityZone.Surface).First();
                HashSet<RoomIdentifier> checkpoint_a = RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name == RoomName.HczCheckpointA || r.Name == RoomName.LczCheckpointA).ToHashSet();
                HashSet<RoomIdentifier> checkpoint_b = RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name == RoomName.HczCheckpointB || r.Name == RoomName.LczCheckpointB).ToHashSet();

                var mid_spawns = RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name == RoomName.HczCheckpointToEntranceZone);
                RoomIdentifier ez_a = mid_spawns.Where(r => r.Zone == FacilityZone.Entrance).First();
                RoomIdentifier ez_b = mid_spawns.Where(r => r.Zone == FacilityZone.Entrance).Last();
                RoomIdentifier heavy_a = FacilityManager.GetAdjacent(ez_a).Keys.Where(r => r.Zone == FacilityZone.HeavyContainment).First();
                RoomIdentifier heavy_b = FacilityManager.GetAdjacent(ez_b).Keys.Where(r => r.Zone == FacilityZone.HeavyContainment).First();

                FacilityManager.LockRoom(surface, DoorLockReason.AdminCommand);
                FacilityManager.LockJoinedRooms(new HashSet<RoomIdentifier> { ez_a, heavy_a }, DoorLockReason.AdminCommand);
                FacilityManager.LockJoinedRooms(new HashSet<RoomIdentifier> { ez_b, heavy_b }, DoorLockReason.AdminCommand);
                FacilityManager.LockJoinedRooms(checkpoint_a, DoorLockReason.AdminCommand);
                FacilityManager.LockJoinedRooms(checkpoint_b, DoorLockReason.AdminCommand);

                foreach (var door in ElevatorDoor.AllElevatorDoors[ElevatorManager.ElevatorGroup.Nuke])
                    FacilityManager.LockDoor(door, DoorLockReason.AdminCommand);

                foreach (var door in ElevatorDoor.AllElevatorDoors[ElevatorManager.ElevatorGroup.Scp049])
                    FacilityManager.LockDoor(door, DoorLockReason.AdminCommand);

                FacilityManager.LockRooms(RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Name == RoomName.HczCheckpointA || r.Name == RoomName.HczCheckpointB).ToHashSet(), DoorLockReason.AdminCommand);
            }
            else
                zone = FacilityZone.None;

            stopwatch.Restart();
            Round.IsLocked = true;
            EndRoom = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone == FacilityZone.Surface).First();
            RoomOffset = new Vector3(40.000f, 14.080f, -32.600f);

            zombies.Add(Player.GetPlayers().RandomItem().PlayerId);

            update = Timing.RunCoroutine(_Update());

            speed_handle = Timing.CallDelayed(config.SpeedRampingDelay * 60.0f, () =>
            {
                foreach (var player in Player.GetPlayers())
                {
                    player.SendBroadcast("Zombie speed is mutating!", 15, shouldClearPrevious: true);
                    if (player.Role == RoleTypeId.ClassD)
                        player.AddItem(ItemType.Adrenaline);
                }
            });

            health_handle = Timing.CallDelayed(config.HealthRampingDelay * 60.0f, () =>
            {
                foreach (var player in Player.GetPlayers())
                {
                    player.SendBroadcast("Zombie health is mutating!", 15, shouldClearPrevious: true);
                    if (player.Role == RoleTypeId.ClassD)
                        player.AddItem(ItemType.SCP500);
                }
            });

            knock_back_handle = Timing.RunCoroutine(_KnockBack());
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        bool OnRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted || new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Filmmaker || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Overwatch)
                return true;

            if(found_winner)
                return HandleGameOverRoleChange(player, new_role);

            if(zombies.Contains(player.PlayerId))
            {
                if(new_role != RoleTypeId.Scp0492)
                {
                    Timing.CallDelayed(0.0f,()=>
                    {
                        player.SetRole(RoleTypeId.Scp0492);
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

            if(found_winner)
            {
                HandleGameOverSpawn(player);
                return;
            }

            if(role == RoleTypeId.Scp0492 && zombies.Contains(player.PlayerId))
            {
                float minutes = stopwatch.ElapsedMilliseconds / (1000.0f * 60.0f);
                speed_ramping = (byte)Mathf.FloorToInt(Mathf.Clamp((minutes - config.SpeedRampingDelay) * config.SpeedRampingRate, 0.0f, 255.0f));
                health_ramping = Mathf.FloorToInt(Mathf.Max((minutes - config.HealthRampingDelay) * config.HealthRampingRate, 0.0f));

                Timing.CallDelayed(0.1f,()=>
                {
                    RoomIdentifier surface = RoomIdentifier.AllRoomIdentifiers.First(r => r.Zone == FacilityZone.Surface);
                    if (zone != FacilityZone.Surface)
                    {
                        RoomIdentifier target_room;
                        HashSet<RoomIdentifier> occupied = new HashSet<RoomIdentifier>();
                        HashSet<RoomIdentifier> adjacent = new HashSet<RoomIdentifier>();
                        foreach (var p in Player.GetPlayers())
                        {
                            if (p.Room != null && !zombies.Contains(p.PlayerId))
                                occupied.Add(p.Room);
                        }
                        foreach (var room in occupied)
                            foreach (var adj in FacilityManager.GetAdjacent(room).Keys)
                                if (zone == FacilityZone.None || adj.Zone == zone)
                                    adjacent.Add(adj);

                        adjacent.ExceptWith(occupied);
                        if (occupied.Contains(surface))
                            adjacent.Add(surface);

                        var valid_zone = RoomIdentifier.AllRoomIdentifiers.Where(r => zone == FacilityZone.None || r.Zone == zone);
                        if (!adjacent.IsEmpty())
                            target_room = adjacent.ElementAt(Random.Range(0, adjacent.Count));
                        else
                            target_room = valid_zone.ElementAt(Random.Range(0, valid_zone.Count()));
                        Teleport.RoomRandom(player, target_room);
                    }
                    else
                    {
                        Teleport.RoomRandom(player, surface);
                    }

                    player.EffectsManager.ChangeState<MovementBoost>(speed_ramping);
                    player.Health = (health_ramping + config.ZombieHealth) * ((float)humans_alive / zombies_alive);
                });
            }
            else
            {
                Timing.CallDelayed(0.1f,()=>
                {
                    if (player.Role == RoleTypeId.ClassD && !zombies.Contains(player.PlayerId))
                    {
                        if (zone != FacilityZone.None)
                            Teleport.RoomRandom(player, RoomIdentifier.AllRoomIdentifiers.Where(r => r.Zone == zone).ToList().RandomItem());
                        else
                            Teleport.RoomRandom(player, RoomIdentifier.AllRoomIdentifiers.ToList().RandomItem());

                        player.ClearInventory();
                        player.AddItem(ItemType.ArmorHeavy);
                        switch(Random.Range(0,11))
                        {
                            case 0:
                                AddFirearm(player, ItemType.GunLogicer, true);
                                AddFirearm(player, ItemType.GunLogicer, true);
                                player.SendBroadcast("Role: Heavy", 15, shouldClearPrevious: true);
                                break;
                            case 1:
                                AddFirearm(player, ItemType.GunE11SR, true);
                                player.AddItem(ItemType.Painkillers);
                                player.AddItem(ItemType.GrenadeHE);
                                player.AddItem(ItemType.GrenadeHE);
                                player.AddItem(ItemType.GrenadeHE);
                                player.AddItem(ItemType.GrenadeFlash);
                                player.SendBroadcast("Role: Demo", 15, shouldClearPrevious: true);
                                break;
                            case 2:
                                AddFirearm(player, ItemType.GunCrossvec, true);
                                player.AddItem(ItemType.Medkit);
                                player.AddItem(ItemType.Medkit);
                                player.AddItem(ItemType.Painkillers);
                                player.AddItem(ItemType.Painkillers);
                                player.AddItem(ItemType.SCP330);
                                player.AddItem(ItemType.SCP330);
                                player.AddItem(ItemType.SCP330);
                                player.AddItem(ItemType.SCP330);
                                player.AddItem(ItemType.SCP330);
                                player.AddItem(ItemType.SCP330);
                                player.AddItem(ItemType.SCP500);
                                player.SendBroadcast("Role: Medic", 15, shouldClearPrevious: true);
                                break;
                            case 3:
                                AddFirearm(player, ItemType.GunFSP9, true);
                                player.AddItem(ItemType.Painkillers);
                                player.AddItem(ItemType.Painkillers);
                                player.AddItem(ItemType.SCP018);
                                player.AddItem(ItemType.SCP018);
                                player.AddItem(ItemType.SCP018);
                                player.SendBroadcast("Role: Ball", 15, shouldClearPrevious: true);
                                break;
                            case 4:
                                AddFirearm(player, ItemType.GunRevolver, true);
                                AddFirearm(player, ItemType.GunShotgun, true);
                                player.AddItem(ItemType.Jailbird);
                                player.AddItem(ItemType.SCP330);
                                Scp330Bag bag;
                                if (Scp330Bag.TryGetBag(player.ReferenceHub, out bag))
                                    bag.Candies[bag.Candies.Count - 1] = CandyKindID.Pink;
                                player.SendBroadcast("Role: Kamikaze", 15, shouldClearPrevious: true);
                                break;
                            case 5:
                                AddFirearm(player, ItemType.GunCom45, true);
                                player.AddItem(ItemType.MicroHID);
                                player.AddItem(ItemType.MicroHID);
                                player.AddItem(ItemType.SCP1853);
                                player.AddItem(ItemType.SCP1853);
                                player.AddItem(ItemType.SCP1853);
                                player.AddItem(ItemType.SCP1853);
                                player.SendBroadcast("Role: Specialist", 15, shouldClearPrevious: true);
                                break;
                            case 6:
                                AddFirearm(player, ItemType.GunCrossvec, true);
                                ParticleDisruptor pd = player.AddItem(ItemType.ParticleDisruptor) as ParticleDisruptor;
                                pd.Status = new FirearmStatus(15, pd.Status.Flags, pd.Status.Attachments);
                                player.SendBroadcast("Role: Atomizer", 15, shouldClearPrevious: true);
                                break;
                            case 7:
                                AddFirearm(player, ItemType.GunCrossvec, true);
                                player.AddItem(ItemType.SCP207);
                                player.AddItem(ItemType.SCP207);
                                player.AddItem(ItemType.SCP207);
                                player.AddItem(ItemType.Painkillers);
                                player.AddItem(ItemType.Painkillers);
                                player.AddItem(ItemType.Painkillers);
                                player.SendBroadcast("Role: Runner", 15, shouldClearPrevious: true);
                                break;
                            case 8:
                                AddFirearm(player, ItemType.GunCrossvec, true);
                                player.AddItem(ItemType.SCP244a);
                                player.AddItem(ItemType.SCP244b);
                                player.AddItem(ItemType.Painkillers);
                                player.SendBroadcast("Role: Freezer", 15, shouldClearPrevious: true);
                                break;
                            case 9:
                                AddFirearm(player, ItemType.GunAK, true);
                                player.AddItem(ItemType.Painkillers);
                                player.AddItem(ItemType.SCP268);
                                player.SendBroadcast("Role: Spy", 15, shouldClearPrevious: true);
                                break;
                            case 10:
                                AddFirearm(player, ItemType.GunCrossvec, true);
                                player.AddItem(ItemType.SCP2176);
                                player.AddItem(ItemType.SCP2176);
                                player.AddItem(ItemType.SCP2176);
                                player.AddItem(ItemType.SCP2176);
                                player.SendBroadcast("Role: Delayer", 15, shouldClearPrevious: true);
                                break;
                        }
                    }
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerDamage)]
        void OnPlayerDamage(Player attacker, Player victim, DamageHandlerBase damage)
        {
            if (victim == null || attacker == null)
                return;

            bool zombie = zombies.Contains(victim.PlayerId);
            if (zombie && damage is FirearmDamageHandler firearm)
            {
                victim.EffectsManager.ChangeState<Disabled>(1, 1);

                if (!knock_back.ContainsKey(victim.PlayerId))
                    knock_back.Add(victim.PlayerId, Vector3.zero);
                Vector3 dir = attacker.ReferenceHub.PlayerCameraReference.rotation * Vector3.forward;
                knock_back[victim.PlayerId] += (dir * config.KnockBackMultiple * (zombies_alive / humans_alive) * (firearm.Damage / 100.0f));

                //Vector3 dir = attacker.ReferenceHub.PlayerCameraReference.rotation * Vector3.forward;
                //var fpm = victim.GameObject.GetComponentInChildren<FirstPersonMovementModule>();
                //Timing.CallDelayed(0.0f, () =>
                //{
                //    float ping = (LiteNetLib4MirrorServer.Peers[victim.ReferenceHub.netIdentity.connectionToClient.connectionId].Ping * 4.0f) / 1000.0f;
                //    fpm.CharController.Move((victim.Velocity * ping) + (dir * config.KnockBackMultiple * (zombies_alive / humans_alive) * (firearm.Damage / 100.0f)));
                //    fpm.ServerOverridePosition(fpm.CharController.transform.position, Vector3.zero);
                //});
            }
        }

        [PluginEvent(ServerEventType.PlayerDeath)]
        void OnPlayerDeath(Player victim, Player attacker, DamageHandlerBase damageHandler)
        {
            if (victim == null || !Round.IsRoundStarted)
                return;

            if(!found_winner)
                found_winner = WinConditionLastClassD(victim);

            if (found_winner)
            {
                Round.IsLocked = false;
                return;
            }

            zombies.Add(victim.PlayerId);
        }

        [PluginEvent(ServerEventType.PlayerDropAmmo)]
        bool OnPlayerDroppedAmmo(Player player, ItemType type, int amount)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerReloadWeapon)]
        void OnReloadWeapon(Player player, Firearm gun)
        {
            if (gun.ItemTypeId != ItemType.ParticleDisruptor)
                player.SetAmmo(gun.AmmoType, GetStandardAmmoLimit(player.ReferenceHub.inventory.TryGetBodyArmor(out BodyArmor armor) ? armor : null, gun.AmmoType));
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

        [PluginEvent(ServerEventType.PlayerUsedItem)]
        void OnPlayerUsedItem(Player player, ItemBase item)
        {
            if (item.ItemTypeId == ItemType.Adrenaline)
                player.EffectsManager.ChangeState<MovementBoost>((byte)Mathf.Min(speed_ramping * 2.0f, 255.0f), 7);
        }

        private static IEnumerator<float> _Update()
        {
            while(true)
            {
                if (found_winner)
                    break;

                try
                {
                    zombies_alive = 0;
                    humans_alive = 0;
                    foreach(var player in Player.GetPlayers())
                    {
                        if (player.Role == RoleTypeId.Scp0492 || zombies.Contains(player.PlayerId))
                            zombies_alive++;
                        else if (player.Role == RoleTypeId.ClassD)
                            humans_alive++;
                        if(zombies.Contains(player.PlayerId))
                        {
                            int id = player.PlayerId;
                            Timing.CallDelayed(1.0f,()=>
                            {
                                Player p = Player.Get(id);
                                if (p != null && p.Role == RoleTypeId.Spectator)
                                    p.SetRole(RoleTypeId.Scp0492);
                            });
                        }
                    }
                    if (zombies_alive == 0)
                        zombies_alive = 1;
                    if (humans_alive == 0)
                        humans_alive = 1;

                }
                catch(System.Exception ex)
                {
                    Log.Error("_Update error: " + ex.ToString());
                }


                yield return Timing.WaitForSeconds(1.0f);
            }
        }

        private IEnumerator<float> _KnockBack()
        {
            while (true)
            {
                try
                {
                    foreach (var p in ReadyPlayers())
                    {
                        if (knock_back.ContainsKey(p.PlayerId) && knock_back[p.PlayerId] != Vector3.zero)
                        {
                            float ping = (LiteNetLib4MirrorServer.Peers[p.ReferenceHub.netIdentity.connectionToClient.connectionId].Ping * 4.0f) / 1000.0f;
                            var fpm = p.GameObject.GetComponentInChildren<FirstPersonMovementModule>();
                            fpm.CharController.Move((p.Velocity * ping) + knock_back[p.PlayerId]);
                            fpm.ServerOverridePosition(fpm.CharController.transform.position, Vector3.zero);
                        }
                    }
                    knock_back.Clear();
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }

                yield return Timing.WaitForOneFrame;
            }
        }
    }


    public class ZombieSurvivalEvent:IEvent
    {
        public static ZombieSurvivalEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Zombie Survival";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "ZS";
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
            harmony = new Harmony("ZombieSurvivalEvent");
            harmony.PatchAll();
            EventHandler.Start(EventConfig);
            Log.Info(EventName + " event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            harmony.UnpatchAll("ZombieSurvivalEvent");
            EventHandler.Stop();
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Zombie Survival Event", "1.0.0", "", "The Riptide")]
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
