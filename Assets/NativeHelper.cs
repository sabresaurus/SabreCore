using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Sabresaurus.SabreSlice
{
    public static class NativeHelper
    {
        // From https://gist.github.com/LotteMakesStuff/c2f9b764b15f74d14c00ceb4214356b4
        static unsafe NativeArray<float3> GetNativeVertexArrays(Vector3[] vertexArray)
        {
            // create a destination NativeArray to hold the vertices
            NativeArray<float3> verts = new NativeArray<float3>(vertexArray.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            // pin the mesh's vertex buffer in place...
            fixed (void* vertexBufferPointer = vertexArray)
            {
                // ...and use memcpy to copy the Vector3[] into a NativeArray<floar3> without casting. whould be fast!
                UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(verts),
                    vertexBufferPointer, vertexArray.Length * (long) UnsafeUtility.SizeOf<float3>());
            }
            // we only hve to fix the .net array in place, the NativeArray is allocated in the C++ side of the engine and
            // wont move arround unexpectedly. We have a pointer to it not a reference! thats basically what fixed does,
            // we create a scope where its 'safe' to get a pointer and directly manipulate the array

            return verts;
        }
    }
}