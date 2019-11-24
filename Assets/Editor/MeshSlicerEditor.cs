using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Sabresaurus.SabreSlice
{
    [CustomEditor(typeof(MeshSlicer))]
    public class MeshSlicerEditor : Editor
    {
        protected virtual void OnSceneGUI()
        {
            MeshSlicer castTarget = (MeshSlicer) target;
            Handles.matrix = castTarget.transform.localToWorldMatrix;

            foreach (PlaneData planeData in castTarget.PlaneDatas)
            {
                if (EditorTools.activeToolType.Name == "RotateTool")
                {
                    EditorGUI.BeginChangeCheck();
                    Quaternion newRotation = Handles.RotationHandle(planeData.PlaneOrientation, planeData.PointOnPlane);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(castTarget, "Change Look At Target Position");
                        planeData.PlaneOrientation = newRotation;
                    }
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 newPosition = Handles.PositionHandle(planeData.PointOnPlane, planeData.PlaneOrientation);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(castTarget, "Change Look At Target Position");
                        planeData.PointOnPlane = newPosition;
                    }
                }
            }
        }
    }
}