// MIT License
// 
// Copyright (c) 2021 Sabresaurus
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCore
{
    [CustomEditor(typeof(MeshFilter)), CanEditMultipleObjects]
    public class MeshFilterInspector : Editor
    {
        private Editor meshFilterMeshEditor = null;
        private Editor meshRendererAdditionalStreamsEditor = null;

        private static bool DisplayMeshDetails
        {
            get => EditorPrefs.GetBool(nameof(MeshFilterInspector) + "." + nameof(DisplayMeshDetails));
            set => EditorPrefs.SetBool(nameof(MeshFilterInspector) + "." + nameof(DisplayMeshDetails), value);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Object[] meshes = new Object[targets.Length];
            Object[] additionalMeshes = new Object[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                MeshFilter meshFilter = (MeshFilter) targets[i];
                meshes[i] = meshFilter.sharedMesh;

                if (meshFilter.TryGetComponent(out MeshRenderer meshRenderer))
                {
                    additionalMeshes[i] = meshRenderer.additionalVertexStreams;
                }
            }

            CreateCachedEditor(meshes, null, ref meshFilterMeshEditor);
            CreateCachedEditor(additionalMeshes, null, ref meshRendererAdditionalStreamsEditor);

            DisplayMeshDetails = EditorGUILayout.BeginFoldoutHeaderGroup(DisplayMeshDetails, "Mesh Details");

            if (DisplayMeshDetails)
            {
                EditorGUI.indentLevel++;

                meshFilterMeshEditor.OnInspectorGUI();

                if (additionalMeshes.Any(item => item != null))
                {
                    GUI.enabled = true;
                    EditorGUILayout.Space(4f, true);
                    GUILayout.Label("Additional Vertex Streams (MeshRenderer)", EditorStyles.boldLabel);
                    EditorGUILayout.Space(2f, true);
                    meshRendererAdditionalStreamsEditor.OnInspectorGUI();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}