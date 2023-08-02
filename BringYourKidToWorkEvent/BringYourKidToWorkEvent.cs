using CedMod.Addons.Events;
using Mirror;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MEC;
using HarmonyLib;
using CedMod.Addons.Events.Interfaces;
using static TheRiptide.Utility;
using VoiceChat.Networking;
using VoiceChat.Codec;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        public float ChildSize { get; set; } = 0.65f;
        public string Description { get; set; } = "Half the players will be small. Small players have 75% max health but get a speed boost equivalent to a cola without the negative effect\n\n";
    }


    public class EventHandler
    {
        static HashSet<RoleTypeId> human_roles = new HashSet<RoleTypeId>
        {
            RoleTypeId.ClassD,
            RoleTypeId.Scientist,
            RoleTypeId.FacilityGuard,
            RoleTypeId.NtfPrivate,
            RoleTypeId.NtfSergeant,
            RoleTypeId.NtfSpecialist,
            RoleTypeId.NtfCaptain,
            RoleTypeId.ChaosConscript,
            RoleTypeId.ChaosMarauder,
            RoleTypeId.ChaosRepressor,
            RoleTypeId.ChaosRifleman,
        };

        public static Dictionary<int, bool> IsPlayerChild = new Dictionary<int, bool>();
        static CoroutineHandle cola_handle;

        private static OpusDecoder decoder = null;
        private static OpusEncoder encoder = null;
        private static float[] samples = null;
        private static SMBPitchShifter shifter = new SMBPitchShifter();

        public static void Start()
        {
            cola_handle = Timing.RunCoroutine(_ApplyColaEffect());

            if (decoder == null)
                decoder = new OpusDecoder();
            if (encoder == null)
                encoder = new OpusEncoder(VoiceChat.Codec.Enums.OpusApplicationType.Voip);
            if (samples == null)
                samples = new float[VoiceTransceiver._packageSize];
        }

        public static void Stop()
        {
            Timing.KillCoroutines(cola_handle);
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + BringYourKidToWorkEvent.Singleton.EventName + "\n<size=30>" + BringYourKidToWorkEvent.Singleton.EventDescription.Replace("\n", "") + "</size>", 30, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            if (player != null)
            {
                if (!IsPlayerChild.ContainsKey(player.PlayerId))
                    IsPlayerChild.Add(player.PlayerId, false);

                if (human_roles.Contains(role) && UnityEngine.Random.value > 0.5)
                {
                    IsPlayerChild[player.PlayerId] = true;
                    SetScale(player, BringYourKidToWorkEvent.Singleton.EventConfig.ChildSize);
                }
                else if (role != RoleTypeId.None && role != RoleTypeId.Spectator && role != RoleTypeId.Overwatch)
                {
                    IsPlayerChild[player.PlayerId] = false;
                    if (player.GameObject.transform.localScale != new UnityEngine.Vector3(1.0f, 1.0f, 1.0f))
                        SetScale(player, 1.0f);
                }
            }
        }

        //private void SetScale(Player player, float scale)
        //{
        //    try
        //    {
        //        NetworkIdentity identity = player.GameObject.GetComponent<NetworkIdentity>();
        //        Random random = new Random();
        //        player.GameObject.transform.localScale = new UnityEngine.Vector3(scale, scale, scale);

        //        ObjectDestroyMessage destroy_message = new ObjectDestroyMessage();
        //        destroy_message.netId = identity.netId;

        //        foreach (var p in Player.GetPlayers())
        //        {
        //            NetworkConnection player_connection = p.GameObject.GetComponent<NetworkIdentity>().connectionToClient;
        //            if (p.GameObject != player.GameObject)
        //                player_connection.Send(destroy_message, 0);
        //            NetworkServer.SendSpawnMessage(identity, player_connection);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.ToString());
        //    }
        //}

        //public static void ProcessVoice(VoiceMessage msg)
        //{
        //    int id = msg.Speaker.PlayerId;
        //    if (IsHumanRole(msg.Speaker.GetRoleId()) && IsPlayerChild.ContainsKey(id) && IsPlayerChild[id])
        //    {
        //        if (decoder == null || encoder == null)
        //            return;

        //        int sample_count = decoder.Decode(msg.Data, msg.DataLength, samples);
        //        shifter.PitchShift(2.0f, sample_count, 48000, samples);
        //        msg.DataLength = encoder.Encode(samples, msg.Data);
        //    }
        //}

        public static IEnumerator<float> _ApplyColaEffect()
        {
            while(BringYourKidToWorkEvent.IsRunning)
            {
                try
                {
                    foreach (var player in Player.GetPlayers())
                    {
                        if (player.IsAlive && player.IsHuman && IsPlayerChild.ContainsKey(player.PlayerId) && IsPlayerChild[player.PlayerId])
                        {
                            player.EffectsManager.ChangeState<CustomPlayerEffects.MovementBoost>(20, 0);
                            player.EffectsManager.ChangeState<CustomPlayerEffects.Invigorated>(1, 0);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Log.Error("_ApplyColaEffect Error: " + ex.ToString());
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }
    }

    public class BringYourKidToWorkEvent : IEvent
    {
        public static BringYourKidToWorkEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Bring Your Kid To Work Event";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "BYKTW";
        public bool OverrideWinConditions { get; }
        public bool BulletHolesAllowed { get; set; } = false;
        public PluginHandler PluginHandler { get; }
        public IEventConfig Config => EventConfig;

        [PluginConfig]
        public Config EventConfig;

        //private Harmony harmony;

        public void PrepareEvent()
        {
            Log.Info("BringYourKidToWorkEvent is preparing");
            IsRunning = true;
            EventHandler.Start();
            //harmony = new Harmony("BringYourKidToWorkEvent");
            //harmony.PatchAll();
            Log.Info("BringYourKidToWorkEvent is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            EventHandler.Stop();
            //harmony.UnpatchAll("BringYourKidToWorkEvent");
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Bring Your Kid To Work Event", "1.0.0", "half the players will be small. small players have less max health but get a speed boost", "The Riptide")]
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
