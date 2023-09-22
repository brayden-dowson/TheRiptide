using MEC;
using PluginAPI.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheRiptide.TraitorAmongUsUtility;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public class Announcement
    {
        public string msg;
        public float time_left;

        public Announcement(string msg, float duration)
        {
            this.msg = msg;
            time_left = duration;
        }
    }

    public class Announcements
    {
        private static List<Announcement> announcements = new List<Announcement>();
        private static CoroutineHandle update;

        public static void Add(Announcement announcement)
        {
            announcements.Add(announcement);
            RefreshInnocentInfo();
        }

        public static void Start()
        {
            announcements.Clear();
            update = Timing.RunCoroutine(_AnnouncementUpdate());
        }

        public static void Stop()
        {
            Timing.KillCoroutines(update);
            BroadcastOverride.ClearLines(BroadcastPriority.Highest);
            BroadcastOverride.UpdateAllDirty();
        }


        private static IEnumerator<float> _AnnouncementUpdate()
        {
            TraitorAmongUs.round_timer = 0.0f;
            while (true)
            {
                try
                {
                    string current_msg = "<size=24><align=left><voffset=-7em>\n<voffset=0em>";
                    for (int i = announcements.Count - 1; i >= 0; i--)
                    {
                        current_msg += announcements[i].msg + "\n";
                        announcements[i].time_left -= 1.0f;
                    }

                    System.TimeSpan time_left = new System.TimeSpan(0, 0, Mathf.RoundToInt(TraitorAmongUs.RoundLength() * 60.0f - TraitorAmongUs.round_timer));
                    current_msg += "Time left: " + time_left.Minutes + ":" + time_left.Seconds.ToString("D2") + "\n";
                    foreach (var p in ReadyPlayers())
                        if (TraitorAmongUs.IsPlayerReady(p))
                            p.ReceiveHint(current_msg, 2);

                    announcements.RemoveAll(a => a.time_left < 0.0f);
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }

                yield return Timing.WaitForSeconds(1.0f);
            }
        }

        public static void RefreshInnocentInfo()
        {
            int innocent_count = 0;
            int traitor_count = 0;
            foreach (var p in Player.GetPlayers())
            {
                if (p.IsAlive)
                {
                    if (TraitorAmongUs.traitors.Contains(p.PlayerId))
                        traitor_count++;
                    else if(TraitorAmongUs.IsPlayerReady(p))
                        innocent_count++;
                }
            }

            foreach(var b in BodyManager.Unided.Values)
            {
                if (b.Real == TauRole.Traitor)
                    traitor_count++;
                else if (b.Real != TauRole.Unassigned)
                    innocent_count++;
            }

            foreach (var p in Player.GetPlayers())
            {
                if (p.IsAlive)
                {
                    if (TraitorAmongUs.detectives.Contains(p.PlayerId) || (!TraitorAmongUs.traitors.Contains(p.PlayerId) && !TraitorAmongUs.jesters.Contains(p.PlayerId) && !TraitorAmongUs.not_ready.Contains(p.PlayerId)))
                        BroadcastOverride.BroadcastLine(p, 5, 60 * 60, BroadcastPriority.Medium, "<align=left>" + traitor_count + " Traitors and " + innocent_count + " Innocents remaining");
                }
            }
            BroadcastOverride.UpdateAllDirty();
        }
    }
}
