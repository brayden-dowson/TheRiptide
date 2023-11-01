using HarmonyLib;
using MapGeneration.Distributors;
using Mirror;
using System;
using System.Collections.Generic;
using NorthwoodLib.Pools;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using PluginAPI.Core;
using MapGeneration;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(StructureDistributor))]
    class SpawnablesDistributorSettingsPatch
    {
        private static int previous_min;
        private static int previous_max;
        [HarmonyPatch(nameof(StructureDistributor.PlaceSpawnables), MethodType.Normal)]
        public static void Prefix(StructureDistributor __instance)
        {
            foreach (var structure in __instance.Settings.SpawnableStructures)
            {
                if (structure.StructureType == StructureType.Scp079Generator)
                {
                    previous_min = structure.MinAmount;
                    previous_max = structure.MaxAmount;
                    structure.MinAmount = GeneratorOverride.Singelton.config.MinGenerators;
                    structure.MaxAmount = GeneratorOverride.Singelton.config.MaxGenerators;
                }
            }

            if (GeneratorOverride.Singelton.config.OnePerRoom)
            {
                HashSet<RoomName> set = new HashSet<RoomName>();

                foreach (var spawnpoint in StructureSpawnpoint.AvailableInstances.ToList())
                {
                    if (!spawnpoint.CompatibleStructures.Contains(StructureType.Scp079Generator))
                        continue;
                    if (!set.Contains(spawnpoint.RoomName))
                        set.Add(spawnpoint.RoomName);
                    else
                        StructureSpawnpoint.AvailableInstances.Remove(spawnpoint);
                }
            }
        }

        [HarmonyPatch(nameof(StructureDistributor.PlaceSpawnables), MethodType.Normal)]
        public static void Postfix(StructureDistributor __instance)
        {
            foreach (var structure in __instance.Settings.SpawnableStructures)
            {
                if (structure.StructureType == StructureType.Scp079Generator)
                {
                    structure.MinAmount = previous_min;
                    structure.MaxAmount = previous_max;
                }
            }

            foreach (var room in RoomIdentifier.AllRoomIdentifiers)
            {
                Scp079Generator[] generators = room.GetComponentsInChildren<Scp079Generator>();
                foreach (var generator in generators)
                {
                    generator._totalActivationTime = GeneratorOverride.Singelton.config.ActivationTime;
                    generator._totalDeactivationTime = GeneratorOverride.Singelton.config.DeactivationTime;
                }
            }
        }
    }
}
