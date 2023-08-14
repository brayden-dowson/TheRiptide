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
using MapGeneration;
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

    public interface ISnapshot
    {
        object Load();
        void Save(object obj);
    }

    public static class Snapshot
    {
        private static Dictionary<object, ISnapshot> object_snapshots = new Dictionary<object, ISnapshot>();
        private static Dictionary<object, ISnapshot> snapshot_objects = new Dictionary<object, ISnapshot>();

        public static ISnapshot GetSnapshot<T>(T value)
        {
            if (object_snapshots.ContainsKey(value))
                return object_snapshots[value];
            ISnapshot snapshot = null;
            if (value is ItemBase item)
                snapshot = ItemSnapshot.CreateSnapshot(item);

            object_snapshots.Add(value, snapshot);
            return snapshot;
        }

        public static T GetValue<T>(ISnapshot snapshot)
        {
            if (snapshot_objects.ContainsKey(snapshot))
                return snapshot_objects[snapshot] as T;
            T value = null;
        }

        public static void BeginSave()
        {

        }

        public static void EndSave()
        {

        }

        public static void BeginLoad()
        {

        }

        public static void EndLoad()
        {

        }
    }

    public interface IPlayerSnapshot
    {
        void Load(Player player);
        void Save(Player player);
    }

    public class KeyFrameSnapshot
    {
        private float time;
        private float value;
        private float in_tangent;
        private float out_tangent;
        private float in_weight;
        private float out_weight;
        private WeightedMode mode;

        public KeyFrameSnapshot(Keyframe keyframe)
        {
            Save(keyframe);
        }

        public Keyframe Load()
        {

        }

        public void Save(Keyframe keyframe)
        {
            time = keyframe.time;
            value = keyframe.value;
            in_tangent = keyframe.inTangent;
            out_tangent = keyframe.outTangent;
            in_weight = keyframe.inWeight;
            out_weight = keyframe.outWeight;
            mode = keyframe.weightedMode;
        }
    }

    public class AnimationCurveSnapshot
    {
        private int length;
        private KeyFrameSnapshot[] keys;
        private WrapMode pre_wrap_mode;
        private WrapMode post_wrap_mode;

        public AnimationCurveSnapshot(AnimationCurve curve)
        {
            Save(curve);
        }

        public AnimationCurve Load()
        {

        }

        public void Save(AnimationCurve curve)
        {
            length = curve.length;
            keys = curve.keys.ToList().ConvertAll(x => new KeyFrameSnapshot(x)).ToArray();
            pre_wrap_mode = curve.preWrapMode;
            post_wrap_mode = curve.postWrapMode;
        }
    }

    public class RegenerationProcessSnapshot
    {
        private AnimationCurveSnapshot curve;
        private float speed;
        private float hp;
        private float heal;
        private float elapsed;

        public RegenerationProcessSnapshot(RegenerationProcess process)
        {
            Save(process);
        }

        public RegenerationProcess Load()
        {
            var process = new RegenerationProcess(curve.Load(), speed, hp);
            process._healValue = heal;
            process._elapsed = elapsed;
            return process;
        }

        public void Save(RegenerationProcess process)
        {
            curve = new AnimationCurveSnapshot(process._regenCurve);
            speed = process._speedMultip;
            hp = process._hpMultip / process._speedMultip;
            heal = process._healValue;
            elapsed = process._elapsed;
        }

    }

    public class PlayerHandlerSnapshot
    {
        private UsableSnapshot currently_used_item = null;
        private float currently_used_start_time = 0.0f;
        private List<RegenerationProcessSnapshot> regeneration_processes = new List<RegenerationProcessSnapshot>();
        private Dictionary<ItemType, float> item_cooldowns = new Dictionary<ItemType, float>();

        public PlayerHandlerSnapshot(PlayerHandler handler)
        {
            Save(handler);
        }

        //public PlayerHandler Load()
        //{
        //    var handler = new PlayerHandler();
        //    handler.CurrentUsable = new CurrentlyUsedItem();
        //    if(currently_used_item != null)
        //        handler.CurrentUsable.Item = currently_used_item.l
        //    return handler;
        //}

        public void Save(PlayerHandler handler)
        {
            regeneration_processes.Clear();
            item_cooldowns.Clear();
            currently_used_item = handler.CurrentUsable.Item != null ? new UsableSnapshot(handler.CurrentUsable.Item) : null;
            currently_used_start_time = handler.CurrentUsable.StartTime;
            regeneration_processes = handler.ActiveRegenerations.ConvertAll(x => new RegenerationProcessSnapshot(x));
            item_cooldowns = handler.PersonalCooldowns.ToDictionary(x => x.Key, x => x.Value);
        }
    }

    public class UsableItemsControllerSnapshot
    {
        private Dictionary<int, PlayerHandlerSnapshot> player_handlers = new Dictionary<int, PlayerHandlerSnapshot>();
        private Dictionary<ushort, float> item_cooldowns = new Dictionary<ushort, float>();

        public void Load()
        {
            
        }

        public void Save()
        {
            player_handlers.Clear();
            item_cooldowns.Clear();
            player_handlers = UsableItemsController.Handlers.ToDictionary(x => x.Key.PlayerId, x => new PlayerHandlerSnapshot(x.Value));
            item_cooldowns = UsableItemsController.GlobalItemCooldowns.ToDictionary(x => x.Key, x => x.Value);
        }
    }

    public class HumanSnapshot : IPlayerSnapshot
    {
        public RoleTypeId role;
        public Vector3 position;
        public Vector3 rotation;
        public List<IItemSnapshot> items = new List<IItemSnapshot>();
        public Dictionary<ItemType, ushort> ammo = new Dictionary<ItemType, ushort>();
        public ushort current_item;
        public List<KeyValuePair<byte, float>> effects;
        public List<float> stats;

        public HumanSnapshot(Player player)
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
        public Dictionary<int, IPlayerSnapshot> player_save = new Dictionary<int, IPlayerSnapshot>();

        public void Save()
        {
            player_save.Clear();
            foreach(var p in ReadyPlayers())
                player_save.Add(p.PlayerId, new HumanSnapshot(p));
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
