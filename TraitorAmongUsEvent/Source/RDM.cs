using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerStatsSystem;
using static TheRiptide.Utility;
using UnityEngine;
using MEC;

namespace TheRiptide
{
    class Leeway
    {
        public float value = 0.0f;
        public IDableBody seen = null;
        public float avg_rate = 0.0f;
        public bool exceeded = false;
    }

    public class RDM
    {
        //private const float damage_threshold = 90.0f;
        //private const int kill_threshold = 2;
        private static Dictionary<int, float> player_ffdmg = new Dictionary<int, float>();
        private static Dictionary<int, int> player_ffkills = new Dictionary<int, int>();
        private static HashSet<int> player_grace = new HashSet<int>();
        private static Action<ReferenceHub, DamageHandlerBase> on_player_damaged;
        private static Action<ReferenceHub, DamageHandlerBase> on_player_died;
        private static CoroutineHandle update;

        public static void Start()
        {
            on_player_damaged = (hub, handler) =>
            {
                if (handler is AttackerDamageHandler attacker_handler)
                {
                    Player victim = Player.Get(hub);
                    Player attacker = Player.Get(attacker_handler.Attacker.Hub);
                    bool attacker_is_traitor = TraitorAmongUs.GetPlayerTauRole(attacker) == TauRole.Traitor;
                    player_grace.Remove(attacker.PlayerId);
                    if (!player_ffdmg.ContainsKey(attacker.PlayerId))
                        player_ffdmg.Add(attacker.PlayerId, 0.0f);

                    if (!attacker_is_traitor && player_grace.Contains(victim.PlayerId))
                        player_ffdmg[attacker.PlayerId] += attacker_handler.DealtHealthDamage;
                    else
                    {
                        bool victim_is_traitor = TraitorAmongUs.GetPlayerTauRole(victim) == TauRole.Traitor;
                        if (victim_is_traitor && attacker_is_traitor)
                            player_ffdmg[attacker.PlayerId] += attacker_handler.DealtHealthDamage;
                    }
                }

            };
            on_player_died = (hub, handler) =>
            {
                if (handler is AttackerDamageHandler attacker_handler)
                {
                    Player victim = Player.Get(hub);
                    Player attacker = Player.Get(attacker_handler.Attacker.Hub);
                    bool attacker_is_traitor = TraitorAmongUs.GetPlayerTauRole(attacker) == TauRole.Traitor;
                    player_grace.Remove(attacker.PlayerId);
                    if (!player_ffkills.ContainsKey(attacker.PlayerId))
                        player_ffkills.Add(attacker.PlayerId, 0);

                    if (!attacker_is_traitor && player_grace.Contains(victim.PlayerId))
                        player_ffkills[attacker.PlayerId]++;
                    else
                    {
                        bool victim_is_traitor = TraitorAmongUs.GetPlayerTauRole(victim) == TauRole.Traitor;
                        if (victim_is_traitor == attacker_is_traitor)
                            player_ffkills[attacker.PlayerId]++;
                    }
                }
            };
            PlayerStats.OnAnyPlayerDamaged += on_player_damaged;
            PlayerStats.OnAnyPlayerDied += on_player_died;
            update = Timing.RunCoroutine(_Update());
        }

        public static void Stop()
        {
            PlayerStats.OnAnyPlayerDamaged -= on_player_damaged;
            PlayerStats.OnAnyPlayerDied -= on_player_died;
            Timing.KillCoroutines(update);
        }

        public static void Reset()
        {
            player_ffdmg.Clear();
            player_ffkills.Clear();
            player_grace.Clear();
            foreach (var p in Player.GetPlayers())
            {
                if (p.IsReady && !TraitorAmongUs.jesters.Contains(p.PlayerId))
                    player_grace.Add(p.PlayerId);
            }
        }

        public static IEnumerator<float> _Update()
        {
            //const float max_leeway = 3.0f;
            Dictionary<int, Leeway> player_leeway = new Dictionary<int, Leeway>();
            while(true)
            {
                foreach(var p in ReadyPlayers())
                {
                    if (!p.IsAlive)
                        continue;
                    if (!player_leeway.ContainsKey(p.PlayerId))
                        player_leeway.Add(p.PlayerId, new Leeway());

                    Leeway leeway = player_leeway[p.PlayerId];
                    if (leeway.exceeded)
                        continue;
                    if (leeway.seen != null && !BodyManager.Unided.ContainsKey(leeway.seen.Collider))
                        leeway.seen = null;

                    float sqr_dist = 32.0f;
                    Vector3 start = p.ReferenceHub.PlayerCameraReference.position;
                    Vector3 look_dir = p.ReferenceHub.PlayerCameraReference.rotation * Vector3.forward;
                    foreach (var b in BodyManager.Unided.Values.Where(b => Vector3.SqrMagnitude(start - b.Collider.transform.position) < sqr_dist))
                    {
                        if (leeway.seen != null && !(Vector3.SqrMagnitude(leeway.seen.Collider.transform.position - p.Position) > Vector3.SqrMagnitude(b.Collider.transform.position - p.Position)))
                            continue;

                        Vector3 body_dir = (b.Collider.transform.position - start).normalized;
                        float distance = Vector3.Distance(start, b.Collider.transform.position);
                        if (Vector3.Dot(body_dir, look_dir) > 0.3f && !Physics.Raycast(start, body_dir, distance, Physics.AllLayers & ~(1 << 13) & ~(1 << 17)))
                            leeway.seen = b;
                    }

                    //for (int i = 0; i < 1; i++)
                    //{
                    //    var b = BodyManager.ReadyUpBody;
                    //    if (b == null)
                    //        continue;
                    //    Vector3 body_dir = (b.Collider.transform.position - start).normalized;
                    //    float distance = Vector3.Distance(start, b.Collider.transform.position);
                    //    if (Vector3.Dot(body_dir, look_dir) > 0.3f && !Physics.Raycast(start, body_dir, distance, Physics.AllLayers & ~(1 << 13) & ~(1 << 17)))
                    //        leeway.seen = b;
                    //}

                    if (leeway.seen != null)
                    {
                        Vector3 body_dir = (leeway.seen.Collider.transform.position - start).normalized;
                        float movement = Vector3.Dot(body_dir, p.Velocity.normalized);
                        float rate = Mathf.Pow(movement - 1, 2) / Vector2.Distance(start, leeway.seen.Collider.transform.position);
                        leeway.avg_rate = (leeway.avg_rate + rate) / 2.0f;
                        leeway.value += leeway.avg_rate * Timing.DeltaTime;
                        if (leeway.value > TraitorAmongUsEvent.Singleton.EventConfig.UnidedBodyLeeway)
                        {
                            EndGrace(p);
                            leeway.exceeded = true;
                        }
                    }
                    else
                    {
                        leeway.avg_rate = 0.0f;
                        leeway.value = 0.0f;
                    }
                    //BroadcastOverride.ClearLines(p, BroadcastPriority.Highest);
                    //BroadcastOverride.BroadcastLine(p, 1, 1.0f, BroadcastPriority.Highest, (leeway.seen != null).ToString() + " | " + leeway.value + " | " + leeway.avg_rate);
                    //BroadcastOverride.UpdateIfDirty(p);
                }
                yield return Timing.WaitForOneFrame;
            }
        }

        public static void EndGrace()
        {
            player_grace.Clear();
        }

        public static void EndGrace(Player player)
        {
            player_grace.Remove(player.PlayerId);
        }

        public static bool OverRDMLimit(Player player)
        {
            return (player_ffdmg.ContainsKey(player.PlayerId) && player_ffdmg[player.PlayerId] >= TraitorAmongUsEvent.Singleton.EventConfig.RdmDamageThreshold) ||
                (player_ffkills.ContainsKey(player.PlayerId) && player_ffkills[player.PlayerId] >= TraitorAmongUsEvent.Singleton.EventConfig.RdmKillThreshold);
        }

        public static void ForcePlayerOverLimit(Player player)
        {
            if (!player_ffkills.ContainsKey(player.PlayerId))
                player_ffkills.Add(player.PlayerId, TraitorAmongUsEvent.Singleton.EventConfig.RdmKillThreshold);
            else
                player_ffkills[player.PlayerId] = TraitorAmongUsEvent.Singleton.EventConfig.RdmKillThreshold;
        }

        public static void ForgivePlayer(Player player)
        {
            player_ffdmg.Remove(player.PlayerId);
            player_ffkills.Remove(player.PlayerId);
        }
    }
}
