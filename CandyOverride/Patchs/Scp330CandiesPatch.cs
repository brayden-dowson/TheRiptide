using HarmonyLib;
using InventorySystem.Items.Usables.Scp330;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TheRiptide;

namespace TheRiptide.Patchs
{
    [HarmonyPatch(typeof(Scp330Candies))]
    class Scp330CandiesPatch
    {
        private static float OverrideWeight(ICandy candy)
        {
            float value;
            if (CandyOverride.Singelton.config.CandyWeights.TryGetValue(candy.Kind, out value))
                return value;
            return candy.SpawnChanceWeight;
        }

        [HarmonyPatch(nameof(Scp330Candies.GetRandom))]
        public static bool Prefix(ref CandyKindID __result)
        {
            float maxInclusive = 0.0f;
            foreach (ICandy candy in Scp330Candies.AllCandies)
                maxInclusive += OverrideWeight(candy);
            float num = Random.Range(0.0f, maxInclusive);
            foreach (ICandy candy in Scp330Candies.AllCandies)
            {
                num -= OverrideWeight(candy);
                if (num <= 0.0)
                {
                    __result = candy.Kind;
                    return false;
                }
            }
            __result = CandyKindID.None;
            return false;
        }
    }
}
