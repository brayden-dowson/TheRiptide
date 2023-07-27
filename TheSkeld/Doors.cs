using AdminToys;
using Interactables.Interobjects;
using slocLoader;
using slocLoader.Objects;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    [RequireComponent(typeof(BreakableDoor))]
    public class DoorSkin: MonoBehaviour
    {
        public BreakableDoor door_base;
        private PrimitiveObjectToy left_skin;
        private PrimitiveObjectToy right_skin;

        public void Start()
        {
            door_base = GetComponent<BreakableDoor>();
            PrimitiveObject left_po = new PrimitiveObject(ObjectType.Cube);
            left_po.Transform.Position = door_base.transform.position + (Vector3.up * 1.5f);
            left_po.Transform.Rotation = door_base.transform.rotation;
            left_po.ColliderMode = PrimitiveObject.ColliderCreationMode.NoCollider;
            left_po.Transform.Scale = new Vector3(1.75f, 3.0f, 0.25f);
            left_po.MaterialColor = new Color(52 / 255.0f, 54 / 255.0f, 66 / 255.0f);
            left_skin = left_po.SpawnObject().GetComponent<PrimitiveObjectToy>();

            PrimitiveObject right_po = new PrimitiveObject(ObjectType.Cube);
            right_po.Transform.Position = door_base.transform.position + (Vector3.up * 1.5f);
            right_po.Transform.Rotation = door_base.transform.rotation;
            right_po.ColliderMode = PrimitiveObject.ColliderCreationMode.NoCollider;
            right_po.Transform.Scale = new Vector3(1.75f, 3.0f, 0.25f);
            right_po.MaterialColor = new Color(52 / 255.0f, 54 / 255.0f, 66 / 255.0f);
            right_skin = right_po.SpawnObject().GetComponent<PrimitiveObjectToy>();
        }

        void Update()
        {
            if (door_base.TargetState)
            {
                left_skin.NetworkMovementSmoothing = 3;
                right_skin.NetworkMovementSmoothing = 3;
            }
            else
            {
                left_skin.NetworkMovementSmoothing = 10;
                right_skin.NetworkMovementSmoothing = 10;
            }

            Vector3 origin = door_base.transform.position + (Vector3.up * 1.5f);
            Vector3 left_closed_pos = door_base.transform.rotation * (Vector3.left * 0.875f);
            Vector3 left_opened_pos = door_base.transform.rotation * (Vector3.left * 2.375f);
            left_skin.transform.position = origin + Vector3.Lerp(left_closed_pos, left_opened_pos, door_base.TargetState ? 1.0f : 0.0f);
            Vector3 right_closed_pos = door_base.transform.rotation * (Vector3.right * 0.875f);
            Vector3 right_opened_pos = door_base.transform.rotation * (Vector3.right * 2.375f);
            right_skin.transform.position = origin + Vector3.Lerp(right_closed_pos, right_opened_pos, door_base.TargetState ? 1.0f : 0.0f);
        }
    }
}
