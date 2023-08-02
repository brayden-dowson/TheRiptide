using HarmonyLib;
using Mirror;
using PlayerRoles.Voice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceChat;
using VoiceChat.Networking;

namespace TheRiptide.Patches
{
    //[HarmonyPatch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))]
    //public class VoiceTransceiverPatch
    //{
    //    static bool Prefix(NetworkConnection conn, VoiceMessage msg)
    //    {
    //        if (msg.SpeakerNull || (int)msg.Speaker.netId != (int)conn.identity.netId || !(msg.Speaker.roleManager.CurrentRole is IVoiceRole currentRole1) || !currentRole1.VoiceModule.CheckRateLimit() || VoiceChatMutes.IsMuted(msg.Speaker))
    //            return false;

    //        EventHandler.ProcessVoice(msg);

    //        VoiceChatChannel channel = currentRole1.VoiceModule.ValidateSend(msg.Channel);
    //        if (channel == VoiceChatChannel.None)
    //            return false;
    //        currentRole1.VoiceModule.CurrentChannel = channel;
    //        foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
    //        {
    //            if (allHub.roleManager.CurrentRole is IVoiceRole currentRole2)
    //            {
    //                VoiceChatChannel voiceChatChannel = currentRole2.VoiceModule.ValidateReceive(msg.Speaker, channel);
    //                if (voiceChatChannel != VoiceChatChannel.None)
    //                {
    //                    msg.Channel = voiceChatChannel;
    //                    allHub.connectionToClient.Send<VoiceMessage>(msg);
    //                }
    //            }
    //        }
    //        return false;
    //    }
    //}
}
