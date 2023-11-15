using CommandSystem;
using CustomPlayerEffects;
using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp3114;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public class Follow
    {
        public static Follow Singleton { get; private set; }
        public static bool normal_round;

        private int target = -1;
        private int follower = -1;
        private CoroutineHandle update;

        [PluginEntryPoint("Follow", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            Singleton = this;
            EventManager.RegisterEvents(this);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart(RoundStartEvent e)
        {
            normal_round = true;
            Type event_manager_type = GetType("CedMod.Addons.Events.EventManager");
            if (event_manager_type != null && event_manager_type.GetField("CurrentEvent", BindingFlags.Public | BindingFlags.Static).GetValue(null) != null)
            {
                normal_round = false;
                return;
            }

            update = Timing.RunCoroutine(_Update());
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(PlayerJoinedEvent e)
        {
            if (!normal_round)
                return;

            if (follower == e.Player.PlayerId)
                follower = -1;
            if (target == e.Player.PlayerId)
                target = -1;
        }

        [PluginEvent(ServerEventType.PlayerDamage)]
        void OnPlayerDamage(PlayerDamageEvent e)
        {
            if (!normal_round)
                return;

            if (e.DamageHandler is Scp3114DamageHandler handler)
            {
                if (handler.Attacker.PlayerId == Server.Instance.PlayerId && handler.Subtype == Scp3114DamageHandler.HandlerType.Strangulation)
                    handler.Damage = 0;
            }
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(PlayerLeftEvent e)
        {
            if (!normal_round)
                return;

            if (follower == e.Player.PlayerId)
                follower = -1;
            if (target == e.Player.PlayerId)
                target = -1;
        }

        [PluginEvent(ServerEventType.RoundEnd)]
        void OnRoundEnd(RoundEndEvent e)
        {
            Timing.KillCoroutines(update);
        }

        [PluginEvent(ServerEventType.RoundRestart)]
        void OnRoundRestart(RoundRestartEvent e)
        {
            Timing.KillCoroutines(update);
        }

        public void Addfollower(Player target, Player follower)
        {
            this.target = target.PlayerId;
            this.follower = follower.PlayerId;
        }

        public void Unfollow()
        {
            target = -1;
            follower = -1;
        }

        private IEnumerator<float> _Update()
        {
            //int previous_follower = follower;
            while(true)
            {
                try
                {
                    if (target == -1 && follower == -1)
                    {
                        if (Server.Instance.Role != RoleTypeId.None)
                            Server.Instance.SetRole(RoleTypeId.None);
                        else
                            goto skip;
                    }
                    else
                    {
                        if (Server.Instance.Role != RoleTypeId.Scp3114)
                        {
                            //Server.Instance.GameObject.transform.localScale = Vector3.zero;
                            Server.Instance.SetRole(RoleTypeId.Scp3114);
                            Server.Instance.EffectsManager.EnableEffect<Invisible>();
                            goto skip;
                        }
                    }

                    Scp3114StrangleAudio audio;
                    Scp3114Strangle strangle;
                    if (!(Server.Instance.RoleBase is Scp3114Role scp3114) || !scp3114.SubroutineModule.TryGetSubroutine(out strangle) || !scp3114.SubroutineModule.TryGetSubroutine(out audio))
                        goto skip;

                    Player target_player;
                    Player follower_player;
                    if (!Player.TryGet(target, out target_player) || !Player.TryGet(follower, out follower_player))
                        goto skip;
                    float dist = Mathf.Min(3.0f, Vector3.Distance(target_player.Position, follower_player.Position));
                    MovementBoost boost;
                    follower_player.ReferenceHub.playerEffectsController.TryGetEffect(out boost);
                    boost._intensity = 255;
                    boost._duration = 0.1f;
                    follower_player.EffectsManager.EnableEffect<Strangled>(1);
                    strangle.SyncTarget = new Scp3114Strangle.StrangleTarget(follower_player.ReferenceHub, strangle.GetStranglePosition(follower_player.RoleBase as HumanRole), strangle.CastRole.FpcModule.Position);
                    strangle.ServerSendRpc(true);
                    audio._syncKillTime = 0.0f;
                    audio.ServerSendRpc(Scp3114StrangleAudio.RpcType.ChokeSync);
                    Server.Instance.Position = follower_player.Position + Vector3.Normalize(target_player.Position - follower_player.Position) * dist;
                }
                catch(Exception ex)
                {
                    target = -1;
                    follower = -1;
                    Log.Error(ex.ToString());
                }
                skip:
                yield return Timing.WaitForOneFrame;
            }
        }

        public static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class FollowCmd : ICommand
    {
        public string Command { get; } = "follow";

        public string[] Aliases { get; } = new string[] {};

        public string Description { get; } = "make player follow you. usage: follow <player_id> | follow <player_id> <target_id>";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Follow.normal_round)
            {
                response = "Follow disabled during CedMod events as it may cause bugs";
                return false;
            }

            if (arguments.Count == 0 || arguments.Count > 2)
            {
                response = "To execute this command provide 1 or 2 arguments!\nUsage: follow <player_id> | follow <player_id> <target_id>";
                return false;
            }

            int id;
            if(!int.TryParse(arguments.At(0), out id))
            {
                response = "Failed: couldnt parse argument 1 as an int. recieved: " + arguments.At(0);
                return false;
            }

            Player player;
            if(!Player.TryGet(id, out player))
            {
                response = "Failed: no player matching id: " + id;
                return false;
            }

            Player target;
            if (arguments.Count >= 2)
            {
                int target_id;
                if(!int.TryParse(arguments.At(1), out target_id))
                {
                    response = "Failed: couldnt parse argument 2 as an int. recieved: " + arguments.At(1);
                    return false;
                }
                if (!Player.TryGet(target_id, out target))
                {
                    response = "Failed: no player matching id: " + target_id;
                    return false;
                }
            }
            else
            {
                if (!Player.TryGet(sender, out target))
                {
                    response = "Failed: command sender not a player";
                    return false;
                }
            }

            Follow.Singleton.Addfollower(target, player);
            response = "Success: player " + player.Nickname + " is now following " + target.Nickname;
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class UnfollowCmd : ICommand
    {
        public string Command { get; } = "unfollow";

        public string[] Aliases { get; } = new string[] { };

        public string Description { get; } = "remove follower";

        public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Follow.normal_round)
            {
                response = "Follow disabled during CedMod events as it may cause bugs";
                return false;
            }

            Follow.Singleton.Unfollow();
            response = "Success";
            return true;
        }
    }
}
