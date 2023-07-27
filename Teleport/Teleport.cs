using CommandSystem;
using MapGeneration;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public class Teleport
    {
        public readonly struct RoomID
        {
            public FacilityZone zone { get; }
            public RoomName name { get; }
            public RoomShape shape { get; }

            public RoomID(FacilityZone zone, RoomName name, RoomShape shape)
            {
                this.zone = zone;
                this.name = name;
                this.shape = shape;
            }
        }

        public static Dictionary<RoomID, List<Vector3>> room_local_positions = new Dictionary<RoomID, List<Vector3>>
        {
            { new RoomID(FacilityZone.LightContainment, RoomName.LczClassDSpawn, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(6.000f, 0.960f, 3.141f) }},
            { new RoomID(FacilityZone.LightContainment, RoomName.LczComputerRoom, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(-6.598f, 0.960f, -2.979f) }},
            { new RoomID(FacilityZone.LightContainment, RoomName.LczCheckpointA, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(-2.746f, 0.960f, -5.692f) }},
            { new RoomID(FacilityZone.LightContainment, RoomName.LczCheckpointB, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(-2.626f, 0.960f, -6.109f) }},
            { new RoomID(FacilityZone.LightContainment, RoomName.LczToilets, RoomShape.Straight),
                new List<Vector3>{ new Vector3(-2.891f, 0.960f, -4.891f) }},
            { new RoomID(FacilityZone.LightContainment, RoomName.LczArmory, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(1.320f, 0.960f, -3.441f) }},
            { new RoomID(FacilityZone.LightContainment, RoomName.Lcz173, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(-6.800f, 3.382f, 2.301f) }},
            { new RoomID(FacilityZone.LightContainment, RoomName.LczGlassroom, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(-5.211f, 0.960f, 2.477f) }},
            { new RoomID(FacilityZone.LightContainment, RoomName.Lcz330, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(-2.256f, 0.956f, 4.707f) }},
            { new RoomID(FacilityZone.LightContainment, RoomName.Lcz914, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(-6.322f, 0.960f, 2.116f) }},
            { new RoomID(FacilityZone.LightContainment, RoomName.LczGreenhouse, RoomShape.Straight),
                new List<Vector3>{ new Vector3(0.035f, 0.964f, -5.340f) }},
            { new RoomID(FacilityZone.LightContainment, RoomName.LczAirlock, RoomShape.Straight),
                new List<Vector3>{ new Vector3(-2.141f, 0.969f, -5.402f) }},
            { new RoomID(FacilityZone.HeavyContainment, RoomName.HczCheckpointToEntranceZone, RoomShape.Straight),
                new List<Vector3>{ new Vector3(-0.830f, 0.959f, -6.383f) }},
            { new RoomID(FacilityZone.HeavyContainment, RoomName.HczCheckpointA, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(-2.056f, 0.960f, -2.062f) }},
            { new RoomID(FacilityZone.HeavyContainment, RoomName.HczCheckpointB, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(-2.056f, 0.960f, -2.062f) }},
            { new RoomID(FacilityZone.HeavyContainment, RoomName.Hcz079, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(6.039f, -2.372f, 0.129f) }},
            { new RoomID(FacilityZone.HeavyContainment, RoomName.Hcz096, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(-5.656f, 0.960f, -2.199f) }},
            { new RoomID(FacilityZone.HeavyContainment, RoomName.Hcz106, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(-6.438f, 1.251f, 2.602f) }},
            { new RoomID(FacilityZone.HeavyContainment, RoomName.Hcz939, RoomShape.Curve),
                new List<Vector3>{ new Vector3(-2.977f, 0.985f, -3.289f) }},
            { new RoomID(FacilityZone.HeavyContainment, RoomName.HczMicroHID, RoomShape.Straight),
                new List<Vector3>{ new Vector3(-0.008f, 0.960f, -3.727f) }},
            { new RoomID(FacilityZone.HeavyContainment, RoomName.HczArmory, RoomShape.TShape),
                new List<Vector3>{ new Vector3(0.535f, 0.960f, 1.438f) }},
            { new RoomID(FacilityZone.HeavyContainment, RoomName.HczServers, RoomShape.Straight),
                new List<Vector3>{ new Vector3(-1.930f, -4.802f, 6.292f) }},
            { new RoomID(FacilityZone.HeavyContainment, RoomName.HczTesla, RoomShape.Straight),
                new List<Vector3>{ new Vector3(-3.687f, 0.955f, -0.101f) }},
            { new RoomID(FacilityZone.HeavyContainment, RoomName.HczTestroom, RoomShape.Straight),
                new List<Vector3>{ new Vector3(-0.039f, 0.960f, -6.570f) }},
            { new RoomID(FacilityZone.Entrance, RoomName.EzCollapsedTunnel, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(-1.289f, 0.960f, 6.954f) }},
            { new RoomID(FacilityZone.Entrance, RoomName.EzGateA, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(2.179f, 0.960f, 6.594f) }},
            { new RoomID(FacilityZone.Entrance, RoomName.EzGateB, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(1.939f, 0.960f, 6.352f) }},
            { new RoomID(FacilityZone.Entrance, RoomName.EzRedroom, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(3.400f, 0.960f, -0.708f) }},
            { new RoomID(FacilityZone.Entrance, RoomName.EzEvacShelter, RoomShape.Endroom),
                new List<Vector3>{ new Vector3(-2.078f, 0.960f, 6.438f) }},
            { new RoomID(FacilityZone.Entrance, RoomName.EzIntercom, RoomShape.Curve),
                new List<Vector3>{ new Vector3(5.080f, -3.405f, 4.389f) }},
            { new RoomID(FacilityZone.Entrance, RoomName.EzOfficeStoried, RoomShape.Straight),
                new List<Vector3>{ new Vector3(6.082f, 3.826f, 5.086f) }},
            { new RoomID(FacilityZone.Entrance, RoomName.EzOfficeLarge, RoomShape.Straight),
                new List<Vector3>{ new Vector3(-3.102f, 0.960f, 5.552f) }},
            { new RoomID(FacilityZone.Entrance, RoomName.EzOfficeSmall, RoomShape.Straight),
                new List<Vector3>{ new Vector3(0.059f, 0.960f, -0.055f) } },
            { new RoomID(FacilityZone.Surface, RoomName.Outside, RoomShape.Undefined),
                new List<Vector3>{ new Vector3(0.000f, 0.960f, 0.000f), new Vector3(-39.895f, -12.066f, -42.703f),
                    new Vector3(28.125f, -8.115f, -28.320f), new Vector3(108.659f, -4.306f, -33.340f),
                    new Vector3(132.454f, -11.208f, 24.496f)} }
        };

        public static void Room(Player player, RoomIdentifier room)
        {
            RoomID key = new RoomID(room.Zone, room.Name, room.Shape);
            if (room_local_positions.ContainsKey(key))
                player.Position = room.transform.TransformPoint(room_local_positions[key].First());
            else
                player.Position = room.transform.position + Vector3.up * 0.5f;
        }

        public static void RoomPos(Player player, RoomIdentifier room, Vector3 pos)
        {
            player.Position = room.transform.TransformPoint(pos);
        }

        public static void RoomRandom(Player player, RoomIdentifier room)
        {
            RoomID key = new RoomID(room.Zone, room.Name, room.Shape);
            if (room_local_positions.ContainsKey(key))
                player.Position = room.transform.TransformPoint(room_local_positions[key].RandomItem());
            else
                player.Position = room.transform.position + Vector3.up * 0.5f;
        }

        public static void RoomAt(Player player, RoomIdentifier room, int index)
        {

            RoomID key = new RoomID(room.Zone, room.Name, room.Shape);
            if (room_local_positions.ContainsKey(key))
                player.Position = room.transform.TransformPoint(room_local_positions[key][index]);
            else
                player.Position = room.transform.position + Vector3.up * 0.5f;
        }

        public static List<Vector3> RoomPositions(RoomIdentifier room)
        {
            RoomID key = new RoomID(room.Zone, room.Name, room.Shape);
            List<Vector3> world_positions = new List<Vector3>();
            if (room_local_positions.ContainsKey(key))
                world_positions = room_local_positions[key].ConvertAll((local) => room.transform.TransformPoint(local));
            else
                world_positions.Add(room.transform.position);
            return world_positions;
        }
    }
}
