using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using MapGeneration;
using Mirror;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    //-2.570, 12.370, -5.430
    public class ResearchCardIn173
    {
        [PluginEntryPoint("Research Card In 173", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            if (UnityEngine.Random.value < 0.3)
            {
                RoomIdentifier scp173_room = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Name == RoomName.Lcz173).First();
                Vector3 offset = new Vector3(-2.570f, 12.370f, -5.430f);
                Vector3 pos = scp173_room.transform.TransformPoint(offset);
                Quaternion rot = scp173_room.transform.rotation;
                ItemBase item;
                if (InventoryItemLoader.TryGetItem(ItemType.KeycardResearchCoordinator, out item))
                {
                    ItemPickupBase pickup = UnityEngine.Object.Instantiate(item.PickupDropModel, pos, rot);
                    if (pickup != null)
                    {
                        pickup.NetworkInfo = new PickupSyncInfo(ItemType.KeycardResearchCoordinator, 1.0f);
                        NetworkServer.Spawn(pickup.gameObject);
                    }
                    else
                        Log.Error("could not convert PickupDropModel " + "KeycardResearchCoordinator" + " to AmmoPickup");
                }
                else
                    Log.Error("could not load item of type " + "KeycardResearchCoordinator");
            }
        }

    }
}
