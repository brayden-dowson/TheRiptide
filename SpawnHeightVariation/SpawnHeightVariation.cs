using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mirror;
using MEC;
using System.ComponentModel;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public class Config
    {
        [Description("very low values(below 0.1) can cause players to freeze and must dc and rc to unfreeze")]
        public float Min { get; set; } = 0.95f;
        public float Max { get; set; } = 1.05f;
    }

    public class SpawnHeightVariation
    {
        [PluginConfig]
        public Config Config;

        HashSet<RoleTypeId> human_roles = new HashSet<RoleTypeId>
        {
            RoleTypeId.ClassD,
            RoleTypeId.Scientist,
            RoleTypeId.FacilityGuard,
            RoleTypeId.NtfPrivate,
            RoleTypeId.NtfSergeant,
            RoleTypeId.NtfSpecialist,
            RoleTypeId.NtfCaptain,
            RoleTypeId.ChaosConscript,
            RoleTypeId.ChaosMarauder,
            RoleTypeId.ChaosRepressor,
            RoleTypeId.ChaosRifleman,
        };

        [PluginEntryPoint("Spawn Height Variation", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        void OnPlayerSpawn(Player player, RoleTypeId role)
        {
            if (player != null)
            {
                if (human_roles.Contains(role))
                {
                    Random random = new Random();
                    float x = (float)random.NextDouble();
                    float scale = Config.Min * (1.0f - x) + Config.Max * x;
                    SetScale(player, scale);
                    player.SendConsoleMessage("scale set to: " + scale.ToString());
                    //Log.Info("human role scaled: " + scale.ToString());
                }
                else if (role != RoleTypeId.None && role != RoleTypeId.Spectator && role != RoleTypeId.Overwatch)
                {
                    if (player.GameObject.transform.localScale != new UnityEngine.Vector3(1.0f, 1.0f, 1.0f))
                    {
                        SetScale(player, 1.0f);
                        Log.Info("role scale reset: ");
                    }
                }
            }
        }
    }
}
