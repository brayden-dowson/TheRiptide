using MEC;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public class HeadSecurity: IComparable
    {
        [PluginEntryPoint("Head Security", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        static int head_guard = -1;

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            head_guard = -1;
            int attempts = 0;
            Timing.CallDelayed(0.1f, () =>
            {
                while (head_guard == -1)
                {
                    Player random = Player.GetPlayers().RandomItem();
                    if (random.Role == RoleTypeId.FacilityGuard && !random.TemporaryData.Contains("custom_class"))
                    {
                        head_guard = random.PlayerId;
                        random.TemporaryData.Add("custom_class", this);
                        random.SendBroadcast("[Head Security] check inv.", 15, shouldClearPrevious: true);
                        random.AddItem(ItemType.ArmorCombat);
                        RemoveItem(random, ItemType.ArmorLight);
                        RemoveItem(random, ItemType.GunFSP9);
                        AddOrDropFirearm(random, ItemType.GunCOM18, true);
                        AddOrDropFirearm(random, ItemType.GunShotgun, true);
                    }
                    else
                    {
                        attempts++;
                        if (attempts >= 100)
                            break;
                    }
                }
            });
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        void OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId newRole, RoleChangeReason reason)
        {
            if (player != null && player.PlayerId == head_guard && newRole != RoleTypeId.FacilityGuard)
            {
                head_guard = -1;
                player.TemporaryData.Remove("custom_class");
            }
        }

        public int CompareTo(object obj)
        {
            return Comparer<HeadSecurity>.Default.Compare(this, obj as HeadSecurity);
        }
    }
}
