using InventorySystem;
using InventorySystem.Searching;
using PluginAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide.Patches
{
    public class ItemSearchCompletorPatch
    {
        public static bool Prefix(ItemSearchCompletor __instance)
        {
            if (!EventManager.ExecuteEvent(new PlayerSearchedPickupEvent(__instance.Hub, __instance.TargetPickup)))
                return false;
            __instance.Hub.inventory.ServerAddItem(__instance.TargetPickup.Info.ItemId, __instance.TargetPickup.Info.Serial, __instance.TargetPickup);
            CoinPickupStack stack;
            if (__instance.TargetPickup.Info.ItemId != ItemType.Coin || !__instance.TargetPickup.TryGetComponent(out stack) || stack.Size == 0)
                __instance.TargetPickup.DestroySelf();
            else
                __instance.TargetPickup.NetworkInfo = new InventorySystem.Items.Pickups.PickupSyncInfo
                {
                    _flags = __instance.TargetPickup.Info._flags,
                    ItemId = __instance.TargetPickup.Info.ItemId,
                    Serial = __instance.TargetPickup.Info.Serial,
                    WeightKg = __instance.TargetPickup.Info.WeightKg,
                    InUse = false,
                };
            __instance.CheckCategoryLimitHint();
            return false;
        }
    }
}
