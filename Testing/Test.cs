using AdminToys;
using Footprinting;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables.Scp244;
using MapGeneration;
using MEC;
using Mirror;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Core.Items;
using PluginAPI.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public class Test
    {
        private static HashSet<ItemType> target_items = new HashSet<ItemType>
        {
            ItemType.SCP018,
            ItemType.SCP207,
            ItemType.SCP244a,
            ItemType.SCP244b,
            ItemType.SCP268,
            ItemType.SCP500,
            ItemType.SCP1576,
            ItemType.SCP1853,
            ItemType.SCP2176
        };

        [PluginEntryPoint("Test", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        static List<DoorVariant> doors = new List<DoorVariant>();
        static Dictionary<int, LightSourceToy> player_lights = new Dictionary<int, LightSourceToy>();
        static HashSet<ushort> dropped_lights = new HashSet<ushort>();
        static CoroutineHandle out_of_bounds;
        static Scp244DeployablePickup scp244a;
        static Scp244DeployablePickup scp244b;

        [PluginEvent(ServerEventType.WaitingForPlayers)]
        void OnWaitingForPlayers()
        {
            KeycardPermissions permission = KeycardPermissions.ContainmentLevelThree;
            DoorDamageType ignore_all = DoorDamageType.ServerCommand | DoorDamageType.Grenade | DoorDamageType.Weapon | DoorDamageType.Scp096;

            foreach (var d in DoorVariant.DoorsByRoom[RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Hcz096)])
            {
                if (d.Rooms.Count() == 1)
                {
                    d.RequiredPermissions.RequiredPermissions = permission;
                    BreakableDoor breakable = d as BreakableDoor;
                    breakable.IgnoredDamageSources = ignore_all;
                    doors.Add(d);
                }
            }

            RoomIdentifier scp049 = RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Hcz049);
            foreach (var d in DoorVariant.DoorsByRoom[scp049])
            {
                if (d.Rooms.Count() == 1 && d is PryableDoor)
                {
                    d.RequiredPermissions.RequiredPermissions = permission;
                    doors.Add(d);
                }
            }

            Vector3 blocK_offset = new Vector3(4.246f, 193.946f, 7.856f);
            Vector3 block_position = scp049.transform.TransformPoint(blocK_offset);
            GameObject cube_pf = NetworkManager.singleton.spawnPrefabs.First(p => p.name == "PrimitiveObjectToy");
            PrimitiveObjectToy toy = Object.Instantiate(cube_pf, block_position, scp049.transform.rotation).GetComponent<PrimitiveObjectToy>();
            toy.transform.localScale = new Vector3(2.0f, 3.0f, 0.1f);
            toy.NetworkPrimitiveType = PrimitiveType.Cube;
            NetworkServer.Spawn(toy.gameObject);

            Vector3 offset = new Vector3(-5.262f, 0.0f, -1.422f);
            RoomIdentifier scp939 = RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Hcz939);
            Vector3 position = scp939.transform.TransformPoint(offset);
            GameObject pf = NetworkManager.singleton.spawnPrefabs.First(p => p.name == "HCZ BreakableDoor");
            BreakableDoor door = Object.Instantiate(pf, position, scp939.transform.rotation).GetComponent<BreakableDoor>();
            door.transform.localScale = new Vector3(1.0f, 1.0f, 1.5f);
            door.IgnoredDamageSources = ignore_all;
            door.RequiredPermissions.RequiredPermissions = permission;
            NetworkServer.Spawn(door.gameObject);
            doors.Add(door);

            //item lights
            Timing.CallDelayed(5.0f, () =>
            {
                ItemPickupBase[] items = Object.FindObjectsOfType<ItemPickupBase>();
                foreach (var item in items)
                {
                    if (target_items.Contains(item.Info.ItemId))
                    {
                        AddLight(item.gameObject.transform);
                        dropped_lights.Add(item.Info.Serial);
                    }
                }
            });

            out_of_bounds = Timing.RunCoroutine(_OutofBounds());
        }

        [PluginEvent(ServerEventType.PlayerInteractDoor)]
        public void OnPlayerInteractDoor(Player player, DoorVariant door, bool can_open)
        {
            if(can_open && doors.Contains(door))
            {
                player.SendBroadcast("opened special door", 10, shouldClearPrevious: true);
                door.ServerChangeLock(DoorLockReason.SpecialDoorFeature, true);
            }
        }

        [PluginEvent(ServerEventType.PlayerDropItem)]
        void OnPlayerDroppedItem(Player player, ItemBase item)
        {
            if (target_items.Contains(item.ItemTypeId))
            {
                dropped_lights.Remove(item.ItemSerial);
                Timing.CallDelayed(0.5f, () =>
                {
                    try
                    {
                        ItemPickupBase[] items = Object.FindObjectsOfType<ItemPickupBase>();
                        foreach (var i in items)
                        {
                            if (target_items.Contains(i.Info.ItemId) && !dropped_lights.Contains(i.Info.Serial))
                            {
                                AddLight(i.gameObject.transform);
                                dropped_lights.Add(i.Info.Serial);
                            }
                        }
                        AddLight(item.PickupDropModel.gameObject.transform);
                        UpdatePlayerLight(player);
                    }
                    catch(System.Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerSearchedPickup)]
        void OnSearchedPickup(Player player, ItemPickupBase pickup)
        {
            if(target_items.Contains(pickup.NetworkInfo.ItemId))
            {
                Timing.CallDelayed(0.5f,()=>
                {
                    try
                    {
                        UpdatePlayerLight(player);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                });
            }
        }

        [PluginEvent(ServerEventType.GrenadeExploded)]
        void OnGrenadeExploded(Footprint owner, Vector3 position, ItemPickupBase grenade)
        {
            Scp244State prev_a = scp244a == null ? Scp244State.PickedUp : scp244a.State;
            Scp244State prev_b = scp244b == null ? Scp244State.PickedUp : scp244b.State;

            if (prev_a == Scp244State.PickedUp && prev_b == Scp244State.PickedUp)
                return;

            Timing.CallDelayed(0.0f,()=>
            {
                bool destroyed = false;
                if (scp244a != null && prev_a != Scp244State.Destroyed && scp244a.State == Scp244State.Destroyed)
                    destroyed = true;
                if (scp244b != null && prev_b != Scp244State.Destroyed && scp244b.State == Scp244State.Destroyed)
                    destroyed = true;

                Player player;
                if (!destroyed || owner.Hub == null || !Player.TryGet(owner.Hub, out player))
                    return;
                player.Kill("destroyed scp244");
            });
        }

        private static void UpdatePlayerLight(Player player)
        {
            int count = 0;
            foreach (var item in player.ReferenceHub.inventory.UserInventory.Items.Values)
                if (target_items.Contains(item.ItemTypeId))
                    count++;
            if (!player_lights.ContainsKey(player.PlayerId))
                player_lights.Add(player.PlayerId, AddLight(player.GameObject.transform));
            Log.Info("update player light: " + count);
            LightSourceToy light = player_lights[player.PlayerId];
            light.NetworkLightIntensity = count * 1.0f;
            light.NetworkLightRange = count * 10.0f;
        }

        private static LightSourceToy AddLight(Transform transform)
        {
            GameObject light_pf = NetworkManager.singleton.spawnPrefabs.First(p => p.name == "LightSourceToy");
            LightSourceToy light_toy = Object.Instantiate(light_pf, transform).GetComponent<LightSourceToy>();
            light_toy.NetworkLightColor = new Color(0.0f, 1.0f, 0.0f);
            light_toy.NetworkLightIntensity = 2.0f;
            light_toy.NetworkLightRange = 10.0f;
            light_toy.NetworkLightShadows = true;
            light_toy.NetworkMovementSmoothing = 128;
            NetworkServer.Spawn(light_toy.gameObject);
            Log.Info("added light");
            return light_toy;
        }

        private static IEnumerator<float> _OutofBounds()
        {
            while (true)
            {
                try
                {
                    ItemPickupBase[] items = Object.FindObjectsOfType<ItemPickupBase>();
                    foreach (var item in items)
                    {
                        if (target_items.Contains(item.Info.ItemId) && item.transform.position.y < -1007.0f)
                        {
                            Player owner;
                            if (item.PreviousOwner.Hub != null && Player.TryGet(item.PreviousOwner.Hub, out owner))
                                owner.Kill("dropped item out of bounds");
                            dropped_lights.Remove(item.Info.Serial);
                            NetworkServer.Destroy(item.gameObject);
                        }

                        if (item.Info.ItemId == ItemType.SCP244a)
                            scp244a = item as Scp244DeployablePickup;
                        else if (item.Info.ItemId == ItemType.SCP244b)
                            scp244b = item as Scp244DeployablePickup;
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error("_UpdateLights error: " + ex.ToString());
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }
    }
}
