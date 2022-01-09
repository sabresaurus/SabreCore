using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sabresaurus.NineSlicedMesh
{
    public enum TriangleClassification
    {
        Front,
        Straddle,
        Back
    }

    public class Classifier
    {
        /// <summary>
        /// Determines a triangle's relation to a plane (in front, behind, or with vertices straddling either side),
        /// uses a pre-classified array of which side each vertex is on.
        /// </summary>
        /// <param name="index1"></param>
        /// <param name="index2"></param>
        /// <param name="index3"></param>
        /// <param name="classificationArray"></param>
        /// <returns></returns>
        public static TriangleClassification ClassifyTriangle(int index1, int index2, int index3, NativeArray<int> classificationArray)
        {
            int numberInFront = 0;
            int numberBehind = 0;

            if (classificationArray[index1] == 1)
                numberInFront++;
            else
                numberBehind++;

            if (classificationArray[index2] == 1)
                numberInFront++;
            else
                numberBehind++;

            if (classificationArray[index3] == 1)
                numberInFront++;
            else
                numberBehind++;

            if (numberInFront == 0) // None in front, all must be behind
                return TriangleClassification.Back;
            if (numberBehind == 0) // None behind, all must be in front
                return TriangleClassification.Front;

            return TriangleClassification.Straddle;
        }
        
        /// <summary>
        /// When supplied with a vertex array and a plane this job will fill a buffer with 1 or -1 values based on which
        /// side of the plane each vertex is on
        /// </summary>
        [BurstCompile]
        public struct ClassifyVerticesAgainstPlane : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> vertices;

            [ReadOnly] public float3 planeNormal;
            [ReadOnly] public float planeDistance;

            public NativeArray<int> classificationResult;

            public void Execute(int index)
            {
                float3 point = vertices[index];
                float dot = math.dot(planeNormal, point) + planeDistance;
                classificationResult[index] = dot > 0f ? -1 : 1;
            }
        }
    }
}