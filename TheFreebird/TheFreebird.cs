using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MEC;
using PluginAPI.Core;
using PlayerRoles;

namespace TheRiptide
{
    public class TheFreebird:IComparable
    {
        [PluginEntryPoint("The Freebird", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        static int freebird_dclass = -1;

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            freebird_dclass = -1;
            int attempts = 0;
            Timing.CallDelayed(0.1f, () =>
            {
                while (freebird_dclass == -1)
                {
                    Player random = Player.GetPlayers().RandomItem();
                    if (random.Role == RoleTypeId.ClassD && !random.TemporaryData.Contains("custom_class"))
                    {
                        freebird_dclass = random.PlayerId;
                        random.TemporaryData.Add("custom_class", this);
                        random.SendBroadcast("[The Freebird] check inv.", 15, shouldClearPrevious: true);
                        random.AddItem(ItemType.Jailbird);
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
            if (player != null && player.PlayerId == freebird_dclass && newRole != RoleTypeId.ClassD)
            {
                freebird_dclass = -1;
                player.TemporaryData.Remove("custom_class");
            }
        }

        public int CompareTo(object obj)
        {
            return Comparer<TheFreebird>.Default.Compare(this, obj as TheFreebird);
        }
    }
}
