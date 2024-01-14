using CedMod.Addons.Events;
using HarmonyLib;
using PlayerRoles;
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
using PlayerRoles.PlayableScps.HumeShield;
using CustomPlayerEffects;
using MEC;
using CedMod.Addons.Events.Interfaces;
using PluginAPI.Events;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        public string Description { get; set; } = "Humans Vs SCPs. All human teams are allied to defeat the SCPs. SCPs gain a speed boost equivilant to a cola, +1000 health and +1000 sheild (at 20% health)\n\n";
    }

    public class EventHandler
    {
        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + AStrangeAllianceEvent.Singleton.EventName + "\n<size=30>" + AStrangeAllianceEvent.Singleton.EventDescription.Replace("\n", "") + "</size>", 30, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.PlayerDamage)]
        void OnPlayerDamage(PlayerDamageEvent e)
        {
            if (e.Player == null || e.Target == null)
                return;

            if (e.Target.IsHuman && e.Player.IsHuman && e.Target != e.Player)
                if (e.DamageHandler is StandardDamageHandler standard)
                    standard.Damage = 0.0f;
        }

        [PluginEvent(ServerEventType.PlayerReceiveEffect)]
        bool OnReceiveEffect(Player player, StatusEffectBase effect, byte intensity, float duration)
        {
            if (player != null && player.IsAlive && player.IsHuman && effect is Flashed)
                return false;
            else
                return true;
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            if(player != null && player.IsSCP)
            {
                Timing.CallDelayed(0.1f, () =>
                {
                    player.EffectsManager.ChangeState<MovementBoost>(20, 0);
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerDeath)]
        void OnPlayerDeath(Player victim, Player attacker, DamageHandlerBase damageHandler)
        {
            int scps_alive = 0;
            int humans_alive = 0;
            foreach (Player player in Player.GetPlayers())
            {
                if (player.IsSCP)
                    scps_alive++;
                else
                    humans_alive++;
            }

            if(scps_alive == 0)
            {
                foreach(Player player in Player.GetPlayers())
                {
                    player.SendBroadcast("Humans Win!", 10, shouldClearPrevious: true);
                }
                Round.End();
                Timing.CallDelayed(15.0f, () => Round.Restart(false));
            }
            else if(humans_alive == 0)
            {
                foreach (Player player in Player.GetPlayers())
                {
                    player.SendBroadcast("Scps Win!", 10, shouldClearPrevious: true);
                }
                Round.End();
                Timing.CallDelayed(15.0f, () => Round.Restart(false));
            }
        }


        public static void Start()
        {

        }

        public static void Stop()
        {

        }
    }

    public class AStrangeAllianceEvent : IEvent
    {
        public static AStrangeAllianceEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "A Strange Alliance Event";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription 
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); } 
        }
        public string EventPrefix { get; } = "ASA";
        public bool OverrideWinConditions { get; }
        public bool BulletHolesAllowed { get; set; } = false;
        public PluginHandler PluginHandler { get; }
        public IEventConfig Config => EventConfig;

        [PluginConfig]
        public Config EventConfig;

        private Harmony harmony;

        public void PrepareEvent()
        {
            Log.Info("AStrangeAllianceEvent is preparing");
            IsRunning = true;
            EventHandler.Start();
            harmony = new Harmony("AStrangeAllianceEvent");
            harmony.PatchAll();
            Log.Info("AStrangeAllianceEvent is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            EventHandler.Stop();
            harmony.UnpatchAll("AStrangeAllianceEvent");
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("A Strange Alliance Event", "1.0.0", "scps vs everyone. scps gain +1000 maxhealth and shield", "The Riptide")]
        public void OnEnabled()
        {
            Singleton = this;
            //PrepareEvent();
            Handler = PluginHandler.Get(this);
        }

        [PluginUnload]
        public void OnDisabled()
        {
            StopEvent();
        }
    }
}
