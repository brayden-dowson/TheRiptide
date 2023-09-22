using HarmonyLib;
using InventorySystem;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using Mirror;
using PlayerRoles.Spectating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(AttachmentsServerHandler))]
    class AttachmentsServerHandlerPatch
    {
        [HarmonyPatch(nameof(AttachmentsServerHandler.ServerReceiveChangeRequest), MethodType.Normal)]
        public static bool Prefix(NetworkConnection conn, AttachmentsChangeRequest msg)
        {
            ReferenceHub hub;
            if (!NetworkServer.active || !ReferenceHub.TryGetHub(conn.identity.gameObject, out hub) || !(hub.inventory.CurInstance is Firearm curInstance) || hub.inventory.CurItem.SerialNumber != msg.WeaponSerial)
                return false;
            bool flag = hub.roleManager.CurrentRole is SpectatorRole;
            if (!flag)
            {
                foreach (WorkstationController allWorkstation in WorkstationController.AllWorkstations)
                {
                    if (!(allWorkstation == null) && allWorkstation.Status == 3 && allWorkstation.IsInRange(hub))
                    {
                        flag = true;
                        break; 
                    }
                }
            }
            if (!flag)
                return false;
            curInstance.ApplyAttachmentsCode(msg.AttachmentsCode, true);
            if (curInstance.Status.Ammo > curInstance.AmmoManagerModule.MaxAmmo)
                hub.inventory.ServerAddAmmo(curInstance.AmmoType, curInstance.Status.Ammo - curInstance.AmmoManagerModule.MaxAmmo);
            curInstance.Status = new FirearmStatus((byte)Mathf.Min(curInstance.Status.Ammo, curInstance.AmmoManagerModule.MaxAmmo), curInstance.Status.Flags, msg.AttachmentsCode);

            UpdatePreferences(hub, curInstance, msg);

            return false;
        }

        private static void UpdatePreferences(ReferenceHub hub, Firearm firearm, AttachmentsChangeRequest msg)
        {
            Dictionary<ItemType, uint> preferences;
            if (!AttachmentsServerHandler.PlayerPreferences.TryGetValue(hub, out preferences))
                return;

            if (!preferences.ContainsKey(firearm.ItemTypeId))
                return;

            preferences[firearm.ItemTypeId] = msg.AttachmentsCode;
        }
    }
}
