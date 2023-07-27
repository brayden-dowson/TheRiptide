using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using CustomPlayerEffects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Pickups;
using MapGeneration;
using MEC;
using Mirror;
using Mirror.LiteNetLib4Mirror;
using PlayerRoles.FirstPersonControl;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
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

        public bool AllowMoving { get; set; } = true;
        public float Cooldown { get; set; } = 30.0f;
        public float ItemPdDelay { get; set; } = 60.0f * 1.0f;
    }

    public class EventHandler
    {
        enum Dir { None, PX, NX, PZ, NZ };
        enum RoomType { Other, LczStraight, LczCurve, LczTShape, LczXShape, HczStraight, HczCurve, HczTShape, HczXShape };

        class DoorOffsets
        {
            const float floor_y = 0.0f;
            const float door_y = 2.36f;

            public DoorOffsets(Dir dir)
            {
                offsets = new Vector3[6];
                switch (dir)
                {
                    case Dir.PX:
                        offsets[0] = new Vector3(7.5f, floor_y, 0.7f);
                        offsets[1] = new Vector3(7.5f, floor_y, -0.7f);
                        offsets[2] = new Vector3(7.5f, door_y, 0.7f);
                        offsets[3] = new Vector3(7.5f, door_y, -0.7f);
                        break;
                    case Dir.NX:
                        offsets[0] = new Vector3(-7.5f, floor_y, 0.7f);
                        offsets[1] = new Vector3(-7.5f, floor_y, -0.7f);
                        offsets[2] = new Vector3(-7.5f, door_y, 0.7f);
                        offsets[3] = new Vector3(-7.5f, door_y, -0.7f);
                        break;
                    case Dir.PZ:
                        offsets[0] = new Vector3(0.7f, floor_y, 7.5f);
                        offsets[1] = new Vector3(-0.7f, floor_y, 7.5f);
                        offsets[2] = new Vector3(0.7f, door_y, 7.5f);
                        offsets[3] = new Vector3(-0.7f, door_y, 7.5f);
                        break;
                    case Dir.NZ:
                        offsets[0] = new Vector3(0.7f, floor_y, -7.5f);
                        offsets[1] = new Vector3(-0.7f, floor_y, -7.5f);
                        offsets[2] = new Vector3(0.7f, door_y, -7.5f);
                        offsets[3] = new Vector3(-0.7f, door_y, -7.5f);
                        break;
                }
                offsets[4] = (offsets[0] + offsets[2]) / 2.0f;
                offsets[5] = (offsets[1] + offsets[3]) / 2.0f;
            }

            public Vector3[] offsets { get; private set; }
        }

        private static Dictionary<Dir, DoorOffsets> dir_offset = new Dictionary<Dir, DoorOffsets>
        {
            { Dir.PX, new DoorOffsets(Dir.PX) },
            { Dir.NX, new DoorOffsets(Dir.NX) },
            { Dir.PZ, new DoorOffsets(Dir.PZ) },
            { Dir.NZ, new DoorOffsets(Dir.NZ) }
        };

        private static Dictionary<RoomType, List<Dir>> type_to_offsets = new Dictionary<RoomType, List<Dir>>()
        {
            { RoomType.HczStraight, new List<Dir>{Dir.PX, Dir.NX } },
            { RoomType.HczCurve,    new List<Dir>{Dir.PX, Dir.NZ } },
            { RoomType.HczTShape,   new List<Dir>{Dir.NX, Dir.PZ, Dir.NZ } },
            { RoomType.HczXShape,   new List<Dir>{Dir.PX, Dir.NX, Dir.PZ, Dir.NZ } },
            { RoomType.LczStraight, new List<Dir>{Dir.PX, Dir.NX } },
            { RoomType.LczCurve,    new List<Dir>{Dir.PX, Dir.NZ } },
            { RoomType.LczTShape,   new List<Dir>{Dir.NX, Dir.PZ, Dir.NZ } },
            { RoomType.LczXShape,   new List<Dir>{Dir.PX, Dir.NX, Dir.PZ, Dir.NZ } },
        };

        private static Dictionary<RoomType, HashSet<RoomIdentifier>> type_to_rooms = new Dictionary<RoomType, HashSet<RoomIdentifier>>();
        private static Dictionary<int, Camera> player_cameras = new Dictionary<int, Camera>();
        private static Dictionary<RoomIdentifier, float> invalid_time = new Dictionary<RoomIdentifier, float>();

        private static CoroutineHandle update_player_handle;
        private static CoroutineHandle update_invalid_handle;

        public static Config config;

        public static void Start(Config config)
        {
            EventHandler.config = config;
            player_cameras.Clear();
            invalid_time.Clear();

            type_to_rooms.Clear();
            foreach (var room in RoomIdentifier.AllRoomIdentifiers)
            {
                if (room.Name == RoomName.Unnamed && GetRoomType(room) != RoomType.Other)
                {
                    if (!type_to_rooms.ContainsKey(GetRoomType(room)))
                        type_to_rooms.Add(GetRoomType(room), new HashSet<RoomIdentifier>());
                    type_to_rooms[GetRoomType(room)].Add(room);
                }
            }

            HashSet<RoomIdentifier> has_ws = new HashSet<RoomIdentifier>();
            foreach (RoomIdentifier room in type_to_rooms[RoomType.HczTShape])
                if (room.GetComponentInChildren<WorkstationController>() != null)
                    has_ws.Add(room);

            type_to_rooms[RoomType.HczTShape].ExceptWith(has_ws);

            type_to_rooms[RoomType.LczXShape].Clear();

            foreach (var key in type_to_rooms.Keys.ToList())
                if (type_to_rooms[key].Count == 1)
                    type_to_rooms[key].Clear();

            foreach (var kvp in type_to_rooms)
            {
                Log.Info(kvp.Key.ToString() + " | " + kvp.Value.Count.ToString());
            }

            update_player_handle = Timing.RunCoroutine(_UpdatePlayers());
            update_invalid_handle = Timing.RunCoroutine(_UpdateInvalid());
        }

        public static void Stop()
        {
            Timing.KillCoroutines(update_player_handle);
            Timing.KillCoroutines(update_invalid_handle);
            type_to_rooms.Clear();
            player_cameras.Clear();
            invalid_time.Clear();
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            List<string> sounds = new List<string>
            {
                ".G1 ",
                ".G2 ",
                ".G3 ",
                ".G4 ",
                ".G5 ",
                ".G6 "
            };

            string cassie = "";
            for(int i = 0; i < 1000; i++)
            {
                float pitch = (10.0f / Mathf.Pow(2, i * 0.15f));
                cassie += "pitch_" + pitch.ToString("0.00") + " "; 
                for(int j = 0; j < Mathf.CeilToInt(pitch * 0.5f); j++)
                    cassie += sounds.RandomItem();

                if (pitch < 0.1)
                    break;
            }

            Timing.CallDelayed(7.0f,()=>
            {
                Cassie.Message(cassie);
                foreach (var p in Player.GetPlayers())
                    p.EffectsManager.EnableEffect<Scanned>(7);
            });
        }

        [PluginEvent(ServerEventType.PlayerDropItem)]
        void OnPlayerDropItem(Player player, ItemBase item)
        {
            RoomIdentifier room = RoomIdUtils.RoomAtPosition(item.transform.position);
            if (room != null && room.Name != RoomName.Unnamed && GetRoomType(room) != RoomType.Other && !invalid_time.ContainsKey(room))
                invalid_time.Add(room, config.ItemPdDelay);
        }

        [PluginEvent(ServerEventType.PlayerThrowItem)]
        void OnPlayerThrowItem(Player player, ItemBase item, Rigidbody rb)
        {
            RoomIdentifier room = RoomIdUtils.RoomAtPosition(item.transform.position);
            if (room != null && room.Name != RoomName.Unnamed && GetRoomType(room) != RoomType.Other && !invalid_time.ContainsKey(room))
                invalid_time.Add(room, config.ItemPdDelay);
        }

        private static RoomType GetRoomType(RoomIdentifier room)
        {
            switch (room.Shape)
            {
                case RoomShape.Straight:
                    if (room.Zone == FacilityZone.LightContainment)
                        return RoomType.LczStraight;
                    else if (room.Zone == FacilityZone.HeavyContainment)
                        return RoomType.HczStraight;
                    break;
                case RoomShape.Curve:
                    if (room.Zone == FacilityZone.LightContainment)
                        return RoomType.LczCurve;
                    else if (room.Zone == FacilityZone.HeavyContainment)
                        return RoomType.HczCurve;
                    break;
                case RoomShape.TShape:
                    if (room.Zone == FacilityZone.LightContainment)
                        return RoomType.LczTShape;
                    else if (room.Zone == FacilityZone.HeavyContainment)
                        return RoomType.HczTShape;
                    break;
                case RoomShape.XShape:
                    if (room.Zone == FacilityZone.LightContainment)
                        return RoomType.LczXShape;
                    else if (room.Zone == FacilityZone.HeavyContainment)
                        return RoomType.HczXShape;
                    break;
            }
            return RoomType.Other;
        }

        private static bool IsDoorClosed(DoorVariant door)
        {
            return door.GetExactState() == 0;
        }

        private static bool LookingAtDoor(Player player, RoomIdentifier room, Dir dir)
        {
            int id = player.PlayerId;
            if (!player_cameras.ContainsKey(id))
                player_cameras.Add(id, UnityEngine.Object.Instantiate(Camera.main, player.ReferenceHub.PlayerCameraReference));
            Camera camera = player_cameras[id];

            foreach (var offset in dir_offset[dir].offsets)
                if (InsideViewport(camera.WorldToViewportPoint(room.transform.TransformPoint(offset))))
                    return true;

            return false;
        }

        private static Dir DoorToDir(RoomIdentifier room, DoorVariant door)
        {
            RoomType type = GetRoomType(room);
            return type_to_offsets[type].FirstOrDefault((d) => Vector3.Distance(room.transform.TransformPoint(dir_offset[d].offsets[0]), door.transform.position) < 1.0f);
        }

        private static bool PlayerMeetConditions(Player player, out Dir dir)
        {
            dir = Dir.None;
            if (!config.AllowMoving && player.Velocity.magnitude > 0.03)
                return false;

            RoomIdentifier room = player.Room;
            if (room == null || room.Name != RoomName.Unnamed || GetRoomType(room) == RoomType.Other)
                return false;

            if (invalid_time.ContainsKey(room))
                return false;

            DoorVariant nearest = null;
            foreach (var door in DoorVariant.DoorsByRoom[room])
                if (door.Rooms.Count() == 2 && (nearest == null || Vector3.Distance(nearest.transform.position, player.Position) > Vector3.Distance(door.transform.position, player.Position)))
                    nearest = door;

            var near_dir = DoorToDir(room, nearest);
            if (near_dir == Dir.None)
                return false;

            Vector3 room_pos = room.transform.InverseTransformPoint(player.Position);
            if (!ValidateTeleport(room_pos, near_dir, room.Shape != RoomShape.Straight))
                return false;

            if (LookingAtDoor(player, room, near_dir))
            {
                if (!IsDoorClosed(nearest))
                    return false;
                else
                    dir = near_dir;
            }

            DoorVariant opposite = null;
            if (room.Shape != RoomShape.Curve)
                foreach (var door in DoorVariant.DoorsByRoom[room])
                    if (door.Rooms.Count() == 2 && 14.0f < Vector3.Distance(door.transform.position, nearest.transform.position))
                        opposite = door;

            if (opposite != null)
            {
                if (room.Zone == FacilityZone.LightContainment)
                    return false;

                var opposite_dir = DoorToDir(room, opposite);
                if (opposite_dir == Dir.None)
                    return false;
                if (LookingAtDoor(player, room, opposite_dir))
                {
                    if (!IsDoorClosed(opposite))
                        return false;
                    else
                        dir = opposite_dir;
                }
            }

            return true;
        }

        private static bool RoomMeetsConditions(RoomIdentifier room, List<Dir> closed)
        {
            foreach (var door in DoorVariant.DoorsByRoom[room])
                if (door.Rooms.Length == 2 && !IsDoorClosed(door) && closed.Contains(DoorToDir(room, door)))
                    return false;
            return true;
        }

        private static IEnumerator<float> _UpdatePlayers()
        {
            while (true)
            {
                foreach (var player in Player.GetPlayers())
                {
                    bool met_conditions = false;
                    Dir see = Dir.None;
                    try { met_conditions = PlayerMeetConditions(player, out see); }
                    catch (Exception ex) { Log.Error("_UpdatePlayers Error: " + ex.ToString()); }
                    if (met_conditions)
                    {
                        try
                        {
                            RoomIdentifier room = player.Room;
                            if (invalid_time.ContainsKey(room))
                                continue;

                            HashSet<RoomIdentifier> occupied = new HashSet<RoomIdentifier>();
                            foreach (var p in Player.GetPlayers())
                                if (p.Room != null)
                                    occupied.Add(p.Room);
                            if (type_to_rooms[GetRoomType(room)].Except(invalid_time.Keys).Except(occupied).IsEmpty())
                                continue;

                            IEnumerable<Player> with = Player.GetPlayers().Where((p) => p != player && p.Room == room);
                            List<Dir> visible_doors = new List<Dir> { see };
                            bool didnt_meet_conditions = false;
                            foreach (var p in with)
                            {
                                visible_doors.Add(Dir.None);
                                see = Dir.None;
                                if (!PlayerMeetConditions(p, out see))
                                {
                                    didnt_meet_conditions = true;
                                    break;
                                }
                            }
                            if (didnt_meet_conditions)
                                continue;
                            visible_doors.RemoveAll((d) => d == Dir.None);

                            var available = type_to_rooms[GetRoomType(room)].Where((r) => RoomMeetsConditions(r, visible_doors)).Except(invalid_time.Keys).Except(occupied);
                            if (available.IsEmpty())
                                continue;
                            RoomIdentifier dest = available.ElementAt(UnityEngine.Random.Range(0, available.Count()));
                            SeamlessTeleport(player, room, dest);
                            foreach (var p in with)
                                SeamlessTeleport(p, room, dest);
                            Log.Info("successful tp");
                        }
                        catch (Exception ex)
                        {
                            Log.Error("_UpdatePlayers Error: " + ex.ToString());
                        }
                        yield return Timing.WaitForSeconds(config.Cooldown);
                    }
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }

        private static IEnumerator<float> _UpdateInvalid()
        {
            float delta = 1.0f;
            while (true)
            {
                try
                {
                    HashSet<RoomIdentifier> occupied = new HashSet<RoomIdentifier>();
                    foreach (var p in Player.GetPlayers())
                        if (p.Room != null)
                            occupied.Add(p.Room);

                    ItemPickupBase[] items = UnityEngine.Object.FindObjectsOfType<ItemPickupBase>();
                    BasicRagdoll[] ragdolls = UnityEngine.Object.FindObjectsOfType<BasicRagdoll>();
                    foreach (var item in items)
                    {
                        if (item != null)
                        {
                            RoomIdentifier room = RoomIdUtils.RoomAtPosition(item.transform.position);
                            if (config != null && room != null && room.Name != RoomName.Unnamed && GetRoomType(room) != RoomType.Other && !invalid_time.ContainsKey(room))
                                invalid_time.Add(room, config.ItemPdDelay);
                        }
                    }

                    foreach (var ragdoll in ragdolls)
                    {
                        if(ragdoll != null)
                        {
                            RoomIdentifier room = RoomIdUtils.RoomAtPosition(ragdoll.transform.position);
                            if (config != null && room != null && room.Name != RoomName.Unnamed && GetRoomType(room) != RoomType.Other && !invalid_time.ContainsKey(room))
                                invalid_time.Add(room, config.ItemPdDelay);
                        }
                    }

                    foreach (var room in invalid_time.Keys.ToList())
                    {
                        invalid_time[room] -= delta;
                        if (invalid_time[room] < 0.0f && !occupied.Contains(room))
                        {
                            invalid_time.Remove(room);

                            foreach (var item in items)
                            {
                                if (item != null)
                                {
                                    RoomIdentifier r = RoomIdUtils.RoomAtPosition(item.transform.position);
                                    if (r != null && r == room)
                                    {
                                        PickupSyncInfo o = item.NetworkInfo;
                                        item.transform.position = new Vector3(UnityEngine.Random.Range(-4.0f, 4.0f), -1999.0f, UnityEngine.Random.Range(-4.0f, 4.0f));
                                        item.NetworkInfo = new PickupSyncInfo(o.ItemId, o.WeightKg, o.Serial);
                                    }
                                }
                            }

                            foreach (var ragdoll in ragdolls)
                            {
                                if (ragdoll != null)
                                {
                                    RoomIdentifier r = RoomIdUtils.RoomAtPosition(ragdoll.transform.position);
                                    if (r != null && r == room)
                                    {
                                        NetworkServer.Destroy(ragdoll.gameObject);
                                    }
                                }
                            }

                            FacilityManager.CloseRoom(room);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("_UpdateInvalid Error: " + ex.ToString());
                }
                yield return Timing.WaitForSeconds(delta);
            }
        }

        private static bool ValidateTeleport(Vector3 pos, Dir dir, bool check_max)
        {
            const float max_threshold = 3.5f;
            const float min_threshold = 1.0f;
            float dist = 0.0f;
            switch (dir)
            {
                case Dir.PX:
                    dist = Vector2.Distance(new Vector2(7.5f, 0.0f), new Vector2(pos.x, pos.z));
                    break;
                case Dir.NX:
                    dist = Vector2.Distance(new Vector2(-7.5f, 0.0f), new Vector2(pos.x, pos.z));
                    break;
                case Dir.PZ:
                    dist = Vector2.Distance(new Vector2(0.0f, 7.5f), new Vector2(pos.x, pos.z));
                    break;
                case Dir.NZ:
                    dist = Vector2.Distance(new Vector2(0.0f, -7.5f), new Vector2(pos.x, pos.z));
                    break;
            }
            if (check_max)
                return min_threshold < dist && dist < max_threshold;
            else
                return min_threshold < dist;
        }

        private static bool InsideViewport(Vector3 p)
        {
            return 0.0f < p.x && p.x < 1.0f && 0.0f < p.y && p.y < 1.0f && 0.0f < p.z;
        }

        private static void SeamlessTeleport(Player player, RoomIdentifier room, RoomIdentifier dest)
        {
            float ping = (LiteNetLib4MirrorServer.Peers[player.ReferenceHub.netIdentity.connectionToClient.connectionId].Ping * 4.0f) / 1000.0f;
            Vector3 pos = room.transform.InverseTransformPoint(player.Position);
            Vector3 delta_pos = dest.transform.TransformPoint(pos) - player.Position;
            Vector3 delta_rot = dest.transform.rotation.eulerAngles - room.transform.rotation.eulerAngles;
            var fpm = player.GameObject.GetComponentInChildren<FirstPersonMovementModule>();
            fpm.CharController.transform.position += delta_pos;
            fpm.CharController.Move(((Quaternion.Euler(delta_rot) * player.Velocity) * ping));
            fpm.ServerOverridePosition(fpm.CharController.transform.position, delta_rot);
        }
    }


    public class DogInfectionEvent : IEvent
    {
        public static DogInfectionEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Insanity";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription { get; set; } = "[DATA EXPUNGED]\n\n";
        public string EventPrefix { get; } = "INS";
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

        [PluginEntryPoint("Insanity", "1.0.0", "[DATA EXPUNGED]", "The Riptide")]
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
