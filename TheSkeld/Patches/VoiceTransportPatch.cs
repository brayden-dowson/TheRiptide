using HarmonyLib;
using Mirror;
using PlayerRoles.Voice;
using PlayerStatsSystem;
using PluginAPI.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoiceChat;
using VoiceChat.Networking;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))]
    public class VoiceTransceiverPatch
    {
        public static bool ScrambleRadio = false;

        static bool Prefix(NetworkConnection conn, VoiceMessage msg)
        {
            if (msg.SpeakerNull || (int)msg.Speaker.netId != (int)conn.identity.netId || !(msg.Speaker.roleManager.CurrentRole is IVoiceRole currentRole1) || !currentRole1.VoiceModule.CheckRateLimit() || VoiceChatMutes.IsMuted(msg.Speaker))
                return false;

            VoiceChatChannel channel = currentRole1.VoiceModule.ValidateSend(msg.Channel);
            if (channel == VoiceChatChannel.None)
                return false;

            if(ScrambleRadio && channel == VoiceChatChannel.Radio && CommunicationController.scrambled_data.Count != 0)
            {
                if (!CommunicationController.player_index.ContainsKey(msg.Speaker.PlayerId))
                    CommunicationController.player_index.Add(msg.Speaker.PlayerId, 0);
                int index = CommunicationController.player_index[msg.Speaker.PlayerId];
                CommunicationController.player_index[msg.Speaker.PlayerId] = (CommunicationController.player_index[msg.Speaker.PlayerId] + 1) % CommunicationController.scrambled_data.Count;
                msg.Data = CommunicationController.scrambled_data[index];
                msg.DataLength = CommunicationController.scrambled_data_size[index];
                //msg.Channel = VoiceChatChannel.RoundSummary;
                //msg.Speaker.connectionToClient.Send(msg);
            }

            currentRole1.VoiceModule.CurrentChannel = channel;
            foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
            {
                if (allHub.roleManager.CurrentRole is IVoiceRole currentRole2)
                {
                    VoiceChatChannel voiceChatChannel = currentRole2.VoiceModule.ValidateReceive(msg.Speaker, channel);
                    if (voiceChatChannel != VoiceChatChannel.None)
                    {
                        msg.Channel = voiceChatChannel;
                        allHub.connectionToClient.Send<VoiceMessage>(msg);
                    }
                }
            }
            return false;
        }
    }
}
