using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sabresaurus.SabreSlice
{
    public class ClassifyJobs
    {
        [BurstCompile]
        public struct ClassifyVertices : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> vertices;

            [ReadOnly] public float3 planeNormal;
            [ReadOnly] public float planeDistance;

            public NativeArray<float> classificationResult;

            public void Execute(int index)
            {
                float3 point = vertices[index];
                float dot = math.dot(planeNormal, point) + planeDistance;
                classificationResult[index] = dot > 0f ? -1 : 1;
            }
        }
    }
}