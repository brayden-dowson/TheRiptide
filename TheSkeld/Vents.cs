using AdminToys;
using Interactables.Interobjects;
using MEC;
using Mirror;
using PluginAPI.Core;
using slocLoader;
using slocLoader.Objects;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public class VentSkin:MonoBehaviour
    {
        public BreakableDoor door_base;
        private PrimitiveObjectToy skin;
        private int opened_count = 0;

        public void Start()
        {
            door_base = GetComponent<BreakableDoor>();
            door_base._nonInteractable = true;
            PrimitiveObject po = new PrimitiveObject(ObjectType.Cube);
            po.Transform.Position = door_base.transform.position;
            po.Transform.Rotation = door_base.transform.rotation;
            po.ColliderMode = PrimitiveObject.ColliderCreationMode.NoCollider;
            po.Transform.Scale = new Vector3(1.0f, 0.125f, 1.0f);
            po.MaterialColor = new Color(175 / 255.0f, 199 / 255.0f, 209 / 255.0f);
            skin = po.SpawnObject().GetComponent<PrimitiveObjectToy>();
            skin.NetworkMovementSmoothing = 20;

            Vector3 origin = door_base.transform.position + new Vector3(0.0f, 0.0f, 0.5f);
            skin.transform.position = origin;
            skin.transform.rotation = Quaternion.identity;
        }

        public void OnPlayerUse(Player player)
        {
            Open();
            Timing.RunCoroutine(_VentAnimation(player));
            Timing.CallDelayed(0.5f, () => Close());
        }

        private void Open()
        {
            if (opened_count == 0)
            {
                Vector3 origin = door_base.transform.position + new Vector3(0.0f, 0.0f, 0.5f);
                Vector3 opened_pos = origin + (Vector3.back * 0.5f) + (Vector3.up * 0.5f);
                skin.transform.position = opened_pos;
                skin.transform.rotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
            }
            opened_count++;
        }

        private void Close()
        {
            opened_count--;
            if (opened_count == 0)
            {
                Vector3 origin = door_base.transform.position + new Vector3(0.0f, 0.0f, 0.5f);
                skin.transform.position = origin;
                skin.transform.rotation = Quaternion.identity;
            }
        }

        private IEnumerator<float> _VentAnimation(Player player)
        {
            RDM.EndGrace(player);
            RespawnForPlayerAtOffset(door_base.netIdentity, player, new Vector3(1000.0f, 1000.0f, 1000.0f));
            bool is_going_up = player.Position.y < door_base.transform.position.y;
            const float setup_time = 0.25f;
            float x = 0.0f;

            Vector3 start_pos = player.Position;
            Vector3 end_pos = is_going_up ? door_base.transform.position + new Vector3(0.0f, -1.3f, 0.5f) : door_base.transform.position + new Vector3(0.0f, 1.0f, 0.5f);
            while (x < setup_time)
            {
                player.Position = Vector3.Lerp(start_pos, end_pos, x * (1.0f / setup_time));
                yield return Timing.WaitForOneFrame;
                x += Timing.DeltaTime;
            }

            const float transition_time = 0.5f;
            x = 0.0f;
            start_pos = end_pos;
            end_pos = is_going_up ? door_base.transform.position + new Vector3(0.0f, 1.0f, 0.5f) : door_base.transform.position + new Vector3(0.0f, -1.3f, 0.5f);
            while (x < transition_time)
            {
                player.Position = Vector3.Lerp(start_pos, end_pos, x * (1.0f / transition_time));
                yield return Timing.WaitForOneFrame;
                x += Timing.DeltaTime;
            }
            RespawnForPlayerAtOffset(door_base.netIdentity, player, Vector3.zero);
        }

        private static void RespawnForPlayerAtOffset(NetworkIdentity identity, Player player, Vector3 offset)
        {
            NetworkConnection conn = player.Connection;
            using (NetworkWriterPooled ownerWriter = NetworkWriterPool.Get())
            {
                using (NetworkWriterPooled observersWriter = NetworkWriterPool.Get())
                {
                    System.ArraySegment<byte> spawnMessagePayload = NetworkServer.CreateSpawnMessagePayload(false, identity, ownerWriter, observersWriter);
                    SpawnMessage message = new SpawnMessage()
                    {
                        netId = identity.netId,
                        isLocalPlayer = conn.identity == identity,
                        isOwner = false,
                        sceneId = identity.sceneId,
                        assetId = identity.assetId,
                        position = identity.transform.localPosition - offset,
                        rotation = identity.transform.localRotation,
                        scale = identity.transform.localScale,
                        payload = spawnMessagePayload
                    };
                    conn.Send(message);
                }
            }
        }
    }
}
