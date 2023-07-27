using AdminToys;
using Interactables.Interobjects;
using MEC;
using PluginAPI.Core;
using slocLoader;
using slocLoader.Objects;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public class LeverSkin:MonoBehaviour
    {
        public bool State { get; private set; }
        public BreakableDoor door_base;
        public ISabotage sabotage;
        private GameObject root_obj;
        private Stopwatch cool_down = new Stopwatch();
        private const float cool_down_time = 1.0f;

        public LeverSkin()
        {
            door_base = GetComponent<BreakableDoor>();
            List<slocGameObject> lever = new List<slocGameObject>();
            var l1 = new PrimitiveObject(ObjectType.Cube);
            l1.ColliderMode = PrimitiveObject.ColliderCreationMode.NoCollider;
            l1.Transform.Scale = new Vector3(0.05f, 0.25f, 0.05f);
            l1.Transform.Position = new Vector3(0.07f, 0.125f, 0.0f);
            l1.MaterialColor = new Color(150 / 255.0f, 165 / 255.0f, 176 / 255.0f);

            var l2 = new PrimitiveObject(ObjectType.Cube);
            l2.ColliderMode = PrimitiveObject.ColliderCreationMode.NoCollider;
            l2.Transform.Scale = new Vector3(0.05f, 0.25f, 0.05f);
            l2.Transform.Position = new Vector3(-0.07f, 0.125f, 0.0f);
            l2.MaterialColor = new Color(150 / 255.0f, 165 / 255.0f, 176 / 255.0f);

            var l3 = new PrimitiveObject(ObjectType.Cube);
            l3.ColliderMode = PrimitiveObject.ColliderCreationMode.NoCollider;
            l3.Transform.Scale = new Vector3(0.3f, 0.09f, 0.09f);
            l3.Transform.Position = new Vector3(0.0f, 0.3f, 0.0f);
            l3.MaterialColor = new Color(145 / 255.0f, 32 / 255.0f, 17 / 255.0f);

            var root = new EmptyObject();
            root.Transform.Position = door_base.transform.position + new Vector3(0.0f, 0.25f, 0.0f);
            root.Transform.Rotation = Quaternion.Euler(-45.0f, door_base.transform.rotation.eulerAngles.y, 0.0f);

            root_obj = API.SpawnObject(root);
            API.SpawnObject(l1, root_obj).GetComponent<PrimitiveObjectToy>().NetworkMovementSmoothing = 30;
            API.SpawnObject(l2, root_obj).GetComponent<PrimitiveObjectToy>().NetworkMovementSmoothing = 30;
            API.SpawnObject(l3, root_obj).GetComponent<PrimitiveObjectToy>().NetworkMovementSmoothing = 30;

        }

        public bool TryEnable(Player player)
        {
            float cd_time = Mathf.Max(cool_down_time, sabotage.ActivationCoolDown);
            if (cool_down.IsRunning && cool_down.Elapsed.TotalSeconds >= cd_time)
                cool_down.Stop();

            if (cool_down.IsRunning || State == true)
            {
                BroadcastOverride.BroadcastLine(player, 6, 5.0f, BroadcastPriority.Medium, "<b>System cooling down: " + (cd_time - cool_down.Elapsed.TotalSeconds).ToString("0") + "</b>");
                BroadcastOverride.UpdateIfDirty(player);
                return false;
            }

            cool_down.Restart();

            ForceEnable(player);
            RDM.EndGrace(player);
            return true;
        }

        public void ForceEnable(Player enabler)
        {
            root_obj.transform.rotation = Quaternion.Euler(-135.0f, door_base.transform.rotation.eulerAngles.y, 0.0f);
            State = true;
            sabotage.Enable(enabler);
            if (sabotage.AutoResetTime > 0.0f)
                Timing.CallDelayed(sabotage.AutoResetTime, () =>
                {
                    if (State == true)
                    {
                        root_obj.transform.rotation = Quaternion.Euler(-45.0f, door_base.transform.rotation.eulerAngles.y, 0.0f);
                        State = false;
                    }
                });
        }

        public bool TryDisable(Player player)
        {
            if (cool_down.IsRunning && cool_down.Elapsed.TotalSeconds >= cool_down_time)
                cool_down.Stop();

            if (cool_down.IsRunning || State == false)
                return false;

            cool_down.Restart();
            if (TraitorAmongUs.detectives.Contains(player.PlayerId))
                Shop.RewardCash(player, 50, "<b><color=#00FF00>$50</color> reward for stopping a sabotage! Check shop for options</b>");
            ForceDisable();
            return true;
        }

        public void ForceDisable()
        {
            root_obj.transform.rotation = Quaternion.Euler(-45.0f, door_base.transform.rotation.eulerAngles.y, 0.0f);
            State = false;
            sabotage.Disable();
        }
    }
}
