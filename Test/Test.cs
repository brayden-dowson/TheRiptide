using Achievements;
using CommandSystem;
using MapGeneration;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheRiptide
{
    public class Config
    {
    }

    public class TeleportData
    {
        public RoomName n;
        public Vector3 p;
        public Vector3 r;
    }

    public class Test
    {
        [PluginConfig]
        public Config config;

        public static Dictionary<int, TeleportData> data = new Dictionary<int, TeleportData>
        {

        };


        [PluginEntryPoint("Test", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            EventManager.RegisterEvents(this);
        }




    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class gpr : ICommand
    {
        public string Command { get; } = "gpr";

        public string[] Aliases { get; } = new string[] {};

        public string Description { get; } = "get pos and rot";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            Vector3 pos;
            Vector3 rot;
            pos = player.Room.transform.InverseTransformPoint(player.Position);
            rot = player.Room.transform.InverseTransformDirection(player.GameObject.transform.rotation * Vector3.forward);

            int id = Test.data.Count;
            Test.data.Add(id, new TeleportData { n = player.Room.Name, p = pos, r = rot });

            Log.Info(pos.ToPreciseString() + " | " + rot.ToPreciseString());
            response = "ID: " + id;
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class spr : ICommand
    {
        public string Command { get; } = "spr";

        public string[] Aliases { get; } = new string[] { };

        public string Description { get; } = "set pos and rot";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            int id;
            if(!int.TryParse(arguments.At(0),out id))
            {
                response = "Failed: invalid id: " + arguments.At(0);
                return false;
            }

            TeleportData td = Test.data[id];
            RoomIdentifier target = RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == td.n);
            player.ReferenceHub.TryOverridePosition(target.transform.TransformPoint(td.p), Quaternion.LookRotation(target.transform.TransformDirection(td.r)).eulerAngles - player.Rotation);
            response = "Success";
            return true;
        }
    }
}


