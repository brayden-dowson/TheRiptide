using InventorySystem;
using InventorySystem.Configs;
using InventorySystem.Items;
using InventorySystem.Items.Armor;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Pickups;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Items;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace TheRiptide
{
    public static class Utility
    {
        public static List<Player> ReadyPlayers()
        {
            return Player.GetPlayers().Where(p => p.IsReady).ToList();
        }

        public static System.Type GetType(string typeName)
        {
            var type = System.Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }

        public static void ClearItemPickups()
        {
            ItemPickupBase[] items = Object.FindObjectsOfType<ItemPickupBase>();
            foreach (var item in items)
                NetworkServer.Destroy(item.gameObject);
        }

        public static void ClearRagdolls()
        {
            BasicRagdoll[] ragdolls = Object.FindObjectsOfType<BasicRagdoll>();
            foreach (var ragdoll in ragdolls)
                NetworkServer.Destroy(ragdoll.gameObject);
        }

        public static BasicRagdoll CreateRagdoll(ReferenceHub owner, RoleTypeId role, Vector3 position, Quaternion rotation, string msg)
        {
            PlayerRoleBase template;
            if (!PlayerRoleLoader.TryGetRoleTemplate(role, out template))
            {
                Log.Error("SpawnReadyUpRagdoll: could not get " + role + " ragdoll");
                return null;
            }

            GameObject obj = Object.Instantiate((template as IRagdollRole).Ragdoll.gameObject, position, rotation);
            BasicRagdoll ragdoll = obj.GetComponent<BasicRagdoll>();
            if (ragdoll == null)
            {
                Log.Error("SpawnReadyUpRagdoll: BasicRagdoll was null");
                return null;
            }
            ragdoll.NetworkInfo = new RagdollData(owner, new UniversalDamageHandler(0.0f, DeathTranslations.Unknown), role, position, rotation, msg, NetworkTime.time);
            return ragdoll;
        }

        public static void RemoveAttachment(Firearm firearm, AttachmentName name)
        {
            int at_bit = 0;
            AttachmentSlot slot = 0;
            foreach (var a in firearm.Attachments)
            {
                if (a.Name == name)
                {
                    slot = a.Slot;
                    break;
                }
                at_bit++;
            }
            if (at_bit == firearm.Attachments.Length)
                return;

            uint slot_mask = 0;
            int slot_bit = 0;
            foreach (var a in firearm.Attachments)
            {
                if (a.Slot == slot)
                    slot_mask |= (1U << slot_bit);
                slot_bit++;
            }

            uint code = (firearm.Status.Attachments & ~slot_mask);
            if (code != firearm.Status.Attachments)
            {
                firearm.ApplyAttachmentsCode(code, true);
                firearm.Status = new FirearmStatus(firearm.Status.Ammo, firearm.Status.Flags, code);
            }
        }

        public static void AddAttachment(Firearm firearm, AttachmentName name)
        {
            int at_bit = 0;
            AttachmentSlot slot = 0;
            foreach (var a in firearm.Attachments)
            {
                if (a.Name == name)
                {
                    slot = a.Slot;
                    break;
                }
                at_bit++;
            }
            if (at_bit == firearm.Attachments.Length)
                return;

            uint slot_mask = 0;
            int slot_bit = 0;
            foreach (var a in firearm.Attachments)
            {
                if (a.Slot == slot)
                    slot_mask |= (1U << slot_bit);
                slot_bit++;
            }

            uint code = (firearm.Status.Attachments & ~slot_mask) | (1U << at_bit);
            if (code != firearm.Status.Attachments)
            {
                firearm.ApplyAttachmentsCode(code, true);
                firearm.Status = new FirearmStatus(firearm.Status.Ammo, firearm.Status.Flags, code);
            }
        }

        public static void SetScale(Player player, float scale)
        {
            SetScale(player, new Vector3(scale, scale, scale));
        }

        public static void SetScale(Player player, Vector3 scale)
        {
            if (player.GameObject.transform.localScale == scale)
                return;

            try
            {
                player.GameObject.transform.localScale = scale;
                foreach (var p in Player.GetPlayers())
                    NetworkServer.SendSpawnMessage(player.ReferenceHub.networkIdentity, p.Connection);
            }
            catch (System.Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        public static void AddOrDropFirearm(Player player, ItemType type, bool grant_ammo)
        {
            if (!player.IsInventoryFull)
                AddFirearm(player, type, grant_ammo);
            else
            {
                Firearm firearm;
                if (InventoryItemLoader.TryGetItem(type, out firearm))
                {
                    if (grant_ammo)
                        player.SetAmmo(firearm.AmmoType, (ushort)player.GetAmmoLimit(firearm.AmmoType));
                    Transform t = player.GameObject.transform;
                    FirearmPickup pickup = Object.Instantiate(firearm.PickupDropModel, t.position, t.rotation) as FirearmPickup;
                    if (pickup != null)
                    {
                        uint attachment_code = AttachmentsServerHandler.PlayerPreferences[player.ReferenceHub][type];
                        pickup.NetworkStatus = new FirearmStatus(0, FirearmStatusFlags.None, attachment_code);
                        pickup.NetworkInfo = new PickupSyncInfo(type, 1.0f);
                        NetworkServer.Spawn(pickup.gameObject);
                    }
                }
            }
        }

        public static ushort GetStandardAmmoLimit(BodyArmor armor, ItemType ammo_type)
        {
            ushort limit = 0;
                if (!InventoryLimits.StandardAmmoLimits.TryGetValue(ammo_type, out limit))
                    return 200;
            if (armor != null)
            {
                foreach (BodyArmor.ArmorAmmoLimit ammoLimit in armor.AmmoLimits)
                {
                    if (ammoLimit.AmmoType == ammo_type)
                    {
                        limit += ammoLimit.Limit;
                        break;
                    }
                }
            }
            return limit;
        }

        public static Firearm AddFirearm(Player player, ItemType type, bool grant_ammo)
        {
            int ammo_reserve = 0;
            int load_ammo = 0;
            uint attachment_code = AttachmentsServerHandler.PlayerPreferences[player.ReferenceHub][type];
            Firearm firearm = player.AddItem(type) as Firearm;
            if (grant_ammo)
            {
                BodyArmor bodyArmor;
                ammo_reserve = GetStandardAmmoLimit(!player.ReferenceHub.inventory.TryGetBodyArmor(out bodyArmor) ? null : bodyArmor, GunAmmoType(type));//  player.GetAmmoLimit(firearm.AmmoType);
            }
            else
                ammo_reserve = player.GetAmmo(firearm.AmmoType);

            AttachmentsUtils.ApplyAttachmentsCode(firearm, attachment_code, true);
            load_ammo = math.min(ammo_reserve, firearm.AmmoManagerModule.MaxAmmo);
            firearm.Status = new FirearmStatus((byte)load_ammo, FirearmStatusFlags.MagazineInserted, attachment_code);
            ammo_reserve -= load_ammo;
            player.SetAmmo(firearm.AmmoType, (ushort)ammo_reserve);
            return firearm;
        }

        public static void AddOrDropItem(Player player, ItemType type)
        {
            if (player.IsInventoryFull)
            {
                ItemBase item;
                if (InventoryItemLoader.TryGetItem(type, out item))
                {
                    Transform t = player.GameObject.transform;
                    ItemPickupBase pickup = Object.Instantiate(item.PickupDropModel, t.position, t.rotation);
                    if (pickup != null)
                    {
                        pickup.NetworkInfo = new PickupSyncInfo(type, 1.0f);
                        NetworkServer.Spawn(pickup.gameObject);
                    }
                    else
                        Log.Error("could not convert PickupDropModel " + type.ToString() + " to AmmoPickup");
                }
                else
                    Log.Error("could not load ammo of type " + type.ToString());
            }
            else
                player.AddItem(type);
        }

        public static bool RemoveItem(Player player, ItemType type)
        {
            IEnumerable<ItemBase> matches = player.Items.Where((i) => i.ItemTypeId == type);
            if (matches.Count() >= 1)
            {
                player.RemoveItem(new Item(matches.First()));
                return true;
            }
            return false;
        }


        public static bool IsArmor(ItemType item)
        {
            return item == ItemType.ArmorLight || item == ItemType.ArmorCombat || item == ItemType.ArmorHeavy;
        }

        public static ItemType GetItemFromDamageHandler(DamageHandlerBase damage)
        {
            if (damage is FirearmDamageHandler firearm)
                return firearm.WeaponType;
            else if (damage is DisruptorDamageHandler)
                return ItemType.ParticleDisruptor;
            else if (damage is ExplosionDamageHandler)
                return ItemType.GrenadeHE;
            else if (damage is JailbirdDamageHandler)
                return ItemType.Jailbird;
            else if (damage is MicroHidDamageHandler)
                return ItemType.MicroHID;
            else if (damage is Scp018DamageHandler)
                return ItemType.SCP018;
            else
                return ItemType.None;
        }

        public static bool IsHumanRole(RoleTypeId role)
        {
            return role == RoleTypeId.ChaosConscript || role == RoleTypeId.ChaosMarauder || role == RoleTypeId.ChaosRepressor || role == RoleTypeId.ChaosRifleman ||
                role == RoleTypeId.ClassD || role == RoleTypeId.FacilityGuard || role == RoleTypeId.NtfCaptain || role == RoleTypeId.NtfPrivate ||
                role == RoleTypeId.NtfSergeant || role == RoleTypeId.NtfSpecialist || role == RoleTypeId.Scientist;
        }

        public static void AddItems(Player player, List<ItemType> items)
        {
            foreach (ItemType i in items)
                if (!player.IsInventoryFull || i == ItemType.SCP330)
                    player.AddItem(i);
        }

        public static bool IsGun(ItemType type)
        {
            bool result = false;
            switch (type)
            {
                case ItemType.GunCOM15:
                    result = true;
                    break;
                case ItemType.GunCOM18:
                    result = true;
                    break;
                case ItemType.GunCom45:
                    result = true;
                    break;
                case ItemType.GunFSP9:
                    result = true;
                    break;
                case ItemType.GunCrossvec:
                    result = true;
                    break;
                case ItemType.GunE11SR:
                    result = true;
                    break;
                case ItemType.GunAK:
                    result = true;
                    break;
                case ItemType.GunRevolver:
                    result = true;
                    break;
                case ItemType.GunShotgun:
                    result = true;
                    break;
                case ItemType.GunLogicer:
                    result = true;
                    break;
                case ItemType.ParticleDisruptor:
                    result = true;
                    break;
            }
            return result;
        }

        public static ItemType GunAmmoType(ItemType type)
        {
            ItemType ammo = ItemType.None;
            switch (type)
            {
                case ItemType.GunCOM15:
                case ItemType.GunCOM18:
                case ItemType.GunCom45:
                case ItemType.GunFSP9:
                case ItemType.GunCrossvec:
                    ammo = ItemType.Ammo9x19;
                    break;
                case ItemType.GunE11SR:
                    ammo = ItemType.Ammo556x45;
                    break;
                case ItemType.GunAK:
                case ItemType.GunLogicer:
                    ammo = ItemType.Ammo762x39;
                    break;
                case ItemType.GunRevolver:
                    ammo = ItemType.Ammo44cal;
                    break;
                case ItemType.GunShotgun:
                    ammo = ItemType.Ammo12gauge;
                    break;
            }
            return ammo;
        }
    }
}
