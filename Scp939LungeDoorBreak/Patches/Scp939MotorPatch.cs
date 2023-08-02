using HarmonyLib;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using PlayerRoles.PlayableScps.Scp939;
using PlayerStatsSystem;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    [HarmonyPatch(typeof(Scp939Motor), nameof(Scp939Motor.OverlapCapsule))]
    class Scp939MotorPatch
    {
        private static readonly CachedLayerMask Mask = new CachedLayerMask(new string[3]
        {
            "Hitbox",
            "Glass",
            "Door"
        });

        static bool Prefix(Scp939Motor __instance, Vector3 point1, Vector3 point2)
        {
            if (!ScpDoorBreak.Singleton.config.Enable939Lunge)
                return true;

            StaminaStat stamina;
            if (!__instance.Hub.playerStats.TryGetModule(out stamina) || (stamina.CurValue - ScpDoorBreak.Singleton.config.Scp939StaminaPerTick) < 0.0)
            {
                if (stamina != null)
                    stamina.CurValue = 0.0f;
                return true;
            }

            int num = Physics.OverlapCapsuleNonAlloc(point1, point2, 0.6f, Scp939Motor.Detections, Mask);
            for (int index = 0; index < num; ++index)
            {
                IDestructible component;
                if (Scp939Motor.Detections[index].TryGetComponent(out component))
                {
                    switch (component)
                    {
                        case HitboxIdentity hid:
                            __instance.ProcessHitboxCollision(hid);
                            continue;
                        case BreakableWindow window:
                            __instance.ProcessWindowCollision(window);
                            continue;
                    }
                }
                BreakableDoor door = Scp939Motor.Detections[index].GetComponentInParent<BreakableDoor>();
                if (door != null)
                {
                    if (door.GetExactState() != 1.0f)
                    {
                        door.ServerDamage(ScpDoorBreak.Singleton.config.Scp939DamagePerTick, ScpDoorBreak.Singleton.config.Scp939DamageType);
                        stamina.ModifyAmount(-ScpDoorBreak.Singleton.config.Scp939StaminaPerTick);
                        Scp939LungeAbility lunge;
                        if (__instance._role.SubroutineModule.TryGetSubroutine(out lunge))
                            lunge._movementModule.StateProcessor._regenStopwatch.Restart();
                    }
                }
            }
            return false;
        }
    }
}
