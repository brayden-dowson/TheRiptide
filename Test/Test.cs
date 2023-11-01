using Achievements;
using PlayerRoles;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;
using System.Collections.Generic;

namespace TheRiptide
{
    public class Config
    {
        public Dictionary<AchievementName, string> AchievementDescription { get; set; } = new Dictionary<AchievementName, string>
        {
            { AchievementName.AccessGranted, "lol "+ Test.ColorRoleText(RoleTypeId.ClassD) }
        };
    }

    public class Test
    {
        [PluginConfig]
        public Config config;

        [PluginEntryPoint("Test", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            EventManager.RegisterEvents(this);
        }

        public static string ColorRoleText(RoleTypeId role)
        {
            PlayerRoleBase role_base = null;
            if (PlayerRoleLoader.AllRoles.TryGetValue(role, out role_base))
                return "<color=" + role_base.RoleColor.ToHex() + ">" + role.ToString() + "</color>";
            else
                return role.ToString();
        }
    }
}
