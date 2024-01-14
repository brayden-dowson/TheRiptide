using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp1507;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        public string Description { get; set; } = "A single Flamingo spawns in LCZ 173s room. Room opens 30 seconds into the round. Flamingos infect others on kill. Otherwise its a normal round\n\n";

        public string AlphaFlamingoSpawnMessage { get; set; } = "You have spawned as an infectious Flamingo, All entities killed are converted to Flamingos. You are very weak alone and can die easily to SCPs/Guards so build a horde first!";

        public string FlamingoSpawnMessage { get; set; } = "You are a infectious Flamingo, All entities killed are converted to Flamingos";
    }

    public class EventHandler
    {
        private static Config config;

        public static void Start(Config config)
        {
            EventHandler.config = config;
        }

        public static void Stop()
        {
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            Timing.CallDelayed(1.0f, ()=>
            {
                var targets = Player.GetPlayers().Where(p => p.Role == RoleTypeId.ClassD).ToList();
                if(targets.IsEmpty())
                {
                    Log.Info("No ClassD found to convert to flamingo");
                    return;
                }
                targets.RandomItem().ReferenceHub.roleManager.ServerSetRole(RoleTypeId.AlphaFlamingo, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.AssignInventory);

                var door = DoorVariant.AllDoors.First(d => d is Timed173PryableDoor) as Timed173PryableDoor;
                if (door != null)
                {
                    door._timeMark = 30.0f;
                }
                else
                    Log.Info("Could not find 173s door");
            });
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(PlayerChangeRoleEvent e)
        {
            if (e.Player == null || !Round.IsRoundStarted)
                return true;

            if(e.NewRole == RoleTypeId.Scp3114)
            {
                Log.Info("Found scp 3114");
                Timing.CallDelayed(0.0f, () =>
                {
                    e.Player.SetRole(RoleTypeId.ClassD);
                    Log.Info("Spawning Scp3114 as ClassD");
                });
                return false;
            }
            return true;
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(PlayerSpawnEvent e)
        {
            if (e.Player == null || !Round.IsRoundStarted)
                return;

            if(e.Role == RoleTypeId.AlphaFlamingo)
            {
                e.Player.Position = RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Lcz173).transform.TransformPoint(new UnityEngine.Vector3(17.5f, 12.129f, 8.0f));
                e.Player.SendBroadcast(config.AlphaFlamingoSpawnMessage, 45, shouldClearPrevious: true);
            }
            else if (e.Role == RoleTypeId.Flamingo)
            {
                e.Player.SendBroadcast(config.FlamingoSpawnMessage, 10, shouldClearPrevious: true);
            }
        }

        [PluginEvent(ServerEventType.PlayerDying)]
        bool OnPlayerDying(PlayerDyingEvent e)
        {
            if (e.Player == null || e.Attacker == null)
                return true;

            if(e.DamageHandler is Scp1507DamageHandler handler)
            {
                e.Player.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Flamingo, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.AssignInventory);
                return false;
            }

            return true;
        }
    }

    public class FlamingoInfectionEvent : IEvent
    {
        public static FlamingoInfectionEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Flamingo Infection";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "FI";
        public bool OverrideWinConditions { get; }
        public bool BulletHolesAllowed { get; set; } = false;
        public PluginHandler PluginHandler { get; }
        public IEventConfig Config => EventConfig;

        [PluginConfig]
        public Config EventConfig;

        //private Harmony harmony;

        public void PrepareEvent()
        {
            Log.Info(EventName + " event is preparing");
            IsRunning = true;
            //harmony = new Harmony("FlamingoInfectionEvent");
            //harmony.PatchAll();
            EventHandler.Start(EventConfig);
            Log.Info(EventName + " event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            //harmony.UnpatchAll("FlamingoInfectionEvent");
            EventHandler.Stop();
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Flamingo Infection Event", "1.0.0", "Flamingo Infection", "The Riptide")]
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
