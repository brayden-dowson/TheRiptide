using CommandSystem;
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public class SCP127
    {
        [PluginEntryPoint("SCP127", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        public static Firearm e11;
        static CoroutineHandle regen_ammo;

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            //foreach (var v in NetworkClient.prefabs.Values)
            //    Log.Info(v.name);

            RoomIdentifier hcz_armory = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Name == RoomName.HczArmory).First();
            Vector3 offset = new Vector3(4.22f, 0.0f, 0.0f);

            var game_object = Object.Instantiate(NetworkClient.prefabs.Values.First(x => x.name == "RifleRackStructure"), hcz_armory.transform.TransformPoint(offset), hcz_armory.transform.rotation * Quaternion.Euler(Vector3.up * -90));
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
                }
            }
            NetworkServer.Spawn(game_object);
            Timing.KillCoroutines(regen_ammo);
            regen_ammo = Timing.RunCoroutine(_TeethGunAmmoRegen());
        }

        [PluginEvent(ServerEventType.RoundRestart)]
        void OnRoundRestart()
        {
            Timing.KillCoroutines(regen_ammo);
        }

        [PluginEvent(ServerEventType.RoundEnd)]
        void OnRoundEnd(RoundSummary.LeadingTeam leadingTeam)
        {
            Timing.KillCoroutines(regen_ammo);
        }

        private static bool IsScp127(ItemBase item)
        {
            return item.ItemTypeId == ItemType.GunE11SR && item is Firearm firearm && firearm.GetCurrentAttachmentsCode() == 0;
        }

        [PluginEvent(ServerEventType.PlayerDropItem)]
        bool OnPlayerDropItem(Player player, ItemBase item)
        {
            if (IsScp127(item))
            {
                byte ammo = (item as Firearm).Status.Ammo;
                player.RemoveItem(new Item(item));
                Timing.CallDelayed(0.1f, () =>
                {
                    DropScp127(ammo, player.GameObject.transform.position, player.GameObject.transform.rotation);
                });
                return false;
            }
            else
                return true;
        }

        [PluginEvent(ServerEventType.PlayerSearchedPickup)]
        bool OnSearchedPickup(Player player, ItemPickupBase pickup)
        {
            if (pickup.Info.ItemId == ItemType.GunE11SR && pickup is FirearmPickup firearm && firearm.NetworkStatus.Attachments == 0)
            {
                
                byte ammo = firearm.Status.Ammo;
                pickup.DestroySelf();
                Timing.CallDelayed(0.1f, () =>
                {
                    player.SendBroadcast("You have picked up SCP-127", 5);
                    GiveScp127(player, ammo);
                });
                return false;
            }
            else
                return true;
        }

        [PluginEvent(ServerEventType.PlayerUnloadWeapon)]
        bool OnUnloadWeapon(Player player, Firearm gun)
        {
            if (gun.ItemTypeId == ItemType.GunE11SR && gun.GetCurrentAttachmentsCode() == 0)
                return false;
            else
                return true;
        }

        [PluginEvent(ServerEventType.PlayerReloadWeapon)]
        bool OnReloadWeapon(Player player, Firearm gun)
        {
            if (gun.ItemTypeId == ItemType.GunE11SR && gun.GetCurrentAttachmentsCode() == 0)
                return false;
            else
                return true;
        }

        public static void GiveScp127(Player player, byte ammo)
        {
            Firearm scp127 = player.AddItem(ItemType.GunE11SR) as Firearm;
            scp127.Status = new FirearmStatus(ammo, FirearmStatusFlags.MagazineInserted, 0);
            AttachmentsUtils.ApplyAttachmentsCode(scp127, 0, false);
        }

        public static void DropScp127(byte ammo, Vector3 position, Quaternion rotation)
        {
            Firearm firearm;
            if (InventoryItemLoader.TryGetItem(ItemType.GunE11SR, out firearm))
            {
                FirearmPickup pickup = Object.Instantiate(firearm.PickupDropModel, position, rotation) as FirearmPickup;
                if (pickup != null)
                {
                    pickup.NetworkInfo = new PickupSyncInfo(ItemType.GunE11SR, 1.0f);
                    pickup.NetworkStatus = new FirearmStatus(ammo, FirearmStatusFlags.None, 0);
                    NetworkServer.Spawn(pickup.gameObject);
                }
            }
        }

        static IEnumerator<float> _TeethGunAmmoRegen()
        {
            while(true)
            {
                foreach(var player in Player.GetPlayers())
                {
                    if(player.IsAlive && player.IsHuman)
                    {
                        foreach (var item in player.ReferenceHub.inventory.UserInventory.Items)
                        {
                            if(IsScp127(item.Value))
                            {
                                if (item.Value is Firearm firearm && firearm.Status.Ammo < 60)
                                    firearm.Status = new FirearmStatus((byte)(firearm.Status.Ammo + 1), FirearmStatusFlags.MagazineInserted, 0);
                            }
                        }
                    }
                }
                yield return Timing.WaitForSeconds(1.0f);
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

    //[CommandHandler(typeof(RemoteAdminCommandHandler))]
    //public class sa : ICommand
    //{
    //    public string Command { get; } = "sa";

    //    public string[] Aliases { get; } = new string[] { };

    //    public string Description { get; } = "attachments";

    //    public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
    //    {
    //        Player player;
    //        if (Player.TryGet(sender, out player))
    //        {
    //            uint code = 0;
    //            if(!uint.TryParse(arguments.ElementAt(0), out code))
    //            {
    //                response = "falied to parse: " + arguments.ElementAt(0);
    //                return false;
    //            }
    //            SCP127.e11 = player.AddItem(ItemType.GunE11SR) as Firearm;
    //            SCP127.e11.Status = new FirearmStatus(60, FirearmStatusFlags.MagazineInserted, code);
    //            AttachmentsUtils.ApplyAttachmentsCode(SCP127.e11, code, false);
    //            response = "success";
    //            return true;
    //        }
    //        response = "failed";
    //        return false;
    //    }
    //}
}
