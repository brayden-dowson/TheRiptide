using HarmonyLib;
using Interactables.Interobjects;
using Mirror;
using PlayerRoles.PlayableScps.Scp173;
using PlayerStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(Scp173MovementModule), nameof(Scp173MovementModule.UpdateGlassBreaking))]
    class Scp173MovementModulePatch
    {
        private static readonly CachedLayerMask Mask = new CachedLayerMask(new string[2]
        {
            "Glass",
            "Door"
        });

        static bool Prefix(Scp173MovementModule __instance)
        {
            if (!ScpDoorBreak.Singleton.config.Enable173Breakneck)
                return true;

            if (!NetworkServer.active || !__instance._breakneckSpeeds.IsActive)
                return false;
            Vector3 moveDirection = __instance.Motor.MoveDirection;
            float maxDistance = __instance.CharController.radius + 0.3f;
            RaycastHit hitInfo;
            if (Physics.Raycast(__instance.Position, moveDirection, out hitInfo, maxDistance, Mask))
            {
                BreakableWindow component;
                if (hitInfo.collider.TryGetComponent(out component))
                    component.Damage(component.health, __instance._role.DamageHandler, Vector3.zero);
                BreakableDoor door = hitInfo.collider.GetComponentInParent<BreakableDoor>();
                if (door != null)
                {
                    if (door.GetExactState() != 1.0f)
                    {
                        door.ServerDamage(ScpDoorBreak.Singleton.config.Scp173DamagePerTick, ScpDoorBreak.Singleton.config.Scp173DamageType);
                        if (door.RemainingHealth <= 0.0)
                        {
                            if (__instance._breakneckSpeeds._disableTime <= 0.0f)
                                __instance._breakneckSpeeds._disableTime = __instance._breakneckSpeeds.Elapsed + Scp173BreakneckSpeedsAbility.StareLimit;
                            __instance._breakneckSpeeds._disableTime -= ScpDoorBreak.Singleton.config.Scp173BreakneckBreakPenalty;
                        }
                    }
                }
            }
            return false;
        }
    }
}
