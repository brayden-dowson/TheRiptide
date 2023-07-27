using Interactables.Interobjects.DoorUtils;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerRoles.PlayableScps.Subroutines;
using PlayerRoles.PlayableScps.Scp096;
using Interactables.Interobjects;
using UnityEngine;
using MEC;


namespace TheRiptide
{
    public class StrongClassD: IComparable
    {
        [PluginEntryPoint("Strong Class-D", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        static int strong_dclass = -1;

        [PluginEvent(ServerEventType.RoundStart)]
        void OnRoundStart()
        {
            strong_dclass = -1;
            int attempts = 0;
            Timing.CallDelayed(0.1f, () =>
            {
                while (strong_dclass == -1)
                {
                    Player random = Player.GetPlayers().RandomItem();
                    if (random.Role == RoleTypeId.ClassD && !random.TemporaryData.Contains("custom_class"))
                    {
                        strong_dclass = random.PlayerId;
                        random.TemporaryData.Add("custom_class", this);
                        random.SendBroadcast("[Strong Class-D] you can pry gates open at the cost of your health", 15, shouldClearPrevious: true);
                    }
                    else
                    {
                        attempts++;
                        if (attempts >= 100)
                            break;
                    }
                }
            });
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        void OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId newRole, RoleChangeReason reason)
        {
            if(player != null && player.PlayerId == strong_dclass && newRole != RoleTypeId.ClassD)
            {
                strong_dclass = -1;
                player.TemporaryData.Remove("custom_class");
            }
        }

        [PluginEvent(ServerEventType.PlayerInteractDoor)]
        void OnPlayerInteractDoor(Player player, DoorVariant door, bool canOpen)
        {
            if(player != null && player.PlayerId == strong_dclass && !canOpen && !door.IsConsideredOpen() && door is PryableDoor gate && door.ActiveLocks != 0)
            {
                Vector3 relative_position = gate.transform.InverseTransformPoint(player.Position);
                if (Vector3.Distance(Vector3.zero, relative_position) < 4.0f)
                { 
                    gate.TryPryGate(player.ReferenceHub);
                    Timing.RunCoroutine(_PryAnimation(player, gate));
                }
            }
        }

        private double sig(double x, double b)
        {
            x = Math.Min(x, 1.0f);
            double x_b = Math.Pow(x, b);
            return x_b / (x_b + Math.Pow(1.0 - x, b));

            //x ^ b / (x ^ b + (1 - x) ^ b)
        }

        public IEnumerator<float> _PryAnimation(Player player, PryableDoor gate)
        {
            player.EffectsManager.ChangeState<CustomPlayerEffects.Ensnared>(1, 2.0f);
            player.EffectsManager.ChangeState<CustomPlayerEffects.CardiacArrest>(1, 1.0f);
            player.EffectsManager.ChangeState<CustomPlayerEffects.Exhausted>(1, 2.0f);
            player.EffectsManager.ChangeState<CustomPlayerEffects.Asphyxiated>(1, 5.0f);
            float t = 0.0f;

            float setup_time = 1.0f;
            float passthrough_time = 1.0f;

            Vector3 previous_position = player.Position;
            Vector3 target_position;
            Vector3 relative_position = gate.transform.InverseTransformPoint(player.Position);
            if (relative_position.z > 0.0f)
                target_position = new Vector3(0.0f, 1.64f, 1.0f);
            else
                target_position = new Vector3(0.0f, 1.64f, -1.0f);
            target_position = gate.transform.TransformPoint(target_position);

            while (t < setup_time)
            {
                t += Timing.DeltaTime;
                float x = (float)sig(t / setup_time, 1.3);
                Vector3 new_pos = previous_position * (1.0f - x) + target_position * x;
                player.Position = new Vector3(new_pos.x, player.Position.y, new_pos.z);
                yield return Timing.WaitForOneFrame;
            }

            player.EffectsManager.ChangeState<CustomPlayerEffects.CardiacArrest>(1, 1.0f);
            player.EffectsManager.ChangeState<CustomPlayerEffects.InsufficientLighting>(1, 0.05f);
            player.EffectsManager.ChangeState<CustomPlayerEffects.Blinded>(1, 0.25f);

            t = 0.0f;
            previous_position = target_position;
            if (relative_position.z > 0.0f)
                target_position = new Vector3(0.0f, 1.64f, -1.0f);
            else
                target_position = new Vector3(0.0f, 1.64f, 1.0f);
            target_position = gate.transform.TransformPoint(target_position);

            while (t < passthrough_time)
            {
                t += Timing.DeltaTime;
                float x = (float)sig(t / passthrough_time, 1.3);
                Vector3 new_pos = previous_position * (1.0f - x) + target_position * x;
                player.Position = new Vector3(new_pos.x, player.Position.y, new_pos.z);
                yield return Timing.WaitForOneFrame;
            }

        }

        public int CompareTo(object other)
        {
            return Comparer<StrongClassD>.Default.Compare(this, other as StrongClassD);
        }
    }
}
