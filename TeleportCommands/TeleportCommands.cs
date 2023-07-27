using CommandSystem;
using MapGeneration;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public class DummyClass
    {
        [PluginEntryPoint("Teleport cmd dummy", "1.0", "", "The Riptide")]
        void EntryPoint()
        {

        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class tp : ICommand
    {
        public string Command { get; } = "tp";

        public string[] Aliases { get; } = new string[] { };

        public string Description { get; } = "teleport to room specified by, name_id and/or zone_id and/or shape and/or instance_id. use -1 as a null placeholder";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (Player.TryGet(sender, out player))
            {
                if (arguments.Count != 0)
                {
                    int name_id = -1;
                    int zone_id = -1;
                    int shape_id = -1;
                    int instance_id = -1;
                    if (arguments.Count >= 1)
                    {
                        if (!int.TryParse(arguments.ElementAt(0), out name_id))
                        {
                            response = "name_id must be an interger. " + arguments.ElementAt(0);
                            return false;
                        }
                    }
                    if (arguments.Count >= 2)
                    {
                        if (!int.TryParse(arguments.ElementAt(1), out zone_id))
                        {
                            response = "zone_id must be an interger. " + arguments.ElementAt(1);
                            return false;
                        }
                    }
                    if (arguments.Count >= 3)
                    {
                        if (!int.TryParse(arguments.ElementAt(2), out shape_id))
                        {
                            response = "shape_id must be an interger. " + arguments.ElementAt(2);
                            return false;
                        }
                    }
                    if (arguments.Count >= 4)
                    {
                        if (!int.TryParse(arguments.ElementAt(3), out instance_id))
                        {
                            response = "instance_id must be an interger. " + arguments.ElementAt(3);
                            return false;
                        }
                    }
                    HashSet<RoomIdentifier> set = RoomIdentifier.AllRoomIdentifiers.ToHashSet();
                    if (name_id != -1)
                    {
                        if (Enum.IsDefined(typeof(RoomName), name_id))
                            set.RemoveWhere((r) => r.Name != (RoomName)name_id);
                        else
                        {
                            response = "name_id value out of range. " + name_id.ToString();
                            return false;
                        }
                    }
                    if (zone_id != -1)
                    {
                        if (Enum.IsDefined(typeof(FacilityZone), zone_id))
                            set.RemoveWhere((r) => r.Zone != (FacilityZone)zone_id);
                        else
                        {
                            response = "zone_id value out of range. " + zone_id.ToString();
                            return false;
                        }
                    }
                    if (shape_id != -1)
                    {
                        if (Enum.IsDefined(typeof(RoomShape), shape_id))
                            set.RemoveWhere((r) => r.Shape != (RoomShape)shape_id);
                        else
                        {
                            response = "shape_id value out of range. " + shape_id.ToString();
                            return false;
                        }
                    }
                    if (set.IsEmpty())
                    {
                        response = "no rooms match the criteria";
                        return false;
                    }
                    if (instance_id == -1)
                        instance_id = 0;

                    if (instance_id >= set.Count || instance_id < 0)
                    {
                        response = "instance_id value out of range. " + instance_id.ToString() + ", room count = " + set.Count.ToString();
                        return false;
                    }
                    else
                    {
                        Teleport.Room(player, set.ElementAt(instance_id));
                        response = "teleport successful";
                    }
                }
                else
                {
                    response = "\nname_ids\n";
                    foreach (var name in Enum.GetValues(typeof(RoomName)))
                        response += "\t" + name.ToString() + " = " + ((int)name).ToString() + "\n";
                    response += "zone_ids\n";
                    foreach (var zone in Enum.GetValues(typeof(FacilityZone)))
                        response += "\t" + zone.ToString() + " = " + ((int)zone).ToString() + "\n";
                    response += "shape_ids\n";
                    foreach (var shape in Enum.GetValues(typeof(RoomShape)))
                        response += "\t" + shape.ToString() + " = " + ((int)shape).ToString() + "\n";
                }
                return true;
            }
            else
            {
                response = "Teleport is for Players only";
                return false;
            }
        }
    }


    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class gp : ICommand
    {
        public string Command { get; } = "gp";

        public string[] Aliases { get; } = new string[] { };

        public string Description { get; } = "get local pos";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (Player.TryGet(sender, out player))
            {
                Vector3 pos = player.Room.transform.InverseTransformPoint(player.Position);
                ServerConsole.AddLog(pos.ToPreciseString(), ConsoleColor.Cyan);
                response = pos.ToPreciseString() + " | " + player.Room.Zone.ToString() + " | " + player.Room.Name.ToString() + " | " + player.Room.Shape.ToString();
                return true;
            }
            response = "failed";
            return false;
        }
    }
}
