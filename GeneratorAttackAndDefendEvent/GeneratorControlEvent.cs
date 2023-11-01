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
using AdminToys;
using InventorySystem.Items.Pickups;
using PluginAPI.Events;
using PlayerRoles.PlayableScps.Scp079;
using static TheRiptide.Utility;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using PlayerRoles.Ragdolls;

namespace TheRiptide
{
    public class ConfigColor
    {
        public float Red { get; set; } = 1.0f;
        public float Green { get; set; } = 1.0f;
        public float Blue { get; set; } = 1.0f;

        public Color ToColor()
        {
            return new Color(Red, Green, Blue);
        }
    }

    public class CustomRole
    {
        public string Name { get; set; } = "unnamed";
        public string Description { get; set; } = "";
        public int MtfWeight { get; set; } = 1;
        public int ChaosWeight { get; set; } = 1;
        public int ScpWeight { get; set; } = 1;
        public Faction Faction { get; set; } = Faction.FoundationStaff;
        public List<ItemType> Inventory { get; set; } = new List<ItemType>();
    }

    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        public int WaveCount { get; set; } = 5;
        public float WavePeriod { get; set; } = 2.0f;

        public bool EnableScpTeam { get; set; } = true;
        public bool StaggerWaves { get; set; } = true;

        public string Description { get; set; } = "CHAOS, NTF and SCPs must each turn on the genrators around the facility and defend them from opposing factions. The Room will glow the color of which ever faction holds the current generator. CHAOS teaming with the SCPs is not allowed. All zones except heavy are locked down. NTF and CHAOS get to respawn. After all respawns the team with the most generators on wins!\n\n";

        public int MinGenerators { get; set; } = 5;
        public int MaxGenerators { get; set; } = 5;
        public bool OnePerRoom { get; set; } = true;
        public bool NoEndRooms { get; set; } = false;

        public float ActivationTime { get; set; } = 60.0f;
        public float CooldownTime { get; set; } = 20.0f;

        public ConfigColor MtfColor { get; set; } = new ConfigColor { Red = 0.25f, Green = 0.5f, Blue = 1.25f };
        public ConfigColor ChaosColor { get; set; } = new ConfigColor { Red = 0.3f, Green = 0.75f, Blue = 0.3f };
        public ConfigColor ScpColor { get; set; } = new ConfigColor { Red = 1.00f, Green = 0.25f, Blue = 0.25f };

        public int MaxPickups { get; set; } = 200;
        public int MaxRagdolls { get; set; } = 30;

        public bool UseCustomRoles { get; set; } = false;
        [Description("used in the weights calculations. higher values smooth out fluctuations in the currect score more")]
        public float MovingAverageScoreWindowSize { get; set; } = 4.0f;
        public List<CustomRole> CustomRoles { get; set; } = new List<CustomRole>
        {
            new CustomRole{ Name = "Scavenger", MtfWeight = 10, ChaosWeight = 0, ScpWeight = 0, Faction = Faction.FoundationStaff, Inventory = new List<ItemType>{
                ItemType.ArmorLight, ItemType.GunCOM18, ItemType.Adrenaline, ItemType.Radio, ItemType.KeycardMTFOperative, ItemType.GrenadeHE } },
            new CustomRole{ Name = "Guard", MtfWeight = 6, ChaosWeight = 2, ScpWeight = 1, Faction = Faction.FoundationStaff, Inventory = new List<ItemType>{
                ItemType.ArmorCombat, ItemType.GunFSP9, ItemType.Painkillers, ItemType.Radio, ItemType.KeycardMTFOperative, ItemType.GrenadeHE } },
            new CustomRole{ Name = "Private", MtfWeight = 2, ChaosWeight = 6, ScpWeight = 2, Faction = Faction.FoundationStaff, Inventory = new List<ItemType>{
                ItemType.ArmorCombat, ItemType.GunCrossvec, ItemType.Medkit, ItemType.Radio, ItemType.KeycardMTFOperative } },
            new CustomRole{ Name = "Sergeant", MtfWeight = 2, ChaosWeight = 10, ScpWeight = 2, Faction = Faction.FoundationStaff, Inventory = new List<ItemType>{
                ItemType.ArmorHeavy, ItemType.GunE11SR, ItemType.SCP500, ItemType.Radio, ItemType.SCP1853, ItemType.KeycardMTFOperative } },

            new CustomRole{ Name = "Cleanup Crew", MtfWeight = 0, ChaosWeight = 0, ScpWeight = 3, Faction = Faction.FoundationStaff, Inventory = new List<ItemType>{
                ItemType.ArmorLight, ItemType.GunCrossvec, ItemType.Adrenaline, ItemType.Radio, ItemType.SCP207, ItemType.SCP2176, ItemType.KeycardMTFCaptain, ItemType.GrenadeFlash } },
            new CustomRole{ Name = "Security", MtfWeight = 0, ChaosWeight = 0, ScpWeight = 3, Faction = Faction.FoundationStaff, Inventory = new List<ItemType>{
                ItemType.ArmorLight, ItemType.ParticleDisruptor, ItemType.SCP330, ItemType.SCP330, ItemType.SCP330, ItemType.Radio, ItemType.SCP2176, ItemType.KeycardMTFCaptain, ItemType.GrenadeFlash } },
            new CustomRole{ Name = "Exterminator", MtfWeight = 0, ChaosWeight = 0, ScpWeight = 3, Faction = Faction.FoundationStaff, Inventory = new List<ItemType>{
                ItemType.ArmorLight, ItemType.MicroHID, ItemType.SCP330, ItemType.SCP330, ItemType.SCP330, ItemType.Radio, ItemType.SCP2176, ItemType.KeycardMTFCaptain, ItemType.GrenadeFlash } },
            new CustomRole{ Name = "Recontainer", MtfWeight = 0, ChaosWeight = 1, ScpWeight = 3, Faction = Faction.FoundationStaff, Inventory = new List<ItemType>{
                ItemType.ArmorLight, ItemType.Jailbird, ItemType.SCP330, ItemType.SCP330, ItemType.SCP330, ItemType.Radio, ItemType.SCP018, ItemType.KeycardMTFCaptain, ItemType.GrenadeFlash } },
            new CustomRole{ Name = "Obliterator", MtfWeight = 0, ChaosWeight = 2, ScpWeight = 5, Faction = Faction.FoundationStaff, Inventory = new List<ItemType>{
                ItemType.ArmorCombat, ItemType.GunFRMG0, ItemType.SCP330, ItemType.SCP330, ItemType.SCP330, ItemType.Radio, ItemType.SCP244a, ItemType.KeycardMTFCaptain } },

            new CustomRole{ Name = "Scavenger", MtfWeight = 0, ChaosWeight = 10, ScpWeight = 0, Faction = Faction.FoundationEnemy, Inventory = new List<ItemType>{
                ItemType.ArmorLight, ItemType.GunCOM18, ItemType.Adrenaline, ItemType.Radio, ItemType.KeycardChaosInsurgency, ItemType.GrenadeHE } },
            new CustomRole{ Name = "Guard", MtfWeight = 2, ChaosWeight = 6, ScpWeight = 1, Faction = Faction.FoundationEnemy, Inventory = new List<ItemType>{
                ItemType.ArmorCombat, ItemType.GunA7, ItemType.Painkillers, ItemType.Radio, ItemType.KeycardChaosInsurgency, ItemType.GrenadeHE } },
            new CustomRole{ Name = "Private", MtfWeight = 6, ChaosWeight = 2, ScpWeight = 2, Faction = Faction.FoundationEnemy, Inventory = new List<ItemType>{
                ItemType.ArmorCombat, ItemType.GunAK, ItemType.Medkit, ItemType.Radio, ItemType.KeycardChaosInsurgency } },
            new CustomRole{ Name = "Sergeant", MtfWeight = 10, ChaosWeight = 2, ScpWeight = 2, Faction = Faction.FoundationEnemy, Inventory = new List<ItemType>{
                ItemType.ArmorHeavy, ItemType.GunRevolver, ItemType.GunShotgun, ItemType.SCP500, ItemType.Radio, ItemType.SCP1853, ItemType.KeycardChaosInsurgency } },

            new CustomRole{ Name = "Cleanup Crew", MtfWeight = 0, ChaosWeight = 0, ScpWeight = 3, Faction = Faction.FoundationEnemy, Inventory = new List<ItemType>{
                ItemType.ArmorLight, ItemType.GunAK, ItemType.Adrenaline, ItemType.SCP207, ItemType.SCP2176, ItemType.KeycardChaosInsurgency, ItemType.GrenadeFlash } },
            new CustomRole{ Name = "Security", MtfWeight = 0, ChaosWeight = 0, ScpWeight = 4, Faction = Faction.FoundationEnemy, Inventory = new List<ItemType>{
                ItemType.ArmorLight, ItemType.ParticleDisruptor, ItemType.SCP330, ItemType.SCP330, ItemType.SCP330, ItemType.SCP2176, ItemType.KeycardChaosInsurgency, ItemType.GrenadeFlash } },
            new CustomRole{ Name = "Exterminator", MtfWeight = 0, ChaosWeight = 0, ScpWeight = 4, Faction = Faction.FoundationEnemy, Inventory = new List<ItemType>{
                ItemType.ArmorLight, ItemType.MicroHID, ItemType.SCP330, ItemType.SCP330, ItemType.SCP330, ItemType.SCP2176, ItemType.KeycardChaosInsurgency, ItemType.GrenadeFlash } },
            new CustomRole{ Name = "Recontainer", MtfWeight = 1, ChaosWeight = 0, ScpWeight = 4, Faction = Faction.FoundationEnemy, Inventory = new List<ItemType>{
                ItemType.ArmorLight, ItemType.Jailbird, ItemType.SCP330, ItemType.SCP330, ItemType.SCP330,  ItemType.SCP018, ItemType.KeycardChaosInsurgency, ItemType.GrenadeFlash } },
            new CustomRole{ Name = "Obliterator", MtfWeight = 2, ChaosWeight = 0, ScpWeight = 5, Faction = Faction.FoundationEnemy, Inventory = new List<ItemType>{
                ItemType.ArmorCombat, ItemType.GunLogicer, ItemType.SCP330, ItemType.SCP330, ItemType.SCP330, ItemType.SCP244a, ItemType.KeycardChaosInsurgency } },
        };

        [Description("[AUTO GENERATED] all item types to use for their inventory")]
        public List<ItemType> AllItemTypes { get; set; } = System.Enum.GetValues(typeof(ItemType)).ToArray<ItemType>().ToList();
    }

    public class SpawnWeights
    {
        private int window_size;
        private Queue<float> mtf_gens = new Queue<float>();
        private Queue<float> chaos_gens = new Queue<float>();
        private Queue<float> scp_gens = new Queue<float>();
        private float mtf_total = 0;
        private float chaos_total = 0;
        private float scp_total = 0;

        public SpawnWeights(int window_size)
        {
            this.window_size = window_size;
            for (int i = 0; i < window_size; i++)
            {
                mtf_gens.Enqueue(0);
                chaos_gens.Enqueue(0);
                scp_gens.Enqueue(0);
            }
        }

        public void AddRecord(float mtf, float chaos, float scp)
        {
            mtf_gens.Enqueue(mtf);
            chaos_gens.Enqueue(chaos);
            scp_gens.Enqueue(scp);
            mtf_total += mtf;
            chaos_total += chaos;
            scp_total += scp;
            if (mtf_gens.Count > window_size)
                mtf_total -= mtf_gens.Dequeue();
            if (chaos_gens.Count > window_size)
                chaos_total -= chaos_gens.Dequeue();
            if (scp_gens.Count > window_size)
                scp_total -= scp_gens.Dequeue();
        }

        public float GetMtfWeight()
        {
            if (mtf_gens.Count == 0)
                return mtf_total;
            return mtf_total / mtf_gens.Count;
        }

        public float GetChaosWeight()
        {
            if (chaos_gens.Count == 0)
                return chaos_total;
            return chaos_total / chaos_gens.Count;
        }

        public float GetScpWeight()
        {
            if (scp_gens.Count == 0)
                return scp_total;
            return scp_total / scp_gens.Count;
        }
    }

    public class EventHandler
    {
        class TeamInfo
        {
            public Team holding;
            public Team capturing;
        }

        public static Config config;

        private static Dictionary<Scp079Generator, TeamInfo> generator_team = new Dictionary<Scp079Generator, TeamInfo>();
        private static CoroutineHandle update;
        private static CoroutineHandle two_min;
        private static CoroutineHandle one_min;
        private static CoroutineHandle end;
        private static CoroutineHandle item_update;
        private static CoroutineHandle ragdoll_update;
        private static List<CoroutineHandle> spawn_waves = new List<CoroutineHandle>();
        private static CoroutineHandle weight_update;

        private static HashSet<int> chaos = new HashSet<int>();
        private static HashSet<int> mtf = new HashSet<int>();
        private static List<RoleTypeId> chaos_roles = new List<RoleTypeId>();
        private static List<RoleTypeId> mtf_roles = new List<RoleTypeId>();

        private static RoomIdentifier team_a_room;
        private static RoomIdentifier team_b_room;

        private static PrimitiveObjectToy team_a_blocker = null;
        private static PrimitiveObjectToy team_b_blocker = null;
        private static PrimitiveObjectToy team_a_window = null;
        private static PrimitiveObjectToy team_b_window = null;

        private static SpawnWeights spawn_weights;

        public static void Start(Config config)
        {
            EventHandler.config = config;
            generator_team.Clear();

            chaos.Clear();
            mtf.Clear();
            chaos_roles.Clear();
            mtf_roles.Clear();
            spawn_weights = new SpawnWeights(Mathf.RoundToInt(60 * config.MovingAverageScoreWindowSize));
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
            team_a_blocker = null;
            team_b_blocker = null;
            team_a_window = null;
            team_b_window = null;
            Timing.KillCoroutines(update, two_min, one_min, end, item_update, ragdoll_update, weight_update);
            foreach (var wave in spawn_waves)
                Timing.KillCoroutines(wave);
            spawn_weights = null;
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
                        foreach (var generator in generators)
                        {
                            generator.ServerSetFlag(Scp079Generator.GeneratorFlags.Unlocked, true);
                            generator._totalActivationTime = config.ActivationTime;
                            generator._totalDeactivationTime = config.CooldownTime;
                            generator_team.Add(generator, new TeamInfo { holding = Team.Dead, capturing = Team.Dead });
                        }
                    }

                    update = Timing.RunCoroutine(_Update());
                    item_update = Timing.RunCoroutine(_ItemUpdate());
                    ragdoll_update = Timing.RunCoroutine(_RagdollUpdate());
                    weight_update = Timing.RunCoroutine(_WeightsUpdate());
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

            slocLoader.Objects.PrimitiveObject blocker = new slocLoader.Objects.PrimitiveObject(slocLoader.Objects.ObjectType.Quad);
            blocker.Transform.Scale = new Vector3(4.5f, 3.0f, 1.0f);
            blocker.Transform.Position = new Vector3(-0.575f, 1.5f, 1.15f);
            blocker.Transform.Rotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);
            blocker.MaterialColor = new Color(1.0f, 0.0f, 0.0f);
            blocker.ColliderMode = slocLoader.Objects.PrimitiveObject.ColliderCreationMode.ClientOnly;
            team_a_blocker = slocLoader.API.SpawnObject(blocker, team_a_room.gameObject).GetComponent<PrimitiveObjectToy>();
            Timing.CallDelayed(0.0f, () => team_a_blocker._spawnedPrimitive.layer = LayerMask.NameToLayer("Hitbox"));
            team_b_blocker = slocLoader.API.SpawnObject(blocker, team_b_room.gameObject).GetComponent<PrimitiveObjectToy>();
            Timing.CallDelayed(0.0f, () => team_b_blocker._spawnedPrimitive.layer = LayerMask.NameToLayer("Hitbox"));

            slocLoader.Objects.PrimitiveObject window = new slocLoader.Objects.PrimitiveObject(slocLoader.Objects.ObjectType.Quad);
            window.Transform.Scale = new Vector3(2.8f, 1.6f, 1.0f);
            window.Transform.Position = new Vector3(-4.5f, 1.5f, -5.0f);
            window.Transform.Rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
            window.MaterialColor = new Color(1.0f, 0.0f, 0.0f);
            window.ColliderMode = slocLoader.Objects.PrimitiveObject.ColliderCreationMode.ClientOnly;
            team_a_window = slocLoader.API.SpawnObject(window, team_a_room.gameObject).GetComponent<PrimitiveObjectToy>();
            team_b_window = slocLoader.API.SpawnObject(window, team_b_room.gameObject).GetComponent<PrimitiveObjectToy>();

            SetSpawnWave();
            if (config.StaggerWaves)
            {
                spawn_waves.Add(Timing.CallDelayed(60.0f * (config.WavePeriod * 0.5f), () =>
                {
                    foreach (var p in Player.GetPlayers())
                        if (p.Role == RoleTypeId.Spectator && chaos.Contains(p.PlayerId))
                            p.SetRole(RoleTypeId.ClassD);
                }));
            }
            for (int i = 0; i < config.WaveCount; i++)
            {
                spawn_waves.Add(Timing.CallDelayed(60.0f * (config.WavePeriod * (i + 1.0f)), () =>
                {
                    SetSpawnWave();
                    foreach (var p in Player.GetPlayers())
                        if (p.Role == RoleTypeId.Spectator && mtf.Contains(p.PlayerId))
                            p.SetRole(RoleTypeId.ClassD);
                }));
                float offset = (config.StaggerWaves && i + 1 != config.WaveCount) ? 1.5f : 1.0f;
                spawn_waves.Add(Timing.CallDelayed(60.0f * (config.WavePeriod * (i + offset)), () =>
                {
                    foreach (var p in Player.GetPlayers())
                        if (p.Role == RoleTypeId.Spectator && chaos.Contains(p.PlayerId))
                            p.SetRole(RoleTypeId.ClassD);
                }));
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
                if (scps > mtf && scps > chaos)
                    EndRound(RoundSummary.LeadingTeam.Anomalies);
                else if (mtf > scps && mtf > chaos)
                    EndRound(RoundSummary.LeadingTeam.FacilityForces);
                else if (chaos > mtf && chaos > scps)
                    EndRound(RoundSummary.LeadingTeam.ChaosInsurgency);
                else
                    EndRound(RoundSummary.LeadingTeam.Draw);
            });
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted ||
                new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Overwatch || (config.EnableScpTeam && new_role.GetTeam() == Team.SCPs) || new_role == RoleTypeId.Filmmaker)
                return true;

            if (reason == RoleChangeReason.RoundStart && config.StaggerWaves && chaos.Contains(player.PlayerId))
                Timing.CallDelayed(0.0f, () => { player.SetRole(RoleTypeId.Spectator); });

            if (new_role == RoleTypeId.ChaosConscript)
            {
                mtf.Remove(player.PlayerId);
                chaos.Add(player.PlayerId);
            }
            else if (new_role == RoleTypeId.NtfSpecialist)
            {
                chaos.Remove(player.PlayerId);
                mtf.Add(player.PlayerId);
            }

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

            if (team_a_blocker != null)
                NetworkServer.SendSpawnMessage(team_a_blocker.netIdentity, player.Connection);
            if (team_a_window != null)
                NetworkServer.SendSpawnMessage(team_a_window.netIdentity, player.Connection);
            if (team_b_blocker != null)
                NetworkServer.SendSpawnMessage(team_b_blocker.netIdentity, player.Connection);
            if (team_b_blocker != null)
                NetworkServer.SendSpawnMessage(team_b_window.netIdentity, player.Connection);

            if (role.GetTeam() == Team.FoundationForces && role != RoleTypeId.FacilityGuard)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role.GetTeam() != Team.FoundationForces || player.Role == RoleTypeId.FacilityGuard)
                        return;
                    Teleport.Room(player, team_a_room);
                    player.EffectsManager.EnableEffect<SpawnProtected>(10);
                    player.Connection.Send(new ObjectDestroyMessage { netId = team_a_blocker.netId });
                    player.Connection.Send(new ObjectDestroyMessage { netId = team_a_window.netId });
                    SetLoadout(player);
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
                    player.Connection.Send(new ObjectDestroyMessage { netId = team_b_blocker.netId });
                    player.Connection.Send(new ObjectDestroyMessage { netId = team_b_window.netId });
                    SetLoadout(player);
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
            while (true)
            {
                try
                {
                    foreach (var gen in generator_team.Keys)
                    {
                        RoomIdentifier room = RoomIdUtils.RoomAtPosition(gen.transform.position);
                        float captured = 1.0f - (gen.RemainingTime / gen._totalActivationTime);
                        FacilityManager.SetRoomLightColor(room, ((1.0f - captured) * TeamColor(generator_team[gen].holding)) + (captured * TeamColor(generator_team[gen].capturing)));
                        if (gen.Engaged)
                        {
                            gen.Engaged = false;
                            NetworkServer.UnSpawn(gen.gameObject);
                            NetworkServer.Spawn(gen.gameObject);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error("_Update error: " + ex.ToString());
                }

                yield return Timing.WaitForOneFrame;
            }
        }

        private static Color TeamColor(Team team)
        {
            switch (team)
            {
                case Team.SCPs:
                    return config.ScpColor.ToColor();
                case Team.FoundationForces:
                    return config.MtfColor.ToColor();
                case Team.ChaosInsurgency:
                    return config.ChaosColor.ToColor();
            }
            return Color.white;
        }

        private static string TeamString(Team team)
        {
            switch (team)
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

        private static void SetLoadout(Player player)
        {
            if (!config.UseCustomRoles)
                return;

            if (player.Role.GetFaction() == Faction.FoundationStaff)
                ApplyCustomRole(player, Faction.FoundationStaff);
            else if (player.Role.GetFaction() == Faction.FoundationEnemy)
                ApplyCustomRole(player, Faction.FoundationEnemy);
        }

        private static void ApplyCustomRole(Player player, Faction faction)
        {
            float mtf_s = spawn_weights.GetMtfWeight();
            float chaos_s = spawn_weights.GetChaosWeight();
            float scp_s = spawn_weights.GetScpWeight();

            float mtf = 1.0f - (chaos_s + scp_s);
            float chaos = 1.0f - (mtf_s + scp_s);
            float scp = 1.0f - (mtf_s + chaos_s);
            float total = mtf + chaos + scp;
            Log.Info("Moving Average Score [mtf:{0}, chaos:{1}, scp:{2}] Weights [mtf:{3}, chaos:{4}, scp:{5}]".
                Replace("{0}", mtf_s.ToString("0.000")).
                Replace("{1}", chaos_s.ToString("0.000")).
                Replace("{2}", scp_s.ToString("0.000")).
                Replace("{3}", mtf.ToString("0.000")).
                Replace("{4}", chaos.ToString("0.000")).
                Replace("{5}", scp.ToString("0.000")));
            float weight_total = 0.0f;
            List<KeyValuePair<CustomRole, float>> weights = new List<KeyValuePair<CustomRole, float>>();
            foreach (var r in config.CustomRoles.Where(r => r.Faction == faction))
            {
                float w = (mtf * r.MtfWeight + chaos * r.ChaosWeight + scp * r.ScpWeight) / total;
                weight_total += w;
                weights.Add(new KeyValuePair<CustomRole, float>(r, w));
            }
            float x = Random.value * weight_total;
            float y = weights.First().Value;
            int i = 0;
            for (; ; )
            {
                if (x < y || (i + 1 >= weights.Count))
                    break;
                i++;
                y += weights[i].Value;
            }
            CustomRole role = weights[i].Key;
            player.ClearInventory();
            foreach(var item in role.Inventory)
            {
                if (IsGun(item))
                {
                    if (item != ItemType.ParticleDisruptor)
                        AddFirearm(player, item, true);
                    else
                    {
                        var pd = player.AddItem(item) as ParticleDisruptor;
                        pd.ApplyAttachmentsCode(0, true);
                        pd.Status = new FirearmStatus(5, FirearmStatusFlags.MagazineInserted, pd.GetCurrentAttachmentsCode());
                    }
                }
                else
                    player.AddItem(item);
            }
            player.ReceiveHint("\n\n\n\n<size=48><b>" + role.Name + "</b></size>\n" + role.Description, 10);
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

        private static IEnumerator<float> _WeightsUpdate()
        {
            while(true)
            {
                try
                {
                    float mtf = 0, chaos = 0, scp = 0;
                    foreach(var gen in generator_team)
                    {
                        float spilt = gen.Key.RemainingTime / gen.Key._totalActivationTime;
                        switch (gen.Value.holding)
                        {
                            case Team.FoundationForces: mtf += (1.0f - spilt); break;
                            case Team.ChaosInsurgency: chaos += (1.0f - spilt); break;
                            case Team.SCPs: scp += (1.0f - spilt); break;
                        }
                        switch (gen.Value.capturing)
                        {
                            case Team.FoundationForces: mtf += spilt; break;
                            case Team.ChaosInsurgency: chaos += spilt; break;
                            case Team.SCPs: scp += spilt; break;
                        }
                    }
                    spawn_weights.AddRecord(mtf / generator_team.Count, chaos / generator_team.Count, scp / generator_team.Count);
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }

        private static IEnumerator<float> _ItemUpdate()
        {
            while(true)
            {
                try
                {
                    List<ItemPickupBase> items = Object.FindObjectsOfType<ItemPickupBase>().Where(i => i.PreviousOwner.IsSet).ToList();
                    if(items.Count > config.MaxPickups)
                    {
                        items.Sort((l, r) => (int)(r.PreviousOwner.Stopwatch.Elapsed.TotalSeconds - l.PreviousOwner.Stopwatch.Elapsed.TotalSeconds));
                        int destroy_count = items.Count - config.MaxPickups;
                        for (int i = 0; i < destroy_count; i++)
                            NetworkServer.Destroy(items[i].gameObject);
                    }
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                yield return Timing.WaitForSeconds(10.0f);
            }
        }

        private static IEnumerator<float> _RagdollUpdate()
        {
            while (true)
            {
                try
                {
                    List<BasicRagdoll> ragdolls = Object.FindObjectsOfType<BasicRagdoll>().Where(r => r.Info != null).ToList();
                    if (ragdolls.Count > config.MaxRagdolls)
                    {
                        ragdolls.Sort((l, r) => (int)(l.Info.CreationTime - r.Info.CreationTime));
                        int destroy_count = ragdolls.Count - config.MaxRagdolls;
                        for (int i = 0; i < destroy_count; i++)
                            NetworkServer.Destroy(ragdolls[i].gameObject);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                yield return Timing.WaitForSeconds(10.0f);
            }
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
