using PlayerRoles;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide
{
    public class CustomRole
    {
        public string name;
        public string description;
        public RoleTypeId role_base;
    }

    public static class CustomRoleManager
    {
        private static bool IsEnabled = true;
        private static Dictionary<int, CustomRole> all_roles = new Dictionary<int, CustomRole>();
        private static Dictionary<int, HashSet<int>> player_roles = new Dictionary<int, HashSet<int>>();

        public static int RegisterRole(CustomRole role)
        {
            int id = Enumerable.Range(0, int.MaxValue).Except(all_roles.Keys).FirstOrDefault();
            all_roles.Add(id, role);
            return id;
        }

        public static void UnregisterRole(int role_id)
        {
            all_roles.Remove(role_id);
            foreach (var p in player_roles.Keys.ToList())
                player_roles[p].Remove(role_id);
        }

        public static void AddRole(Player player, int role_id)
        {
            player_roles[player.PlayerId].Add(role_id);
        }

        public static void RemoveRole(Player player, int role_id)
        {
            player_roles[player.PlayerId].Remove(role_id);
        }

        public static void BroadcastRole(Player player)
        {
            if (player_roles.ContainsKey(player.PlayerId))
            {
                string role_broadcast = "";
                foreach (var id in player_roles[player.PlayerId])
                {
                    CustomRole role = all_roles[id];
                    role_broadcast += "[" + role.name + "] " + role.description + "\n";
                }
                player.SendBroadcast(role_broadcast, 15);
            }
        }

        public static void BroadcastRole()
        {
            foreach (var p in Player.GetPlayers())
                BroadcastRole(p);
        }

        public static bool HasAnyRole(Player player)
        {
            if (!player_roles.ContainsKey(player.PlayerId))
                return false;
            return !player_roles[player.PlayerId].IsEmpty();
        }

        public static bool HasRole(Player player, int role_id)
        {
            if (!player_roles.ContainsKey(player.PlayerId))
                return false;
            return player_roles[player.PlayerId].Contains(role_id);
        }
    }
}
