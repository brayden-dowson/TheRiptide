using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide
{
    public static class EnumExtensions
    {
        public enum WeaponType : byte
        {
            None,
            Com15,
            Com18,
            Com45,
            FSP9,
            Crossvec,
            E11SR,
            FRMG0,
            A7,
            AK,
            Shotgun,
            Revolver,
            Logicer,
            MicroHID,
            ParticleDisruptor,
            Jailbird,
        }

        public enum MedicalType : byte
        {
            None,
            Medkit,
            Painkillers,
            Adrenaline,
        }

        public enum ScpType : byte
        {
            None,
            SCP018,
            SCP207,
            AntiSCP207,
            SCP244a,
            SCP244b,
            SCP268,
            SCP500,
            SCP1576,
            SCP1853,
            SCP2176,
        }

        //public enum CandyType : byte
        //{
        //    None,
        //    Blue,
        //    Green,
        //    Purple,
        //    Rainbow,
        //    Red,
        //    Yellow,
        //}

        public enum OtherType : byte
        {
            None,
            Flashlight,
            GrenadeHE,
            GrenadeFlash,
        }

        public enum Category : byte
        {
            None,
            Weapon,
            Medical,
            SCP,
            Other
        }

        public static Dictionary<ItemType, Category> ItemTypeToCategory = new Dictionary<ItemType, Category>
        {
            { ItemType.GunCOM15, Category.Weapon },
            { ItemType.GunCOM18, Category.Weapon },
            { ItemType.GunCom45, Category.Weapon },
            { ItemType.GunFSP9, Category.Weapon },
            { ItemType.GunCrossvec, Category.Weapon },
            { ItemType.GunE11SR, Category.Weapon },
            { ItemType.GunFRMG0, Category.Weapon },
            { ItemType.GunA7, Category.Weapon },
            { ItemType.GunAK, Category.Weapon },
            { ItemType.GunShotgun, Category.Weapon },
            { ItemType.GunRevolver, Category.Weapon },
            { ItemType.GunLogicer, Category.Weapon },
            { ItemType.MicroHID, Category.Weapon },
            { ItemType.ParticleDisruptor, Category.Weapon },
            { ItemType.Jailbird, Category.Weapon },

            { ItemType.Medkit, Category.Medical },
            { ItemType.Painkillers, Category.Medical },
            { ItemType.Adrenaline, Category.Medical },

            { ItemType.SCP018, Category.SCP },
            { ItemType.SCP207, Category.SCP },
            { ItemType.AntiSCP207, Category.SCP },
            { ItemType.SCP244a, Category.SCP },
            { ItemType.SCP244b, Category.SCP },
            { ItemType.SCP268, Category.SCP },
            { ItemType.SCP500, Category.SCP },
            { ItemType.SCP1576, Category.SCP },
            { ItemType.SCP1853, Category.SCP },
            { ItemType.SCP2176, Category.SCP },

            { ItemType.Flashlight, Category.Other },
            { ItemType.GrenadeHE, Category.Other },
            { ItemType.GrenadeFlash, Category.Other },
        };

        public static List<ItemType> WeaponToItem;
        public static List<ItemType> MedicalToItem;
        public static List<ItemType> SCPToItem;
        public static List<ItemType> OtherToItem;

        public static List<byte> ItemToSpecific;

        static EnumExtensions()
        {
            WeaponToItem = BuildCategoryLut(Category.Weapon);
            MedicalToItem = BuildCategoryLut(Category.Medical);
            SCPToItem = BuildCategoryLut(Category.SCP);
            OtherToItem = BuildCategoryLut(Category.Other);
            ItemToSpecific = BuildItemLut();
        }

        public static Category ItemCategory(this ItemType type)
        {
            if (ItemTypeToCategory.ContainsKey(type))
                return ItemTypeToCategory[type];
            return Category.None;
        }

        public static WeaponType ToWeaponType(this ItemType type)
        {
            if (type.ItemCategory() == Category.Weapon)
                return (WeaponType)ItemToSpecific[(int)type];
            return WeaponType.None;
        }

        public static ItemType ToItemType(this WeaponType type)
        {
            return WeaponToItem[(byte)type];
        }

        public static MedicalType ToMedicalType(this ItemType type)
        {
            if (type.ItemCategory() == Category.Medical)
                return (MedicalType)ItemToSpecific[(int)type];
            return MedicalType.None;
        }

        public static ItemType ToItemType(this MedicalType type)
        {
            return MedicalToItem[(byte)type];
        }

        public static ScpType ToSCPType(this ItemType type)
        {
            if (type.ItemCategory() == Category.SCP)
                return (ScpType)ItemToSpecific[(int)type];
            return ScpType.None;
        }

        public static ItemType ToItemType(this ScpType type)
        {
            return SCPToItem[(byte)type];
        }

        public static OtherType ToOtherType(this ItemType type)
        {
            if (type.ItemCategory() == Category.Other)
                return (OtherType)ItemToSpecific[(int)type];
            return OtherType.None;
        }

        public static ItemType ToItemType(this OtherType type)
        {
            return OtherToItem[(byte)type];
        }

        private static List<ItemType> BuildCategoryLut(Category category)
        {
            List<ItemType> lut = new List<ItemType>();
            lut.Add(ItemType.None);
            foreach (var d in ItemTypeToCategory)
                if (d.Value == category)
                    lut.Add(d.Key);
            return lut;
        }

        private static List<byte> BuildItemLut()
        {
            int size = System.Enum.GetValues(typeof(ItemType)).ToArray<int>().Max() + 1;
            List<byte> lut = new List<byte>(size);
            lut.AddRange(Enumerable.Repeat((byte)0, size));
            foreach (var d in ItemTypeToCategory)
            {
                switch (d.Key.ItemCategory())
                {
                    case Category.Weapon: lut[(int)d.Key] = (byte)WeaponToItem.IndexOf(d.Key); break;
                    case Category.Medical: lut[(int)d.Key] = (byte)MedicalToItem.IndexOf(d.Key); break;
                    case Category.SCP: lut[(int)d.Key] = (byte)SCPToItem.IndexOf(d.Key); break;
                    case Category.Other: lut[(int)d.Key] = (byte)OtherToItem.IndexOf(d.Key); break;
                }
            }
            return lut;
        }
    }
}
