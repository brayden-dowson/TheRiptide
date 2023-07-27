using CommandSystem;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Pickups;
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
using PlayerRoles.FirstPersonControl;
using MapGeneration;

namespace TheRiptide
{
    public class TranquiliserGun
    {
        [PluginEntryPoint("Tranquiliser Gun", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        static uint attachment_code = 0;
        static ItemType item_model = ItemType.GunRevolver;

        [PluginEvent(ServerEventType.WaitingForPlayers)]
        void WaitingForPlayers()
        {
            foreach (var prefab in NetworkClient.prefabs)
                Log.Info(prefab.Value.name);
            RoomIdentifier room = RoomIdentifier.AllRoomIdentifiers.First();
            Log.Info(room.gameObject.layer.ToString());


            for(int layer = 0; layer < 32; layer++)
            {
                Log.Info(layer.ToString() + " | " + LayerMask.LayerToName(layer) + " | " + System.Convert.ToString(LayerMask.GetMask(LayerMask.LayerToName(layer)), 2));
            }
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            Server.Instance.SetRole(PlayerRoles.RoleTypeId.Scp939);

            foreach (var player in Player.GetPlayers())
                Log.Info(player.Nickname + " | " + player.GameObject.layer.ToString());
        }

        private static bool IsTranqGun(ItemBase item)
        {
            return item.ItemTypeId == item_model && item is Firearm firearm && firearm.GetCurrentAttachmentsCode() == attachment_code;
        }

        [PluginEvent(ServerEventType.PlayerDropItem)]
        bool OnPlayerDropItem(Player player, ItemBase item)
        {
            if (IsTranqGun(item))
            {
                byte ammo = (item as Firearm).Status.Ammo;
                player.RemoveItem(new Item(item));
                Timing.CallDelayed(0.1f, () =>
                {
                    Drop(ammo, player.GameObject.transform.position, player.GameObject.transform.rotation);
                });
                return false;
            }
            else
                return true;
        }

        [PluginEvent(ServerEventType.PlayerSearchedPickup)]
        bool OnSearchedPickup(Player player, ItemPickupBase pickup)
        {
            if (pickup.Info.ItemId == item_model && pickup is FirearmPickup firearm && firearm.NetworkStatus.Attachments == attachment_code)
            {
                byte ammo = firearm.Status.Ammo;
                pickup.DestroySelf();
                Timing.CallDelayed(0.1f, () =>
                {
                    player.SendBroadcast("You have picked up the Tranquiliser Gun", 5);
                    Give(player, ammo);
                });
                return false;
            }
            else
                return true;
        }

        [PluginEvent(ServerEventType.PlayerShotWeapon)]
        bool OnShotWeapon(Player player, Firearm gun)
        {
            //if (IsTranqGun(gun))
            //{
            //    Transform t = player.GameObject.transform;
            //    Vector3 pos = t.position + new Vector3(0.0f, 0.55f, 0.0f);
            //    Quaternion rot = player.ReferenceHub.PlayerCameraReference.rotation * Quaternion.Euler(-5.0f, 0.0f, 0.0f);
            //    var prefab = NetworkClient.prefabs.Values.First((x) => x.name == "AdrenalinePrefab");
            //    var game_object = Object.Instantiate(prefab, pos, rot);
            //    var pickup = game_object.GetComponent<CollisionDetectionPickup>();
            //    var collider = game_object.GetComponentInChildren<CapsuleCollider>();
            //    //collider.
            //    foreach (var c in game_object.GetComponentsInChildren<Component>())
            //        Log.Info(c.name + " | " + c.GetType());
            //    //var collider = game_object.GetComponent<Collider>();
            //    game_object.layer = 8;
            //    //collider.gameObject.layer = 8;
            //    Log.Info("angles: " + rot.eulerAngles.ToPreciseString());
            //    Vector3 force = (rot * Vector3.forward) * 500.0f;
            //    //Log.Info(force.ToPreciseString());
            //    pickup.RigidBody.AddForce(force);
            //    //Vector3 old_force = t.TransformPoint(Vector3.forward) * 4.0f;
            //    //Log.Info("old: " + old_force.ToPreciseString());
            //    pickup.RigidBody.AddForce(force);
            //    pickup.OnCollided += new System.Action<Collision>((Collision collision) =>
            //    {
            //        player.SendBroadcast("collision " + collision.collider.tag, 3);
            //        if (collision.gameObject != player.GameObject)
            //        {
            //            FpcStandardRoleBase target = collision.gameObject.GetComponent<FpcStandardRoleBase>();
            //            if(target != null)
            //            {
            //                player.SendBroadcast("hit " + collision.collider.tag, 3);
            //            }
            //        }
            //    });
            //    pickup.NetworkInfo = new PickupSyncInfo(ItemType.Adrenaline, pos, rot, 1.0f);
            //    gun.Status = new FirearmStatus((byte)(gun.Status.Ammo - 1), FirearmStatusFlags.Chambered, attachment_code);
            //    NetworkServer.Spawn(pickup.gameObject);
            //    return false;
            //}
            //else
            //    return true;
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

        public static void Give(Player player, byte ammo)
        {
            Firearm gun = player.AddItem(item_model) as Firearm;
            gun.Status = new FirearmStatus(ammo, FirearmStatusFlags.MagazineInserted, attachment_code);
            AttachmentsUtils.ApplyAttachmentsCode(gun, attachment_code, false);
        }

        public static void Drop(byte ammo, Vector3 position, Quaternion rotation)
        {
            Firearm firearm;
            if (InventoryItemLoader.TryGetItem(item_model, out firearm))
            {
                FirearmPickup pickup = Object.Instantiate(firearm.PickupDropModel, position, rotation) as FirearmPickup;
                if (pickup != null)
                {
                    pickup.NetworkInfo = new PickupSyncInfo(item_model, 1.0f);
                    pickup.NetworkStatus = new FirearmStatus(ammo, FirearmStatusFlags.None, attachment_code);
                    NetworkServer.Spawn(pickup.gameObject);
                }
            }
        }

        //private static IEnumerator<float> _TranqTick(Player player, CollisionDetectionPickup tranq)
        //{
        //    bool flying = true;
        //    while(flying)
        //    {
        //        tranq.on

        //        yield return Timing.WaitForOneFrame;
        //    }
        //}
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class GiveTG: ICommand
    {
        public string Command { get; } = "giveTG";

        public string[] Aliases { get; } = new string[] { };

        public string Description { get; } = "Give Tranquiliser Gun";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (Player.TryGet(sender, out player))
            {
                TranquiliserGun.Give(player, 60);
                response = "success";
                return true;
            }
            response = "failed";
            return false;
        }
    }
}
