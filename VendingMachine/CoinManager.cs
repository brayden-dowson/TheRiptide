using HarmonyLib;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Coin;
using InventorySystem.Items.Pickups;
using InventorySystem.Searching;
using MEC;
using Mirror;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Items;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public class CoinConfig
    {
        [Description("make it so players can hold unlimited coins with a single inventory slot")]
        public bool EnableCoinStacking { get; set; } = true;
        [Description("how many coins can stack in one slot.")]
        public int MaxStackSize { get; set; } = 8;
        [Description("how many stacks your can have. e.g. if max_stack_size = 8 and coin_stack_limit = 3 then a player can hold 8 x 3 = 24 coins with 3 used inventory slots")]
        public int CoinStackLimit { get; set; } = 3;
        [Description("which roles will get coins on spawn")]
        public Dictionary<RoleTypeId, int> GrantCoinsToRoleOnSpawn { get; set; } = new Dictionary<RoleTypeId, int>
        {
            {RoleTypeId.ClassD, 1 },
            {RoleTypeId.Scientist, 2 },
            {RoleTypeId.FacilityGuard, 1 },
            {RoleTypeId.ChaosConscript, 1 },
            {RoleTypeId.NtfSpecialist, 1 },
        };

        [Description("send broadcast to player each time they get a coin. 0 = disabed")]
        public ushort BroadastTimeOnChanged { get; set; } = 0;
        [Description("send hint to player each time they get a coin. 0.0 = disabed")]
        public float HintTimeOnChanged { get; set; } = 2.5f;

        public ushort BroadcastTimeOnEquip { get; set; } = 0;
        public float HintTimeOnEquip { get; set; } = 2.5f;

        [Description("{count} = amount in current stack {total} = amount added in all stacks, {delta} = number of coins added or removed(only for OnChanged)")]
        public string OnChangedTranslation { get; set; } = "[{delta}] stack: {count} coins: {total}";
        public string OnEquipTranslation { get; set; } = "stack: {count} coins: {total}";
        public string OnReachedCoinStackLimit { get; set; } = "Reached the limit of <color=#FFFF00>Coin stacks</color> (<color=#FFFF00>3 stacks</color>)";
        public bool BroadcastShouldClearPrevious { get; set; } = true;

        public float CoinPickupModelScale { get; set; } = 2.0f;
    }

    [RequireComponent(typeof(ItemPickupBase))]
    public class CoinPickupStack:MonoBehaviour
    {
        //public static Dictionary<ItemPickupBase, CoinPickupStack> PickupCoinStacks = new Dictionary<ItemPickupBase, CoinPickupStack>();

        //public ItemPickupBase Coin { get; private set; }
        public int Size = 1;

        //public void Awake()
        //{
        //    Log.Info("pickup stack add");
        //    Coin = GetComponent<ItemPickupBase>();
        //    PickupCoinStacks.Add(Coin, this);
        //}

        //public void OnDestroy()
        //{
        //    Log.Info("pickup stack remove");
        //    PickupCoinStacks.Remove(Coin);
        //}
    }

    [RequireComponent(typeof(Coin))]
    public class CoinStack:MonoBehaviour
    {
        //public static Dictionary<ReferenceHub, List<CoinStack>> PlayerCoinStacks = new Dictionary<ReferenceHub, List<CoinStack>>();

        //public Coin Coin { get; private set; }
        public int Size = 1;

        //public void Awake()
        //{
        //    Log.Info("coin stack add");
        //    Coin = GetComponent<Coin>();
        //    if (!PlayerCoinStacks.ContainsKey(Coin.Owner))
        //        PlayerCoinStacks.Add(Coin.Owner, new List<CoinStack> { this });
        //    else
        //        PlayerCoinStacks[Coin.Owner].Add(this);
        //}

        ////public void Add()
        ////{
        ////    Log.Info("coin stack add");
        ////    Coin = GetComponent<Coin>();
        ////    if (!PlayerCoinStacks.ContainsKey(Coin.Owner))
        ////        PlayerCoinStacks.Add(Coin.Owner, new List<CoinStack> { this });
        ////    else
        ////        PlayerCoinStacks[Coin.Owner].Add(this);
        ////}

        //public void OnDestroy()
        //{
        //    Log.Info("coin stack remove");
        //    if (PlayerCoinStacks.ContainsKey(Coin.Owner))
        //        PlayerCoinStacks[Coin.Owner].Remove(this);
        //}
    }

    public static class CoinManager
    {
        public static CoinConfig config;
        private static Dictionary<ItemBase, int> player_coins = new Dictionary<ItemBase, int>();
        private static Action<ReferenceHub, ItemBase, ItemPickupBase> on_added;
        private static Action<ReferenceHub, ItemBase, ItemPickupBase> on_removed;
        private static Harmony harmony;

        static CoinManager()
        {
            harmony = new Harmony("CoinManager");
            on_added = (hub, item, pickup) =>
            {
                if (item.ItemTypeId != ItemType.Coin)
                    return;

                Log.Info("on_added");

                int count = 1;
                CoinPickupStack pickup_stack = null;
                if (pickup != null && pickup.TryGetComponent(out pickup_stack))
                    count = pickup_stack.Size;
                else
                {
                    if (pickup == null)
                        Log.Info("null pickup");
                    else
                        Log.Info("no pickup stack");
                }
                Log.Info("adding " + count);

                CoinStack stack = item.GetComponent<CoinStack>();
                if (stack == null)
                {
                    stack = item.gameObject.AddComponent<CoinStack>();
                    stack.Size = count;
                    //stack.Add();
                }
                else
                    stack.Size += count;

                if (pickup_stack != null)
                {
                    pickup_stack.Size = 0;
                    //pickup_stack.Remove();
                }

                DisplayTotalOnChanged(hub, stack.Size, count);

                //Timing.CallDelayed(0.0f, () => DisplayTotalOnChanged(hub, stack.Size, count));
                Log.Info("added " + count);
            };

            on_removed = (hub, item, pickup) =>
            {
                if (item.ItemTypeId != ItemType.Coin)
                    return;

                Log.Info("on_removed");

                int count = 1;
                CoinStack stack = null;
                if (item.TryGetComponent(out stack))
                    count = stack.Size;

                if (pickup != null)
                {
                    CoinPickupStack pickup_stack = pickup.GetComponent<CoinPickupStack>();
                    if (pickup_stack == null)
                    {
                        Log.Info("new stack");
                        pickup_stack = pickup.gameObject.AddComponent<CoinPickupStack>();
                        pickup_stack.Size = count;
                        //pickup_stack.Add();
                    }
                    else
                    {
                        Log.Info("existing stack");
                        pickup_stack.Size += count;
                    }
                }
                else
                    Log.Info("null pickup");

                if (stack != null)
                {
                    stack.Size = 0;
                    //stack.Remove();
                }

                DisplayTotalOnChanged(hub, 0, -count);
            };
        }

        public static void Enable(CoinConfig config)
        {
            CoinManager.config = config;
            InventoryExtensions.OnItemAdded += on_added;
            InventoryExtensions.OnItemRemoved += on_removed;
            harmony.Patch(typeof(InventoryExtensions).GetMethod("ServerAddItem"), null, null, new HarmonyMethod(typeof(Patches.InventoryExtensionsPatch).GetMethod("Transpiler")));
            harmony.Patch(typeof(InventoryExtensions).GetMethod("ServerCreatePickup", new Type[] { typeof(ItemBase), typeof(PickupSyncInfo), typeof(Vector3), typeof(Quaternion), typeof(bool), typeof(Action<ItemPickupBase>) }), new HarmonyMethod(typeof(Patches.InventoryExtensionsPatch).GetMethod("Prefix")));
            harmony.Patch(typeof(ItemSearchCompletor).GetMethod("Complete"), new HarmonyMethod(typeof(Patches.ItemSearchCompletorPatch).GetMethod("Prefix")));
        }

        public static void Disable()
        {
            InventoryExtensions.OnItemAdded -= on_added;
            InventoryExtensions.OnItemRemoved -= on_removed;
            harmony.UnpatchAll("CoinManager");
        }

        public static int TotalCoins(Player player)
        {
            int total = 0;
            foreach(var item in player.ReferenceHub.inventory.UserInventory.Items.Values)
            {
                if(item is Coin)
                {
                    CoinStack stack = item.GetComponent<CoinStack>();
                    if (stack != null)
                        total += stack.Size;
                }
            }    

            return total;
        }

        public static void Pay(Player player, int cost)
        {
            List<KeyValuePair<ItemBase, CoinStack>> items = player.ReferenceHub.inventory.UserInventory.Items.Values.Where(i => (i is Coin && i.GetComponent<CoinStack>() != null)).ToList().ConvertAll(i => new KeyValuePair<ItemBase, CoinStack>(i, i.GetComponent<CoinStack>()));
            items.Sort((l, r) => l.Value.Size - r.Value.Size);
            foreach (var item in items)
            {
                int deduct = Mathf.Min(item.Value.Size, cost);
                item.Value.Size -= deduct;
                cost -= deduct;
                if (item.Value.Size == 0)
                    player.RemoveItem(new Item(item.Key));

            }

            //List<CoinStack> stacks;
            //if (CoinStack.PlayerCoinStacks.TryGetValue(player.ReferenceHub, out stacks))
            //{
            //    while(cost > 0 && !stacks.All(s=>s.Size == 0))
            //    {
            //        CoinStack stack = stacks.Last(s=>s.Size != 0);
            //        int deduct = Mathf.Min(stack.Size, cost);
            //        stack.Size -= deduct;
            //        cost -= deduct;
            //        if (stack.Size == 0)
            //            player.RemoveItem(new Item(stack.Coin));
            //    }
            //}
        }

        public static ItemBase TryAddToStack(Inventory inv, ItemType type, ItemPickupBase pickup)
        {
            if (type != ItemType.Coin)
                return null;

            int count = 1;
            CoinPickupStack pickup_stack = null;
            if (pickup != null && pickup.TryGetComponent(out pickup_stack))
                count = pickup_stack.Size;

            int total = 0;
            int stack_count = 0;
            int transfered = 0;
            ItemBase result = null;

            foreach (var item in inv.UserInventory.Items.Values)
            {
                if (!(item is Coin))
                    continue;
                CoinStack stack = item.GetComponent<CoinStack>();
                if (!item.TryGetComponent(out stack))
                    continue;

                int addable = Mathf.Min(config.MaxStackSize - stack.Size, count);
                stack.Size += addable;
                transfered += addable;
                count -= addable;
                if (addable != 0)
                    result = item;
                total += stack.Size;
                stack_count++;
            }

            //foreach (var stack in stacks)
            //{
            //    int addable = Mathf.Min(config.MaxStackSize - stack.Size, count);
            //    stack.Size += addable;
            //    transfered += addable;
            //    count -= addable;
            //    if (addable != 0)
            //        result = stack.Coin;
            //    total += stack.Size;
            //    stack_count++;
            //}

            if (transfered != 0 && pickup_stack != null)
                pickup_stack.Size -= transfered;

            if (total != 0 && result != null)
            {
                string on_changed_msg = config.OnChangedTranslation.Replace("{count}", result.GetComponent<CoinStack>().Size.ToString()).Replace("{total}", total.ToString()).Replace("{delta}", transfered.ToString("+0;-#"));
                if (config.BroadastTimeOnChanged > 0)
                    Player.Get(inv._hub).SendBroadcast(on_changed_msg, config.BroadastTimeOnChanged, shouldClearPrevious: config.BroadcastShouldClearPrevious);
                if (config.HintTimeOnChanged > 0)
                    Player.Get(inv._hub).ReceiveHint(on_changed_msg, config.HintTimeOnChanged);
            }

            Log.Info("transfered " + transfered);

            if (count != 0 && config.CoinStackLimit > stack_count)
                result = null;

            return result;

            //if (stack_count >= config.CoinStackLimit && count != 0)
            //    return false;
            //else
            //    return true;
        }

        public static void DisplayTotalOnChanged(ReferenceHub hub, int count, int change)
        {
            if (EventHandler.Dying.Contains(hub.PlayerId))
                return;

            //List<CoinStack> stacks;
            //if (!CoinStack.PlayerCoinStacks.TryGetValue(hub, out stacks))
            //    return;

            int total = TotalCoins(new Player(hub));
            //foreach (var s in CoinStack.PlayerCoinStacks[hub])
            //    total += s.Size;

            string on_changed_msg = config.OnChangedTranslation.Replace("{count}", count.ToString()).Replace("{total}", total.ToString()).Replace("{delta}", change.ToString("+0;-#"));
            if (config.BroadastTimeOnChanged > 0)
                Player.Get(hub).SendBroadcast(on_changed_msg, config.BroadastTimeOnChanged, shouldClearPrevious: config.BroadcastShouldClearPrevious);
            if (config.HintTimeOnChanged > 0)
                Player.Get(hub).ReceiveHint(on_changed_msg, config.HintTimeOnChanged);
        }

        public static void DisplayTotalOnEquip(ReferenceHub hub, ItemBase item)
        {
            CoinStack stack = item.GetComponent<CoinStack>();
            if (stack == null)
                return;

            //List<CoinStack> stacks;
            //if (!CoinStack.PlayerCoinStacks.TryGetValue(hub, out stacks))
            //    return;

            //int total = 0;
            //foreach (var s in CoinStack.PlayerCoinStacks[hub])
            //    total += s.Size;

            int total = TotalCoins(new Player(hub));

            string on_equiped_msg = config.OnEquipTranslation.Replace("{count}", stack.Size.ToString()).Replace("{total}", total.ToString());
            if (config.BroadcastTimeOnEquip > 0)
                Player.Get(hub).SendBroadcast(on_equiped_msg, config.BroadcastTimeOnEquip, shouldClearPrevious: config.BroadcastShouldClearPrevious);
            if (config.HintTimeOnEquip > 0)
                Player.Get(hub).ReceiveHint(on_equiped_msg, config.HintTimeOnEquip);
        }
    }
}
