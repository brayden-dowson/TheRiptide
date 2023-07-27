using CommandSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide
{
    public class LeaderBoard
    {
        [PluginEntryPoint("Leader Board", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        [CommandHandler(typeof(RemoteAdminCommandHandler))]
        [CommandHandler(typeof(GameConsoleCommandHandler))]
        public class ToggleLeaderBoard : ICommand
        {
            public string Command { get; } = "leaderboard";

            public string[] Aliases { get; } = new string[] { ".leaderboard" ,"lb",".lb"};

            public string Description { get; } = "show leader board";

            public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                Player player;
                if (!Player.TryGet(sender, out player))
                {
                    response = "failed";
                    return false;
                }

                response = "success";
                return true;
            }
        }
    }
}
