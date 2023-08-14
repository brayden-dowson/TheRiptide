using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using CustomPlayerEffects;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Pickups;
using MapGeneration;
using MEC;
using Mirror;
using NWAPIPermissionSystem;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using Respawning;
using Scp914;
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
        public string Description { get; set; } = "Crossvec facility wide pvp. Autonuke 5 minutes into the round!\n\n";
    }

    //1 970 -44
    public class EventHandler
    {
        private static HashSet<int> team_a = new HashSet<int>();
        private static HashSet<int> team_b = new HashSet<int>();

        private static RoomIdentifier team_a_room;
        private static RoomIdentifier team_b_room;

        private static RoleTypeId team_a_role;
        private static RoleTypeId team_b_role;

        private static bool old_ff;

        public static void Start()
        {
            ChristmasZoneTeamManager.Singleton.RandomizeTeamsAssignment();
            old_ff = Server.FriendlyFire;
            Server.FriendlyFire = false;
            team_a.Clear();
            team_b.Clear();
        }

        public static void Stop()
        {
            Server.FriendlyFire = old_ff;
            team_a.Clear();
            team_b.Clear();
            team_a_room = null;
            team_b_room = null;
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            string info = "Event being played: " + ChristmasZonePvPEvent.Singleton.EventName + "\n<size=24>" + ChristmasZonePvPEvent.Singleton.EventDescription + "</size>";
            ChristmasZoneTeamManager.Singleton.AssignTeam(player, team_a, team_b, info);
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            team_a.Remove(player.PlayerId);
            team_b.Remove(player.PlayerId);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            ClearItemPickups();

            foreach(var ng in ServerStatic.GetPermissionsHandler()._groups)
            {
                Log.Info(ng.Key + " | " + ng.Value.BadgeText);
            }

            RoomIdentifier surface = RoomIdentifier.AllRoomIdentifiers.First(r => r.Zone == FacilityZone.Surface);
            RoomIdentifier class_d_cells = RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.LczClassDSpawn);

            if (Random.value < 0.5)
            {
                team_a_room = surface;
                team_a_role = RoleTypeId.Scientist;
                team_b_room = class_d_cells;
                team_b_role = RoleTypeId.ClassD;
            }
            else
            {
                team_b_room = surface;
                team_b_role = RoleTypeId.Scientist;
                team_a_room = class_d_cells;
                team_a_role = RoleTypeId.ClassD;
            }

            Timing.CallDelayed(3.0f, () =>
            {
                Cassie.Message("3 . 2 . 1");
            });

            ChristmasZoneTeamManager.Singleton.BroadcastTeamCount(team_a, team_b);
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted || new_role == RoleTypeId.Filmmaker || new_role == RoleTypeId.Overwatch || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Spectator)
                return true;

            if (team_a.Contains(player.PlayerId))
            {
                if (new_role != team_a_role)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(team_a_role);
                    });
                    return false;
                }
            }
            else if (team_b.Contains(player.PlayerId))
            {
                if (new_role != team_b_role)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(team_b_role);
                    });
                    return false;
                }
            }
            else
            {
                if (new_role != RoleTypeId.Spectator)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.Spectator);
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

            if (team_a.Contains(player.PlayerId))
            {
                if (role == team_a_role)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        SetLoadout(player);
                        Teleport.Room(player, team_a_room);
                    });
                    Timing.CallDelayed(7.0f, () =>
                    {
                        player.EffectsManager.EnableEffect<Scanned>(10);
                    });
                }
            }
            else if (team_b.Contains(player.PlayerId))
            {
                if (role == team_b_role)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        SetLoadout(player);
                        Teleport.Room(player, team_b_room);
                    });
                    Timing.CallDelayed(7.0f, () =>
                    {
                        player.EffectsManager.EnableEffect<Scanned>(10);
                    });
                }
            }
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        bool OnRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerEscape)]
        bool OnPlayerEscape(Player player, RoleTypeId role)
        {
            return false;
        }

        [PluginEvent(ServerEventType.Scp914Activate)]
        bool OnScp914Activate(Player player, Scp914KnobSetting knob_setting)
        {
            return false;
        }

        private void SetLoadout(Player player)
        {
            player.ClearInventory();
            player.AddItem(ItemType.KeycardNTFLieutenant);
            player.AddItem(ItemType.ArmorCombat);
            AddFirearm(player, ItemType.GunCrossvec, true);
            player.AddItem(ItemType.Medkit);
            player.AddItem(ItemType.Painkillers);
            player.EffectsManager.EnableEffect<Ensnared>(10);
        }
    }

    public class ChristmasZonePvPEvent : IEvent
    {
        public static ChristmasZonePvPEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Christmas Zone PvP";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "CZP";
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
            EventHandler.Start();
            Log.Info(EventName + " event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            EventHandler.Stop();
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Christmas Zone PvP", "1.0.0", "scientists vs class-d in a random zone with a random weapon", "The Riptide")]
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
