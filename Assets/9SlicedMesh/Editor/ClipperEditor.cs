using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Sabresaurus.NineSlicedMesh
{
    [CustomEditor(typeof(Clipper))]
    public class ClipperEditor : Editor
    {
        protected virtual void OnSceneGUI()
        {
            Clipper castTarget = (Clipper) target;
            
            if(castTarget.HasClipperTransform)
                return;
            
            Handles.matrix = castTarget.transform.localToWorldMatrix;
            Clipper.PlaneData planeData = castTarget.PrimaryPlaneData;
            if (EditorTools.activeToolType.Name == "RotateTool")
            {
                EditorGUI.BeginChangeCheck();
                Quaternion newRotation = Handles.RotationHandle(planeData.PlaneOrientation, planeData.PointOnPlane);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(castTarget, "Change Look At Target Position");
                    planeData.PlaneOrientation = newRotation;
                    castTarget.Update();
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
                    castTarget.Update();
                }
            }
        }
    }
}