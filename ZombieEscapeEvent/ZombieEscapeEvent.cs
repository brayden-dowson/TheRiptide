using AdminToys;
using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using CustomPlayerEffects;
using HarmonyLib;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using MapGeneration;
using MapGeneration.Distributors;
using MEC;
using Mirror;
using Mirror.LiteNetLib4Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.Voice;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using Respawning;
using RoundRestarting;
using Scp914;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        public int DefendTime { get; set; } = 45;
        public int GeneratorTime { get; set; } = 60;
        public int TransitionTime { get; set; } = 15;

        public string Description { get; set; } = "Everyone spawns in light. There are one or more zombies the rest are NTF. The NTF have to escape the facility but must do certain objectives first. Zombies infect players on kill but will lose their health doing so. Guns deal knock back based on the ratio of zombies to humans alive. Less humans more zombies = more knockback. The more zombies there are the less health each one has. Zombies and guns increase in strength as more objectives are completed. The path though the facility is fixed based on the objective and a there will be a light to guide you.\n\n";
    }

    public class EventHandler
    {
        public static Config config;
        public static HashSet<int> zombies = new HashSet<int>();
        public static int humans_alive = 1;
        public static int zombies_alive = 1;
        public static int health_pool;
        private static float knock_back;

        private static List<List<RoomIdentifier>> stage_path = new List<List<RoomIdentifier>>();
        private static List<Scp079Generator> generator_order = new List<Scp079Generator>();
        private static RoomIdentifier spawn_room;

        private static int stage;
        private static int guide_stage;
        private static LightSourceToy guide;
        private static float guide_interpolation_first = 0.0f;
        private static float guide_interpolation_last = 0.0f;
        private static bool lights_out = true;

        private static Stopwatch stopwatch = new Stopwatch();
        private static CoroutineHandle stage_update;
        private static CoroutineHandle guide_update;
        private static CoroutineHandle warhead_lever;
        private static CoroutineHandle no_flashlight;
        private static CoroutineHandle path_shortener;
        private static CoroutineHandle room_hints;
        private static CoroutineHandle zombie_update;

        private static bool lock_warhead_lever = true;


        public static void Start(Config config)
        {
            EventHandler.config = config;
            zombies.Clear();
            stage = 0;

            Round.IsLobbyLocked = true;
            Timing.CallDelayed(60.0f, () => Round.IsLobbyLocked = false);
        }

        public static void Stop()
        {
            zombies.Clear();
            Timing.KillCoroutines(stage_update, guide_update, warhead_lever, no_flashlight, path_shortener, room_hints, zombie_update);
            stage_path.Clear();
            generator_order.Clear();
            stopwatch.Reset();
            NetworkServer.Destroy(guide.gameObject);
            guide = null;
            spawn_room = null;
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + ZombieEscapeEvent.Singleton.EventName + "\n<size=24>" + ZombieEscapeEvent.Singleton.EventDescription.Replace("\n", "") + "</size>", 90, shouldClearPrevious: true);

            if (Round.IsRoundStarted)
            {
                Timing.CallDelayed(1.0f, () =>
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

            //if (zombies.Count == 0)
            //{
            //    HashSet<int> spectators = new HashSet<int>();
            //    foreach (var p in Player.GetPlayers())
            //        if (p.Role == RoleTypeId.Spectator)
            //            spectators.Add(p.PlayerId);

            //    if (!spectators.IsEmpty())
            //        zombies.Add(spectators.ElementAt(Random.Range(0, spectators.Count)));
            //    else
            //        zombies.Add(Player.GetPlayers().RandomItem().PlayerId);
            //    Player.Get(zombies.First()).SetRole(RoleTypeId.Scp0492);
            //}
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted || new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Overwatch)
                return true;

            if (zombies.Contains(player.PlayerId))
            {
                if (new_role != RoleTypeId.Scp0492)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.Scp0492);
                    });
                    return false;
                }
            }
            else
            {
                if (new_role != RoleTypeId.NtfSergeant)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.NtfSergeant);
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

            if (role == RoleTypeId.Scp0492 && zombies.Contains(player.PlayerId))
            {
                Timing.CallDelayed(0.2f, () =>
                {
                    if (spawn_room.Zone == FacilityZone.Surface)
                        Teleport.RoomPos(player, spawn_room, new Vector3(-9.797f, 0.960f, 0.414f));
                    else
                        Teleport.Room(player, spawn_room);
                });
            }
            else
            {
                Timing.CallDelayed(0.1f, () =>
                {
                    if (player.Role == RoleTypeId.NtfSergeant && !zombies.Contains(player.PlayerId))
                    {
                        if (spawn_room.Zone == FacilityZone.Surface)
                            Teleport.RoomPos(player, spawn_room, new Vector3(-9.797f, 0.960f, 0.414f));
                        else
                            Teleport.Room(player, spawn_room);
                        player.ClearInventory();
                        player.AddItem(ItemType.KeycardO5);
                        player.AddItem(ItemType.ArmorCombat);
                        AddFirearm(player, ItemType.GunCOM15, true);
                        player.AddItem(ItemType.Radio);
                    }
                });
            }
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            Round.IsLocked = true;
            stage = 0;
            guide_stage = 0;
            guide_interpolation_first = 0.0f;
            guide_interpolation_last = 0.0f;

            health_pool = 15;
            knock_back = 50.0f;

            lights_out = true;
            lock_warhead_lever = true;

            BuildStagePath();
            FacilityManager.LockAllRooms(DoorLockReason.AdminCommand);
            FacilityManager.UnlockJoinedRooms(stage_path[stage].ToHashSet(), DoorLockReason.AdminCommand);

            stage_update = Timing.RunCoroutine(_StageUpdate());
            guide_update = Timing.RunCoroutine(_GuideUpdate());
            warhead_lever = Timing.RunCoroutine(_WarHeadLever());
            no_flashlight = Timing.RunCoroutine(_NoFlashlightAttachments());
            path_shortener = Timing.RunCoroutine(_PathShortener());
            room_hints = Timing.RunCoroutine(_FirstRoomHint());

            GameObject light_pf = NetworkManager.singleton.spawnPrefabs.First(p => p.name == "LightSourceToy");
            guide = Object.Instantiate(light_pf, stage_path[0].First().transform.position + Vector3.up, Quaternion.identity).GetComponent<LightSourceToy>();
            guide.NetworkLightColor = new Color(0.5f, 0.5f, 0.5f);
            guide.NetworkLightIntensity = 50.0f;
            guide.NetworkLightRange = 50.0f;
            guide.NetworkLightShadows = true;
            guide.NetworkMovementSmoothing = 50;
            NetworkServer.Spawn(guide.gameObject);

            spawn_room = RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.LczClassDSpawn);
            Timing.CallDelayed(1.0f, () => FacilityManager.LockRoom(spawn_room, DoorLockReason.AdminCommand));
            Timing.CallDelayed(3.0f, () =>
            {
                FacilityManager.SetAllRoomLightStates(false);
                FacilityManager.UnlockRoom(spawn_room, DoorLockReason.AdminCommand);
            });

            Timing.CallDelayed(15.0f, () =>
            {
                //if (zombies.IsEmpty())
                //{
                //    Player selected = Player.GetPlayers().RandomItem();
                //    zombies.Add(selected.PlayerId);
                //    selected.SetRole(RoleTypeId.Scp0492);
                //}
                zombie_update = Timing.RunCoroutine(_ZombieUpdate());
            });

            DoorDamageType ignore_all = DoorDamageType.ServerCommand | DoorDamageType.Grenade | DoorDamageType.Weapon | DoorDamageType.Scp096;
            foreach(var d in DoorVariant.AllDoors)
                if (d is BreakableDoor door)
                    door.IgnoredDamageSources = ignore_all;

            Log.Info("zombie escape has started");
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

                Vector3 dir = attacker.ReferenceHub.PlayerCameraReference.rotation * Vector3.forward;
                var fpm = victim.GameObject.GetComponentInChildren<FirstPersonMovementModule>();
                Timing.CallDelayed(0.0f, () =>
                {
                    float ping = (LiteNetLib4MirrorServer.Peers[victim.ReferenceHub.netIdentity.connectionToClient.connectionId].Ping * 4.0f) / 1000.0f;
                    fpm.CharController.Move((victim.Velocity * ping) + (dir * knock_back * (zombies_alive / humans_alive) * (firearm.Damage / 100.0f)));
                    fpm.ServerOverridePosition(fpm.CharController.transform.position, Vector3.zero);
                });
            }
        }

        //[PluginEvent(ServerEventType.PlayerDeath)]
        //bool OnPlayerDying(Player victim, Player attacker, DamageHandlerBase damageHandler)
        //{
        //    if (victim == null || !Round.IsRoundStarted)
        //        return true;

        //    if(zombies.Contains(victim.PlayerId))
        //    {
        //        victim.SetRole(RoleTypeId.Scp0492);
        //        return false;
        //    }
        //    return true;
        //}

        [PluginEvent(ServerEventType.PlayerDeath)]
        void OnPlayerDeath(Player victim, Player attacker, DamageHandlerBase damageHandler)
        {
            if (victim == null || !Round.IsRoundStarted)
                return;

            if (attacker != null && zombies.Contains(attacker.PlayerId))
                attacker.Health = 1.0f;

            zombies.Add(victim.PlayerId);
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        bool OnRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerActivateGenerator)]
        bool OnPlayerActivateGenerator(Player player, Scp079Generator gen)
        {
            if (generator_order.First() == gen && stage == guide_stage)
                return true;
            else
                player.SendBroadcast("Generators must be turned on in order", 3, shouldClearPrevious: true);
            return false;
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

        [PluginEvent(ServerEventType.WarheadStart)]
        void OnWarheadStart(bool auto, Player player, bool resumed)
        {
            spawn_room = RoomIdentifier.AllRoomIdentifiers.First(r => r.Zone == FacilityZone.Surface);
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
                player.SetAmmo(gun.AmmoType, (ushort)player.GetAmmoLimit(gun.AmmoType));
        }

        [PluginEvent(ServerEventType.Scp914Activate)]
        bool OnScp914Activate(Player player, Scp914KnobSetting knob_setting)
        {
            return false;
        }

        private static IEnumerator<float> _GuideUpdate()
        {
            //float first_completion = 0.0f;
            while (true)
            {
                try
                {
                    List<RoomIdentifier> guide_path = stage_path[guide_stage];
                    //RoomIdentifier first_room = guide_path.First();
                    float average = 0.0f;
                    int count = 0;
                    //int first_index = guide_path.IndexOf(first_room);
                    foreach (var p in Player.GetPlayers())
                    {
                        if (p.Role != RoleTypeId.NtfSergeant || p.Room == null)
                            continue;

                        int room_index = guide_path.IndexOf(p.Room);
                        if (room_index == -1)
                            continue;

                        float start_dist = 1.0f, end_dist = 1.0f;
                        if (p.Room == guide_path.First())
                            end_dist = 0.0f;
                        else if (IsEndRoom(p.Room))
                        {
                            if (room_index != 0 && DoorVariant.DoorsByRoom[p.Room].Any(d => d.Rooms.Contains(guide_path[room_index - 1])))
                            {
                                DoorVariant start = DoorVariant.DoorsByRoom[p.Room].First(d => d.Rooms.Contains(guide_path[room_index - 1]));
                                start_dist = Vector2.Distance(new Vector2(p.Position.x, p.Position.z), new Vector2(start.transform.position.x, start.transform.position.z));
                                end_dist = Vector2.Distance(new Vector2(p.Position.x, p.Position.z), new Vector2(p.Room.transform.position.x, p.Room.transform.position.z));
                            }
                            else if(room_index != guide_path.Count - 1 && DoorVariant.DoorsByRoom[p.Room].Any(d => d.Rooms.Contains(guide_path[room_index + 1])))
                            {
                                DoorVariant end = DoorVariant.DoorsByRoom[p.Room].First(d => d.Rooms.Contains(guide_path[room_index + 1]));
                                start_dist = Vector2.Distance(new Vector2(p.Position.x, p.Position.z), new Vector2(p.Room.transform.position.x, p.Room.transform.position.z));
                                end_dist = Vector2.Distance(new Vector2(p.Position.x, p.Position.z), new Vector2(end.transform.position.x, end.transform.position.z));
                            }
                        }
                        else if (0 < room_index && room_index < guide_path.Count - 1)
                        {
                            DoorVariant start = DoorVariant.DoorsByRoom[p.Room].First(d => d.Rooms.Contains(guide_path[room_index - 1]));
                            DoorVariant end = DoorVariant.DoorsByRoom[p.Room].First(d => d.Rooms.Contains(guide_path[room_index + 1]));
                            start_dist = Vector2.Distance(new Vector2(p.Position.x, p.Position.z), new Vector2(start.transform.position.x, start.transform.position.z));
                            end_dist = Vector2.Distance(new Vector2(p.Position.x, p.Position.z), new Vector2(end.transform.position.x, end.transform.position.z));
                        }
                        average += room_index + (start_dist / (start_dist + end_dist));
                        count++;
                    }
                    average = average / count;
                    average += 0.25f;
                    average = Mathf.Clamp(average, 0.0f, guide_path.Count - 0.001f);
                    int index = Mathf.FloorToInt(average);
                    if (index >= 0 && index < guide_path.Count)
                    {
                        float fraction = average - index;

                        RoomIdentifier room = guide_path[index];

                        Vector3 position = Vector3.zero;
                        if (room != guide_path.First() && room != guide_path.Last() && !IsEndRoom(room))
                        {
                            DoorVariant start = DoorVariant.DoorsByRoom[room].First(d => d.Rooms.Contains(guide_path[index - 1]));
                            DoorVariant end = DoorVariant.DoorsByRoom[room].First(d => d.Rooms.Contains(guide_path[index + 1]));
                            position = QuadraticBezierCurve(start.transform.position + Vector3.up, room.transform.position + Vector3.up, end.transform.position + Vector3.up, fraction);
                        }
                        else
                        {
                            if (room == guide_path.First())
                            {
                                guide_interpolation_first = Mathf.Min(guide_interpolation_first + 0.05f, 1.0f);
                                Vector3 start;
                                Vector3 end;
                                if (room.Name == RoomName.LczCheckpointB)
                                {
                                    start = room.transform.TransformPoint(new Vector3(15.0f, 1.0f, 0.0f));
                                    end = guide_path[index + 1].transform.TransformPoint(new Vector3(-7.5f, 1.0f, 0.0f));
                                }
                                else
                                {
                                    start = room.transform.position + Vector3.up;
                                    end = DoorVariant.DoorsByRoom[room].First(d => d.Rooms.Contains(guide_path[index + 1])).transform.position + Vector3.up;
                                }
                                position = ((1.0f - guide_interpolation_first) * start) + (guide_interpolation_first * end);
                            }
                            else if (room == guide_path.Last())
                            {
                                Vector3 start;
                                Vector3 end;
                                if (room.Name == RoomName.LczCheckpointB)
                                {
                                    guide_interpolation_last = Mathf.Min(guide_interpolation_last + 0.025f, 1.0f);
                                    start = DoorVariant.DoorsByRoom[room].First(d => d.Rooms.Contains(guide_path[index - 1])).transform.position + Vector3.up; ;
                                    end = room.transform.TransformPoint(new Vector3(15.0f, 1.0f, 0.0f));
                                }
                                else if (room.Zone == FacilityZone.Surface)
                                {
                                    guide_interpolation_last = Mathf.Min(guide_interpolation_last + 0.001f, 1.0f);
                                    start = RoomIdentifier.AllRoomIdentifiers.First(r => r.Zone == FacilityZone.Surface).transform.TransformPoint(new Vector3(-19.372f, -8.351f, -42.773f));
                                    end = RoomIdentifier.AllRoomIdentifiers.First(r => r.Zone == FacilityZone.Surface).transform.TransformPoint(new Vector3(126.862f, -4.544f, -43.040f));
                                }
                                else
                                {
                                    guide_interpolation_last = Mathf.Min(guide_interpolation_last + 0.025f, 1.0f);
                                    start = DoorVariant.DoorsByRoom[room].First(d => d.Rooms.Contains(guide_path[index - 1])).transform.position + Vector3.up;
                                    end = room.transform.position + Vector3.up;
                                }
                                position = ((1.0f - guide_interpolation_last) * start) + (guide_interpolation_last * end);
                            }
                            else if (IsEndRoom(room))
                                if (room.Name == RoomName.HczCheckpointB)
                                    position = room.transform.TransformPoint(new Vector3(-6.0f, 1.0f, 0.0f));
                        }

                        //idk what im doing
                        guide.transform.position = position;
                        //guide.NetworkPosition = position;
                        //guide.UpdatePositionServer();
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }

                yield return Timing.WaitForSeconds(0.1f);
            }
        }

        private static bool IsEndRoom(RoomIdentifier room)
        {
            int count = DoorVariant.DoorsByRoom[room].Where(d => d.Rooms.Count() == 2).Count();
            return count == 1 || room.Zone == FacilityZone.Surface;
        }

        private static float TransitionToNextStage()
        {
            if (stage + 1 > stage_path.Count)
                return 0.0f;
            guide_interpolation_first = 0.0f;
            guide_interpolation_last = 0.0f;
            guide_stage = stage + 1;
            FacilityManager.ResetRoomLight(stage_path[stage].First());
            if (lights_out)
                FacilityManager.SetRoomLightState(stage_path[stage].First(), false);
            FacilityManager.SetRoomLightState(stage_path[stage + 1].First(), true);
            FacilityManager.SetRoomLightColor(stage_path[stage + 1].First(), new Color(0.5f, 0.5f, 0.1f));
            FacilityManager.UnlockJoinedRooms(stage_path[stage + 1].ToHashSet(), DoorLockReason.AdminCommand);
            spawn_room = stage_path[stage + 1].Last();
            Timing.CallDelayed(config.TransitionTime, () =>
            {
                HashSet<RoomIdentifier> old = stage_path[stage].Except(stage_path[stage + 1]).ToHashSet();
                while(stage_path[stage + 1].Count < 4)
                {
                    HashSet<RoomIdentifier> adjacent = FacilityManager.GetAdjacent(stage_path[stage + 1].First()).Keys.ToHashSet();
                    bool found = false;
                    foreach(var adj in adjacent)
                    {
                        if(!stage_path[stage + 1].Contains(adj))
                        {
                            found = true;
                            stage_path[stage + 1].Insert(0, adj);
                            break;
                        }
                    }
                    if (!found)
                        break;
                }
                spawn_room = stage_path[stage + 1].First();
                FacilityManager.SetRoomLightColor(spawn_room, new Color(0.5f, 0.1f, 0.1f));
                stage++;
                FacilityManager.CloseRooms(old);
                FacilityManager.LockRooms(old, DoorLockReason.AdminCommand);
                FacilityManager.UnlockJoinedRooms(stage_path[stage].ToHashSet(), DoorLockReason.AdminCommand);
            });
            return config.TransitionTime + 1.0f;
        }

        enum Objective
        {
            None, Scp914, LczArmory, HczArmory, Generator, Nuke, Intercom, Surface
        };

        private static void ObjectiveReward(Objective objective)
        {
            switch(objective)
            {
                case Objective.Scp914:
                    health_pool = 25;
                    knock_back = 40.0f;
                    foreach (var p in Player.GetPlayers())
                    {
                        if (p.Role != RoleTypeId.NtfSergeant)
                            continue;
                        RemoveItem(p, ItemType.GunCOM15);
                        if (!p.IsInventoryFull)
                            AddFirearm(p, ItemType.GunCOM18, true);
                    }
                    break;
                case Objective.LczArmory:
                    health_pool = 100;
                    knock_back = 12.0f;
                    foreach (var p in Player.GetPlayers())
                    {
                        if (p.Role != RoleTypeId.NtfSergeant)
                            continue;
                        RemoveItem(p, ItemType.GunCOM18);
                        if (!p.IsInventoryFull)
                            AddFirearm(p, ItemType.GunFSP9, true);
                    }
                    break;
                case Objective.HczArmory:
                    health_pool = 300;
                    knock_back = 4.0f;
                    foreach (var p in Player.GetPlayers())
                    {
                        if (p.Role != RoleTypeId.NtfSergeant)
                            continue;
                        RemoveItem(p, ItemType.GunFSP9);
                        if (!p.IsInventoryFull)
                        {
                            Firearm gun = AddFirearm(p, ItemType.GunCrossvec, true);
                            if (gun != null)
                                AddAttachment(gun, AttachmentName.Flashlight);
                        }
                    }
                    break;
                case Objective.Generator:
                    health_pool += 100;
                    knock_back += 2.0f;
                    ItemType reward = ItemType.None;
                    switch(generator_order.Count)
                    {
                        case 1: reward = ItemType.GrenadeHE; break;
                        case 2: reward = ItemType.Medkit; break;
                        case 3: reward = ItemType.Painkillers; break;
                    }
                    foreach (var p in Player.GetPlayers())
                    {
                        if (p.Role != RoleTypeId.NtfSergeant)
                            continue;
                        p.AddItem(reward);
                    }
                    break;
                case Objective.Nuke:
                    health_pool = 900;
                    foreach (var p in Player.GetPlayers())
                    {
                        if (p.Role != RoleTypeId.NtfSergeant)
                            continue;
                        if (!p.IsInventoryFull)
                            AddFirearm(p, ItemType.GunE11SR, true);
                    }
                    break;
                case Objective.Intercom:
                    health_pool = 1200;
                    foreach (var p in Player.GetPlayers())
                    {
                        if (p.Role != RoleTypeId.NtfSergeant)
                            continue;
                        p.AddItem(ItemType.GrenadeHE);
                    }
                    break;
                case Objective.Surface:
                    health_pool = 1500;
                    knock_back = 5.0f;
                    foreach (var p in Player.GetPlayers())
                    {
                        if (p.Role != RoleTypeId.NtfSergeant)
                            continue;
                        if (!p.IsInventoryFull)
                            AddFirearm(p, ItemType.GunLogicer, true);
                        p.AddItem(ItemType.MicroHID);
                    }
                    break;
            }
        }

        private static IEnumerator<float> _FirstRoomHint()
        {
            while(true)
            {
                try
                {
                    RoomIdentifier first = stage_path[guide_stage].First();
                    RoomIdentifier last = stage_path[guide_stage].Last();
                    var at_gen = last.gameObject.GetComponentsInChildren<Scp079Generator>().Where(g => generator_order.IsEmpty() ? false : g == generator_order.First());
                    bool overcharge_ready = generator_order.IsEmpty() && last.Name == RoomName.Hcz079;
                    foreach (var p in Player.GetPlayers())
                    {
                        if (p.Role != RoleTypeId.NtfSergeant || p.Room == null)
                            continue;

                        if (first == p.Room)
                        {
                            if (first.Name == RoomName.LczClassDSpawn)
                                p.SendBroadcast("Objective: go to scp 914", 2, shouldClearPrevious: true);
                            else if (first.Name == RoomName.Lcz914)
                                p.SendBroadcast("Objective: go to the light containment zone armory", 2, shouldClearPrevious: true);
                            else if (first.Name == RoomName.LczArmory)
                                p.SendBroadcast("Objective: go to checkpoint b elevators", 2, shouldClearPrevious: true);
                            else if (first.Name == RoomName.LczCheckpointB)
                                p.SendBroadcast("Objective: go to the heavy containment zone armory to get flashlights", 2, shouldClearPrevious: true);
                            else if (!at_gen.IsEmpty())
                                p.SendBroadcast("Objective: go turn on generator number: " + (4 - generator_order.Count), 2, shouldClearPrevious: true);
                            else if (overcharge_ready)
                                p.SendBroadcast("Objective: go turn on the lights at scp 079", 2, shouldClearPrevious: true);
                            else if (first.Name == RoomName.Hcz079)
                                p.SendBroadcast("Objective: go turn on the nuke", 2, shouldClearPrevious: true);
                            else if (first.Name == RoomName.HczWarhead)
                                p.SendBroadcast("Objective: go radio for help at the intercom", 2, shouldClearPrevious: true);
                            else if (first.Name == RoomName.EzIntercom)
                                p.SendBroadcast("Objective: go to entrance gate a", 2, shouldClearPrevious: true);
                            else if (first.Name == RoomName.EzGateA)
                                p.SendBroadcast("Objective: detonate nuke and escape in helicopter", 2, shouldClearPrevious: true);
                        }
                    }
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }

        private static IEnumerator<float> _StageUpdate()
        {
            Scp079Recontainer recontainer = Object.FindObjectOfType<Scp079Recontainer>();
            bool overcharged = false;
            bool helisent = false;
            List<string> winners = new List<string>();
            while (true)
            {
                float yield = Timing.WaitForSeconds(1.0f);
                try
                {
                    RoomIdentifier last = stage_path[stage].Last();
                    RoomIdentifier first = stage_path[guide_stage].First();
                    var at_gen = last.gameObject.GetComponentsInChildren<Scp079Generator>().Where(g => generator_order.IsEmpty() ? false : g == generator_order.First());
                    bool overcharge_ready = generator_order.IsEmpty() && last.Name == RoomName.Hcz079;
                    bool nuke = lock_warhead_lever == false && last.Name == RoomName.HczWarhead;
                    bool intercom = last.Name == RoomName.EzIntercom;
                    bool surface = last.Zone == FacilityZone.Surface;
                    foreach (var p in Player.GetPlayers())
                    {
                        if (p.Role != RoleTypeId.NtfSergeant || p.Room == null)
                            continue;

                        if (last == p.Room)
                        {
                            if (!at_gen.IsEmpty())
                            {
                                Scp079Generator gen = at_gen.First();
                                if (!gen.Activating)
                                    p.SendBroadcast("Activate the generator", 2, shouldClearPrevious: true);
                                else
                                    p.SendBroadcast("Generator has " + gen.RemainingTime + " seconds remaining", 2, shouldClearPrevious: true);
                            }
                            else if (overcharge_ready)
                            {
                                if (!recontainer._alreadyRecontained)
                                    p.SendBroadcast("Overcharge to turn on the lights", 2, shouldClearPrevious: true);
                            }
                            else if (nuke)
                            {
                                if (Warhead.LeverStatus == false)
                                    p.SendBroadcast("Activate the nuke", 2, shouldClearPrevious: true);
                            }
                            else if (intercom)
                            {
                                if (Intercom.State == IntercomState.Ready)
                                    p.SendBroadcast("Radio for help", 2, shouldClearPrevious: true);
                            }
                            else if (surface)
                            {
                                if (!Warhead.IsDetonationInProgress)
                                    p.SendBroadcast("Start nuke", 2, shouldClearPrevious: true);
                                else if (Warhead.DetonationTime > 30)
                                    p.SendBroadcast("Defend for " + (Warhead.DetonationTime - 30).ToString("0") + " seconds", 2, shouldClearPrevious: true);
                                else if (Warhead.DetonationTime > 17)
                                    p.SendBroadcast("Escape in the helicopter", 2, shouldClearPrevious: true);
                                else if (Warhead.DetonationTime > 9)
                                {
                                    yield = 0.2f;
                                    Vector3 offset = last.transform.InverseTransformPoint(p.Position);
                                    float dist = Vector3.Distance(offset, new Vector3(126.862f, -4.544f, -43.040f));
                                    if (dist < 5.0f)
                                    {
                                        winners.Add(p.Nickname);
                                        p.SetRole(RoleTypeId.Spectator);
                                    }
                                    else
                                        p.SendBroadcast("Hurry up! The helicopter will not wait!", 2, shouldClearPrevious: true);
                                }
                                else
                                    p.SendBroadcast("You failed to reach the helicopter in time", 60, shouldClearPrevious: true);
                            }
                            else
                            {
                                if (!stopwatch.IsRunning)
                                    stopwatch.Start();
                                else
                                    p.SendBroadcast("Defend for " + (1 + config.DefendTime - stopwatch.Elapsed.TotalSeconds).ToString("0") + " seconds", 2, shouldClearPrevious: true);
                            }
                        }
                    }

                    if(!at_gen.IsEmpty())
                    {
                        Scp079Generator gen = at_gen.First();
                        if(gen.Engaged)
                        {
                            yield = Timing.WaitForSeconds(TransitionToNextStage());
                            ObjectiveReward(Objective.Generator);
                            generator_order.Remove(gen);
                        }
                    }
                    else if(overcharge_ready && recontainer._alreadyRecontained)
                    {
                        if(!overcharged)
                        {
                            overcharged = true;
                            yield = Timing.WaitForSeconds(20.0f);
                        }
                        else
                        {
                            lights_out = false;
                            FacilityManager.SetAllRoomLightStates(true);
                            lock_warhead_lever = false;
                            guide.NetworkLightColor = new Color(0.0f, 0.5f, 1.0f);
                            yield = Timing.WaitForSeconds(TransitionToNextStage());
                        }
                    }
                    else if (nuke)
                    {
                        if (Warhead.LeverStatus)
                        {
                            lock_warhead_lever = true;
                            yield = Timing.WaitForSeconds(TransitionToNextStage());
                            ObjectiveReward(Objective.Nuke);
                        }
                    }
                    else if(intercom)
                    {
                        if(Intercom.State == IntercomState.Cooldown)
                        {
                            ObjectiveReward(Objective.Intercom);
                            yield = Timing.WaitForSeconds(TransitionToNextStage());
                        }
                    }
                    else if (surface)
                    {
                        if (!helisent && Warhead.IsDetonationInProgress && Warhead.DetonationTime < 30)
                        {
                            RespawnEffectsController.ExecuteAllEffects(RespawnEffectsController.EffectType.Selection, SpawnableTeamType.NineTailedFox);
                            helisent = true;
                        }
                    }
                    else if (stopwatch.IsRunning && stopwatch.Elapsed.TotalSeconds > config.DefendTime)
                    {
                        switch(last.Name)
                        {
                            case RoomName.Lcz914: ObjectiveReward(Objective.Scp914); break;
                            case RoomName.LczArmory: ObjectiveReward(Objective.LczArmory); break;
                            case RoomName.HczArmory: ObjectiveReward(Objective.HczArmory); break;
                            case RoomName.EzGateA: ObjectiveReward(Objective.Surface); break;
                        }

                        stopwatch.Reset();
                        yield = Timing.WaitForSeconds(TransitionToNextStage());
                    }

                    if(helisent && Warhead.IsDetonationInProgress && Warhead.DetonationTime < 5 && !winners.IsEmpty())
                    {
                        string broadcast = string.Join(", ", winners) + " Escaped!";
                        foreach (var p in Player.GetPlayers())
                            p.SendBroadcast(broadcast, 60, shouldClearPrevious: true);
                        Round.IsLocked = false;
                        //EndRound(RoundSummary.LeadingTeam.FacilityForces);
                        break;
                    }
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }

                yield return yield;
            }
        }

        private static void BuildStagePath()
        {
            Dictionary<RoomName, RoomIdentifier> named = new Dictionary<RoomName, RoomIdentifier>();
            foreach (var room in RoomIdentifier.AllRoomIdentifiers)
                if (!named.ContainsKey(room.Name))
                    named.Add(room.Name, room);

            Dictionary<Scp079Generator, RoomIdentifier> generator_rooms = new Dictionary<Scp079Generator, RoomIdentifier>();
            foreach (var room in RoomIdentifier.AllRoomIdentifiers)
            {
                Scp079Generator[] generators = room.GetComponentsInChildren<Scp079Generator>();
                foreach(var generator in generators)
                {
                    generator._totalActivationTime = config.GeneratorTime;
                    generator_rooms.Add(generator, room);
                }
            }

            RoomIdentifier surface = RoomIdentifier.AllRoomIdentifiers.First(r => r.Zone == FacilityZone.Surface);

            stage_path.Clear();
            stage_path.Add(Path(named[RoomName.LczClassDSpawn], named[RoomName.Lcz914]));
            stage_path.Add(Path(named[RoomName.Lcz914],         named[RoomName.LczArmory]));
            stage_path.Add(Path(named[RoomName.LczArmory],      named[RoomName.LczCheckpointB]));
            stage_path.Add(Path(named[RoomName.LczCheckpointB], named[RoomName.HczArmory]));
            RoomName prev = RoomName.HczArmory;
            while (!generator_rooms.IsEmpty())
            {
                int dist = 0;
                RoomIdentifier furthest_room = null;
                Scp079Generator furthest_gen = null;
                foreach (var kv in generator_rooms)
                {
                    var path = Path(named[prev], kv.Value);
                    if (path.Count > dist)
                    {
                        furthest_room = kv.Value;
                        furthest_gen = kv.Key;
                        dist = path.Count;
                    }
                }
                generator_rooms.Remove(furthest_gen);
                generator_order.Add(furthest_gen);
                stage_path.Add(Path(named[prev], named[furthest_room.Name]));
                prev = furthest_room.Name;
            }
            var path_to_079 = Path(named[prev], named[RoomName.Hcz079]);
            if(path_to_079.Count >= 2)
                stage_path.Add(path_to_079);

            stage_path.Add(Path(named[RoomName.Hcz079],         named[RoomName.HczWarhead]));
            stage_path.Add(Path(named[RoomName.HczWarhead],     named[RoomName.EzIntercom]));
            stage_path.Add(Path(named[RoomName.EzIntercom],     named[RoomName.EzGateA]));
            stage_path.Add(Path(named[RoomName.EzGateA],        surface));
        }

        private static List<RoomIdentifier> Path(RoomIdentifier start, RoomIdentifier end)
        {
            Dictionary<RoomIdentifier, RoomIdentifier> child_parent = new Dictionary<RoomIdentifier, RoomIdentifier>();
            HashSet<RoomIdentifier> visited = new HashSet<RoomIdentifier> { start };
            Queue<RoomIdentifier> next = new Queue<RoomIdentifier>();
            next.Enqueue(start);

            while(!next.IsEmpty())
            {
                RoomIdentifier room = next.Dequeue();
                if (room == end)
                    break;

                foreach(var adj in FacilityManager.GetAdjacent(room).Keys)
                {
                    if(!visited.Contains(adj))
                    {
                        child_parent.Add(adj, room);
                        visited.Add(adj);
                        if (room.Name != RoomName.LczCheckpointA)
                            next.Enqueue(adj);
                    }
                }
            }

            List<RoomIdentifier> path = new List<RoomIdentifier> { end };
            while (child_parent.ContainsKey(path.Last()))
                path.Add(child_parent[path.Last()]);
            path.Reverse();
            return path;
        }

        private static Vector3 QuadraticBezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            return p1 + ((1 - t) * (1 - t) * (p0 - p1)) + (t * t * (p2 - p1));
        }

        private static IEnumerator<float> _WarHeadLever()
        {
            AlphaWarheadNukesitePanel warhead = Server.Instance.GetComponent<AlphaWarheadNukesitePanel>(true);
            bool state = warhead.enabled;
            while (true)
            {
                try
                {
                    if (lock_warhead_lever && state != warhead.enabled)
                        warhead.Networkenabled = state;
                    else
                        state = warhead.enabled;
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                yield return Timing.WaitForOneFrame;
            }
        }

        //private static void EndRound(RoundSummary.LeadingTeam team)
        //{
        //    FriendlyFireConfig.PauseDetector = true;
        //    int round_cd = Mathf.Clamp(GameCore.ConfigFile.ServerConfig.GetInt("auto_round_restart_time", 10), 5, 1000);
        //    RoundSummary.singleton.RpcShowRoundSummary(
        //        new RoundSummary.SumInfo_ClassList(),
        //        new RoundSummary.SumInfo_ClassList(),
        //        team,
        //        RoundSummary.EscapedClassD,
        //        RoundSummary.EscapedScientists,
        //        RoundSummary.KilledBySCPs,
        //        round_cd,
        //        (int)GameCore.RoundStart.RoundLength.TotalSeconds);
        //    Timing.CallDelayed(round_cd - 1, () => RoundSummary.singleton.RpcDimScreen());
        //    Timing.CallDelayed(round_cd, () => RoundRestart.InitiateRoundRestart());
        //}

        private static IEnumerator<float> _NoFlashlightAttachments()
        {
            while(true)
            {
                try
                {
                    if (stage_path[stage].Last().Name == RoomName.HczArmory)
                        break;

                    foreach(var p in Player.GetPlayers())
                    {
                        foreach(var item in p.ReferenceHub.inventory.UserInventory.Items.Values)
                        {
                            if (IsGun(item.ItemTypeId) && item.ItemTypeId != ItemType.ParticleDisruptor)
                                RemoveAttachment(item as Firearm, AttachmentName.Flashlight);
                        }
                    }
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }

        private static IEnumerator<float> _PathShortener()
        {
            while(true)
            {
                try
                {
                    if (stage == guide_stage)
                    {
                        List<RoomIdentifier> path = stage_path[stage];
                        if (path.Count > 4)
                        {
                            int min = path.Count - 1;
                            bool is_player = false;
                            foreach (var p in Player.GetPlayers())
                            {
                                if (p.Role != RoleTypeId.NtfSergeant || p.Room == null)
                                    continue;
                                int index = path.IndexOf(p.Room);
                                if (index == -1)
                                    continue;
                                if (index < min)
                                {
                                    min = index;
                                    is_player = true;
                                }
                            }
                            if (is_player && min > 2)
                            {
                                int remove = min - 2;
                                remove = -(Mathf.Max(path.Count - remove, 4) - path.Count);

                                FacilityManager.ResetRoomLight(path.First());
                                if (lights_out)
                                    FacilityManager.SetRoomLightState(path.First(), false);
                                HashSet<RoomIdentifier> removed = path.GetRange(0, remove).ToHashSet();
                                FacilityManager.CloseRooms(removed);
                                FacilityManager.LockRooms(removed, DoorLockReason.AdminCommand);
                                path.RemoveRange(0, remove);
                                spawn_room = path.First();
                                FacilityManager.SetRoomLightColor(path.First(), new Color(0.5f, 0.1f, 0.1f));
                                FacilityManager.SetRoomLightState(path.First(), true);
                            }
                        }
                    }
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }

        private static IEnumerator<float> _ZombieUpdate()
        {
            while (true)
            {
                try
                {
                    zombies_alive = 0;
                    humans_alive = 0;
                    foreach(var p in Player.GetPlayers())
                    {
                        if (p.IsHuman)
                            humans_alive++;
                        else if (p.Role != RoleTypeId.None && (p.Role == RoleTypeId.Scp0492 || zombies.Contains(p.PlayerId)))
                            zombies_alive++;

                        if (p.IsHuman)
                        {
                            if (p.Room != null && !(stage_path[stage].Contains(p.Room) || stage_path[guide_stage].Contains(p.Room)))
                                Teleport.Room(p, spawn_room);
                        }
                        else if (p.Role == RoleTypeId.Scp0492)
                        {
                            if (p.Room == null || !(stage_path[stage].Contains(p.Room) || stage_path[guide_stage].Contains(p.Room)))
                                Teleport.Room(p, spawn_room);
                        }

                        if ((p.Role == RoleTypeId.Spectator || p.Role == RoleTypeId.NtfSergeant) && zombies.Contains(p.PlayerId))
                            p.SetRole(RoleTypeId.Scp0492);
                    }
                    if (zombies_alive == 0)
                    {
                        List<Player> valid = Player.GetPlayers().Where(r => r.Role == RoleTypeId.Spectator).ToList();
                        if (valid.IsEmpty())
                            valid = Player.GetPlayers();
                        Player selected = valid.RandomItem();
                        zombies.Add(selected.PlayerId);
                        zombies_alive = 1;
                    }
                    if (humans_alive == 0)
                    {
                        humans_alive = 1;
                        Round.IsLocked = false;
                        //EndRound(RoundSummary.LeadingTeam.Anomalies);
                        break;
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }
    }

    public class ZombieEscapeEvent:IEvent
    {
        public static ZombieEscapeEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Zombie Escape";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "ZE";
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
            harmony = new Harmony("ZombieEscapeEvent");
            harmony.PatchAll();
            EventHandler.Start(EventConfig);
            Log.Info(EventName + " event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            harmony.UnpatchAll("ZombieEscapeEvent");
            EventHandler.Stop();
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Zombie Escape Event", "1.0.0", "", "The Riptide")]
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
