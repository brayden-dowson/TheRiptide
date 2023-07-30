using MEC;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide
{
    public class Config
    {
        public string Message { get; set; } = "Dont forget to join the discord at https://discord.com/invite/PzR7dCvVAw check server info for a clickable link";
        [Description("delay in minutes after round start")]
        public int Delay { get; set; } = 10;
        [Description("duration in seconds")]
        public ushort Duration { get; set; } = 15;
    }

    public class DiscordMessage
    {
        [PluginConfig]
        public Config config;


        [PluginEntryPoint("Discord Message", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            Timing.CallDelayed(config.Delay * 60.0f, () =>
            {
                foreach (var player in Utility.ReadyPlayers())
                    player.SendBroadcast(config.Message, config.Duration);
            });
        }

    }
}
