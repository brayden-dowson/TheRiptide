using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MEC;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Core.Interfaces;
using PluginAPI.Enums;
using PluginAPI.Core.Factories;
using PluginAPI.Events;
using CommandSystem;
using RemoteAdmin;
using Hints;

namespace HintTest
{
    public class MyPlayer:Player
    {
        public MyPlayer(IGameComponent component):base(component)
        {
            EventManager.RegisterEvents(HintTest.Singleton, this);
        }

        public override void OnDestroy()
        {
            EventManager.UnregisterEvents(HintTest.Singleton, this);
        }
    }

    public class MyPlayerFactory:PlayerFactory
    {
        public override Type BaseType { get; } = typeof(MyPlayer);

        public override IPlayer Create(IGameComponent component) => new MyPlayer(component);
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class DisplayHintsCommand:ICommand
    {
        public string Command { get; } = "dhint";

        public string[] Aliases { get; } = new string[] {};

        public string Description { get; } = "show all hints for debug purposes";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            MyPlayer player;
            if (Player.TryGet(sender, out player))
            {
                player.SendBroadcast("<align=right>test hints</align>", 60, shouldClearPrevious: true);

                int value = 5;
                Timing.CallPeriodically(30.0f, 0.1f, ()=> 
                {
                    try
                    {
                        string text = "<align=right><align=bottom>My hint. value: " + value.ToString() + "</align></align>";
                        //player.SendConsoleMessage($"[REPORTING] {text}", "white");
                        value++;
                        player.ReferenceHub.hints.Show(new TextHint(text, new StringHintParameter[]{ new StringHintParameter(text) }, durationScalar: 1.0f));
                    }
                    catch(Exception ex)
                    {
                        ServerConsole.AddLog(ex.ToString());
                    }
                    //param.Serialize(player.ReferenceHub.netIdentity.);
                });
                response = "Success";
                return true;
            }
            else
            {
                response = "Fail";
                return false;
            }
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class HintCommand : ICommand
    {
        public string Command { get; } = "h";

        public string[] Aliases { get; } = new string[] { };

        public string Description { get; } = "show all hints for debug purposes";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            MyPlayer player;
            if (Player.TryGet(sender, out player))
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach(var argument in arguments)
                    stringBuilder.Append(argument + " ");

                stringBuilder.Remove(stringBuilder.Length - 1, 1);

                player.SendBroadcast(stringBuilder.ToString(), 60, shouldClearPrevious: true);
                player.ReceiveHint(stringBuilder.ToString(), 60);

                response = "Success";
                return true;
            }
            else
            {
                response = "Fail";
                return false;
            }
        }
    }

    public class HintTest
    {
        public static HintTest Singleton { get; private set; }

        [PluginEntryPoint("Hint testing", "1.0", "testing env", "The Riptide")]
        void EntryPoint()
        {
            Singleton = this;
            EventManager.RegisterEvents(this);
            FactoryManager.RegisterPlayerFactory(this, new MyPlayerFactory());
        }

    }
}
