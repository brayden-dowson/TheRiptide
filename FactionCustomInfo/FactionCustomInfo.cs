using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PluginAPI.Events;
using MEC;
using System.Reflection;
using Mirror;
using static TheRiptide.Utility;
using UnityEngine;
using PlayerRoles;
using PluginAPI.Core;
using PlayerStatsSystem;
using CustomPlayerEffects;

namespace TheRiptide
{
    public class InfoDesync
    {
        private int HP;
        private int AHP;
        private int HS;
        private bool GodMode;
        private bool Noclip;
        private class EffectInfo 
        {
            public bool Enabled;
            public string Name;
            public int Intensity;
            public int Duration;
            public StatusEffectBase.EffectClassification Classification;
        }
        private Dictionary<int, EffectInfo> EffectStatus = new Dictionary<int, EffectInfo>();
        private HashSet<int> Observing = new HashSet<int>();
        private NetworkWriter CustomWriter = new NetworkWriter();
        private static NetworkWriter DefaultWriter = new NetworkWriter();

        static InfoDesync()
        {
            SerializeInfo("", DefaultWriter);
        }

        public void AddObserver(Player target, Player observer)
        {
            if (!Observing.Contains(observer.PlayerId))
            {
                Observing.Add(observer.PlayerId);
                if (CustomWriter.Position != 0)
                    observer.Connection.Send(new EntityStateMessage { netId = target.ReferenceHub.nicknameSync.netId, payload = CustomWriter.ToArraySegment() });
                else
                    observer.Connection.Send(new EntityStateMessage { netId = target.ReferenceHub.nicknameSync.netId, payload = DefaultWriter.ToArraySegment() });
            }
        }

        public void RemoveObserver(Player target, Player observer)
        {
            if (Observing.Contains(observer.PlayerId))
            {
                Observing.Remove(observer.PlayerId);
                observer.Connection.Send(new EntityStateMessage { netId = target.ReferenceHub.nicknameSync.netId, payload = DefaultWriter.ToArraySegment() });
            }
        }

        public void Update(Player player)
        {
            if (Observing.IsEmpty())
                return;

            if (IsDirty(player))
            {
                SerializeInfo(BuildCustomInfo(), CustomWriter);

                foreach (var id in Observing.ToList())
                {
                    Player p = Player.Get(id);
                    if (p == null)
                        Observing.Remove(id);
                    else
                        p.Connection.Send(new EntityStateMessage { netId = player.ReferenceHub.nicknameSync.netId, payload = CustomWriter.ToArraySegment() });
                }
            }
        }

        public void Reset()
        {
            HP = 0;
            AHP = 0;
            HS = 0;
            GodMode = false;
            Noclip = false;
            foreach (var status in EffectStatus.Values)
            {
                status.Enabled = false;
                status.Intensity = 0;
                status.Duration = 0;
            }
            Observing.Clear();
            CustomWriter.Reset();
        }

        private bool IsDirty(Player player)
        {
            bool changed = false;
            if (HP != Mathf.RoundToInt(player.Health))
            {
                HP = Mathf.RoundToInt(player.Health);
                changed = true;
            }

            AhpStat ahp = null;
            if (player.ReferenceHub.playerStats.TryGetModule(out ahp))
            {
                if (ahp.CurValue != 0 && AHP != Mathf.RoundToInt(ahp.CurValue))
                {
                    AHP = Mathf.RoundToInt(ahp.CurValue);
                    changed = true;
                }
            }

            HumeShieldStat hume = null;
            if (player.ReferenceHub.playerStats.TryGetModule(out hume))
            {
                if (hume.CurValue != 0 && HS != Mathf.RoundToInt(hume.CurValue))
                {
                    HS = Mathf.RoundToInt(hume.CurValue);
                    changed = true;
                }
            }

            if(GodMode != player.IsGodModeEnabled)
            {
                GodMode = player.IsGodModeEnabled;
                changed = true;
            }
            if(Noclip != player.IsNoclipEnabled)
            {
                Noclip = player.IsNoclipEnabled;
                changed = true;
            }

            int id = 0;
            foreach (var seb in player.ReferenceHub.playerEffectsController.AllEffects)
            {
                if(!EffectStatus.ContainsKey(id))
                {
                    EffectStatus.Add(id, new EffectInfo { Enabled = seb.IsEnabled, Name = seb.ToString().Split(' ').First(), Intensity = seb.Intensity, Duration = Mathf.RoundToInt(seb.Duration), Classification = seb.Classification });
                    changed = true;
                }

                if(EffectStatus[id].Enabled !=seb.IsEnabled)
                {
                    EffectStatus[id].Enabled = seb.IsEnabled;
                    changed = true;
                }

                if (EffectStatus[id].Intensity != seb.Intensity)
                {
                    EffectStatus[id].Intensity = seb.Intensity;
                    changed = true;
                }

                if (EffectStatus[id].Duration != Mathf.RoundToInt(seb.Duration))
                {
                    EffectStatus[id].Duration = Mathf.RoundToInt(seb.Duration);
                    changed = true;
                }
                id++;
            }
            return changed;
        }

        private string BuildCustomInfo()
        {
            string info = " <voffset=-4em>Health " + HP + "\n";
            if (AHP != 0)
                info += "Artificial Health " + AHP + "\n";

            if (HS != 0)
                info += "Hume Shield " + HS + "\n";

            if (GodMode)
                info += "[God Mode] ";
            if (Noclip)
                info += "[Noclip]";
            if (GodMode || Noclip)
                info += "\n";

            foreach (var status in EffectStatus.Values)
                if (status.Enabled)
                    info += status.Intensity + "x " + status.Name + (status.Duration == 0 ? "" : " " + status.Duration) + "\n";

            info += "</voffset>";

            return info;
        }

        //private string BuildCustomInfo()
        //{
        //    string info = "<color=#00FF00>Health " + HP + "</color>\n";
        //    if (AHP != 0)
        //        info += "<color=#FFFF00>Artificial Health " + AHP + "</color>\n";

        //    if (HS != 0)
        //        info += "<color=#FFFF00>Hume Shield " + HS + "</color>\n";

        //    if (GodMode || Noclip)
        //        info += "<color=#000000>";
        //    if (GodMode)
        //        info += "[God Mode] ";
        //    if (Noclip)
        //        info += "[Noclip]";
        //    if (GodMode || Noclip)
        //        info += "</color>\n";

        //    foreach (var status in EffectStatus.Values)
        //    {
        //        if (status.Enabled)
        //        {
        //            switch (status.Classification)
        //            {
        //                case StatusEffectBase.EffectClassification.Negative:
        //                    info += "<color=#FF0000>";
        //                    break;
        //                case StatusEffectBase.EffectClassification.Mixed:
        //                    info += "<color=#FF80FF>";
        //                    break;
        //                case StatusEffectBase.EffectClassification.Positive:
        //                    info += "<color=#FF0000>";
        //                    break;
        //            }
        //            info += status.Intensity + "x " + status.Name + (status.Duration == 0 ? "" : " " + status.Duration) + "</color>\n";
        //        }

        //    }

        //    return info;
        //}

        private static void SerializeInfo(string info, NetworkWriter writer)
        {
            //Send fake CustomInfo for NinkNameSync for Reference Hub
            writer.Reset();
            Compression.CompressVarUInt(writer, 1 << 3);//network behaviour dirty bits(NickNameSync has an index of 3 inside Reference Hub) must be compressed
            int position1 = writer.Position;
            writer.WriteByte(0);//placeholder for size
            int position2 = writer.Position;

            writer.WriteULong(0L);//sync obj dirty bits
            //sync object data

            writer.WriteULong(2L);//sync var dirty bits
            writer.WriteString(info);//sync var data

            //calculate size and save it in the placeholder
            int position3 = writer.Position;
            writer.Position = position1;
            byte num = (byte)(position3 - position2 & byte.MaxValue);
            writer.WriteByte(num);
            writer.Position = position3;
        }
    }

    public class FactionCustomInfo
    {
        private static bool normal_round = true;
        private static CoroutineHandle update;

        private static Dictionary<int, InfoDesync> player_desync = new Dictionary<int, InfoDesync>();

        [PluginEntryPoint("Faction Custom Info", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            EventManager.RegisterEvents(this);
        }

        [PluginUnload]
        public void OnDisable()
        {
            EventManager.UnregisterEvents(this);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            normal_round = true;
            Type event_manager_type = Utility.GetType("CedMod.Addons.Events.EventManager");
            if (event_manager_type != null && event_manager_type.GetField("CurrentEvent", BindingFlags.Public | BindingFlags.Static).GetValue(null) != null)
                normal_round = false;
            if (normal_round)
                update = Timing.RunCoroutine(_Update());
        }

        [PluginEvent(ServerEventType.RoundEnd)]
        void OnRoundEnd(RoundEndEvent ev)
        {
            if (normal_round)
                Timing.KillCoroutines(update);
        }

        [PluginEvent(ServerEventType.RoundRestart)]
        void OnRoundRestart(RoundRestartEvent ev)
        {
            if (normal_round)
                Timing.KillCoroutines(update);
        }

        //[PluginEvent(ServerEventType.PlayerJoined)]
        //void OnPlayerJoined(PlayerJoinedEvent ev)
        //{
        //    if (!normal_round || ev.Player == null)
        //        return;

        //    if (!info_cache.ContainsKey(ev.Player.PlayerId))
        //        info_cache.Add(ev.Player.PlayerId, "");
        //    else
        //        info_cache[ev.Player.PlayerId] = "";
        //}

        //[PluginEvent(ServerEventType.PlayerLeft)]
        //void OnPlayerLeft(PlayerJoinedEvent ev)
        //{
        //    if (ev.Player == null)
        //        return;
        //    if (info_cache.ContainsKey(ev.Player.PlayerId))
        //        info_cache.Remove(ev.Player.PlayerId);
        //}

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        void OnRoleChange(PlayerChangeRoleEvent ev)
        {
            if (!normal_round || ev.Player == null)
                return;

            Timing.CallDelayed(0.0f, () =>
            {
                if (ev.Player.Role != ev.NewRole)
                    return;

                if (!player_desync.ContainsKey(ev.Player.PlayerId))
                    player_desync.Add(ev.Player.PlayerId, new InfoDesync());
                player_desync[ev.Player.PlayerId].Reset();
                foreach (var p in ReadyPlayers())
                    player_desync[p.PlayerId].RemoveObserver(p, ev.Player);

                Faction faction = ev.Player.Role.GetFaction();
                if (faction == Faction.Unclassified)
                    return;

                foreach (var p in ReadyPlayers())
                {
                    if (p.Role.GetFaction() == faction && p != ev.Player)
                    {
                        player_desync[ev.Player.PlayerId].AddObserver(ev.Player, p);
                        player_desync[p.PlayerId].AddObserver(p, ev.Player);
                    }
                }



                //ev.Player.ReferenceHub.nicknameSync.syncVarDirtyBits |= 2L;
            });
        }

        //private void UpdateCustomInfo(Player player)
        //{
        //    Faction faction = player.Role.GetFaction();
        //    if (faction == Faction.Unclassified)
        //    {
        //        player.ReferenceHub.nicknameSync.syncVarDirtyBits |= 2L;
        //        return;
        //    }

        //    string info = "<color=#00FF00>Health " + player.Health.ToString("0") + "</color>\n";

        //    AhpStat ahp = null;
        //    if (player.ReferenceHub.playerStats.TryGetModule(out ahp))
        //        if (ahp.CurValue != 0)
        //            info += "<color=#FFFF00>Artificial Health " + ahp.CurValue.ToString("0") + "</color>\n";

        //    HumeShieldStat hume = null;
        //    if (player.ReferenceHub.playerStats.TryGetModule(out hume))
        //        if (hume.CurValue != 0)
        //            info += "<color=#FFFF00>Hume Shield " + hume.CurValue.ToString("0") + "</color>\n";

        //    if (player.IsGodModeEnabled || player.IsNoclipEnabled)
        //        info += "<color=#000000>";
        //    if (player.IsGodModeEnabled)
        //        info += "[God Mode] ";
        //    if (player.IsNoclipEnabled)
        //        info += "[Noclip]";
        //    if (player.IsGodModeEnabled || player.IsNoclipEnabled)
        //        info += "</color>\n";

        //    foreach (var seb in player.ReferenceHub.playerEffectsController.AllEffects)
        //    {
        //        if (!seb.IsEnabled)
        //            continue;

        //        switch(seb.Classification)
        //        {
        //            case StatusEffectBase.EffectClassification.Negative:
        //                info += "<color=#FF0000>";
        //                break;
        //            case StatusEffectBase.EffectClassification.Mixed:
        //                info += "<color=#FF80FF>";
        //                break;
        //            case StatusEffectBase.EffectClassification.Positive:
        //                info += "<color=#FF0000>";
        //                break;
        //        }

        //        info += seb.Intensity + "x " + seb.ToString().Split(' ').First() + (seb.TimeLeft == 0 ? "" : " " + seb.TimeLeft.ToString("0")) + "</color>\n";
        //    }

        //    if (!info_cache.ContainsKey(player.PlayerId))
        //        info_cache.Add(player.PlayerId, "");
        //    if(info != info_cache[player.PlayerId])
        //    {
        //        info_cache[player.PlayerId] = info;

        //        using (NetworkWriterPooled writer = NetworkWriterPool.Get())
        //        {
        //            Compression.CompressVarUInt(writer, 1 << 3);
        //            int position1 = writer.Position;
        //            writer.WriteByte(0);
        //            int position2 = writer.Position;

        //            writer.WriteULong(0L);
        //            writer.WriteULong(2L);
        //            writer.WriteString(info);

        //            int position3 = writer.Position;
        //            writer.Position = position1;
        //            byte num = (byte)(position3 - position2 & byte.MaxValue);
        //            writer.WriteByte(num);
        //            writer.Position = position3;

        //            foreach (var p in ReadyPlayers())
        //            {
        //                if (p == player || p.Role.GetFaction() == Faction.Unclassified || p.Role.GetFaction() != player.Role.GetFaction())
        //                    continue;
        //                p.Connection.Send(new EntityStateMessage { netId = player.ReferenceHub.nicknameSync.netId, payload = writer.ToArraySegment() });
        //            }
        //        }
        //    }
        //}

        private IEnumerator<float> _Update()
        {
            while (true)
            {
                try
                {
                    foreach (var p in ReadyPlayers())
                    {
                        if (p.Role.GetFaction() != Faction.Unclassified)
                        {
                            if (!player_desync.ContainsKey(p.PlayerId))
                                player_desync.Add(p.PlayerId, new InfoDesync());
                            player_desync[p.PlayerId].Update(p);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }
    }
}
