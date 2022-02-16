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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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

            List<Object> meshes = new List<Object>();
            List<Object> additionalMeshes = new List<Object>();

            bool anyMeshRenderers = false;
            bool additionalMeshesMixed = false;

            for (int i = 0; i < targets.Length; i++)
            {
                MeshFilter meshFilter = (MeshFilter) targets[i];
                if (meshFilter.sharedMesh != null)
                {
                    meshes.Add(meshFilter.sharedMesh);
                }

                if (meshFilter.TryGetComponent(out MeshRenderer meshRenderer))
                {
                    anyMeshRenderers = true;
                    if (meshRenderer.additionalVertexStreams != null)
                    {
                        additionalMeshes.Add(meshRenderer.additionalVertexStreams);

                        if (additionalMeshes.Count >= 2 && additionalMeshes[i] != additionalMeshes[i - 1])
                        {
                            additionalMeshesMixed = true;
                        }
                    }
                }
            }

            // TODO - This line has an issue if one element is null
            CreateCachedEditor(meshes.ToArray(), null, ref meshFilterMeshEditor);

            // TODO - This line has an issue if one element is null
            CreateCachedEditor(additionalMeshes.ToArray(), null, ref meshRendererAdditionalStreamsEditor);

            DisplayMeshDetails = EditorGUILayout.BeginFoldoutHeaderGroup(DisplayMeshDetails, "Mesh Details");

            if (DisplayMeshDetails)
            {
                EditorGUI.indentLevel++;

                meshFilterMeshEditor?.OnInspectorGUI();

                if (anyMeshRenderers)
                {
                    GUI.enabled = true;
                    EditorGUILayout.Space(4f, true);
                    bool wasMixed = EditorGUI.showMixedValue;

                    EditorGUI.showMixedValue = additionalMeshesMixed;

                    EditorGUI.BeginChangeCheck();
                    Object firstAdditionalMesh = additionalMeshes.Count > 0 ? additionalMeshes[0] : null;
                    Mesh newValue = (Mesh) EditorGUILayout.ObjectField(new GUIContent("Additional Streams", "MeshRenderer.additionalVertexStreams"), firstAdditionalMesh, typeof(Mesh), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        for (int i = 0; i < targets.Length; i++)
                        {
                            MeshFilter meshFilter = (MeshFilter) targets[i];
                            meshes[i] = meshFilter.sharedMesh;
                    
                            if (meshFilter.TryGetComponent(out MeshRenderer meshRenderer))
                            {
                                Undo.RecordObject(meshRenderer, "Undo Inspector");
                                meshRenderer.additionalVertexStreams = newValue;
                            }
                        }
                    }

                    EditorGUI.showMixedValue = wasMixed;
                }

                if (additionalMeshes.Any(item => item != null))
                {
                    EditorGUILayout.Space(2f, true);
                    meshRendererAdditionalStreamsEditor.OnInspectorGUI();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void OnDestroy()
        {
            DestroyImmediate(meshFilterMeshEditor);
            DestroyImmediate(meshRendererAdditionalStreamsEditor);
        }
    }
}