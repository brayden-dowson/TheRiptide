using HarmonyLib;
using Interactables.Interobjects.DoorUtils;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public class Config
    {
        public bool Enable939Lunge { get; set; } = true;
        public float Scp939DamagePerTick { get; set; } = 0.5f;
        [Description("If stamina is at 0 it will do no damage to the door")]
        public float Scp939StaminaPerTick { get; set; } = 0.003f;
        public DoorDamageType Scp939DamageType { get; set; } = DoorDamageType.Scp096;

        public bool Enable173Breakneck { get; set; } = true;
        public float Scp173DamagePerTick { get; set; } = 15.0f;
        public DoorDamageType Scp173DamageType { get; set; } = DoorDamageType.Grenade;
        [Description("time reduction to breakneck ability on break")]
        public float Scp173BreakneckBreakPenalty { get; set; } = 7.00f;

        public bool Enable173TeleportBreakneck { get; set; } = true;
        public float Scp173DamageOnTeleport { get; set; } = 1000.0f;
        public DoorDamageType Scp173TeleportDamageType { get; set; } = DoorDamageType.ServerCommand;
        [Description("time reduction to breakneck ability on teleport hit")]
        public float Scp173TeleportPenalty { get; set; } = 3.0f;

        [Description("All door damage types (do not edit)")]
        public List<DoorDamageType> DoorDamageTypes { get; set; } = Enum.GetValues(typeof(DoorDamageType)).ToArray<DoorDamageType>().ToList();
    }

    public class ScpDoorBreak
    {
        public static ScpDoorBreak Singleton { get; private set; }

        [PluginConfig]
        public Config config;

        private Harmony harmony;

        [PluginEntryPoint("ScpDoorBreak", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            Singleton = this;
            harmony = new Harmony("ScpDoorBreak");
            harmony.PatchAll();
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        [PluginUnload]
        public void OnDisabled()
        {
            harmony.UnpatchAll("ScpDoorBreak");
            harmony = null;
        }
    }
}

// 0 | 11111111111110101110111101111111 | Default
// 1 | 11111111001110101010111100110111 | TransparentFX
// 2 | 11011111001110101000001100110111 | Ignore Raycast
// 3 | 11111111111111111111111111111111 | 
// 4 | 11111111101110101010111100110111 | Water
// 5 | 11111111101110101010001100110110 | UI
// 6 | 11111111111111111111111111111111 | 
// 7 | 11111111111111111111111111111111 | 
// 8 | 10011111000110101000001100110111 | Player
// 9 | 10010011010000100010000000010110 | InteractableNoPlayerCollision
//10 | 11111111001110101010001100110110 | Viewmodel
//11 | 11111111101110101010001100110110 | 
//12 | 11111111101110101010001100110110 | RenderAfterFog
//13 | 00010011000000101010001100100110 | Hitbox
//14 | 11111111111111101110111101111111 | Glass
//15 | 00010011000000001010001100100110 | 
//16 | 11111111101111110010001100110111 | InvisibleCollider
//17 | 10010011000000100100000000000110 | Ragdoll
//18 | 11011111011111111010111100110111 | CCTV
//19 | 00010011000000000000000100100110 | 
//20 | 11011011000000100010101100110110 | Grenade
//21 | 11011011000000100010001100110110 | Phantom
//22 | 11111111101111111010111100110110 | 
//23 | 11111111101111111011111100110110 | 
//24 | 00010011000000000000000000100110 | 
//25 | 10010011000000100000000000110110 | OnlyWorldCollision:
//26 | 11111111101111111011111111110111 | 
//27 | 11111111111110101010111101111111 | Door
//28 | 10010011000000100000000000010111 | Skybox
//29 | 11111111111111111111111111111110
//30 | 11111111111111111111111111111111
//31 | 11111011100000101010000000111011