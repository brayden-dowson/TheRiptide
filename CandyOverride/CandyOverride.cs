using HarmonyLib;
using InventorySystem.Items.Usables.Scp330;
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

namespace TheRiptide
{
    public class Config
    {
        public int MaxCandies { get; set; } = 2;
        public Dictionary<CandyKindID, float> CandyWeights { get; set; } = Scp330Candies.CandiesById.ToDictionary(x => x.Key, x => x.Value.SpawnChanceWeight);
    }

    public class CandyOverride
    {
        public static CandyOverride Singelton { get; private set; }

        [PluginConfig]
        public Config config;

        private Harmony harmony;

        [PluginEntryPoint("CandyOverride", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            Singelton = this;
            PluginAPI.Events.EventManager.RegisterEvents(this);
            harmony = new Harmony("CandyOverride");
            harmony.PatchAll();
        }

        [PluginUnload]
        public void OnDisabled()
        {
            harmony.UnpatchAll("CandyOverride");
        }

//[PluginEvent(ServerEventType.WaitingForPlayers)]
//void OnWaitingForPlayers()
//{
//    Timing.CallDelayed(0.0f, () =>
//    {
//        try
//        {
//            Type event_manager_type = GetType("CedMod.Addons.Events.EventManager");
//            if (!(event_manager_type != null && event_manager_type.GetField("CurrentEvent", BindingFlags.Public | BindingFlags.Static).GetValue(null) != null))
//            {
//                harmony = new Harmony("CandyOverride");
//                harmony.PatchAll();
//            }
//        }
//        catch (Exception ex)
//        {
//            Log.Error(ex.ToString());
//        }
//    });
//}

//[PluginEvent(ServerEventType.RoundEnd)]
//void OnRoundEnded(RoundSummary.LeadingTeam leadingTeam)
//{
//    harmony.UnpatchAll("CandyOverride");
//}

//[PluginEvent(ServerEventType.RoundRestart)]
//void OnRoundRestart()
//{
//    harmony.UnpatchAll("CandyOverride");
//}


//public static Type GetType(string typeName)
//{
//    var type = Type.GetType(typeName);
//    if (type != null) return type;
//    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
//    {
//        type = a.GetType(typeName);
//        if (type != null)
//            return type;
//    }
//    return null;
//}
    }
}
