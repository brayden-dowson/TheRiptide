using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using HarmonyLib;
using MEC;
using PlayerRoles;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using Respawning;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        public int Health { get; set; } = 500;
        public int Shield { get; set; } = 0;

        public string Description { get; set; } = "Two people spawn as 173 the rest are Class-D. 173 has less health/sheild. 173s multiply on kill. Class-D must escape and kill all the 173s to win. 173s win if they kill all the Chaos/Class-Ds.\n\n";
    }

    public class EventHandler
    {
        public static Config config;
        private static HashSet<int> scp173s = new HashSet<int>();
        private static CoroutineHandle win_condition;

        public static void Start(Config config)
        {
            EventHandler.config = config;
            Round.IsLocked = true;
            scp173s.Clear();
        }

        public static void Stop()
        {
            Timing.KillCoroutines(win_condition);
            scp173s.Clear();
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + PeanutMultiplicationRevisedEntryEvent.Singleton.EventName + "\n<size=32>" + PeanutMultiplicationRevisedEntryEvent.Singleton.EventDescription + "</size>", 30, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            List<Player> players = ReadyPlayers();
            scp173s.Add(players.PullRandomItem().PlayerId);
            if (!players.IsEmpty())
                scp173s.Add(players.PullRandomItem().PlayerId);

            win_condition = Timing.RunCoroutine(_CheckWinCondition());
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        bool OnRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted || new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Overwatch || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Filmmaker)
                return true;

            if (scp173s.Contains(player.PlayerId))
            {
                if (new_role != RoleTypeId.Scp173)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        try
                        {
                            player.SetRole(RoleTypeId.Scp173);
                        }
                        catch (System.Exception e)
                        {
                            Log.Error(e.ToString());
                        }
                    });
                    return false;
                }
            }
            else
            {
                if (new_role.GetTeam() != Team.ChaosInsurgency && new_role != RoleTypeId.ClassD)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        try
                        {
                            player.SetRole(RoleTypeId.ClassD);
                        }
                        catch(System.Exception e)
                        {
                            Log.Error(e.ToString());
                        }
                    });
                    return false;
                }
            }
            return true;
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            if (player == null || !Round.IsRoundStarted)
                return;

            if (scp173s.Contains(player.PlayerId) && role == RoleTypeId.Scp173)
            {
                Timing.CallDelayed(0.0f,()=>
                {
                    if (player.Role != RoleTypeId.Scp173)
                        return;

                    HumeShieldStat hume = null;
                    if (player.ReferenceHub.playerStats.TryGetModule(out hume))
                        hume.CurValue = 0.0f;
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerDying)]
        void OnPlayerDying(Player victim, Player attacker, DamageHandlerBase damageHandler)
        {
            if (victim == null || !Round.IsRoundStarted)
                return;

            Vector3 pos = victim.Position;
            if (attacker != null && attacker.Role == RoleTypeId.Scp173)
            {
                scp173s.Add(victim.PlayerId);
                Timing.CallDelayed(0.0f, () =>
                {
                    if (victim.Role != RoleTypeId.Spectator)
                        return;
                    victim.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Scp173, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);
                    victim.Position = pos;
                });
            }
        }

        private static IEnumerator<float> _CheckWinCondition()
        {
            while(true)
            {
                try
                {
                    int scp173_count = 0;
                    int human_count = 0;
                    foreach (var p in ReadyPlayers())
                    {
                        if (p.Role == RoleTypeId.Scp173)
                            scp173_count++;
                        else if (p.IsHuman && p.Role != RoleTypeId.Tutorial)
                            human_count++;
                    }
                    if (scp173_count == 0 || human_count == 0)
                    {
                        Round.IsLocked = false;
                        break;
                    }
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }
    }

    public class PeanutMultiplicationRevisedEntryEvent : IEvent
    {
        public static PeanutMultiplicationRevisedEntryEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Peanut Multiplication [Revised Entry]";
        public string EvenAuthor { get; } = "The Riptide. Idea by Wallace";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "PMRE";
        public bool OverrideWinConditions { get; }
        public bool BulletHolesAllowed { get; set; } = false;
        public PluginHandler PluginHandler { get; }
        public IEventConfig Config => EventConfig;

        [PluginConfig]
        public Config EventConfig;

        private Harmony harmony;

        public void PrepareEvent()
        {
            Log.Info(EventName + " event is preparing");
            IsRunning = true;
            EventHandler.Start(EventConfig);
            harmony = new Harmony("PeanutMultiplicationRevisedEntryEvent");
            harmony.PatchAll();
            Log.Info(EventName + " event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            EventHandler.Stop();
            harmony.UnpatchAll("PeanutMultiplicationRevisedEntryEvent");
            harmony = null;
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Peanut Multiplication Revised", "1.0.0", "173 revised entry(peanut multiplication)", "The Riptide")]
        public void OnEnabled()
        {
            Singleton = this;
            //PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
            Handler = PluginHandler.Get(this);
        }

        [PluginUnload]
        public void OnDisabled()
        {
            StopEvent();
        }
    }
}
