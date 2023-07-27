using MapGeneration;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapGeneration.Distributors;
using PluginAPI.Core;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using InventorySystem.Items;
using InventorySystem;
using InventorySystem.Items.Pickups;
using UnityEngine;
using InventorySystem.Items.Firearms.Ammo;

namespace TheRiptide
{
    public class NormalLockerAmmo
    {
        [PluginEntryPoint("Normal Locker Ammo", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        //[PluginEvent(ServerEventType.MapGenerated)]
        //void OnMapGenerated()
        //{
        //    foreach (Locker locker in UnityEngine.Object.FindObjectsOfType<Locker>())
        //    {
        //        if(locker.StructureType == StructureType.StandardLocker)
        //        {
        //            Log.Info("locker in door: " + locker.name);
        //            LockerLoot loot = new LockerLoot();
        //            loot.MaxPerChamber = 1;
        //            loot.ProbabilityPoints = 1;
        //            loot.RemainingUses = 1;
        //            loot.TargetItem = ItemType.Ammo9x19;
        //            locker.Loot = locker.Loot.Append(loot).ToArray();

        //            foreach (var l in locker.Loot)
        //                Log.Info("\t" + l.TargetItem.ToString() + ", " + l.ProbabilityPoints + ", " + l.RemainingUses);
        //        }
        //    }
        //}

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            bool found = false;
            foreach (Locker locker in UnityEngine.Object.FindObjectsOfType<Locker>())
            {
                if (locker.StructureType == StructureType.StandardLocker && 
                    RoomIdUtils.RoomAtPosition(locker.transform.position) != null &&
                    RoomIdUtils.RoomAtPosition(locker.transform.position).Zone == FacilityZone.LightContainment)
                {
                    foreach (var chamber in locker.Chambers)
                    {
                        if (chamber._content.IsEmpty())
                        {
                            ItemBase item_base;
                            if (InventoryItemLoader.TryGetItem(ItemType.Ammo9x19, out item_base))
                            {
                                AmmoPickup item_pickup_base = Object.Instantiate(item_base.PickupDropModel, chamber._spawnpoint.position, chamber._spawnpoint.rotation) as AmmoPickup;
                                item_pickup_base.NetworkSavedAmmo = 30;
                                item_pickup_base.NetworkInfo = new PickupSyncInfo(ItemType.Ammo9x19, 1.0f);
                                NetworkServer.UnSpawn(item_pickup_base.gameObject);

                                item_pickup_base.transform.SetParent(chamber._spawnpoint);
                                item_pickup_base.Info.Locked = true;
                                chamber._content.Add(item_pickup_base);

                                (item_pickup_base as IPickupDistributorTrigger)?.OnDistributed();

                                if (chamber._spawnOnFirstChamberOpening)
                                    chamber._toBeSpawned.Add(item_pickup_base);
                                else
                                    ItemDistributor.SpawnPickup(item_pickup_base);

                                found = true;
                                break;
                            }
                        }
                    }
                    if (found)
                        break;

                    //locker.Chambers.RandomItem().SpawnItem(ItemType.Ammo9x19, 1);

                    //Log.Info("locker in door: " + locker.name);
                    ////LockerLoot loot = new LockerLoot();
                    ////loot.MaxPerChamber = 1;
                    ////loot.ProbabilityPoints = 1;
                    ////loot.RemainingUses = 1;
                    ////loot.TargetItem = ItemType.Ammo9x19;
                    ////locker.Loot = locker.Loot.Append(loot).ToArray();

                    //foreach (var l in locker.Loot)
                    //    Log.Info("\t" + l.TargetItem.ToString() + ", " + l.ProbabilityPoints + ", " + l.RemainingUses);

                    //NetworkServer.UnSpawn(locker.gameObject);
                    //NetworkServer.Spawn(locker.gameObject);
                }
            }
            //Log.Info("empty locker found: " + found.ToString());
        }

    }
}
