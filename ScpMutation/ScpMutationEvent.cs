using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using MEC;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
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
        public string Description { get; set; } = "Normal round but the SCPs have random sizes\n\n";
    }

    public class EventHandler
    {
        public static void Start()
        {
        }

        public static void Stop()
        {
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + ScpMutationEvent.Singleton.EventName + "\n<size=32>" + ScpMutationEvent.Singleton.EventDescription + "</size>", 20, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        void OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null)
                return;

            if (oldRole != null && oldRole.RoleTypeId.GetFaction() == Faction.SCP)
            {
                SetScale(player, 1.0f);
            }
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            if(role.GetFaction() == Faction.SCP)
            {
                Timing.CallDelayed(1.0f,()=>
                {
                    float x = Random.Range(0, 1.15f);
                    float y = Random.Range(0.5f, 1.15f);
                    float z = Random.Range(0, 1.15f);
                    SetScale(player, new Vector3(x, y, z));
                });
            }
        }
    }

    public class ScpMutationEvent:IEvent
    {
        public static ScpMutationEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Scp Mutation";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "SM";
        public bool OverrideWinConditions { get; }
        public bool BulletHolesAllowed { get; set; } = false;
        public PluginHandler PluginHandler { get; }
        public IEventConfig Config => EventConfig;

        [PluginConfig]
        public Config EventConfig;

        public void PrepareEvent()
        {
            Log.Info(EventName + " event is preparing");
            IsRunning = true;
            EventHandler.Start();
            Log.Info(EventName + " event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            EventHandler.Stop();
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Scp Mutation", "1.0.0", "Scps have random sizes", "The Riptide")]
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
