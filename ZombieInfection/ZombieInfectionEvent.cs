using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using CommandSystem;
using CustomPlayerEffects;
using HarmonyLib;
using InventorySystem.Items.Firearms;
using MEC;
using Mirror;
using Mirror.LiteNetLib4Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using Respawning;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public sealed class Config : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;

        public int MotherZombieHealth { get; set; } = 4000;
        public int ChildrenHealthPool { get; set; } = 400;
        [Description("Knockback in meters with a 1:1 ratio of zombies to humans per 100 dmg")]
        public float KnockBackMultiple { get; set; } = 10.00f;
        public int AutoNuke { get; set; } = 15;
        public int CureKillsThreshold { get; set; } = 2;
    }


    public class EventHandler
    {
        private Config config;
        public static Dictionary<int, HashSet<int>> zombies = new Dictionary<int, HashSet<int>>();
        public static HashSet<string> left_as_zombie = new HashSet<string>();
        public static Dictionary<int, int> kills_as_child = new Dictionary<int, int>();

        public static CoroutineHandle warhead_handle = new CoroutineHandle();
        public static CoroutineHandle validity_checks_handle = new CoroutineHandle();
        public static int humans_alive = 1;
        public static int zombies_alive = 1;

        public EventHandler()
        {
            config = ZombieInfectionEvent.Singleton.EventConfig;
        }

        public static void Start()
        {
            Round.IsLobbyLocked = true;
            Timing.CallDelayed(30.0f, () =>
            {
                Round.IsLobbyLocked = false;
            });
            zombies.Clear();
            left_as_zombie.Clear();
            kills_as_child.Clear();
        }

        public static void Stop()
        {
            Timing.KillCoroutines(warhead_handle, validity_checks_handle);
            zombies.Clear();
            left_as_zombie.Clear();
            kills_as_child.Clear();
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            if (player.DoNotTrack)
                player.SendBroadcast("CUSTOM GAMEMODE EVENT YOUR DO NOT TRACK SIGNAL WILL BE IGNORED. Tracking whether you are a zombie for the duration of the round, info will be deleted as soon as the event ends", 60, shouldClearPrevious: true);
            else
                player.SendBroadcast("Event being played: " + ZombieInfectionEvent.Singleton.EventName + "\n<size=24>" + ZombieInfectionEvent.Singleton.EventDescription + "</size>", 60, shouldClearPrevious: true);

            int player_id = player.PlayerId;

            Timing.CallDelayed(1.0f, () =>
            {
                try
                {
                    if (zombies.Count >= 1)
                    {
                        Player p = Player.Get(player_id);
                        if (p != null && left_as_zombie.Contains(player.UserId))
                        {
                            left_as_zombie.Remove(player.UserId);
                            zombies[Random.Range(0, zombies.Count)].Add(player_id);
                            p.SetRole(RoleTypeId.Scp0492);
                        }
                    }
                }
                catch(System.Exception ex)
                {
                    Log.Error("error on joined: " + ex.ToString());
                }
            });
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            foreach(var mother in zombies.Keys.ToList())
            {
                if(zombies[mother].Contains(player.PlayerId))
                {
                    zombies[mother].Remove(player.PlayerId);
                    left_as_zombie.Add(player.UserId);
                }
            }
        }

        private void ShortRespawn()
        {
            RespawnManager.Singleton._timeForNextSequence = 90.0f;
            RespawnManager.Singleton._curSequence = RespawnManager.RespawnSequencePhase.RespawnCooldown;
            if (RespawnManager.Singleton._stopwatch.IsRunning)
                RespawnManager.Singleton._stopwatch.Restart();
            else
                RespawnManager.Singleton._stopwatch.Start();
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            Timing.CallDelayed(10.0f, () =>
            {
                RespawnManager.Singleton.NextKnownTeam = SpawnableTeamType.NineTailedFox;
                RespawnTokensManager.ForceTeamDominance(SpawnableTeamType.NineTailedFox, 1f);
                ShortRespawn();
            });

            Timing.CallDelayed(7.0f, () =>
            {
                Cassie.Message("bell_start pitch_0.4 .G4 bell_start bell_start pitch_0.9 jam_043_1 Danger jam_043_2 an unknown jam_043_3 jam_043_2 infection jam_043_1 has jam_043_3 been jam_043_5 detected in the jam_043_1 facility bell_start bell_start pitch_0.32 .G2");
            });

            warhead_handle = Timing.CallDelayed(60.0f * config.AutoNuke, () =>
            {
                AlphaWarheadController.Singleton.IsLocked = false;
                AlphaWarheadController.Singleton.enabled = true;
                AlphaWarheadController.Singleton.StartDetonation(true, true);
            });

            validity_checks_handle = Timing.RunCoroutine(_Checks());
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        void OnTeamRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            Timing.CallDelayed(30.0f, () =>
            {
                ShortRespawn();
            });
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player != null)
            {
                int player_id = player.PlayerId;
                if (new_role.GetTeam() == Team.SCPs && new_role != RoleTypeId.Scp0492 && !zombies.ContainsKey(player_id))
                    zombies.Add(player_id, new HashSet<int>());

                if (zombies.ContainsKey(player_id))
                {
                    if (new_role != RoleTypeId.Scp0492 && new_role != RoleTypeId.Spectator && new_role != RoleTypeId.Tutorial && new_role != RoleTypeId.Overwatch)
                    {
                        if (new_role == RoleTypeId.Scp079 || new_role == RoleTypeId.Scp106)
                        {
                            Timing.CallDelayed(0.1f, () =>
                            {
                                Player p = Player.Get(player_id);
                                if (p != null)
                                    p.SetRole(RoleTypeId.Scp939);
                            });
                        }
                        else
                        {
                            Timing.CallDelayed(0.1f, () =>
                            {
                                Player p = Player.Get(player_id);
                                if (p != null)
                                    p.SetRole(RoleTypeId.Scp0492);
                            });
                        }
                        return false;
                    }
                }
                else if (zombies.Any((c) => c.Value.Contains(player_id)))
                {
                    if (new_role != RoleTypeId.Scp0492 && new_role != RoleTypeId.Spectator && new_role != RoleTypeId.Tutorial && new_role != RoleTypeId.Overwatch)
                    {
                        Timing.CallDelayed(0.1f, () =>
                        {
                            Player p = Player.Get(player_id);
                            if (p != null)
                                p.SetRole(RoleTypeId.Scp0492);
                        });
                        return false;
                    }
                }
                else if (new_role == RoleTypeId.ClassD || new_role == RoleTypeId.Scientist)
                {
                    Timing.CallDelayed(1.0f, () =>
                    {
                        Player p = Player.Get(player_id);
                        if (p != null)
                        {
                            if (new_role == RoleTypeId.ClassD)
                            {
                                p.AddItem(ItemType.GunCOM15);
                                p.AddAmmo(ItemType.Ammo9x19, 10);
                            }
                            else if (new_role == RoleTypeId.Scientist)
                            {
                                RemoveItem(p, ItemType.KeycardScientist);
                                p.AddItem(ItemType.KeycardResearchCoordinator);
                                p.AddItem(ItemType.GunCOM18);
                            }
                        }
                    });
                }
            }
            else
                Log.Info("null player on change roles");
            return true;
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            if(player != null)
            {
                int player_id = player.PlayerId;
                if (role == RoleTypeId.Scp0492)
                {
                    if (zombies.ContainsKey(player_id))
                    {
                        SetScale(player, 1.10f);
                        Timing.CallDelayed(1.0f, () =>
                        {
                            Player p = Player.Get(player_id);
                            if (p != null)
                            {
                                p.SendBroadcast("You have spawed as a Mother Zombie!\nyou have the ability to open gates", 5);
                                p.IsBypassEnabled = true;
                            }
                        });
                    }
                    else if (zombies.Any((c) => c.Value.Contains(player_id)))
                    {
                        SetScale(player, 0.85f);
                        Timing.CallDelayed(1.0f, () =>
                        {
                            Player p = Player.Get(player_id);
                            if (p != null)
                            {
                                p.SendBroadcast("You have spawed as a Child Zombie!\n", 5);
                            }
                        });

                        int id = zombies.Where((c) => c.Value.Contains(player_id)).First().Key;
                        Player mother = Player.Get(id);
                        if (mother != null)
                            player.Position = mother.Position;
                    }
                }
            }
        }

        [PluginEvent(ServerEventType.PlayerDamage)]
        void OnPlayerDamage(Player attacker, Player victim, DamageHandlerBase damage)
        {
            if (victim != null && attacker != null)
            {
                bool mother = zombies.ContainsKey(victim.PlayerId);
                bool child = zombies.Any((c) => c.Value.Contains(victim.PlayerId));

                if ((mother || child) && damage is FirearmDamageHandler firearm)
                {
                    victim.EffectsManager.ChangeState<Disabled>(1, 1);

                    float multiplier = 1.0f;
                    if (firearm.WeaponType == ItemType.GunCOM15)
                        multiplier = 5.0f;

                    Vector3 dir = attacker.ReferenceHub.PlayerCameraReference.rotation * Vector3.forward;
                    var fpm = victim.GameObject.GetComponentInChildren<FirstPersonMovementModule>();
                    Timing.CallDelayed(0.0f, () =>
                    {
                        float ping = (LiteNetLib4MirrorServer.Peers[victim.ReferenceHub.netIdentity.connectionToClient.connectionId].Ping * 4.0f) / 1000.0f;
                        fpm.CharController.Move((victim.Velocity * ping) + (multiplier * dir * config.KnockBackMultiple * (zombies_alive / humans_alive) * (mother ? 0.5f : 1.0f) * (firearm.Damage / 100.0f)));
                        fpm.ServerOverridePosition(fpm.CharController.transform.position, Vector3.zero);
                    });
                }
            }
        }

        [PluginEvent(ServerEventType.PlayerDying)]
        void OnPlayerDying(Player victim, Player attacker, DamageHandlerBase damageHandler)
        {
            if (victim != null)
            {
                int victim_id = victim.PlayerId;
                if (attacker != null && victim != attacker)
                {
                    if (zombies.ContainsKey(attacker.PlayerId))
                        zombies[attacker.PlayerId].Add(victim_id);
                    else if (zombies.Any((c) => c.Value.Contains(attacker.PlayerId)))
                    {
                        zombies[zombies.Where(c => c.Value.Contains(attacker.PlayerId)).First().Key].Add(victim_id);

                        if (!kills_as_child.ContainsKey(attacker.PlayerId))
                            kills_as_child.Add(attacker.PlayerId, 0);
                        kills_as_child[attacker.PlayerId]++;
                    }
                }

                if(zombies.ContainsKey(victim_id))
                {
                    victim.IsBypassEnabled = false;
                    HashSet<int> children = zombies[victim_id];
                    zombies.Remove(victim_id);
                    foreach(var id in children)
                    {
                        Player child = Player.Get(id);
                        if (child != null)
                        {
                            bool adopted = zombies.Count >= 1 && !kills_as_child.ContainsKey(id);
                            child.SendBroadcast("your mother zombie died!" + (adopted ? "\nyou have infected 0 people. so you will be adopted by a different mother so you can respawn" : ""), 20, shouldClearPrevious: true);
                            zombies[Random.Range(0, zombies.Count)].Add(id);
                        }
                    }
                    if ((attacker == null || attacker == victim) && zombies.Count >= 1)
                        zombies[Random.Range(0, zombies.Count)].Add(victim_id);
                }

                if (zombies.Any((c) => c.Value.Contains(victim_id)))
                {
                    bool was_suicide = attacker == null || victim == attacker;
                    var mother = zombies.First(c => c.Value.Contains(victim_id));

                    if (kills_as_child.ContainsKey(victim_id) && kills_as_child[victim_id] >= config.CureKillsThreshold)
                    {
                        mother.Value.Remove(victim_id);
                        kills_as_child.Remove(victim_id);
                        SetScale(victim, 1.0f);
                    }
                    else
                    {
                        Timing.CallDelayed(0.2f, () =>
                        {
                            Player v = Player.Get(victim_id);
                            if (v != null && v.Role != RoleTypeId.Scp0492)
                            {
                                v.SetRole(RoleTypeId.Scp0492);
                                if (!was_suicide)
                                {
                                    Player mother_player = Player.Get(mother.Key);
                                    if (mother_player != null)
                                        mother_player.Damage((float)config.ChildrenHealthPool / mother.Value.Count, "zombie child spawn health drain");
                                }
                            }
                        });
                    }
                }
            }
        }

        [PluginEvent(ServerEventType.RagdollSpawn)]
        void OnRagdollSpawn(Player player, IRagdollRole ragdoll, DamageHandlerBase damageHandler)
        {
            if (!(damageHandler is AttackerDamageHandler))
                Timing.CallDelayed(1.0f, () => NetworkServer.Destroy(ragdoll.Ragdoll.gameObject));
        }

        //[PluginEvent(ServerEventType.PlayerShotWeapon)]
        //void OnShotWeapon(Player player, Firearm gun)
        //{
        //    Vector3 dir = -(player.ReferenceHub.PlayerCameraReference.transform.rotation * Vector3.forward);
        //    var fpm = player.GameObject.GetComponentInChildren<FirstPersonMovementModule>();
        //    Timing.CallDelayed(0.0f, () =>
        //    {
        //        fpm.CharController.Move(dir);
        //        fpm.ServerOverridePosition(fpm.CharController.transform.position, Vector3.zero);
        //    });
        //    Log.Info(dir.ToPreciseString() + " | " + fpm.CharController.transform.position.ToPreciseString());
        //}

        [PluginEvent(ServerEventType.PlayerReloadWeapon)]
        void OnReloadWeapon(Player player, Firearm gun)
        {
            if (gun.ItemTypeId != ItemType.ParticleDisruptor && gun.ItemTypeId != ItemType.GunCOM15)
                player.SetAmmo(gun.AmmoType, (ushort)player.GetAmmoLimit(gun.AmmoType));
        }

        [PluginEvent(ServerEventType.PlayerDropAmmo)]
        bool OnPlayerDropAmmo(Player player, ItemType ammoType, int amount)
        {
            return false;
        }

        private IEnumerator<float> _Checks()
        {
            while(true)
            {
                humans_alive = 0;
                zombies_alive = 0;
                foreach(var p in Player.GetPlayers())
                {
                    int id = p.PlayerId;
                    bool mother = zombies.ContainsKey(id);
                    bool infected = zombies.Any((c) => c.Value.Contains(id));
                    if (mother && infected)
                        zombies.Remove(id);
                    if (mother || infected)
                    {
                        if (p.Role == RoleTypeId.Scp0492)
                            zombies_alive++;
                        if (p.Role != RoleTypeId.Scp0492 && p.Role != RoleTypeId.Spectator && p.Role != RoleTypeId.Tutorial && p.Role != RoleTypeId.Overwatch)
                            p.SetRole(RoleTypeId.Scp0492);
                    }
                    else if (p.IsAlive)
                        humans_alive++;
                }
                yield return Timing.WaitForSeconds(1.0f);
            }
        }
    }

    public class ZombieInfectionEvent:IEvent
    {
        public static ZombieInfectionEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Zombie Infection";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription { get; set; } = "The SCPs spawn as mother zombies which have a large max HP. Zombies infect players on kill creating child zombies. Child zombies respawn next to their mother zombie and take some of their mother zombies health. The more child zombies there are the less health each one has. All players have infinite ammo and deal knockback based on the ratio of humans to zombies. Class-Ds spawn with a com-15 and 10 ammo. Scientists spawn with a com-18 with infinite ammo and an upgraded keycard. Class-Ds and Scientists may team kill to stop the spread of the infection\n\n";
        public string EventPrefix { get; } = "ZI";
        public bool OverrideWinConditions { get; }
        public bool BulletHolesAllowed { get; set; } = false;
        public PluginHandler PluginHandler { get; }
        public IEventConfig Config => EventConfig;

        [PluginConfig]
        public Config EventConfig;

        private Harmony harmony;

        public void PrepareEvent()
        {
            Log.Info(EventName + " event is preparing");
            IsRunning = true;
            harmony = new Harmony("ZombieInfectionEvent");
            harmony.PatchAll();
            EventHandler.Start();
            Log.Info(EventName + " event is prepared");
            PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
        }

        public void StopEvent()
        {
            IsRunning = false;
            harmony.UnpatchAll("ZombieInfectionEvent");
            EventHandler.Stop();
            PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
        }

        [PluginEntryPoint("Zombie Infection Event", "1.0.0", "Zombie Infection", "The Riptide")]
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

    //[CommandHandler(typeof(RemoteAdminCommandHandler))]
    //public class AddForce : ICommand
    //{
    //    public string Command { get; } = "af";
    //
    //    public string[] Aliases { get; } = new string[] { };
    //
    //    public string Description { get; } = "push self";
    //
    //    public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
    //    {
    //        Player player;
    //        if (Player.TryGet(sender, out player))
    //        {
    //            float x, y, z;
    //            if(!float.TryParse(arguments.ElementAt(0),out x))
    //            {
    //                response = "failed to parse x";
    //                return false;
    //            }
    //            if (!float.TryParse(arguments.ElementAt(1), out y))
    //            {
    //                response = "failed to parse y";
    //                return false;
    //            }
    //            if (!float.TryParse(arguments.ElementAt(2), out z))
    //            {
    //                response = "failed to parse z";
    //                return false;
    //            }
    //
    //            Vector3 dir = new Vector3(x, y, z);
    //            CharacterController c = player.GameObject.GetComponentInChildren<CharacterController>();
    //            c.Move(dir);
    //            player.Position = c.transform.position;
    //
    //            response = "success";
    //            return true;
    //        }
    //        response = "failed";
    //        return false;
    //    }
    //}
}
