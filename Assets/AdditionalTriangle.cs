using UnityEngine;

namespace Sabresaurus.SabreSlice
{
    // When a non-linear triangle is split, the generated triangles (two or three triangles) will each reuse one or two
    // vertices from the original triangle. These two structs allow us to track the existing vertices as indices
    // Note that this could be further improved by not duplicating the new vertices multiple times if new triangles share
    // the same new vertices

    public struct NewTriangleWith1NewVertex
    {
        public int ExistingIndex1;
        public int ExistingIndex2;

        public Vector3 NewVertexPosition1;
        public Vector2 NewVertexUV1;
        public Vector3 NewVertexNormal1;
        public Vector3 NewVertexTangent1;

        public bool Flipped; // Is the winding order flipped?
    }

    public struct NewTriangleWith2NewVertex
    {
        public int ExistingIndex1;

        public Vector3 NewVertexPosition1;
        public Vector2 NewVertexUV1;
        public Vector3 NewVertexNormal1;
        public Vector3 NewVertexTangent1;

        public Vector3 NewVertexPosition2;
        public Vector2 NewVertexUV2;
        public Vector3 NewVertexNormal2;
        public Vector3 NewVertexTangent2;

        public bool Flipped; // Is the winding order flipped?
    }

//    public struct NewTriangleWith3NewVertex
//    {
//        public Vector3 NewVertexPosition1;
//        public Vector2 NewVertexUV1;
//        public Vector3 NewVertexNormal1;
//        public Vector3 NewVertexTangent1;
//
//        public Vector3 NewVertexPosition2;
//        public Vector2 NewVertexUV2;
//        public Vector3 NewVertexNormal2;
//        public Vector3 NewVertexTangent2;
//
//
//        public Vector3 NewVertexPosition3;
//        public Vector2 NewVertexUV3;
//        public Vector3 NewVertexNormal3;
//        public Vector3 NewVertexTangent3;
//
//        public bool Flipped;
//    }
}