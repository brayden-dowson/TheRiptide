using AdminToys;
using CustomPlayerEffects;
using HarmonyLib;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using MEC;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using slocLoader;
using slocLoader.Objects;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public class TheSkeld : IMap
    {
        public static TheSkeld Singleton { get; private set; }

        public string Name => "The Skeld";
        public string Author => "The Riptide. Detailing by zInitial";
        public string Description => "From the hit game amugers";
        public float RoundTime => 7.0f;
        public Vector3 ReadyUpBodyPosition => map_offset + (Vector3.up * 2.0f);

        private Vector3 map_offset = new Vector3(0.0f, 500.0f, 0.0f);
        public HashSet<LightSourceToy> lights = new HashSet<LightSourceToy>();

        public Dictionary<DoorVariant, DoorSkin> doors = new Dictionary<DoorVariant, DoorSkin>();
        private Dictionary<DoorVariant, VentSkin> vents = new Dictionary<DoorVariant, VentSkin>();
        private Dictionary<DoorVariant, LeverSkin> levers = new Dictionary<DoorVariant, LeverSkin>();
        private List<GameObject> ready_up_blockers = new List<GameObject>();

        private float spawn_rot = 0.0f;
        private float spawn_dist = 5.0f;

        private Harmony harmony;

        private static readonly List<Vector3> vents_offsets = new List<Vector3>
        {
            new Vector3(10.5f, 0.0f, -3.25f),
            new Vector3(31.25f, 0.0f, -0.75f),
            new Vector3(16.75f, 0.0f, -17.0f),
            new Vector3(46.75f, 0.0f, -16.5f),
            new Vector3(32.75f, 0.0f, -40.75f),
            new Vector3(12.0f, 0.0f, -44.5f),
            new Vector3(6.0f, 0.0f, -53.5f),
            new Vector3(12.0f, 0.0f, -29.5f),
            new Vector3(-18.5f, 0.0f, -27.0f),
            new Vector3(-33.5f, 0.0f, -35.5f),
            new Vector3(-49.0f, 0.0f, -34.0f),
            new Vector3(-49.0f, 0.0f, -13.0f),
            new Vector3(-26.0f, 0.0f, -30.0f),
            new Vector3(-33.5f, 0.0f, -11.5f),
            new Vector3(-20.5f, 0.0f, -17.0f),
        };
        private static readonly List<Vector3> door_offsets = new List<Vector3>
        {
            new Vector3(-17.5f, 0.0f, -7.5f),
            new Vector3(-12.0f, 0.0f, 0.0f),
            new Vector3(12.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 0.0f, -12.0f),
            new Vector3(32.75f, 0.0f, -11.0f),
            new Vector3(26.5f, 0.0f, -5.0f),
            new Vector3(30.25f, 0.0f, -13.5f),
            new Vector3(40.25f, 0.0f, -21.0f),
            new Vector3(26.5f, 0.0f, -37.5f),
            new Vector3(32.75f, 0.0f, -31.5f),
            new Vector3(21.0f, 0.0f, -40.0f),
            new Vector3(7.5f, 0.0f, -37.5f),
            new Vector3(5.0f, 0.0f, -31.0f),
            new Vector3(-8.0f, 0.0f, -47.0f),
            new Vector3(-17.5f, 0.0f, -44.5f),
            new Vector3(-38.0f, 0.0f, -34.0f),
            new Vector3(-32.0f, 0.0f, -42.0f),
            new Vector3(-38.0f, 0.0f, -13.0f),
            new Vector3(-43.5f, 0.0f, -23.5f),
            new Vector3(-32.5f, 0.0f, -23.5f),
            new Vector3(-32.0f, 0.0f, -5.0f),
            new Vector3(7.5f, 0.0f, -21.5f),
        };
        private static readonly List<bool> door_rotation = new List<bool>
        {
            false,
            true,
            true,
            false,
            false,
            true,
            true,
            true,
            true,
            false,
            false,
            true,
            false,
            true,
            false,
            false,
            true,
            false,
            true,
            true,
            true,
            true,
        };
        private static readonly List<System.Tuple<Vector3, Quaternion, ISabotage>> lever_transforms = new List<System.Tuple<Vector3, Quaternion, ISabotage>>()
        {
            new System.Tuple<Vector3, Quaternion, ISabotage>(new Vector3(17.0f, 1.25f, -12.75f), Quaternion.Euler(0.0f, -45.0f, 0.0f), new OxygenSabotage()),
            new System.Tuple<Vector3, Quaternion, ISabotage>(new Vector3(-16.875f,1.25f,-26.25f), Quaternion.Euler(0.0f, 0.0f, 0.0f), new LightsSabotage()),
            new System.Tuple<Vector3, Quaternion, ISabotage>(new Vector3(-31.3125f,1.25f,-17.0625f), Quaternion.Euler(0.0f, -45.0f, 0.0f), new SurveillanceSabotage()),
            new System.Tuple<Vector3, Quaternion, ISabotage>(new Vector3(-41.75f,1.25f,-11.5f), Quaternion.Euler(0.0f, -90.0f, 0.0f), new GeneratorSabotage()),
            new System.Tuple<Vector3, Quaternion, ISabotage>(new Vector3(-41.75f,1.25f,-36.5f), Quaternion.Euler(0.0f, -90.0f, 0.0f), new GeneratorSabotage()),
            new System.Tuple<Vector3, Quaternion, ISabotage>(new Vector3(27.7346249f,1.25f,-42.5f), Quaternion.Euler(0.0f, -90.0f, 0.0f), new ShieldSabotage()),
            new System.Tuple<Vector3, Quaternion, ISabotage>(new Vector3(12.0f,1.25f,-51.75f), Quaternion.Euler(0.0f, -135.0f, 0.0f), new CommunicationSabotage()),
            new System.Tuple<Vector3, Quaternion, ISabotage>(new Vector3(24.0f,1.25f,-28.75f), Quaternion.Euler(0.0f, 135.0f, 0.0f), new DoorSabotage()),
        };

        public bool Load(object plugin)
        {
            Singleton = this;
            List<slocGameObject> objects;
            if (slocLoader.AutoObjectLoader.AutomaticObjectLoader.TryGetObjects("The Riptide's Skeld", out objects))
            {
                GameObject root = API.SpawnObjects(objects, map_offset, Quaternion.Euler(Vector3.zero));
                foreach (var lst in root.GetComponentsInChildren<LightSourceToy>())
                    lights.Add(lst);
            }
            else
                return false;

            LightsController.Singleton.Start();
            var table_go = new GameObject();
            table_go.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
            table_go.transform.position = new Vector3(17.6074963f, 0.8125f, -25.4520988f);
            SurveillanceController.Singleton.Start(table_go, map_offset);

            AddVents();
            AddDoors();
            AddLevers();
            AddBlockers();

            harmony = new Harmony("TheRiptide.TheSkeld");
            harmony.PatchAll();

            EventManager.RegisterEvents(plugin, this);
            return true;
        }

        public void Unload(object plugin)
        {
            lights.Clear();
            doors.Clear();
            vents.Clear();
            levers.Clear();
            ready_up_blockers.Clear();
            SurveillanceController.Singleton.Stop();
            harmony.UnpatchAll("TheRiptide.TheSkeld");
            harmony = null;
            EventManager.UnregisterEvents(plugin, this);
        }

        public void OnReadyUpStart()
        {
            ResetSpawns();
            foreach (var lever in levers.Values)
                lever.ForceDisable();
        }

        public void OnReadyUpEnd()
        {
            foreach (var door in doors.Values)
            {
                door.door_base.NetworkTargetState = false;
                door.door_base.ServerChangeLock(DoorLockReason.AdminCommand, true);
            }
        }

        public void OnRoundStart()
        {
            SurveillanceController.Singleton.Reset();
            ResetSpawns();
            foreach (var door in doors.Values)
                door.door_base.UnlockLater(0.0f, DoorLockReason.AdminCommand);
        }

        public void OnRoundEnd(WinningRole winner)
        {
        }

        public bool InnocentsMetWinCondition()
        {
            return 
                OxygenController.Singleton.State == true &&
                (!GeneratorSabotage.Instances[0].IsDeactivated || !GeneratorSabotage.Instances[0].IsDeactivated) &&
                ShieldController.Singleton.State == true;
        }

        public void OnPlayerSpawn(Player player)
        {
            player.Position = map_offset - (Vector3.down * 99.0f);

            Timing.CallDelayed(0.5f, () =>
            {
                player.ReferenceHub.TryOverridePosition(
                    Vector3.up + map_offset + (Quaternion.Euler(0.0f, spawn_rot, 0.0f) * Vector3.forward * spawn_dist),
                    new Vector3(0.0f, player.GameObject.transform.rotation.eulerAngles.y - (180.0f - spawn_rot), 0.0f));
                spawn_rot += 34.0f / (spawn_dist / 3.5f);
                spawn_dist += 34.0f / 360.0f;
                player.EffectsManager.EnableEffect<InsufficientLighting>();
            });
        }

        public void OnPlayerReady(Player player)
        {
            foreach (var b in ready_up_blockers)
                player.Connection.Send(new ObjectDestroyMessage { netId = b.GetComponent<NetworkIdentity>().netId });
        }

        [PluginEvent(ServerEventType.PlayerInteractDoor)]
        public bool OnPlayerInteractDoor(Player player, DoorVariant door, bool canOpen)
        {
            if (vents.ContainsKey(door))
            {
                if (((!TraitorAmongUs.not_ready.Contains(player.PlayerId) && TraitorAmongUs.is_ready_up) || TraitorAmongUs.traitors.Contains(player.PlayerId) || TraitorAmongUs.jesters.Contains(player.PlayerId)))
                    vents[door].OnPlayerUse(player);
                return false;
            }
            else if (levers.ContainsKey(door))
            {
                var lever = levers[door];
                if (lever.State)
                {
                    if (!lever.TryDisable(player))
                        door.LockBypassDenied(player.ReferenceHub, 0);
                }
                else if (TraitorAmongUs.traitors.Contains(player.PlayerId))
                {
                    if (!lever.TryEnable(player))
                        door.LockBypassDenied(player.ReferenceHub, 0);
                }
                return false;
            }
            return true;
        }

        private void ResetSpawns()
        {
            spawn_rot = 0.0f;
            spawn_dist = 3.5f;
        }

        private void AddVents()
        {
            foreach (var offset in vents_offsets)
            {
                BreakableDoor door = SpawnDoor(map_offset + offset - new Vector3(0.0f, 0.125f, 0.5f), Quaternion.Euler(90.0f, 0.0f, 0.0f), new Vector3(1.0f / 1.3f, 1.0f / 2.35f, 0.5f), "HCZ");
                vents.Add(door, door.gameObject.AddComponent<VentSkin>());
            }
        }

        private void AddDoors()
        {
            for (int i = 0; i < door_offsets.Count; i++)
            {
                BreakableDoor door = SpawnDoor(map_offset + door_offsets[i], Quaternion.Euler(0.0f, door_rotation[i] ? 90.0f : 0.0f, 0.0f), new Vector3(3.0f / 1.3f, 3.0f / 2.35f, 1.0f), "LCZ");
                doors.Add(door, door.gameObject.AddComponent<DoorSkin>());
            }
        }

        private void AddLevers()
        {
            for (int i = 0; i < lever_transforms.Count; i++)
            {
                BreakableDoor door = SpawnDoor(map_offset + lever_transforms[i].Item1 + new Vector3(0.0f, -0.25f, 0.0f), lever_transforms[i].Item2, new Vector3(0.5f / 1.3f, 0.5f / 2.35f, 4.0f), "HCZ");
                LeverSkin skin = door.gameObject.AddComponent<LeverSkin>();
                skin.sabotage = lever_transforms[i].Item3;
                levers.Add(door, skin);
            }
        }

        private BreakableDoor SpawnDoor(Vector3 pos, Quaternion rot, Vector3 scale, string name)
        {
            DoorDamageType ignore_all = DoorDamageType.ServerCommand | DoorDamageType.Grenade | DoorDamageType.Weapon | DoorDamageType.Scp096;
            MapGeneration.DoorSpawnpoint pf = Object.FindObjectsOfType<MapGeneration.DoorSpawnpoint>().First(x => x.TargetPrefab.name.Contains(name));
            BreakableDoor door = Object.Instantiate(pf.TargetPrefab.gameObject, pos, rot).GetComponent<BreakableDoor>();
            door.transform.localScale = scale;
            door.IgnoredDamageSources = ignore_all;
            door.NetworkTargetState = false;
            NetworkServer.Spawn(door.gameObject);
            return door;
        }

        private void AddBlockers()
        {
            ready_up_blockers = new List<GameObject>();
            PrimitiveObject b0 = new PrimitiveObject(ObjectType.Cube);
            b0.ColliderMode = PrimitiveObject.ColliderCreationMode.ClientOnly;
            b0.Transform.Position = map_offset + new Vector3(12.0f, 1.5f, 0.0f);
            b0.Transform.Rotation = Quaternion.Euler(new Vector3(0.0f, 90.0f, 0.0f));
            b0.Transform.Scale = new Vector3(3.5f, 3.0f, 0.5f);
            b0.MaterialColor = new Color(1.0f, 0.0f, 0.0f);
            ready_up_blockers.Add(b0.SpawnObject());

            PrimitiveObject b1 = new PrimitiveObject(ObjectType.Cube);
            b1.ColliderMode = PrimitiveObject.ColliderCreationMode.ClientOnly;
            b1.Transform.Position = map_offset + new Vector3(-12.0f, 1.5f, 0.0f);
            b1.Transform.Rotation = Quaternion.Euler(new Vector3(0.0f, 90.0f, 0.0f));
            b1.Transform.Scale = new Vector3(3.5f, 3.0f, 0.5f);
            b1.MaterialColor = new Color(1.0f, 0.0f, 0.0f);
            ready_up_blockers.Add(b1.SpawnObject());

            PrimitiveObject b2 = new PrimitiveObject(ObjectType.Cube);
            b2.ColliderMode = PrimitiveObject.ColliderCreationMode.ClientOnly;
            b2.Transform.Position = map_offset + new Vector3(0.0f, 1.5f, -12.0f);
            b2.Transform.Rotation = Quaternion.Euler(new Vector3(0.0f, 0.0f, 0.0f));
            b2.Transform.Scale = new Vector3(3.5f, 3.0f, 0.5f);
            b2.MaterialColor = new Color(1.0f, 0.0f, 0.0f);
            ready_up_blockers.Add(b2.SpawnObject());

            Timing.CallDelayed(1.0f, () =>
            {
                foreach (var go in ready_up_blockers)
                    Object.Destroy(go.GetComponentInChildren<Collider>());
            });
        }
    }
}
