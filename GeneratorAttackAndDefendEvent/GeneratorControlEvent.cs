using CedMod.Addons.Events;
using MapGeneration.Distributors;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using Respawning;
using PlayerRoles;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapGeneration;
using MEC;
using UnityEngine;
using Interactables.Interobjects.DoorUtils;
using RoundRestarting;
using CustomPlayerEffects;
using HarmonyLib;
using Mirror;
using CedMod.Addons.Events.Interfaces;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        public int WaveCount { get; set; } = 5;
        public float WavePeriod { get; set; } = 2.0f;

        public string Description { get; set; } = "CHAOS, NTF and SCPs must each turn on the genrators around the facility and defend them from opposing factions. The Room will glow the color of which ever faction holds the current generator. CHAOS teaming with the SCPs is not allowed. All zones except heavy are locked down. NTF and CHAOS get to respawn. After all respawns the team with the most generators on wins!\n\n";
    }

    public class EventHandler
    {
        class TeamInfo
        {
            public Team holding;
            public Team capturing;
        }

        private static Config config;

        private static Dictionary<Scp079Generator, TeamInfo> generator_team = new Dictionary<Scp079Generator, TeamInfo>();
        private static CoroutineHandle update;
        private static CoroutineHandle two_min;
        private static CoroutineHandle one_min;
        private static CoroutineHandle end;

        private static HashSet<int> chaos = new HashSet<int>();
        private static HashSet<int> mtf = new HashSet<int>();
        private static List<RoleTypeId> chaos_roles = new List<RoleTypeId>();
        private static List<RoleTypeId> mtf_roles = new List<RoleTypeId>();

        private static RoomIdentifier team_a_room;
        private static RoomIdentifier team_b_room;

        public static void Start(Config config)
        {
            EventHandler.config = config;

            generator_team.Clear();

            chaos.Clear();
            mtf.Clear();
            chaos_roles.Clear();
            mtf_roles.Clear();
        }

        public static void Stop()
        {
            generator_team.Clear();
            team_a_room = null;
            team_b_room = null;
            chaos.Clear();
            mtf.Clear();
            chaos_roles.Clear();
            mtf_roles.Clear();
            Timing.KillCoroutines(update, two_min, one_min, end);
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + GeneratorControlEvent.Singleton.EventName + "\n<size=24>" + GeneratorControlEvent.Singleton.EventDescription + "</size>", 30, shouldClearPrevious: true);

            if (chaos.Count < mtf.Count)
                chaos.Add(player.PlayerId);
            else
                mtf.Add(player.PlayerId);
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            if (chaos.Contains(player.PlayerId))
                chaos.Remove(player.PlayerId);
            if (mtf.Contains(player.PlayerId))
                mtf.Remove(player.PlayerId);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            Round.IsLocked = true;
            Timing.CallDelayed(1.0f, () =>
            {
                try
                {
                    foreach (var room in RoomIdentifier.AllRoomIdentifiers)
                    {
                        Scp079Generator[] generators = room.GetComponentsInChildren<Scp079Generator>();
                        foreach(var generator in generators)
                        {
                            generator.ServerSetFlag(Scp079Generator.GeneratorFlags.Unlocked, true);
                            generator._totalActivationTime = 60.0f;
                            generator._totalDeactivationTime = 20.0f;
                            generator_team.Add(generator, new TeamInfo { holding = Team.Dead, capturing = Team.Dead });
                        }
                    }

                    update = Timing.RunCoroutine(_Update());
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });

            var mid_spawns = RoomIdentifier.AllRoomIdentifiers.Where(r => r.Name == RoomName.HczCheckpointToEntranceZone);
            RoomIdentifier team_a_ez = mid_spawns.Where(r => r.Zone == FacilityZone.Entrance).First();
            RoomIdentifier team_b_ez = mid_spawns.Where(r => r.Zone == FacilityZone.Entrance).Last();
            team_a_room = FacilityManager.GetAdjacent(team_a_ez).Keys.Where(r => r.Zone == FacilityZone.HeavyContainment).First();
            team_b_room = FacilityManager.GetAdjacent(team_b_ez).Keys.Where(r => r.Zone == FacilityZone.HeavyContainment).First();
            FacilityManager.LockJoinedRooms(new HashSet<RoomIdentifier> { team_a_ez, team_a_room }, DoorLockReason.AdminCommand);
            FacilityManager.LockJoinedRooms(new HashSet<RoomIdentifier> { team_b_ez, team_b_room }, DoorLockReason.AdminCommand);

            foreach (var door in DoorVariant.DoorsByRoom[RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.LczCheckpointA)])
                FacilityManager.LockDoor(door, DoorLockReason.AdminCommand);

            foreach (var door in DoorVariant.DoorsByRoom[RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.LczCheckpointB)])
                FacilityManager.LockDoor(door, DoorLockReason.AdminCommand);

            SetSpawnWave();
            for (int i = 0; i < config.WaveCount; i++)
            {
                Timing.CallDelayed(60.0f * (config.WavePeriod * (i + 1)), () =>
                {
                    SetSpawnWave();
                    foreach (var p in Player.GetPlayers())
                        if (p.Role == RoleTypeId.Spectator)
                            p.SetRole(RoleTypeId.ClassD);
                });
            }
            two_min = Timing.CallDelayed(60.0f * ((config.WavePeriod * (config.WaveCount + 1)) - 2.0f), () =>
            {
                foreach (var p in Player.GetPlayers())
                    p.SendBroadcast("game ending in two minutes\n" + CaptureStatus(), 60, shouldClearPrevious: true);
            });
            one_min = Timing.CallDelayed(60.0f * ((config.WavePeriod * (config.WaveCount + 1)) - 1.0f), () =>
            {
                foreach (var p in Player.GetPlayers())
                    p.SendBroadcast("game ending in one minute\n" + CaptureStatus(), 60, shouldClearPrevious: true);
            });
            end = Timing.CallDelayed(60.0f * (config.WavePeriod * (config.WaveCount + 1)), () =>
            {
                int scps = 0;
                int mtf = 0;
                int chaos = 0;
                foreach (var team in generator_team.Values)
                {
                    switch (team.holding)
                    {
                        case Team.SCPs: scps++; break;
                        case Team.FoundationForces: mtf++; break;
                        case Team.ChaosInsurgency: chaos++; break;
                    }
                }

                Round.IsLocked = false;
                if (scps >= 2)
                    EndRound(RoundSummary.LeadingTeam.Anomalies);
                else if (mtf >= 2)
                    EndRound(RoundSummary.LeadingTeam.FacilityForces);
                else if (chaos >= 2)
                    EndRound(RoundSummary.LeadingTeam.ChaosInsurgency);
                else
                    EndRound(RoundSummary.LeadingTeam.Draw);
            });
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted ||
                new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Overwatch || new_role.GetTeam() == Team.SCPs)
                return true;

            if (mtf.Contains(player.PlayerId) && (new_role.GetTeam() != Team.FoundationForces || new_role == RoleTypeId.FacilityGuard))
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (mtf_roles.IsEmpty())
                        player.SetRole(RoleTypeId.NtfPrivate);
                    else
                        player.SetRole(mtf_roles.PullRandomItem());
                });
                return false;
            }
            else if (chaos.Contains(player.PlayerId) && new_role.GetTeam() != Team.ChaosInsurgency)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (chaos_roles.IsEmpty())
                        player.SetRole(RoleTypeId.ChaosRifleman);
                    else
                        player.SetRole(chaos_roles.PullRandomItem());
                });
                return false;
            }

            return true;
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            if (player == null || !Round.IsRoundStarted)
                return;

            if (role.GetTeam() == Team.FoundationForces && role != RoleTypeId.FacilityGuard)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role.GetTeam() != Team.FoundationForces || player.Role == RoleTypeId.FacilityGuard)
                        return;
                    Teleport.Room(player, team_a_room);
                    player.EffectsManager.EnableEffect<SpawnProtected>(10);
                });
            }
            else if (role.GetTeam() == Team.ChaosInsurgency)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role.GetTeam() != Team.ChaosInsurgency)
                        return;
                    Teleport.Room(player, team_b_room);
                    player.EffectsManager.EnableEffect<SpawnProtected>(10);
                });
            }
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        bool OnRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            return false;
        }

        [PluginEvent(ServerEventType.GeneratorActivated)]
        void OnGeneratorActivated(Scp079Generator gen)
        {
            string broadcast = "The " + TeamString(generator_team[gen].capturing) + " have captured a generator";
            if (generator_team[gen].holding != Team.Dead)
                broadcast += " from the " + TeamString(generator_team[gen].holding);
            generator_team[gen].holding = generator_team[gen].capturing;
            broadcast += "\n" + CaptureStatus();
            gen.Network_syncTime = (short)gen._totalActivationTime;
            gen._currentTime = 0.0f;
            foreach (var p in Player.GetPlayers())
                p.SendBroadcast(broadcast, 15, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.PlayerActivateGenerator)]
        bool OnPlayerActivateGenerator(Player player, Scp079Generator gen)
        {
            if (player.Role.GetTeam() != generator_team[gen].holding)
            {
                if (gen.RemainingTime == gen._totalActivationTime || player.Role.GetTeam() == generator_team[gen].capturing)
                {
                    generator_team[gen].capturing = player.Role.GetTeam();
                    if (generator_team[gen].holding != Team.Dead)
                        foreach (var p in Player.GetPlayers())
                            p.SendBroadcast("A " + TeamString(generator_team[gen].holding) + " generator is being contested\n" + CaptureStatus(), 10, shouldClearPrevious: true);
                    return true;
                }
                else
                    player.SendBroadcast("Generator is cooling down: " + ((gen._totalActivationTime - gen.RemainingTime) / gen.DropdownSpeed).ToString("0"), 5, shouldClearPrevious: true);
            }
            return false;
        }

        [PluginEvent(ServerEventType.PlayerDeactivatedGenerator)]
        bool OnPlayerDeactivatedGenerator(Player player, Scp079Generator gen)
        {
            return player.Role.GetTeam() != generator_team[gen].capturing;
        }

        private static string CaptureStatus()
        {
            int scps = 0;
            int mtf = 0;
            int chaos = 0;
            foreach (var team in generator_team.Values)
            {
                switch (team.holding)
                {
                    case Team.SCPs: scps++; break;
                    case Team.FoundationForces: mtf++; break;
                    case Team.ChaosInsurgency: chaos++; break;
                }
            }
            return TeamString(Team.SCPs) + ": " + scps + " " + TeamString(Team.FoundationForces) + ": " + mtf + " " + TeamString(Team.ChaosInsurgency) + ": " + chaos;
        }

        private static IEnumerator<float> _Update()
        {
            while(true)
            {
                try
                {
                    foreach (var gen in generator_team.Keys)
                    {
                        RoomIdentifier room = RoomIdUtils.RoomAtPosition(gen.transform.position);
                        float captured = 1.0f - (gen.RemainingTime / gen._totalActivationTime);
                        FacilityManager.SetRoomLightColor(room, ((1.0f - captured) * TeamColor(generator_team[gen].holding)) + (captured * TeamColor(generator_team[gen].capturing)));
                        if(gen.Engaged)
                        {
                            gen.Engaged = false;
                            NetworkServer.UnSpawn(gen.gameObject);
                            NetworkServer.Spawn(gen.gameObject);
                        }
                    }
                }
                catch(System.Exception ex)
                {
                    Log.Error("_Update error: " + ex.ToString());
                }

                yield return Timing.WaitForOneFrame;
            }
        }

        private static Color TeamColor(Team team)
        {
            switch(team)
            {
                case Team.SCPs:
                    return new Color(1.0f, 0.0f, 0.0f);
                case Team.FoundationForces:
                    return new Color(0.0f, 0.0f, 1.0f);
                case Team.ChaosInsurgency:
                    return new Color(0.0f, 1.0f, 0.0f);
            }
            return Color.white;
        }

        private static string TeamString(Team team)
        {
            switch(team)
            {
                case Team.SCPs:
                    return "<color=#FF0000>SCPs</color>";
                case Team.FoundationForces:
                    return "<color=#0000FF>NTF</color>";
                case Team.ChaosInsurgency:
                    return "<color=#00FF00>CHAOS</color>";
            }
            return "None";
        }

        private static void SetSpawnWave()
        {
            int chaos_spectating = 0;
            int mtf_spectating = 0;
            foreach (var player in Player.GetPlayers())
            {
                if (player.Role != RoleTypeId.Spectator)
                    continue;

                if (chaos.Contains(player.PlayerId))
                    chaos_spectating++;
                else if (mtf.Contains(player.PlayerId))
                    mtf_spectating++;
            }

            chaos_roles.Clear();
            chaos_roles.AddRange(Enumerable.Repeat(RoleTypeId.ChaosRepressor, chaos_spectating / 7));
            chaos_roles.AddRange(Enumerable.Repeat(RoleTypeId.ChaosRepressor, chaos_spectating / 4));
            chaos_roles.AddRange(Enumerable.Repeat(RoleTypeId.ChaosRifleman, Mathf.Max(chaos_spectating - chaos_roles.Count, 0)));
            mtf_roles.Clear();
            mtf_roles.Add(RoleTypeId.NtfCaptain);
            mtf_roles.AddRange(Enumerable.Repeat(RoleTypeId.NtfSergeant, mtf_spectating / 3));
            mtf_roles.AddRange(Enumerable.Repeat(RoleTypeId.NtfPrivate, Mathf.Max(mtf_spectating - mtf_roles.Count, 0)));
        }

        private static void EndRound(RoundSummary.LeadingTeam team)
        {
            FriendlyFireConfig.PauseDetector = true;
            int round_cd = Mathf.Clamp(GameCore.ConfigFile.ServerConfig.GetInt("auto_round_restart_time", 10), 5, 1000);
            RoundSummary.singleton.RpcShowRoundSummary(
                new RoundSummary.SumInfo_ClassList(),
                new RoundSummary.SumInfo_ClassList(),
                team,
                RoundSummary.EscapedClassD,
                RoundSummary.EscapedScientists,
                RoundSummary.KilledBySCPs,
                round_cd,
                (int)GameCore.RoundStart.RoundLength.TotalSeconds);
            Timing.CallDelayed(round_cd - 1, () => RoundSummary.singleton.RpcDimScreen());
            Timing.CallDelayed(round_cd, () => RoundRestart.InitiateRoundRestart());
        }
    }

    public class GeneratorControlEvent:IEvent
    {
        public static GeneratorControlEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Generator Control";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "GC";
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
            harmony = new Harmony("GeneratorControlEvent");
            harmony.PatchAll();
            EventHandler.Start(EventConfig);
            Log.Info(EventName + " event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            harmony.UnpatchAll("GeneratorControlEvent");
            EventHandler.Stop();
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Generator Control Event", "1.0.0", "", "The Riptide")]
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
