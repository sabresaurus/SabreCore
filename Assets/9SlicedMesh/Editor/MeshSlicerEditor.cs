using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Sabresaurus.NineSlicedMesh
{
    [CustomEditor(typeof(MeshSlicer))]
    public class MeshSlicerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            MeshSlicer[] meshSlicers = Array.ConvertAll(targets, item => (MeshSlicer) item);

            if (GUILayout.Button("Reset Size"))
            {
                Undo.RecordObjects(targets, "Reset Size");
                foreach (var meshSlicer in meshSlicers)
                {
                    meshSlicer.ResetSize();
                }
            }
        }
    }
}