using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using InventorySystem.Items.Jailbird;
using MapGeneration;
using MEC;
using Mirror;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using slocLoader;
using slocLoader.Objects;
using UnityEngine;
using PlayerRoles;
using CustomPlayerEffects;
using Respawning;
using PlayerStatsSystem;
using InventorySystem.Items.Firearms;
using AdminToys;
using NWAPIPermissionSystem;
using PluginAPI.Events;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        public string Description { get; set; } = "Class-D vs NTF in a special Jailbird arena. Jailbirds get an extended charge duration, up to 3 seconds and never wearout. Everyone starts with {hp} HP. Random chance for everyone to get a Particle Disruptor or a MicroHID. Theres a chance the lights will be very dim. Last team alive wins!\n\n";

        public float SpawnHealth { get; set; } = 1000.0f;
        public float DimLightChance { get; set; } = 0.5f;
        public float DimLightIntensity { get; set; } = 1.0f;

        [Description("weighted chance to only get the Jailbird")]
        public float NoneWeight { get; set; } = 1.0f;
        [Description("weighted chance to get the Particle Disruptor")]
        public float ParticleDisruptorWeight { get; set; } = 1.0f;
        [Description("weighted chance to get the Micro Hid")]
        public float MicroHidWeight { get; set; } = 1.0f;

    }

    enum ExtraWeapon { None, ParticleDisruptor, MicroHid };

    public class EventHandler
    {
        private static Config config;
        private static Vector3 spawn_a = new Vector3(20.71f, 968.19f, -12.16f);
        private static Vector3 spawn_b = new Vector3(-21.26f, 968.19f, -48.57f);

        private static HashSet<int> team_a = new HashSet<int>();
        private static HashSet<int> team_b = new HashSet<int>();
        private static ExtraWeapon extra;
        private static bool lights_out;

        private static List<LightSourceToy> lights = new List<LightSourceToy>();

        private static bool old_ff;
        private static bool old_uq;

        public static void Start(Config config)
        {
            ChristmasZoneTeamManager.Singleton.RandomizeTeamsAssignment();
            EventHandler.config = config;
            old_uq = UltraQuaternion.Enabled;
            UltraQuaternion.Enable();
            old_ff = Server.FriendlyFire;
            Server.FriendlyFire = false;
            extra = ExtraWeapon.None;
            float total = config.NoneWeight + config.ParticleDisruptorWeight + config.MicroHidWeight;
            float val = Random.value * total;
            if (val < config.ParticleDisruptorWeight + config.MicroHidWeight)
            {
                extra = ExtraWeapon.MicroHid;
                if (val < config.ParticleDisruptorWeight)
                    extra = ExtraWeapon.ParticleDisruptor;
            }

            lights_out = Random.value < config.DimLightChance;
        }

        public static void Stop()
        {
            if (!old_uq)
                UltraQuaternion.Disable();
            Server.FriendlyFire = old_ff;
            team_a.Clear();
            team_b.Clear();
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            string info = "Event being played: " + ChristmasZoneEnergyArenaEvent.Singleton.EventName + "\n<size=24>" + ChristmasZoneEnergyArenaEvent.Singleton.EventDescription + "</size>";
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
            List<slocGameObject> objects;
            if (slocLoader.AutoObjectLoader.AutomaticObjectLoader.TryGetObjects("JailbirdArena", out objects))
            {
                GameObject map = API.SpawnObjects(objects, new Vector3(-12.0f, 975.0f, -50.0f), Quaternion.Euler(Vector3.zero));
                lights.Clear();
                foreach (var light in map.GetComponentsInChildren<LightSourceToy>())
                    lights.Add(light);
            }
            else
                Log.Error("couldnt load map");

            Timing.CallDelayed(3.0f, () =>
            {
                Cassie.Message("3 . 2 . 1");
            });

            if (lights_out)
            {
                Timing.CallDelayed(3.0f, () =>
                {
                    foreach (var light in lights)
                        light.NetworkLightIntensity = config.DimLightIntensity;
                });
            }

            ChristmasZoneTeamManager.Singleton.BroadcastTeamCount(team_a, team_b);
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted ||
                new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Overwatch || new_role == RoleTypeId.Filmmaker)
                return true;

            if (team_a.Contains(player.PlayerId))
            {
                if (new_role != RoleTypeId.ClassD)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.ClassD);
                    });
                    return false;
                }
            }
            else if (team_b.Contains(player.PlayerId))
            {
                if (new_role != RoleTypeId.NtfSpecialist)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.NtfSpecialist);
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
            if (team_a.Contains(player.PlayerId))
            {
                if (role == RoleTypeId.ClassD)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        SetLoadout(player);
                        player.Position = spawn_a;
                        player.EffectsManager.EnableEffect<Ensnared>(10);
                        player.Health = 1000.0f;
                    });
                    Timing.CallDelayed(7.0f, () =>
                    {
                        player.EffectsManager.EnableEffect<Scanned>(10);
                    });
                }
            }
            else if (team_b.Contains(player.PlayerId))
            {
                if (role == RoleTypeId.NtfSpecialist)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        SetLoadout(player);
                        player.Position = spawn_b;
                        player.EffectsManager.EnableEffect<Ensnared>(10);
                        player.Health = 1000.0f;
                    });
                    Timing.CallDelayed(7.0f, () =>
                    {
                        player.EffectsManager.EnableEffect<Scanned>(10);
                    });
                }
            }
        }

        [PluginEvent(ServerEventType.PlayerDamage)]
        void OnPlayerDamage(Player player, Player attacker, DamageHandlerBase damage_handler)
        {
            if (damage_handler is JailbirdDamageHandler || damage_handler is ExplosionDamageHandler)
                return;

            if (damage_handler is UniversalDamageHandler udh)
                udh.Damage = 0.0f;
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        bool OnRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerChangeItem)]
        void OnPlayerChangesItem(Player player, ushort old_item, ushort new_item)
        {
            if (player.ReferenceHub.inventory.UserInventory.Items.ContainsKey(new_item))
            {
                if (player.ReferenceHub.inventory.UserInventory.Items[new_item] is JailbirdItem jailbird)
                {
                    jailbird._chargeDuration = 3.0f;
                    jailbird.TotalChargesPerformed = -1000;
                }
            }
        }

        [PluginEvent(ServerEventType.PlayerReceiveEffect)]
        bool OnScp914Activate(PlayerReceiveEffectEvent arg)
        {
            if (arg.Effect is Flashed)
                return false;
            return true;
        }

        private void SetLoadout(Player player)
        {
            player.ClearInventory();
            player.AddItem(ItemType.Jailbird);
            if (extra == ExtraWeapon.ParticleDisruptor)
            {
                ParticleDisruptor pd = player.AddItem(ItemType.ParticleDisruptor) as ParticleDisruptor;
                pd.Status = new FirearmStatus(5, pd.Status.Flags, pd.Status.Attachments);
            }
            else if (extra == ExtraWeapon.MicroHid)
                player.AddItem(ItemType.MicroHID);
        }
    }

    public class ChristmasZoneEnergyArenaEvent : IEvent
    {
        public static ChristmasZoneEnergyArenaEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Christmas Zone Energy Arena";
        public string EvenAuthor { get; } = "The Riptide (map by zInitial)";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description.Replace("{hp}", EventConfig.SpawnHealth.ToString("0")); }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "CZEA";
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

        [PluginEntryPoint("Christmas Zone Energy Arena", "1.0.0", "[DATA EXPUNGED]", "The Riptide")]
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
