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
        
        foreach (PlaneData planeData in castTarget.PlaneDatas)
        {
            if (EditorTools.activeToolType.Name == "MoveTool")
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newPosition = Handles.PositionHandle(planeData.PointOnPlane, planeData.PlaneOrientation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(castTarget, "Change Look At Target Position");
                    planeData.PointOnPlane = newPosition;
                }
            }
            else if (EditorTools.activeToolType.Name == "RotateTool")
            {
                EditorGUI.BeginChangeCheck();
                Quaternion newRotation = Handles.RotationHandle(planeData.PlaneOrientation, planeData.PointOnPlane);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(castTarget, "Change Look At Target Position");
                    planeData.PlaneOrientation = newRotation;
                }
            }
        }
    }
}