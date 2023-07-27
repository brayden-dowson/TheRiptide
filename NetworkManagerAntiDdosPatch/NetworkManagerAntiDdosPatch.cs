using HarmonyLib;
using LiteNetLib;
using LiteNetLib.Utils;
using PluginAPI.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide
{
    [HarmonyPatch(typeof(NetManager))]
    public class NetworkManagerAntiDdosPatch
    {
        [HarmonyPatch(nameof(NetManager.DataReceived), MethodType.Normal)]
        public static bool Prefix(NetManager __instance, NetPacket packet, IPEndPoint remoteEndPoint)
        {
            if (__instance.EnableStatistics)
            {
                __instance.Statistics.IncrementPacketsReceived();
                __instance.Statistics.AddBytesReceived((long)packet.Size);
            }
            if (__instance._ntpRequests.Count > 0 && __instance._ntpRequests.TryGetValue(remoteEndPoint, out NtpRequest _))
            {
                if (packet.Size < 48)
                    return false;
                byte[] numArray = new byte[packet.Size];
                Buffer.BlockCopy((Array)packet.RawData, 0, (Array)numArray, 0, packet.Size);
                NtpPacket packet1 = NtpPacket.FromServerResponse(numArray, DateTime.UtcNow);
                try
                {
                    packet1.ValidateReply();
                }
                catch (InvalidOperationException ex)
                {
                    packet1 = (NtpPacket)null;
                }
                if (packet1 == null)
                    return false;
                __instance._ntpRequests.Remove(remoteEndPoint);
                if (__instance._ntpEventListener == null)
                    return false;
                __instance._ntpEventListener.OnNtpResponse(packet1);
            }
            else
            {
                if (__instance._extraPacketLayer != null)
                {
                    int offset = 0;
                    __instance._extraPacketLayer.ProcessInboundPacket(remoteEndPoint, ref packet.RawData, ref offset, ref packet.Size);
                    if (packet.Size == 0)
                        return false;
                }
                if (!packet.Verify())
                {
                    NetDebug.WriteError("[NM] Bad data from " + remoteEndPoint.ToString());
                    __instance.NetPacketPool.Recycle(packet);
                }
                else
                {
                    switch (packet.Property)
                    {
                        case PacketProperty.ConnectRequest:
                            if (NetConnectRequestPacket.GetProtocolId(packet) != 11)
                            {
                                __instance.SendRawAndRecycle(__instance.NetPacketPool.GetWithProperty(PacketProperty.InvalidProtocol), remoteEndPoint);
                                return false;
                            }
                            break;
                        case PacketProperty.UnconnectedMessage:
                            if (!__instance.UnconnectedMessagesEnabled)
                                return false;
                            __instance.CreateEvent(NetEvent.EType.ReceiveUnconnected, remoteEndPoint: remoteEndPoint, readerSource: packet);
                            return false;
                        case PacketProperty.Broadcast:
                            if (!__instance.BroadcastReceiveEnabled)
                                return false;
                            __instance.CreateEvent(NetEvent.EType.Broadcast, remoteEndPoint: remoteEndPoint, readerSource: packet);
                            return false;
                        case PacketProperty.NatMessage:
                            if (!__instance.NatPunchEnabled)
                                return false;
                            __instance.NatPunchModule.ProcessMessage(remoteEndPoint, packet);
                            return false;
                    }
                    __instance._peersLock.EnterReadLock();
                    NetPeer netPeer;
                    bool flag = __instance._peersDict.TryGetValue(remoteEndPoint, out netPeer);
                    __instance._peersLock.ExitReadLock();
                    switch (packet.Property)
                    {
                        case PacketProperty.ConnectRequest:
                            NetConnectRequestPacket connRequest = NetConnectRequestPacket.FromData(packet);
                            if (connRequest == null)
                                break;
                            __instance.ProcessConnectRequest(remoteEndPoint, netPeer, connRequest);
                            break;
                        case PacketProperty.ConnectAccept:
                            if (!flag)
                                break;
                            NetConnectAcceptPacket packet2 = NetConnectAcceptPacket.FromData(packet);
                            if (packet2 == null || !netPeer.ProcessConnectAccept(packet2))
                                break;
                            __instance.CreateEvent(NetEvent.EType.Connect, netPeer);
                            break;
                        case PacketProperty.Disconnect:
                            if (flag)
                            {
                                DisconnectResult disconnectResult = netPeer.ProcessDisconnect(packet);
                                if (disconnectResult == DisconnectResult.None)
                                {
                                    __instance.NetPacketPool.Recycle(packet);
                                    break;
                                }
                                __instance.DisconnectPeerForce(netPeer, disconnectResult == DisconnectResult.Disconnect ? DisconnectReason.RemoteConnectionClose : DisconnectReason.ConnectionRejected, SocketError.Success, packet);
                            }
                            else
                                __instance.NetPacketPool.Recycle(packet);
                            __instance.SendRawAndRecycle(__instance.NetPacketPool.GetWithProperty(PacketProperty.ShutdownOk), remoteEndPoint);
                            break;
                        case PacketProperty.PeerNotFound:
                            if (flag)
                            {
                                if (netPeer.ConnectionState != ConnectionState.Connected)
                                    break;
                                if (packet.Size == 1)
                                {
                                    NetPacket withProperty = __instance.NetPacketPool.GetWithProperty(PacketProperty.PeerNotFound, 9);
                                    withProperty.RawData[1] = (byte)0;
                                    FastBitConverter.GetBytes(withProperty.RawData, 2, netPeer.ConnectTime);
                                    __instance.SendRawAndRecycle(withProperty, remoteEndPoint);
                                    break;
                                }
                                if (packet.Size != 10 || packet.RawData[1] != (byte)1 || BitConverter.ToInt64(packet.RawData, 2) != netPeer.ConnectTime)
                                    break;
                                __instance.DisconnectPeerForce(netPeer, DisconnectReason.RemoteConnectionClose, SocketError.Success, (NetPacket)null);
                                break;
                            }
                            if (packet.Size != 10 || packet.RawData[1] != (byte)0)
                                break;
                            packet.RawData[1] = (byte)1;
                            __instance.SendRawAndRecycle(packet, remoteEndPoint);
                            break;
                        case PacketProperty.InvalidProtocol:
                            if (!flag || netPeer.ConnectionState != ConnectionState.Outgoing)
                                break;
                            __instance.DisconnectPeerForce(netPeer, DisconnectReason.InvalidProtocol, SocketError.Success, (NetPacket)null);
                            break;
                        default:
                            if (flag)
                            {
                                netPeer.ProcessPacket(packet);
                                break;
                            }
                            __instance.SendRawAndRecycle(__instance.NetPacketPool.GetWithProperty(PacketProperty.PeerNotFound), remoteEndPoint);
                            break;
                    }
                }
            }
            return false;
        }
    }

    public class Plugin
    {
        public static Plugin Singleton { get; private set; }
        public static Harmony Harmony { get; private set; }

        [PluginEntryPoint("WIP", "1.0.0", "Logs ips of bad data", "The Riptide")]
        public void OnEnabled()
        {
            Singleton = this;
            Harmony = new Harmony("NetworkManagerAntiDdosPatch");
            Harmony.PatchAll();
        }

        [PluginUnload]
        public void OnDisabled()
        {
            Harmony.UnpatchAll("NetworkManagerAntiDdosPatch");
            Harmony = null;
        }


    }
}
