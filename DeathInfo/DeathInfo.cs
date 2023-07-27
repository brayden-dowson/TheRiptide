using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide
{
    public class DeathInfo
    {
        private static bool normal_round;
        private static Action<BasicRagdoll> on_spawned;

        [PluginEntryPoint("Death Info", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
            on_spawned = (ragdoll) =>
            {
                if (!normal_round)
                    return;

                if (ragdoll.Info.Handler is AttackerDamageHandler attack_handler)
                {
                    RagdollData p = ragdoll.Info;
                    ragdoll.NetworkInfo = new RagdollData(p.OwnerHub, p.Handler, p.RoleType, p.StartPosition, p.StartRotation, p.Nickname + "\n killed by " + attack_handler.Attacker.Nickname + "\n", p.CreationTime);
                }
            };

            RagdollManager.OnRagdollSpawned += on_spawned;
        }

        [PluginUnload]
        public void OnDisable()
        {
            PluginAPI.Events.EventManager.UnregisterEvents(this);
            RagdollManager.OnRagdollSpawned -= on_spawned;
        }

        [PluginEvent(ServerEventType.RoundStart)]
        public void OnRoundStart()
        {
            normal_round = true;
            Type event_manager_type = Utility.GetType("CedMod.Addons.Events.EventManager");
            if (event_manager_type != null && event_manager_type.GetField("CurrentEvent", BindingFlags.Public | BindingFlags.Static).GetValue(null) != null)
                normal_round = false;
        }
    }
}
