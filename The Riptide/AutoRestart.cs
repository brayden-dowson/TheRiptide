using PluginAPI.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MEC;
using PluginAPI.Core;
using System.ComponentModel;

namespace AutoRestart
{
    public class Config
    {
        [Description("Time in 24h to restart the server")]
        public int Time = 4;
    }

    public class AutoRestart
    {
        [PluginConfig]
        public Config config;

        [PluginEntryPoint("Auto Restart", "1.0", "needs no explanation", "The Riptide")]
        void EntryPoint()
        {
            DateTime now = DateTime.Now;
            DateTime restart = new DateTime(now.Year, now.Month, now.Day, config.Time, 0, 0);
            TimeSpan time = restart.Subtract(now);
            if (time.TotalSeconds < 0)
                time+= new TimeSpan(1, 0, 0, 0);
            ServerConsole.AddLog("Time Now is: " + now.Hour + " hours, " + now.Minute + " minutes and " + now.Second + " seconds");
            ServerConsole.AddLog("Server Restart in: " + time.Days +" days, " + time.Hours + " hours, " + time.Minutes + " minutes and " + time.Seconds + " seconds");
            float total_seconds = (float)time.TotalSeconds;
            if (total_seconds > 15.0f)
                Timing.CallDelayed(total_seconds - 10.0f, () => { Server.SendBroadcast(config.Time + ":00 Server Restart in 10 seconds", 10, Broadcast.BroadcastFlags.Normal, true); });
            Timing.CallDelayed(total_seconds, () => { Server.Restart(); });
        }
    }
}
