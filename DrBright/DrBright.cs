using MapGeneration;
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
    public class DrBright : IComparable
    {
        [PluginEntryPoint("Dr Bright", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        static int dr_bright = -1;
        //static UnityEngine.Vector3 offset = new UnityEngine.Vector3(-2.590f, 0.960f, 0.012f);

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            dr_bright = -1;
            int attempts = 0;
            Timing.CallDelayed(0.1f, () =>
            {
                while (dr_bright == -1)
                {
                    Player random = Player.GetPlayers().RandomItem();
                    if (random.Role == RoleTypeId.Scientist && !random.TemporaryData.Contains("custom_class"))
                    {
                        dr_bright = random.PlayerId;
                        random.TemporaryData.Add("custom_class", this);
                        random.SendBroadcast("[Dr Bright] check inv.", 15, shouldClearPrevious: true);
                        Teleport.Room(random, RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Name == RoomName.EzOfficeSmall).First());
                        RemoveItem(random, ItemType.KeycardScientist);
                        AddOrDropItem(random, ItemType.KeycardContainmentEngineer);
                        for (int i = 0; i < 2; i++)
                        {
                            switch (UnityEngine.Random.Range(0, 10))
                            {
                                case 0: AddOrDropItem(random, ItemType.SCP018); break;
                                case 1: AddOrDropItem(random, ItemType.SCP1576); break;
                                case 2: AddOrDropItem(random, ItemType.SCP1853); break;
                                case 3: AddOrDropItem(random, ItemType.SCP207); break;
                                case 4: AddOrDropItem(random, ItemType.SCP2176); break;
                                case 5: AddOrDropItem(random, ItemType.SCP244a); break;
                                case 6: AddOrDropItem(random, ItemType.SCP244b); break;
                                case 7: AddOrDropItem(random, ItemType.SCP268); break;
                                case 8: AddOrDropItem(random, ItemType.SCP330); break;
                                case 9: AddOrDropItem(random, ItemType.SCP500); break;
                            }
                        }
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
            if (player != null && player.PlayerId == dr_bright && newRole != RoleTypeId.Scientist)
            {
                dr_bright = -1;
                player.TemporaryData.Remove("custom_class");
            }
        }

        public int CompareTo(object obj)
        {
            return Comparer<DrBright>.Default.Compare(this, obj as DrBright);
        }
    }
}
