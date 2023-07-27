using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventorySystem;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Firearms.Ammo;
using Mirror;
using MapGeneration;
using UnityEngine;
using MEC;
using System.ComponentModel;

namespace TheRiptide
{
    public class Config
    {
        [Description("pickup ammount")]
        public int Hcz_762x39Pickups { get; set; } = 8;
        [Description("ammo per pickup")]
        public int Hcz_762x39Ammo { get; set; } = 60;

        public int Hcz_12GaugePickups { get; set; } = 4;
        public int Hcz_12GaugeAmmo { get; set; } = 24;

        public int Hcz_44MagPickups { get; set; } = 4;
        public int Hcz_44MagAmmo { get; set; } = 24;
    }

    public class AddChaosAmmoInArmory
    {
        [PluginConfig] 
        public Config Config;

        [PluginEntryPoint("Add Chaos Ammo In Armory", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            RoomIdentifier hcz_armory = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Name == RoomName.HczArmory).First();
            Vector3 hcz_offset = new Vector3(0.270f, 1.847f, -1.763f);

            float delta = 0.2f;

            int offset = 0;
            for (int i = 0; i < Config.Hcz_762x39Pickups; i++)
                Timing.CallDelayed((offset + i) * delta, () => { SpawnAmmo(ItemType.Ammo762x39, (ushort)Config.Hcz_762x39Ammo, hcz_armory, hcz_offset); });
            offset += Config.Hcz_762x39Pickups;
            for (int i = 0; i < Config.Hcz_12GaugePickups; i++)
                Timing.CallDelayed((offset + i) * delta, () => { SpawnAmmo(ItemType.Ammo12gauge, (ushort)Config.Hcz_12GaugeAmmo, hcz_armory, hcz_offset); });
            offset += Config.Hcz_12GaugePickups;
            for (int i = 0; i < Config.Hcz_44MagPickups; i++)
                Timing.CallDelayed((offset + i) * delta, () => { SpawnAmmo(ItemType.Ammo44cal, (ushort)Config.Hcz_44MagAmmo, hcz_armory, hcz_offset); });
        }

        private void SpawnAmmo(ItemType type, ushort amount, RoomIdentifier room, Vector3 offset)
        {
            AmmoItem ammo;
            if (InventoryItemLoader.TryGetItem(type, out ammo))
            {
                Transform t = room.transform;
                AmmoPickup pickup = Object.Instantiate(ammo.PickupDropModel, t.TransformPoint(offset), t.rotation) as AmmoPickup;
                if (pickup != null)
                {
                    pickup.NetworkInfo = new PickupSyncInfo(type, 1.0f);
                    pickup.NetworkSavedAmmo = amount;
                    NetworkServer.Spawn(pickup.gameObject);
                }
                else
                {
                    Log.Error("could not convert PickupDropModel " + type.ToString() + " to AmmoPickup");
                }
            }
            else
            {
                Log.Error("could not load ammo of type " + type.ToString());
            }
        }
    }
}
