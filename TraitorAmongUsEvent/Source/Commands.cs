﻿using CedMod.Addons.QuerySystem;
using CommandSystem;
using PluginAPI.Core;
using NWAPIPermissionSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TauMap : ICommand
    {
        public string Command { get; } = "tau_map";

        public string[] Aliases { get; } = new string[] { "taum" };

        public string Description { get; } = "Select the map for Traitor Among Us";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = "To execute this command provide at least 1 argument!\nUsage: tau_map <map name>";
                return false;
            }

            if (sender.IsPanelUser() ? !sender.CheckPermission(PlayerPermissions.FacilityManagement) : !sender.CheckPermission("cedmod.events.enable"))
            {
                response = "No permission";
                return false;
            }

            string name = string.Join(" ", arguments);
            if (TraitorAmongUsEvent.IsRunning)
            {
                if (TraitorAmongUs.SetMap(name))
                {
                    response = "Success";
                    return true;
                }
                else
                {
                    response = "Failed: No map of name: " + name;
                    return false;
                }
            }
            else
            {
                TraitorAmongUs.map_selected = name;
            }
            response = "Success: preset map for next round";
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TauPause : ICommand
    {
        public string Command { get; } = "tau_pause";

        public string[] Aliases { get; } = new string[] { "taup" };

        public string Description { get; } = "pauses or resumes ready up";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement))
            {
                response = "No permission";
                return false;
            }

            TraitorAmongUs.pause_ready_up = !TraitorAmongUs.pause_ready_up;
            response = "Success: " + (TraitorAmongUs.pause_ready_up ? "Paused ready up" : "Resumed ready up");
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TauRDM : ICommand
    {
        public string Command { get; } = "tau_rdm";

        public string[] Aliases { get; } = new string[] { "taur" };

        public string Description { get; } = "Punish rdming players";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = "To execute this command provide at least 1 argument!\nUsage: tau_rdm <player_id>";
                return false;
            }
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement))
            {
                response = "No permission";
                return false;
            }
            int id = 0;
            if (!int.TryParse(arguments.At(0), out id))
            {
                response = "Failed: could not parse " + arguments.At(0) + " as an interger";
                return false;
            }
            Player target = null;
            if (!Player.TryGet(id, out target))
            {
                response = "Failed: could not find player with id: " + id;
                return false;
            }
            RDM.ForcePlayerOverLimit(target);
            response = "Success: " + target.Nickname + " will be set to spectator next round";
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TauForgive : ICommand
    {
        public string Command { get; } = "tau_forgive";

        public string[] Aliases { get; } = new string[] { "tauf" };

        public string Description { get; } = "Forgive rdming players";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = "To execute this command provide at least 1 argument!\nUsage: tau_forgive <player_id>";
                return false;
            }
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement))
            {
                response = "No permission";
                return false;
            }
            int id = 0;
            if (!int.TryParse(arguments.At(0), out id))
            {
                response = "Failed: could not parse " + arguments.At(0) + " as an interger";
                return false;
            }
            Player target = null;
            if (!Player.TryGet(id, out target))
            {
                response = "Failed: could not find player with id: " + id;
                return false;
            }
            RDM.ForgivePlayer(target);
            response = "Success: " + target.Nickname + " has been forgiven";
            return true;
        }
    }
}
