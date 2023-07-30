using AdminToys;
using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using CustomPlayerEffects;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using MEC;
using Mirror;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using Respawning;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheRiptide.EventUtility;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        [Description("how many players per SCP, this is the ratio for the hard round (only one escape)")]
        public float ScpRatio { get; set; } = 6.0f;
        [Description("multiplier for the SCP count for the easy round (adjustment for having a two door room)")]
        public float EasyMultiplier { get; set; } = 1.333f;
        [Description("multiplier for the SCP count for the medium round (adjustment for having a two door room)")]
        public float MediumMultiplier { get; set; } = 1.666f;
    }

    public class EventHandler
    {
        private static Config config;
        private static bool game_over = false;
        private static Difficulty difficulty;
        private static RoomName room;
        private static Dictionary<int, RoleTypeId> scps = new Dictionary<int, RoleTypeId>();
        //private static List<RoleTypeId> scp_roles = new List<RoleTypeId>();
        private static List<RoleTypeId> mtf_roles = new List<RoleTypeId>();
        private static string round_info;
        private static int current_winner = -1;
        private static RoomIdentifier spawn_room = null;
        private static RoomIdentifier adjacent_a = null;
        private static RoomIdentifier adjacent_b = null;
        private static HashSet<RoomIdentifier> escape = new HashSet<RoomIdentifier>();
        private static List<LightSourceToy> lights = new List<LightSourceToy>();
        private static CoroutineHandle check_round_conditions;
        private static Dictionary<RoomName, Vector3> room_offsets = new Dictionary<RoomName, Vector3>
        {
            { RoomName.EzOfficeStoried, new Vector3(0.052f, 0.960f, 0.011f)},
            { RoomName.EzOfficeLarge, new Vector3(0.008f, 0.960f, -0.347f)},
            { RoomName.HczServers, new Vector3(-0.320f, 0.960f, 0.063f)},
            { RoomName.EzOfficeSmall, new Vector3(-0.320f, 0.960f, 0.063f)},
            { RoomName.LczAirlock, new Vector3(0.083f, 0.969f, 0.069f)},
            { RoomName.Lcz914, new Vector3(0.359f, 0.960f, -0.055f)},
            { RoomName.Hcz079, new Vector3(5.073f, -2.372f, -6.573f)},
            { RoomName.LczGlassroom, new Vector3(-0.122f, 0.960f, -3.868f)},
        };
        private static bool randomize_side;

        enum Difficulty { Easy, Medium, Hard };

        public static void Start(Config config)
        {
            EventHandler.config = config;
            round_info = "";
            game_over = false;
            WinnerReset();
            current_winner = -1;
            difficulty = Difficulty.Easy;
            Round.IsLocked = true;
            Round.IsLobbyLocked = true;
            Timing.CallDelayed(30.0f, () =>
            {
                try
                {
                    Round.IsLobbyLocked = false;
                    SetUpRound();
                    foreach (var p in Player.GetPlayers())
                        p.SendBroadcast("<size=24>" + TheLastStandEvent.Singleton.EventDescription.Replace("\n", "") + "</size>" + round_info , 30, shouldClearPrevious: true);
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
        }

        public static void Stop()
        {
            round_info = "";
            game_over = false;
            WinnerReset();
            current_winner = -1;
            Timing.KillCoroutines(check_round_conditions);
            escape.Clear();
            spawn_room = null;
            adjacent_a = null;
            adjacent_b = null;
            foreach (var light in lights)
                NetworkServer.Destroy(light.gameObject);
            lights.Clear();
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + TheLastStandEvent.Singleton.EventName + "\n<size=24>" + TheLastStandEvent.Singleton.EventDescription.Replace("\n", "") + "</size>", 30, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            EndRoom = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone == FacilityZone.Surface).First();
            RoomOffset = new Vector3(40.000f, 14.080f, -32.600f);

            Timing.CallDelayed(1.0f, () => { check_round_conditions = Timing.RunCoroutine(_CheckRoundConditions()); });

            FacilityManager.LockRoom(spawn_room, DoorLockReason.AdminCommand);
            RoomIdentifier sr = spawn_room;
            Timing.CallDelayed(15.0f, () => { FacilityManager.UnlockRoom(sr, DoorLockReason.AdminCommand); });
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        bool OnRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            //Log.Info("changed role: " + player.Nickname + " | " + new_role.ToString());
            if (player == null || !Round.IsRoundStarted || new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Overwatch || new_role == RoleTypeId.Tutorial)
                return true;

            if (game_over)
                return HandleGameOverRoleChange(player, new_role);

            if (scps.ContainsKey(player.PlayerId))
            {
                if(scps[player.PlayerId] != new_role)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        if (player.Role != scps[player.PlayerId])
                            player.SetRole(scps[player.PlayerId]);
                    });
                }
            }
            else
            {
                if (new_role.GetTeam() != Team.FoundationForces || new_role == RoleTypeId.FacilityGuard)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        if (player.Role.GetTeam() == Team.FoundationForces && player.Role != RoleTypeId.FacilityGuard)
                            return;
                        if (mtf_roles.IsEmpty())
                            player.SetRole(RoleTypeId.NtfPrivate);
                        else
                            player.SetRole(mtf_roles.PullRandomItem());
                    });
                }
            }
            return true;
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            if (player == null || !Round.IsRoundStarted)
                return;

            if (game_over)
            {
                HandleGameOverSpawn(player);
                return;
            }

            if(role.GetTeam() == Team.SCPs && role != RoleTypeId.Scp0492)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if(player.Role.GetTeam() == Team.SCPs)
                    {
                        if(player.Role == RoleTypeId.Scp079)
                        {
                            Scp079Camera camera = spawn_room.gameObject.GetComponentInChildren<Scp079Camera>();
                            Scp079Role role_079 = player.RoleBase as Scp079Role;
                            role_079._curCamSync.CurrentCamera = camera;
                        }
                        else
                        {
                            if (adjacent_b != null && randomize_side)
                            {
                                ScpDoorTeleport(player, adjacent_b);
                                randomize_side = false;
                            }
                            else
                            {
                                ScpDoorTeleport(player, adjacent_a);
                                randomize_side = true;
                            }
                            player.EffectsManager.ChangeState<Ensnared>(1, 5);
                        }

                        //if ((adjacent_b == null && player.Role != RoleTypeId.Scp079) || player.Role == RoleTypeId.Scp106 || player.Role == RoleTypeId.Scp939)
                        //    ScpDoorTeleport(player, adjacent_a);
                        //else if(player.Role == RoleTypeId.Scp049 || player.Role == RoleTypeId.Scp173 || player.Role == RoleTypeId.Scp096)
                        //    ScpDoorTeleport(player, adjacent_b);
                        //else if(player.Role == RoleTypeId.Scp079)
                        //{
                        //    Scp079Camera camera = spawn_room.gameObject.GetComponentInChildren<Scp079Camera>();
                        //    Scp079Role role_079 = player.RoleBase as Scp079Role;
                        //    role_079._curCamSync.CurrentCamera = camera;
                        //}
                        //if (player.Role != RoleTypeId.Scp079)
                        //    player.EffectsManager.ChangeState<Ensnared>(1, 5);
                    }
                });
            }
            else if (role.GetTeam() == Team.FoundationForces && role != RoleTypeId.FacilityGuard)
            {
                Timing.CallDelayed(0.0f, ()=>
                {
                    if(player.Role.GetTeam() == Team.FoundationForces && player.Role != RoleTypeId.FacilityGuard)
                    {
                        if (room_offsets.ContainsKey(room))
                            Teleport.RoomPos(player, spawn_room, room_offsets[room]);
                        else
                            Teleport.Room(player, spawn_room);
                        if (scps.Count == 6)
                        {
                            Timing.CallDelayed(11.0f, () =>
                            {
                                player.EffectsManager.ChangeState<Scanned>(1, 7);
                            });
                        }
                        player.EffectsManager.ChangeState<Ensnared>(1, 7);
                        RemoveItem(player, ItemType.GrenadeHE);
                    }
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerExitPocketDimension)]
        public void OnPlayerExitPocketDimension(Player player, bool is_succsefull)
        {
            Timing.CallDelayed(0.0f,()=>
            {
                Teleport.Room(player, spawn_room);
            });
        }

        [PluginEvent(ServerEventType.PlayerInteractDoor)]
        public bool OnPlayerInteractDoor(Player player, DoorVariant door, bool can_open)
        {
            if (can_open && player.Role.GetTeam() == Team.SCPs && door.TargetState == false &&
                door.Rooms.Count() == 2 && !door.Rooms.Contains(spawn_room))
            {
                player.SendBroadcast("you can only close this door", 5, shouldClearPrevious: true);
                return false;
            }
            return true;
        }

        private static void SetUpRound()
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    room = new List<RoomName>
                    {
                        RoomName.EzOfficeStoried,
                        RoomName.EzOfficeLarge,
                        RoomName.HczTestroom,
                        RoomName.HczServers,
                    }.RandomItem();
                    break;
                case Difficulty.Medium:
                    room = new List<RoomName>
                    {
                        RoomName.EzOfficeSmall,
                        RoomName.HczMicroHID,
                        RoomName.LczGreenhouse,
                        RoomName.LczAirlock,
                    }.RandomItem();
                    break;
                case Difficulty.Hard:
                    room = new List<RoomName>
                    {
                        RoomName.Lcz914,
                        RoomName.Lcz173,
                        RoomName.Lcz330,
                        RoomName.Hcz106,
                        RoomName.Hcz079,
                        RoomName.LczGlassroom,
                    }.RandomItem();
                    break;
            }

            int scp_count = Mathf.Max(Mathf.RoundToInt(Player.Count / config.ScpRatio), 1);
            if (difficulty == Difficulty.Easy)
                scp_count = Mathf.RoundToInt(scp_count * config.EasyMultiplier);
            else if (difficulty == Difficulty.Medium)
                scp_count = Mathf.RoundToInt(scp_count * config.MediumMultiplier);

            List<RoleTypeId> scp_roles = new List<RoleTypeId>()
            {
                RoleTypeId.Scp939,
                RoleTypeId.Scp173,
                RoleTypeId.Scp106,
                RoleTypeId.Scp049,
                RoleTypeId.Scp096,
                RoleTypeId.Scp079,
                RoleTypeId.Scp939,
                RoleTypeId.Scp173,
                RoleTypeId.Scp106,
                RoleTypeId.Scp049,
                RoleTypeId.Scp096,
            };
            if (scp_count > scp_roles.Count)
                scp_count = scp_roles.Count;

            if (scp_count != scp_roles.Count)
                scp_roles.RemoveRange(scp_count, scp_roles.Count - scp_count);

            scps.Clear();
            List<Player> players = Player.GetPlayers().ToList();
            for (int i = 0; i < scp_count; i++)
                if (players.Count != 0)
                    scps.Add(players.PullRandomItem().PlayerId, scp_roles.PullRandomItem());

            int mtf_count = Player.Count - scps.Count;
            mtf_roles.Clear();
            mtf_roles.Add(RoleTypeId.NtfCaptain);
            mtf_roles.AddRange(Enumerable.Repeat(RoleTypeId.NtfSergeant, mtf_count / 3));
            mtf_roles.AddRange(Enumerable.Repeat(RoleTypeId.NtfPrivate, Mathf.Max(mtf_count - mtf_roles.Count, 0)));

            FacilityManager.ResetAllRoomLights();
            escape.Clear();
            foreach (var light in lights)
                NetworkServer.Destroy(light.gameObject);
            lights.Clear();
            spawn_room = RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == room);
            HashSet<RoomIdentifier> adjacent = FacilityManager.GetAdjacent(spawn_room).Keys.ToHashSet();
            adjacent_a = adjacent.ElementAt(0);
            if (adjacent.Count == 2)
                adjacent_b = adjacent.ElementAt(1);
            else
                adjacent_b = null;
            foreach (var adj in adjacent)
            {
                foreach (var escape_room in FacilityManager.GetAdjacent(adj).Keys)
                {
                    if (escape_room != spawn_room)
                    {
                        escape.Add(escape_room);
                        FacilityManager.SetRoomLightColor(escape_room, new Color(0.0f, 1.0f, 0.0f));
                    }
                }

                foreach (var door in DoorVariant.DoorsByRoom[adj])
                {
                    if (door.Rooms.Length == 2)
                    {
                        if (door.Rooms.Contains(spawn_room))
                            lights.Add(AddLight((((3.0f * door.transform.position) + adj.transform.position) / 4.0f) + (1.0f * Vector3.up), new Color(1.0f, 1.0f, 0.0f)));
                        else
                            lights.Add(AddLight((((3.0f * door.transform.position) + adj.transform.position) / 4.0f) + (1.0f * Vector3.up), new Color(0.0f, 1.0f, 0.0f)));
                    }
                }
            }
            Player winner = Player.Get(current_winner);
            round_info = "\n<color=#FFFF00>Round: " + ((int)difficulty + 1) + " of 3, Difficulty: " + difficulty + ", Room: " + room + ", SCPs: " + scps.Count + ", CurrentWinner: " + (winner == null ? "None" : winner.Nickname) + "</color>";
        }

        private static void ScpDoorTeleport(Player player, RoomIdentifier room)
        {
            foreach(var door in DoorVariant.DoorsByRoom[room])
            {
                if(door.Rooms.Contains(spawn_room))
                {
                    Vector3 pos = room.transform.InverseTransformPoint(door.transform.position) * 0.85f;
                    pos.y = 1.0f;
                    Teleport.RoomPos(player, room, pos);
                }
            }    
        }

        private static LightSourceToy AddLight(Vector3 position, Color color)
        {
            GameObject light_pf = NetworkManager.singleton.spawnPrefabs.First(p => p.name == "LightSourceToy");
            LightSourceToy light_toy = Object.Instantiate(light_pf, position, Quaternion.identity).GetComponent<LightSourceToy>();
            light_toy.NetworkLightColor = color;
            light_toy.NetworkLightIntensity = 5.0f;
            light_toy.NetworkLightRange = 15.0f;
            light_toy.NetworkLightShadows = true;
            light_toy.NetworkMovementSmoothing = 10;
            NetworkServer.Spawn(light_toy.gameObject);

            return light_toy;
        }

        private static float RespawnSequence(bool win)
        {
            if (win)
                foreach (var p in Player.GetPlayers())
                    p.SendBroadcast("Player: " + Player.Get(current_winner).Nickname + " Escaped!\nNew round starting in 15 seconds", 10, shouldClearPrevious: true);
            else
                foreach (var p in Player.GetPlayers())
                    p.SendBroadcast("All NTF died\nNew round starting in 15 seconds", 10, shouldClearPrevious: true);

            Timing.CallDelayed(3.0f, () =>
            {
                foreach (var p in Player.GetPlayers())
                    if (p.Role != RoleTypeId.Spectator)
                        p.SetRole(RoleTypeId.Spectator);
            });

            Timing.CallDelayed(10.0f,()=>
            {
                if (difficulty == Difficulty.Easy)
                    difficulty = Difficulty.Medium;
                else if (difficulty == Difficulty.Medium)
                    difficulty = Difficulty.Hard;
                SetUpRound();
                foreach (var p in Player.GetPlayers())
                    p.SendBroadcast(round_info, 20, shouldClearPrevious: true);
            });

            Timing.CallDelayed(15.0f, () => 
            {
                FacilityManager.LockRoom(spawn_room, DoorLockReason.AdminCommand);
                RoomIdentifier sr = spawn_room;
                Timing.CallDelayed(15.0f, () => { FacilityManager.UnlockRoom(sr, DoorLockReason.AdminCommand); });

                foreach (var p in Player.GetPlayers())
                    p.SetRole(RoleTypeId.ClassD);
            });

            return 20.0f;
        }

        private static IEnumerator<float> _CheckRoundConditions()
        {
            while(true)
            {
                float yield = Timing.WaitForOneFrame;
                try
                {
                    int mtf_count = 0;
                    foreach (var p in Player.GetPlayers())
                    {
                        if (p.Role.GetTeam() == Team.FoundationForces && p.Role != RoleTypeId.FacilityGuard)
                        {
                            mtf_count++;
                            if (p.Room == null || !escape.Contains(p.Room))
                                continue;

                            current_winner = p.PlayerId;
                            if (difficulty == Difficulty.Hard)
                            {
                                game_over = true;
                                FoundWinner(p);
                                Timing.CallDelayed(6.0f, () => Round.IsLocked = false);
                                yield break;
                            }
                            else
                            {
                                yield = Timing.WaitForSeconds(RespawnSequence(true));
                                break;
                            }
                        }
                        else if (p.Role.GetTeam() == Team.SCPs)
                        {
                            if (p.Room != null && escape.Contains(p.Room))
                            {
                                if (FacilityManager.GetAdjacent(p.Room).ContainsKey(adjacent_a))
                                    p.Position = ((5.0f * p.Position) + adjacent_a.transform.position) / 6.0f;
                                else if (adjacent_b != null && FacilityManager.GetAdjacent(p.Room).ContainsKey(adjacent_b))
                                    p.Position = ((5.0f * p.Position) + adjacent_b.transform.position) / 6.0f;
                                else
                                    Log.Error("scp out of bounds");
                                p.SendBroadcast("you are at the wrong door. only mtf are allowed through this door!", 5, shouldClearPrevious: true);
                            }
                        }
                    }

                    if (mtf_count == 0)
                    {
                        if (difficulty == Difficulty.Hard)
                        {
                            game_over = true;
                            Player winner = Player.Get(current_winner);
                            if (winner != null)
                            {
                                FoundWinner(winner);
                                Timing.CallDelayed(6.0f, () => Round.IsLocked = false);
                            }
                            else
                            {
                                Round.IsLocked = false;
                                foreach (var p in Player.GetPlayers())
                                    p.SendBroadcast("There were no winners", 30, shouldClearPrevious: true);
                            }
                            yield break;
                        }
                        else
                        {
                            yield = Timing.WaitForSeconds(RespawnSequence(false));
                        }
                    }
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }

                yield return yield;
            }
        }

    }

    public class TheLastStandEvent:IEvent
    {
        public static TheLastStandEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "The Last Stand";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription { get; set; } = "All the NTF get trapped by all the SCPs in a random room. The SCPs must stop all the NTF from escaping. There will be 3 rounds increasing in difficulty. The player that escapes the most difficult round wins!\n\n";
        public string EventPrefix { get; } = "TLS";
        public bool OverrideWinConditions { get; }
        public bool BulletHolesAllowed { get; set; } = false;
        public PluginHandler PluginHandler { get; }
        public IEventConfig Config => EventConfig;

        [PluginConfig]
        public Config EventConfig;

        public void PrepareEvent()
        {
            Log.Info(EventName + " event is preparing");
            IsRunning = true;
            EventHandler.Start(EventConfig);
            Log.Info(EventName + " event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            EventHandler.Stop();
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("The Last Stand Event", "1.0.0", "", "The Riptide")]
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
