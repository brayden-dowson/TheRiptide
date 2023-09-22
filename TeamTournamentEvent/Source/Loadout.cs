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

namespace TheRiptide
{
    public class Loadout
    {
        public static Dictionary<int, Loadout> AllLoadouts = new Dictionary<int, Loadout>();

        public WeaponType weapon;
        public MedicalType medical;
        public ScpType scp;
        public CandyKindID candy;
        public OtherType other;

        public void Reset()
        {
            weapon = WeaponType.None;
            medical = MedicalType.None;
            scp = ScpType.None;
            candy = CandyKindID.None;
            other = OtherType.None;
        }

        public void SetItem(ItemType type)
        {
            switch (type.ItemCategory())
            {
                case Category.Weapon: weapon = type.ToWeaponType(); break;
                case Category.Medical: medical = type.ToMedicalType(); break;
                case Category.SCP: scp = type.ToSCPType(); break;
                case Category.Other: other = type.ToOtherType(); break;
            }
        }

        public void SetCandy(CandyKindID type)
        {
            candy = type;
        }

        public void RemoveItem(ItemType type)
        {
            switch (type.ItemCategory())
            {
                case Category.Weapon: weapon = WeaponType.None; break;
                case Category.Medical: medical = MedicalType.None; break;
                case Category.SCP: scp = ScpType.None; break;
                case Category.Other: other = OtherType.None; break;
            }
        }

        public void RemoveCandy(CandyKindID type)
        {
            if (candy == type)
                candy = CandyKindID.None;
        }

        public void UpdateInventoy(Player player, bool grant_ammo)
        {
            bool has_weapon = false;
            bool has_medical = false;
            bool has_scp = false;
            bool has_other = false;
            bool has_candy = false;
            foreach (var serial in player.ReferenceHub.inventory.UserInventory.Items.Keys.ToList())
            {
                ItemBase item = player.ReferenceHub.inventory.UserInventory.Items[serial];

                switch (item.ItemTypeId.ItemCategory())
                {
                    case Category.Weapon:
                        has_weapon = true;
                        if (weapon != item.ItemTypeId.ToWeaponType())
                        {
                            player.RemoveItem(new Item(item));
                            if (weapon != WeaponType.None)
                                AddWeapon(player, weapon.ToItemType(), grant_ammo);
                        }
                        break;
                    case Category.Medical:
                        has_medical = true;
                        if (medical != item.ItemTypeId.ToMedicalType())
                        {
                            player.RemoveItem(new Item(item));
                            if (medical != MedicalType.None)
                                player.AddItem(medical.ToItemType());
                        }
                        break;
                    case Category.SCP:
                        has_scp = true;
                        if (scp != item.ItemTypeId.ToSCPType())
                        {
                            player.RemoveItem(new Item(item));
                            if (scp != ScpType.None)
                                player.AddItem(scp.ToItemType());
                        }
                        break;
                    case Category.Other:
                        has_other = true;
                        if (other != item.ItemTypeId.ToOtherType())
                        {
                            player.RemoveItem(new Item(item));
                            if (other != OtherType.None)
                                player.AddItem(other.ToItemType());
                        }
                        break;
                }

                if (item.ItemTypeId == ItemType.SCP330 && item is Scp330Bag bag)
                {
                    has_candy = true;
                    if (bag.Candies[0] != candy)
                    {
                        if (candy != CandyKindID.None)
                            bag.Candies[0] = candy;
                        else
                            bag.Candies.Clear();
                        bag.ServerRefreshBag();
                    }
                }
            }

            if (!has_weapon && weapon != WeaponType.None)
                AddWeapon(player, weapon.ToItemType(), grant_ammo);
            if (!has_medical && medical != MedicalType.None)
                player.AddItem(medical.ToItemType());
            if (!has_scp && scp != ScpType.None)
                player.AddItem(scp.ToItemType());
            if (!has_other && other != OtherType.None)
                player.AddItem(other.ToItemType());
            if (!has_candy && candy != CandyKindID.None)
            {
                Scp330Bag b = player.AddItem(ItemType.SCP330) as Scp330Bag;
                b.Candies[0] = candy;
            }
        }

        public void Broadcast(Player player)
        {
            Broadcast(player, "");
        }

        public void Broadcast(Player player, string extra)
        {
            string info = "<color=#b7eb8f>Loadout\n" +
    "</color><color=#87e8de>Weapon:</color> " + (weapon == WeaponType.None ? "<color=#FF0000>" : "<color=#b7eb8f>") + weapon +
    "</color> <color=#87e8de>Medical:</color> " + (medical == MedicalType.None ? "<color=#FF0000>" : "<color=#b7eb8f>") + medical +
    "</color>\n<color=#87e8de> Candy:</color> " + (candy == CandyKindID.None ? "<color=#FF0000>" : "<color=#b7eb8f>") + candy +
    "</color> <color=#87e8de> SCP:</color> " + (scp == ScpType.None ? "<color=#FF0000>" : "<color=#b7eb8f>") + scp.ToString().Replace("SCP", "") +
    "</color> <color=#87e8de> Other:</color> " + (other == OtherType.None ? "<color=#FF0000>" : "<color=#b7eb8f>") + other + "</color>" + extra;
            player.SendBroadcast(info, 300, shouldClearPrevious: true);
        }

        private void AddWeapon(Player player, ItemType type, bool grant_ammo)
        {
            ItemBase firearm;
            if (type == ItemType.ParticleDisruptor)
            {
                firearm = player.AddItem(type);
                var pd = firearm as ParticleDisruptor;
                pd.ApplyAttachmentsCode(0, true);
                if (grant_ammo)
                    pd.Status = new FirearmStatus(5, FirearmStatusFlags.MagazineInserted, pd.GetCurrentAttachmentsCode());
            }
            else if (IsGun(type))
                firearm = AddFirearm(player, type, grant_ammo);
            else
                firearm = player.AddItem(type);

            Timing.CallDelayed(0.0f, () => player.CurrentItem = firearm);
        }

        public static Loadout Get(Player player)
        {
            if (!AllLoadouts.ContainsKey(player.PlayerId))
                AllLoadouts.Add(player.PlayerId, new Loadout());
            return AllLoadouts[player.PlayerId];
        }
    }
}
