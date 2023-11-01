using CustomPlayerEffects;
using Footprinting;
using HarmonyLib;
using Interactables.Interobjects;
using InventorySystem.Items.Usables.Scp330;
using PlayerRoles;
using PluginAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide.Patchs
{
    [HarmonyPatch(typeof(Scp330Interobject))]
    class Scp330InterobjectPatch
    {
        [HarmonyPatch(nameof(Scp330Interobject.ServerInteract))]
        public static bool Prefix(Scp330Interobject __instance, ReferenceHub ply, byte colliderId)
        {
            if (!ply.IsHuman())
                return false;
            Footprint other = new Footprint(ply);
            float a = 0.1f;
            int uses1 = 0;
            foreach (Footprint takenCandy in __instance._takenCandies)
            {
                if (takenCandy.SameLife(other))
                {
                    a = Mathf.Min(a, (float)takenCandy.Stopwatch.Elapsed.TotalSeconds);
                    ++uses1;
                }
            }
            if ((double)a < 0.10000000149011612 || !Scp330Bag.ServerProcessPickup(ply, (Scp330Pickup)null, out Scp330Bag _))
                return false;
            PlayerInteractScp330Event args = new PlayerInteractScp330Event(ply, uses1);
            if (!EventManager.ExecuteEvent((IEventArguments)args))
                return false;
            if (args.PlaySound)
                __instance.RpcMakeSound();
            int uses2 = args.Uses;
            if (args.AllowPunishment && uses2 >= CandyOverride.Singelton.config.MaxCandies)
            {
                ply.playerEffectsController.EnableEffect<SeveredHands>();
                do;
                while (__instance._takenCandies.Remove(other));
            }
            else
                __instance._takenCandies.Add(other);

            return false;
        }

    }
}
