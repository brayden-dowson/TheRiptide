using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public enum WinningRole
    {
        Innocents,
        Traitors,
        Jesters
    }

    public interface IMap
    {
        string Name { get; }
        string Author { get; }
        string Description { get; }
        float RoundTime { get; }
        Vector3 ReadyUpBodyPosition { get; }

        bool Load(object plugin);
        void Unload(object plugin);

        void OnReadyUpStart();
        void OnReadyUpEnd();
        void OnRoundStart();
        void OnRoundEnd(WinningRole winner);
        void OnPlayerSpawn(Player player);
        void OnPlayerReady(Player player);

        bool InnocentsMetWinCondition();
    }
}
