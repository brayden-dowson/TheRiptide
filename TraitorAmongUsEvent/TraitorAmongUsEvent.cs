using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using CustomPlayerEffects;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using MEC;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Core.Items;
using PluginAPI.Enums;
using Respawning;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using static TheRiptide.Utility;
using InventorySystem.Items.ThrowableProjectiles;
using InventorySystem.Items.MicroHID;
using InventorySystem.Items.Jailbird;
using System.Reflection;
using System.IO;
using static TheRiptide.TraitorAmongUsUtility;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        public int RoundCount { get; set; } = 3;
        [Description("how many players per detective")]
        public float DetectiveRatio { get; set; } = 7.5f;
        [Description("how many players per traitor (min 1 traitor)")]
        public float TraitorRatio { get; set; } = 4.0f;
        [Description("how many players per jester (min 1 jester)")]
        public float JesterRatio { get; set; } = 15.0f;
        [Description("chance the jesters will spawn for the round (0 = never, 1 = always)")]
        public float JesterChancePerRound { get; set; } = 0.3f;

        [Description("amount of damage done to a player with grace to trigger auto slay")]
        public float RdmDamageThreshold { get; set; } = 90.0f;
        [Description("amount of kills done to a players with grace or to players on the same team to trigger auto slay")]
        public int RdmKillThreshold { get; set; } = 2;
        [Description("how much leeway is given once a player can see a body that is unided until they lose grace. higher values give more time for the player to id the body")]
        public float UnidedBodyLeeway { get; set; } = 3.0f;

        public string Description { get; set; } = "A few players are Traitors, Detectives and Jesters the rest are Innocents. Traitors must kill all Innocents to win while Innocents must figure out who the Traitors are and kill them. Innocents vastly outnumber Traitors, but dont know who they are. Detectives are proven innocent and must help the Innocents win. Jesters cannot kill and win when an innocent player kills them.\n\n";

        public string DetectiveHint { get; set; } = "<b><size=128><color=#0000FF>Role: Detective</color></size></b>";
        public string InnocentHint { get; set; } = "<b><size=128><color=#00FF00>Role: Innocent</color></size></b>";
        public string TraitorHint { get; set; } = "<b><size=128><color=#FF0000>Role: Traitor</color></size></b>";
        public string JesterHint { get; set; } = "<b><size=128><color=#FF80FF>Role: Jester</color></size></b>";

        public List<string> DetectiveBroadcast { get; set; } = new List<string>{
            "<b><color=#0000FF><align=left>Detective</color> - Objective: Help innocents win</b>",
            "As a detective you are proven innocent by default",
            "You get access to a shop to help you find the traitors(Right click keycard in inventory)",
            "you earn points for the shop by collecting evidence e.g. ID-ing bodies" };

        public List<string> InnocentBroadcast { get; set; } = new List<string> {
            "<b><color=#00FF00><align=left>Innocent</color> - Objective: Survive</b>",
            "You must not RDM(kill with an invalid reason) otherwise you will be slain and miss out the next round",
            "You get an ID-Gun(the revolver) which when pointed at a body determines if they where a traitor or not(USE IT)",
            "you can kill players that do not ID bodies right away e.g. a player walks over a body without ID-ing it" };

        public List<string> TraitorBroadcast { get; set; } = new List<string> {
            "<b><color=#FF0000><align=left>Traitor</color> - Objective: Kill all innocents</b>",
            "You can see your traitor teammates as they will apear as Class-Ds",
            "You get access to a shop to help you kill all the innocents(Right click keycard in inventory)",
            "You can use the vents(press E on them) to get around the map fast",
            "You can sabotage by pulling certain levers around the ship e.g. turn off the lights in electrical" };

        public List<string> JesterBroadcast { get; set; } = new List<string> {
            "<b><color=#FF80FF><align=left>Jester</color> - Objective: Die</b>",
            "You must only die to a innocent to win, traitors dont count",
            "You are not able to kill anyone as the jester",
            "You can use vents but traitors can see that you are the jester" };

        public string ReadyUpBroadcast { get; set; } = "<size=20><line-height=80%><color=#00FF00><b>Gamemode:</b></color> Traitor Among US (Adaptation of the CSGO mod [TTT] Trouble in Terrorist Town) - A few players are Traitors, Detectives and Jesters the rest are Innocents. Traitors must kill all Innocents to win while Innocents must figure out who the Traitors are and kill them. Innocents vastly outnumber Traitors, but dont know who they are. Detectives are proven innocent and must help the Innocents win. Jesters cannot kill and win when an innocent player kills them.<size=27><line-height=75%>\n<color=#FF0000><b>Rules:</b> YOU MUST BE CERTAIN THAT A PERSON IS A TRAITOR BEFORE SHOOTING!!!.</color> All players get ID-Guns which when pointed at a body will ID it announcing to the server if the player was innocent or a traitor. If you see someone shoot and kill another person and they do not ID the body or the victim is innocent the attacker is a Traitor. You must see this happen and cant go of your \"feeling\" or because you heard shooting around the corner. Seeing a player use the vents is not proof they are the Traitor as Jesters can use vents too. If you understand these rules to ready up, ID the body on the table (do not tell other people how to ready up other than to say to read the rules)";

        public string JestersWinHint { get; set; } = "<b><size=92>The Jester Was Killed by {name}\n<color=#FF0000>They will sit out next round as punishment</color>\n<color=#FF80FF>The Jester Wins!</color></size></b>";
        public string InnocentsWinHint { get; set; } = "<b><size=128>All Traitors Eliminated\n<color=#00FF00>Innocents Win!</color></size></b>";
        public string TraitorsWinHint { get; set; } = "<b><size=128>All Innocents Eliminated\n<color=#FF0000>Traitors Win!</color></size></b>";
        public string TraitorsOutOfTimeAnnouncement { get; set; } = "<b><size=128>Traitors Ran out of Time</size></b>";

        public string RdmSlainMessage { get; set; } = "You RDM'd to much this round so you were set to spectator!";

        public string ReadyUpNotReadyHint { get; set; } = "<b><size=92><color=#FF0000>READ THE RULES TO READY UP</color>\n<color=#87ceeb>{ready}/{total} Players Ready\nRound starts in {time}</color></size></b>";
        public string ReadyUpReadyHint { get; set; } = "<b><size=92><color=#00FF00>YOU ARE READY</color>\n<color=#87ceeb>{ready}/{total} Players Ready\nRound starts in {time}</color></size></b>";
        public string ReadyUpToSlowHint { get; set; } = "<b><size=92><color=#FF0000>YOU DID NOT READY UP IN TIME</color></b>";

        public string KilledJesterHint { get; set; } = "You killed the jester last round!\n Read the rules to make sure this does not happen again";
        public string RdmedToMuchHint { get; set; } = "You RDM'd to much last round!\n Read the rules to make sure this does not happen again";

        public string StaffDidNotSelectMap { get; set; } = "[ERROR] The staff did not select a map!";

        public string RoundStartingHint { get; set; } = "<b><size=64><color=#87ceeb>Round {round} of {total} Starting...</color></size></b>";
    }
 
    public enum TauRole { Unassigned, Innocent, Detective, Traitor, Jester };

    public class TraitorAmongUs
    {
        public static Config config;
        public static float round_timer;
        private static int round;

        public static HashSet<int> not_ready = new HashSet<int>();
        public static HashSet<int> detectives = new HashSet<int>();
        public static HashSet<int> traitors = new HashSet<int>();
        public static HashSet<int> jesters = new HashSet<int>();

        private static List<IMap> maps = new List<IMap>();
        private static IMap map;
        public static string map_selected = "";

        public static bool force_start = false;
        public static bool pause_ready_up = false;
        public static bool is_ready_up = false;
        public static bool round_lock = false;
        private static Player jester_killer = null;
        private static CoroutineHandle round_logic;
        private static CoroutineHandle ready_up;
        private static CoroutineHandle round_setup;

        private static float ff_old;
        private static bool ff_state;
        private static bool uq_state;

        public static void Start(Config config)
        {
            uq_state = UltraQuaternion.Enabled;
            UltraQuaternion.Enable();
            AlphaWarheadController.Singleton._autoDetonate = false;
            TraitorAmongUs.config = config;
            FriendlyFireConfig.PauseDetector = true;
            ff_old = AttackerDamageHandler._ffMultiplier;
            AttackerDamageHandler._ffMultiplier = 1.0f;
            round = 0;

            PluginHandler handler = PluginHandler.Get(TraitorAmongUsEvent.Singleton);
            foreach (var dir in Directory.GetFiles(Path.Combine(handler.PluginDirectoryPath, "Maps"), "*.dll", SearchOption.TopDirectoryOnly))
            {
                var dll = Assembly.LoadFile(dir);
                foreach (System.Type type in dll.GetTypes())
                {
                    if (type.GetInterface(nameof(IMap)) != null)
                    {
                        IMap map = System.Activator.CreateInstance(type) as IMap;
                        if (maps.All(m => m.Name != map.Name))
                        {
                            maps.Add(map);
                            Log.Info("Map Registered: " + map.Name + " by " + map.Author + ". " + map.Description);
                        }
                    }
                }
            }
            Round.IsLobbyLocked = true;
            if(map_selected != "")
            {
                SetMap(map_selected);
                map_selected = "";
            }

            if (map == null && maps.Count == 1)
            {
                map = maps.First();
                Log.Info("Loading Map: " + map.Name);
                map.Load(TraitorAmongUsEvent.Singleton);
                Round.IsLobbyLocked = false;
            }
            BodyManager.Start();
            IDGunManager.Start();
            Shop.SetupMenu();
        }

        public static void Stop()
        {
            if (!uq_state)
                UltraQuaternion.Disable();
            jester_killer = null;
            Server.FriendlyFire = ff_state;
            AttackerDamageHandler._ffMultiplier = ff_old;

            not_ready.Clear();
            detectives.Clear();
            traitors.Clear();
            jesters.Clear();

            map.Unload(TraitorAmongUsEvent.Singleton);
            map = null;

            BodyManager.Stop();
            IDGunManager.Stop();

            Timing.KillCoroutines(round_logic, round_setup, ready_up);
            Announcements.Stop();
            RDM.Stop();
            RDM.Clear();
            BroadcastOverride.Reset();
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            if (Round.IsRoundStarted)
                player.SendBroadcast("Event being played: " + TraitorAmongUsEvent.Singleton.EventName + "\n<size=24>" + TraitorAmongUsEvent.Singleton.EventDescription + "</size>", 30, shouldClearPrevious: true);
            else if (map != null)
                player.SendBroadcast("Map: " + map.Name + " by " + map.Author + ". " + map.Description, 60, shouldClearPrevious: true);
            else
                player.SendBroadcast("Staff map select: " + string.Join(", ", maps.ConvertAll(m => m.Name)), 300, shouldClearPrevious: true);

            not_ready.Add(player.PlayerId);
            if (Round.IsRoundStarted && is_ready_up)
            {
                player.SendBroadcast(config.ReadyUpBroadcast, 300, shouldClearPrevious: true);
                player.SetRole(RoleTypeId.Scientist);
                BodyManager.RespawnReadyupBodyForClient(player);
            }
            player.ReferenceHub.nicknameSync.NetworkViewRange = 32.0f;
            BroadcastOverride.RegisterPlayer(player);
            BroadcastOverride.SetEvenLineSizes(player, 6);
            InventoryMenu.Singleton.RegisterPlayer(player);
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            not_ready.Remove(player.PlayerId);
            detectives.Remove(player.PlayerId);
            traitors.Remove(player.PlayerId);
            jesters.Remove(player.PlayerId);
            BroadcastOverride.UnregisterPlayer(player);
            InventoryMenu.Singleton.UnregisterPlayer(player);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            ff_state = Server.FriendlyFire;
            Server.FriendlyFire = true;

            if (map == null)
                foreach (var p in ReadyPlayers())
                    p.SendBroadcast(config.StaffDidNotSelectMap, 300, shouldClearPrevious: true);
            Round.IsLocked = true;

            is_ready_up = true;
            Timing.CallDelayed(1.0f, () => { round_logic = Timing.RunCoroutine(_RoundLogic(180, 30)); });
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted ||
                new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Overwatch || new_role == RoleTypeId.Filmmaker)
                return true;

            InventoryMenu.Singleton.ResetMenuState(player);

            if (detectives.Contains(player.PlayerId))
            {
                if (new_role != RoleTypeId.NtfPrivate)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.NtfPrivate);
                    });
                    return false;
                }
            }
            else if ((jesters.Contains(player.PlayerId) || !traitors.Contains(player.PlayerId) || not_ready.Contains(player.PlayerId)))
            {
                if (new_role != RoleTypeId.Scientist)
                {
                    Timing.CallDelayed(0.0f, () =>
                    {
                        player.SetRole(RoleTypeId.Scientist);
                    });
                    return false;
                }
            }
            else if(traitors.Contains(player.PlayerId))
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
            return true;
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            if (player == null || !Round.IsRoundStarted)
                return;

            if(role == RoleTypeId.NtfPrivate)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role != RoleTypeId.NtfPrivate)
                        return;
                    PlayerSetup(player);
                    player.AddItem(ItemType.KeycardMTFCaptain);
                    player.AddItem(ItemType.ArmorHeavy);
                    AddFirearm(player, ItemType.GunE11SR, true);
                    player.AddItem(ItemType.Medkit);
                    player.AddItem(ItemType.Painkillers);

                    player.ReceiveHint(config.DetectiveHint, 15);
                    BroadcastOverride.BroadcastLines(player, 1, 60 * 60, BroadcastPriority.Medium, config.DetectiveBroadcast);
                    BroadcastOverride.UpdateIfDirty(player);
                });
            }
            else if(role == RoleTypeId.Scientist)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role != RoleTypeId.Scientist)
                        return;

                    PlayerSetup(player);
                    if (is_ready_up)
                        return;
                    if (jesters.Contains(player.PlayerId))
                        player.AddItem(ItemType.KeycardChaosInsurgency);
                    player.AddItem(ItemType.ArmorCombat);
                    AddFirearm(player, ItemType.GunCrossvec, true);
                    
                    if (jesters.Contains(player.PlayerId))
                    {
                        player.ReceiveHint(config.JesterHint, 15);
                        BroadcastOverride.BroadcastLines(player, 1, 60 * 60, BroadcastPriority.Medium, config.JesterBroadcast);
                    }
                    else
                    {
                        player.ReceiveHint(config.InnocentHint, 15);
                        BroadcastOverride.BroadcastLines(player, 1, 60 * 60, BroadcastPriority.Medium, config.InnocentBroadcast);
                    }
                    BroadcastOverride.UpdateIfDirty(player);
                });
            }
            else if(role == RoleTypeId.ClassD)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role != RoleTypeId.ClassD)
                        return;

                    PlayerSetup(player);
                    player.AddItem(ItemType.KeycardChaosInsurgency);
                    player.AddItem(ItemType.ArmorCombat);
                    AddFirearm(player, ItemType.GunCrossvec, true);

                    player.ReceiveHint(config.TraitorHint, 15);
                    BroadcastOverride.BroadcastLines(player, 1, 60 * 60, BroadcastPriority.Medium, config.TraitorBroadcast);
                    BroadcastOverride.UpdateIfDirty(player);
                });
            }
           
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        bool OnRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerShotWeapon)]
        void OnShotWeapon(Player player, Firearm gun)
        {
            if (gun.ItemTypeId != ItemType.GunRevolver || gun.Status.Attachments != 0)
                RDM.EndGrace(player);
        }

        [PluginEvent(ServerEventType.PlayerDropItem)]
        bool OnPlayerDroppedItem(Player player, ItemBase item)
        {
            if (item.ItemTypeId == ItemType.GunRevolver && item is Firearm gun && gun._status.Attachments == 0)
                return false;
            if (!InventoryMenu.Singleton.OnPlayerDropitem(player, item))
                return false;

            if (detectives.Contains(player.PlayerId) && item.ItemTypeId == ItemType.KeycardMTFCaptain)
            {
                Shop.SaveInventory(player);
                Shop.ShowMenu(player, MenuPage.DetectiveMainMenu);
                return false;
            }
            else if (traitors.Contains(player.PlayerId) && item.ItemTypeId == ItemType.KeycardChaosInsurgency)
            {
                Shop.SaveInventory(player);
                Shop.ShowMenu(player, MenuPage.TraitorMainMenu);
                return false;
            }
            return true;
        }

        [PluginEvent(ServerEventType.PlayerChangeItem)]
        void OnPlayerChangesItem(Player player, ushort old_item, ushort new_item)
        {
            ItemBase held = null;
            if (!player.ReferenceHub.inventory.UserInventory.Items.TryGetValue(new_item, out held))
                return;
            InventoryMenu.Singleton.OnPlayerChangedItem(player, held);
            if (jesters.Contains(player.PlayerId) && held is Firearm firearm)
                firearm.ActionModule = new JesterFirearmActionModule();
            if (held is MicroHIDItem micro)
            {
                micro.RemainingEnergy = 0.0f;
                micro.ServerSendStatus(HidStatusMessageType.EnergySync, 0);
            }
            if(held is Revolver id_gun)
            {
                if (id_gun.Status.Attachments != 0)
                    id_gun.Status = new FirearmStatus(0, FirearmStatusFlags.None, 0);
            }
        }

        [PluginEvent(ServerEventType.PlayerThrowProjectile)]
        void OnPlayerThrowProjectile(Player player, ThrowableItem item, ThrowableItem.ProjectileSettings projectileSettings, bool fullForce)
        {
            if (jesters.Contains(player.PlayerId))
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    ThrownProjectile[] projectiles = Object.FindObjectsOfType<ThrownProjectile>();
                    foreach(var projectile in projectiles)
                    {
                        if (projectile.Info.Serial == item.ItemSerial)
                        {
                            NetworkServer.Destroy(projectile.gameObject);
                        }
                    }
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerDamage)]
        void OnPlayerDamage(Player attacker, Player victim, DamageHandlerBase handler)
        {
            if (victim == null)
                return;

            if(attacker != null && attacker != victim)
            {
                if (handler is JailbirdDamageHandler jailbird_handler)
                {
                    jailbird_handler.Damage = 0.0f;
                    if ((attacker.CurrentItem as JailbirdItem).TotalChargesPerformed != 5)
                    {
                        TauRole victim_role = GetPlayerTauRole(victim);
                        TauRole scanner_role = detectives.Contains(attacker.PlayerId) ? TauRole.Detective : TauRole.Innocent;
                        Announcements.Add(new Announcement(
                            "<color=#87ceeb><b>" + TauRoleToColor(scanner_role) + attacker.Nickname + "</color></b> proved <b>" +
                            victim.Nickname + "</b> is " + (victim_role == TauRole.Innocent ? "<b>" : "a <b>") +
                            TauRoleToColor(victim_role) + victim_role + "</b></color></color>", 60.0f));
                        if (victim_role == TauRole.Traitor || victim_role == TauRole.Jester)
                            RDM.EndGrace(victim);
                    }
                    var jailbird = attacker.CurrentItem as JailbirdItem;
                    jailbird.TotalChargesPerformed = 5;
                }
            }
        }

        [PluginEvent(ServerEventType.PlayerDying)]
        void OnPlayerDying(Player victim, Player attacker, DamageHandlerBase damageHandler)
        {
            if (not_ready.Contains(victim.PlayerId))
                victim.ClearInventory();
            List<ItemBase> to_remove = new List<ItemBase>();
            if (InventoryMenu.Singleton.GetPlayerMenuID(victim) != 0)
                victim.ClearInventory();
            foreach (var item in victim.ReferenceHub.inventory.UserInventory.Items.Values)
            {
                if (item.ItemTypeId == ItemType.GunRevolver && item is Firearm gun && gun._status.Attachments == 0)
                    to_remove.Add(item);
                if (item.ItemTypeId == ItemType.KeycardChaosInsurgency || item.ItemTypeId == ItemType.KeycardMTFCaptain)
                    to_remove.Add(item);
            }

            foreach (var item in to_remove)
                victim.RemoveItem(new Item(item));
        }

        [PluginEvent(ServerEventType.PlayerDeath)]
        void OnPlayerDied(Player victim, Player attacker, DamageHandlerBase damageHandler)
        {
            if (victim == null)
                return;

            BroadcastOverride.ClearLines(victim, BroadcastPriority.Medium);
            if (attacker != null && victim != attacker)
            {
                if (jesters.Contains(victim.PlayerId) && !traitors.Contains(attacker.PlayerId))
                    jester_killer = attacker;

                if (traitors.Contains(attacker.PlayerId))
                {
                    BroadcastOverride.BroadcastLine(victim, 1, 15.0f, BroadcastPriority.Medium, "You were killed by a " + TauRoleToColor(TauRole.Traitor) + "Traitor</color>");
                    int reward = 50;
                    if (traitors.Contains(victim.PlayerId))
                        reward = -1000;
                    else if (jesters.Contains(victim.PlayerId))
                        reward = 100;
                    else if (detectives.Contains(victim.PlayerId))
                        reward = 100;
                    else if (not_ready.Contains(victim.PlayerId))
                        reward = 0;
                    if (reward != 0)
                        Shop.RewardCash(attacker, reward, "<b><color=#00FF00>$" + reward + "</color> reward for killing an Innocent! Check shop for options</b>");
                    if(reward < 0)
                        Shop.RewardCash(attacker, reward, "<b><color=#FF0000>$" + reward + "</color> penalty for killing a Teammate!</b>");
                }
                else if (detectives.Contains(attacker.PlayerId))
                {
                    BroadcastOverride.BroadcastLine(victim, 1, 15.0f, BroadcastPriority.Medium, "You were killed by a " + TauRoleToColor(TauRole.Detective) + "Detective</color>");
                    int reward = -1000;
                    if (traitors.Contains(victim.PlayerId))
                        reward = 250;
                    else if (jesters.Contains(victim.PlayerId))
                        reward = -1000;
                    else if (detectives.Contains(victim.PlayerId))
                        reward = -1000;
                    else if (not_ready.Contains(victim.PlayerId))
                        reward = 0;
                    if (reward != 0)
                        Shop.RewardCash(attacker, reward, "<b><color=#00FF00>$" + reward + "</color> reward for killing a Traitor! Check shop for options</b>");
                    if (reward < 0)
                        Shop.RewardCash(attacker, reward, "<b><color=#FF0000>$" + reward + "</color> penalty for killing a Teammate!</b>");
                }
                else if(not_ready.Contains(attacker.PlayerId))
                    BroadcastOverride.BroadcastLine(victim, 1, 15.0f, BroadcastPriority.Medium, "You were killed by an " + TauRoleToColor(TauRole.Unassigned) + "[Error]Unassigned</color>");
                else
                    BroadcastOverride.BroadcastLine(victim, 1, 15.0f, BroadcastPriority.Medium, "You were killed by an " + TauRoleToColor(TauRole.Innocent) + "Innocent</color>");
            }
            BroadcastOverride.UpdateIfDirty(victim);
            if (attacker != null)
                BroadcastOverride.UpdateIfDirty(attacker);

            List<Player> player_traitors = Player.GetPlayers().Where(p => p.IsAlive && traitors.Contains(p.PlayerId) && p.Role == RoleTypeId.ClassD).ToList();
            foreach (var t in player_traitors)
                victim.Connection.Send(new RoleSyncInfo(t.ReferenceHub, RoleTypeId.ClassD, victim.ReferenceHub));

            List<Player> player_jesters = Player.GetPlayers().Where(p => p.IsAlive && jesters.Contains(p.PlayerId) && p.Role == RoleTypeId.Scientist).ToList();
            foreach(var j in player_jesters)
                victim.Connection.Send(new RoleSyncInfo(j.ReferenceHub, RoleTypeId.Scp173, victim.ReferenceHub));
        }

        [PluginEvent(ServerEventType.PlayerHandcuff)]
        bool OnPlayerHandcuffed(Player player, Player target)
        {
            return false;
        }

        public static bool IsPlayerReady(Player player)
        {
            return !not_ready.Contains(player.PlayerId);
        }

        public static void ReadyUpPlayer(Player player)
        {
            not_ready.Remove(player.PlayerId);
            map.OnPlayerReady(player);
        }

        public static float RoundLength()
        {
            return map == null ? 10.0f : map.RoundTime;
        }

        public static TauRole GetPlayerTauRole(Player player)
        {
            TauRole role = TauRole.Unassigned;
            if (traitors.Contains(player.PlayerId))
                role = TauRole.Traitor;
            else if (jesters.Contains(player.PlayerId))
                role = TauRole.Jester;
            else if (detectives.Contains(player.PlayerId))
                role = TauRole.Detective;
            else if (not_ready.Contains(player.PlayerId))
                role = TauRole.Unassigned;
            else
                role = TauRole.Innocent;
            return role;
        }

        private static IEnumerator<float> _RoundLogic(int first_ready_up_time, int ready_up_time)
        {
            ClearRagdolls();
            ClearItemPickups();
            
            ready_up = Timing.RunCoroutine(_ReadyUp(first_ready_up_time));

            while (round < config.RoundCount)
            {
                while (ready_up.IsAliveAndPaused || ready_up.IsRunning)
                    yield return Timing.WaitForSeconds(1.0f);

                round_setup = Timing.RunCoroutine(_SetupRound());
                while (round_setup.IsAliveAndPaused || round_setup.IsRunning)
                    yield return Timing.WaitForOneFrame;

                round_timer = 0.0f;
                yield return Timing.WaitForSeconds(15.0f);

                Announcements.Start();

                bool sent_out_of_time_annoucement = false;
                WinningRole winner;
                while (true)
                {
                    try
                    {
                        if (!round_lock)
                        {
                            if (jester_killer != null)
                            {
                                winner = WinningRole.Jesters;
                                Timing.CallDelayed(0.0f, () =>
                                {
                                    foreach (var p in ReadyPlayers())
                                    {
                                        if (traitors.Contains(p.PlayerId))
                                            p.Kill("Cause of Death: You let an Innocent kill the Jester");
                                        p.ReceiveHint(config.JestersWinHint.Replace("{name}", jester_killer.Nickname), 10);
                                    }
                                });
                                break;
                            }

                            int jesters_alive = 0;
                            int traitors_alive = 0;
                            int innocents_alive = 0;
                            foreach (var p in Player.GetPlayers())
                            {
                                if (p.IsAlive && p.Role != RoleTypeId.Tutorial)
                                {
                                    if (RDM.OverRDMLimit(p))
                                    {
                                        p.Kill("Cause of Death: Complications from being retarded.");
                                        p.SendBroadcast(config.ReadyUpBroadcast, 300, shouldClearPrevious: true);
                                        p.ReceiveHint(config.RdmSlainMessage, 60);
                                        not_ready.Add(p.PlayerId);
                                    }
                                    else
                                    {
                                        if (traitors.Contains(p.PlayerId))
                                            traitors_alive++;
                                        else if (jesters.Contains(p.PlayerId))
                                            jesters_alive++;
                                        else
                                            innocents_alive++;
                                    }
                                }
                            }
                            if (traitors_alive == 0 && map.InnocentsMetWinCondition())
                            {
                                winner = WinningRole.Innocents;
                                Timing.CallDelayed(0.0f, () =>
                                {
                                    foreach (var p in ReadyPlayers())
                                        p.ReceiveHint(config.InnocentsWinHint, 10);
                                });
                                break;
                            }
                            else if (innocents_alive == 0)
                            {
                                winner = WinningRole.Traitors;
                                Timing.CallDelayed(0.0f, () =>
                                {
                                    foreach (var p in ReadyPlayers())
                                        p.ReceiveHint(config.TraitorsWinHint, 10);
                                });
                                break;
                            }
                            if (round_timer > (60.0f * map.RoundTime))
                            {
                                foreach (var p in ReadyPlayers())
                                    if (traitors.Contains(p.PlayerId))
                                        p.Kill("you were to slow!");
                                if (!sent_out_of_time_annoucement)
                                {
                                    Announcements.Add(new Announcement(config.TraitorsOutOfTimeAnnouncement, 30.0f));
                                    sent_out_of_time_annoucement = true;
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }

                    yield return Timing.WaitForSeconds(1.0f);
                    round_timer += 1.0f;
                }
                map.OnRoundEnd(winner);
                RDM.Stop();
                Announcements.Stop();
                yield return Timing.WaitForSeconds(10.0f);
                round++;
                if (round >= config.RoundCount)
                    break;

                ResetRound();

                foreach (var p in ReadyPlayers())
                    if (p.Role != RoleTypeId.Overwatch && p.Role != RoleTypeId.Tutorial)
                        p.SetRole(RoleTypeId.Scientist);

                ready_up = Timing.RunCoroutine(_ReadyUp(ready_up_time));
            }
            Round.IsLocked = false;
        }

        private static void ResetRound()
        {
            detectives.Clear();
            traitors.Clear();
            jesters.Clear();
            BodyManager.Reset();
            IDGunManager.Reset();
            Shop.Reset();
        }

        private static IEnumerator<float> _SetupRound()
        {
            foreach (var player in ReadyPlayers())
                if (!not_ready.Contains(player.PlayerId))
                    player.ReceiveHint(config.RoundStartingHint.Replace("{round}", (round + 1).ToString()).Replace("{total}", config.RoundCount.ToString()), 4);

            yield return Timing.WaitForSeconds(3.0f);

            List<Player> players = ReadyPlayers().Where(p => p.Role != RoleTypeId.Overwatch && !not_ready.Contains(p.PlayerId) && p != jester_killer && !RDM.OverRDMLimit(p)).ToList();
            int detective_count = Mathf.RoundToInt(players.Count / config.DetectiveRatio);
            int traitor_count = Mathf.Max(Mathf.RoundToInt(players.Count / config.TraitorRatio), 1);
            int jester_count = 0;

            if (Random.value < config.JesterChancePerRound)
                jester_count = Mathf.Max(Mathf.RoundToInt(players.Count / config.JesterRatio), 1);

            for (int i = 0; i < detective_count; i++)
                if (!players.IsEmpty())
                    detectives.Add(players.PullRandomItem().PlayerId);

            for (int i = 0; i < traitor_count; i++)
                if (!players.IsEmpty())
                    traitors.Add(players.PullRandomItem().PlayerId);

            for (int i = 0; i < jester_count; i++)
                if (!players.IsEmpty())
                    jesters.Add(players.PullRandomItem().PlayerId);

            map.OnRoundStart();

            foreach (var p in ReadyPlayers())
            {
                if(!not_ready.Contains(p.PlayerId))
                {
                    if (p.Role == RoleTypeId.Overwatch || p.Role == RoleTypeId.Tutorial)
                        continue;
                    if(jester_killer != null && p == jester_killer)
                    {
                        p.SetRole(RoleTypeId.Spectator);
                        p.ReceiveHint(config.KilledJesterHint, 6000);
                        p.SendBroadcast(config.ReadyUpBroadcast, 300, shouldClearPrevious: true);
                        not_ready.Add(p.PlayerId);
                        jester_killer = null;
                    }
                    else if(RDM.OverRDMLimit(p))
                    {
                        p.SetRole(RoleTypeId.Spectator);
                        p.ReceiveHint(config.RdmedToMuchHint, 6000);
                        p.SendBroadcast(config.ReadyUpBroadcast, 300, shouldClearPrevious: true);
                        not_ready.Add(p.PlayerId);
                    }
                    else
                    {
                        p.SetRole(RoleTypeId.ClassD);
                    }
                }
            }

            yield return Timing.WaitForOneFrame;
            yield return Timing.WaitForOneFrame;

            Announcements.RefreshInnocentInfo();
            BodyManager.Reset();
            IDGunManager.Reset();
            Shop.Reset();
            RDM.Reset();
            RDM.Start();

            List<Player> player_traitors = Player.GetPlayers().Where(p => p.IsAlive && traitors.Contains(p.PlayerId) && p.Role == RoleTypeId.ClassD).ToList();
            foreach (var p in Player.GetPlayers())
                if (p.IsAlive && (detectives.Contains(p.PlayerId) || jesters.Contains(p.PlayerId) || !traitors.Contains(p.PlayerId)))
                    foreach (var t in player_traitors)
                        p.Connection.Send(new RoleSyncInfo(t.ReferenceHub, RoleTypeId.Scientist, p.ReferenceHub));
        }

        private static IEnumerator<float> _ReadyUp(int time)
        {
            force_start = false;
            is_ready_up = true;
            map.OnReadyUpStart();
            BodyManager.BeginReadyUP(map.ReadyUpBodyPosition);
            foreach (var p in ReadyPlayers())
                p.SendBroadcast(config.ReadyUpBroadcast, 300, shouldClearPrevious: true);

            HashSet<Player> not_ready_desync = ReadyPlayers().Where(p => p.IsAlive && not_ready.Contains(p.PlayerId)).ToHashSet();
            foreach (var p in ReadyPlayers().Where(p => p.Role != RoleTypeId.None))
                foreach (var nr in not_ready_desync)
                    if (p != nr)
                        p.Connection.Send(new RoleSyncInfo(nr.ReferenceHub, RoleTypeId.Tutorial, p.ReferenceHub));

            int passed = 0;
            while (passed <= time)
            {
                int ready = 0;
                try
                {
                    string ready_str = (Player.Count - not_ready.Count).ToString();
                    string total_str = Mathf.Max(Player.Count, 2).ToString();
                    string time_str = pause_ready_up ? "Paused" : (time - passed).ToString();
                    foreach (var p in ReadyPlayers())
                    {
                        if (!p.IsAlive)
                            continue;

                        if (not_ready.Contains(p.PlayerId))
                        {
                            if(!not_ready_desync.Contains(p))
                            {
                                not_ready_desync.Add(p);
                                foreach (var player in ReadyPlayers().Where(player => p.Role != RoleTypeId.None))
                                    if (p != player)
                                        player.Connection.Send(new RoleSyncInfo(p.ReferenceHub, RoleTypeId.Tutorial, player.ReferenceHub));
                                foreach (var nr in not_ready_desync)
                                    if (p != nr)
                                        p.Connection.Send(new RoleSyncInfo(nr.ReferenceHub, RoleTypeId.Tutorial, p.ReferenceHub));
                            }
                            p.ReceiveHint(config.ReadyUpNotReadyHint.Replace("{ready}", ready_str).Replace("{total}", total_str).Replace("{time}", time_str), 2);
                        }
                        else
                        {
                            if(not_ready_desync.Contains(p))
                            {
                                foreach (var player in ReadyPlayers().Where(player => p.Role != RoleTypeId.None))
                                    if (p != player)
                                        player.Connection.Send(new RoleSyncInfo(p.ReferenceHub, RoleTypeId.Scientist, player.ReferenceHub));
                                not_ready_desync.Remove(p);
                            }
                            p.ReceiveHint(config.ReadyUpReadyHint.Replace("{ready}", ready_str).Replace("{total}", total_str).Replace("{time}", time_str), 2);
                            ready++;
                        }
                    }
                    if (ready >= 3 && ready >= (Player.GetPlayers().Where(p => p.Role != RoleTypeId.Overwatch).Count() - 1) && (time - passed) > 10)
                        passed = time - 10;
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                yield return Timing.WaitForSeconds(1.0f);
                if (ready >= 3 && !pause_ready_up)
                    passed += 1;
                if (force_start)
                    break;
            }
            BodyManager.EndReadyUp();

            foreach (var player in ReadyPlayers())
            {
                if (not_ready.Contains(player.PlayerId))
                {
                    player.SetRole(RoleTypeId.Spectator);
                    player.ReceiveHint(config.ReadyUpToSlowHint, 30);
                    player.SendBroadcast(config.ReadyUpBroadcast, 300, shouldClearPrevious: true);
                }
            }
            map.OnReadyUpEnd();
            is_ready_up = false;
        }

        private static void PlayerSetup(Player player)
        {
            map.OnPlayerSpawn(player);
            player.EffectsManager.EnableEffect<Ensnared>(1);
            player.ClearInventory();
            IDGunManager.GivePlayerIDGun(player);
            if (is_ready_up)
                return;
            player.AddItem(ItemType.Radio);
        }

        public static bool SetMap(string name)
        {
            List<IMap> selected = maps.FindAll(m => m.Name.ToLower() == name.ToLower());
            if (!selected.IsEmpty())
            {
                map = selected.RandomItem();
                Round.IsLobbyLocked = false;
                foreach (var p in ReadyPlayers())
                    p.SendBroadcast("Map: " + map.Name + " by " + map.Author + ". " + map.Description, 60, shouldClearPrevious: true);
                return true;
            }
            return false;
        }
    }

    public class TraitorAmongUsEvent:IEvent
    {
        public static TraitorAmongUsEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Traitor Among Us";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return EventConfig == null ? "config not loaded" : EventConfig.Description; }
            set { if (EventConfig != null) EventConfig.Description = value; else Log.Error("EventConfig null when setting value"); }
        }
        public string EventPrefix { get; } = "TAU";
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
            TraitorAmongUs.Start(EventConfig);
            Log.Info(EventName + " event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<TraitorAmongUs>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            TraitorAmongUs.Stop();
            PluginAPI.Events.EventManager.UnregisterEvents<TraitorAmongUs>(this);
        }

        [PluginEntryPoint("Traitor Among Us Event", "1.0.0", "", "The Riptide")]
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
