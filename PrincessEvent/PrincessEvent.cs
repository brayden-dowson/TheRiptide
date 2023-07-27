using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using HarmonyLib;
using MEC;
using Mirror;
using PlayerRoles;
using PlayerRoles.Voice;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceChat;
using VoiceChat.Codec;
using VoiceChat.Networking;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;
    }

    public class EventHandler
    {
        private static HashSet<int> dogs = new HashSet<int>();

        private static OpusDecoder decoder = null;
        private static OpusEncoder encoder = null;
        //private static float[] samples = null;
        //private static SMBPitchShifter shifter = new SMBPitchShifter();

        private static RubberBand.RubberBandStretcher stretcher;
        private static float[][] samples = new float[1][];

        public static void Start()
        {
            if (decoder == null)
                decoder = new OpusDecoder();
            if (encoder == null)
                encoder = new OpusEncoder(VoiceChat.Codec.Enums.OpusApplicationType.Voip);
            //if (samples == null)
            //    samples = new float[VoiceTransceiver._packageSize];

            RubberBand.RubberBandStretcher.Options options =
                RubberBand.RubberBandStretcher.Options.ProcessRealTime |
                RubberBand.RubberBandStretcher.Options.FormantPreserved |
                RubberBand.RubberBandStretcher.Options.PitchHighQuality |
                RubberBand.RubberBandStretcher.Options.WindowShort;

            stretcher = new RubberBand.RubberBandStretcher(48000, 1, options);
            stretcher.SetPitchScale(2.0f);
        }

        public static void Stop()
        {

        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + PrincessBanquetEvent.Singleton.EventName + "\n<size=24>" + PrincessBanquetEvent.Singleton.EventDescription + "</size>", 30, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            //List<Player> players = Player.GetPlayers().Where(p => p.Role != RoleTypeId.None).ToList();

            //int dog_count = 2;
            //for (int i = 0; i < dog_count; i++)
            //    dogs.Add(players.PullRandomItem().PlayerId);
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted ||
                new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Overwatch)
                return true;

            if(dogs.Contains(player.PlayerId))
            {
                if(new_role != RoleTypeId.Scp939)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.Scp939);
                    });
                    return false;
                }
            }
            else if (player.PlayerId % 2 == 0)
            {
                if (new_role != RoleTypeId.ClassD)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.ClassD);
                    });
                    return false;
                }
            }
            else
            {
                if (new_role != RoleTypeId.Scientist)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.Scientist);
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

            if(role == RoleTypeId.Scp939)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role != RoleTypeId.Scp939)
                        return;

                    
                });
            }
            else if(role == RoleTypeId.ClassD)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role != RoleTypeId.ClassD)
                        return;
                    SetScale(player, 0.6f);
                });
            }
            else if(role == RoleTypeId.Scientist)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role != RoleTypeId.Scientist)
                        return;
                    SetScale(player, 0.6f);
                });
            }
        }

        public static void ProcessVoice(VoiceMessage msg)
        {
            if (msg.Speaker.GetRoleId() == RoleTypeId.Scientist || msg.Speaker.GetRoleId() == RoleTypeId.ClassD)
            {
                if (decoder == null || encoder == null)
                    return;

                int sample_count = decoder.Decode(msg.Data, msg.DataLength, samples[0]);
                stretcher.Process(samples, false);
                //shifter.PitchShift(2.0f, sample_count, 48000, samples);
                msg.DataLength = encoder.Encode(samples[0], msg.Data);
                msg.Channel = VoiceChatChannel.RoundSummary;
                msg.Speaker.connectionToClient.Send(msg);
            }
        }
    }

    public class PrincessBanquetEvent : IEvent
    {
        public static PrincessBanquetEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Princess Banquet";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription { get; set; } = "todo\n\n";
        public string EventPrefix { get; } = "PB";
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
            EventHandler.Start();
            harmony = new Harmony("PrincessBanquetEvent");
            harmony.PatchAll();
            Log.Info(EventName + " event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            EventHandler.Stop();
            harmony.UnpatchAll("PrincessBanquetEvent");
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Princess Banquet Event", "1.0.0", "", "The Riptide")]
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
