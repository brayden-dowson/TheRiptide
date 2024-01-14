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
    public class Tournament : ParentCommand
    {
        public override string Command => "tournament";

        public override string[] Aliases { get; } = new string[] { "tour", "t" };

        public override string Description => "Parent command for tournament related commands. usage t <sub_command> <parameters>...";

        public Tournament() => LoadGeneratedCommands();

        public override void LoadGeneratedCommands()
        {
            RegisterCommand(new UnsetTeam());
            RegisterCommand(new SaveLog());
            RegisterCommand(new UndoWin());
            RegisterCommand(new ForceWin());
            RegisterCommand(new Predefined());
            RegisterCommand(new RunMatch());
            RegisterCommand(new AutoRun());
            RegisterCommand(new Scrimmage());
            RegisterCommand(new SetTeam());
            RegisterCommand(new CreateTeam());
            RegisterCommand(new RemoveTeam());
            RegisterCommand(new TournamentListTeams());
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = $"Invalid subcommand. Valid subcommands are:\n{string.Join("\n", Commands.Values.Select(c => c.Command + " aliases: " + string.Join(", ", c.Aliases) + " description: " + c.Description))}";
            return false;
        }
    }

    public class UnsetTeam : ICommand
    {
        public string Command { get; } = "unset_team";

        public string[] Aliases { get; } = new string[] { "ut" };

        public string Description { get; } = "removes player from team. usage: t ut <player_id>";

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

    public class SaveLog : ICommand
    {
        public string Command { get; } = "save_log";

        public string[] Aliases { get; } = new string[] { "sl" };

        public string Description { get; } = "saves log. usage: t sl";

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

    public class UndoWin : ICommand
    {
        public string Command { get; } = "undo_win";

        public string[] Aliases { get; } = new string[] { "uw" };

        public string Description { get; } = "undo a teams last win. usage: t uw <team_name> <reason>";

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

    public class ForceWin : ICommand
    {
        public string Command { get; } = "force_win";

        public string[] Aliases { get; } = new string[] { "fw" };

        public string Description { get; } = "force a team to win their next match. usage: t fw <team_name>";

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

    public class Predefined : ICommand
    {
        public string Command { get; } = "predefined";

        public string[] Aliases { get; } = new string[] { "predef" };

        public string Description { get; } = "setup predefined bracket and load log. usage: t predef";

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

    public class RunMatch : ICommand
    {
        public string Command { get; } = "run_match";

        public string[] Aliases { get; } = new string[] { "rm" };

        public string Description { get; } = "run match by specifing a team. usage: t rm <team_name>";

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

    public class AutoRun : ICommand
    {
        public string Command { get; } = "auto_run";

        public string[] Aliases { get; } = new string[] { "ar" };

        public string Description { get; } = "Auto runs the tournament. usage: t ar";

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

    public class Scrimmage : ICommand
    {
        public string Command { get; } = "scrimmage";

        public string[] Aliases { get; } = new string[] { "scrim" };

        public string Description { get; } = "setup tournament as a scrimmage with a certain amount of teams assigned randomly to players. Usage 't scrim <team_count>'";

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

    public class SetTeam : ICommand
    {
        public string Command { get; } = "set_team";

        public string[] Aliases { get; } = new string[] { "st" };

        public string Description { get; } = "assign player to team. usage: t st <player_id> <team_name>";

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

    public class CreateTeam : ICommand
    {
        public string Command { get; } = "create_team";

        public string[] Aliases { get; } = new string[] { "ct" };

        public string Description { get; } = "create a new team. usage: t ct <team_name>";

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

    public class RemoveTeam : ICommand
    {
        public string Command { get; } = "remove_team";

        public string[] Aliases { get; } = new string[] { "rt" };

        public string Description { get; } = "removes a team. usage: t rt <team_name>";

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

    public class TournamentListTeams : ICommand
    {
        public string Command { get; } = "list_team";

        public string[] Aliases { get; } = new string[] { "lt" };

        public string Description { get; } = "list all teams. usage: t lt";

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
