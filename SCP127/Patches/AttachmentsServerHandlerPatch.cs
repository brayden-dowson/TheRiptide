using HarmonyLib;
using InventorySystem.Items.Firearms.Attachments;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(AttachmentsServerHandler), nameof(AttachmentsServerHandler.ServerReceiveChangeRequest))]
    class AttachmentsServerHandlerPatch
    {
        static bool Prefix(NetworkConnection conn, AttachmentsChangeRequest msg)
        {
            return !SCP127.scp_127.Contains(msg.WeaponSerial);
        }
    }
}
