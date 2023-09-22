using AdminToys;
using Hints;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Coin;
using InventorySystem.Items.Pickups;
using MapGeneration;
using MEC;
using Mirror;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using slocLoader;
using slocLoader.Objects;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheRiptide.Utility;

namespace TheRiptide
{
    public class VendingMachine
    {
        private static GameObject button_prefab;

        private static Vector3 item_limits = new Vector3(0.234375f, 0.28125f, 0.1875f);
        private static Vector3 limits = new Vector3(0.234375f, 0.28125f, 0.3725f);
        private static Vector3 offset = new Vector3(-0.5625f, 0.671875f, 0.3125f);
        private static Vector2 stride = new Vector3(0.25f, 0.3125f);
        private const float space_z = 0.015625f;

        private static Vector3 div_size = new Vector3(0.015625f, 0.125f, 0.375f);
        private static Vector3 div_offset = new Vector3(-0.6875f, 0.734375f, 0.5f);
        private static Color plastic_color = new Color(0.8531322f, 0.8584906f, 0.7896494f);

        private static Vector3 pusher_size = new Vector3(0.1875f, 0.125f, 0.0078125f);
        private const float pusher_gap_y = 0.0625f;

        private static Vector3 button_offset = new Vector3(0.53125f, 1.78125f, 0.0156246424f);
        private const float button_stride_y = -0.15625f;
        private static List<slocGameObject> model = null;

        public struct ChamberID { public int x; public int y; }
        public class Chamber 
        { 
            public List<GameObject> stock = new List<GameObject>();
            public List<GameObject> pushers = new List<GameObject>();
        }
        public class Button
        {
            public PickupStandardPhysics display_item;
            public ItemType type;
            public LightSourceToy light;
            public int price;
        }

        private GameObject game_object;
        private Dictionary<ItemType, Dictionary<ChamberID, Chamber>> stock = new Dictionary<ItemType, Dictionary<ChamberID, Chamber>>();
        private Dictionary<Collider, Button> buttons = new Dictionary<Collider, Button>();
        private CoroutineHandle dispense;
        private CoroutineHandle spinner;
        private CoroutineHandle hint_display;
        private HashSet<int> previously_inside;

        static VendingMachine()
        {
            if (model == null && !slocLoader.AutoObjectLoader.AutomaticObjectLoader.TryGetObjects("VendingMachine", out model))
            {
                Log.Error("could not load VendingMachine.sloc make sure you add it too slocLoader/Objects");
                return;
            }
        }

        public VendingMachine(Vector3 offset, float rot_y, RoomIdentifier room, Dictionary<ItemType, ItemInfo> items)
        {
            game_object = API.SpawnObjects(model, room.transform.TransformPoint(offset), Quaternion.Euler(0.0f, rot_y, 0.0f));

            PrimitiveObject divider = new PrimitiveObject(ObjectType.Cube);
            divider.ColliderMode = PrimitiveObject.ColliderCreationMode.ClientOnly;
            divider.Transform.Scale = div_size;
            divider.MaterialColor = plastic_color;

            for (int y = 0; y < 4; y++)
            {
                divider.Transform.Position = div_offset + new Vector3(0.0f, stride.y * y, 0.0f);
                divider.SpawnObject(game_object);
                for (int x = 0; x < 4;)
                {
                    var found = items.Where(i => i.Value.Chambers.Any(c => c.X == x && c.Y == y));
                    if (found.IsEmpty())
                        continue;
                    var selected = found.ToList().RandomItem();
                    ItemType type = selected.Key;
                    ItemInfo info = selected.Value;
                    ChamberInfo chamber = info.Chambers.First(c => c.X == x && c.Y == y);
                    int amount = Random.Range(chamber.Min, chamber.Max + 1);
                    if (amount > 0)
                        x += AddStock(type, Quaternion.Euler(info.RotX, info.RotY, info.RotZ), info.Scale, new ChamberID { x = x, y = y }, amount);
                    else
                        x++;
                    divider.Transform.Position = div_offset + new Vector3(stride.x * x, stride.y * y, 0.0f);
                    divider.SpawnObject(game_object);
                }
            }

            foreach (var info in items)
                AddButton(info.Key, Quaternion.Euler(info.Value.RotX, info.Value.RotY, info.Value.RotZ), info.Value.Price);

            spinner = Timing.RunCoroutine(_Spinner());
            hint_display = Timing.RunCoroutine(_HintDisplay());
        }

        public void Destroy()
        {
            Timing.KillCoroutines(dispense);
            Timing.KillCoroutines(spinner);
            NetworkServer.Destroy(game_object);
            stock.Clear();
            buttons.Clear();
        }

        private int AddStock(ItemType type, Quaternion rotation, float scale, ChamberID id, int amount)
        {
            int width = 1;
            if (!stock.ContainsKey(type))
                stock.Add(type, new Dictionary<ChamberID, Chamber>());
            Chamber chamber = new Chamber();
            stock[type].Add(id, chamber);

            ItemBase item;
            if (InventoryItemLoader.TryGetItem(type, out item))
            {
                float offset_y;
                Bounds bounds;
                {
                    ItemPickupBase pickup = Object.Instantiate(item.PickupDropModel, game_object.transform);
                    pickup.transform.localRotation = rotation;
                    pickup.transform.localScale = new Vector3(scale, scale, scale);
                    bounds = pickup.gameObject.GetComponentInChildren<Collider>().bounds;
                    foreach (var c in pickup.gameObject.GetComponentsInChildren<Collider>())
                        bounds.Encapsulate(c.bounds);
                    offset_y = pickup.transform.position.y - bounds.center.y;
                    Object.Destroy(pickup);
                }
                width = Mathf.Min(Mathf.CeilToInt(bounds.size.x / item_limits.x), 4 - id.x);
                float stride_x = stride.x * (id.x + (id.x + (width - 1))) / 2.0f;

                PrimitiveObject pusher = new PrimitiveObject(ObjectType.Cube);
                pusher.ColliderMode = PrimitiveObject.ColliderCreationMode.ClientOnly;
                pusher.Transform.Scale = new Vector3(width * pusher_size.x + pusher_gap_y * (width - 1), pusher_size.y, pusher_size.z);
                pusher.MaterialColor = plastic_color;

                for (int z = 0; z < amount; z++)
                {
                    ItemPickupBase pickup = Object.Instantiate(item.PickupDropModel, game_object.transform);
                    pickup.transform.localRotation = rotation;
                    pickup.transform.localScale = new Vector3(scale, scale, scale);
                    Vector3 size = bounds.size;
                    GameObject obj;
                    if (size != Vector3.Min(size, new Vector3(item_limits.x * width, item_limits.y, item_limits.z)))
                    {
                        size = BoxItem(pickup, bounds, scale, width, out obj);
                        var rb = obj.AddComponent<Rigidbody>();
                        rb.isKinematic = true;
                        rb.detectCollisions = false;
                        rb.constraints = RigidbodyConstraints.FreezeRotation;
                        obj.transform.localPosition = offset + new Vector3(stride_x, (size.y / 2.0f) + stride.y * id.y, space_z + (size.z / 2.0f) + ((size.z + space_z) * z));
                    }
                    else
                    {
                        obj = pickup.gameObject;
                        pickup.transform.localPosition = offset + new Vector3(stride_x, offset_y + (size.y / 2.0f) + stride.y * id.y, space_z + (size.z / 2.0f) + (size.z + space_z) * z);
                    }
                    if (size.z + (size.z + space_z) * z > limits.z)
                    {
                        Log.Error("over stocked vending machine with " + z + " " + type);
                        Object.Destroy(obj);
                        break;
                    }
                    pickup.NetworkInfo = new PickupSyncInfo(type, 1.0f);
                    NetworkServer.Spawn(pickup.gameObject);
                    if (pickup.PhysicsModule is PickupStandardPhysics physics)
                    {
                        physics.Rb.detectCollisions = false;
                        physics.Rb.isKinematic = true;
                    }
                    chamber.stock.Add(obj);
                    pusher.Transform.Position = offset + new Vector3(stride_x, stride.y * id.y + (pusher_size.y / 2.0f) - pusher_size.z, (space_z / 2.0f) + (size.z + space_z) * (z + 1));
                    chamber.pushers.Add(pusher.SpawnObject(game_object));
                    chamber.pushers.Last().GetComponent<PrimitiveObjectToy>().NetworkMovementSmoothing = 128;
                }
            }
            else
                Log.Error("could not load item of type " + type.ToString());

            return width;
        }

        private Vector3 BoxItem(ItemPickupBase item, Bounds old_bounds, float scale, int width, out GameObject cube)
        {
            Log.Info("item boxed");
            Vector3 size = Vector3.Min(old_bounds.size, new Vector3(item_limits.x * width, item_limits.y, item_limits.z));

            PrimitiveObject obj = new PrimitiveObject(ObjectType.Cube);
            obj.ColliderMode = PrimitiveObject.ColliderCreationMode.ServerOnlyNonSpawned;
            obj.Transform.Scale = size;
            obj.MaterialColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            cube = obj.SpawnObject(game_object);
            Vector3 offset = item.transform.position - old_bounds.center;
            item.transform.parent = cube.transform;
            item.transform.localPosition = new Vector3(offset.x * (1.0f / size.x), offset.y * (1.0f / size.y), offset.z * (1.0f / size.z));
            item.transform.localScale = new Vector3(scale, scale, scale);
            return size;
        }

        private void AddButton(ItemType type, Quaternion rotation, int price)
        {
            ItemBase item;
            if (InventoryItemLoader.TryGetItem(type, out item))
            {
                float offset_y;
                Bounds bounds;
                ItemPickupBase pickup = Object.Instantiate(item.PickupDropModel, game_object.transform);
                pickup.transform.localRotation = rotation * (item.Category != ItemCategory.Keycard ? Quaternion.Euler(-30.0f, 0.0f, 0.0f) : Quaternion.identity);
                bounds = pickup.gameObject.GetComponentInChildren<Collider>().bounds;
                foreach (var c in pickup.gameObject.GetComponentsInChildren<Collider>())
                    bounds.Encapsulate(c.bounds);
                offset_y = pickup.transform.position.y - bounds.center.y;
                float s = Mathf.Min(0.25f / bounds.size.x, 0.125f / bounds.size.y, 0.125f / bounds.size.z) * 0.9f;
                pickup.transform.localScale = new Vector3(s, s, s);
                pickup.transform.localPosition = button_offset + new Vector3(0.0f, offset_y + buttons.Count * button_stride_y, 0.125f);
                pickup.NetworkInfo = new PickupSyncInfo(type, 1.0f);
                NetworkServer.Spawn(pickup.gameObject);
                if (pickup.PhysicsModule is PickupStandardPhysics physics)
                {
                    physics.Rb.detectCollisions = false;
                    physics.Rb.constraints = RigidbodyConstraints.FreezePosition;
                    physics.Rb.centerOfMass = Vector3.zero;
                }
                if (button_prefab == null)
                {
                    button_prefab = new GameObject("vending_machine_button", new System.Type[] { typeof(BoxCollider) });
                    BoxCollider collider = button_prefab.GetComponent<BoxCollider>();
                    collider.size = new Vector3(0.25f, 0.125f, 0.125f);
                    button_prefab.layer = 28;
                }

                LightObject light = new LightObject();
                light.Intensity = 0.0625f;
                light.Range = 0.5f;
                light.Transform.Position = button_offset + new Vector3(0.0f, buttons.Count * button_stride_y, -0.125f);
                LightSourceToy light_toy = light.SpawnObject(game_object).GetComponent<LightSourceToy>();

                GameObject button = Object.Instantiate(button_prefab, game_object.transform);
                button.transform.localPosition = button_offset + new Vector3(0.0f, buttons.Count * button_stride_y, 0.0f);
                buttons.Add(button.GetComponent<BoxCollider>(), new Button { display_item = pickup.PhysicsModule as PickupStandardPhysics, type = type, light = light_toy, price = price });
            }
            else
                Log.Error("could not load item of type " + type.ToString());
        }

        public bool HitButton(Player player, BoxCollider collider)
        {
            if(buttons.ContainsKey(collider) && !(dispense.IsAliveAndPaused || dispense.IsRunning))
            {
                previously_inside.Remove(player.PlayerId);
                Timing.CallDelayed(1.0f, () => previously_inside.Remove(player.PlayerId));
                Button button = buttons[collider];
                int coins = CoinManager.TotalCoins(player);
                if (coins < button.price)
                    player.ReceiveHint(EventHandler.Singleton.config.HintInsufficientFunds, 3);
                else
                {
                    if (!stock.ContainsKey(button.type) || stock[button.type].IsEmpty())
                        player.ReceiveHint(EventHandler.Singleton.config.HintNoStock, 3);
                    else
                    {
                        CoinManager.Pay(player, button.price);
                        player.ReceiveHint(EventHandler.Singleton.config.HintBought.Replace("{item}", button.type.ToString()).Replace("{price}", button.price.ToString("0")), 3);
                    }
                    dispense = Timing.RunCoroutine(_Dispense(button));
                }
                return true;
            }
            return false;
        }

        private IEnumerator<float> _Dispense(Button button)
        {
            button.light.NetworkLightColor = new Color(0.0f, 1.0f, 0.0f);
            button.light.NetworkLightIntensity = 0.5f;
            yield return Timing.WaitForSeconds(0.5f);
            if (!stock.ContainsKey(button.type) || stock[button.type].IsEmpty())
            {
                yield return Timing.WaitForSeconds(0.5f);
                button.light.NetworkLightColor = new Color(1.0f, 0.0f, 0.0f);
                yield return Timing.WaitForSeconds(0.5f);
            }
            else
            {
                var chamber = stock[button.type].ToList().RandomItem();
                if (chamber.Value.stock.IsEmpty())
                {
                    yield return Timing.WaitForSeconds(0.5f);
                    button.light.NetworkLightColor = new Color(1.0f, 0.0f, 0.0f);
                    yield return Timing.WaitForSeconds(0.5f);
                }
                else
                {
                    float delta = 0.001f;
                    while (chamber.Value.pushers.First().transform.localPosition.z > offset.z)
                    {
                        foreach (var s in chamber.Value.stock)
                            s.transform.localPosition = new Vector3(s.transform.localPosition.x, s.transform.localPosition.y, s.transform.localPosition.z - delta);
                        foreach (var p in chamber.Value.pushers)
                            p.transform.localPosition = new Vector3(p.transform.localPosition.x, p.transform.localPosition.y, p.transform.localPosition.z - delta);
                        yield return Timing.WaitForOneFrame;
                    }
                    NetworkServer.Destroy(chamber.Value.pushers.First());
                    ItemPickupBase item = chamber.Value.stock.First().GetComponent<ItemPickupBase>();
                    Rigidbody rb = null;
                    if (item != null)
                    {
                        item.transform.localPosition = new Vector3(item.transform.localPosition.x, item.transform.localPosition.y, item.transform.localPosition.z - space_z);
                        if (item.PhysicsModule is PickupStandardPhysics physics)
                            rb = physics.Rb;
                        item.transform.parent = null;
                        item.transform.localScale = Vector3.one;
                    }
                    else
                    {
                        GameObject o = chamber.Value.stock.First();
                        rb = o.GetComponent<Rigidbody>();
                        Timing.CallDelayed(1.0f, () =>
                        {
                            ItemPickupBase contents = o.GetComponentInChildren<ItemPickupBase>();
                            contents.transform.parent = null;
                            contents.transform.localScale = Vector3.one;
                            Object.Destroy(o);
                        });
                    }

                    if (rb != null)
                    {
                        rb.detectCollisions = true;
                        rb.isKinematic = false;
                        rb.WakeUp();
                    }

                    chamber.Value.pushers.RemoveAt(0);
                    chamber.Value.stock.RemoveAt(0);
                }
                if (chamber.Value.stock.IsEmpty())
                    stock[button.type].Remove(chamber.Key);
            }
            yield return Timing.WaitForSeconds(0.5f);
            button.light.NetworkLightColor = new Color(1.0f, 1.0f, 1.0f);
            button.light.NetworkLightIntensity = 0.0625f;
        }

        private IEnumerator<float> _Spinner()
        {
            Quaternion delta = Quaternion.Euler(0.0f, 0.1f, 0.0f);
            while(true)
            {
                foreach(var button in buttons)
                {
                    button.Value.display_item.Rb.angularVelocity = new Vector3(0.0f, 1.0f, 0.0f);
                    button.Value.display_item.Rb.velocity = Vector3.zero;
                    button.Value.display_item._serverNextUpdateTime = 0.0f;
                }
                yield return Timing.WaitForOneFrame;
            }
        }

        private IEnumerator<float> _HintDisplay()
        {
            float sqr_dist = EventHandler.Singleton.config.HintDisplayDistance * EventHandler.Singleton.config.HintDisplayDistance;
            string msg = "";
            foreach(var button in buttons.Values)
            {
                msg += button.type + ": " + button.price + "\n";
            }
            Vector3 position = game_object.transform.position + Vector3.up;
            previously_inside = new HashSet<int>();
            while (true)
            {
                HashSet<int> inside = new HashSet<int>();
                foreach(var p in ReadyPlayers())
                {
                    if(p.IsAlive && Vector3.SqrMagnitude(p.Position - position) < sqr_dist)
                    {
                        inside.Add(p.PlayerId);
                        if (previously_inside.Contains(p.PlayerId))
                        {
                            int total = CoinManager.TotalCoins(p);
                            p.ReceiveHint(EventHandler.Singleton.config.HintDisplayPrefix + "You have " + total + (total == 1 ? " Coin" : " Coins") + "!\n" + EventHandler.Singleton.config.HintDisplayHint + "\n" + msg, 2);
                        }
                    }
                }
                previously_inside = inside.ToHashSet();
                yield return Timing.WaitForSeconds(1.0f);
            }
        }
    }
}
