using MEC;
using PluginAPI.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide
{
    public class StandingDisplay
    {
        private static HashSet<int> currently_viewing = new HashSet<int>();
        private static CoroutineHandle update;
        private static string current_standing = "";
        private static bool dirty = true;

        public static void Start()
        {
            update = Timing.RunCoroutine(_Update());
        }

        public static void Stop()
        {
            Timing.KillCoroutines(update);
        }

        public static void AddPlayer(Player player)
        {
            currently_viewing.Add(player.PlayerId);
        }

        public static void RemovePlayer(Player player)
        {
            currently_viewing.Remove(player.PlayerId);
        }

        public static void UpdateStanding(string new_standing)
        {
            dirty = true;
            current_standing = new_standing;
        }

        private static IEnumerator<float> _Update()
        {
            HashSet<int> viewing_previously = new HashSet<int>();
            while(true)
            {
                try
                {
                    foreach (var id in viewing_previously)
                    {
                        if(!currently_viewing.Contains(id))
                        {
                            Player p;
                            if (Player.TryGet(id, out p) && p.IsReady)
                                p.ReceiveHint("", 1);
                        }
                    }

                    HashSet<int> viewing_previously_copy = viewing_previously.ToHashSet();
                    viewing_previously.Clear();
                    foreach (var id in currently_viewing.ToList())
                    {
                        Player p;
                        if (!Player.TryGet(id, out p) && p.Role == PlayerRoles.RoleTypeId.Spectator)
                            currently_viewing.Remove(id);
                        if (!viewing_previously_copy.Contains(id) || dirty)
                            p.ReceiveHint(current_standing, 3000);
                        viewing_previously.Add(id);
                    }
                    dirty = false;
                }
                catch(System.Exception ex)
                {
                    PluginAPI.Core.Log.Error(ex.ToString());
                }
                yield return Timing.WaitForOneFrame;
            }
        }
    }
}
