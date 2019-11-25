using UnityEngine;

namespace Sabresaurus.SabreSlice
{
    public static class PlaneExtensions
    {
        // Adapted from SabreCSG
        /// <summary>
        /// Gets the normalized interpolant between <paramref name="point1"/> and <paramref name="point2"/> where the edge they
        /// represent intersects with the supplied <paramref name="plane"/>.
        /// </summary>
        /// <param name="plane">The plane that intersects with the edge.</param>
        /// <param name="point1">The first point of the edge.</param>
        /// <param name="point2">The last point of the edge.</param>
        /// <returns>The normalized interpolant between the edge points where the plane intersects.</returns>
        public static float GetPlaneIntersectionInterpolant(this Plane plane, Vector3 point1, Vector3 point2)
        {
            float interpolant = (-plane.normal.x * point1.x - plane.normal.y * point1.y - plane.normal.z * point1.z - plane.distance)
                                / (-plane.normal.x * (point1.x - point2.x) - plane.normal.y * (point1.y - point2.y) - plane.normal.z * (point1.z - point2.z));

            return interpolant;
        }
    }
}