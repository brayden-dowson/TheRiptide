using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Usables.Scp330;
using MEC;
using PluginAPI.Core;
using PluginAPI.Core.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TheRiptide.EnumExtensions;
using static TheRiptide.Utility;
using static TheRiptide.StaticTranslation;

namespace TheRiptide
{
    class Bans
    {
        public static Dictionary<int, Bans> AllItemBans = new Dictionary<int, Bans>();

        public Dictionary<Category, HashSet<ItemType>> items = new Dictionary<Category, HashSet<ItemType>>()
        {
            {Category.Weapon, new HashSet<ItemType>() },
            {Category.Medical, new HashSet<ItemType>() },
            {Category.SCP, new HashSet<ItemType>() },
            {Category.Other, new HashSet<ItemType>() }
        };
        public HashSet<CandyKindID> candies = new HashSet<CandyKindID>();
        public HashSet<Zone> zones = new HashSet<Zone>();

        public void Reset()
        {
            foreach (var c in items)
                c.Value.Clear();
            candies.Clear();
            zones.Clear();
        }

        public bool SetZoneBan(ItemType type)
        {
            switch(type)
            {
                case ItemType.KeycardJanitor: zones.Clear(); return true;
                case ItemType.KeycardGuard: zones = new HashSet<Zone> { Zone.Surface }; return true;
                case ItemType.KeycardMTFPrivate: zones = new HashSet<Zone> { Zone.Entrance }; return true;
                case ItemType.KeycardMTFOperative: zones = new HashSet<Zone> { Zone.Heavy }; return true;
                case ItemType.KeycardMTFCaptain: zones = new HashSet<Zone> { Zone.Light }; return true;
            }
            return false;
        }

        public bool TryAddItemBan(ItemType type)
        {
            Category c = type.ItemCategory();
            if (c == Category.None)
                return false;
            if (items[c].Count < EventHandler.config.TeamCategoryBansLimit[c])
                return items[c].Add(type);
            return false;
        }

        public bool TryAddCandyBan(CandyKindID type)
        {
            if (candies.Count < EventHandler.config.TeamCandyBanLimit)
                return candies.Add(type);
            return false;
        }

        public bool TryRemoveItemBan(ItemType type)
        {
            Category c = type.ItemCategory();
            if (c == Category.None)
                return false;

            return items[c].Remove(type);
        }

        public bool TryRemoveCandyBan(CandyKindID type)
        {
            return candies.Remove(type);
        }

        public void RemoveAllCandyBans()
        {
            candies.Clear();
        }

        public void UpdateInventoy(Player player)
        {
            var player_items = player.ReferenceHub.inventory.UserInventory.Items.Values.ToDictionary(i => i, i => i.ItemTypeId);
            Dictionary<Category, HashSet<ItemType>> missing_from_inventory = items.ToDictionary(b => b.Key, b => b.Value.Except(player_items.Values).ToHashSet());
            Dictionary<Category, HashSet<ItemBase>> missing_from_bans = new Dictionary<Category, HashSet<ItemBase>>();
            foreach (var b in items)
                missing_from_bans[b.Key] = player_items.Where(i => i.Value.ItemCategory() == b.Key && !b.Value.Contains(i.Value)).Select(i => i.Key).ToHashSet();

            foreach (var b in missing_from_bans.Values)
                foreach (var i in b)
                    player.RemoveItem(new Item(i));

            foreach (var b in missing_from_inventory.Values)
            {
                foreach (var i in b)
                {
                    if (player.IsInventoryFull)
                        break;
                    AddItem(player, i);
                }
            }

            Scp330Bag bag = null;
            if (!Scp330Bag.TryGetBag(player.ReferenceHub, out bag))
            {
                if (!candies.IsEmpty() && !player.IsInventoryFull)
                    bag = player.AddItem(ItemType.SCP330) as Scp330Bag;
            }
            else if (candies.IsEmpty())
                player.RemoveItem(new Item(bag));

            if (bag != null)
            {
                HashSet<CandyKindID> missing_candies_from_bag = candies.Except(bag.Candies).ToHashSet();
                HashSet<CandyKindID> missing_candies_from_bans = bag.Candies.Except(candies).ToHashSet();

                foreach (var c in missing_candies_from_bans)
                    bag.Candies.Remove(c);
                foreach (var c in missing_candies_from_bag)
                    if (bag.Candies.Count() < Scp330Bag.MaxCandies)
                        bag.Candies.Add(c);
                bag.ServerRefreshBag();
            }
        }

        public static void BroadcastTeam(Team team)
        {
            string team_votes = GetTeamVotes(team);
            string team_bans = GetTeamBans(team);
            string zone_votes = GetTeamZoneVotes(team);
            string zone_ban = GetTeamZoneBanStr(team);
            foreach (var user in team.Users)
            {
                Player p = null;
                if (Player.TryGet(user, out p))
                {
                    var b = Get(p);
                    b.Broadcast(p, team_votes, team_bans, zone_votes, zone_ban);
                }
            }
        }

        public void Broadcast(Player player, string team_votes, string team_bans, string zone_votes, string zone_ban)
        {
            var limits = EventHandler.config.TeamCategoryBansLimit;
            //string vote_limits = "weapons " + limits[Category.Weapon] + ", medical " + limits[Category.Medical] + ", scp " + limits[Category.SCP] + ", other " + limits[Category.Other] + ", candy " + EventHandler.config.TeamCandyBanLimit;
            string weapon_votes = "[" + string.Join(", ", items[Category.Weapon].ToList().ConvertAll(t => t.ToString().Replace("Gun", ""))) + "]";
            string medical_votes = "[" + string.Join(", ", items[Category.Medical]) + "]";
            string scp_votes = "[" + string.Join(", ", items[Category.SCP].ToList().ConvertAll(t => t.ToString().Replace("SCP", ""))) + "]";
            string other_votes = "[" + string.Join(", ", items[Category.Other]) + "]";
            string candy_votes = "[" + string.Join(", ", candies) + "]";
            string your_votes = string.Join(" ", weapon_votes, medical_votes, scp_votes, other_votes, candy_votes);

            //string info = "<size=29><color=#ff8a62>Bans - " +
            //    "</color><color=#b6ff61>Your votes: " + string.Join(" ", weapon_votes, medical_votes, scp_votes, other_votes, candy_votes) + "\n" +
            //    "</color><color=#ffb84c>Team votes: <color=#ffb84c>" + team_votes + "</color>\n" +
            //    "</color><color=#ff6565>Item Bans: <color=#ff6565>" + team_bans + "</color>\n" +
            //    "</color><color=#f5ff5b>Zones: <color=#b6ff61>[" + string.Join("", zones) + "]</color> <color=#ffb84c>Votes: " + zone_votes + "</color> <color=#ff6565>Ban: " + zone_ban;
            player.SendBroadcast(Translation.VoteBanFormat.
                Replace("{your_votes}", your_votes).
                Replace("{team_votes}", team_votes).
                Replace("{team_bans}", team_bans).
                Replace("{your_zone_vote}", string.Join("", zones)).
                Replace("{team_zone_votes}", zone_votes).
                Replace("{zone_bans}", zone_ban), 300, shouldClearPrevious: true);
        }

        public static Bans Get(Player player)
        {
            if (!AllItemBans.ContainsKey(player.PlayerId))
                AllItemBans.Add(player.PlayerId, new Bans());
            return AllItemBans[player.PlayerId];
        }

        public static string GetTeamZoneVotes(Team team)
        {
            string team_votes = "";
            Dictionary<Zone, int> zone_tally = new Dictionary<Zone, int>();

            foreach (var user in team.Users)
            {
                Player p = null;
                Bans bans = null;
                if (Player.TryGet(user, out p) && AllItemBans.TryGetValue(p.PlayerId, out bans))
                {
                    foreach (var z in bans.zones)
                    {
                        if (zone_tally.ContainsKey(z))
                            zone_tally[z]++;
                        else
                            zone_tally.Add(z, 1);
                    }
                }
            }
            foreach (var zt in zone_tally)
                team_votes += zt.Key.ToString() + "(" + zt.Value + ") ";

            return team_votes;
        }

        public static HashSet<Zone> GetTeamZoneBan(Team team)
        {
            Dictionary<Zone, int> zone_tally = new Dictionary<Zone, int>();

            foreach (var user in team.Users)
            {
                Player p = null;
                Bans bans = null;
                if (Player.TryGet(user, out p) && AllItemBans.TryGetValue(p.PlayerId, out bans))
                {
                    foreach (var z in bans.zones)
                    {
                        if (zone_tally.ContainsKey(z))
                            zone_tally[z]++;
                        else
                            zone_tally.Add(z, 1);
                    }
                }
            }
            if (!zone_tally.Any(zt => zt.Value >= EventHandler.config.BanThreshold))
                return new HashSet<Zone>();
            List<KeyValuePair<Zone, int>> sorted_zone_tally = zone_tally.ToList();
            sorted_zone_tally.Sort((l, r) => r.Value - l.Value);
            return new HashSet<Zone> { sorted_zone_tally.First().Key };
        }

        public static string GetTeamZoneBanStr(Team team)
        {
            var zone = GetTeamZoneBan(team);
            return zone.IsEmpty() ? "None" : zone.First().ToString();
        }


        public static string GetTeamVotes(Team team)
        {
            string team_votes = "";
            Dictionary<ItemType, int> item_tally = new Dictionary<ItemType, int>();
            Dictionary<CandyKindID, int> candy_tally = new Dictionary<CandyKindID, int>();
            foreach (var user in team.Users)
            {
                Player p = null;
                Bans bans = null;
                if (Player.TryGet(user, out p) && AllItemBans.TryGetValue(p.PlayerId, out bans))
                {
                    foreach (var ban in bans.items.Values)
                    {
                        foreach (var i in ban)
                        {
                            if (item_tally.ContainsKey(i))
                                item_tally[i]++;
                            else
                                item_tally.Add(i, 1);
                        }
                    }

                    foreach (var c in bans.candies)
                    {
                        if (candy_tally.ContainsKey(c))
                            candy_tally[c]++;
                        else
                            candy_tally.Add(c, 1);
                    }
                }
            }
            foreach (var it in item_tally)
                team_votes += it.Key.ToString().Replace("Gun", "") + "(" + it.Value + ") ";
            foreach (var ct in candy_tally)
                team_votes += ct.Key + "(" + ct.Value + ") ";

            return team_votes;
        }

        public static HashSet<ItemType> GetTeamItemBans(Team team)
        {
            Dictionary<ItemType, int> item_tally = new Dictionary<ItemType, int>();
            foreach (var user in team.Users)
            {
                Player p = null;
                Bans bans = null;
                if (Player.TryGet(user, out p) && AllItemBans.TryGetValue(p.PlayerId, out bans))
                {
                    foreach (var ban in bans.items.Values)
                    {
                        foreach (var i in ban)
                        {
                            if (item_tally.ContainsKey(i))
                                item_tally[i]++;
                            else
                                item_tally.Add(i, 1);
                        }
                    }
                }
            }

            Dictionary<Category, int> category_tally = new Dictionary<Category, int>()
            { { Category.Weapon, 0}, { Category.Medical, 0}, { Category.SCP, 0}, { Category.Other, 0} };
            HashSet<ItemType> items = new HashSet<ItemType>();
            List<KeyValuePair<ItemType, int>> sorted_item_tally = item_tally.ToList();
            sorted_item_tally.Sort((l, r) => r.Value - l.Value);
            foreach (var it in sorted_item_tally)
            {
                if (it.Value >= EventHandler.config.BanThreshold)
                {
                    Category c = it.Key.ItemCategory();
                    category_tally[c]++;
                    if (category_tally[c] <= EventHandler.config.TeamCategoryBansLimit[c])
                        items.Add(it.Key);
                }
            }
            return items;
        }

        public static HashSet<CandyKindID> GetTeamCandyBans(Team team)
        {
            Dictionary<CandyKindID, int> candy_tally = new Dictionary<CandyKindID, int>();
            foreach (var user in team.Users)
            {
                Player p = null;
                Bans bans = null;
                if (Player.TryGet(user, out p) && AllItemBans.TryGetValue(p.PlayerId, out bans))
                {
                    foreach (var c in bans.candies)
                    {
                        if (candy_tally.ContainsKey(c))
                            candy_tally[c]++;
                        else
                            candy_tally.Add(c, 1);
                    }
                }
            }

            HashSet<CandyKindID> candies = new HashSet<CandyKindID>();
            foreach (var ct in candy_tally)
                if (ct.Value >= EventHandler.config.BanThreshold)
                    candies.Add(ct.Key);
            return candies;
        }

        public static string GetTeamBans(Team team)
        {
            string team_bans = "";
            Dictionary<ItemType, int> item_tally = new Dictionary<ItemType, int>();
            Dictionary<CandyKindID, int> candy_tally = new Dictionary<CandyKindID, int>();
            foreach (var user in team.Users)
            {
                Player p = null;
                Bans bans = null;
                if (Player.TryGet(user, out p) && AllItemBans.TryGetValue(p.PlayerId, out bans))
                {
                    foreach (var ban in bans.items.Values)
                    {
                        foreach (var i in ban)
                        {
                            if (item_tally.ContainsKey(i))
                                item_tally[i]++;
                            else
                                item_tally.Add(i, 1);
                        }
                    }

                    foreach (var c in bans.candies)
                    {
                        if (candy_tally.ContainsKey(c))
                            candy_tally[c]++;
                        else
                            candy_tally.Add(c, 1);
                    }
                }
            }

            foreach (var it in item_tally)
                if (it.Value >= EventHandler.config.BanThreshold)
                    team_bans += it.Key.ToString().Replace("Gun", "") + " ";
            foreach (var ct in candy_tally)
                if (ct.Value >= EventHandler.config.BanThreshold)
                    team_bans += ct.Key + " ";

            return team_bans;
        }

        private void AddItem(Player player, ItemType type)
        {
            ItemBase item;
            if (type == ItemType.ParticleDisruptor)
            {
                item = player.AddItem(type);
                var firearm = item as ParticleDisruptor;
                firearm.ApplyAttachmentsCode(0, true);
                firearm.Status = new FirearmStatus(5, FirearmStatusFlags.MagazineInserted, firearm.GetCurrentAttachmentsCode());
            }
            else if (IsGun(type))
                item = AddFirearm(player, type, false);
            else
                item = player.AddItem(type);
        }
    }
}
