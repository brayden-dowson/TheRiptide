using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using InventorySystem.Disarming;
using InventorySystem.Items;
using InventorySystem.Items.Usables.Scp330;
using InventorySystem.Searching;
using PlayerRoles;
using PluginAPI.Core;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(Scp330SearchCompletor))]
    class Scp330SearchCompletorPatch
    {
        [HarmonyPatch(nameof(Scp330SearchCompletor.ValidateAny), MethodType.Normal)]
        public static bool Prefix(Scp330SearchCompletor __instance, ref bool __result)
        {
            if (!(__instance.Hub.IsHuman() && !__instance.TargetPickup.Info.Locked && !__instance.Hub.inventory.IsDisarmed() && !__instance.Hub.interCoordinator.AnyBlocker(BlockedInteraction.GrabItems))
                || !EventHandler.Singleton.OnPlayerPickupScp330(Player.Get(__instance.Hub), __instance.TargetPickup as Scp330Pickup))
            {
                __result = false;
                return false;
            }
            bool hasBag = __instance._playerBag != null;
            int count = __instance.Hub.inventory.UserInventory.Items.Count;
            if (!hasBag && count < 8 || hasBag && __instance._playerBag.Candies.Count < 6)
            {
                __result = true;
                return false;
            }
            Scp330SearchCompletor.ShowOverloadHint(__instance.Hub, hasBag);
            __result = false;
            return false;
        }
    }
}
