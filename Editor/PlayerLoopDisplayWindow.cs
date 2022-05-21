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
        private Vector2 scrollPosition = Vector2.zero;
        private GUIStyle buttonStyle;

        private readonly Dictionary<Type, MonoScript> typesToMonoScripts = new Dictionary<Type, MonoScript>();

        private static bool HideNative
        {
            get => EditorPrefs.GetBool(nameof(PlayerLoopDisplayWindow) + "." + nameof(HideNative));
            set => EditorPrefs.SetBool(nameof(PlayerLoopDisplayWindow) + "." + nameof(HideNative), value);
        }

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
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft
            };

            HideNative = EditorGUILayout.Toggle("Hide Native", HideNative);

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
                    GUI.Button(buttonRect, "Root", buttonStyle);
                }
            }
            else if (system.type != null && !(system.updateFunction != (IntPtr) 0 && HideNative))
            {
                Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.button);
                float width = rect.width;
                rect.xMin += depth * 10;

                Rect buttonRect = rect;
                buttonRect.xMax = width * 0.7f;
                Rect labelRect = buttonRect;
                labelRect.x = width * 0.7f;

                var activeType = system.type;
                if (system.updateDelegate != null)
                {
                    activeType = system.updateDelegate.Method.DeclaringType;
                }

                using (new EditorGUI.DisabledScope(!typesToMonoScripts.ContainsKey(activeType)))
                {
                    string buttonLabel = activeType.Name;

                    if (system.updateDelegate != null)
                    {
                        buttonLabel += $".{system.updateDelegate.Method.Name}";

                        if (system.type != activeType)
                        {
                            // System type is not the same as the declaring type of the method, so make that visible
                            buttonLabel += $" ({system.type.Name})";
                        }
                    }

                    if (GUI.Button(buttonRect, new GUIContent(buttonLabel, system.type.FullName), buttonStyle))
                    {
                        AssetDatabase.OpenAsset(typesToMonoScripts[activeType]);
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