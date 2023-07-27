using CedMod.Addons.Events;
using CustomPlayerEffects;
using MapGeneration;
using MEC;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public class SpawnBugFix
    {
        public static bool normal_round;

        Dictionary<RoleTypeId, Vector3> role_offsets = new Dictionary<RoleTypeId, Vector3>
        {
            {RoleTypeId.Scp106, new Vector3(22.238f, 0.895f, -7.250f) },
            {RoleTypeId.Scp939, new Vector3(-3.185f, 1.345f, -6.030f) },
            {RoleTypeId.Scp173, new Vector3(2.850f, 197.765f, 10.100f) },
            {RoleTypeId.Scp049, new Vector3(-5.000f, 193.400f, -10.350f) },
            {RoleTypeId.Scp096, new Vector3(-5.482f, 0.950f, 0.000f) }
        };

        [PluginEntryPoint("Spawn Bug Fix", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            normal_round = true;
            Type event_manager_type = GetType("CedMod.Addons.Events.EventManager");
            if (event_manager_type != null && event_manager_type.GetField("CurrentEvent", BindingFlags.Public | BindingFlags.Static).GetValue(null) != null)
                normal_round = false;
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            Timing.CallDelayed(0.0f, () =>
            {
                switch (player.Role)
                {
                    case RoleTypeId.Scp106: FixSpawn(player, RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Hcz106), role); break;
                    case RoleTypeId.Scp939: FixSpawn(player, RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Hcz939), role); break;
                    case RoleTypeId.Scp173: FixSpawn(player, RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Hcz049), role); break;
                    case RoleTypeId.Scp049: FixSpawn(player, RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Hcz049), role); break;
                    case RoleTypeId.Scp096: FixSpawn(player, RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Hcz096), role); break;
                }
            });
        }

        private void FixSpawn(Player player, RoomIdentifier room, RoleTypeId role)
        {
            if (normal_round)
            {
                Timing.CallDelayed(0.1f, () =>
                {
                    player.Position = room.transform.TransformPoint(role_offsets[role]);
                    player.SendBroadcast("NW moment! your position was reset with a plugin", 5);
                    if (player.Role == role && Vector3.Distance(room.transform.InverseTransformPoint(player.Position), role_offsets[role]) > 1.0f)
                    {
                        Log.Error("out of spawn");
                    }
                });
            }
        }

        public static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
