using System;
using UnityEngine.LowLevel;

namespace Sabresaurus.SabreCore
{
    public static class PlayerLoopHelper
    {
        public static PlayerLoopSystem GetSystem<T>(this PlayerLoopSystem system)
        {
            if (system.type == typeof(T))
            {
                return system;
            }

            foreach (var subSystem in system.subSystemList)
            {
                var match = subSystem.GetSystem<T>();

                if (match.type == typeof(T))
                {
                    return match;
                }
            }

            return default;
        }

        public static void GetSystem<T>(this PlayerLoopSystem system, out PlayerLoopSystem matched)
        {
            if (system.type == typeof(T))
            {
                matched = system;
                return;
            }

            foreach (var subSystem in system.subSystemList)
            {
                var match = subSystem.GetSystem<T>();

                if (match.type == typeof(T))
                {
                    matched = match;
                    return;
                }
            }

            matched = default;
        }

        public static void AddSystemToMatchedParent<TParent>(ref PlayerLoopSystem system, PlayerLoopSystem newSystem)
        {
            if (system.type == typeof(TParent))
            {
                AddSystem(ref system, newSystem);
            }
            else if (system.subSystemList != null)
            {
                for (var index = 0; index < system.subSystemList.Length; index++)
                {
                    AddSystemToMatchedParent<TParent>(ref system.subSystemList[index], newSystem);
                }
            }
        }

        public static void AddSystem(ref PlayerLoopSystem parent, PlayerLoopSystem newSystem)
        {
            Array.Resize(ref parent.subSystemList, parent.subSystemList.Length + 1);

            parent.subSystemList[parent.subSystemList.Length - 1] = newSystem;
        }

        public static void RemoveSystem(ref PlayerLoopSystem system, PlayerLoopSystem systemToRemove)
        {
            if (system.subSystemList != null)
            {
                bool contained = false;

                for (int i = 0; i < system.subSystemList.Length; i++)
                {
                    if (system.subSystemList[i].type == systemToRemove.type)
                    {
                        contained = true;
                        break;
                    }
                }

                if (contained)
                {
                    PlayerLoopSystem[] subSystemList = new PlayerLoopSystem[system.subSystemList.Length - 1];

                    int index = 0;
                    for (int i = 0; i < system.subSystemList.Length; i++)
                    {
                        if (system.subSystemList[i].type != systemToRemove.type)
                        {
                            subSystemList[index] = system.subSystemList[i];
                            index++;
                        }
                    }

                    system.subSystemList = subSystemList;
                    return;
                }


                for (var index = 0; index < system.subSystemList.Length; index++)
                {
                    RemoveSystem(ref system.subSystemList[index], systemToRemove);
                }
            }
        }
    }
}