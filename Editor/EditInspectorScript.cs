using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCore
{
    /// <summary>
    /// Adds a context menu to component's that allows you to edit the active inspector script for them if it's
    /// accessible
    /// </summary>
    public class EditInspectorScript
    {
        [MenuItem("CONTEXT/Component/Edit Inspector Script")]
        static void EditInspector(MenuCommand command)
        {
            Editor editor = Editor.CreateEditor(command.context);

            MonoScript monoScript = MonoScript.FromScriptableObject(editor);
            if (monoScript != null)
            {
                AssetDatabase.OpenAsset(monoScript);
            }

            Object.DestroyImmediate(editor);
        }

        [MenuItem("CONTEXT/Component/Edit Inspector Script", true)]
        static bool EditInspectorValidate(MenuCommand command)
        {
            Editor editor = Editor.CreateEditor(command.context);

            MonoScript monoScript = MonoScript.FromScriptableObject(editor);
            Object.DestroyImmediate(editor);

            return monoScript != null && monoScript.hideFlags == HideFlags.None;
        }
    }
}