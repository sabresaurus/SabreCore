using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

[CustomEditor(typeof(MeshFilterTest)), CanEditMultipleObjects]
public class MeshFilterTestEditor : Editor
{
    protected virtual void OnSceneGUI()
    {
        MeshFilterTest castTarget = (MeshFilterTest) target;
        Handles.matrix = castTarget.transform.localToWorldMatrix;

        if (EditorTools.activeToolType.Name == "MoveTool")
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.PositionHandle(castTarget.PointOnPlane, castTarget.PlaneOrientation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(castTarget, "Change Look At Target Position");
                castTarget.PointOnPlane = newPosition;
            }
        }
        else if (EditorTools.activeToolType.Name == "RotateTool")
        {
            EditorGUI.BeginChangeCheck();
            Quaternion newRotation = Handles.RotationHandle(castTarget.PlaneOrientation, castTarget.PointOnPlane);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(castTarget, "Change Look At Target Position");
                castTarget.PlaneOrientation = newRotation;
            }
        }
    }
}