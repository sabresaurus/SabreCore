using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class PlaneData
{
    [SerializeField] private Vector3 pointOnPlane;
    [SerializeField] private Quaternion planeOrientation = Quaternion.identity;

    public Vector3 PointOnPlane
    {
        get => pointOnPlane;
        set => pointOnPlane = value;
    }

    public Quaternion PlaneOrientation
    {
        get => planeOrientation;
        set => planeOrientation = value;
    }
}

[RequireComponent(typeof(MeshFilter))]
public class MeshFilterTest : MonoBehaviour
{
    [SerializeField] private PlaneData[] planeDatas = new PlaneData[1];

    [SerializeField] private bool showDebug = true;

    public PlaneData[] PlaneDatas
    {
        get => planeDatas;
        set => planeDatas = value;
    }


    Plane CalculatePlane(PlaneData planeData)
    {
        return new Plane(planeData.PlaneOrientation * Vector3.forward, planeData.PointOnPlane);
    }


    [SerializeField] private Mesh sourceMesh;

    private void Reset()
    {
        sourceMesh = GetComponent<MeshFilter>().sharedMesh;
    }

    private void OnDrawGizmosSelected()
    {
        SliceMesh(sourceMesh);
    }

    void SliceMesh(Mesh sourceMesh)
    {
        //sourceMesh.isReadable


        Mesh newMesh = Instantiate(sourceMesh);
        Gizmos.matrix = transform.localToWorldMatrix;

        var vertices = sourceMesh.vertices;
        var uv = sourceMesh.uv;
        var normals = sourceMesh.normals;
        var tangents = sourceMesh.tangents;

//        foreach (Vector3 vertex in vertices)
//        {
//            Gizmos.color = (plane.GetSide(vertex)) ? Color.blue : Color.green;
//            Gizmos.DrawSphere(vertex, 0.01f);
//        }

        NativeArray<float3> verticesNativeArray = new NativeArray<float3>(vertices.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < vertices.Length; i++)
        {
            verticesNativeArray[i] = vertices[i];
        }

        foreach (PlaneData planeData in planeDatas)
        {
            var plane = CalculatePlane(planeData);
            NativeArray<float> classificationArray = new NativeArray<float>(vertices.Length, Allocator.TempJob, NativeArrayOptions.ClearMemory);

            var jobData = new ClassifyJobs.ClassifyVertices()
            {
                vertices = verticesNativeArray,
                classificationResult = classificationArray,
                planeDistance = plane.distance,
                planeNormal = plane.normal
            };

            var handle = jobData.Schedule(vertices.Length, 128);
            handle.Complete();

            int[] triangles = sourceMesh.triangles;


            List<NewTriangleWith1NewVertex> additionalTrianglesWith1NewVertices = new List<NewTriangleWith1NewVertex>();
            List<NewTriangleWith2NewVertex> additionalTrianglesWith2NewVertices = new List<NewTriangleWith2NewVertex>();

            for (int i = 0; i < triangles.Length / 3; i++)
            {
                int index1 = triangles[i * 3 + 0];
                int index2 = triangles[i * 3 + 1];
                int index3 = triangles[i * 3 + 2];

                Vector3 point1 = vertices[index1];
                Vector3 point2 = vertices[index2];
                Vector3 point3 = vertices[index3];
                Classification classification = Classify(index1, index2, index3, classificationArray);

                if (showDebug)
                {
                    if (classification == Classification.Front)
                        Gizmos.color = Color.red;
                    else if (classification == Classification.Back)
                        Gizmos.color = Color.blue;
                    else
                        Gizmos.color = Color.green;

                    Gizmos.DrawLine(point1, point2);
                    Gizmos.DrawLine(point2, point3);
                    Gizmos.DrawLine(point3, point1);
                }

                if (classification == Classification.Straddle)
                {
                    float classificationSum = (classificationArray[index1] + classificationArray[index2] + classificationArray[index3]);

                    if (classificationSum == 1 + 1 - 1 // Two in front
                        || classificationSum == 1 - 1 - 1) // Two behind 
                    {
                        // Which out which vertex is isolated at the back of the plane
                        int isolatedIndex = -1;
                        for (int j = 0; j < 3; j++)
                        {
                            if (classificationArray[triangles[i * 3 + j]] != classificationSum)
                            {
                                isolatedIndex = j;
                                break;
                            }
                        }

                        Debug.Assert(isolatedIndex >= 0 && isolatedIndex <= 2);

                        int indexA = (isolatedIndex + 1) % 3;
                        int indexB = (isolatedIndex + 2) % 3;

                        // Categorise the three points in the triangle, as the one isolated behind the plane, then the other two in winding order
                        Vector3 isolatedPoint = vertices[triangles[i * 3 + isolatedIndex]];
                        Vector3 pointA = vertices[triangles[i * 3 + indexA]];
                        Vector3 pointB = vertices[triangles[i * 3 + indexB]];

                        // Calculate the normalized intersection along each edge from the isolated point
                        float interpolantA = GetPlaneIntersectionInterpolant(plane, isolatedPoint, pointA);
                        float interpolantB = GetPlaneIntersectionInterpolant(plane, isolatedPoint, pointB);

                        Vector3 newPointA = Vector3.Lerp(isolatedPoint, pointA, interpolantA);
                        Vector3 newPointB = Vector3.Lerp(isolatedPoint, pointB, interpolantB);

                        Vector3 newUVA = Vector2.Lerp(uv[triangles[i * 3 + isolatedIndex]], uv[triangles[i * 3 + indexA]], interpolantA);
                        Vector3 newUVB = Vector2.Lerp(uv[triangles[i * 3 + isolatedIndex]], uv[triangles[i * 3 + indexB]], interpolantB);

                        Vector3 newNormalA = Vector3.Lerp(normals[triangles[i * 3 + isolatedIndex]], normals[triangles[i * 3 + indexA]], interpolantA);
                        Vector3 newNormalB = Vector3.Lerp(normals[triangles[i * 3 + isolatedIndex]], normals[triangles[i * 3 + indexB]], interpolantB);

                        Vector3 newTangentA = Vector3.Lerp(tangents[triangles[i * 3 + isolatedIndex]], tangents[triangles[i * 3 + indexA]], interpolantA);
                        Vector3 newTangentB = Vector3.Lerp(tangents[triangles[i * 3 + isolatedIndex]], tangents[triangles[i * 3 + indexB]], interpolantB);

                        // ORIGINAL TRIANGLE
//                    Gizmos.color = Color.green;
//                    DrawTriangle(point1, point2, point3);

                        // NEW CLIPPED TRIANGLE
                        Gizmos.color = Color.blue;

                        additionalTrianglesWith1NewVertices.Add(new NewTriangleWith1NewVertex()
                        {
                            ExistingIndex1 = triangles[i * 3 + indexA],
                            ExistingIndex2 = triangles[i * 3 + indexB],

                            NewVertexPosition1 = newPointA,
                            NewVertexUV1 = newUVA,
                            NewVertexNormal1 = newNormalA,
                            NewVertexTangent1 = newTangentA,

                            Flipped = false,
                        });

                        DrawTriangle(pointB, pointA, newPointA);

                        // Add other triangle

                        additionalTrianglesWith2NewVertices.Add(new NewTriangleWith2NewVertex()
                        {
                            ExistingIndex1 = triangles[i * 3 + indexB],

                            NewVertexPosition1 = newPointA,
                            NewVertexUV1 = newUVA,
                            NewVertexNormal1 = newNormalA,
                            NewVertexTangent1 = newTangentA,

                            NewVertexPosition2 = newPointB,
                            NewVertexUV2 = newUVB,
                            NewVertexNormal2 = newNormalB,
                            NewVertexTangent2 = newTangentB,

                            Flipped = false,
                        });

                        DrawTriangle(pointB, newPointA, newPointB);

                        additionalTrianglesWith2NewVertices.Add(new NewTriangleWith2NewVertex()
                        {
                            ExistingIndex1 = triangles[i * 3 + isolatedIndex],

                            NewVertexPosition1 = newPointA,
                            NewVertexUV1 = newUVA,
                            NewVertexNormal1 = newNormalA,
                            NewVertexTangent1 = newTangentA,

                            NewVertexPosition2 = newPointB,
                            NewVertexUV2 = newUVB,
                            NewVertexNormal2 = newNormalB,
                            NewVertexTangent2 = newTangentB,

                            Flipped = true,
                        });

                        // DRAW SPLIT LINE
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(newPointA, newPointB);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }

                if (classification == Classification.Straddle)
                {
                    triangles[i * 3 + 0] = 0;
                    triangles[i * 3 + 1] = 0;
                    triangles[i * 3 + 2] = 0;
                }
            }

            int baseTriangleCount = triangles.Length; // Before additional triangles start getting taken into account
            int baseVertexCount = vertices.Length; // Before additional triangles start getting taken into account

            int additionalTriangleCount = additionalTrianglesWith1NewVertices.Count + additionalTrianglesWith2NewVertices.Count;

            // Resize the triangle indices to accomodate the additional triangles we're adding
            Array.Resize(ref triangles, baseTriangleCount + (additionalTriangleCount * 3));

            // Resize the vertex attribute buffers to accomodate the additional triangles we're adding
            Array.Resize(ref vertices, baseVertexCount + additionalTrianglesWith1NewVertices.Count + additionalTrianglesWith2NewVertices.Count * 2);
            Array.Resize(ref uv, baseVertexCount + additionalTrianglesWith1NewVertices.Count + additionalTrianglesWith2NewVertices.Count * 2);
            Array.Resize(ref normals, baseVertexCount + additionalTrianglesWith1NewVertices.Count + additionalTrianglesWith2NewVertices.Count * 2);
            Array.Resize(ref tangents, baseVertexCount + additionalTrianglesWith1NewVertices.Count + additionalTrianglesWith2NewVertices.Count * 2);

            // Add in new triangles with one new vertex
            for (var index = 0; index < additionalTrianglesWith1NewVertices.Count; index++)
            {
                NewTriangleWith1NewVertex newTriangleWith1NewVertex = additionalTrianglesWith1NewVertices[index];

                vertices[baseVertexCount + index] = newTriangleWith1NewVertex.NewVertexPosition1;
                uv[baseVertexCount + index] = newTriangleWith1NewVertex.NewVertexUV1;
                normals[baseVertexCount + index] = newTriangleWith1NewVertex.NewVertexNormal1;
                tangents[baseVertexCount + index] = newTriangleWith1NewVertex.NewVertexTangent1;

                if (newTriangleWith1NewVertex.Flipped)
                {
                    triangles[baseTriangleCount + index * 3 + 2] = newTriangleWith1NewVertex.ExistingIndex1;
                    triangles[baseTriangleCount + index * 3 + 1] = newTriangleWith1NewVertex.ExistingIndex2;
                    triangles[baseTriangleCount + index * 3 + 0] = baseVertexCount + index;
                }
                else
                {
                    triangles[baseTriangleCount + index * 3 + 0] = newTriangleWith1NewVertex.ExistingIndex1;
                    triangles[baseTriangleCount + index * 3 + 1] = newTriangleWith1NewVertex.ExistingIndex2;
                    triangles[baseTriangleCount + index * 3 + 2] = baseVertexCount + index;
                }
            }

            int offset = additionalTrianglesWith1NewVertices.Count; // Offset to accomodate the new triangles we added

            // Add in new triangles with two new vertices
            for (var index = 0; index < additionalTrianglesWith2NewVertices.Count; index++)
            {
                NewTriangleWith2NewVertex newTriangleWith2NewVertex = additionalTrianglesWith2NewVertices[index];

                vertices[baseVertexCount + offset + index * 2 + 0] = newTriangleWith2NewVertex.NewVertexPosition1;
                uv[baseVertexCount + offset + index * 2 + 0] = newTriangleWith2NewVertex.NewVertexUV1;
                normals[baseVertexCount + offset + index * 2 + 0] = newTriangleWith2NewVertex.NewVertexNormal1;
                tangents[baseVertexCount + offset + index * 2 + 0] = newTriangleWith2NewVertex.NewVertexTangent1;

                vertices[baseVertexCount + offset + index * 2 + 1] = newTriangleWith2NewVertex.NewVertexPosition2;
                uv[baseVertexCount + offset + index * 2 + 1] = newTriangleWith2NewVertex.NewVertexUV2;
                normals[baseVertexCount + offset + index * 2 + 1] = newTriangleWith2NewVertex.NewVertexNormal2;
                tangents[baseVertexCount + offset + index * 2 + 1] = newTriangleWith2NewVertex.NewVertexTangent2;

                if (newTriangleWith2NewVertex.Flipped)
                {
                    triangles[baseTriangleCount + offset * 3 + index * 3 + 0] = newTriangleWith2NewVertex.ExistingIndex1;
                    triangles[baseTriangleCount + offset * 3 + index * 3 + 1] = baseVertexCount + offset + index * 2 + 0;
                    triangles[baseTriangleCount + offset * 3 + index * 3 + 2] = baseVertexCount + offset + index * 2 + 1;
                }
                else
                {
                    triangles[baseTriangleCount + offset * 3 + index * 3 + 2] = newTriangleWith2NewVertex.ExistingIndex1;
                    triangles[baseTriangleCount + offset * 3 + index * 3 + 1] = baseVertexCount + offset + index * 2 + 0;
                    triangles[baseTriangleCount + offset * 3 + index * 3 + 0] = baseVertexCount + offset + index * 2 + 1;
                }
            }


            newMesh.vertices = vertices;
            newMesh.uv = uv;
            newMesh.normals = normals;
            newMesh.tangents = tangents;
            newMesh.triangles = triangles;
            GetComponent<MeshFilter>().sharedMesh = newMesh;

            classificationArray.Dispose();
        }

        verticesNativeArray.Dispose();
    }

    static void DrawTriangle(Vector3 point1, Vector3 point2, Vector3 point3)
    {
        Gizmos.DrawLine(point1, point2);
        Gizmos.DrawLine(point2, point3);
        Gizmos.DrawLine(point3, point1);
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

    // From https://gist.github.com/LotteMakesStuff/c2f9b764b15f74d14c00ceb4214356b4
    unsafe NativeArray<float3> GetNativeVertexArrays(Vector3[] vertexArray)
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

    static Classification Classify(int index1, int index2, int index3, NativeArray<float> classificationArray)
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
            return Classification.Back;
        if (numberBehind == 0) // None behind, all must be in front
            return Classification.Front;

        return Classification.Straddle;
    }

    enum Classification
    {
        Front,
        Straddle,
        Back
    }
}