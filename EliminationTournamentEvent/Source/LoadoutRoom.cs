using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables.Scp330;
using InventorySystem.Items.Firearms.Attachments;
using MapGeneration;
using MEC;
using Mirror;
using slocLoader.Objects;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using PluginAPI.Core;

namespace TheRiptide
{
    public class LoadoutRoom
    {
        public class RoomPairSpawn
        {
            public Vector3 team_a;
            public Vector3 team_b;
        }

        public class RoomPair
        {
            public LoadoutRoom team_a;
            public LoadoutRoom team_b;
            public int index;
        }

        //    1   2
        //  3   f   4
        //    4   3
        //  1   f   2
        //    e   e
        private static List<RoomPairSpawn> loadout_room_pairs = new List<RoomPairSpawn>()
        {
            new RoomPairSpawn{ team_a = new Vector3(-16.0f, 500.0f, 0.0f), team_b = new Vector3(-8.0f, 500.0f, 24.0f) },
            new RoomPairSpawn{ team_a = new Vector3(16.0f, 500.0f, 0.0f), team_b = new Vector3(8.0f, 500.0f, 24.0f) },
            new RoomPairSpawn{ team_a = new Vector3(-16.0f, 500.0f, 16.0f), team_b = new Vector3(8.0f, 500.0f, 8.0f) },
            new RoomPairSpawn{ team_a = new Vector3(16.0f, 500.0f, 16.0f), team_b = new Vector3(-8.0f, 500.0f, 8.0f) },
        };

        private static RoomPairSpawn final_room_pair = new RoomPairSpawn { team_a = new Vector3(0.0f, 500.0f, 0.0f), team_b = new Vector3(0.0f, 500.0f, 16.0f) };
        private static RoomPairSpawn extra_room_pair = new RoomPairSpawn { team_a = new Vector3(-8.0f, 500.0f, -8.0f), team_b = new Vector3(8.0f, 500.0f, -8.0f) };
        private static RoomPairSpawn extra_room_pair2 = new RoomPairSpawn { team_a = new Vector3(0.0f, 500.0f, -16.0f), team_b = new Vector3(0.0f, 500.0f, 32.0f) };

        private static LoadoutRoom[] loadout_rooms;
        private static HashSet<int> in_use = new HashSet<int>();

        private static List<slocGameObject> loadout_room = null;
        private static Dictionary<DoorVariant, System.Action<Player>> buttons = new Dictionary<DoorVariant, System.Action<Player>>();

        private Vector3 offset;
        private GameObject sloc_object;
        private BreakableDoor door;
        private List<ItemPickupBase> items = new List<ItemPickupBase>();
        private WorkstationController work_station;

        private LoadoutRoom(Vector3 offset)
        {
            this.offset = offset;
            sloc_object = slocLoader.API.SpawnObjects(loadout_room, offset, Quaternion.identity);
            door = SpawnDoor(offset + new Vector3(3.125f, 1.125f, -0.375f), Quaternion.Euler(90.0f, 0.0f, 0.0f), new Vector3(0.5f / 1.3f, 0.75f / 2.35f, 1.0f), "LCZ");
        }

        private void Destroy()
        {
            NetworkServer.Destroy(sloc_object);
            NetworkServer.Destroy(door.gameObject);
        }

        public static void Start()
        {
            if (loadout_room == null && !slocLoader.AutoObjectLoader.AutomaticObjectLoader.TryGetObjects("LoadoutRoom", out loadout_room))
                PluginAPI.Core.Log.Error("could not load LoadoutRoom.sloc make sure you add it too slocLoader/Objects");

            loadout_rooms = new LoadoutRoom[14];
            loadout_rooms[0] = new LoadoutRoom(loadout_room_pairs[0].team_a);
            loadout_rooms[1] = new LoadoutRoom(loadout_room_pairs[0].team_b);
            loadout_rooms[2] = new LoadoutRoom(loadout_room_pairs[1].team_a);
            loadout_rooms[3] = new LoadoutRoom(loadout_room_pairs[1].team_b);
            loadout_rooms[4] = new LoadoutRoom(loadout_room_pairs[2].team_a);
            loadout_rooms[5] = new LoadoutRoom(loadout_room_pairs[2].team_b);
            loadout_rooms[6] = new LoadoutRoom(loadout_room_pairs[3].team_a);
            loadout_rooms[7] = new LoadoutRoom(loadout_room_pairs[3].team_b);
            loadout_rooms[8] = new LoadoutRoom(final_room_pair.team_a);
            loadout_rooms[9] = new LoadoutRoom(final_room_pair.team_b);
            loadout_rooms[10] = new LoadoutRoom(extra_room_pair.team_a);
            loadout_rooms[11] = new LoadoutRoom(extra_room_pair.team_b);
            loadout_rooms[12] = new LoadoutRoom(extra_room_pair2.team_a);
            loadout_rooms[13] = new LoadoutRoom(extra_room_pair2.team_b);
        }

        public static void Stop()
        {
            foreach (var room in loadout_rooms)
                room.Destroy();
        }

        public static RoomPair BorrowLoadoutRoomPair()
        {
            List<int> avaliable = new HashSet<int> { 0, 1, 2, 3 }.Except(in_use).ToList();
            if(avaliable.IsEmpty())
            {
                PluginAPI.Core.Log.Error("no rooms avaliable");
                return null;
            }
            int selected = avaliable.RandomItem();
            in_use.Add(selected);
            return new RoomPair { team_a = loadout_rooms[selected * 2], team_b = loadout_rooms[selected * 2 + 1], index = selected };
        }

        public static void ReturnRoomPair(RoomPair pair)
        {
            in_use.Remove(pair.index);
            pair.index = -1;
            pair.team_a = null;
            pair.team_b = null;
        }


        //public LoadoutRoom(HashSet<ItemType> item_bans, HashSet<CandyKindID> candy_bans, System.Action<Player> ready_callback)
        //{
        //    offset = RetrieveSpawnPoint();
        //    this.item_bans = item_bans;
        //    this.candy_bans = candy_bans;
        //    this.ready_callback = ready_callback;
        //}

        //private static Vector3 RetrieveSpawnPoint()
        //{
        //    if (loadout_room_spawns.IsEmpty())
        //    {
        //        float offset_x = (failsafe_z_iterator % 2 == 1) ? 8.0f : 0.0f;
        //        Vector3 pos = new Vector3(failsafe_x_iterator * 16.0f + offset_x, 500.0f, failsafe_z_iterator * 8.0f);
        //        failsafe_z_iterator++;
        //        if(failsafe_z_iterator >= 3)
        //        {
        //            failsafe_x_iterator++;
        //            failsafe_z_iterator = 0;
        //        }
        //        PluginAPI.Core.Log.Info("failsafe: " + pos.ToPreciseString());
        //        return pos;
        //    }
        //    else
        //    {
        //        Vector3 pos = loadout_room_spawns.PullRandomItem();
        //        PluginAPI.Core.Log.Info("list: " + pos.ToPreciseString());
        //        return pos;
        //    }
        //}

        //public void ReturnSpawnPoint()
        //{
        //    loadout_room_spawns.Add(offset);
        //}

        //public static void LoadSlocObjects()
        //{
        //    if (loadout_room == null && !slocLoader.AutoObjectLoader.AutomaticObjectLoader.TryGetObjects("LoadoutRoom", out loadout_room))
        //        PluginAPI.Core.Log.Error("could not load LoadoutRoom.sloc make sure you add it too slocLoader/Objects");
        //}

        public void Spawn(HashSet<ItemType> item_bans, HashSet<CandyKindID> candy_bans, System.Action<Player> ready_callback)
        {
            //sloc_object = slocLoader.API.SpawnObjects(loadout_room, offset, Quaternion.identity);

            //door = SpawnDoor(offset + new Vector3(3.125f, 1.125f, -0.375f), Quaternion.Euler(90.0f, 0.0f, 0.0f), new Vector3(0.5f / 1.3f, 0.75f / 2.35f, 1.0f), "LCZ");
            buttons.Add(door, ready_callback);
            Timing.RunCoroutine(_SpawnWeapons(item_bans));
            Timing.RunCoroutine(_SpawnMedicals(item_bans));
            Timing.RunCoroutine(_SpawnCandies(candy_bans));
            Timing.RunCoroutine(_SpawnScps(item_bans));
            Timing.RunCoroutine(_SpawnOthers(item_bans));
            Timing.RunCoroutine(_SpawnKeycards());

            work_station = SpawnWorkstation(offset + new Vector3(-3.5f, 0.0f, 0.0f), Quaternion.Euler(0.0f, 90.0f, 0.0f));
            if (work_station != null)
            {
                work_station.NetworkStatus = (byte)WorkstationController.WorkstationStatus.PoweringUp;
                work_station._serverStopwatch.Restart();
            }
        }

        public void Unspawn()
        {
            //NetworkServer.Destroy(sloc_object);
            foreach (var item in items)
            {
                try
                {
                    NetworkServer.Destroy(item.gameObject);
                }
                catch (System.Exception ex)
                {
                    PluginAPI.Core.Log.Error("item: " + ex.ToString());
                }
            }
            items.Clear();
            try { NetworkServer.Destroy(work_station.gameObject); }
            catch (System.Exception ex) { PluginAPI.Core.Log.Error("work station: " + ex.ToString()); }
            buttons.Remove(door);
            //try { NetworkServer.Destroy(door.gameObject); }
            //catch (System.Exception ex) { PluginAPI.Core.Log.Error("door: " + ex.ToString()); }
        }

        public void TeleportPlayer(Player player)
        {
            player.Position = offset + Vector3.up;
        }

        public static bool ButtonPressed(Player player, DoorVariant door)
        {
            if(buttons.ContainsKey(door))
            {
                buttons[door](player);
                return true;
            }
            return false;
        }

        private IEnumerator<float> _SpawnWeapons(HashSet<ItemType> item_bans)
        {
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(1.5f, 1.0f, -2.75f), Quaternion.Euler(0.0f, 0.0f, 90.0f), ItemType.GunCOM15);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(1.5f, 1.25f, -3.125f), Quaternion.Euler(0.0f, 0.0f, 90.0f), ItemType.GunCOM18);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(1.5f, 1.0f, -3.5f), Quaternion.Euler(0.0f, 0.0f, 90.0f), ItemType.GunCom45);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(1.125f, 1.0f, -3.375f), Quaternion.Euler(0.0f, 0.0f, 90.0f), ItemType.GunFSP9);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(1.125f, 1.0f, -2.875f), Quaternion.Euler(0.0f, 0.0f, 90.0f), ItemType.GunCrossvec);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(0.875f, 1.0f, -3.125f), Quaternion.Euler(0.0f, 0.0f, 90.0f), ItemType.GunE11SR);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(0.625f, 1.0f, -3.125f), Quaternion.identity, ItemType.GunFRMG0); // Quaternion.identity
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(0.375f, 1.0f, -3.125f), Quaternion.identity, ItemType.GunA7); // Quaternion.identity
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(0.125f, 1.25f, -3.125f), Quaternion.identity, ItemType.GunAK); // Quaternion.identity
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(-0.125f, 1.25f, -3.125f), Quaternion.Euler(0.0f, 0.0f, 90.0f), ItemType.GunShotgun); // Quaternion.Euler(0.0f, 0.0f, 90.0f)
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(-0.375f, 1.0f, -3.125f), Quaternion.Euler(0.0f, 0.0f, 90.0f), ItemType.GunRevolver); // Quaternion.Euler(0.0f, 0.0f, 90.0f)
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(-0.625f, 1.25f, -3.125f), Quaternion.Euler(0.0f, 90.0f, 0.0f), ItemType.GunLogicer); // Quaternion.Euler(0.0f, 90.0f, 0.0f)
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(-0.9375f, 1.25f, -3.125f), Quaternion.Euler(-90.0f, 0.0f, 0.0f), ItemType.MicroHID); // Quaternion.Euler(-90.0f, 0.0f, 0.0f)
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(-1.25f, 1.0f, -3.125f), Quaternion.Euler(0.0f, 0.0f, 90.0f), ItemType.ParticleDisruptor); // Quaternion.Euler(0.0f, 0.0f, 90.0f)
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(-1.5f, 1.0f, -3.125f), Quaternion.Euler(0.0f, 180.0f, 90.0f), ItemType.Jailbird); // Quaternion.Euler(0.0f, 180.0f, 90.0f)
        }

        private IEnumerator<float> _SpawnMedicals(HashSet<ItemType> item_bans)
        {
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(1.125f, 1.0f, 2.75f), Quaternion.identity, ItemType.Medkit);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(1.125f, 1.0f, 3.125f), Quaternion.identity, ItemType.Painkillers);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(1.125f, 1.0f, 3.5f), Quaternion.identity, ItemType.Adrenaline);
        }

        private IEnumerator<float> _SpawnCandies(HashSet<CandyKindID> candy_bans)
        {
            yield return Timing.WaitForSeconds(0.1f);
            SpawnCandy(candy_bans, offset + new Vector3(0.75f, 1.0f, 2.8125f), Quaternion.identity, CandyKindID.Blue);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnCandy(candy_bans, offset + new Vector3(0.75f, 1.0f, 2.9375f), Quaternion.identity, CandyKindID.Green);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnCandy(candy_bans, offset + new Vector3(0.75f, 1.0f, 3.0625f), Quaternion.identity, CandyKindID.Purple);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnCandy(candy_bans, offset + new Vector3(0.75f, 1.0f, 3.1875f), Quaternion.identity, CandyKindID.Rainbow);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnCandy(candy_bans, offset + new Vector3(0.75f, 1.0f, 3.3125f), Quaternion.identity, CandyKindID.Red);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnCandy(candy_bans, offset + new Vector3(0.75f, 1.0f, 3.4375f), Quaternion.identity, CandyKindID.Yellow);
        }

        private IEnumerator<float> _SpawnScps(HashSet<ItemType> item_bans)
        {
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(0.375f, 1.0f, 2.75f), Quaternion.identity, ItemType.SCP018);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(0.375f, 1.0f, 3.125f), Quaternion.identity, ItemType.SCP207);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(0.375f, 1.0f, 3.5f), Quaternion.identity, ItemType.AntiSCP207);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(0.0f, 1.0f, 2.75f), Quaternion.identity, ItemType.SCP244a);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(0.0f, 1.0f, 3.125f), Quaternion.identity, ItemType.SCP244b);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(0.0f, 1.0f, 3.5f), Quaternion.Euler(-90.0f, 0.0f, 0.0f), ItemType.SCP268);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(-0.375f, 1.0f, 2.75f), Quaternion.identity, ItemType.SCP500);
            yield return Timing.WaitForSeconds(0.1f);
            //SpawnItem(item_bans, offset + new Vector3(-0.375f, 1.0f, 3.125f), Quaternion.Euler(0.0f, 180.0f, 0.0f), ItemType.SCP1576);
            //yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(-0.375f, 1.0f, 3.5f), Quaternion.identity, ItemType.SCP1853);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(-0.75f, 1.0f, 2.75f), Quaternion.identity, ItemType.SCP2176);
        }

        private IEnumerator<float> _SpawnOthers(HashSet<ItemType> item_bans)
        {
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(-0.75f, 1.0f, 3.125f), Quaternion.Euler(0.0f, 180.0f, 0.0f), ItemType.Flashlight);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(-0.75f, 1.0f, 3.5f), Quaternion.Euler(0.0f, 90.0f, 0.0f), ItemType.GrenadeHE);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(item_bans, offset + new Vector3(-1.125f, 1.0f, 3.125f), Quaternion.Euler(0.0f, 180.0f, 0.0f), ItemType.GrenadeFlash);
        }

        private IEnumerator<float> _SpawnKeycards()
        {
            HashSet<ItemType> none = new HashSet<ItemType>();
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(none, offset + new Vector3(-2.5f, 1.0f, -1.0f), Quaternion.identity, ItemType.KeycardJanitor);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(none, offset + new Vector3(-2.5f, 1.0f, -0.5f), Quaternion.identity, ItemType.KeycardGuard);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(none, offset + new Vector3(-2.5f, 1.0f, 0.0f), Quaternion.identity, ItemType.KeycardMTFPrivate);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(none, offset + new Vector3(-2.5f, 1.0f, 0.5f), Quaternion.identity, ItemType.KeycardMTFOperative);
            yield return Timing.WaitForSeconds(0.1f);
            SpawnItem(none, offset + new Vector3(-2.5f, 1.0f, 1.0f), Quaternion.identity, ItemType.KeycardMTFCaptain);
        }

        private ItemPickupBase SpawnItem(HashSet<ItemType> item_bans, Vector3 pos, Quaternion rot, ItemType type)
        {
            if (item_bans.Contains(type))
                return null;
 
            ItemBase item;
            if (InventoryItemLoader.TryGetItem(type, out item))
            {
                ItemPickupBase pickup = Object.Instantiate(item.PickupDropModel, pos, rot);
                if (pickup != null)
                {
                    pickup.NetworkInfo = new PickupSyncInfo(type, 1.0f);
                    NetworkServer.Spawn(pickup.gameObject);
                    items.Add(pickup);
                    return pickup;
                }
                else
                    PluginAPI.Core.Log.Error("PickupDropModel for " + type.ToString() + " was null");
            }
            else
                PluginAPI.Core.Log.Error("could not load item of type " + type.ToString());
            return null;
        }


        private Scp330Pickup SpawnCandy(HashSet<CandyKindID> candy_bans, Vector3 pos, Quaternion rot, CandyKindID type)
        {
            if (candy_bans.Contains(type))
                return null;

            Scp330Pickup pickup = SpawnItem(new HashSet<ItemType> { }, pos, rot, ItemType.SCP330) as Scp330Pickup;
            if (pickup == null)
            {
                PluginAPI.Core.Log.Error("null pickup when spawning candy");
                return null;
            }

            pickup.StoredCandies.Add(type);
            pickup.NetworkExposedCandy = type;
            return pickup;
        }

        private BreakableDoor SpawnDoor(Vector3 pos, Quaternion rot, Vector3 scale, string name)
        {
            DoorDamageType ignore_all = DoorDamageType.ServerCommand | DoorDamageType.Grenade | DoorDamageType.Weapon | DoorDamageType.Scp096;
            DoorSpawnpoint pf = Object.FindObjectsOfType<DoorSpawnpoint>().First(x => x.TargetPrefab.name.Contains(name));
            BreakableDoor door = Object.Instantiate(pf.TargetPrefab.gameObject, pos, rot).GetComponent<BreakableDoor>();
            door.transform.localScale = scale;
            door.IgnoredDamageSources = ignore_all;
            door.NetworkTargetState = false;
            NetworkServer.Spawn(door.gameObject);
            return door;
        }

        private WorkstationController SpawnWorkstation(Vector3 pos, Quaternion rot)
        {
            GameObject prefab = NetworkClient.prefabs.Values.First(p => p.name == "Spawnable Work Station Structure");
            if (prefab == null)
            {
                PluginAPI.Core.Log.Error("cant get workstation prefab");
                return null;
            }

            GameObject obj = Object.Instantiate(prefab, pos, rot);
            if (obj == null)
            {
                PluginAPI.Core.Log.Error("cant instantiate workstation prefab");
                return null;
            }

            WorkstationController wc = obj.GetComponent<WorkstationController>();
            if (wc == null)
            {
                PluginAPI.Core.Log.Error("workstation prefab does not have a WorkstationController component");
                return null;
            }

            obj.transform.position = pos;
            NetworkServer.Spawn(obj);
            return wc;
        }
    }
}
