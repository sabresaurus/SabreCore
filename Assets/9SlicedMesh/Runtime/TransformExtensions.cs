using UnityEngine;

namespace Sabresaurus.NineSlicedMesh
{
    public static class TransformExtensions
    {
        /// <summary>
        /// Equivalent of the built in transform.InverseTransformPoint but for Quaternion rotations
        /// </summary>
        public static Quaternion InverseTransformRotation(this Transform transform, Quaternion rotation)
        {
            return Quaternion.Inverse(transform.rotation) * rotation;
        }
    }
}