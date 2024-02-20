using Achievements;
using HarmonyLib;
using Mirror;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(AchievementHandlerBase), nameof(AchievementHandlerBase.ServerAchieve))]
    class AchievementHandlerBasePatch
    {
        public static bool Prefix(NetworkConnection conn, AchievementName targetAchievement)
        {
            conn.Send(new AchievementManager.AchievementMessage()
            {
                AchievementId = (byte)targetAchievement
            });

            try
            {
                MVP.Instance.OnPlayerAchieve(Player.Get(conn.identity), targetAchievement);
            }
            catch (Exception ex)
            {
                Log.Error("TheRiptide.Patches.AchievementHandlerBasePatch Error: " + ex.ToString());
            }

            return false;
        }
    }
}
