using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Sabresaurus.SabreCore
{
    // Special thanks to https://medium.com/@thebeardphantom/unity-2018-and-playerloop-5c46a12a677
    public class PlayerLoopDisplayWindow : EditorWindow
    {
        Vector2 scrollPosition = Vector2.zero;

        private readonly Dictionary<Type, MonoScript> typesToMonoScripts = new Dictionary<Type, MonoScript>();

        private void OnEnable()
        {
            MonoScript[] monoScripts = Resources.FindObjectsOfTypeAll<MonoScript>();

            foreach (MonoScript monoScript in monoScripts)
            {
                Type type = monoScript.GetClass();
                if (type != null)
                {
                    typesToMonoScripts[type] = monoScript;
                }
            }

        }

        [MenuItem("Window/Player Loop")]
        private static void Init()
        {
            GetWindow<PlayerLoopDisplayWindow>("Player Loop").Show();
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Restore Default Loop"))
            {
                PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());
            }

            EditorGUILayout.Space();

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            DisplayLoopRecursively(PlayerLoop.GetCurrentPlayerLoop());
            GUILayout.EndScrollView();
        }

        private static string GetFlags(PlayerLoopSystem system)
        {
            if (system.updateFunction != (IntPtr) 0)
            {
                return "Native Function";
            }

            if (system.updateDelegate != null)
            {
                return "Custom Method";
            }

            return "No Mapping";
        }

        private void DisplayLoopRecursively(PlayerLoopSystem system, int depth = 0)
        {
            if (depth == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.button);
                    float width = rect.width;
                    Rect buttonRect = rect;
                    buttonRect.xMax = width * 0.7f;
                    GUI.Button(buttonRect, "Root");
                }
            }
            else if (system.type != null)
            {
                Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.button);
                float width = rect.width;
                rect.xMin += depth * 10;

                Rect buttonRect = rect;
                buttonRect.xMax = width * 0.7f;
                Rect labelRect = buttonRect;
                labelRect.x = width * 0.7f;

                using(new EditorGUI.DisabledScope(!typesToMonoScripts.ContainsKey(system.type)))
                {
                    if (GUI.Button(buttonRect, new GUIContent(system.type.Name, system.type.FullName)))
                    {
                        AssetDatabase.OpenAsset(typesToMonoScripts[system.type]);
                    }
                }

                if (system.loopConditionFunction != (IntPtr) 0)
                {
                    GUI.Label(labelRect, GetFlags(system) + ", Has Loop Condition");
                }
                else
                {
                    GUI.Label(labelRect, GetFlags(system));
                }
            }

            if (system.subSystemList == null) return;
            foreach (PlayerLoopSystem subSystem in system.subSystemList)
            {
                DisplayLoopRecursively(subSystem, depth + 1);
            }
        }
    }
}