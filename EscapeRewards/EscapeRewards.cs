using CustomPlayerEffects;
using MEC;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

using static TheRiptide.Utility;

namespace TheRiptide
{
    public class Config
    {
        [Description("Enables the rewards for escaping with scp items")]
        public bool EnableScpRewards { get; set; } = true;
        [Description("Keeps status effects on escape")]
        public bool KeepEffectsOnEscape { get; set; } = true;
    }

    public struct PreviousState
    {
        public byte intensity;
        public float duration;
    }

    public class EscapeRewards
    {
        [PluginConfig]
        public Config config;

        [PluginEntryPoint("Escape Rewards", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        static HashSet<ItemType> scp_items = new HashSet<ItemType>
        {
            ItemType.SCP018,
            ItemType.SCP1576,
            ItemType.SCP1853,
            ItemType.SCP207,
            ItemType.SCP2176,
            ItemType.SCP244a,
            ItemType.SCP244b,
            ItemType.SCP268,
            ItemType.SCP330,
            ItemType.SCP500
        };

        [PluginEvent(ServerEventType.PlayerEscape)]
        void OnPlayerEscape(Player player, RoleTypeId role)
        {
            int scp_items_count = player.ReferenceHub.inventory.UserInventory.Items.Values.Where((item) => scp_items.Contains(item.ItemTypeId)).Count();
            List<PreviousState> states = new List<PreviousState>();
            foreach (var seb in player.ReferenceHub.playerEffectsController.AllEffects)
            {
                //if (seb.IsEnabled)
                //    Log.Info("intensity: " + seb.Intensity.ToString() + " time left: " + seb.TimeLeft.ToString() + " duration: " + seb.Duration);
                if (!(seb is Invisible))
                    states.Add(new PreviousState { intensity = seb.Intensity, duration = seb._timeLeft });
                else
                    states.Add(new PreviousState { intensity = 0, duration = 0 });
            }
            if (role == RoleTypeId.NtfSpecialist)
            {
                Timing.CallDelayed(0.1f, () =>
                {
                    if (player.Role != RoleTypeId.NtfSpecialist)
                        return;

                    try
                    {
                        if (config.EnableScpRewards)
                        {
                            Log.Info("escapee had: " + scp_items_count.ToString() + " scp items");
                            if (scp_items_count >= 3)
                            {
                                AddOrDropItem(player, ItemType.KeycardMTFCaptain);
                            }
                            if (scp_items_count >= 2)
                            {
                                RemoveItem(player, ItemType.ArmorCombat);
                                RemoveItem(player, ItemType.ArmorLight);
                                AddOrDropItem(player, ItemType.ArmorHeavy);
                                AddOrDropItem(player, ItemType.Adrenaline);
                                AddOrDropFirearm(player, ItemType.GunShotgun, true);
                            }
                            if (scp_items_count >= 1 && scp_items_count != 2)
                            {
                                AddOrDropFirearm(player, ItemType.GunCOM18, true);
                                AddOrDropItem(player, ItemType.Medkit);
                            }
                        }
                        if (config.KeepEffectsOnEscape)
                        {
                            int index = 0;
                            foreach (var seb in player.ReferenceHub.playerEffectsController.AllEffects)
                            {
                                seb.ServerSetState(states[index].intensity, states[index].duration);
                                index++;
                            }
                        }
                    }
                    catch(System.Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                });
            }
            else if (role == RoleTypeId.ChaosConscript)
            {
                Timing.CallDelayed(0.1f, () =>
                {
                    if (player.Role != RoleTypeId.ChaosConscript)
                        return;

                    try
                    {
                        if (config.EnableScpRewards)
                        {
                            Log.Info("escapee had: " + scp_items_count.ToString() + " scp items");
                            if (scp_items_count >= 2)
                            {
                                RemoveItem(player, ItemType.ArmorCombat);
                                RemoveItem(player, ItemType.ArmorLight);
                                AddOrDropItem(player, ItemType.ArmorHeavy);
                                AddOrDropItem(player, ItemType.Adrenaline);
                            }
                            if (scp_items_count >= 3)
                            {
                                AddOrDropFirearm(player, ItemType.GunShotgun, true);
                            }
                            if (scp_items_count >= 1 && scp_items_count != 2)
                            {
                                AddOrDropFirearm(player, ItemType.GunRevolver, true);
                            }
                        }
                        if (config.KeepEffectsOnEscape)
                        {
                            int index = 0;
                            foreach (var seb in player.ReferenceHub.playerEffectsController.AllEffects)
                            {
                                seb.ServerSetState(states[index].intensity, states[index].duration);
                                index++;
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                });
            }
        }
    }
}
