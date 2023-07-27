using MapGeneration;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MapGeneration.Distributors;
using Mirror;
using PluginAPI.Core;
using PluginAPI.Core.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Firearms.Ammo;
using InventorySystem;
using InventorySystem.Items;

namespace TheRiptide
{
    public class SurfaceArmory
    {
        [PluginEntryPoint("Surface Armory", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            RoomIdentifier surface = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone == FacilityZone.Surface).First();
            Vector3 offset = new Vector3(62.8f, -9.286f, -51.6f);

            var game_object = Object.Instantiate(NetworkClient.prefabs.Values.First(x => x.name == "LargeGunLockerStructure"), surface.transform.TransformPoint(offset), surface.transform.rotation);
            Locker locker = game_object.GetComponent<Locker>();

            locker.Loot = new LockerLoot[0];

            int index = 1;
            foreach (LockerChamber chamber in locker.Chambers)
            {
                if (Random.value > 0.4)
                    SpawnItem(chamber, ItemType.Ammo9x19, Random.Range(1, 13));
                else
                    SpawnItem(chamber, ItemType.Ammo556x45, Random.Range(1, 5));
                index++;
            }
            NetworkServer.Spawn(game_object);

        }

        private static void SpawnItem(LockerChamber locker_chamber, ItemType item, int amount)
        {
            ItemBase item_base;
            if (InventoryItemLoader.TryGetItem(item, out item_base))
            {
                for (int i = 0; i < amount; i++)
                {
                    ItemPickupBase item_pickup_base = Object.Instantiate(item_base.PickupDropModel, locker_chamber._spawnpoint.position, locker_chamber._spawnpoint.rotation);
                    item_pickup_base.NetworkInfo = new PickupSyncInfo(item, 1.0f);
                    NetworkServer.UnSpawn(item_pickup_base.gameObject);

                    item_pickup_base.transform.SetParent(locker_chamber._spawnpoint);
                    item_pickup_base.Info.Locked = true;
                    locker_chamber._content.Add(item_pickup_base);

                    (item_pickup_base as IPickupDistributorTrigger)?.OnDistributed();

                    if (locker_chamber._spawnOnFirstChamberOpening)
                        locker_chamber._toBeSpawned.Add(item_pickup_base);
                    else
                        ItemDistributor.SpawnPickup(item_pickup_base);
                }
            }
        }

    }
}
