using CommandSystem;
using PluginAPI.Core;
using PluginAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TheRiptide.Utility;

namespace TheRiptide.Source
{
    //run match (test)
    //setup predefined (test)
    //force win (test)
    //undo win (test)
    //save log (test)

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TournamentUnsetTeam : ICommand
    {
        public string Command { get; } = "tour_unset_team";

        public string[] Aliases { get; } = new string[] { "tut" };

        public string Description { get; } = "removes player from team. usage: tst <player_id>";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement) || !Player.TryGet(sender, out player))
            {
                response = "No permission";
                return false;
            }

            int target_id = -1;
            if (!int.TryParse(arguments.At(0), out target_id))
            {
                response = "failed to parse player_id: " + arguments.At(0);
                return false;
            }

            Player target;
            if (!Player.TryGet(target_id, out target))
            {
                response = "failed to find player with id: " + target_id;
                return false;
            }

            EventHandler.Singleton.UnsetTeam(target);
            response = "Success";
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TournamentSaveLog : ICommand
    {
        public string Command { get; } = "tour_save_log";

        public string[] Aliases { get; } = new string[] { "tsl" };

        public string Description { get; } = "saves log. usage: tsl";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement) || !Player.TryGet(sender, out player))
            {
                response = "No permission";
                return false;
            }

            EventHandler.Singleton.SaveLog();

            response = "Success";
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TournamentUndoWin : ICommand
    {
        public string Command { get; } = "tour_undo_win";

        public string[] Aliases { get; } = new string[] { "tuw" };

        public string Description { get; } = "undo a teams last win. usage: tuw <team_name> <reason>";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement) || !Player.TryGet(sender, out player))
            {
                response = "No permission";
                return false;
            }

            string team_name = arguments.At(0);
            string reason = arguments.Count == 1 ? "no reason specified" : "";
            for (int i = 1; i < arguments.Count; i++)
                reason += arguments.At(i) + " ";
            if (!EventHandler.Singleton.UndoWin(player, team_name, reason))
            {
                response = "failed to undo win as the team: " + team_name + " could not be found in the remaining bracket. check tour_list_team for list of all teams";
                return false;
            }

            response = "Success";
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TournamentForceWin : ICommand
    {
        public string Command { get; } = "tour_force_win";

        public string[] Aliases { get; } = new string[] { "tfw" };

        public string Description { get; } = "force a team to win their next match. usage: tfw <team_name>";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement) || !Player.TryGet(sender, out player))
            {
                response = "No permission";
                return false;
            }

            string team_name = arguments.At(0);
            if (!EventHandler.Singleton.ForceWin(player, team_name))
            {
                response = "failed to force win as the team: " + team_name + " could not be found in the remaining bracket. check tour_list_team for list of all teams";
                return false;
            }

            response = "Success";
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TournamentPredefined : ICommand
    {
        public string Command { get; } = "tour_predefined";

        public string[] Aliases { get; } = new string[] { "predef" };

        public string Description { get; } = "setup predefined bracket and load log. usage: predef";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement) || !Player.TryGet(sender, out player))
            {
                response = "No permission";
                return false;
            }

            EventHandler.Singleton.SetupPredefined();

            response = "Success";
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TournamentRunMatch : ICommand
    {
        public string Command { get; } = "tour_run_match";

        public string[] Aliases { get; } = new string[] { "trm" };

        public string Description { get; } = "run match by specifing a team. usage: trm <team_name>";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement) || !Player.TryGet(sender, out player))
            {
                response = "No permission";
                return false;
            }

            string team_name = arguments.At(0);
            if (!EventHandler.Singleton.TryRunMatch(team_name))
            {
                response = "failed to run match as the team: " + team_name + " could not be found in the remaining bracket. check tour_list_team for list of all teams";
                return false;
            }

            response = "Success";
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TournamentAutoRun : ICommand
    {
        public string Command { get; } = "tour_auto_run";

        public string[] Aliases { get; } = new string[] { "tar" };

        public string Description { get; } = "Auto runs the tournament";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement) || !Player.TryGet(sender, out player))
            {
                response = "No permission";
                return false;
            }

            EventHandler.Singleton.AutoRunTournament();

            response = "Success";
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TournamentScrimmage : ICommand
    {
        public string Command { get; } = "tour_scrimmage";

        public string[] Aliases { get; } = new string[] { "scrim" };

        public string Description { get; } = "setup tournament as a scrimmage with a certain amount of teams assigned randomly to players. Usage 'scrim <team_count>'";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement) || !Player.TryGet(sender, out player))
            {
                response = "No permission";
                return false;
            }

            int team_count;
            if (!int.TryParse(arguments.At(0), out team_count))
            {
                response = "failed";
                return false;
            }

            EventHandler.Singleton.SetupScrimmage(team_count);
            response = "Success";
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TournamentFixBots : ICommand
    {
        public string Command { get; } = "fix_bots";

        public string[] Aliases { get; } = new string[] { "fb" };

        public string Description { get; } = "fix cedmod bots";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement) || !Player.TryGet(sender, out player))
            {
                response = "No permission";
                return false;
            }

            foreach (var p in ReadyPlayers())
            {
                if (!Player.PlayersUserIds.ContainsKey(p.UserId))
                {
                    Player.PlayersUserIds.Add(p.UserId, p.ReferenceHub);
                    PluginAPI.Events.EventManager.ExecuteEvent(new PlayerJoinedEvent(p.ReferenceHub));
                }
            }

            response = "Success";
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TournamentSetTeam : ICommand
    {
        public string Command { get; } = "tour_set_team";

        public string[] Aliases { get; } = new string[] { "tst" };

        public string Description { get; } = "assign player to team. usage: tst <player_id> <team_name>";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement) || !Player.TryGet(sender, out player))
            {
                response = "No permission";
                return false;
            }

            int target_id = -1;
            if (!int.TryParse(arguments.At(0), out target_id))
            {
                response = "failed to parse player_id: " + arguments.At(0);
                return false;
            }

            Player target;
            if (!Player.TryGet(target_id, out target))
            {
                response = "failed to find player with id: " + target_id;
                return false;
            }

            string team_name = arguments.At(1);
            Team team = EventHandler.Singleton.TryAssignTeam(target, team_name);
            if (team == null)
            {
                response = "failed to assign " + target.Nickname + " to team " + team_name + "because the team does not exist";
                return false;
            }

            target.SendBroadcast("You have been assigned to: <b>" + team.BadgeColor + team.BadgeName + "</b></color> by " + player.Nickname, 15, shouldClearPrevious: true);

            response = "Success";
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TournamentCreateTeam : ICommand
    {
        public string Command { get; } = "tour_create_team";

        public string[] Aliases { get; } = new string[] { "tct" };

        public string Description { get; } = "create a new team. usage: tct <team_name>";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement) || !Player.TryGet(sender, out player))
            {
                response = "No permission";
                return false;
            }

            string team_name = arguments.At(0);
            if (!EventHandler.Singleton.TryCreateTeam(team_name))
            {
                response = "failed to create team: " + team_name + " because this team already exists";
                return false;
            }

            response = "Success";
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TournamentRemoveTeam : ICommand
    {
        public string Command { get; } = "tour_remove_team";

        public string[] Aliases { get; } = new string[] { "trt" };

        public string Description { get; } = "removes a team. usage: trt <team_name>";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement) || !Player.TryGet(sender, out player))
            {
                response = "No permission";
                return false;
            }

            string team_name = arguments.At(0);
            if (!EventHandler.Singleton.TryRemoveTeam(team_name))
            {
                response = "failed to remove team: " + team_name + " because this team doesnt exists";
                return false;
            }

            response = "Success";
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TournamentListTeams : ICommand
    {
        public string Command { get; } = "tour_list_team";

        public string[] Aliases { get; } = new string[] { "tlt" };

        public string Description { get; } = "list all teams. usage: tlt";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement) || !Player.TryGet(sender, out player))
            {
                response = "No permission";
                return false;
            }

            response = EventHandler.Singleton.TeamList();
            return true;
        }
    }


    [CommandHandler(typeof(ClientCommandHandler))]
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class ClearHint : ICommand
    {
        public string Command { get; } = "clear_hint";

        public string[] Aliases { get; } = new string[] { "ch" };

        public string Description { get; } = "clears hint. usage: ch";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (!Player.TryGet(sender, out player))
            {
                response = "No permission";
                return false;
            }

            player.ReceiveHint("", 3000);
            response = "Success";
            return true;
        }
    }
}
