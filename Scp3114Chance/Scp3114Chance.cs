using HarmonyLib;
using MEC;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Scp3114Chance
{
    public class Config
    {
        public float SpawnChance { get; set; } = 1.0f;
    }
    public class Scp3114Chance
    {
        public static Scp3114Chance Singelton { get; private set; }

        [PluginConfig]
        public Config config;

        private Harmony harmony;

        [PluginEntryPoint("Scp3114Chance", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            Singelton = this;
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        [PluginEvent(ServerEventType.WaitingForPlayers)]
        void OnWaitingForPlayers()
        {
            Timing.CallDelayed(0.0f, () =>
            {
                try
                {
                    Type event_manager_type = GetType("CedMod.Addons.Events.EventManager");
                    if (!(event_manager_type != null && event_manager_type.GetField("CurrentEvent", BindingFlags.Public | BindingFlags.Static).GetValue(null) != null))
                    {
                        harmony = new Harmony("Scp3114Chance");
                        harmony.PatchAll();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
        }

        [PluginEvent(ServerEventType.RoundEnd)]
        void OnRoundEnded(RoundSummary.LeadingTeam leadingTeam)
        {
            harmony.UnpatchAll("Scp3114Chance");
        }

        [PluginEvent(ServerEventType.RoundRestart)]
        void OnRoundRestart()
        {
            harmony.UnpatchAll("Scp3114Chance");
        }


        public static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
