using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventorySystem.Items;
using MEC;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Core.Items;
using PluginAPI.Enums;
using PluginAPI.Events;

namespace TheRiptide
{
    public class ScpProximityChatConfig
    {
        public List<RoleTypeId> Roles { get; set; } = new List<RoleTypeId>
        {
            RoleTypeId.Scp0492,
            RoleTypeId.Scp049,
            RoleTypeId.Scp939
        };
    }

    public class ScpProximityChat
    {
        [PluginConfig]
        public ScpProximityChatConfig config;

        private Dictionary<int, bool> proximity_toggled = new Dictionary<int, bool>();

        [PluginEntryPoint("ScpProximityChat", "1.0.0", "Allow scps to talk in proximity chat", "The Riptide")]
        public void OnEnabled()
        {
            EventManager.RegisterEvents(this);
            EventManager.RegisterEvents<VoiceChatOverride>(this);
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            if (role.GetTeam() == Team.SCPs && config.Roles.Contains(role))
            {
                if (proximity_toggled.ContainsKey(player.PlayerId))
                    proximity_toggled[player.PlayerId] = true;
                else
                    proximity_toggled.Add(player.PlayerId, true);
                VoiceChatOverride.Singleton.SetSendValidator(player, (channel) =>
                 {
                     if (proximity_toggled[player.PlayerId])
                         return VoiceChat.VoiceChatChannel.Proximity;
                     else
                         return channel;
                 });
                Timing.CallDelayed(0.0f, () =>
                {
                    player.AddItem(ItemType.KeycardJanitor);
                });
            }
            else
            {
                VoiceChatOverride.Singleton.ResetSendValidator(player);
                proximity_toggled.Remove(player.PlayerId);
            }
        }

        [PluginEvent(ServerEventType.PlayerChangeItem)]
        void OnPlayerChangeItem(Player player, ushort oldItem, ushort newItem)
        {
            if (config.Roles.Contains(player.Role))
            {
                if (player.ReferenceHub.inventory.UserInventory.Items.ContainsKey(newItem))
                {
                    ItemBase item = player.ReferenceHub.inventory.UserInventory.Items[newItem];
                    if(item.ItemTypeId == ItemType.KeycardJanitor)
                    {
                        if (proximity_toggled.ContainsKey(player.PlayerId))
                        {
                            proximity_toggled[player.PlayerId] = !proximity_toggled[player.PlayerId];
                            player.RemoveItem(new Item(item));
                            player.AddItem(ItemType.KeycardJanitor);
                        }
                    }
                }
            }
        }

        //[PluginEvent(ServerEventType.PlayerUseHotkey)]
        //void OnPlayerUseHotkey(Player player, ActionName action)
        //{
        //    Log.Info("player: " + player.Nickname + " used hotkey " + action.ToString());
        //}
    }
}
