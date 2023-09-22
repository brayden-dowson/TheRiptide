using Hints;
using InventorySystem.Items;
using InventorySystem.Items.Coin;
using MapGeneration;
using MEC;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public struct RoomInfo
    {
        public FacilityZone Zone { get; set; }
        public RoomName Name { get; set; }
        public RoomShape Shape { get; set; }
    }

    public class VendingSpawnPoint
    {
        public List<string> VendingMachines { get; set; } = new List<string>();
        public float X { get; set; } = 0.0f;
        public float Y { get; set; } = 0.0f;
        public float Z { get; set; } = 0.0f;
        public float Rotation { get; set; } = 0.0f;
    }

    public class ChamberInfo
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
    }

    public class ItemInfo
    {
        public int Price { get; set; }
        public float RotX { get; set; } = 0.0f;
        public float RotY { get; set; } = 0.0f;
        public float RotZ { get; set; } = 0.0f;
        public float Scale { get; set; } = 1.0f;
        public List<ChamberInfo> Chambers { get; set; } = new List<ChamberInfo>();
    }

    public class VendingMachineInfo
    {
        public float SpawnChance { get; set; } = 1.0f;
        public Dictionary<ItemType, ItemInfo> Items { get; set; } = new Dictionary<ItemType, ItemInfo>();
    }

    public class Config
    {
        public float HintDisplayDistance { get; set; } = 2.0f;
        public string HintDisplayPrefix { get; set; } = "<color=#000000><size=24><line-height=75%><b>";
        public string HintDisplayHint { get; set; } = "Hold a coin out and click a button to use";
        public string HintInsufficientFunds { get; set; } = "<color=#FF0000>Insufficient Funds</color>";
        public string HintNoStock { get; set; } = "<color=#FF0000>Out of items for this type</color>";
        public string HintBought { get; set; } = "<color=#00FF00>You bought {item} for {price}</color>";

        public Dictionary<string, VendingMachineInfo> VendingMachines { get; set; } = new Dictionary<string, VendingMachineInfo>
        {
            //{ 
            //    "light",
            //    new VendingMachineInfo
            //    {
            //        SpawnChance = 1.0f,
            //        Items = new Dictionary<ItemType, ItemInfo>
            //        {
            //            {ItemType.me }
            //        }
            //    }
            //}

            {
                "type_a",
                new VendingMachineInfo
                {
                    SpawnChance = 1.0f,
                    Items = new Dictionary<ItemType, ItemInfo>
                    {
                        { ItemType.Medkit, new ItemInfo{ Price = 1, RotY = 90.0f, RotZ = 90.0f, Chambers = new List<ChamberInfo>{ new ChamberInfo { X = 0, Y = 0, Min = 0, Max = 3 }, new ChamberInfo { X = 0, Y = 1, Min = 0, Max = 3 }, new ChamberInfo { X = 0, Y = 2, Min = 0, Max = 3 } } } },
                        { ItemType.Painkillers, new ItemInfo{ Price = 1, RotX = -90.0f, Chambers = new List<ChamberInfo>{ new ChamberInfo { X = 1, Y = 0, Min = 0, Max = 3 }, new ChamberInfo { X = 1, Y = 1, Min = 0, Max = 3 }, new ChamberInfo { X = 1, Y = 2, Min = 0, Max = 3 } } } },
                        { ItemType.Adrenaline, new ItemInfo{ Price = 1, RotY = -90.0f, Chambers = new List<ChamberInfo>{ new ChamberInfo { X = 2, Y = 0, Min = 0, Max = 3 }, new ChamberInfo { X = 2, Y = 1, Min = 0, Max = 3 } } } },
                        { ItemType.SCP207, new ItemInfo{ Price = 8, RotX = -90.0f, Chambers = new List<ChamberInfo>{ new ChamberInfo { X = 3, Y = 0, Min = 0, Max = 3 }, new ChamberInfo { X = 3, Y = 1, Min = 0, Max = 3 } } } },
                        { ItemType.GrenadeFlash, new ItemInfo{ Price = 1, Chambers = new List<ChamberInfo>{ new ChamberInfo { X = 0, Y = 3, Min = 0, Max = 3 }, new ChamberInfo { X = 1, Y = 3, Min = 0, Max = 3 } } } },
                        { ItemType.GrenadeHE, new ItemInfo{ Price = 2, Chambers = new List<ChamberInfo>{ new ChamberInfo { X = 2, Y = 2, Min = 0, Max = 3 }, new ChamberInfo { X = 3, Y = 2, Min = 0, Max = 3 } } } },
                        { ItemType.KeycardMTFOperative, new ItemInfo{ Price = 1,RotX = -110.0f, RotY = 180.0f, Chambers = new List<ChamberInfo>{ new ChamberInfo { X = 2, Y = 3, Min = 0, Max = 3 } } } },
                        { ItemType.KeycardMTFCaptain, new ItemInfo{ Price = 3, RotX = -110.0f, RotY = 180.0f, Chambers = new List<ChamberInfo>{ new ChamberInfo { X = 3, Y = 3, Min = 0, Max = 3 } } } }
                    }
                }
            }
        };

        public Dictionary<RoomInfo, List<VendingSpawnPoint>> RoomSpawnPoints { get; set; } = new Dictionary<RoomInfo, List<VendingSpawnPoint>>//-3.273, 0.960, 1.859
        {
            { new RoomInfo { Zone = FacilityZone.Surface, Name = RoomName.Outside, Shape = RoomShape.Undefined }, new List<VendingSpawnPoint> { new VendingSpawnPoint{ VendingMachines = new List<string>{ "type_a" }, X = -3.273f, Y = 0.05f, Z = 2.45f, Rotation = 0.0f } } }
            //{ new RoomInfo { Zone = FacilityZone.HeavyContainment, Name = RoomName.Unnamed, Shape = RoomShape.Curve }, new VendingSpawnPoint{ } }
        };

        //public Dictionary<ItemType, ItemInfo> Items { get; set; } = new Dictionary<ItemType, ItemInfo>
        //{
        //    { ItemType.Medkit, new ItemInfo{ Price = 1, RotY = 90.0f, RotZ = 90.0f, Chambers = new List<ItemInfo.Position>{ new ItemInfo.Position {X = 0, Y = 0, Amount = 3 }, new ItemInfo.Position { X = 0, Y = 1, Amount = 3 }, new ItemInfo.Position { X = 0, Y = 2, Amount = 3 } } } },
        //    //{ ItemType.GunLogicer, new ItemInfo{ Price = 1, Scale = 0.95f, RotZ = 10.0f, RotY = 180.0f, Chambers = new List<ItemInfo.Position>{ new ItemInfo.Position {X = 0, Y = 0, Amount = 3 }, new ItemInfo.Position { X = 0, Y = 1, Amount = 3 }, new ItemInfo.Position { X = 0, Y = 2, Amount = 3 } } } },
        //    //{ ItemType.MicroHID, new ItemInfo{ Price = 1, Scale = 0.8f, RotX = -90.0f, RotY = 180.0f, RotZ = -90.0f, Chambers = new List<ItemInfo.Position>{ new ItemInfo.Position {X = 0, Y = 0, Amount = 3 }, new ItemInfo.Position { X = 0, Y = 1, Amount = 3 }, new ItemInfo.Position { X = 0, Y = 2, Amount = 3 } } } },
        //    //{ ItemType.Jailbird, new ItemInfo{ Price = 1, RotZ = -90.0f, RotY = 90.0f, Chambers = new List<ItemInfo.Position>{ new ItemInfo.Position {X = 0, Y = 0, Amount = 3 }, new ItemInfo.Position { X = 0, Y = 1, Amount = 3 }, new ItemInfo.Position { X = 0, Y = 2, Amount = 3 } } } },
        //    { ItemType.Painkillers, new ItemInfo{ Price = 1, RotX = -90.0f, Chambers = new List<ItemInfo.Position>{ new ItemInfo.Position {X = 1, Y = 0, Amount = 3 }, new ItemInfo.Position { X = 1, Y = 1, Amount = 3 }, new ItemInfo.Position { X = 1, Y = 2, Amount = 3 } } } },
        //    { ItemType.Adrenaline, new ItemInfo{ Price = 1, RotY = -90.0f, Chambers = new List<ItemInfo.Position>{ new ItemInfo.Position {X = 2, Y = 0, Amount = 3 }, new ItemInfo.Position { X = 2, Y = 1, Amount = 3 } } } },
        //    { ItemType.SCP207, new ItemInfo{ Price = 8, RotX = -90.0f, Chambers = new List<ItemInfo.Position>{ new ItemInfo.Position {X = 3, Y = 0, Amount = 3 }, new ItemInfo.Position { X = 3, Y = 1, Amount = 3 } } } },
        //    { ItemType.GrenadeFlash, new ItemInfo{ Price = 1, Chambers = new List<ItemInfo.Position>{ new ItemInfo.Position {X = 0, Y = 3, Amount = 3 }, new ItemInfo.Position { X = 1, Y = 3, Amount = 3 } } } },
        //    { ItemType.GrenadeHE, new ItemInfo{ Price = 2, Chambers = new List<ItemInfo.Position>{ new ItemInfo.Position {X = 2, Y = 2, Amount = 3 }, new ItemInfo.Position { X = 3, Y = 2, Amount = 3 } } } },
        //    { ItemType.KeycardNTFOfficer, new ItemInfo{ Price = 1,RotX = -110.0f, RotY = 180.0f, Chambers = new List<ItemInfo.Position>{ new ItemInfo.Position {X = 2, Y = 3, Amount = 7 } } } },
        //    { ItemType.KeycardNTFLieutenant, new ItemInfo{ Price = 3, RotX = -110.0f, RotY = 180.0f, Chambers = new List<ItemInfo.Position>{ new ItemInfo.Position {X = 3, Y = 3, Amount = 10 } } } }
        //};


    }

    public class EventHandler
    {
        public static EventHandler Singleton { get; private set; }

        [PluginConfig]
        public Config config;

        [PluginConfig("coin.yml")]
        public CoinConfig coin_config;

        private List<VendingMachine> vending_machines = new List<VendingMachine>();
        public static HashSet<int> Dying = new HashSet<int>();

        [PluginEntryPoint("VendingMachine", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
            CoinManager.Enable(coin_config);
            Singleton = this;
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(PlayerJoinedEvent e)
        {
            if (!coin_config.EnableCoinStacking)
                return;

            //if (player_coins.ContainsKey(e.Player.PlayerId))
            //    player_coins[e.Player.PlayerId] = 0;
            //else
            //    player_coins.Add(e.Player.PlayerId, 0);
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(PlayerLeftEvent e)
        {
            if (!coin_config.EnableCoinStacking)
                return;

            //player_coins.Remove(e.Player.PlayerId);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            Dictionary<RoomInfo, List<RoomIdentifier>> valid_rooms = new Dictionary<RoomInfo, List<RoomIdentifier>>();
            foreach (var room in RoomIdentifier.AllRoomIdentifiers)
            {
                RoomInfo key = new RoomInfo { Name = room.Name, Zone = room.Zone, Shape = room.Shape };
                if (!valid_rooms.ContainsKey(key))
                    valid_rooms.Add(key, new List<RoomIdentifier> { room });
                else
                    valid_rooms[key].Add(room);
            }

            //foreach(var vr in valid_rooms)
            //{
            //    Log.Info(vr.Key.Name + " | " + vr.Key.Zone + " | " + vr.Key.Shape + " | " + vr.Value.Count);
            //}

            HashSet<VendingSpawnPoint> used = new HashSet<VendingSpawnPoint>();
            foreach (var vm in config.VendingMachines)
            {
                List<KeyValuePair<RoomInfo, VendingSpawnPoint>> spawn_points = new List<KeyValuePair<RoomInfo, VendingSpawnPoint>>();
                foreach (var rsp in config.RoomSpawnPoints)
                {
                    if (!valid_rooms.ContainsKey(rsp.Key))
                        continue;

                    foreach (var sp in rsp.Value)
                    {
                        if (used.Contains(sp) || !sp.VendingMachines.Contains(vm.Key))
                            continue;
                        spawn_points.Add(new KeyValuePair<RoomInfo, VendingSpawnPoint>(rsp.Key, sp));
                    }
                }

                int count = 0;
                float remaining_chance = vm.Value.SpawnChance;
                while (remaining_chance > 0.0f)
                {
                    if (spawn_points.IsEmpty())
                    {
                        Log.Error("Out of spawn points for vending machine " + vm.Key + ". Spawned " + count + " of this kind");
                        break;
                    }
                    float chance = Mathf.Min(remaining_chance, 1.0f);
                    remaining_chance -= chance;
                    if (Random.value < chance)
                    {
                        KeyValuePair<RoomInfo, VendingSpawnPoint> s = spawn_points.PullRandomItem();
                        vending_machines.Add(new VendingMachine(new Vector3(s.Value.X, s.Value.Y, s.Value.Z), s.Value.Rotation, valid_rooms[s.Key].RandomItem(), vm.Value.Items));
                        used.Add(s.Value);
                        count++;
                    }
                }
            }
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        void OnPlayerChangeRole(PlayerChangeRoleEvent e)
        {
            Dying.Remove(e.Player.PlayerId);

            if (e.ChangeReason != RoleChangeReason.RemoteAdmin && !coin_config.GrantCoinsToRoleOnSpawn.ContainsKey(e.NewRole))
                return;

            Timing.CallDelayed(0.1f, () =>
            {
                if (e.Player.Role != e.NewRole)
                    return;

                for (int i = 0; i < coin_config.GrantCoinsToRoleOnSpawn[e.NewRole]; i++)
                    e.Player.AddItem(ItemType.Coin);


            });
        }

        [PluginEvent(ServerEventType.RoundEnd)]
        void OnRoundEnd(RoundEndEvent e)
        {
            RoundEnd();
        }

        [PluginEvent(ServerEventType.RoundRestart)]
        void OnRoundRestart(RoundRestartEvent e)
        {
            RoundEnd();
        }

        [PluginEvent(ServerEventType.PlayerChangeItem)]
        void OnPlayerChangeItem(PlayerChangeItemEvent e)
        {
            ItemBase item;
            if (e.Player.ReferenceHub.inventory.UserInventory.Items.TryGetValue(e.NewItem, out item))
                if (item.ItemTypeId == ItemType.Coin)
                    CoinManager.DisplayTotalOnEquip(e.Player.ReferenceHub, item);
        }

        [PluginEvent(ServerEventType.PlayerSearchPickup)]
        bool OnPlayerSearchPickup(PlayerSearchPickupEvent e)
        {
            if (e.Item.Info.ItemId == ItemType.Coin)
            {
                int stack_count = 0;
                bool all_full = true;
                foreach (var item in e.Player.ReferenceHub.inventory.UserInventory.Items.Values)
                {
                    if (item is Coin)
                    {
                        CoinStack stack = item.GetComponent<CoinStack>();
                        if (stack != null)
                        {
                            stack_count++;
                            if (stack.Size < coin_config.MaxStackSize)
                                all_full = false;
                        }
                    }
                }

                if (stack_count >= coin_config.CoinStackLimit && all_full)
                {
                    e.Player.ReceiveHint(coin_config.OnReachedCoinStackLimit, HintEffectPresets.FadeInAndOut(0.25f), 3);
                    return false;
                }

                //List<CoinStack> stacks;
                //if (CoinStack.PlayerCoinStacks.TryGetValue(e.Player.ReferenceHub, out stacks) && stacks.Count >= coin_config.CoinStackLimit && stacks.All(c => c.Size == coin_config.MaxStackSize))
                //{
                //    e.Player.ReceiveHint(coin_config.OnReachedCoinStackLimit, HintEffectPresets.FadeInAndOut(0.25f), 3);
                //    return false;
                //}
            }
            return true;
        }

        [PluginEvent(ServerEventType.PlayerDying)]
        void OnPlayerDying(PlayerDyingEvent e)
        {
            if (e.Player == null)
                return;
            Dying.Add(e.Player.PlayerId);
        }

        [PluginEvent(ServerEventType.PlayerPreCoinFlip)]
        PlayerPreCoinFlipCancellationData OnPlayerPreCoinFlip(PlayerPreCoinFlipEvent e)
        {
            Vector3 start = e.Player.ReferenceHub.PlayerCameraReference.position;
            Vector3 dir = e.Player.ReferenceHub.PlayerCameraReference.rotation * Vector3.forward;

            RaycastHit info;
            if (Physics.Raycast(new Ray(start, dir), out info, 2.5f, (1 << 28)))
            {
                if (info.collider is BoxCollider bc)
                {
                    foreach (var vending_machine in vending_machines)
                        if (vending_machine.HitButton(e.Player, bc))
                            return PlayerPreCoinFlipCancellationData.PreventFlip();
                    return PlayerPreCoinFlipCancellationData.PreventFlip();
                }
            }
            return PlayerPreCoinFlipCancellationData.LeaveUnchanged();
        }

        private void RoundEnd()
        {
            foreach (var vm in vending_machines)
                vm.Destroy();
        }
    }
}
