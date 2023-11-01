using MEC;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ZombieDefence
{
    class NavMesh
    {
        const int iterations = 1000;
        const float step = 0.5f;

        private enum Dir
        {
            XP,
            XPZP,
            ZP,
            ZPXN,
            XN,
            XNZN,
            ZN,
            ZNXP
        }

        public class Node
        {
            public enum State:byte
            {
                Undefined,
                None,
                Wall,
                Step,
                Gap,
            }
            public State[] Edges = new State[8];
            public Vector3 Position;
        }

        private CharacterController probe;
        private CoroutineHandle build;

        public HashSet<Node> DefinedSet = new HashSet<Node>();


        public void Build(GameObject obj, Vector3 offset)
        {
            probe = new GameObject("navigation mesh probe", new Type[] { typeof(CharacterController), }).GetComponent<CharacterController>();
            probe.radius = 0.35f;
            probe.height = 1.75f;
            build = Timing.RunCoroutine(_Build(obj.transform.TransformPoint(offset)));
        }

        private IEnumerator<float> _Build(Vector3 start)
        {
            probe.transform.position = start;
            probe.Move(Vector3.down * 100.0f);
            HashSet<Node> ws = new HashSet<Node> { new Node { Position = probe.transform.position } };

            while (!ws.IsEmpty())
            {
                for(int i = 0; i < iterations; i++)
                {







                }
                yield return Timing.WaitForOneFrame;
            }
        }

        private Vector3 DirToVec(Dir dir)
        {
            switch(dir)
            {
                case Dir.XP:    return new Vector3( 1.0f, 0.0f,  0.0f);
                case Dir.XPZP:  return new Vector3( 1.0f, 0.0f,  1.0f);
                case Dir.ZP:    return new Vector3( 0.0f, 0.0f,  1.0f);
                case Dir.ZPXN:  return new Vector3(-1.0f, 0.0f,  1.0f);
                case Dir.XN:    return new Vector3(-1.0f, 0.0f,  0.0f);
                case Dir.XNZN:  return new Vector3(-1.0f, 0.0f, -1.0f);
                case Dir.ZN:    return new Vector3( 0.0f, 0.0f, -1.0f);
                case Dir.ZNXP:  return new Vector3( 1.0f, 0.0f, -1.0f);
            }
            return Vector3.zero;
        }

    }
}
