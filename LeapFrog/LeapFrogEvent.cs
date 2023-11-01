using CedMod;
using CedMod.Addons.Events;
using CedMod.Addons.Events.Interfaces;
using CustomPlayerEffects;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp939;
using PlayerRoles.PlayableScps.Scp939.Ripples;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using Respawning;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheRiptide.EventUtility;

namespace TheRiptide
{
    public sealed class Config
    {
    }

    public sealed class CedModConfig : IEventConfig
    {
        [Description("Indicates whether the event is enabled or not")]
        public bool IsEnabled { get; set; } = true;
        public Config config { get; set; } = new Config();
    }

    //public sealed class AutoConfig : AutoEvent.Interfaces.EventConfig
    //{
    //    public Config config { get; set; } = new Config();
    //}

    public sealed class Translation
    {
        public string Name { get; set; } = "Leap Frog";
        public string Description { get; set; } = "Everyone will spawn in doctors chamber with the elevators locked. All SCPs will become 939 and everyone else Class-D. The 939s are ensnared so they can only use their lunge ability to attack. A new dog spawns in every minute. Class-D get an adrenaline and painkillers. There is a 50% chance the lights will be out and be given flashlights. The last Class-D alive wins!\n\n";
    }

    public class EventHandler
    {
        public static EventHandler Singleton { get; private set; }

        private static HashSet<int> dogs = new HashSet<int>();
        public static bool found_winner;
        private static RoomIdentifier scp049;
        private static bool lights_out;
        private static CoroutineHandle spawn;

        public EventHandler()
        {
            Singleton = this;
        }

        public static void Start()
        {
            lights_out = Random.value >= 0.5f;
            found_winner = false;
            WinnerReset();
            dogs.Clear();
            scp049 = null;

            if(Round.IsRoundStarted)
            {
                Log.Info("Late start");
                Singleton.OnRoundStart();
                foreach(var p in Player.GetPlayers())
                {
                    if (p.IsReady)
                    {
                        Singleton.OnPlayerChangeRole(p, null, p.Role, RoleChangeReason.RoundStart);
                        Singleton.OnPlayerSpawn(p, p.Role);
                    }
                }
            }
        }

        public static void Stop()
        {
            found_winner = false;
            WinnerReset();
            dogs.Clear();
            scp049 = null;
            Timing.KillCoroutines(spawn);
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            player.SendBroadcast("Event being played: " + LeapFrogEvent.Singleton.EventName + "\n<size=32>" + LeapFrogEvent.Singleton.EventDescription + "</size>", 30, shouldClearPrevious: true);
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            if (player == null || !Round.IsRoundStarted)
                return;
            if(dogs.Contains(player.PlayerId))
            {
                List<Player> available = Player.GetPlayers().Where(p => p.Role == RoleTypeId.Spectator).ToList();
                if (available.IsEmpty())
                    available = Player.GetPlayers().Where(p => !dogs.Contains(p.PlayerId) && (p.Role == RoleTypeId.ClassD || p.Role == RoleTypeId.Spectator)).ToList();
                if (available.IsEmpty())
                    return;
                Player selected = available.RandomItem();
                dogs.Add(selected.PlayerId);
                selected.SetRole(RoleTypeId.Scp939);
            }
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            EndRoom = RoomIdentifier.AllRoomIdentifiers.Where((r) => r.Zone == FacilityZone.Surface).First();
            RoomOffset = new Vector3(40.000f, 14.080f, -32.600f);

            scp049 = RoomIdentifier.AllRoomIdentifiers.First(r => r.Name == RoomName.Hcz049);
            foreach (var door in ElevatorDoor.AllElevatorDoors[ElevatorManager.ElevatorGroup.Scp049])
                FacilityManager.LockDoor(door, DoorLockReason.AdminCommand);

            spawn = Timing.CallPeriodically(30.0f * 60.0f, 30.0f,()=>
            {
                List<Player> spectators = Player.GetPlayers().Where(p => p.Role == RoleTypeId.Spectator).ToList();
                if (!spectators.IsEmpty())
                {
                    Player selected = spectators.RandomItem();
                    dogs.Add(selected.PlayerId);
                    selected.SetRole(RoleTypeId.Scp939);
                }
            });

            if (lights_out)
            {
                Timing.CallDelayed(3.0f, () =>
                {
                    foreach (var controller in scp049.gameObject.GetComponentsInChildren<RoomLightController>())
                        controller.NetworkLightsEnabled = false;
                });
            }
        }

        [PluginEvent(ServerEventType.TeamRespawn)]
        bool OnRespawn(SpawnableTeamType team, List<Player> players, int max)
        {
            return false;
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        bool OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId new_role, RoleChangeReason reason)
        {
            if (player == null || !Round.IsRoundStarted || new_role == RoleTypeId.Spectator || new_role == RoleTypeId.Overwatch || new_role == RoleTypeId.Tutorial || new_role == RoleTypeId.Filmmaker)
                return true;

            if (found_winner)
                return HandleGameOverRoleChange(player, new_role);

            if(new_role.GetTeam() == Team.SCPs)
            {
                dogs.Add(player.PlayerId);
                if(new_role != RoleTypeId.Scp939)
                {
                    Timing.CallDelayed(0.0f,()=>
                    {
                        player.SetRole(RoleTypeId.Scp939);
                    });
                    return false;
                }
            }
            else
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

            if (found_winner)
            {
                HandleGameOverSpawn(player);
                return;
            }

            if(role == RoleTypeId.Scp939)
            {
                Timing.CallDelayed(0.0f,()=>
                {
                    if (player.Role != RoleTypeId.Scp939)
                        return;
                    Teleport.RoomPos(player, scp049, new Vector3(15.240f, 197.765f, 10.147f));
                    player.EffectsManager.EnableEffect<Ensnared>();
                    player.SendBroadcast("you are ensnared, press and hold C to lunge", 30, shouldClearPrevious: true);
                });
            }
            else if(role == RoleTypeId.ClassD)
            {
                Timing.CallDelayed(0.0f, () =>
                {
                    if (player.Role != RoleTypeId.ClassD)
                        return;
                    Teleport.RoomPos(player, scp049, new Vector3(1.173f, 197.765f, 10.170f));
                    player.ClearInventory();
                    player.AddItem(ItemType.Adrenaline);
                    player.AddItem(ItemType.Painkillers);
                    if (lights_out)
                        player.AddItem(ItemType.Flashlight);
                });
            }
        }

        [PluginEvent(ServerEventType.PlayerDeath)]
        void OnPlayerDeath(Player victim, Player attacker, DamageHandlerBase damageHandler)
        {
            if (victim == null || !Round.IsRoundStarted)
                return;

            if (!found_winner)
                found_winner = WinConditionLastClassD(victim);
        }
    }

    public class LeapFrogEvent:IEvent
    {
        public static LeapFrogEvent Singleton { get; private set; }

        public static bool IsRunning = false;
        public PluginHandler Handler;

        public string EventName { get; } = "Leap Frog";
        public string EvenAuthor { get; } = "The Riptide";
        public string EventDescription
        {
            get { return Translation == null ? "Translation not loaded" : Translation.Description; }
            set { if (Translation != null) Translation.Description = value; else Log.Error("Translation null when setting value"); }
        }
        public string EventPrefix { get; } = "LF";
        public bool OverrideWinConditions { get; }
        public bool BulletHolesAllowed { get; set; } = false;
        public PluginHandler PluginHandler { get; }
        public IEventConfig Config => EventConfig;

        [PluginConfig]
        public CedModConfig EventConfig;

        [PluginConfig("translation.yml")]
        public Translation Translation;

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

        [PluginEntryPoint("Leap Frog Event", "1.0.0", "", "The Riptide")]
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

    //public class LeapFrogAutoEvent : AutoEvent.Interfaces.Event
    //{
    //    [AutoEvent.Interfaces.EventConfig]
    //    public AutoConfig Config { get; set; } = new AutoConfig();

    //    [AutoEvent.Interfaces.EventConfig]
    //    private Translation Translation { get; set; } = new Translation();

    //    public override string Name { get { return Translation.Name; } set { Translation.Name = value; } }
    //    public override string Description { get { return Translation.Description; } set { Translation.Description = value; } }
    //    public override string Author { get; set; } = "The Riptide";
    //    public override string CommandName { get; set; } = "LF";
    //    public override bool AutoLoad { get; protected set; } = true;
    //    protected override float PostRoundDelay { get; set; } = 0.0f;

    //    protected override void OnStart()
    //    {
    //        Log.Info(Name + " event is preparing");
    //        PluginAPI.Events.EventManager.RegisterEvents<EventHandler>(this);
    //        EventHandler.Start();
    //        Log.Info(Name + " event is prepared");
    //    }

    //    protected override bool IsRoundDone()
    //    {
    //        return EventHandler.found_winner;
    //    }

    //    protected override void OnFinished()
    //    {
    //        Log.Info(Name + " event is ending");
    //        EventHandler.Stop();
    //        PluginAPI.Events.EventManager.UnregisterEvents<EventHandler>(this);
    //        Log.Info(Name + " event is ended");
    //    }
    //}
}
