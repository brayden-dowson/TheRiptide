using CommandSystem;
using HarmonyLib;
using MEC;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using RemoteAdmin;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public class UltraQuaternionConfig
    {
        public bool IsEnabled { get; set; } = true;

        public List<PlayerPermissions> CmdPermissions { get; set; } = new List<PlayerPermissions>
        {
            PlayerPermissions.FacilityManagement
        };
    }

    [HarmonyPatch(typeof(LowPrecisionQuaternion))]
    public class LowPrecisionQuaternionPatch
    {
        public static Dictionary<Quaternion, LowPrecisionQuaternion> cache = new Dictionary<Quaternion, LowPrecisionQuaternion>();
        public static HashSet<Quaternion> previous_frame = new HashSet<Quaternion>();
        public static HashSet<Quaternion> this_frame = new HashSet<Quaternion>();

        public static MethodBase TargetMethod()
        {
            return AccessTools.Constructor(typeof(LowPrecisionQuaternion), new System.Type[] { typeof(Quaternion) });
        }

        public static void Postfix(ref LowPrecisionQuaternion __instance, Quaternion value)
        {
            if (!cache.ContainsKey(value))
            {
                if (previous_frame.Contains(value))
                {
                    Quaternion q = new Quaternion(value.x, value.y, value.z, value.w);
                    float max_mag = 0.0f;
                    for (int i = 0; i < 4; i++)
                        if (Mathf.Abs(q[i]) > max_mag)
                            max_mag = Mathf.Abs(q[i]);
                    for (int i = 0; i < 4; i++)
                        q[i] = q[i] * (1.0f / max_mag) * sbyte.MaxValue;

                    float best_scale = 1.0f;
                    float best_dist = 1.0f;
                    for (int i = 0; i < sbyte.MaxValue; i++)
                    {
                        float scale = 1.0f - (i / (float)sbyte.MaxValue);
                        float dist = 0.0f;
                        for (int j = 0; j < 4; j++)
                        {
                            float d = q[j] * scale - Mathf.RoundToInt(q[j] * scale);
                            dist += d * d;
                        }
                        dist = Mathf.Pow(dist, 1.0f / 4.0f);
                        if (dist < best_dist)
                        {
                            best_dist = dist;
                            best_scale = scale;
                        }
                    }

                    byte[] bytes = __instance.CastToArray();
                    bytes[0] = System.BitConverter.GetBytes((sbyte)Mathf.RoundToInt(q.x * best_scale))[0];
                    bytes[1] = System.BitConverter.GetBytes((sbyte)Mathf.RoundToInt(q.y * best_scale))[0];
                    bytes[2] = System.BitConverter.GetBytes((sbyte)Mathf.RoundToInt(q.z * best_scale))[0];
                    bytes[3] = System.BitConverter.GetBytes((sbyte)Mathf.RoundToInt(q.w * best_scale))[0];
                    LowPrecisionQuaternion lpq = bytes.CastToStruct<LowPrecisionQuaternion>();
                    __instance = lpq;
                    cache.Add(value, lpq);
                    previous_frame.Remove(value);
                }
                else
                {
                    this_frame.Add(value);
                }
            }
            else
                __instance = cache[value];
        }
    }

    public class UltraQuaternion
    {
        public static UltraQuaternion Singleton { get; private set; }
        public static Harmony Harmony { get; private set; }
        public static bool Enabled = false;
        private static CoroutineHandle update_handle;

        [PluginConfig]
        public UltraQuaternionConfig config;

        [PluginEntryPoint("UltraQuaternion", "1.0.0", "increases precision of LPQs at the expense of performance", "The Riptide")]
        public void OnEnabled()
        {
            Singleton = this;
            Harmony = new Harmony("UltraQuaternion");
            if (config.IsEnabled)
                Enable();
        }

        [PluginUnload]
        public void OnDisabled()
        {
            Disable();
            Harmony = null;
        }

        public static void Enable()
        {
            if(!Enabled)
            {
                update_handle = Timing.RunCoroutine(_Update());
                Harmony.PatchAll();
                Enabled = true;
            }
        }

        public static void Disable()
        {
            if(Enabled)
            {
                Timing.KillCoroutines(update_handle);
                Harmony.UnpatchAll("UltraQuaternion");
                Enabled = false;
            }
        }

        private static IEnumerator<float> _Update()
        {
            while(true)
            {
                try
                {
                    LowPrecisionQuaternionPatch.previous_frame = LowPrecisionQuaternionPatch.this_frame.ToHashSet();
                    LowPrecisionQuaternionPatch.this_frame.Clear();

                    //foreach(var p in Player.GetPlayers())
                    //    p.SendBroadcast(LowPrecisionQuaternionPatch.cache.Count + " | " + LowPrecisionQuaternionPatch.previous_frame.Count + " | " + LowPrecisionQuaternionPatch.this_frame.Count + " | ", 1, shouldClearPrevious: true);
                }
                catch(System.Exception ex)
                {
                    Log.Error(ex.ToString());
                }

                yield return Timing.WaitForOneFrame;
            }
        }

        [CommandHandler(typeof(RemoteAdminCommandHandler))]
        public class UltraQuaternionToggle : ICommand
        {
            public string Command { get; } = "uq";

            public string[] Aliases { get; } = new string[] { };

            public string Description { get; } = "Turns Ultra Quaternion on or off";

            public bool Execute(System.ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                if (sender is PlayerCommandSender sender1 && !sender1.CheckPermission(Singleton.config.CmdPermissions.ToArray(), out response))
                    return false;

                if(Harmony == null)
                {
                    response = "Harmony not loaded";
                    return false;
                }

                if(Enabled)
                {
                    Disable();
                    response = "Ultra Quaternion Disabled";
                }
                else
                {
                    Enable();
                    response = "Ultra Quaternion Enabled";
                }
                return true;
            }
        }
    }

    public static class CastingHelper
    {
        public static T CastToStruct<T>(this byte[] data) where T : struct
        {
            var pData = GCHandle.Alloc(data, GCHandleType.Pinned);
            var result = (T)Marshal.PtrToStructure(pData.AddrOfPinnedObject(), typeof(T));
            pData.Free();
            return result;
        }

        public static byte[] CastToArray<T>(this T data) where T : struct
        {
            var result = new byte[Marshal.SizeOf(typeof(T))];
            var pResult = GCHandle.Alloc(result, GCHandleType.Pinned);
            Marshal.StructureToPtr(data, pResult.AddrOfPinnedObject(), true);
            pResult.Free();
            return result;
        }
    }
}
