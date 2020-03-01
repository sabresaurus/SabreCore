using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sabresaurus.NineSlicedMesh
{
    [Serializable]
    public class SlicerAxisData
    {
        [SerializeField,HideInInspector] private int axisIndex = 0;
        [SerializeField,HideInInspector] private Vector3 planeDirection; // 
        [SerializeField,HideInInspector] private float planeDistance0;
        [SerializeField,HideInInspector] private float planeDistance1;
        [SerializeField, Range(0, 1)] private float inset1 = 0.1f;
        [SerializeField, Range(0, 1)] private float inset2 = 0.1f;
        [SerializeField,HideInInspector] private float sizeOffset = 0;

        public float Inset1 => inset1;

        public float Inset2 => inset2;

        public float Offset
        {
            get => sizeOffset;
            set => sizeOffset = value;
        }

        public int AxisIndex => axisIndex;

        public Vector3 GetTransformedOffset(int planeIndex)
        {
            return planeIndex == 1 ? 1 * sizeOffset * planeDirection : -1 * sizeOffset * planeDirection;
        }

        public Plane CalculatePlane(int planeIndex)
        {
            // Skip a sqrt here by using the parameterless constructor as we know normal is normalized
            return new Plane()
            {
                normal = planeIndex == 1 ? planeDirection : -planeDirection,
                distance = planeIndex == 1 ? planeDistance1 : -planeDistance0
            };
        }

        public void Configure(int axisIndex, Vector3 size, Bounds sourceBounds)
        {
            this.axisIndex = axisIndex;

            planeDirection = Vector3.zero;
            planeDirection[axisIndex] = 1;

            planeDistance0 = sourceBounds.center[axisIndex] + sourceBounds.size[axisIndex] * (-0.5f + 1f - inset1);
            planeDistance1 = sourceBounds.center[axisIndex] + sourceBounds.size[axisIndex] * (-0.5f + 1f - inset2) * -1;

            sizeOffset = (size[axisIndex] - sourceBounds.size[axisIndex]) / 2f;
        }
    }
}