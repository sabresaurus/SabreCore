using UnityEngine;

namespace Sabresaurus.SabreSlice
{
    public static class GizmoHelper
    {
        public static void DrawTriangle(Vector3 point1, Vector3 point2, Vector3 point3)
        {
            Gizmos.DrawLine(point1, point2);
            Gizmos.DrawLine(point2, point3);
            Gizmos.DrawLine(point3, point1);
        }
    }
}