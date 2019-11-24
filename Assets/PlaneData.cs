using System;
using UnityEngine;

namespace Sabresaurus.SabreSlice
{
    [Serializable]
    public class PlaneData
    {
        [SerializeField] private Vector3 pointOnPlane;
        [SerializeField] private Vector3 planeEuler;
        [SerializeField] private float offset = 0;

        public Vector3 PointOnPlane
        {
            get => pointOnPlane;
            set => pointOnPlane = value;
        }

        public Quaternion PlaneOrientation
        {
            get => Quaternion.Euler(planeEuler);
            set => planeEuler= value.eulerAngles;
        }

        public Vector3 TransformedOffset => PlaneOrientation * Vector3.forward * -offset;

        public Plane CalculatePlane()
        {
            Vector3 normal = PlaneOrientation * Vector3.forward;
            
            // Skip a sqrt here by using the parameterless constructor as we know normal is normalized
            return new Plane()
            {
                normal = normal,
                distance = -Vector3.Dot(normal, pointOnPlane)
            };
        }
        
        // From SabreCSG
        /// <summary>
        /// Gets the normalized interpolant between <paramref name="point1"/> and <paramref name="point2"/> where the edge they
        /// represent intersects with the supplied <paramref name="plane"/>.
        /// </summary>
        /// <param name="plane">The plane that intersects with the edge.</param>
        /// <param name="point1">The first point of the edge.</param>
        /// <param name="point2">The last point of the edge.</param>
        /// <returns>The normalized interpolant between the edge points where the plane intersects.</returns>
        public static float GetPlaneIntersectionInterpolant(Plane plane, Vector3 point1, Vector3 point2)
        {
            float interpolant = (-plane.normal.x * point1.x - plane.normal.y * point1.y - plane.normal.z * point1.z - plane.distance)
                                / (-plane.normal.x * (point1.x - point2.x) - plane.normal.y * (point1.y - point2.y) - plane.normal.z * (point1.z - point2.z));

            return interpolant;
        }
    }
}