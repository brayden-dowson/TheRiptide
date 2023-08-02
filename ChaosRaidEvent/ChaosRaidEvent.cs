using CedMod;
using CedMod.Addons.Events;
using InventorySystem.Items;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PlayerRoles;
using UnityEngine;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PluginAPI.Core.Items;
using static TheRiptide.Utility;
using CustomPlayerEffects;
using MEC;
using Respawning;
using InventorySystem.Items.Pickups;
using MapGeneration;
using Interactables.Interobjects.DoorUtils;
using AdminToys;
using InventorySystem.Items.Usables.Scp244;
using Mirror;
using Footprinting;
using Interactables.Interobjects;
using InventorySystem.Items.Usables;
using InventorySystem.Items.ThrowableProjectiles;
using PlayerStatsSystem;
using CedMod.Addons.Events.Interfaces;
using Scp914;
using LightContainmentZoneDecontamination;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        public string Description { get; set; } = "Half the players spawn as CHAOS the rest as NTF. CHAOS must steal all the items in the facility while NTF must stop them. The CHAOS can let SCPs and Class-D out. CHAOS receive reward items on successfully stealing items. CHAOS win if they steal all the items while NTF win if they survive all the spawn waves and kill all the CHAOS, SCPs and Class-Ds. If the Nuke goes off the winner is decided by who was in the lead stolen/protected\n\n";
    }

    public class EventHandler
    {
        private static HashSet<ItemType> target_items = new HashSet<ItemType>
        {
            ItemType.SCP018,
            ItemType.SCP207,
            ItemType.SCP244a,
            ItemType.SCP244b,
            ItemType.SCP268,
            ItemType.SCP500,
            ItemType.SCP1576,
            ItemType.SCP1853,
            ItemType.SCP2176
        };

        private static CoroutineHandle check_items;
        private static int total_items = 0;
        private static int items_left = 0;

        private static bool is_first_spawn = true;
        private static HashSet<int> chaos = new HashSet<int>();
        private static HashSet<int> mtf = new HashSet<int>();
        private static HashSet<int> classd = new HashSet<int>();
        private static List<RoleTypeId> chaos_roles = new List<RoleTypeId>();
        private static List<RoleTypeId> mtf_roles = new List<RoleTypeId>();

        private static Dictionary<DoorVariant, RoleTypeId> scp_doors = new Dictionary<DoorVariant, RoleTypeId>();

        private static Dictionary<int, LightSourceToy> player_lights = new Dictionary<int, LightSourceToy>();
        private static HashSet<ushort> dropped_lights = new HashSet<ushort>();
        private static CoroutineHandle light_update;
        private static CoroutineHandle nuke;
        private static CoroutineHandle class_d;
        private static List<CoroutineHandle> respawns = new List<CoroutineHandle>();

        private static System.Action<ItemPickupBase> pickup_added;

        public static void Start()
        {
            scp_doors.Clear();
            chaos.Clear();
            mtf.Clear();
            classd.Clear();
            total_items = 0;
            items_left = 0;

            player_lights.Clear();
            dropped_lights.Clear();
            Timing.KillCoroutines(check_items, light_update, nuke, class_d);

            Round.IsLobbyLocked = true;
            Timing.CallDelayed(60.0f, () => Round.IsLobbyLocked = false);

            pickup_added = new System.Action<ItemPickupBase>((item) =>
            {
                try
                {
                    if (target_items.Contains(item.Info.ItemId))
                    {
                        dropped_lights.Add(item.Info.Serial);
                        AddLight(item.transform);
                        RaycastHit info;
                        if (!Physics.Raycast(item.transform.position, Vector3.down, out info, 50.0f, item.gameObject.layer) || info.collider.name == "Killer")
                        {
                            Player player = Player.Get(item.PreviousOwner.Hub);
                            if (player != null)
                                player.Kill("dropped item out of bounds");
                            if (!SafeItemTeleport(item))
                            {
                                dropped_lights.Remove(item.Info.Serial);
                                NetworkServer.Destroy(item.gameObject);
                                Log.Error("item could not be saved. player pos: " + player.Position.ToPreciseString() + " | item pos: " + item.transform.position.ToPreciseString());
                            }
                        }
                        else
                        {
                            FreezeItemAtPosition(item, info.point + (0.1f * Vector3.up));
                        }
                    }
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }

            });
        }

        public static void Stop()
        {
            scp_doors.Clear();
            chaos.Clear();
            mtf.Clear();
            classd.Clear();
            total_items = 0;
            items_left = 0;

            player_lights.Clear();
            dropped_lights.Clear();
            Timing.KillCoroutines(check_items, light_update, nuke, class_d);
            foreach (var r in respawns)
                Timing.KillCoroutines(r);

            typeof(ItemPickupBase).GetEvent("OnPickupAdded").RemoveEventHandler(null, pickup_added);
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + ChaosRaidEvent.Singleton.EventName + "\n<size=22>" + ChaosRaidEvent.Singleton.EventDescription + "</size>", 120, shouldClearPrevious: true);
            if ((chaos.Count * 1) < (mtf.Count * 1))
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
            DecontaminationController.Singleton.NetworkDecontaminationOverride = DecontaminationController.DecontaminationStatus.Disabled;
            Round.IsLocked = true;

            Timing.CallDelayed(1.0f, () =>
            {
                ItemPickupBase[] items = Object.FindObjectsOfType<ItemPickupBase>();
                foreach (var item in items)
                {
                    if (target_items.Contains(item.NetworkInfo.ItemId))
                    {
                        FreezeItemAtPosition(item, item.Position);
                        total_items++;
                    }
                }
                items_left = total_items;
                typeof(ItemPickupBase).GetEvent("OnPickupAdded").AddEventHandler(null, pickup_added);
            });

            check_items = Timing.RunCoroutine(_CheckStolen());

            SetupDoors();
            SetupLights();

            foreach (var door in DoorVariant.DoorsByRoom[RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Hcz079)])
                FacilityManager.UnlockDoor(door, DoorLockReason.SpecialDoorFeature);

            is_first_spawn = true;
            Timing.CallDelayed(30.0f, () => is_first_spawn = false);
            SetSpawnWave();
            int wave_count = 5;
            for (int i = 0; i < wave_count; i++)
            {
                respawns.Add(Timing.CallDelayed(60.0f * (3.5f * (i + 1)), () =>
                {
                    SetSpawnWave();
                    foreach (var p in Player.GetPlayers())
                        if (p.Role == RoleTypeId.Spectator)
                            p.SetRole(RoleTypeId.ClassD);
                }));
            }
            nuke = Timing.CallDelayed(60.0f * (3.5f * (wave_count + 0.5f)), () =>
            {
                Round.IsLocked = false;
                Warhead.IsLocked = false;
                Warhead.LeverStatus = true;
                Warhead.Start();
            });
            class_d = Timing.RunCoroutine(_UpdateClassD());
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        bool OnRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            return false;
        }

        [PluginEvent(ServerEventType.Scp914Activate)]
        bool OnScp914Activate(Player player, Scp914KnobSetting knob_setting)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted ||
                new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Overwatch ||
                (!is_first_spawn && new_role.GetTeam() == Team.SCPs) || classd.Contains(player.PlayerId))
                return true;

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
            else if(chaos.Contains(player.PlayerId) && new_role.GetTeam() != Team.ChaosInsurgency)
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

            if(role.GetTeam() == Team.FoundationForces && role != RoleTypeId.FacilityGuard)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    Timing.CallDelayed(1.5f, () =>
                    {
                        if (player.Role.GetTeam() == Team.FoundationForces)
                            player.SendBroadcast("Objective: protect SCP items from the CHAOS insurgency \n" + (total_items - items_left) + " out of " + total_items + " items stolen", 30, shouldClearPrevious: true);
                    });
                    if (player.Role.GetTeam() != Team.FoundationForces || player.Role == RoleTypeId.FacilityGuard)
                        return;
                    Teleport.RoomPos(player, RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Hcz079), new Vector3(-3.371f, -4.276f, -6.790F));
                });
            }
            else if(role == RoleTypeId.Scp096)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role == RoleTypeId.Scp096)
                        Teleport.RoomPos(player, RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Hcz096), new Vector3(-1.805f, 0.960f, -0.012f));
                });
            }
            else if(role.GetTeam() == Team.ChaosInsurgency)
            {
                Timing.CallDelayed(1.5f, () =>
                {
                    if (player.Role.GetTeam() == Team.ChaosInsurgency)
                        player.SendBroadcast("Objective: steal SCP items from the foundation \nstolen " + (total_items - items_left) + " out of " + total_items + " items", 30, shouldClearPrevious: true);
                });
            }
            else if (role == RoleTypeId.ClassD)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role == RoleTypeId.ClassD)
                        player.SendBroadcast("Objective: escape the facility", 30, shouldClearPrevious: true);
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerChangeItem)]
        void OnPlayerChangesItem(Player player, ushort old_item, ushort new_item)
        {
            ItemBase item;
            if (!player.ReferenceHub.inventory.UserInventory.Items.TryGetValue(new_item, out item))
                return;

            if (!target_items.Contains(item.ItemTypeId))
                return;

            player.CurrentItem = null;
        }

        [PluginEvent(ServerEventType.PlayerCancelUsingItem)]
        bool OnPlayerCancelsUsingItem(Player player, UsableItem item)
        {
            if (target_items.Contains(item.ItemTypeId))
                return false;
            return true;
        }

        [PluginEvent(ServerEventType.PlayerInteractDoor)]
        public bool OnPlayerInteractDoor(Player player, DoorVariant door, bool can_open)
        {
            if (!can_open)
                return true;

            if(door.Rooms.Count() == 1 && door.Rooms.First().Name == RoomName.LczClassDSpawn)
            {
                if (!chaos.Contains(player.PlayerId))
                {
                    player.SendBroadcast("NTF can not let Class-D during a raid", 3, shouldClearPrevious: true);
                    return false;
                }
                List<Player> spectators = Player.GetPlayers().Where(p => p.Role == RoleTypeId.Spectator).ToList();
                //if (spectators.IsEmpty())
                //    spectators = new List<Player> { player };//testing remove
                if (spectators.IsEmpty())
                {
                    player.SendBroadcast("No Class-D in the cells at the moment, come back later.", 3, shouldClearPrevious: true);
                    return false;
                }

                player.SendBroadcast("Class-D freed, take him to surface to increase CHAOS spawn count", 5, shouldClearPrevious: true);
                int count = Random.Range(1, Mathf.Min(5, spectators.Count + 1));
                for (int i = 0; i < count; i++)
                {
                    Player selected = spectators.PullRandomItem();
                    classd.Add(selected.PlayerId);
                    selected.SetRole(RoleTypeId.ClassD);
                    Timing.CallDelayed(0.1f, () => selected.Position = door.transform.TransformPoint(new Vector3(0.0f, 1.0f, -2.0f)));
                }
                door.ServerChangeLock(DoorLockReason.SpecialDoorFeature, true);
            }

            if (scp_doors.ContainsKey(door) && !door.TargetState)
            {
                if (!chaos.Contains(player.PlayerId))
                {
                    door.PermissionsDenied(player.ReferenceHub, 0);
                    player.SendBroadcast("NTF can not let SCPs out of containment", 3, shouldClearPrevious: true);
                    return false;
                }

                if(player.CurrentItem != null && player.CurrentItem.ItemTypeId != ItemType.KeycardContainmentEngineer)
                {
                    door.PermissionsDenied(player.ReferenceHub, 0);
                    player.SendBroadcast("Door permissions have been raised to protect the SCPs from the CHAOS raid.\nYou need a Containment Engineer card to open this door", 3, shouldClearPrevious: true);
                    return false;
                }

                player.RemoveItem(new Item(player.ReferenceHub.inventory.UserInventory.Items.Values.First(i => i.ItemTypeId == ItemType.KeycardContainmentEngineer)));
                player.SendBroadcast("The scanner ate the card!", 15, shouldClearPrevious: true);

                List<Player> players = Player.GetPlayers().Where(p => p.Role == RoleTypeId.Spectator).ToList();
                if (players.IsEmpty())
                    players = Player.GetPlayers().Where(p=>p.Role.GetTeam()!= Team.SCPs).ToList();
                if (players.IsEmpty())
                    players = new List<Player> { player };

                Player scp = players.RandomItem();
                string scp_code = "";
                switch(scp_doors[door])
                {
                    case RoleTypeId.Scp049: scp_code = "0 4 9"; break;
                    case RoleTypeId.Scp106: scp_code = "1 0 6"; break;
                    case RoleTypeId.Scp173: scp_code = "1 7 3"; break;
                    case RoleTypeId.Scp939: scp_code = "9 3 9"; break;
                }
                if (scp.Role.IsHuman())
                    scp.DropEverything();
                scp.SetRole(scp_doors[door]);
                scp_doors.Remove(door);
                scp.SendBroadcast("You have been let out of containment! Kill everyone", 30, shouldClearPrevious: true);
                Cassie.Message("Danger SCP " + scp_code + " has been detected outside its containment chamber . The nearest mobile task force unit must neutralize the threat immediately");

                door.ServerChangeLock(DoorLockReason.SpecialDoorFeature, true);
            }
            return true;
        }

        [PluginEvent(ServerEventType.WarheadDetonation)]
        public void OnWarheadDetonation()
        {
            Timing.CallDelayed(3.0f,()=>
            {
                Round.IsLocked = false;
                if (items_left >= total_items / 2)
                {
                    foreach (var p in Player.GetPlayers())
                    {
                        p.SendBroadcast("[TIE-BREAKER] NTF wins by preventing the CHAOS from getting more than half of their items! " + (total_items - items_left) + " / " + total_items, 60, shouldClearPrevious: true);
                        if (p.Role.GetTeam() != Team.FoundationForces && p.IsAlive)
                            p.Kill("you failed to steal enough items");
                    }
                }
                else
                {
                    foreach (var p in Player.GetPlayers())
                    {
                        p.SendBroadcast("[TIE-BREAKER] CHAOS wins by stealing more than half of the items! " + (total_items - items_left) + " / " + total_items, 60, shouldClearPrevious: true);
                        if (p.Role.GetTeam() != Team.ChaosInsurgency && p.IsAlive)
                            p.Kill("you failed to protect enough items");
                    }
                }
            });
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

        private static bool SafeItemTeleport(ItemPickupBase item)
        {
            RoomIdentifier room = RoomIdUtils.RoomAtPosition(item.transform.position);
            if (room == null)
            {
                Log.Info("null room for item pos");
                return false;
            }

            Vector3 position = Teleport.RoomPositions(room).RandomItem();
            RaycastHit info;
            if (!Physics.Raycast(position, Vector3.down, out info, 50.0f, item.gameObject.layer) || info.collider.name == "Killer")
            {
                Log.Info("pos: " + position.ToPreciseString());
                Log.Info("failed raycast: " + info.collider.name);
                return false;
            }

            FreezeItemAtPosition(item, info.point + (0.05f * Vector3.up));

            return true;
        }

        private static IEnumerator<float> _CheckStolen()
        {
            while (true)
            {
                foreach (var player in Player.GetPlayers())
                {
                    if (player.Role.GetTeam() != Team.ChaosInsurgency || player.Room == null || player.Room.Zone != FacilityZone.Surface)
                        continue;

                    string reward_hint = "reward:";
                    bool had_item = false;
                    foreach (var serial in player.ReferenceHub.inventory.UserInventory.Items.Keys.ToList())
                    {
                        ItemBase item = player.ReferenceHub.inventory.UserInventory.Items[serial];
                        if (!target_items.Contains(item.ItemTypeId))
                            continue;

                        items_left--;
                        had_item = true;
                        player.RemoveItem(new Item(item));
                        if (!player.ReferenceHub.inventory.UserInventory.Items.Values.Any(i => i.ItemTypeId == ItemType.KeycardContainmentEngineer))
                        {
                            player.AddItem(ItemType.KeycardContainmentEngineer);
                            reward_hint += " keycard containment engineer,";
                        }
                        else
                        {
                            switch (Random.Range(0, 7))
                            {
                                case 0: reward_hint += " 3x scp330,"; player.AddItem(ItemType.SCP330); player.AddItem(ItemType.SCP330); player.AddItem(ItemType.SCP330); break;
                                case 1: reward_hint += " anti cola,"; player.AddItem(ItemType.AntiSCP207); break;
                                case 2: reward_hint += " jailbird,"; player.AddItem(ItemType.Jailbird); break;
                                case 3: reward_hint += " com45,"; AddFirearm(player, ItemType.GunCom45, true); break;
                                case 4: reward_hint += " micro,"; player.AddItem(ItemType.MicroHID); break;
                                case 5: reward_hint += " keycard facility manager,"; player.AddItem(ItemType.KeycardFacilityManager); break;
                                case 6:
                                    reward_hint += " +1 cola effect,";
                                    CustomPlayerEffects.Scp207 effect;
                                    if (player.EffectsManager.TryGetEffect(out effect))
                                        player.EffectsManager.ChangeState<CustomPlayerEffects.Scp207>((byte)(effect.Intensity + 1));
                                    else
                                        player.EffectsManager.ChangeState<CustomPlayerEffects.Scp207>(1);
                                    break;
                            }
                        }
                    }
                    UpdatePlayerLight(player);

                    if (had_item)
                    {
                        reward_hint = reward_hint.Remove(reward_hint.Length - 1);
                        player.SendBroadcast(reward_hint, 15, shouldClearPrevious: true);
                        foreach(var p in Player.GetPlayers())
                        {
                            player.SendBroadcast((total_items - items_left) + " out of " + total_items + " items stolen", 15, shouldClearPrevious: player != p);
                        }
                        CheckChaosWin();
                    }
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }

        private static void CheckChaosWin()
        {
            if (items_left == 0)
            {
                foreach(var p in Player.GetPlayers())
                {
                    if (p.Role.GetTeam() == Team.FoundationForces)
                        p.Kill("you lost all SCP items to the CHAOS insurgency");
                    else
                        p.SendBroadcast("The CHAOS insurgency won!", 60, shouldClearPrevious: true);
                }
                Round.IsLocked = false;
            }
        }

        private static void SetupDoors()
        {
            KeycardPermissions permission = KeycardPermissions.ContainmentLevelThree;
            DoorDamageType ignore_all = DoorDamageType.ServerCommand | DoorDamageType.Grenade | DoorDamageType.Weapon | DoorDamageType.Scp096;

            foreach (var d in DoorVariant.DoorsByRoom[RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Hcz096)])
            {
                if (d.Rooms.Count() == 1)
                {
                    d.RequiredPermissions.RequiredPermissions = permission;
                    BreakableDoor breakable = d as BreakableDoor;
                    breakable.IgnoredDamageSources = ignore_all;
                    scp_doors.Add(d, RoleTypeId.Scp096);
                }
            }

            RoomIdentifier scp049 = RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Hcz049);
            foreach (var d in DoorVariant.DoorsByRoom[scp049])
            {
                if (d is PryableDoor)
                {
                    d.RequiredPermissions.RequiredPermissions = permission;
                    if (scp049.transform.InverseTransformPoint(d.transform.position).z < 0)
                        scp_doors.Add(d, RoleTypeId.Scp049);
                    else
                        scp_doors.Add(d, RoleTypeId.Scp173);
                }
            }

            scp049.gameObject.GetComponentInChildren<BreakableWindow>().health = float.PositiveInfinity;

            Vector3 offset = new Vector3(-5.262f, 0.0f, -1.422f);
            RoomIdentifier scp939 = RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Hcz939);
            Vector3 position = scp939.transform.TransformPoint(offset);
            GameObject pf = NetworkManager.singleton.spawnPrefabs.First(p => p.name == "HCZ BreakableDoor");
            BreakableDoor door = Object.Instantiate(pf, position, scp939.transform.rotation).GetComponent<BreakableDoor>();
            door.transform.localScale = new Vector3(1.0f, 1.0f, 1.5f);
            door.IgnoredDamageSources = ignore_all;
            door.RequiredPermissions.RequiredPermissions = permission;
            door.NetworkTargetState = false;
            NetworkServer.Spawn(door.gameObject);
            scp_doors.Add(door, RoleTypeId.Scp939);
        }

        private static void SetupLights()
        {
            Timing.CallDelayed(5.0f, () =>
            {
                ItemPickupBase[] items = Object.FindObjectsOfType<ItemPickupBase>();
                foreach (var item in items)
                {
                    if (target_items.Contains(item.Info.ItemId))
                    {
                        AddLight(item.gameObject.transform);
                        dropped_lights.Add(item.Info.Serial);
                    }
                }
            });

            light_update = Timing.RunCoroutine(_UpdateLights());
        }

        private static int UpdatePlayerLight(Player player)
        {
            int count = 0;
            foreach (var item in player.ReferenceHub.inventory.UserInventory.Items.Values)
                if (target_items.Contains(item.ItemTypeId))
                    count++;
            if (!player_lights.ContainsKey(player.PlayerId))
                player_lights.Add(player.PlayerId, AddLight(player.GameObject.transform));

            LightSourceToy light = player_lights[player.PlayerId];
            light.NetworkLightIntensity = count * 1.0f;
            light.NetworkLightRange = count * 30.0f;
            if(count != 0)
            {
                if (chaos.Contains(player.PlayerId) && player.Role.GetTeam() == Team.ChaosInsurgency)
                    player.SendBroadcast("You have an SCP item, take it to surface", 2, shouldClearPrevious: true);
                else if (mtf.Contains(player.PlayerId) && player.Role.GetTeam() == Team.FoundationForces)
                    player.SendBroadcast("You have an SCP item, protect it from the CHAOS insurgency", 2, shouldClearPrevious: true);
            }
            return count;
        }

        private static LightSourceToy AddLight(Transform transform)
        {
            GameObject light_pf = NetworkManager.singleton.spawnPrefabs.First(p => p.name == "LightSourceToy");
            LightSourceToy light_toy = Object.Instantiate(light_pf, transform).GetComponent<LightSourceToy>();
            light_toy.NetworkLightColor = new Color(0.0f, 1.0f, 0.0f);
            light_toy.NetworkLightIntensity = 2.0f;
            light_toy.NetworkLightRange = 10.0f;
            light_toy.NetworkLightShadows = true;
            light_toy.NetworkMovementSmoothing = 10;
            NetworkServer.Spawn(light_toy.gameObject);
            
            return light_toy;
        }

        private static IEnumerator<float> _UpdateLights()
        {
            while (true)
            {
                try
                {
                    items_left = 0;
                    ItemPickupBase[] items = Object.FindObjectsOfType<ItemPickupBase>();
                    foreach (var item in items)
                    {
                        if (target_items.Contains(item.Info.ItemId))
                        {
                            items_left++;
                            if (item.transform.position.y < -1007.0f)
                            {
                                Player owner;
                                if (item.PreviousOwner.Hub != null && Player.TryGet(item.PreviousOwner.Hub, out owner))
                                    owner.Kill("dropped item out of bounds");
                                if(!SafeItemTeleport(item))
                                {
                                    dropped_lights.Remove(item.Info.Serial);
                                    NetworkServer.Destroy(item.gameObject);
                                    Log.Error("item could not be saved. item pos: " + item.transform.position.ToPreciseString());
                                }
                            }
                            else
                            {
                                if(!dropped_lights.Contains(item.Info.Serial))
                                {
                                    AddLight(item.transform);
                                    dropped_lights.Add(item.Info.Serial);
                                }
                            }

                            if(item is Scp244DeployablePickup pickup)
                            {
                                if (pickup.State == Scp244State.PickedUp && dropped_lights.Contains(item.Info.Serial))
                                    NetworkServer.Destroy(pickup.gameObject);
                            }
                        }
                    }

                    foreach (var player in Player.GetPlayers())
                        items_left += UpdatePlayerLight(player);

                }
                catch (System.Exception ex)
                {
                    Log.Error("_UpdateLights error: " + ex.ToString());
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }

        private static void FreezeItemAtPosition(ItemPickupBase item, Vector3 pos)
        {
            if (item.PhysicsModule is PickupStandardPhysics physics)
            {
                physics.Rb.isKinematic = false;
                physics._serverEverDecelerated = false;
                physics.Rb.detectCollisions = false;
                physics.Rb.position = pos;
                physics.Rb.velocity = Vector3.zero;
                Timing.CallDelayed(0.0f, () =>
                {
                    physics._serverEverDecelerated = true;
                    if (physics._freezingMode == PickupStandardPhysics.FreezingMode.Default)
                        physics.Rb.velocity = Vector3.zero;
                    else
                        physics.Rb.Sleep();
                    physics.Rb.isKinematic = true;
                });
            }
        }

        private static IEnumerator<float> _UpdateClassD()
        {
            int class_d_escaped = 0;
            while (true)
            {
                try
                {
                    foreach(var player in Player.GetPlayers())
                    {
                        if (player.Role == RoleTypeId.ClassD)
                        {
                            if (player.Room != null && player.Room.Zone == FacilityZone.Surface)
                            {
                                if (!mtf.Remove(player.PlayerId))
                                {
                                    List<Player> spectators = Player.GetPlayers().Where(p => p.Role == RoleTypeId.Spectator).ToList();
                                    List<Player> valid_mtf = spectators.Where(p => mtf.Contains(p.PlayerId)).ToList();
                                    if (valid_mtf.IsEmpty())
                                        valid_mtf = Player.GetPlayers().Where(p => p.Role.GetTeam() == Team.FoundationForces).ToList();
                                    if (!valid_mtf.IsEmpty())
                                    {
                                        Player selected = valid_mtf.RandomItem();
                                        mtf.Remove(selected.PlayerId);
                                        chaos.Add(selected.PlayerId);
                                    }
                                }
                                else
                                    chaos.Add(player.PlayerId);
                                classd.Remove(player.PlayerId);
                                player.SetRole(RoleTypeId.ChaosConscript);
                                class_d_escaped++;
                                foreach (var p in Player.GetPlayers())
                                    p.SendBroadcast(class_d_escaped + " Class-Ds have escaped!", 15, shouldClearPrevious: true);
                            }
                        }
                        else if (classd.Contains(player.PlayerId))
                            classd.Remove(player.PlayerId);
                    }
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                yield return Timing.WaitForOneFrame;
            }
        }
    }

    public class ChaosRaidEvent:IEvent
    {
        public static ChaosRaidEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Chaos Raid";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "CR";
        public bool OverrideWinConditions { get; } = true;
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

        [PluginEntryPoint("Chaos Raid", "1.0.0", "", "The Riptide")]
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
