using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreSlice
{
    [RequireComponent(typeof(MeshFilter))]
    public abstract class MeshModifier : MonoBehaviour
    {
        [SerializeField] protected Mesh sourceMesh;

        protected virtual void Reset()
        {
            sourceMesh = GetComponent<MeshFilter>().sharedMesh;
        }

        /// <summary>
        /// Removes this component and restores the mapped source mesh
        /// </summary>
        [ContextMenu("Remove and Restore")]
        protected virtual void RemoveAndRestore()
        {
            Undo.RecordObject(GetComponent<MeshFilter>(), "Remove and Restore");
            GetComponent<MeshFilter>().sharedMesh = sourceMesh;
            Undo.DestroyObjectImmediate(this);
        }
    }
}