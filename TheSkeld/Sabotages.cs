using AdminToys;
using CustomPlayerEffects;
using Interactables.Interobjects.DoorUtils;
using MEC;
using PlayerRoles;
using PluginAPI.Core;
using slocLoader;
using slocLoader.Objects;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheRiptide.Patches;
using UnityEngine;
using VoiceChat.Codec;
using Mirror;

namespace TheRiptide
{
    public interface ISabotage
    {
        float Price { get; }
        float ActivationCoolDown { get; }
        float AutoResetTime { get; }
        void Enable(Player enabler);
        void Disable();
    }

    public class ActiveSabotages
    {
        private static Dictionary<string, float> sabotages = new Dictionary<string, float>();

        private static CoroutineHandle auto_update;
        private static Stopwatch stopwatch = new Stopwatch();

        public static void Add(string sabotage, int time_remaining)
        {
            TimeUpdate();
            if (!sabotages.ContainsKey(sabotage))
                sabotages.Add(sabotage, time_remaining);
            Update(false);
        }

        public static void Remove(string sabotage)
        {
            TimeUpdate();
            if (sabotages.ContainsKey(sabotage))
                sabotages.Remove(sabotage);
            Update(false);
        }
        
        private static void TimeUpdate()
        {
            float delta = (float)stopwatch.Elapsed.TotalSeconds;
            foreach (var s in sabotages.Keys.ToList())
            {
                sabotages[s] -= delta;
                sabotages[s] = Mathf.Max(sabotages[s], 0.0f);
            }
            stopwatch.Restart();
        }

        private static void Update(bool update_time)
        {
            if (update_time)
                TimeUpdate();
            Timing.KillCoroutines(auto_update);
            if (sabotages.IsEmpty())
            {
                BroadcastOverride.ClearLine(6, BroadcastPriority.Medium);
                stopwatch.Stop();
            }
            else
            {
                string msg = "<color=#FF0000><b>SABOTAGES: ";
                List<string> formated_sabos = new List<string>();
                foreach (var sabo in sabotages)
                {
                    if (sabo.Value <= 0.0f)
                        formated_sabos.Add("<color=#FF0000>" + sabo.Key + "</color>");
                    else
                        formated_sabos.Add("<color=#FFFF00>" + sabo.Key + " " + sabo.Value.ToString("0") + "</color>");
                }
                msg += string.Join(", ", formated_sabos) + "<b></color>";
                float increment = NextLowestFiveSecondIncrement() + 1 / 20.0f;
                BroadcastOverride.BroadcastLine(6, increment, BroadcastPriority.Medium, msg);
                auto_update = Timing.CallDelayed(increment, () => Update(true));
            }
            BroadcastOverride.UpdateAllDirty();
        }

        private static float NextLowestFiveSecondIncrement()
        {
            float lowest = sabotages.Min(s => s.Value == 0.0f ? 60.0f : s.Value);
            float delta = lowest % 5.0f;
            if (delta == 0.0f)
                return 5.0f;
            else
                return delta;
        }
    }

    public abstract class PoweredController
    {
        private bool state = true;
        private bool power = true;
        public bool Power
        {
            get => power;
            set
            {
                bool prev_state = power && state;
                power = value;
                bool new_state = power && state;
                if (prev_state != new_state)
                    Update(new_state);
            }
        }
        public bool State
        {
            get => state;
            set
            {
                bool prev_state = power && state;
                state = value;
                bool new_state = power && state;
                if (prev_state != new_state)
                    Update(new_state);
            }
        }

        protected abstract void Update(bool new_state);
    }

    public class OxygenController : PoweredController
    {
        public static readonly OxygenController Singleton = new OxygenController();
        private OxygenController() { }

        private CoroutineHandle oxygen_delay;

        protected override void Update(bool new_state)
        {
            if (new_state)
            {
                Timing.KillCoroutines(oxygen_delay);
                foreach (var p in Player.GetPlayers())
                    if (p.IsAlive)
                        p.EffectsManager.DisableEffect<Asphyxiated>();
            }
            else
            {
                oxygen_delay = Timing.CallDelayed(20.0f, () =>
                {
                    foreach (var p in Player.GetPlayers())
                        if (p.IsAlive)
                            p.EffectsManager.EnableEffect<Asphyxiated>();
                });
            }
        }
    }

    public class OxygenSabotage : ISabotage
    {
        public float Price => 0.0f;

        public float ActivationCoolDown => 20.0f;
        public float AutoResetTime => -1;

        public void Disable()
        {
            ActiveSabotages.Remove("OXY");
            OxygenController.Singleton.State = true;
        }

        public void Enable(Player enabler)
        {
            ActiveSabotages.Add("OXY", 20);
            OxygenController.Singleton.State = false;
            Shop.RewardCash(enabler, 10, "<b><color=#00FF00>$10</color> reward for sabotaging Oxygen! Check shop for options</b>");
            Timing.CallDelayed(20.0f, () => { if (OxygenController.Singleton.State == false)
                    Shop.RewardCash(enabler, 65, "<b><color=#00FF00>$65</color> reward for successful sabotage of Oxygen! Check shop for options</b>"); });
        }
    }

    public class LightsController : PoweredController
    {
        public static readonly LightsController Singleton = new LightsController();
        private LightsController()  { }

        private Dictionary<LightSourceToy, float> intensity_cache = new Dictionary<LightSourceToy, float>();

        public void Start()
        {
            intensity_cache.Clear();
            foreach (var light in TheSkeld.Singleton.lights)
                intensity_cache.Add(light, light.LightIntensity);
        }

        private List<float> intensity_transition = new List<float>
        {
            1.0f, 0.5f, 0.9f, 0.3f, 0.7f, 0.1f, 0.5f, 0.0f, 0.3f, 0.0f, 0.1f, 0.0f
        };

        protected override void Update(bool new_state)
        {
            Timing.RunCoroutine(_LightTransition(new_state), Segment.Update);
        }

        private IEnumerator<float> _LightTransition(bool new_state)
        {
            List<float> order = intensity_transition.ToList();
            if (new_state)
                order.Reverse();
            foreach (var intensity in order)
            {
                foreach(var light in intensity_cache)
                {
                    light.Key.NetworkLightIntensity = light.Value * intensity;
                }
                yield return Timing.WaitForSeconds(1.0f / 14.0f);
            }
        }
    }

    public class LightsSabotage : ISabotage
    {
        public float Price => 0.0f;
        public float ActivationCoolDown => 20.0f;
        public float AutoResetTime => -1;

        public void Disable()
        {
            ActiveSabotages.Remove("LGHT");
            LightsController.Singleton.State = true;
        }

        public void Enable(Player enabler)
        {
            ActiveSabotages.Add("LGHT", 0);
            LightsController.Singleton.State = false;
            Shop.RewardCash(enabler, 15, "<b><color=#00FF00>$15</color> reward for sabotaging Lights! Check shop for options</b>");
        }
    }

    public class SurveillanceController : PoweredController
    {
        public static readonly SurveillanceController Singleton = new SurveillanceController();
        private SurveillanceController() {}

        private GameObject table;
        private Vector3 offset;
        private Dictionary<int, PrimitiveObjectToy> player_markers = new Dictionary<int, PrimitiveObjectToy>();
        private bool running = true;

        public void Start(GameObject table, Vector3 offset)
        {
            this.table = table;
            this.offset = offset;
            running = true;
            Timing.RunCoroutine(_Update());
        }

        public void Stop()
        {
            running = false;
            player_markers.Clear();
        }

        public void Reset()
        {
            foreach (var marker in player_markers.Values)
                NetworkServer.Destroy(marker.gameObject);
            player_markers.Clear();
        }

        protected override void Update(bool new_state)
        {
            running = new_state;
            if (new_state)
                Timing.RunCoroutine(_Update());
        }

        private IEnumerator<float> _Update()
        {
            while(running)
            {
                try
                {
                    foreach(var p in Player.GetPlayers())
                    {
                        if (!player_markers.ContainsKey(p.PlayerId))
                            player_markers.Add(p.PlayerId, CreateMarker());

                        if (!p.IsAlive)
                            player_markers[p.PlayerId].transform.position = new Vector3(0.0f, 0.0f, 0.0f);
                        else
                        {
                            Vector3 pos = p.Position - offset;
                            pos.y = 0;
                            player_markers[p.PlayerId].transform.position = offset + table.transform.TransformPoint(pos);
                            if (TraitorAmongUs.detectives.Contains(p.PlayerId))
                                player_markers[p.PlayerId].NetworkMaterialColor = new Color(0.5f, 0.75f, 1.0f);
                            else
                                player_markers[p.PlayerId].NetworkMaterialColor = new Color(1.0f, 1.0f, 0.0f);
                        }
                    }

                    foreach(var body in BodyManager.Bodies)
                    {
                        int id = body.Unided.Info.OwnerHub.PlayerId;
                        if (!player_markers.ContainsKey(id))
                            player_markers.Add(id, CreateMarker());

                        Vector3 pos = body.Unided.Info.StartPosition - offset;
                        pos.y = 0;
                        player_markers[id].transform.position = offset + table.transform.TransformPoint(pos);
                        if (!BodyManager.Unided.ContainsKey(body.Collider))
                        {
                            Color color = Color.clear;
                            switch(body.Real)
                            {
                                case TauRole.Innocent: color = Color.green; break;
                                case TauRole.Detective: color = Color.blue; break;
                                case TauRole.Traitor: color = Color.red; break;
                                case TauRole.Jester: color = new Color(1.0f, 0.5f, 1.0f); break;
                            }
                            player_markers[id].NetworkMaterialColor = color;
                        }
                    }
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                yield return Timing.WaitForSeconds(2.0f);
            }

            foreach(var pm in player_markers)
            {
                pm.Value.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
            }
        }

        private PrimitiveObjectToy CreateMarker()
        {
            PrimitiveObject obj = new PrimitiveObject(ObjectType.Cube);
            obj.ColliderMode = PrimitiveObject.ColliderCreationMode.NoCollider;
            obj.Transform.Scale = new Vector3(0.04f, 0.04f, 0.04f);
            GameObject go = API.SpawnObject(obj);
            var pot = go.GetComponent<PrimitiveObjectToy>();
            //pot.NetworkMovementSmoothing = 60;
            return pot;
        }
    }

    public class SurveillanceSabotage : ISabotage
    {
        public float Price => 0.0f;
        public float ActivationCoolDown => 0.0f;
        public float AutoResetTime => -1.0f;

        public void Disable()
        {
            ActiveSabotages.Remove("SRV");
            SurveillanceController.Singleton.State = true;
        }

        public void Enable(Player enabler)
        {
            ActiveSabotages.Add("SRV", 0);
            SurveillanceController.Singleton.State = false;
            Shop.RewardCash(enabler, 15, "<b><color=#00FF00>$15</color> reward for sabotaging Surveillance! Check shop for options</b>");
        }
    }

    public class GeneratorSabotage : ISabotage
    {
        public static List<GeneratorSabotage> Instances = new List<GeneratorSabotage>();

        public float Price => 0.0f;
        public float ActivationCoolDown => 30.0f;
        public float AutoResetTime => -1.0f;

        private CoroutineHandle generator_update;
        public bool IsDeactivated = false;

        public GeneratorSabotage()
        {
            Instances.Add(this);
        }

        public void Disable()
        {
            ActiveSabotages.Remove("ENG" + (Instances.IndexOf(this) + 1));
            Timing.KillCoroutines(generator_update);
            if (Instances[0].IsDeactivated && Instances[1].IsDeactivated)
            {
                OxygenController.Singleton.Power = true;
                LightsController.Singleton.Power = true;
                DoorsController.Singleton.Power = true;
                ShieldController.Singleton.Power = true;
                CommunicationController.Singleton.Power = true;
            }
            IsDeactivated = false;
        }

        public void Enable(Player enabler)
        {
            ActiveSabotages.Add("ENG" + (Instances.IndexOf(this) + 1), 45);
            generator_update = Timing.RunCoroutine(_GeneratorUpdate(enabler));
        }

        private IEnumerator<float> _GeneratorUpdate(Player enabler)
        {
            Shop.RewardCash(enabler, 15, "<b><color=#00FF00>$15</color> reward for sabotaging Engine! Check shop for options</b>");
            yield return Timing.WaitForSeconds(45.0f);
            Shop.RewardCash(enabler, 85, "<b><color=#00FF00>$85</color> reward for successful sabotage of Engine! Check shop for options</b>");
            IsDeactivated = true;
            if (Instances[0].IsDeactivated && Instances[1].IsDeactivated)
            {
                OxygenController.Singleton.Power = false;
                LightsController.Singleton.Power = false;
                DoorsController.Singleton.Power = false;
                ShieldController.Singleton.Power = false;
                CommunicationController.Singleton.Power = false;
                foreach (var ds in TheSkeld.Singleton.doors.Values)
                    ds.door_base.NetworkTargetState = true;
            }
        }

    }

    public class ShieldController : PoweredController
    {
        public static readonly ShieldController Singleton = new ShieldController();
        private ShieldController() { }

        private CoroutineHandle sheild_update;

        protected override void Update(bool new_state)
        {
            if (new_state)
            {
                Timing.KillCoroutines(sheild_update);
                foreach (var p in Player.GetPlayers())
                {
                    if (p.IsAlive)
                    {
                        p.EffectsManager.DisableEffect<Burned>();
                        p.EffectsManager.DisableEffect<Concussed>();
                        p.EffectsManager.DisableEffect<Deafened>();
                        p.EffectsManager.DisableEffect<Poisoned>();
                    }
                }
            }
            else
            {
                sheild_update = Timing.RunCoroutine(_SheildUpdate());
            }
        }

        private static IEnumerator<float> _SheildUpdate()
        {
            float x = 0.0f;
            while(true)
            {
                if(x > 15.0f)
                    foreach (var p in Player.GetPlayers())
                        if (p.IsAlive)
                            p.EffectsManager.EnableEffect<Burned>();
                if(x > 30.0f)
                    foreach (var p in Player.GetPlayers())
                        if (p.IsAlive)
                            p.EffectsManager.EnableEffect<Concussed>();

                if (x > 45.0f)
                    foreach (var p in Player.GetPlayers())
                        if (p.IsAlive)
                            p.EffectsManager.EnableEffect<Deafened>();

                if (x > 60.0f)
                    foreach (var p in Player.GetPlayers())
                        if (p.IsAlive)
                            p.EffectsManager.EnableEffect<Poisoned>();

                yield return Timing.WaitForSeconds(1.0f);
                x += 1.0f;
            }
        }
    }

    public class ShieldSabotage : ISabotage
    {
        public float Price => 0.0f;
        public float ActivationCoolDown => 30.0f;
        public float AutoResetTime => -1.0f;

        private CoroutineHandle reward_handle;

        public void Disable()
        {
            Timing.KillCoroutines(reward_handle);
            ActiveSabotages.Remove("SHLD");
            ShieldController.Singleton.State = true;
        }

        public void Enable(Player enabler)
        {
            Shop.RewardCash(enabler, 15, "<b><color=#00FF00>$15</color> reward for sabotaging Shields! Check shop for options</b>");
            ActiveSabotages.Add("SHLD", 15);
            ShieldController.Singleton.State = false;
            reward_handle = Timing.CallDelayed(15.0f,()=>Timing.CallPeriodically(60.0f, 15.0f, () => Shop.RewardCash(enabler, 15, "<b><color=#00FF00>$15</color> reward for successful sabotage of Shields! Check shop for options</b>")));
        }
    }

    public class CommunicationController : PoweredController
    {
        public static List<byte[]> scrambled_data = new List<byte[]>();
        public static List<int> scrambled_data_size = new List<int>();
        public static Dictionary<int, int> player_index = new Dictionary<int, int>();
        public static readonly CommunicationController Singleton = new CommunicationController();

        private CommunicationController()
        {
            if (scrambled_data.Count == 0)
            {
                OpusEncoder encoder = new OpusEncoder(VoiceChat.Codec.Enums.OpusApplicationType.Voip);
                PinkNumber generator = new PinkNumber(65536);
                for (int i = 0; i < 1001; i++)
                {
                    float[] data = new float[480];
                    for (int j = 0; j < 480; j++)
                        data[j] = 0.05f * ((generator.GetNextValue() / 65536.0f) - 0.5f);
                    if (i != 1000)
                    {
                        scrambled_data.Add(new byte[512]);
                        scrambled_data_size.Add(encoder.Encode(data, scrambled_data.Last()));
                    }
                    else
                        scrambled_data_size[0] = encoder.Encode(data, scrambled_data[0]);
                }
            }
        }

        protected override void Update(bool new_state)
        {
            VoiceTransceiverPatch.ScrambleRadio = !new_state;
        }
    }

    public class CommunicationSabotage : ISabotage
    {
        public float Price => 0.0f;
        public float ActivationCoolDown => 15.0f;
        public float AutoResetTime => -1.0f;

        public void Disable()
        {
            ActiveSabotages.Remove("COMS");
            CommunicationController.Singleton.State = true;
        }

        public void Enable(Player enabler)
        {
            ActiveSabotages.Add("COMS", 0);
            CommunicationController.Singleton.State = false;
            Shop.RewardCash(enabler, 15, "<b><color=#00FF00>$15</color> reward for sabotaging Communications! Check shop for options</b>");
        }

    }

    class PinkNumber
    {
        private int max_key;
        private int key;
        private uint[] white_values = new uint[5];
        private uint range;

        public PinkNumber(uint range = 128)
        {
            max_key = 0x1f; // Five bits set
            this.range = range;
            key = 0;
            for (int i = 0; i < 5; i++)
                white_values[i] = (uint)(Random.Range(0, int.MaxValue) % (range / 5));
        }

        public int GetNextValue()
        {
            int last_key = key;
            uint sum;

            key++;
            if (key > max_key)
                key = 0;
            // Exclusive-Or previous value with current value. This gives
            // a list of bits that have changed.
            int diff = last_key ^ key;
            sum = 0;
            for (int i = 0; i < 5; i++)
            {
                // If bit changed get new random number for corresponding
                // white_value
                if ((diff & (1 << i)) != 0)
                    white_values[i] = (uint)(Random.Range(0, int.MaxValue) % (range / 5));
                sum += white_values[i];
            }
            return (int)sum;
        }
    };

    public class DoorsController : PoweredController
    {
        public static readonly DoorsController Singleton = new DoorsController();
        private DoorsController() { }

        protected override void Update(bool new_state)
        {
            if (new_state)
            {
                foreach (var ds in TheSkeld.Singleton.doors.Values)
                    ds.door_base.UnlockLater(0.0f, DoorLockReason.AdminCommand);
            }
            else
            {
                if (Power)
                {
                    foreach (var ds in TheSkeld.Singleton.doors.Values)
                    {
                        ds.door_base.ServerChangeLock(DoorLockReason.AdminCommand, true);
                        ds.door_base.NetworkTargetState = false;
                    }
                }
                else
                {
                    foreach (var ds in TheSkeld.Singleton.doors.Values)
                    {
                        ds.door_base.ServerChangeLock(DoorLockReason.AdminCommand, true);
                        ds.door_base.NetworkTargetState = true;
                    }
                }
            }
        }
    }

    public class DoorSabotage : ISabotage
    {
        public float Price => 0.0f;

        public float ActivationCoolDown => 45.0f;
        public float AutoResetTime => 15.0f;

        public void Disable()
        {
            ActiveSabotages.Remove("DRS");
            DoorsController.Singleton.State = true;
        }

        public void Enable(Player enabler)
        {
            ActiveSabotages.Add("DRS", 15);
            DoorsController.Singleton.State = false;
            Timing.CallDelayed(15.0f, () => Disable());
            Shop.RewardCash(enabler, 15, "<b><color=#00FF00>$15</color> reward for sabotaging Doors! Check shop for options</b>");
        }
    }
}
