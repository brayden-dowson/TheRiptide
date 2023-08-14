using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using CustomPlayerEffects;
using HarmonyLib;
using MapGeneration;
using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp096;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;
        public float HealthScaling { get; set; } = 200.0f;
        public string Description { get; set; } = "A Shy Guy spawns outside the facility and is triggered by everyone. His rage never runs out and his speed increase the lower his health gets. His health scales with the number of players on the server. NTF are dispatched to stop him at all costs.\n\n";
    }

    public class EventHandler
    {
        public static int selected = 0;
        public static int player_count;
        public static Config config;

        public static void Start(Config config)
        {
            EventHandler.config = config;
            selected = 0;
        }

        public static void Stop()
        {

        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + ShyGuyXKEvent.Singleton.EventName + "\n<size=24>" + ShyGuyXKEvent.Singleton.EventDescription.Replace("\n", "") + "</size>", 90, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            if (player.PlayerId == selected)
                selected = 0;
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            selected = Player.GetPlayers().Where(p => p.IsReady).ToList().RandomItem().PlayerId;
            player_count = Player.Count;
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Overwatch || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Filmmaker)
                return true;

            int player_id = player.PlayerId;
            if (player_id == selected)
            {
                if (new_role != RoleTypeId.Scp096)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        Player p = Player.Get(player_id);
                        if (p != null)
                            p.SetRole(RoleTypeId.Scp096);
                    });
                    return false;
                }
            }
            else if(new_role.GetTeam() == Team.SCPs)
            {
                if (selected == 0 && new_role == RoleTypeId.Scp096)
                    return true;
                Timing.CallDelayed(0.0f, () =>
                {
                    Player p = Player.Get(player_id);
                    if (p != null)
                        p.SetRole(RoleTypeId.ClassD);
                });
                return false;
            }
            return true;
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            if (player == null)
                return;

            int player_id = player.PlayerId;
            if (role == RoleTypeId.Scp096)
            {
                ForceRage(player);
                Timing.CallDelayed(0.0f, () =>
                {
                    Player p = Player.Get(player_id);
                    if (p != null && p.Role == RoleTypeId.Scp096)
                        Teleport.RoomPos(player, RoomIdentifier.AllRoomIdentifiers.First(r => r.Zone == FacilityZone.Surface), new Vector3(131.925f, -11.208f, 27.378f));
                    p.EffectsManager.EnableEffect<Disabled>();
                });
            }
            else if(role.IsHuman())
            {
                foreach (var shyguy in Player.GetPlayers())
                {
                    if (shyguy.RoleBase is Scp096Role scp096 && IsRaging(scp096.StateController.RageState))
                    {
                        Scp096TargetsTracker tracker;
                        if (scp096.SubroutineModule.TryGetSubroutine(out tracker))
                            tracker.AddTarget(player.ReferenceHub, false);
                    }
                }
            }
        }

        [PluginEvent(ServerEventType.PlayerDamage)]
        void OnPlayerDamaged(Player attacker, Player victim, DamageHandlerBase damageHandler)
        {
            if (victim == null)
                return;

            if(victim.Role == RoleTypeId.Scp096)
            {
                Timing.CallDelayed(0.0f,()=>
                {
                    Scp096Role scp096 = victim.RoleBase as Scp096Role;
                    float x = (victim.Health + scp096.HumeShieldModule.HsCurrent) / (config.HealthScaling * player_count);
                    if (x < 0.5)
                        victim.EffectsManager.DisableEffect<Disabled>();
                    byte speed_boost = (byte)Mathf.Clamp((int)((1.0f - x) * 255.0f), 0, 255);
                    victim.EffectsManager.ChangeState<MovementBoost>(speed_boost);
                });
                if (damageHandler is ExplosionDamageHandler explosion_handler)
                    explosion_handler.Damage = explosion_handler.Damage * 0.33333f;
            }
        }

        [PluginEvent(ServerEventType.PlayerDeath)]
        void OnPlayerDeath(Player victim, Player attacker, DamageHandlerBase damageHandler)
        {
            if (victim == null)
                return;

            foreach (var p in Player.GetPlayers())
            {
                if (p.RoleBase is Scp096Role scp096)
                {
                    Scp096TargetsTracker tracker;
                    if (scp096.SubroutineModule.TryGetSubroutine(out tracker))
                        tracker.RemoveTarget(victim.ReferenceHub);
                }
            }
            selected = 0;
        }

        [PluginEvent(ServerEventType.Scp096ChangeState)]
        public void OnScp096ChangeState(Player player, Scp096RageState rageState)
        {
            if (!IsRaging(rageState))
            {
                ForceRage(player);
            }
        }

        public static bool IsRaging(Scp096RageState rageState)
        {
            return (rageState == Scp096RageState.Distressed || rageState == Scp096RageState.Enraged);
        }

        private static void ForceRage(Player player)
        {
            player.SendBroadcast("you will forcefully enrage in 5 seconds.", 5, shouldClearPrevious: true);
            Timing.CallDelayed(5.0f, () =>
            {
                if (player.RoleBase is Scp096Role scp096)
                {
                    if (!IsRaging(scp096.StateController.RageState))
                    {
                        scp096.StateController.SetRageState(Scp096RageState.Distressed);
                        Scp096TargetsTracker tracker;
                        if (scp096.SubroutineModule.TryGetSubroutine(out tracker))
                        {
                            foreach (var p in Player.GetPlayers())
                                if (p.IsAlive && p.Role.IsHuman())
                                    tracker.AddTarget(p.ReferenceHub, false);
                        }
                        else
                            Log.Error("no TargetTracker");
                        Scp096RageManager rage;
                        if (scp096.SubroutineModule.TryGetSubroutine(out rage))
                        {
                            rage.EnragedTimeLeft = 60.0f * 60.0f;
                            rage.ServerSendRpc(true);
                        }
                        else
                            Log.Error("no RageManager");
                    }
                }
            });
        }
    }

    public class ShyGuyXKEvent:IEvent
    {
        public static ShyGuyXKEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Shy Guy XK";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "SGXK";
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
            harmony = new Harmony("ShyGuyXKEvent");
            harmony.PatchAll();
            Log.Info(EventName + " event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            EventHandler.Stop();
            harmony.UnpatchAll("ShyGuyXKEvent");
            harmony = null;
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Shy Guy XK Event", "1.0.0", "", "The Riptide")]
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
