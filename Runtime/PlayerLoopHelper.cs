using System;
using UnityEngine.LowLevel;

namespace Sabresaurus.SabreCore
{
    /// <summary>
    /// Provides utilities to add, remove and fetch systems from Unity's recursive PlayerLoop structure
    ///
    /// Note that the simple APIs are easy to use but if you are doing multiple changes to the player loop you should
    /// consider the advanced APIs as it may be more performant than repeatedly fetching and writing the player loop 
    /// </summary>
    public static class PlayerLoopHelper
    {
        #region Simple API

        public static PlayerLoopSystem AttachSystem<TParent>(PlayerLoopSystem.UpdateFunction customUpdate)
        {
            PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();

            PlayerLoopSystem newSystem = new PlayerLoopSystem
            {
                updateDelegate = customUpdate,
                type = customUpdate.Method.DeclaringType
            };
            AddSystemToMatchedParent<TParent>(ref loop, newSystem);
            PlayerLoop.SetPlayerLoop(loop);

            return newSystem;
        }
        
        public static PlayerLoopSystem AttachSystem<TParent, TWrapper>(PlayerLoopSystem.UpdateFunction customUpdate)
        {
            PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();

            PlayerLoopSystem newSystem = new PlayerLoopSystem
            {
                updateDelegate = customUpdate,
                type = typeof(TWrapper)
            };
            AddSystemToMatchedParent<TParent>(ref loop, newSystem);
            PlayerLoop.SetPlayerLoop(loop);

            return newSystem;
        }

        public static bool DetachSystem(PlayerLoopSystem systemToRemove)
        {
            PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
            bool wasRemoved = RemoveSystem(ref loop, systemToRemove);
            PlayerLoop.SetPlayerLoop(loop);
            return wasRemoved;
        }

        public static PlayerLoopSystem GetSystemByType<T>()
        {
            PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
            return GetSystemByType<T>(loop);
        }

        public static PlayerLoopSystem GetSystemByTypeAndMethod<T>(PlayerLoopSystem.UpdateFunction customUpdate)
        {
            PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
            return GetSystemByTypeAndMethod<T>(loop, customUpdate);
        }
        #endregion

        #region Advanced API

        public static bool EqualsSystem(this PlayerLoopSystem a, PlayerLoopSystem b)
        {
            return a.type == b.type && a.updateDelegate == b.updateDelegate && a.updateFunction == b.updateFunction && a.loopConditionFunction == b.loopConditionFunction;
        }

        public static PlayerLoopSystem GetSystemByType<T>(this PlayerLoopSystem system)
        {
            if (system.type == typeof(T))
            {
                return system;
            }

            if(system.subSystemList != null)
            {
                foreach (PlayerLoopSystem subSystem in system.subSystemList)
                {
                    PlayerLoopSystem match = subSystem.GetSystemByType<T>();

                    if (match.type == typeof(T))
                    {
                        return match;
                    }
                }
            }

            return default;
        }
        
        public static PlayerLoopSystem GetSystemByTypeAndMethod<T>(this PlayerLoopSystem system, PlayerLoopSystem.UpdateFunction customUpdate)
        {
            if (system.type == typeof(T) && system.updateDelegate == customUpdate)
            {
                return system;
            }

            if(system.subSystemList != null)
            {
                foreach (PlayerLoopSystem subSystem in system.subSystemList)
                {
                    PlayerLoopSystem match = subSystem.GetSystemByTypeAndMethod<T>(customUpdate);

                    if (match.type == typeof(T) && match.updateDelegate == customUpdate)
                    {
                        return match;
                    }
                }
            }

            return default;
        }

        public static void AddSystemToMatchedParent<TParent>(ref PlayerLoopSystem system, PlayerLoopSystem newSystem)
        {
            if (system.type == typeof(TParent))
            {
                AddSystem(ref system, newSystem);
            }
            else if (system.subSystemList != null)
            {
                for (int index = 0; index < system.subSystemList.Length; index++)
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

        public static bool RemoveSystem(ref PlayerLoopSystem system, PlayerLoopSystem systemToRemove)
        {
            if (system.subSystemList != null)
            {
                bool contained = false;

                for (int i = 0; i < system.subSystemList.Length; i++)
                {
                    if (system.subSystemList[i].EqualsSystem(systemToRemove))
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
                        if (!system.subSystemList[i].EqualsSystem(systemToRemove))
                        {
                            subSystemList[index] = system.subSystemList[i];
                            index++;
                        }
                    }

                    system.subSystemList = subSystemList;
                    return true;
                }


                for (int index = 0; index < system.subSystemList.Length; index++)
                {
                    if (RemoveSystem(ref system.subSystemList[index], systemToRemove))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion
    }
}