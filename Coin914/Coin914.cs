using InventorySystem.Items.Pickups;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using Scp914;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using InventorySystem.Items.Coin;
using InventorySystem.Items.Usables;
using InventorySystem.Items.Keycards;
using InventorySystem.Items.Radio;
using Mirror;
using PluginAPI.Core;
using InventorySystem.Items;
using PluginAPI.Core.Items;

namespace TheRiptide
{
    public class Coin914
    {
        [PluginEntryPoint("Coin 914", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        [PluginEvent(ServerEventType.Scp914UpgradeInventory)]
        public void OnScp914UpgradeInventory(Player player, ItemBase item, Scp914KnobSetting setting)
        {
            if(item.ItemTypeId == ItemType.Coin)
            {
                switch(setting)
                {
                    case Scp914KnobSetting.Rough:
                    case Scp914KnobSetting.Coarse:
                    case Scp914KnobSetting.OneToOne:
                        return;
                    case Scp914KnobSetting.Fine:
                        if (Random.value > 0.5)
                        {
                            player.RemoveItem(new Item(item));
                            if (Random.value > 0.5)
                                player.AddItem(ItemType.Painkillers);
                        }
                        return;
                    case Scp914KnobSetting.VeryFine:
                        if (Random.value > 0.5)
                        {
                            player.RemoveItem(new Item(item));
                            if (Random.value < (1.0f / 3.0f))
                                player.AddItem(ItemType.KeycardJanitor);
                            else if(Random.value < (1.0f / 4.0f))
                                player.AddItem(ItemType.Radio);
                        }
                        return;
                }
            }
        }

        [PluginEvent(ServerEventType.Scp914UpgradePickup)]
        public void OnScp914UpgradePickup(ItemPickupBase item, Vector3 position, Scp914KnobSetting setting)
        {
            if (item.Info.ItemId == ItemType.Coin)
            {
                switch (setting)
                {
                    case Scp914KnobSetting.Rough:
                    case Scp914KnobSetting.Coarse:
                    case Scp914KnobSetting.OneToOne:
                        return;
                    case Scp914KnobSetting.Fine:
                        if (Random.value > 0.5)
                        {
                            Quaternion rot = item.transform.rotation;
                            item.DestroySelf();
                            if (Random.value > 0.5)
                            {
                                Painkillers painkillers;
                                if (InventorySystem.InventoryItemLoader.TryGetItem(ItemType.Painkillers, out painkillers))
                                {
                                    ItemPickupBase new_item = Object.Instantiate(painkillers.PickupDropModel, position, rot);
                                    new_item.NetworkInfo = new PickupSyncInfo(ItemType.Painkillers, 1.0f);
                                    NetworkServer.Spawn(new_item.gameObject);
                                }
                            }
                        }
                        return;
                    case Scp914KnobSetting.VeryFine:
                        if (Random.value > 0.5)
                        {
                            Quaternion rot = item.transform.rotation;
                            item.DestroySelf();
                            if (Random.value < (1.0f / 3.0f))
                            {
                                KeycardItem keycard;
                                if (InventorySystem.InventoryItemLoader.TryGetItem(ItemType.KeycardJanitor, out keycard))
                                {
                                    ItemPickupBase new_item = Object.Instantiate(keycard.PickupDropModel, position, rot);
                                    new_item.NetworkInfo = new PickupSyncInfo(ItemType.KeycardJanitor, 1.0f);
                                    NetworkServer.Spawn(new_item.gameObject);
                                }
                            }
                            else if (Random.value < (1.0f / 4.0f))
                            {
                                RadioItem radio;
                                if (InventorySystem.InventoryItemLoader.TryGetItem(ItemType.Radio, out radio))
                                {
                                    ItemPickupBase new_item = Object.Instantiate(radio.PickupDropModel, position, rot);
                                    new_item.NetworkInfo = new PickupSyncInfo(ItemType.Radio, 1.0f);
                                    NetworkServer.Spawn(new_item.gameObject);
                                }
                            }
                        }
                        return;
                }
            }
        }
    }
}
