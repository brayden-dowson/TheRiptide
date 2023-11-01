using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using InventorySystem.Items;
using InventorySystem.Items.SwitchableLightSources;
using InventorySystem.Items.SwitchableLightSources.Flashlight;
using MapGeneration;
using MEC;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using Respawning;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using static TheRiptide.EventUtility;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;
        public string Description { get; set; } = "All lights are out and light is locked down. Everyone spawns in light and are given flashlights and have 30 seconds to hide from peanut. Flashlight do not work on peanut.\n\n";
    }


    public class EventHandler
    {
        public Config config = null;
        public static HashSet<int> seekers = new HashSet<int>();
        public static bool found_winner = false;

        public EventHandler()
        {
            config = PeanutHideAndSeek.Singleton.EventConfig;
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            ClearAllItems();
            LockDownLight();
            EndRoom = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone == FacilityZone.Surface).First();
            RoomOffset = new UnityEngine.Vector3(40.000f, 14.080f, -32.600f);

            seekers.Add(Player.GetPlayers().RandomItem().PlayerId);
            Timing.CallDelayed(30.0f, () =>
            {
                FacilityManager.SetAllRoomLightStates(false);
            });
            Timing.CallDelayed(7.0f,()=>
            {
                Cassie.Message("pitch_0.8 10 . . 9 . . 8 . . 7 . . 6 . . 5 . . 4 . . 3 . . 2 . . 1 . . ");
            });
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + PeanutHideAndSeek.Singleton.EventName + "\n<size=32>" + PeanutHideAndSeek.Singleton.EventDescription + "</size>", 30, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            if (!Round.IsRoundStarted)
                return;

            if (seekers.Contains(player.PlayerId))
            {
                seekers.Remove(player.PlayerId);
                List<Player> avaliable = Player.GetPlayers().Where(p => p != player && p.Role == RoleTypeId.Spectator).ToList();
                if(avaliable.Count == 0)
                    avaliable = Player.GetPlayers().Where(p => p != player).ToList();

                Player seeker = avaliable.RandomItem();
                seekers.Add(seeker.PlayerId);
                seeker.SetRole(RoleTypeId.Scp173);
            }    
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        bool OnRespawn(SpawnableTeamType team, List<Player> player, int max)
        {
            List<Player> spectators = Player.GetPlayers().Where(p => p.Role == RoleTypeId.Spectator).ToList();
            if (spectators.Count == 0)
                return false;
            Player seeker = spectators.RandomItem();
            seekers.Add(seeker.PlayerId);
            seeker.SetRole(RoleTypeId.Scp173);
            foreach (var p in Player.GetPlayers())
                p.SendBroadcast("a new seeker has spawned", 10, shouldClearPrevious: true);
            return false;
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted || new_role == RoleTypeId.Filmmaker || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Overwatch)
                return true;

            if (found_winner)
                return HandleGameOverRoleChange(player, new_role);

            int player_id = player.PlayerId;
            if (seekers.Contains(player_id))
            {
                if (new_role != RoleTypeId.Scp173)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        Player p = Player.Get(player_id);
                        if (p != null)
                            p.SetRole(RoleTypeId.Scp173);
                    });
                    return false;
                }
            }
            else
            {
                if (new_role != RoleTypeId.ClassD && new_role != RoleTypeId.Spectator)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        Player p = Player.Get(player_id);
                        if (p != null)
                            p.SetRole(RoleTypeId.ClassD);
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

            if (found_winner)
            {
                HandleGameOverSpawn(player);
                return;
            }

            int player_id = player.PlayerId;
            if (role == RoleTypeId.ClassD)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role != RoleTypeId.ClassD)
                        return;

                    player.AddItem(ItemType.Flashlight);
                    player.SendBroadcast("flashlight granted. check inv.", 3);
                });
            }
            else if(role == RoleTypeId.Scp173)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role != RoleTypeId.Scp173)
                        return;
                    Teleport.RoomPos(player, RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Lcz173), new UnityEngine.Vector3(17.797f, 12.429f, 7.933f));
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerDeath)]
        void OnPlayerDeath(Player victim, Player attacker, DamageHandlerBase damageHandler)
        {
            if (victim != null)
            {
                if (!found_winner)
                {
                    found_winner = WinConditionLastClassD(victim);
                    if(found_winner)
                        FacilityManager.ResetAllRoomLights();
                }
            }
        }

        [PluginEvent(ServerEventType.PlayerChangeItem)]
        void OnPlayerChangedItem(Player player, ushort oldItem, ushort newItem)
        {
            if (player != null && player.ReferenceHub.inventory.UserInventory.Items.ContainsKey(newItem))
            {
                ItemBase item = player.ReferenceHub.inventory.UserInventory.Items[newItem];
                if (item != null)
                    if (item is FlashlightItem flashlight)
                        SetFlashlightState(flashlight, false);
            }
        }

        [PluginEvent(ServerEventType.PlayerToggleFlashlight)]
        bool OnToggleFlashlight(Player player, ItemBase item, bool isToggled)
        {
            if (item != null && item is FlashlightItem flashlight)
                SetFlashlightState(flashlight, false);
            return false;
        }

        private void SetFlashlightState(FlashlightItem flashlight, bool state)
        {
            flashlight.IsEmittingLight = false;
            FlashlightNetworkHandler.FlashlightMessage msg = new FlashlightNetworkHandler.FlashlightMessage(flashlight.ItemSerial, state);
            foreach (var p in Player.GetPlayers())
            {
                if (flashlight.Owner.PlayerId != p.PlayerId)
                {
                    NetworkConnection player_connection = p.GameObject.GetComponent<NetworkIdentity>().connectionToClient;
                    player_connection.Send(msg);
                }
            }
        }

        public static void Start()
        {
            seekers.Clear();
            found_winner = false;
            WinnerReset();
        }

        public static void Stop()
        {
            seekers.Clear();
            found_winner = false;
            WinnerReset();
        }
    }

    public class PeanutHideAndSeek : IEvent
    {
        public static PeanutHideAndSeek Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Peanut Hide and Seek";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "PHAS";
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

        [PluginEntryPoint("Peanut Hide and Seek Event", "1.0.0", "Hide and Seek", "The Riptide")]
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
