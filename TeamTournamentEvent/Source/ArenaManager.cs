using AdminToys;
using CustomPlayerEffects;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Pickups;
using MapGeneration;
using MEC;
using Mirror;
using PluginAPI.Core;
using slocLoader;
using slocLoader.Objects;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public enum Zone
    {
        Surface,
        Entrance,
        Heavy,
        Light
    }

    public enum RoomSpawn
    {
        SpawnA,
        SpawnB,
    }

    public class BoundsState
    {
        //public const float SizeDecreaseRate = 1.34f;
        //public const float MinSizeSurface = 3.0f;
        //public const float MinSizeRoom = 7.5f;
        //public const float GracePeriod = 20.0f;

        public bool Enabled = false;
        public float TimeLeft = EventHandler.config.ZoneShrinkDelay;
        public float CurrentSize = 1.0f;
        public RoomIdentifier Target = null;

        public class QuadDuo
        {
            public PrimitiveObjectToy inner = null;
            public PrimitiveObjectToy outer = null;

            public QuadDuo(Vector3 pos, float rot_y, Vector3 scale, Color inner_color, Color outer_color)
            {
                PrimitiveObject obj = new PrimitiveObject(ObjectType.Quad);
                obj.ColliderMode = PrimitiveObject.ColliderCreationMode.NoCollider;
                obj.Transform.Position = pos;
                obj.Transform.Rotation = Quaternion.Euler(0.0f, rot_y, 0.0f);
                obj.Transform.Scale = scale;
                obj.MaterialColor = inner_color;
                inner = obj.SpawnObject().GetComponent<PrimitiveObjectToy>();
                obj.MaterialColor = outer_color;
                obj.Transform.Rotation = Quaternion.Euler(0.0f, rot_y + 180.0f, 0.0f);
                outer = obj.SpawnObject().GetComponent<PrimitiveObjectToy>();
            }

            public void Destroy()
            {
                NetworkServer.Destroy(inner.gameObject);
                NetworkServer.Destroy(outer.gameObject);
            }

            public void SetPos(float x, float z)
            {
                inner.transform.position = new Vector3(x, inner.transform.position.y, z);
                outer.transform.position = new Vector3(x, outer.transform.position.y, z);
            }

            public void SetScaleX(float x)
            {
                inner.transform.localScale = new Vector3(x, inner.transform.localScale.y, inner.transform.localScale.z);
                outer.transform.localScale = new Vector3(x, outer.transform.localScale.y, outer.transform.localScale.z);
            }

            public void SetSmoothing(byte smoothing)
            {
                inner.NetworkMovementSmoothing = smoothing;
                outer.NetworkMovementSmoothing = smoothing;
            }
        }

        public QuadDuo MinX = null;
        public QuadDuo MaxX = null;
        public QuadDuo MinZ = null;
        public QuadDuo MaxZ = null;

        public void Start(ArenaBounds bounds)
        {
            RoomIdentifier closest = null;
            float distance = 0.0f;
            foreach (var room in RoomIdentifier.AllRoomIdentifiers)
            {
                if (closest == null || distance > Vector3.Distance(room.transform.position, bounds.Mid))
                {
                    distance = Vector3.Distance(room.transform.position, bounds.Mid);
                    closest = room;
                }
            }
            Target = closest;

            float scale_y = (bounds.max.y - bounds.min.y) * 2.0f;
            float scale_x = bounds.max.x - bounds.min.x;
            float scale_z = bounds.max.z - bounds.min.z;

            MinX = new QuadDuo(new Vector3(bounds.min.x, bounds.Mid.y, bounds.Mid.z), 90, new Vector3(scale_z, scale_y, 1.0f), new Color(1.0f, 0.0f, 0.0f, 0.25f), new Color(0.0f, 1.0f, 0.0f, 0.5f));
            MaxX = new QuadDuo(new Vector3(bounds.max.x, bounds.Mid.y, bounds.Mid.z), -90, new Vector3(scale_z, scale_y, 1.0f), new Color(1.0f, 0.0f, 0.0f, 0.25f), new Color(0.0f, 1.0f, 0.0f, 0.5f));
            MinZ = new QuadDuo(new Vector3(bounds.Mid.x, bounds.Mid.y, bounds.min.z), 0, new Vector3(scale_x, scale_y, 1.0f), new Color(1.0f, 0.0f, 0.0f, 0.25f), new Color(0.0f, 1.0f, 0.0f, 0.5f));
            MaxZ = new QuadDuo(new Vector3(bounds.Mid.x, bounds.Mid.y, bounds.max.z), 180, new Vector3(scale_x, scale_y, 1.0f), new Color(1.0f, 0.0f, 0.0f, 0.25f), new Color(0.0f, 1.0f, 0.0f, 0.5f));
        }

        public void Update(Zone zone, ArenaBounds b)
        {
            float scale_y = (b.max.y - b.min.y) * 2.0f;
            float x = 1.0f - CurrentSize;
            MinX.SetSmoothing(10);
            MinZ.SetSmoothing(10);
            MaxX.SetSmoothing(10);
            MaxZ.SetSmoothing(10);
            if (zone != Zone.Surface)
            { 
                float lerp_x = Mathf.Lerp(b.Mid.x, Target.transform.position.x, x);
                float lerp_z = Mathf.Lerp(b.Mid.z, Target.transform.position.z, x);

                float min_x = Mathf.Lerp(b.min.x, Target.transform.position.x - EventHandler.config.MinZoneSizeFacility, x);
                MinX.SetPos(min_x, lerp_z);

                float min_z = Mathf.Lerp(b.min.z, Target.transform.position.z - EventHandler.config.MinZoneSizeFacility, x);
                MinZ.SetPos(lerp_x, min_z);

                float max_x = Mathf.Lerp(b.max.x, Target.transform.position.x + EventHandler.config.MinZoneSizeFacility, x);
                MaxX.SetPos(max_x, lerp_z);

                float max_z = Mathf.Lerp(b.max.z, Target.transform.position.z + EventHandler.config.MinZoneSizeFacility, x);
                MaxZ.SetPos(lerp_x, max_z);

                MinX.SetScaleX(max_z - min_z);
                MaxX.SetScaleX(max_z - min_z);
                MinZ.SetScaleX(max_x - min_x);
                MaxZ.SetScaleX(max_x - min_x);
            }
            else
            {
                float min_x = Mathf.Lerp(b.min.x, 45.0f - EventHandler.config.MinZoneSizeSurface, x);
                MinX.SetPos(min_x, b.Mid.z);
                float max_x = Mathf.Lerp(b.max.x, 45.0f + EventHandler.config.MinZoneSizeSurface, x);
                MaxX.SetPos(max_x, b.Mid.z);
            }
        }

        public void Reset()
        {
            if (MinX != null)
                MinX.Destroy();
            if (MaxX != null)
                MaxX.Destroy();
            if (MinZ != null)
                MinZ.Destroy();
            if (MaxZ != null)
                MaxZ.Destroy();

            Enabled = false;
            TimeLeft = EventHandler.config.ZoneShrinkDelay;
            CurrentSize = 1.0f;
            Target = null;
        }

        public bool Contains(Vector3 pos)
        {
            return (MinX.inner.transform.position.x < pos.x && pos.x < MaxX.inner.transform.position.x && MinZ.inner.transform.position.z < pos.z && pos.z < MaxZ.inner.transform.position.z);
        }
    }

    public class ArenaBounds
    {
        public Vector3 min = Vector3.zero;
        public Vector3 max = Vector3.zero;
        public Vector3 Mid { get{ return (min + max) / 2.0f; } }

        public ArenaBounds(RoomIdentifier start)
        {
            min = start.transform.position;
            max = start.transform.position;
            foreach (var coord in start.OccupiedCoords)
                min = Vector3.Min(min, new Vector3(coord.x * RoomIdentifier.GridScale.x, coord.y * RoomIdentifier.GridScale.y, coord.z * RoomIdentifier.GridScale.z));
            foreach (var coord in start.OccupiedCoords)
                max = Vector3.Max(max, new Vector3(coord.x * RoomIdentifier.GridScale.x, coord.y * RoomIdentifier.GridScale.y, coord.z * RoomIdentifier.GridScale.z));
        }

        public void Add(RoomIdentifier room)
        {
            foreach (var coord in room.OccupiedCoords)
                min = Vector3.Min(min, new Vector3(coord.x * RoomIdentifier.GridScale.x, coord.y * RoomIdentifier.GridScale.y, coord.z * RoomIdentifier.GridScale.z));
            foreach (var coord in room.OccupiedCoords)
                max = Vector3.Max(max, new Vector3(coord.x * RoomIdentifier.GridScale.x, coord.y * RoomIdentifier.GridScale.y, coord.z * RoomIdentifier.GridScale.z));
        }

        public void End()
        {
            min -= new Vector3(7.5f, 7.5f, 7.5f);
            max += new Vector3(7.5f, 7.5f, 7.5f);
        }
    }

    class ArenaManager
    {
        private static RoomIdentifier surface;
        private static RoomIdentifier entrance_a;
        private static RoomIdentifier entrance_b;
        private static RoomIdentifier heavy_a;
        private static RoomIdentifier heavy_b;
        private static RoomIdentifier light_a;
        private static RoomIdentifier light_b;

        private static Dictionary<Zone, ArenaBounds> zone_bounds = new Dictionary<Zone, ArenaBounds>
        { {Zone.Surface, null }, {Zone.Entrance, null }, {Zone.Heavy, null }, {Zone.Light, null } };

        private static Dictionary<Zone, BoundsState> bounds_state = new Dictionary<Zone, BoundsState>
        { {Zone.Surface, new BoundsState() }, {Zone.Entrance, new BoundsState() }, {Zone.Heavy, new BoundsState() }, {Zone.Light, new BoundsState() }};

        private static CoroutineHandle bounds_update;
        private static CoroutineHandle out_of_bounds_update;

        private static HashSet<Zone> OccupiedZones = new HashSet<Zone>();

        public static void Start()
        {
            surface = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone == FacilityZone.Surface).First();
            HashSet<RoomIdentifier> checkpoint_a = RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name == RoomName.HczCheckpointA || r.Name == RoomName.LczCheckpointA).ToHashSet();
            HashSet<RoomIdentifier> checkpoint_b = RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name == RoomName.HczCheckpointB || r.Name == RoomName.LczCheckpointB).ToHashSet();

            var mid_spawns = RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name == RoomName.HczCheckpointToEntranceZone);
            entrance_a = mid_spawns.Where(r => r.Zone == FacilityZone.Entrance).First();
            entrance_b = mid_spawns.Where(r => r.Zone == FacilityZone.Entrance).Last();
            heavy_a = FacilityManager.GetAdjacent(entrance_a).Keys.Where(r => r.Zone == FacilityZone.HeavyContainment).First();
            heavy_b = FacilityManager.GetAdjacent(entrance_b).Keys.Where(r => r.Zone == FacilityZone.HeavyContainment).First();

            FacilityManager.LockRoom(surface, DoorLockReason.AdminCommand);
            FacilityManager.LockJoinedRooms(new HashSet<RoomIdentifier> { entrance_a, heavy_a }, DoorLockReason.AdminCommand);
            FacilityManager.LockJoinedRooms(new HashSet<RoomIdentifier> { entrance_b, heavy_b }, DoorLockReason.AdminCommand);
            FacilityManager.LockJoinedRooms(checkpoint_a, DoorLockReason.AdminCommand);
            FacilityManager.LockJoinedRooms(checkpoint_b, DoorLockReason.AdminCommand);

            foreach (var door in ElevatorDoor.AllElevatorDoors[ElevatorManager.ElevatorGroup.Nuke])
                FacilityManager.LockDoor(door, DoorLockReason.AdminCommand);

            foreach (var door in ElevatorDoor.AllElevatorDoors[ElevatorManager.ElevatorGroup.Scp049])
                FacilityManager.LockDoor(door, DoorLockReason.AdminCommand);

            light_a = checkpoint_a.Where(r => r.Zone == FacilityZone.LightContainment).First();
            light_b = checkpoint_b.Where(r => r.Zone == FacilityZone.LightContainment).First();

            DoorDamageType ignore_all = DoorDamageType.ServerCommand | DoorDamageType.Grenade | DoorDamageType.Weapon | DoorDamageType.Scp096;
            foreach (var door in DoorVariant.AllDoors)
            {
                if (door is BreakableDoor breakable)
                {
                    breakable.IgnoredDamageSources = ignore_all;
                    //breakable._nonInteractable = true;
                }
            }

            foreach(var room in RoomIdentifier.AllRoomIdentifiers)
            {
                switch (room.Zone)
                {
                    case FacilityZone.Surface:
                        if (zone_bounds[Zone.Surface] == null)
                            zone_bounds[Zone.Surface] = new ArenaBounds(room);
                        else
                            zone_bounds[Zone.Surface].Add(room);
                        break;
                    case FacilityZone.Entrance:
                        if (zone_bounds[Zone.Entrance] == null)
                            zone_bounds[Zone.Entrance] = new ArenaBounds(room);
                        else
                            zone_bounds[Zone.Entrance].Add(room);
                        break;
                    case FacilityZone.HeavyContainment:
                        if (zone_bounds[Zone.Heavy] == null)
                            zone_bounds[Zone.Heavy] = new ArenaBounds(room);
                        else
                            zone_bounds[Zone.Heavy].Add(room);
                        break;
                    case FacilityZone.LightContainment:
                        if (zone_bounds[Zone.Light] == null)
                            zone_bounds[Zone.Light] = new ArenaBounds(room);
                        else
                            zone_bounds[Zone.Light].Add(room);
                        break;
                }
            }

            zone_bounds[Zone.Heavy].max.y = -1000.0f;

            foreach (var bound in zone_bounds)
            {
                bound.Value.End();
                PluginAPI.Core.Log.Info("Zone: " + bound.Key + " | " + bound.Value.min.ToPreciseString() + " | " + bound.Value.max.ToPreciseString() + " | " + bound.Value.Mid.ToPreciseString());
            }

            bounds_update = Timing.RunCoroutine(_BoundsUpdate());
            out_of_bounds_update = Timing.RunCoroutine(_OutOfBoundsEffect());
        }

        public static void Stop()
        {
            surface = null;
            entrance_a = null;
            entrance_b = null;
            heavy_a = null;
            heavy_b = null;
            light_a = null;
            light_b = null;

            zone_bounds[Zone.Surface] = null;
            zone_bounds[Zone.Entrance] = null;
            zone_bounds[Zone.Heavy] = null;
            zone_bounds[Zone.Light] = null;

            bounds_state[Zone.Surface].Reset();
            bounds_state[Zone.Entrance].Reset();
            bounds_state[Zone.Heavy].Reset();
            bounds_state[Zone.Light].Reset();

            Timing.KillCoroutines(bounds_update);
            Timing.KillCoroutines(out_of_bounds_update);
        }

        public static void TeleportSpawn(Player player, Zone zone, RoomSpawn spawn)
        {
            if (spawn == RoomSpawn.SpawnA)
            {
                switch (zone)
                {
                    case Zone.Surface: Teleport.RoomPos(player, surface, new Vector3(131.581f, -11.208f, 27.433f)); break;
                    case Zone.Entrance: Teleport.RoomPos(player, entrance_a, new Vector3(-5.434f, 0.965f, -0.043f)); break;
                    case Zone.Heavy: Teleport.RoomPos(player, heavy_a, new Vector3(4.231f, 0.959f, -0.016f)); break;
                    case Zone.Light: Teleport.RoomPos(player, light_a, new Vector3(15.152f, 0.960f, -0.011f)); break;
                }
            }
            else if (spawn == RoomSpawn.SpawnB)
            {
                switch (zone)
                {
                    case Zone.Surface: Teleport.RoomPos(player, surface, new Vector3(-9.543f, 0.960f, 0.410f)); break;
                    case Zone.Entrance: Teleport.RoomPos(player, entrance_b, new Vector3(-5.434f, 0.965f, -0.043f)); break;
                    case Zone.Heavy: Teleport.RoomPos(player, heavy_b, new Vector3(4.231f, 0.959f, -0.016f)); break;
                    case Zone.Light: Teleport.RoomPos(player, light_b, new Vector3(15.152f, 0.960f, -0.011f)); break;
                }
            }
        }

        public static void Reset(Zone zone)
        {
            FacilityZone facility_zone = FacilityZone.None;
            switch(zone)
            {
                case Zone.Surface: facility_zone = FacilityZone.Surface; break;
                case Zone.Entrance: facility_zone = FacilityZone.Entrance; break;
                case Zone.Heavy: facility_zone = FacilityZone.HeavyContainment; break;
                case Zone.Light: facility_zone = FacilityZone.LightContainment; break;
            }

            foreach(var door in DoorVariant.AllDoors)
            {
                if (door.Rooms.IsEmpty() || door.Rooms.First().Zone != facility_zone)
                    continue;

                door.NetworkTargetState = false;
            }

            foreach(var pickup in Object.FindObjectsOfType<ItemPickupBase>())
            {
                if (Mathf.Abs(pickup.Position.y - 500.0f) < 50.0f)
                    continue;

                RoomIdentifier room = RoomIdUtils.RoomAtPosition(pickup.transform.position);
                if (room != null && room.Zone != facility_zone)
                    continue;

                NetworkServer.Destroy(pickup.gameObject);
            }

            foreach(var ragdoll in Object.FindObjectsOfType<BasicRagdoll>())
            {
                RoomIdentifier room = RoomIdUtils.RoomAtPosition(ragdoll.transform.position);
                if (room != null && room.Zone != facility_zone)
                    continue;

                NetworkServer.Destroy(ragdoll.gameObject);
            }

            bounds_state[zone].Reset();
        }

        public static void OccupyZone(Zone zone)
        {
            OccupiedZones.Add(zone);
        }

        public static void StartRound(Zone zone)
        {
            bounds_state[zone].Enabled = true;
            bounds_state[zone].Start(zone_bounds[zone]);
        }

        public static void FreeZone(Zone zone)
        {
            OccupiedZones.Remove(zone);
        }

        public static List<Zone> AvailableZones()
        {
            HashSet<Zone> zones = new HashSet<Zone>() { Zone.Surface, Zone.Entrance, Zone.Heavy, Zone.Light };
            return zones.Except(OccupiedZones).ToList();
        }

        private static IEnumerator<float> _BoundsUpdate()
        {
            while(true)
            {
                try
                {
                    foreach (var state in bounds_state)
                    {
                        if (!state.Value.Enabled)
                            continue;
                        if (state.Value.TimeLeft > 0.0f)
                            state.Value.TimeLeft -= 0.1f;
                        else
                        {
                            state.Value.CurrentSize -= EventHandler.config.ZoneSizeShrinkRate * (1.0f / 600.0f);
                            state.Value.Update(state.Key, zone_bounds[state.Key]);
                        }
                    }
                }
                catch(System.Exception ex)
                {
                    PluginAPI.Core.Log.Error(ex.ToString());
                }
                yield return Timing.WaitForSeconds(0.1f);
            }
        }

        public static IEnumerator<float> _OutOfBoundsEffect()
        {
            while(true)
            {
                try
                {
                    foreach(var p in ReadyPlayers())
                    {
                        if (!p.IsAlive || p.Role == PlayerRoles.RoleTypeId.Tutorial)
                        {
                            p.EffectsManager.ChangeState<Poisoned>(0, 0);
                            continue;
                        }

                        if(p.Room != null)
                        {
                            switch(p.Room.Zone)
                            {
                                case FacilityZone.Surface:
                                    if (bounds_state[Zone.Surface].Contains(p.Position))
                                    {
                                        p.EffectsManager.ChangeState<Poisoned>(0, 0);
                                        continue;
                                    }
                                    break;
                                case FacilityZone.Entrance:
                                    if (bounds_state[Zone.Entrance].Contains(p.Position))
                                    {
                                        p.EffectsManager.ChangeState<Poisoned>(0, 0);
                                        continue;
                                    }
                                    break;
                                case FacilityZone.HeavyContainment:
                                    if (bounds_state[Zone.Heavy].Contains(p.Position))
                                    {
                                        p.EffectsManager.ChangeState<Poisoned>(0, 0);
                                        continue;
                                    }
                                    break;
                                case FacilityZone.LightContainment:
                                    if (bounds_state[Zone.Light].Contains(p.Position))
                                    {
                                        p.EffectsManager.ChangeState<Poisoned>(0, 0);
                                        continue;
                                    }
                                    break;
                            }
                        }

                        p.EffectsManager.ChangeState<Poisoned>(1, 0);
                        p.SendBroadcast("<b><color=#FF0000>You are out of bounds!</b></color>", 2, shouldClearPrevious: true);
                    }
                }
                catch(System.Exception ex)
                {
                    PluginAPI.Core.Log.Error(ex.ToString());
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }
    }
}
