using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sabresaurus.SabreCore
{
    [InitializeOnLoad]
    public static class EditorWindowEditScript
    {
        static readonly Type hostViewType = typeof(EditorUtility).Assembly.GetType("UnityEditor.HostView");
        static readonly Type windowActionType = typeof(EditorUtility).Assembly.GetType("UnityEditor.WindowAction");

        static EditorWindowEditScript()
        {
            PropertyInfo windowActionsProperty = hostViewType.GetProperty("windowActions", BindingFlags.Static | BindingFlags.NonPublic);
            FieldInfo windowActionsField = hostViewType.GetField("s_windowActions", BindingFlags.Static | BindingFlags.NonPublic);

            // First get the window actions via the property as this will fill in the array if it's empty
            Array windowActions = (Array) windowActionsProperty.GetValue(null);

            object[] windowActionsToAdd =
            {
                CreateWindowAction("Execute", "Validate", "Edit Script"),
            };

            // Add the new element to the array by creating a new array and assigning it
            Array newWindowActions = Array.CreateInstance(windowActionType, windowActions.Length + windowActionsToAdd.Length);
            Array.Copy(windowActions, newWindowActions, windowActionsToAdd.Length);

            for (int i = 0; i < windowActionsToAdd.Length; i++)
            {
                newWindowActions.SetValue(windowActionsToAdd[i], windowActionsToAdd.Length - 1 + i);
            }

            windowActionsField.SetValue(null, newWindowActions);
        }

        static object CreateWindowAction(string executeMethodName, string validateMethodName, string menuName)
        {
            MethodInfo createMethodInfo = windowActionType.GetMethod("CreateWindowMenuItem", BindingFlags.Static | BindingFlags.Public);

            Type executeHandlerType = windowActionType.GetNestedType("ExecuteHandler");
            Type validateHandlerType = windowActionType.GetNestedType("ValidateHandler");

            Delegate executeDelegate = Delegate.CreateDelegate(executeHandlerType, typeof(EditorWindowEditScript).GetMethod(executeMethodName, BindingFlags.Static | BindingFlags.NonPublic));
            Delegate validateDelegate = Delegate.CreateDelegate(validateHandlerType, typeof(EditorWindowEditScript).GetMethod(validateMethodName, BindingFlags.Static | BindingFlags.NonPublic));

            object result = createMethodInfo.Invoke(null, new object[] {menuName, executeDelegate, menuName});

            windowActionType.GetField("validateHandler", BindingFlags.Public | BindingFlags.Instance).SetValue(result, validateDelegate);

            return result;
        }

        [UsedImplicitly]
        static void Execute(EditorWindow editorWindow, object windowAction)
        {
            MonoScript monoScript = MonoScript.FromScriptableObject(editorWindow);

            if (monoScript != null)
            {
                AssetDatabase.OpenAsset(monoScript);
            }
        }

        [UsedImplicitly]
        static bool Validate(EditorWindow editorWindow, object windowAction)
        {
            string assetPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(editorWindow));
            return assetPath.StartsWith("Assets/") || assetPath.StartsWith("Packages/");
        }
    }
}