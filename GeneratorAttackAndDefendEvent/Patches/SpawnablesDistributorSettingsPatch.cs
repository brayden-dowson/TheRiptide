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
                    structure.MinAmount = EventHandler.config.MinGenerators;
                    structure.MaxAmount = EventHandler.config.MaxGenerators;
                }
            }

            if (EventHandler.config.OnePerRoom)
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

            if(EventHandler.config.NoEndRooms)
            {
                foreach (var spawnpoint in StructureSpawnpoint.AvailableInstances.ToList())
                {
                    if (!spawnpoint.CompatibleStructures.Contains(StructureType.Scp079Generator))
                        continue;
                    RoomIdentifier room = RoomIdentifier.AllRoomIdentifiers.FirstOrDefault(r => r.Name == spawnpoint.RoomName);
                    if (room == null || room.Shape == RoomShape.Endroom)
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
        }
    }
}
