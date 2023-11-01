using HarmonyLib;
using Interactables.Interobjects.DoorUtils;
using PlayerRoles.PlayableScps.Scp079;
using Subtitles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils.Networking;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(Scp079Recontainer))]
    class Scp079RecontainerPatch
    {
        [HarmonyPatch(nameof(Scp079Recontainer.UpdateStatus), MethodType.Normal)]
        public static bool Prefix(Scp079Recontainer __instance, int engagedGenerators)
        {
            int count = Mathf.Min(GeneratorOverride.Singelton.config.RecontainThreshold, Scp079Recontainer.AllGenerators.Count);
            string annc = string.Format(__instance._announcementProgress, engagedGenerators, count);
            List<SubtitlePart> subtitlePartList = new List<SubtitlePart>()
            {
                new SubtitlePart(SubtitleType.GeneratorsActivated, new string[2]
                {
                    engagedGenerators.ToString(),
                    count.ToString()
                })
            };
            if (engagedGenerators >= count)
            {
                annc += __instance._announcementAllActivated;
                __instance.SetContainmentDoors(true, Scp079Role.ActiveInstances.Count > 0);
                subtitlePartList.Add(new SubtitlePart(SubtitleType.AllGeneratorsEngaged, (string[])null));
                foreach (DoorVariant containmentGate in __instance._containmentGates)
                {
                    if (containmentGate is IScp106PassableDoor scp106PassableDoor)
                        scp106PassableDoor.IsScp106Passable = true;
                }
            }
            new SubtitleMessage(subtitlePartList.ToArray()).SendToAuthenticated<SubtitleMessage>();
            __instance.PlayAnnouncement(annc, 1f);
            return false;
        }
    }
}
