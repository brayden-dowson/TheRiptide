using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using MapGeneration.Distributors;
using MEC;
using PlayerRoles;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using Respawning;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;
        public string Description { get; set; } = "A random player is the site manager. They will be spawned in as a Scientist with a pistol, combat armor, a facility manager card and SCP 268. They will have a number of NTF private body guards (Manager and guards will be given {reduction}% damage reduction). The Site manager’s goal is to make it from light containment all the way to surface to turn on the nuke. To turn on the nuke the generators must be done. If the nuke goes off before the Site manager dies then the Site manager and guards win. Chaos will be spawned in and their goal is to simply kill the Site manager before the nuke is detonated.\n\n";

        [Description("Damage reduction as a percentage")]
        public float DamageReduction { get; set; } = 70.0f;
        public float PlayerToGuardRatio { get; set; } = 3.5f;
    }

    public class EventHandler
    {
        private static Config config;
        private static int site_manager;
        private static HashSet<int> guards = new HashSet<int>();
        private static int generators = 0;
        private static CoroutineHandle warhead_lever;

        public static void Start(Config config)
        {
            EventHandler.config = config;
            generators = 0;
            guards.Clear();
            Timing.KillCoroutines(warhead_lever);
        }

        public static void Stop()
        {
            generators = 0;
            guards.Clear();
            Timing.KillCoroutines(warhead_lever);
            Round.IsLobbyLocked = false;
            Round.IsLocked = false;
            Warhead.IsLocked = false;
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + ProtectTheSiteManagerEvent.Singleton.EventName + "\n<size=24>" + ProtectTheSiteManagerEvent.Singleton.EventDescription + "</size>", 60, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            if(player.PlayerId == site_manager)
            {
                List<int> players = ReadyPlayers().ConvertAll((p) => p.PlayerId).ToList();
                players.Remove(player.PlayerId);
                foreach (var id in guards)
                    players.Remove(id);
                if (players.Count == 0)
                    return;
                site_manager = players.RandomItem();
                Player new_sm = Player.Get(site_manager);
                new_sm.SetRole(RoleTypeId.Scientist);
                Timing.CallDelayed(0.1f,()=>
                {
                    if (guards.Count == 1)
                        new_sm.Position = Player.Get(guards.First()).Position;
                });
            }
            else if (guards.Contains(player.PlayerId))
            {
                List<int> players = ReadyPlayers().ConvertAll((p) => p.PlayerId).ToList();
                players.Remove(site_manager);
                foreach (var id in guards)
                    players.Remove(id);
                guards.Remove(player.PlayerId);
                int missing_count = Mathf.CeilToInt(players.Count / config.PlayerToGuardRatio) - guards.Count;
                for(int i = 0; i < missing_count; i++)
                {
                    if (players.Count == 0)
                        return;
                    guards.Add(players.RandomItem());
                }
            }
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            Warhead.IsLocked = true;
            Round.IsLocked = true;
            List<int> players = ReadyPlayers().ConvertAll((p) => p.PlayerId).ToList();
            site_manager = players.PullRandomItem();
            int guard_count = Mathf.RoundToInt(players.Count / config.PlayerToGuardRatio);
            if (guard_count == 0)
                guard_count = 1;
            for (int i = 0; i < guard_count; i++)
                guards.Add(players.PullRandomItem());

            foreach (var door in DoorVariant.DoorsByRoom[RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Lcz914)])
                if (door is PryableDoor)
                    FacilityManager.LockDoor(door, DoorLockReason.AdminCommand);

            foreach (var door in DoorVariant.DoorsByRoom[RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.LczArmory)])
                if (door.Rooms.Length == 1)
                    FacilityManager.LockDoor(door, DoorLockReason.AdminCommand);

            RespawnManager.Singleton.NextKnownTeam = SpawnableTeamType.ChaosInsurgency;
            RespawnTokensManager.ForceTeamDominance(SpawnableTeamType.ChaosInsurgency, 1f);
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        void OnRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            RespawnManager.Singleton.NextKnownTeam = SpawnableTeamType.ChaosInsurgency;
            RespawnTokensManager.ForceTeamDominance(SpawnableTeamType.ChaosInsurgency, 1f);
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted || new_role == RoleTypeId.Filmmaker || new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Overwatch)
                return true;

            if(player.PlayerId == site_manager)
            {
                if(new_role != RoleTypeId.Scientist)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.Scientist);
                    });
                    return false;
                }
            }
            else if(guards.Contains(player.PlayerId))
            {
                if(new_role != RoleTypeId.NtfPrivate)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.NtfPrivate);
                    });
                    return false;
                }
            }
            else
            {
                if (new_role.GetTeam() != Team.ChaosInsurgency)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.ChaosConscript);
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

            if(player.PlayerId == site_manager && role == RoleTypeId.Scientist)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    RemoveItem(player, ItemType.KeycardScientist);
                    player.AddItem(ItemType.KeycardFacilityManager);
                    player.AddItem(ItemType.ArmorCombat);
                    AddFirearm(player, ItemType.GunCOM15, true);
                    player.AddItem(ItemType.SCP268);
                    player.SendBroadcast("You are the site manager! Current objective: turn on all generators", 60, shouldClearPrevious: true);
                });
            }
            else if (guards.Contains(player.PlayerId) && role == RoleTypeId.NtfPrivate)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    Player player_sm = Player.Get(site_manager);
                    if (player_sm != null)
                        player.Position = player_sm.Position;
                    player.SendBroadcast("You are the site managers guard! Current objective: protect the site manager and follow directions", 60, shouldClearPrevious: true);
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerDamage)]
        void OnPlayerDamage(Player attacker, Player victim, DamageHandlerBase damage)
        {
            if (victim == null || attacker == null)
                return;

            if (damage is StandardDamageHandler standard && (victim.PlayerId == site_manager || guards.Contains(victim.PlayerId)))
                standard.Damage = standard.Damage * (1.0f - (config.DamageReduction / 100.0f));
        }

        [PluginEvent(ServerEventType.PlayerDeath)]
        void OnPlayerDeath(Player victim, Player attacker, DamageHandlerBase damageHandler)
        {
            if (victim == null || !Round.IsRoundStarted)
                return;

            if(victim.PlayerId == site_manager)
            {
                Round.IsLocked = false;
                foreach (var p in ReadyPlayers())
                {
                    p.SendBroadcast("CHAOS Insurgency Won!", 10, shouldClearPrevious: true);

                    if (p.Role.GetTeam() != Team.ChaosInsurgency && p.IsAlive)
                        p.Kill();
                }
            }
        }

        [PluginEvent(ServerEventType.WarheadDetonation)]
        public void OnWarheadDetonation()
        {
            Timing.CallDelayed(0.0f, () =>
            {
                Round.IsLocked = false;
                Player player_sm = Player.Get(site_manager);
                if(player_sm != null && player_sm.Role == RoleTypeId.Scientist)
                {
                    foreach (var p in ReadyPlayers())
                    {
                        p.SendBroadcast("The Site Manager Won!", 10, shouldClearPrevious: true);

                        if (p.Role.GetTeam() == Team.ChaosInsurgency)
                            p.Kill();
                    }
                }
            });
        }

        [PluginEvent(ServerEventType.GeneratorActivated)]
        void OnGeneratorActivated(Scp079Generator gen)
        {
            generators++;
            if (generators == 3)
            {
                Warhead.IsLocked = false;
                Player sm = Player.Get(site_manager);
                if (sm != null)
                    sm.SendBroadcast("All generators on. Current objective: Enable the nuke, and go to surface to detonate it", 60, shouldClearPrevious: true);
                warhead_lever = Timing.RunCoroutine(_LockWarheadOnEnabled());
            }
            else
            {
                Player sm = Player.Get(site_manager);
                if (sm != null)
                    sm.SendBroadcast(generators + "/3 generators on. Current objective: turn on all generators", 60, shouldClearPrevious: true);
            }
        }

        [PluginEvent(ServerEventType.PlayerActivateGenerator)]
        bool OnPlayerActivateGenerator(Player player, Scp079Generator gen)
        {
            return player.PlayerId == site_manager || guards.Contains(player.PlayerId);
        }

        [PluginEvent(ServerEventType.PlayerDeactivatedGenerator)]
        bool OnPlayerDeactivatedGenerator(Player player, Scp079Generator gen)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerEscape)]
        bool OnPlayerEscape(Player player, RoleTypeId role)
        {
            return false;
        }

        [PluginEvent(ServerEventType.WarheadStart)]
        public void OnWarheadStart(bool isAutomatic, Player player, bool isResumed)
        {
            Warhead.IsLocked = true;
        }

        private static IEnumerator<float> _LockWarheadOnEnabled()
        {
            AlphaWarheadNukesitePanel warhead = Server.Instance.GetComponent<AlphaWarheadNukesitePanel>(true);
            bool set = false;
            while(true)
            {
                if (!set && Warhead.LeverStatus == true)
                {
                    set = true;
                    Player sm = Player.Get(site_manager);
                    if (sm != null)
                        sm.SendBroadcast("Nuke has been Enabled permanently. Current objective: get to surface to detonate nuke", 60, shouldClearPrevious: true);
                }
                else
                {
                    if (Warhead.LeverStatus == false)
                        warhead.Networkenabled = true;
                }
                if (Warhead.IsDetonationInProgress)
                    break;
                yield return Timing.WaitForSeconds(1.0f);
            }
        }
    }

    public class ProtectTheSiteManagerEvent:IEvent
    {
        public static ProtectTheSiteManagerEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Protect The Site Manager";
        public string EvenAuthor { get; } = "The Riptide. Idea by Guy in Grey";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description.Replace("{reduction}", EventConfig.DamageReduction.ToString("0")); }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "PTSM";
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

        [PluginEntryPoint("Protect The Site Manager Event", "1.0.0", "", "The Riptide")]
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
