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
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCore
{
    [CustomEditor(typeof(SkinnedMeshRenderer)), CanEditMultipleObjects]
    public class SkinnedMeshRendererInspector : Editor
    {
        private Editor editor;

        public override void OnInspectorGUI()
        {
            var type = typeof(EditorUtility).Assembly.GetType("UnityEditor.SkinnedMeshRendererEditor");
            CreateCachedEditor(targets, type, ref editor);
            editor.OnInspectorGUI();
        }

        private void OnSceneGUI()
        {
            SkinnedMeshRenderer skinnedMeshRenderer = (SkinnedMeshRenderer) target;
            foreach (Transform bone in skinnedMeshRenderer.bones)
            {
                var direction = bone.transform.position - bone.parent.transform.position;
                Handles.ConeHandleCap(0, bone.transform.parent.position, Quaternion.LookRotation(direction),
                    direction.magnitude, Event.current.type);
                Handles.DrawLine(bone.transform.position, bone.parent.transform.position);
            }
        }
    }
}