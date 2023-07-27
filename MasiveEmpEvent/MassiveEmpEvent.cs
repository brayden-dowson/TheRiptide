using MEC;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using CustomPlayerEffects;
using InventorySystem.Items;
using InventorySystem.Items.Flashlight;
using InventorySystem.Items.Firearms.Attachments;
using Interactables.Interobjects.DoorUtils;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapGeneration;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments.Components;
using Mirror;
using CedMod.Addons.Events;
using System.ComponentModel;
using PluginAPI.Core.Items;
using UnityEngine;
using InventorySystem.Items.Radio;
using Interactables.Interobjects;
using CedMod.Addons.Events.Interfaces;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        [Description("light malfunctions")]
        public float MinLightTime { get; set; } = 5.0f;
        public float MaxLightTime { get; set; } = 30.0f;
        [Description("chance lights will malfunction every second")]
        public float LightChance { get; set; } = 17.5f;

        [Description("item malfunctions i.e. gun attachment flashlights, radios, flashlights")]
        public float MinItemTime { get; set; } = 0.0f;
        public float MaxItemTime { get; set; } = 15.0f;
        [Description("chance items will malfunction every second")]
        public float ItemChance { get; set; } = 20.0f;

        [Description("door malfunctions")]
        public float MinDoorTime { get; set; } = 1.0f;
        public float MaxDoorTime { get; set; } = 7.5f;
        [Description("chance doors will malfunction every second")]
        public float DoorChance { get; set; } = 4.0f;
        [Description("chance weight given to each door malfunction type")]
        public float OpenWeight = 45.0f;
        public float LockWeight = 45.0f;
        public float CloseWeight = 10.0f;

        [Description("min time between random cassie malfunctions")]
        public int CassieMinTime { get; set; } = 0;
        [Description("max time between random cassie malfunctions")]
        public int CassieMaxTime { get; set; } = 300;
        [Description("max single random cassie malfunction")]
        public int CassieMaxLength { get; set; } = 7;
    }


    public class EventHandler
    {
        public Config config = null;

        public EventHandler()
        {
            config = MassiveEmpEvent.Singleton.EventConfig;
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            Timing.CallDelayed(3.0f, () =>
            {
                Cassie.Message(".G6 .G5 .G4 pitch_0.95 .G1 .G6 .G2 pitch_0.82 .G6 .G1 pitch_0.72 .G2 jam_050_2 error .G4 .G6 pitch_0.40 .G3 pitch_0.60 jam_030_3 system pitch_0.30 .G1 pitch_0.30 .G3 pitch_0.50 jam_020_4 malfunction pitch_0.30 .G1 pitch_0.20 .G3 .G5 pitch_0.10 .G4 .G6 pitch_0.05 .G1 .G4 pitch_0.02 .G4 . pitch_1.00");

                MassiveEmpEvent.Singleton.light_malfunctions = Timing.RunCoroutine(_LightMalfunctions());
                MassiveEmpEvent.Singleton.item_malfunctions = Timing.RunCoroutine(_ItemMalfunctions());
                MassiveEmpEvent.Singleton.door_malfunctions = Timing.RunCoroutine(_DoorMalfunctions());
                MassiveEmpEvent.Singleton.cassie_malfuncions = Timing.RunCoroutine(_CassieMalfunctions());
            });
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + MassiveEmpEvent.Singleton.EventName + "\n<size=26>" + MassiveEmpEvent.Singleton.EventDescription + "</size>", 60, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            if (role.GetTeam() == Team.SCPs && role != RoleTypeId.Scp0492)
            {
                Timing.CallDelayed(1.0f, () =>
                {
                    player.EffectsManager.ChangeState<Disabled>(1, 0);
                });
            }
            else if (role == RoleTypeId.ClassD || role == RoleTypeId.Scientist)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    player.AddItem(ItemType.Flashlight);
                });
            }
        }

        public HashSet<ItemBase> malfunctioning_items = new HashSet<ItemBase>();
        [PluginEvent(ServerEventType.PlayerChangeItem)]
        void OnPlayerChangedItem(Player player, ushort oldItem, ushort newItem)
        {
            if (player != null && player.ReferenceHub.inventory.UserInventory.Items.ContainsKey(newItem))
            {
                ItemBase item = player.ReferenceHub.inventory.UserInventory.Items[newItem];
                if (item != null)
                {
                    if (malfunctioning_items.Contains(item))
                    {
                        if (item is Firearm gun)
                        {
                            SetFirearmLightState(gun, false);
                        }
                        else if (item is FlashlightItem flashlight)
                        {
                            SetFlashlightState(flashlight, false);
                        }
                        else if (item is RadioItem radio)
                        {
                            SetRadioState(radio, false);
                        }
                    }
                }
            }
        }

        [PluginEvent(ServerEventType.PlayerToggleFlashlight)]
        bool OnToggleFlashlight(Player player, ItemBase item, bool isToggled)
        {
            if(malfunctioning_items.Contains(item))
            {
                if (item != null && item is FlashlightItem flashlight)
                    SetFlashlightState(flashlight, false);
                player.SendBroadcast("item seems to be malfunctioning", 3);
                return false;
            }
            return true;
        }

        public IEnumerator<float> _LightMalfunctions()
        {
            HashSet<RoomIdentifier> malfunctioning_rooms = new HashSet<RoomIdentifier>();
            while (true)
            {
                foreach(var room in RoomIdentifier.AllRoomIdentifiers.Except(malfunctioning_rooms))
                {
                    if(Random.value * 100.0f < config.LightChance)
                    {
                        malfunctioning_rooms.Add(room);
                        FacilityManager.SetRoomLightState(room, false);
                        float x = Random.value;
                        Timing.CallDelayed(config.MinLightTime * (1.0f - x) + config.MaxLightTime * x, ()=>
                        {
                            malfunctioning_rooms.Remove(room);
                            //FacilityManager.SetRoomLightIntensity(room, 1.0f);
                            FacilityManager.SetRoomLightState(room, true);
                        });
                    }
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }


        public IEnumerator<float> _ItemMalfunctions()
        {
            float delta = 1.0f / 20.0f;
            while (true)
            {
                foreach(var p in Player.GetPlayers())
                {
                    if(p.CurrentItem != null)
                    {
                        ItemBase i = p.CurrentItem;
                        if (i.Category == ItemCategory.Firearm && i is Firearm gun && gun.IsEmittingLight && Random.value * 100.0f < config.ItemChance * delta)
                        {
                            malfunctioning_items.Add(i);
                            SetFirearmLightState(gun, false);
                            Timing.CallDelayed(Random.Range(config.MinItemTime, config.MaxItemTime), () =>
                            {
                                malfunctioning_items.Remove(i);
                                SetFirearmLightState(gun, true);
                            });
                        }
                        else if (i.ItemTypeId == ItemType.Flashlight && i is FlashlightItem flashlight && flashlight.IsEmittingLight && Random.value * 100.0f < config.ItemChance * delta)
                        {
                            malfunctioning_items.Add(i);
                            SetFlashlightState(flashlight, false);
                            Timing.CallDelayed(Random.Range(config.MinItemTime, config.MaxItemTime), () =>
                            {
                                malfunctioning_items.Remove(i);
                                SetFlashlightState(flashlight, true);
                            });
                        }
                        else if (i.ItemTypeId == ItemType.Radio && i is RadioItem radio && Random.value * 100.0f < config.ItemChance * delta)
                        {
                            malfunctioning_items.Add(i);
                            SetRadioState(radio, false);
                            Timing.CallDelayed(Random.Range(config.MinItemTime, config.MaxItemTime), () =>
                            {
                                malfunctioning_items.Remove(i);
                                SetRadioState(radio, true);
                            });
                        }
                    }
                }
                yield return Timing.WaitForSeconds(delta);
            }
        }

        private void SetFirearmLightState(Firearm firearm, bool state)
        {
            FirearmStatus s = firearm.Status;
            firearm.Status = new FirearmStatus(s.Ammo, state ? s.Flags | FirearmStatusFlags.FlashlightEnabled : s.Flags & ~FirearmStatusFlags.FlashlightEnabled, s.Attachments);
        }

        private void SetFlashlightState(FlashlightItem flashlight, bool state)
        {
            FlashlightNetworkHandler.FlashlightMessage msg = new FlashlightNetworkHandler.FlashlightMessage(flashlight.ItemSerial, state);
            foreach(var p in Player.GetPlayers())
            {
                NetworkConnection player_connection = p.GameObject.GetComponent<NetworkIdentity>().connectionToClient;
                player_connection.Send(msg);
            }
        }

        private void SetRadioState(RadioItem radio, bool state)
        {
            ClientRadioCommandMessage msg = new ClientRadioCommandMessage(state ? RadioMessages.RadioCommand.Enable : RadioMessages.RadioCommand.Disable);
            NetworkConnection player_connection = radio.Owner.gameObject.GetComponent<NetworkIdentity>().connectionToClient;
            player_connection.Send(msg);
        }

        public IEnumerator<float> _DoorMalfunctions()
        {
            HashSet<DoorVariant> malfunctioning_doors = new HashSet<DoorVariant>();
            while (true)
            {
                foreach (var door in DoorVariant.AllDoors.Except(malfunctioning_doors))
                {
                    if (!(door is ElevatorDoor) && !(door is PryableDoor) && Random.value * 100.0f < config.DoorChance)
                    {
                        float total_weight = config.OpenWeight + config.CloseWeight + config.LockWeight;
                        float open_chance = config.OpenWeight / total_weight;
                        float close_chance = config.CloseWeight / total_weight;
                        float v = Random.value;
                        if (v < open_chance)
                            FacilityManager.OpenDoor(door);
                        else if (v < open_chance + close_chance)
                            FacilityManager.CloseDoor(door);
                        else
                        {
                            malfunctioning_doors.Add(door);
                            FacilityManager.LockDoor(door, DoorLockReason.AdminCommand);
                            Timing.CallDelayed(Random.Range(config.MinDoorTime, config.MaxDoorTime), () =>
                            {
                                malfunctioning_doors.Remove(door);
                                FacilityManager.UnlockDoor(door, DoorLockReason.AdminCommand);
                            });
                        }
                    }
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }

        public IEnumerator<float> _CassieMalfunctions()
        {
            List<string> special_codes = new List<string> { " .G1 .", " .G2 .", " .G3 .", " .G4 .", " .G5 .", " .G6 ." };
            while (true)
            {
                string message = "";
                int repeat = Random.Range(1, config.CassieMaxLength);
                for (int i = 0; i < repeat; i++)
                {
                    message += "pitch_";
                    if (Random.value > 0.5)
                    {
                        message += (Random.value + 0.05).ToString("0.00");
                        message += special_codes.RandomItem();
                    }
                    else
                    {
                        message += (Random.value / 4.0f + 0.03).ToString("0.00");
                        message += " ";
                        message += (char)('a' + Random.Range(0, 26));
                        message += " .";
                    }
                    message += " ";
                }
                bool send_noisy = Random.value > 0.9f;
                Cassie.Message(message, false, send_noisy, false);
                yield return Timing.WaitForSeconds(Random.Range(config.CassieMinTime, config.CassieMaxTime));
            }
        }
    }

    public class MassiveEmpEvent:IEvent
    {
        public static MassiveEmpEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Massive EMP Event";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription { get; set; } = "All electronics are malfunctioning. Facility lights will flicker on and off randomly, flashlights and gun flashlights will malfuction so if you want to always have light, you must stick together. Doors will open, close and lock randomly. All scientist and class-d are given flashlights and SCPs have their speed reduced\n\n";
        public string EventPrefix { get; } = "EMP";
        public bool OverrideWinConditions { get; }
        public bool BulletHolesAllowed { get; set; } = false;
        public PluginHandler PluginHandler { get; }
        public IEventConfig Config => EventConfig;

        [PluginConfig]
        public Config EventConfig;

        public CoroutineHandle light_malfunctions;
        public CoroutineHandle item_malfunctions;
        public CoroutineHandle door_malfunctions;
        public CoroutineHandle cassie_malfuncions;

        public void PrepareEvent()
        {
            Log.Info("Massive EMP Event is preparing");
            IsRunning = true;
            Log.Info("Massive EMP Event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            Timing.KillCoroutines(light_malfunctions);
            Timing.KillCoroutines(item_malfunctions);
            Timing.KillCoroutines(cassie_malfuncions);
            Timing.KillCoroutines(door_malfunctions);
            IsRunning = false;
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Masive EMP Event", "1.0.0", "All electronics damaged", "The Riptide")]
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
