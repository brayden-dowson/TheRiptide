using HarmonyLib;
using Respawning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(RespawnEffectsController), nameof(RespawnEffectsController.ServerExecuteEffects))]
    internal static class RespawnEffectsControllerPatch
    {
        private static bool Prefix()
        {
            return false;
        }
    }
}
