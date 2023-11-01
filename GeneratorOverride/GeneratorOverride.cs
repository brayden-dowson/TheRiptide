using HarmonyLib;
using MapGeneration.Distributors;
using MEC;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide
{
    public class Config
    {
        [Description("Amount of generators requiered to be engaged to recontain computer. If this value is larger than the amount spawned it will use the spawn count instead. Default = 3")]
        public int RecontainThreshold { get; set; } = 3;
        [Description("Minimum amount of generators to spawn. For larger values above 8 there is a chance not all the generators will spawn due to spots being taken by pedestals. Default = 3")]
        public int MinGenerators { get; set; } = 3;
        [Description("Maximum amount of generators to spawn. Default = 3")]
        public int MaxGenerators { get; set; } = 3;

        [Description("Amount of time it takes to engage a generator. Default = 125")]
        public float ActivationTime { get; set; } = 125;
        [Description("Amount of time it takes to reset a generators engage time after deactivation from 0. Default = 125")]
        public float DeactivationTime { get; set; } = 125;

        [Description("If enabled only one generator per room can spawn. Default = false")]
        public bool OnePerRoom { get; set; } = false;
    }

    public class GeneratorOverride
    {
        public static GeneratorOverride Singelton { get; private set; }

        [PluginConfig]
        public Config config;

        private Harmony harmony;

        [PluginEntryPoint("GeneratorOverride", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            Singelton = this;
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        [PluginEvent(ServerEventType.WaitingForPlayers)]
        void OnWaitingForPlayers()
        {
            Timing.CallDelayed(0.0f,()=>
            {
                try
                {
                    Type event_manager_type = GetType("CedMod.Addons.Events.EventManager");
                    if (!(event_manager_type != null && event_manager_type.GetField("CurrentEvent", BindingFlags.Public | BindingFlags.Static).GetValue(null) != null))
                    {
                        Log.Info("patching");
                        harmony = new Harmony("GeneratorOverride");
                        harmony.PatchAll();
                    }
                }
                catch(Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
        }

        [PluginEvent(ServerEventType.RoundEnd)]
        void OnRoundEnded(RoundSummary.LeadingTeam leadingTeam)
        {
            harmony.UnpatchAll("GeneratorOverride");
        }

        [PluginEvent(ServerEventType.RoundRestart)]
        void OnRoundRestart()
        {
            harmony.UnpatchAll("GeneratorOverride");
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
