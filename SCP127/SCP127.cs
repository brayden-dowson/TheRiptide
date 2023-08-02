using CommandSystem;
using HarmonyLib;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Pickups;
using MapGeneration;
using MapGeneration.Distributors;
using MEC;
using Mirror;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Core.Items;
using PluginAPI.Enums;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public class Config
    {
        public string PickUpMessage { get; set; } = "You have picked up SCP-127";

        [Description("Maximum clip size")]
        public byte MaxAmmo { get; set; } = 60;
        [Description("How much ammo per second to regenerate")]
        public float RegenRate { get; set; } = 1.0f;

        [Description("Room which SCP127 will spawn it")]
        public RoomName Room { get; set; } = RoomName.HczArmory;
        [Description("Offset within room to spawn RifleRack")]
        public float OffsetX { get; set; } = 4.22f;
        public float OffsetY { get; set; } = 0.0f;
        public float OffsetZ { get; set; } = 0.0f;
        [Description("Rotation in degrees along the y axis of the RifleRack")]
        public float Rotation { get; set; } = -90.0f;
    }

    public class SCP127
    {
        [PluginConfig]
        public Config config;

        public static HashSet<ushort> scp_127 = new HashSet<ushort>();
        private CoroutineHandle ammo_regen;

        private Harmony harmony;

        [PluginEntryPoint("SCP127", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            harmony = new Harmony("SCP127");
            harmony.PatchAll();
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        [PluginUnload]
        public void OnDisabled()
        {
            harmony.UnpatchAll("SCP127");
            harmony = null;
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            scp_127.Clear();
            RoomIdentifier target = RoomIdentifier.AllRoomIdentifiers.First((r) => r.Name == config.Room);
            var game_object = Object.Instantiate(NetworkClient.prefabs.Values.First(x => x.name == "RifleRackStructure"), target.transform.TransformPoint(new Vector3(config.OffsetX, config.OffsetY, config.OffsetZ)), target.transform.rotation * Quaternion.Euler(0.0f, config.Rotation, 0.0f));
            Locker locker = game_object.GetComponent<Locker>();
            locker.Loot = new LockerLoot[0];
            var chamber = locker.Chambers.First();
            Firearm firearm;
            if (InventoryItemLoader.TryGetItem(ItemType.GunE11SR, out firearm))
            {
                FirearmPickup pickup = Object.Instantiate(firearm.PickupDropModel, chamber._spawnpoint.position, chamber._spawnpoint.rotation) as FirearmPickup;
                if (pickup != null)
                {
                    pickup.NetworkInfo = new PickupSyncInfo(ItemType.GunE11SR, 1.0f);
                    pickup.NetworkStatus = new FirearmStatus(60, FirearmStatusFlags.None, 0);
                    NetworkServer.UnSpawn(pickup.gameObject);

                    pickup.transform.SetParent(chamber._spawnpoint);
                    pickup.Info.Locked = true;
                    chamber._content.Add(pickup);

                    (pickup as IPickupDistributorTrigger)?.OnDistributed();

                    if (chamber._spawnOnFirstChamberOpening)
                        chamber._toBeSpawned.Add(pickup);
                    else
                    {
                        pickup.NetworkStatus = new FirearmStatus(60, FirearmStatusFlags.None, 0);
                        ItemDistributor.SpawnPickup(pickup);
                    }
                    scp_127.Add(pickup.Info.Serial);
                }
            }
            NetworkServer.Spawn(game_object);
            Timing.KillCoroutines(ammo_regen);
            ammo_regen = Timing.RunCoroutine(_AmmoRegen());
        }

        [PluginEvent(ServerEventType.RoundRestart)]
        void OnRoundRestart()
        {
            Timing.KillCoroutines(ammo_regen);
        }

        [PluginEvent(ServerEventType.RoundEnd)]
        void OnRoundEnd(RoundSummary.LeadingTeam leadingTeam)
        {
            Timing.KillCoroutines(ammo_regen);
        }

        [PluginEvent(ServerEventType.PlayerSearchedPickup)]
        void OnSearchedPickup(Player player, ItemPickupBase pickup)
        {
            if (pickup.Info.ItemId == ItemType.GunE11SR && pickup is FirearmPickup firearm && scp_127.Contains(pickup.Info.Serial))
            {
                player.SendBroadcast(config.PickUpMessage, 5);
                Timing.CallDelayed(0.0f,()=>
                {
                    foreach (var item in player.ReferenceHub.inventory.UserInventory.Items.Values)
                        if (scp_127.Contains(item.ItemSerial) && item is Firearm scp127)
                            scp127.Status = new FirearmStatus(scp127._status.Ammo, scp127._status.Flags, 0);
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerUnloadWeapon)]
        bool OnUnloadWeapon(Player player, Firearm gun)
        {
            return !IsScp127(gun);
        }

        [PluginEvent(ServerEventType.PlayerReloadWeapon)]
        bool OnReloadWeapon(Player player, Firearm gun)
        {
            return !IsScp127(gun);
        }

        private bool IsScp127(ItemBase item)
        {
            return item.ItemTypeId == ItemType.GunE11SR && scp_127.Contains(item.ItemSerial);
        }

        public static ItemBase GiveScp127(Player player, byte ammo)
        {
            Firearm scp127 = player.AddItem(ItemType.GunE11SR) as Firearm;
            scp127.Status = new FirearmStatus(ammo, FirearmStatusFlags.MagazineInserted, 0);
            AttachmentsUtils.ApplyAttachmentsCode(scp127, 0, false);
            scp_127.Add(scp127.ItemSerial);
            return scp127;
        }

        private IEnumerator<float> _AmmoRegen()
        {
            while (true)
            {
                try
                {
                    foreach (var player in Player.GetPlayers())
                    {
                        if (!player.IsHuman)
                            continue;
                        foreach (var item in player.ReferenceHub.inventory.UserInventory.Items.Values.ToList())
                        {
                            if (!IsScp127(item) || !(item is Firearm firearm))
                                continue;
                            if (firearm.Status.Attachments != 0)
                            {
                                byte ammo = firearm.Status.Ammo;
                                bool held = player.CurrentItem == item;
                                player.RemoveItem(item);
                                ItemBase scp127 = GiveScp127(player, ammo);
                                if (held)
                                    player.CurrentItem = scp127;
                            }
                            else if (firearm.Status.Ammo < config.MaxAmmo)
                                firearm.Status = new FirearmStatus((byte)(firearm.Status.Ammo + 1), FirearmStatusFlags.MagazineInserted, 0);
                        }
                    }
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                yield return Timing.WaitForSeconds(1.0f / config.RegenRate);
            }
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class GiveScp127 : ICommand
    {
        public string Command { get; } = "givescp127";

        public string[] Aliases { get; } = new string[] { };

        public string Description { get; } = "Give Scp-127";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (Player.TryGet(sender, out player))
            {
                SCP127.GiveScp127(player, 60);
                response = "success";
                return true;
            }
            response = "failed";
            return false;
        }
    }
}
