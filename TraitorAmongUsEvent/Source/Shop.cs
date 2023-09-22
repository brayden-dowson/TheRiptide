using InventorySystem.Items;
using MEC;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public enum MenuPage { None, DetectiveMainMenu, TraitorMainMenu, TraitorHeals, TraitorWeapons, TraitorOther }

    public class Shop
    {
        struct Ammo
        {
            public ItemType type;
            public ushort amount;
        }
        private static Dictionary<int, MenuPage> player_menu = new Dictionary<int, MenuPage>();
        private static Dictionary<int, List<ItemBase>> saved_items = new Dictionary<int, List<ItemBase>>();
        private static Dictionary<int, List<Ammo>> saved_ammo = new Dictionary<int, List<Ammo>>();

        private static Dictionary<int, int> player_cash = new Dictionary<int, int>();

        public static void SetupMenu()
        {
            InventoryMenu.Singleton.Clear();
            Func<Player, bool> OnExit = (player) =>
            {
                InventoryMenu.Singleton.SetMenu(player, 0);
                BroadcastOverride.ClearLines(player, BroadcastPriority.High);
                BroadcastOverride.SetEvenLineSizes(player, 6);
                LoadInventory(player);
                if (TraitorAmongUs.detectives.Contains(player.PlayerId))
                    BroadcastOverride.BroadcastLines(player, 1, 60 * 60, BroadcastPriority.Medium, TraitorAmongUs.config.DetectiveBroadcast);
                if (TraitorAmongUs.traitors.Contains(player.PlayerId))
                    BroadcastOverride.BroadcastLines(player, 1, 60 * 60, BroadcastPriority.Medium, TraitorAmongUs.config.TraitorBroadcast);
                BroadcastOverride.UpdateIfDirty(player);
                return false;
            };
            Func<Player,ItemType, int, bool> OnBuy = (player, item, price)=>
            {
                InventoryMenu.Singleton.SetMenu(player, 0);
                BroadcastOverride.ClearLines(player, BroadcastPriority.High);
                BroadcastOverride.SetEvenLineSizes(player, 6);
                LoadInventory(player);
                if (!player_cash.ContainsKey(player.PlayerId) || player_cash[player.PlayerId] < price)
                    BroadcastOverride.BroadcastLine(player, 6, 5, BroadcastPriority.High, "<align=left><color=#FF0000>Insufficient funds</color>");
                else
                {
                    player_cash[player.PlayerId] -= price;
                    if (IsGun(item))
                        AddFirearm(player, item, true);
                    else
                        player.AddItem(item);
                    BroadcastOverride.BroadcastLine(player, 6, 5, BroadcastPriority.High, "<align=left><color=#00FF00>Bought </color>" + item + " for <color=#00FF00>$" + price + "</color>");
                }

                if (TraitorAmongUs.detectives.Contains(player.PlayerId))
                    BroadcastOverride.BroadcastLines(player, 1, 60 * 60, BroadcastPriority.Medium, TraitorAmongUs.config.DetectiveBroadcast);
                if (TraitorAmongUs.traitors.Contains(player.PlayerId))
                    BroadcastOverride.BroadcastLines(player, 1, 60 * 60, BroadcastPriority.Medium, TraitorAmongUs.config.TraitorBroadcast);
                BroadcastOverride.UpdateIfDirty(player);
                return false;
            };
            InventoryMenu.Singleton.CreateMenu((int)MenuPage.DetectiveMainMenu, "<align=left><color=#0000FF>[Detective Menu]</color> - <color=#FF0000>LEFT/RIGHT CLICK TO SELECT</color>", new List<MenuItem>
            {
                new MenuItem(ItemType.KeycardMTFCaptain, "NTFCommander = <color=#FF0000>EXIT</color>", OnExit),
                new MenuItem(ItemType.Adrenaline, "Adrenaline - <color=#00FF00>$25</color>", (player) => OnBuy(player, ItemType.Adrenaline, 25)),
                new MenuItem(ItemType.Painkillers, "Painkillers - <color=#00FF00>$30</color>", (player) => OnBuy(player, ItemType.Painkillers, 30)),
                new MenuItem(ItemType.Jailbird, "Mind-Scanner - <color=#00FF00>$100</color> (reveals the role of an alive player. one time use)", (player) => OnBuy(player, ItemType.Jailbird, 100)),
                new MenuItem(ItemType.GunShotgun, "Shotgun - <color=#00FF00>$150</color>", (player) => OnBuy(player, ItemType.GunShotgun, 150)),
                new MenuItem(ItemType.MicroHID, "DNA-Scanner - <color=#00FF00>$500</color> (reveals who killed the victim when scanned)", (player) => OnBuy(player, ItemType.MicroHID, 500))
            });
            InventoryMenu.Singleton.CreateMenu((int)MenuPage.TraitorMainMenu, "<align=left><color=#FF0000>[Traitor Menu]</color> - <color=#FF0000>LEFT/RIGHT CLICK TO SELECT</color>", new List<MenuItem>
            {
                new MenuItem(ItemType.KeycardChaosInsurgency, "ChaosInsurgency = <color=#FF0000>EXIT</color>", OnExit),
                new MenuItem(ItemType.Medkit, "Medkit = Heals Menu", (player) => { ShowMenu(player, MenuPage.TraitorHeals); return false; }),
                new MenuItem(ItemType.GunCom45, "GunCom45 = Weapons Menu", (player) => { ShowMenu(player, MenuPage.TraitorWeapons); return false; }),
                new MenuItem(ItemType.Coin, "Coin = Other Menu", (player) => { ShowMenu(player, MenuPage.TraitorOther); return false; }),
            });
            InventoryMenu.Singleton.CreateMenu((int)MenuPage.TraitorHeals, "<align=left><color=#FF0000>[Traitor Heals]</color> - <color=#FF0000>LEFT/RIGHT CLICK TO SELECT</color>", new List<MenuItem>
            {
                new MenuItem(ItemType.KeycardChaosInsurgency, "ChaosInsurgency = <color=#FF0000>0EXIT</color>", OnExit),
                new MenuItem(ItemType.Adrenaline, "Adrenaline - <color=#00FF00>$75</color>", (player) => OnBuy(player, ItemType.Adrenaline, 75)),
                new MenuItem(ItemType.Painkillers, "Painkillers - <color=#00FF00>$100</color>", (player) => OnBuy(player, ItemType.Painkillers, 100)),
                new MenuItem(ItemType.Medkit, "Medkit - <color=#00FF00>$150</color>", (player) => OnBuy(player, ItemType.Medkit, 150)),
                new MenuItem(ItemType.AntiSCP207, "Anti-SCP207 - <color=#00FF00>$300</color>", (player) => OnBuy(player, ItemType.AntiSCP207, 300)),
            });
            InventoryMenu.Singleton.CreateMenu((int)MenuPage.TraitorWeapons, "<align=left><color=#FF0000>[Traitor Weapons]</color> - <color=#FF0000>LEFT/RIGHT CLICK TO SELECT</color>", new List<MenuItem>
            {
                new MenuItem(ItemType.KeycardChaosInsurgency, "ChaosInsurgency = <color=#FF0000>EXIT</color>", OnExit),
                new MenuItem(ItemType.GrenadeFlash, "Flash Grenade - <color=#00FF00>$30</color>", (player) => OnBuy(player, ItemType.GrenadeFlash, 30)),
                new MenuItem(ItemType.GrenadeHE, "Explosive Grenade - <color=#00FF00>$50</color>", (player) => OnBuy(player, ItemType.GrenadeHE, 50)),
                new MenuItem(ItemType.SCP244a, "SCP244 - <color=#00FF00>$150</color>", (player) => OnBuy(player, ItemType.SCP244a, 150)),
                new MenuItem(ItemType.GunShotgun, "Shotgun - <color=#00FF00>$250</color>", (player) => OnBuy(player, ItemType.GunShotgun, 250)),
                new MenuItem(ItemType.SCP018, "SCP018 - <color=#00FF00>$300</color>", (player) => OnBuy(player, ItemType.SCP018, 300)),
            });
            InventoryMenu.Singleton.CreateMenu((int)MenuPage.TraitorOther, "<align=left><color=#FF0000>[Traitor Other]</color> - <color=#FF0000>LEFT/RIGHT CLICK TO SELECT</color>", new List<MenuItem>
            {
                new MenuItem(ItemType.KeycardChaosInsurgency, "ChaosInsurgency = <color=#FF0000>EXIT</color>", OnExit),
                new MenuItem(ItemType.ArmorHeavy, "Heavy Armor - <color=#00FF00>$70</color>", (player) => OnBuy(player, ItemType.ArmorHeavy, 30)),
                new MenuItem(ItemType.SCP1853, "SCP1853 - <color=#00FF00>$150</color>", (player) => OnBuy(player, ItemType.SCP1853, 150)),
                new MenuItem(ItemType.SCP1576, "SCP1576 - <color=#00FF00>$250</color>", (player) => OnBuy(player, ItemType.SCP1576, 250)),
                new MenuItem(ItemType.SCP268, "SCP268 - <color=#00FF00>$350</color>", (player) => OnBuy(player, ItemType.SCP268, 350)),
            });

        }

        public static void SaveInventory(Player player)
        {
            if (!saved_items.ContainsKey(player.PlayerId))
            {
                saved_ammo.Add(player.PlayerId, new List<Ammo>());
                saved_items.Add(player.PlayerId, new List<ItemBase>());
            }
            else
            {
                saved_items[player.PlayerId].Clear();
                saved_ammo[player.PlayerId].Clear();
            }
            foreach (var s in player.ReferenceHub.inventory.UserInventory.Items.Keys.ToList())
            {
                saved_items[player.PlayerId].Add(player.ReferenceHub.inventory.UserInventory.Items[s]);
                player.ReferenceHub.inventory.UserInventory.Items.Remove(s);
            }
            foreach (var t in player.ReferenceHub.inventory.UserInventory.ReserveAmmo.Keys.ToList())
            {
                saved_ammo[player.PlayerId].Add(new Ammo { type = t, amount = player.ReferenceHub.inventory.UserInventory.ReserveAmmo[t] });
                player.ReferenceHub.inventory.UserInventory.ReserveAmmo.Remove(t);
            }
        }

        public static void LoadInventory(Player player)
        {
            player.ClearInventory();
            if (saved_items.ContainsKey(player.PlayerId))
            {
                saved_items[player.PlayerId].Reverse();
                foreach (var item in saved_items[player.PlayerId])
                    player.ReferenceHub.inventory.UserInventory.Items.Add(item.ItemSerial, item);
                player.ReferenceHub.inventory.ServerSendItems();
                foreach (var ammo in saved_ammo[player.PlayerId])
                    player.ReferenceHub.inventory.UserInventory.ReserveAmmo.Add(ammo.type, ammo.amount);
                player.ReferenceHub.inventory.ServerSendAmmo();
            }
        }

        public static void ShowMenu(Player player, MenuPage page)
        {
            int line_count = InventoryMenu.Singleton.GetInfo((int)page).broadcast_lines;
            InventoryMenu.Singleton.ShowMenu(player, (int)page);
            BroadcastOverride.BroadcastLine(player, line_count + 1, 60 * 60, BroadcastPriority.High, "<color=#00FF00>Cash: $" + GetPlayerCash(player) + "</color>");
            BroadcastOverride.UpdateIfDirty(player);
        }

        public static void Reset()
        {
            player_cash.Clear();
        }

        public static void RewardCash(Player player, int amount, string reason)
        {
            if (!player_cash.ContainsKey(player.PlayerId))
                player_cash.Add(player.PlayerId, amount);
            else
                player_cash[player.PlayerId] += amount;
            Timing.CallDelayed(0.0f,()=>
            {
                BroadcastOverride.BroadcastLine(player, 6, 7.0f, BroadcastPriority.Medium, reason);
                BroadcastOverride.UpdateIfDirty(player);
            });
        }

        private static int GetPlayerCash(Player player)
        {
            int cash = 0;
            player_cash.TryGetValue(player.PlayerId, out cash);
            return cash;
        }
    }
}
