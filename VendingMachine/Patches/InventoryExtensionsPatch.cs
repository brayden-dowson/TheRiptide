using HarmonyLib;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using Mirror;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide.Patches
{
    //[HarmonyPatch(typeof(InventoryExtensions))]
    public class InventoryExtensionsPatch
    {
        //[HarmonyPatch(nameof(InventoryExtensions.ServerAddItem))]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            //Label skip_stacks_full_return = generator.DefineLabel();
            Label skip_success_return= generator.DefineLabel();
            Label skip_throw = generator.DefineLabel();
            LocalBuilder item_base = generator.DeclareLocal(typeof(ItemBase));
            bool found_throw = false;
            bool emited_patch = false;
            foreach (var instruction in instructions)
            {
                if(!emited_patch)
                {
                    if (instruction.opcode == OpCodes.Brtrue_S)
                        instruction.operand = skip_throw;

                    if (found_throw)
                    {
                        CodeInstruction start = new CodeInstruction(OpCodes.Ldarg_0);
                        start.labels.Add(skip_throw);
                        yield return start;
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Ldarg_3);
                        yield return new CodeInstruction(OpCodes.Call, typeof(CoinManager).GetMethod("TryAddToStack"));
                        yield return new CodeInstruction(OpCodes.Stloc, item_base);
                        yield return new CodeInstruction(OpCodes.Ldloc, item_base);
                        yield return new CodeInstruction(OpCodes.Brfalse, skip_success_return);
                        yield return new CodeInstruction(OpCodes.Ldloc, item_base);
                        yield return new CodeInstruction(OpCodes.Ret);
                        instruction.labels.Add(skip_success_return);
                        emited_patch = true;
                    }
                    else if (instruction.opcode == OpCodes.Throw)
                        found_throw = true;
                }
                yield return instruction;
            }

            if(!emited_patch)
            {
                Log.Error("didnt meet conditions when patching InventoryExtensions.ServerAddItem");
            }
            else
                Log.Error("patched InventoryExtensions.ServerAddItem");
        }

        public static bool Prefix(ref ItemPickupBase __result, ItemBase item, PickupSyncInfo psi, Vector3 position, Quaternion rotation, bool spawn = true, Action<ItemPickupBase> setupMethod = null)
        {
            if (!NetworkServer.active)
                throw new InvalidOperationException("Method ServerCreatePickup can only be executed on the server.");
            __result = UnityEngine.Object.Instantiate<ItemPickupBase>(item.PickupDropModel, position, rotation);
            __result.NetworkInfo = psi;
            if (setupMethod != null)
                setupMethod(__result);
            if (item.ItemTypeId == ItemType.Coin)
                __result.transform.localScale = Vector3.one * CoinManager.config.CoinPickupModelScale;
            if (spawn)
                NetworkServer.Spawn(__result.gameObject);
            return false;
        }

        public static void Debug()
        {
            Log.Info("debug");
        }
    }
}
