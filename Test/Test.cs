using Achievements;
using CommandSystem;
using Hints;
using MapGeneration;
using MEC;
using Mirror;
using Mirror.LiteNetLib4Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TheRiptide
{
    public class Config
    {
    }

    public class TeleportData
    {
        public RoomName n;
        public Vector3 p;
        public Vector3 r;
    }

    public class Test
    {
        [PluginConfig]
        public Config config;

        public static Dictionary<int, TeleportData> data = new Dictionary<int, TeleportData>
        {

        };


        [PluginEntryPoint("Test", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            EventManager.RegisterEvents(this);
        }

        [PluginEvent(PluginAPI.Enums.ServerEventType.WaitingForPlayers)]
        void OnWaitingForPlayers()
        {
            Log.Info("Network Server");
            foreach (var h in NetworkServer.handlers)
            {
                Log.Info($"{h.Key} {h.Value.Method.GetMethodBody().LocalVariables.First().ToString().Replace("(0)","")}");
            }
            Log.Info("Netowrk Client");
            foreach (var h in NetworkClient.handlers)
            {
                Log.Info($"{h.Key} {h.Value.Method.GetMethodBody().LocalVariables.First().ToString().Replace("(0)", "")}");
            }
        }


        [PluginEvent(PluginAPI.Enums.ServerEventType.PlayerDryfireWeapon)]
        public void OnDryfireWeapon(PlayerDryfireWeaponEvent e)
        {
            //e.Player.ReferenceHub.hints.Show(new TextHint("<voffset={0}em>my hint</voffset>", new HintParameter[] { new TimespanHintParameter(System.DateTimeOffset.Now.AddMinutes(5), "s\\.fff", false) }, durationScalar: 60 * 7));
            e.Player.ReferenceHub.hints.Show(new TextHint("my hint: {0}", new HintParameter[] { new IntHintParameter(0) }, durationScalar: 60));
            int i = 1;
            Timing.CallContinuously(60, () =>
            {
                //e.Player.ReferenceHub.hints.Show(new TextHint("", new HintParameter[] { new IntHintParameter(i) }, durationScalar: 60));
                IntHintParameter msg = new IntHintParameter(i);
                NetworkWriter writer = new NetworkWriter();
                writer.WriteUShort((ushort)typeof(SharedHintData).FullName.GetStableHashCode());
                //msg.Serialize(writer);
                Utils.Networking.HintParameterReaderWriter.WriteHintParameter(writer, msg);
                e.Player.Connection.Send(writer.ToArraySegment());
                i++;
            });
            Log.Info("dryfired " + e.Firearm.name);
        }

    }

    //[CommandHandler(typeof(RemoteAdminCommandHandler))]
    //public class gpr : ICommand
    //{
    //    public string Command { get; } = "gpr";

    //    public string[] Aliases { get; } = new string[] {};

    //    public string Description { get; } = "get pos and rot";

    //    public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
    //    {
    //        Player player = Player.Get(sender);
    //        Vector3 pos;
    //        Vector3 rot;
    //        pos = player.Room.transform.InverseTransformPoint(player.Position);
    //        rot = player.Room.transform.InverseTransformDirection(player.GameObject.transform.rotation * Vector3.forward);

    //        int id = Test.data.Count;
    //        Test.data.Add(id, new TeleportData { n = player.Room.Name, p = pos, r = rot });

    //        Log.Info(pos.ToPreciseString() + " | " + rot.ToPreciseString());
    //        response = "ID: " + id;
    //        return true;
    //    }
    //}

    //[CommandHandler(typeof(RemoteAdminCommandHandler))]
    //public class spr : ICommand
    //{
    //    public string Command { get; } = "spr";

    //    public string[] Aliases { get; } = new string[] { };

    //    public string Description { get; } = "set pos and rot";

    //    public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
    //    {
    //        Player player = Player.Get(sender);
    //        int id;
    //        if (!int.TryParse(arguments.At(0), out id))
    //        {
    //            response = "Failed: invalid id: " + arguments.At(0);
    //            return false;
    //        }

    //        TeleportData td = Test.data[id];
    //        RoomIdentifier target = RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == td.n);
    //        player.ReferenceHub.TryOverridePosition(target.transform.TransformPoint(td.p), Quaternion.LookRotation(target.transform.TransformDirection(td.r)).eulerAngles - player.Rotation);
    //        response = "Success";
    //        return true;
    //    }
    //}

    //[CommandHandler(typeof(RemoteAdminCommandHandler))]
    //public class vanish : ICommand
    //{
    //    public string Command { get; } = "vanish";

    //    public string[] Aliases { get; } = new string[] { };

    //    public string Description { get; } = "vanish";

    //    public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
    //    {
    //        Player player = Player.Get(sender);
    //        player.ReferenceHub.authManager.InstanceMode = CentralAuth.ClientInstanceMode.Host;
    //        ServerConsole.RefreshOnlinePlayers();
    //        response = "Success";
    //        return true;
    //    }
    //}

    //[CommandHandler(typeof(RemoteAdminCommandHandler))]
    //public class connected : ICommand
    //{
    //    public string Command { get; } = "connected";

    //    public string[] Aliases { get; } = new string[] { };

    //    public string Description { get; } = "conection count";

    //    public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
    //    {
    //        response = $"{LiteNetLib4MirrorCore.Host.ConnectedPeersCount} | {ServerConsole._playersAmount.ToString() + "/" + CustomNetworkManager.slots.ToString()} | + {ServerConsole._verificationPlayersList} | Player.Count: {Player.Count} Player.Connections: {Player.ConnectionsCount} Player.NonVerified: {Player.NonVerifiedCount}";
    //        return true;
    //    }
    //}
}

//Network Server
//40252 Mirror.ReadyMessage
//46228 Mirror.CommandMessage
//33151 Mirror.NetworkPingMessage
//10297 Mirror.NetworkPongMessage
//1041 Mirror.EntityStateMessage
//53182 Mirror.TimeSnapshotMessage
//13085 Mirror.AddPlayerMessage
//47329 InventorySystem.Items.Autosync.AutosyncMessage
//42021 InventorySystem.Disarming.DisarmMessage
//13752 Interactables.Interobjects.ElevatorManager+ElevatorSyncMsg
//30080 InventorySystem.Items.Firearms.BasicMessages.ShotMessage
//7109 InventorySystem.Items.Firearms.BasicMessages.RequestMessage
//8915 InventorySystem.Items.ToggleableLights.FlashlightNetworkHandler+FlashlightMessage
//3034 PlayerRoles.FirstPersonControl.NetworkMessages.FpcFromClientMessage
//48298 PlayerRoles.FirstPersonControl.NetworkMessages.FpcNoclipToggleMessage
//38347 InventorySystem.Items.Keycards.KeycardItem+UseMessage
//49325 PlayerRoles.Spectating.OverwatchVoiceChannelSelector+ChannelMuteFlagsMessage
//46513 InventorySystem.Items.Radio.ClientRadioCommandMessage
//43964 InventorySystem.Items.Firearms.Attachments.Components.ReflexSightSyncMessage
//34552 InventorySystem.Items.Usables.Scp330.SelectScp330Message
//41675 PlayerRoles.RoleAssign.ScpSpawnPreferences+SpawnPreferences
//13832 PlayerRoles.Spectating.SpectatorNetworking+SpectatedNetIdSyncMessage
//42619 PlayerRoles.Subroutines.SubroutineMessage
//30656 InventorySystem.Items.ThrowableProjectiles.ThrowableNetworkHandler+ThrowableItemRequestMessage
//46106 InventorySystem.Items.Usables.StatusMessage
//36767 PlayerRoles.Voice.VoiceChatReceivePrefs+GroupMuteFlagsMessage
//41876 VoiceChat.Networking.VoiceMessage
//62014 PlayerRoles.PlayableScps.Scp049.Zombies.ZombieConfirmationBox+ScpReviveBlockMessage
//56910 TeslaHitMsg
//52673 CentralAuth.AuthenticationResponse
//9934 EncryptedChannelManager+EncryptedMessageOutside
//Netowrk Client
//54308 Mirror.ObjectDestroyMessage
//3662 Mirror.ObjectHideMessage
//10297 Mirror.NetworkPongMessage
//16484 Mirror.SpawnMessage
//59786 Mirror.ObjectSpawnStartedMessage
//34417 Mirror.ObjectSpawnFinishedMessage
//1041 Mirror.EntityStateMessage
//53182 Mirror.TimeSnapshotMessage
//61436 Mirror.ChangeOwnerMessage
//33978 Mirror.RpcMessage
//40959 Mirror.NotReadyMessage
//30259 Mirror.SceneMessage
//8291 CustomPlayerEffects.AntiScp207+BreakMessage
//47329 InventorySystem.Items.Autosync.AutosyncMessage
//54484 InventorySystem.Items.Firearms.BasicMessages.DamageIndicatorMessage
//41976 InventorySystem.Disarming.DisarmedPlayersListMessage
//6042 InventorySystem.Items.Firearms.Modules.DisruptorHitreg+DisruptorHitMessage
//63023 PlayerRoles.PlayableScps.HumeShield.DynamicHumeShieldController+ShieldBreakMessage
//13752 Interactables.Interobjects.ElevatorManager+ElevatorSyncMsg
//41029 Utils.ExplosionUtils+GrenadeExplosionMessage
//13240 InventorySystem.Items.Firearms.GunAudioMessage
//20680 InventorySystem.Items.Firearms.BasicMessages.StatusMessage
//7109 InventorySystem.Items.Firearms.BasicMessages.RequestMessage
//8915 InventorySystem.Items.ToggleableLights.FlashlightNetworkHandler+FlashlightMessage
//11846 PlayerRoles.FirstPersonControl.NetworkMessages.FpcPositionMessage
//65091 PlayerRoles.FirstPersonControl.NetworkMessages.FpcOverrideMessage
//36741 PlayerRoles.FirstPersonControl.NetworkMessages.FpcFallDamageMessage
//32077 InventorySystem.Items.Firearms.BasicMessages.GunDecalMessage
//2236 InventorySystem.Items.Usables.Scp244.Hypothermia.HumeShieldSubEffect+HumeBlockMsg
//53054 InventorySystem.Items.Usables.Scp244.Hypothermia.Hypothermia+ForcedHypothermiaMessage
//26553 InventorySystem.Items.MicroHID.HidStatusMessage
//24595 VoiceChat.Playbacks.PersonalRadioPlayback+TransmitterPositionMessage
//38952 PlayerRoles.RoleSyncInfo
//6753 PlayerRoles.RoleSyncInfoPack
//41715 InventorySystem.Items.Firearms.Modules.ShotgunResyncMessage
//32437 InventorySystem.Items.Radio.RadioStatusMessage
//43964 InventorySystem.Items.Firearms.Attachments.Components.ReflexSightSyncMessage
//52828 PlayerRoles.PlayableScps.Scp106.Scp106PocketItemManager+WarningMessage
//33967 InventorySystem.Items.Usables.Scp1576.Scp1576SpectatorWarningHandler+SpectatorWarningMessage
//34552 InventorySystem.Items.Usables.Scp330.SelectScp330Message
//61177 InventorySystem.Items.Usables.Scp330.SyncScp330Message
//8787 ServerShutdown+ServerShutdownMessage
//35178 CommandSystem.Commands.RemoteAdmin.Stripdown.StripdownNetworking+StripdownResponse
//42619 PlayerRoles.Subroutines.SubroutineMessage
//36805 Subtitles.SubtitleMessage
//32410 PlayerStatsSystem.SyncedStatMessages+StatMessage
//53913 InventorySystem.Items.ThrowableProjectiles.ThrowableNetworkHandler+ThrowableItemAudioMessage
//20668 Respawning.NamingRules.UnitNameMessage
//46106 InventorySystem.Items.Usables.StatusMessage
//34606 InventorySystem.Items.Usables.ItemCooldownMessage
//20463 VoiceChat.VoiceChatMuteIndicator+SyncMuteMessage
//41876 VoiceChat.Networking.VoiceMessage
//9934 EncryptedChannelManager+EncryptedMessageOutside


