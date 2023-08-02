using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CedMod.Addons.Events;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using MEC;
using MapGeneration;
using System.ComponentModel;
using PluginAPI.Core.Interfaces;
using CedMod;
using PlayerRoles;
using CedMod.Addons.Events.Interfaces;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        [Description("min time between random cassie noises")]
        public int cassie_min_time { get; set; } = 0;

        [Description("max time between random cassie noises")]
        public int cassie_max_time { get; set; } = 150;

        [Description("how many rooms in the facility every second will have their lights flicker on")]
        public string room_light_flickers_per_second { get; set; } = "2.0";

        public string Description { get; set; } = "All facility lights disabled. All SCPs will be SCP939. Everyone will spawn with a Flashlight\n\n";
    }

    public class DogsInTheDarkEvent : IEvent
    {
        public static DogsInTheDarkEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Dogs In The Dark";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "DITD";
        public bool OverrideWinConditions { get; }
        public bool BulletHolesAllowed { get; set; } = false;
        public PluginHandler PluginHandler { get; }
        public IEventConfig Config => EventConfig;

        public void PrepareEvent()
        {
            Log.Info("DogsInTheDarkEvent is preparing");
            IsRunning = true;
            Log.Info("DogsInTheDarkEvent is prepared");
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            Stop();
            PluginAPI.Events.EventManager.UnregisterEvents(this);
        }

        [PluginEntryPoint("Dogs In The Dark Event", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            //PluginAPI.Events.EventManager.RegisterEvents(this);

            Singleton = this;
            Handler = PluginHandler.Get(this);
        }

        [PluginUnload]
        public void OnDisabled()
        {
            StopEvent();
        }

        [PluginConfig]
        public Config EventConfig;

        public static CoroutineHandle light_flickerer = new CoroutineHandle();
        public static CoroutineHandle light_flicker_off = new CoroutineHandle();
        public static CoroutineHandle cassie = new CoroutineHandle();

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + Singleton.EventName + "\n<size=32>" + Singleton.EventDescription + "</size>", 30, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted || new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Overwatch)
                return true;

            if(new_role.GetTeam() == Team.SCPs && new_role != RoleTypeId.Scp939)
            {
                Timing.CallDelayed(0.0f,()=>
                {
                    if (player.Role.GetTeam() != Team.SCPs || player.Role == RoleTypeId.Scp939)
                        return;
                    player.SetRole(RoleTypeId.Scp939);
                });
                return false;
            }
            return true;
        }


        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            Timing.CallDelayed(0.0f, () =>
            {
                if (player.IsHuman)
                {
                    player.AddItem(ItemType.Flashlight);
                    player.SendBroadcast("Flashlight granted, check inv.", 20, shouldClearPrevious: true);
                }
            });
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            Random random = new Random();
            light_flicker_off = Timing.RunCoroutine(_LightsOffFlicker());
            Cassie.Message("pitch_0.1 .G7 s .", true, true, false);
            Timing.CallDelayed(random.Next(EventConfig.cassie_min_time, EventConfig.cassie_max_time), () =>
            {
                cassie = Timing.RunCoroutine(_RandomCassie());
            });
        }

        [PluginEvent(ServerEventType.RoundRestart)]
        void OnRoundRestart()
        {
            Stop();
        }

        public IEnumerator<float> _RandomCassie()
        {
            Random random = new Random();
            List<string> special_codes = new List<string> { " .G1 .", " .G2 .", " .G3 .", " .G4 .", " .G5 .", " .G6 ." };
            while (true)
            {
                string message = "pitch_";
                bool send_noisy = random.NextDouble() > 0.9f;
                if (random.NextDouble() > 0.5)
                {
                    message += (random.NextDouble() + 0.05).ToString("0.00");
                    message += special_codes.RandomItem();
                }
                else
                {
                    message += (random.NextDouble() / 4.0f + 0.03).ToString("0.00");
                    message += " ";
                    message += (char)('a' + random.Next(26));
                    message += " .";
                }

                Cassie.Message(message, false, send_noisy, false);
                yield return Timing.WaitForSeconds(random.Next(EventConfig.cassie_min_time, EventConfig.cassie_max_time));
            }
        }

        private double sig(double x, double b, double c)
        {
            x = Math.Min(x, 1.0f);
            double x_b = Math.Pow(x, b);
            return (1.0 - c) * (x_b / (x_b + Math.Pow(1.0 - x, b))) + (c / 2.0);

            //x ^ b / (x ^ b + (1 - x) ^ b)
        }

        public IEnumerator<float> _LightsOffFlicker()
        {
            Log.Info("flickering lights off");
            float max_decay = 0.002f;

            Dictionary<RoomIdentifier, float> room_flicker_toggle_chance = new Dictionary<RoomIdentifier, float>();
            foreach (var room in RoomIdentifier.AllRoomIdentifiers)
                room_flicker_toggle_chance.Add(room, 0.0f);

            Random random = new Random();
            bool all_off = false;
            while (!all_off)
            {
                try
                {
                    foreach (var rf in room_flicker_toggle_chance)
                    {
                        bool toggle = random.NextDouble() > (float)sig(rf.Value, 9.0f, 0.005f);
                        FacilityManager.SetRoomLightState(rf.Key, toggle);
                    }

                    foreach (var key in room_flicker_toggle_chance.Keys.ToList())
                        room_flicker_toggle_chance[key] += (float)random.NextDouble() * max_decay;

                    foreach (var i in room_flicker_toggle_chance.Where((rf) => rf.Value >= 1.0f).ToList())
                    {
                        room_flicker_toggle_chance.Remove(i.Key);
                        FacilityManager.SetRoomLightState(i.Key, false);
                    }

                    if (room_flicker_toggle_chance.Count == 0)
                        all_off = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                yield return Timing.WaitForOneFrame;
            }
            light_flickerer = Timing.RunCoroutine(_LightFlicker());
        }

        public IEnumerator<float> _LightFlicker()
        {
            Log.Info("flickering lights");
            Random random = new Random();

            while (true)
            {
                RoomIdentifier room = RoomIdentifier.AllRoomIdentifiers.ElementAt(random.Next(RoomIdentifier.AllRoomIdentifiers.Count));
                FacilityManager.SetRoomLightState(room, true);
                Timing.CallDelayed((float)random.NextDouble(), () =>
                {
                    FacilityManager.SetRoomLightState(room, false);
                });

                yield return Timing.WaitForSeconds(1.0f / float.Parse(EventConfig.room_light_flickers_per_second));
            }
        }

        public static void Stop()
        {
            Timing.KillCoroutines(light_flicker_off);
            Timing.KillCoroutines(light_flickerer);
            Timing.KillCoroutines(cassie);
        }
    }
}
