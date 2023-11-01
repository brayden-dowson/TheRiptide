using CedMod.Addons.Events;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide
{
    public class Config
    {

    }

    public class CedModConfig:Config, IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;
        public Config config { get; set; } = new Config();
    }

    public sealed class Translation
    {
        public string Name { get; set; } = "Zombie Defence";
        public string Description { get; set; } = "Everyone will spawn in doctors chamber with the elevators locked. All SCPs will become 939 and everyone else Class-D. The 939s are ensnared so they can only use their lunge ability to attack. A new dog spawns in every minute. Class-D get an adrenaline and painkillers. There is a 50% chance the lights will be out and be given flashlights. The last Class-D alive wins!\n\n";
    }

    public class EventHandler
    {
        public static void Start()
        {

        }

        public static void Stop()
        {

        }
    }

    public class ZombieDefenceEvent
    {
        public static ZombieDefenceEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Leap Frog";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return Translation == null ? "Translation not loaded" : Translation.Description; }
            set { if (Translation != null) Translation.Description = value; else Log.Error("Translation null when setting value"); }
        }
        public string EventPrefix { get; } = "LF";
        public bool OverrideWinConditions { get; }
        public bool BulletHolesAllowed { get; set; } = false;
        public PluginHandler PluginHandler { get; }
        public IEventConfig Config => EventConfig;

        [PluginConfig]
        public CedModConfig EventConfig;

        [PluginConfig("translation.yml")]
        public Translation Translation;

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

        [PluginEntryPoint("Leap Frog Event", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            Singleton = this;
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
            Handler = PluginHandler.Get(this);
        }

        [PluginUnload]
        public void OnDisabled()
        {
            StopEvent();
        }

        [PluginEvent(ServerEventType.WaitingForPlayers)]
        public void OnWaitingForPlayers()
        {
            PrepareEvent();
        }
    }
}
