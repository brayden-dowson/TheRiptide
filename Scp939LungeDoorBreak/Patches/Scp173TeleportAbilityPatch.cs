using HarmonyLib;
using Interactables.Interobjects;
using Mirror;
using PlayerRoles.PlayableScps.Scp173;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(Scp173TeleportAbility), nameof(Scp173TeleportAbility.ServerProcessCmd))]
    class Scp173TeleportAbilityPatch
    {
        private static readonly CachedLayerMask Mask = new CachedLayerMask(new string[1] { "Door" });

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool found_breakneck_is_active_call = false;
            foreach(var instruction in instructions)
            {
                if (instruction.Calls(typeof(Scp173BreakneckSpeedsAbility).GetProperty("IsActive").GetMethod))
                    found_breakneck_is_active_call = true;

                if(found_breakneck_is_active_call && instruction.opcode == OpCodes.Ret)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, typeof(Scp173TeleportAbilityPatch).GetMethod("CheckDoors"));
                    found_breakneck_is_active_call = false;
                }

                yield return instruction;
            }
        }

        public static void CheckDoors(Scp173TeleportAbility instance)
        {
            if (!ScpDoorBreak.Singleton.config.Enable173TeleportBreakneck)
                return;

            int num2 = Physics.OverlapSphereNonAlloc(instance._fpcModule.Position, 0.8f, Scp173TeleportAbility.DetectedColliders, Mask);
            for (int index = 0; index < num2; ++index)
            {
                BreakableDoor door = Scp173TeleportAbility.DetectedColliders[index].GetComponentInParent<BreakableDoor>();
                if (door != null)
                {
                    if (door.GetExactState() != 1.0f)
                    {
                        door.ServerDamage(ScpDoorBreak.Singleton.config.Scp173DamageOnTeleport, ScpDoorBreak.Singleton.config.Scp173TeleportDamageType);
                        instance._breakneckSpeedsAbility._disableTime -= ScpDoorBreak.Singleton.config.Scp173TeleportPenalty;
                    }
                }
            }
        }
    }
}
