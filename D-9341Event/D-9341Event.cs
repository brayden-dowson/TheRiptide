using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using CommandSystem;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Flashlight;
using InventorySystem.Items.Jailbird;
using InventorySystem.Items.MicroHID;
using InventorySystem.Items.Radio;
using InventorySystem.Items.ThrowableProjectiles;
using InventorySystem.Items.Usables;
using InventorySystem.Items.Usables.Scp1576;
using InventorySystem.Items.Usables.Scp244;
using InventorySystem.Items.Usables.Scp330;
using MEC;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils.Networking;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        public string Description { get; set; } = "Todo\n\n";
    }

    public interface IPlayerSave
    {
        void Load(Player player);
        void Save(Player player);
    }

    public class HumanSave : IPlayerSave
    {
        public RoleTypeId role;
        public Vector3 position;
        public Vector3 rotation;
        public List<IItemSnapshot> items = new List<IItemSnapshot>();
        public Dictionary<ItemType, ushort> ammo = new Dictionary<ItemType, ushort>();
        public ushort current_item;
        public List<KeyValuePair<byte, float>> effects;
        public List<float> stats;

        public HumanSave(Player player)
        {
            Save(player);
        }

        public void Load(Player player)
        {
            if (player.Role != role)
                player.SetRole(role);
            Timing.CallDelayed(0.0f, () =>
            {
                player.ReferenceHub.TryOverridePosition(position, new Vector3(0.0f, rotation.y - player.GameObject.transform.rotation.eulerAngles.y, 0.0f));
                player.ClearInventory();
                foreach (var item in items)
                    item.Load(player);
                player.ReferenceHub.inventory.ServerSendItems();
                ItemBase held;
                if (current_item != 0 && player.ReferenceHub.inventory.UserInventory.Items.TryGetValue(current_item, out held))
                    player.CurrentItem = held;
                player.ReferenceHub.inventory.UserInventory.ReserveAmmo = ammo.ToDictionary(x => x.Key, x => x.Value);
                player.ReferenceHub.inventory.ServerSendAmmo();
                int i = 0;
                foreach(var e in player.ReferenceHub.playerEffectsController.AllEffects)
                {
                    e.ServerSetState(effects[i].Key, effects[i].Value);
                    i++;
                }
                i = 0;
                foreach(var s in player.ReferenceHub.playerStats.StatModules)
                {
                    s.CurValue = stats[i];
                    i++;
                }
            });
        }

        public void Save(Player player)
        {
            role = player.Role;
            position = player.Position;
            rotation = player.ReferenceHub.PlayerCameraReference.rotation.eulerAngles;
            items.Clear();
            foreach (var item in player.ReferenceHub.inventory.UserInventory.Items.Values)
                items.Add(ItemSnapshot.CreateSnapshot(item));
            ammo = player.ReferenceHub.inventory.UserInventory.ReserveAmmo.ToDictionary(x => x.Key, x => x.Value);
            current_item = player.CurrentItem == null ? (ushort)0 : player.CurrentItem.ItemSerial;
            effects = player.ReferenceHub.playerEffectsController.AllEffects.ToList().ConvertAll(x => new KeyValuePair<byte, float>(x.Intensity, x.TimeLeft));
            stats = player.ReferenceHub.playerStats.StatModules.ToList().ConvertAll(x => x.CurValue);
        }
    }

    public class WorldSave
    {
        public Dictionary<int, IPlayerSave> player_save = new Dictionary<int, IPlayerSave>();

        public void Save()
        {
            player_save.Clear();
            foreach(var p in ReadyPlayers())
                player_save.Add(p.PlayerId, new HumanSave(p));
        }

        public void Load()
        {
            foreach (var p in player_save)
            {
                Player player = Player.Get(p.Key);
                if (player == null)
                    continue;
                p.Value.Load(player);
            }
        }
    }

    public class EventHandler
    {
        public int d9341;

        public static WorldSave world_save = new WorldSave();

        public static void Start()
        {

        }

        public static void Stop()
        {

        }
    }

    public class D_9341Event:IEvent
    {
        public static D_9341Event Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "D-9341";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "D9341";
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

        [PluginEntryPoint("Peanut Dodgeball Event", "1.0.0", "Everyone spawns in 173s room with 173 the last one alive wins", "The Riptide")]
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

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class Save : ICommand
    {
        public string Command { get; } = "save";

        public string[] Aliases { get; } = new string[] { };

        public string Description { get; } = "save";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (Player.TryGet(sender, out player))
            {
                EventHandler.world_save.Save();
                response = "success";
                return true;
            }
            response = "failed";
            return false;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class Load : ICommand
    {
        public string Command { get; } = "load";

        public string[] Aliases { get; } = new string[] { };

        public string Description { get; } = "load";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player;
            if (Player.TryGet(sender, out player))
            {
                EventHandler.world_save.Load();
                response = "success";
                return true;
            }
            response = "failed";
            return false;
        }
    }
}
