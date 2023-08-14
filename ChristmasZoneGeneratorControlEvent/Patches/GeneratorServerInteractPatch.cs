using Footprinting;
using HarmonyLib;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Keycards;
using MapGeneration.Distributors;
using PlayerRoles;
using PluginAPI.Enums;
using PluginAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(Scp079Generator))]
    class GeneratorServerInteractPatch
    {
        [HarmonyPatch(nameof(Scp079Generator.ServerInteract), MethodType.Normal)]
        public static bool Prefix(Scp079Generator __instance, ReferenceHub ply, byte colliderId)
        {
            if (__instance._cooldownStopwatch.IsRunning && __instance._cooldownStopwatch.Elapsed.TotalSeconds < __instance._targetCooldown || colliderId != 0 && !__instance.HasFlag(__instance._flags, Scp079Generator.GeneratorFlags.Open))
                return false;
            __instance._cooldownStopwatch.Stop();

            switch (colliderId)
            {
                case 0:
                    if (__instance.HasFlag(__instance._flags, Scp079Generator.GeneratorFlags.Unlocked))
                    {
                        __instance.ServerSetFlag(Scp079Generator.GeneratorFlags.Open, !__instance.HasFlag(__instance._flags, Scp079Generator.GeneratorFlags.Open));
                        __instance._targetCooldown = __instance._doorToggleCooldownTime;
                        break;
                    }
                    if ((ply.serverRoles.BypassMode ? 1 : (!(ply.inventory.CurInstance != null) || !(ply.inventory.CurInstance is KeycardItem curInstance) ? 0 : (curInstance.Permissions.HasFlagFast(__instance._requiredPermission) ? 1 : 0))) == 0)
                    {
                        __instance._targetCooldown = __instance._unlockCooldownTime;
                        __instance.RpcDenied();
                        break;
                    }
                    __instance.ServerSetFlag(Scp079Generator.GeneratorFlags.Unlocked, true);
                    __instance.ServerGrantTicketsConditionally(new Footprint(ply), 0.5f);
                    break;
                case 1:
                    if (!__instance.Engaged)
                    {
                        if (!__instance.Activating)
                        {
                            if (!EventManager.ExecuteEvent(new PlayerActivateGeneratorEvent(ply, __instance)))
                                break;
                        }
                        else if (!EventManager.ExecuteEvent(new PlayerDeactivatedGeneratorEvent(ply, __instance)))
                            break;
                        __instance.Activating = !__instance.Activating;
                        if (__instance.Activating)
                        {
                            __instance._leverStopwatch.Restart();
                            __instance._lastActivator = new Footprint(ply);
                        }
                        else
                            __instance._lastActivator = new Footprint();
                        __instance._targetCooldown = __instance._doorToggleCooldownTime;
                        break;
                    }
                    break;
                case 2:
                    if (__instance.Activating && !__instance.Engaged && EventManager.ExecuteEvent(new PlayerDeactivatedGeneratorEvent(ply, __instance)))
                    {
                        __instance.ServerSetFlag(Scp079Generator.GeneratorFlags.Activating, false);
                        __instance._targetCooldown = __instance._unlockCooldownTime;
                        __instance._lastActivator = new Footprint();
                        break;
                    }
                    break;
                default:
                    __instance._targetCooldown = 1f;
                    break;
            }
            __instance._cooldownStopwatch.Restart();
            return false;
        }
    }
}
