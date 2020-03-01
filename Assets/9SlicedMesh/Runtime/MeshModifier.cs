using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sabresaurus.NineSlicedMesh
{
    [RequireComponent(typeof(MeshFilter))]
    public abstract class MeshModifier : MonoBehaviour
    {
        [SerializeField] protected Mesh sourceMesh;

        protected virtual void Reset()
        {
            sourceMesh = GetComponent<MeshFilter>().sharedMesh;
        }

#if UNITY_EDITOR
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
#endif
    }
}