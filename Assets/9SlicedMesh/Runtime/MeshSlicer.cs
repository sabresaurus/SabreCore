using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Sabresaurus.NineSlicedMesh
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    public class MeshSlicer : MeshModifier
    {
        [SerializeField] private SlicerAxisData[] axisDatas = new SlicerAxisData[3];

        [SerializeField] private bool showDebug = false;

        [SerializeField] private Vector3 size;


        protected override void Reset()
        {
            base.Reset();

            ResetSize();
        }

        [ContextMenu("Reset Size")]
        void ResetSize()
        {
            size = sourceMesh.bounds.size;
        }

        private void Update()
        {
            for (int i = 0; i < 3; i++)
            {
                axisDatas[i].Configure(i, size, sourceMesh.bounds);
            }

            SliceMesh(false);
        }

        private void OnDrawGizmosSelected()
        {
            SliceMesh(true);
        }

        void SliceMesh(bool gizmosPass)
        {
            if (sourceMesh == null)
                return;

            Mesh activeMesh = Instantiate(sourceMesh);
            // Loop through each of the three axes, and apply slicing in each
            for (int i = 0; i < 3; i++)
            {
                //SliceMesh(activeMesh, gizmosPass, axisDatas[i]);
            }

            SliceMesh(activeMesh, gizmosPass, axisDatas[0]);
            //SliceMesh(activeMesh, gizmosPass, axisDatas[1]);
            SliceMesh(activeMesh, gizmosPass, axisDatas[2]);
        }

        void SliceMesh(Mesh activeMesh, bool gizmosPass, SlicerAxisData activeAxisData)
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            var vertices = activeMesh.vertices;
            var uv = activeMesh.uv;
            var normals = activeMesh.normals;
            var tangents = activeMesh.tangents;
            int[] triangles = activeMesh.triangles;
            // Second copy so that we can modify triangles buffer while iterating through it
            int[] newTriangles = activeMesh.triangles;

            List<NewTriangleWith1NewVertex> additionalTrianglesWith1NewVertices = new List<NewTriangleWith1NewVertex>();
            List<NewTriangleWith2NewVertex> additionalTrianglesWith2NewVertices = new List<NewTriangleWith2NewVertex>();

            NativeArray<float3> verticesNativeArray = new NativeArray<float3>(vertices.Length, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < vertices.Length; i++)
            {
                verticesNativeArray[i] = vertices[i];
            }

            NativeArray<int>[] classificationArrays = new NativeArray<int>[2];

            // Loop through all the planes and classify the source vertices relative to each plane
            for (var planeIndex = 0; planeIndex < 2; planeIndex++)
            {
                Plane plane = activeAxisData.CalculatePlane(planeIndex);

                classificationArrays[planeIndex] = new NativeArray<int>(vertices.Length, Allocator.TempJob,
                    NativeArrayOptions.UninitializedMemory);

                var jobData = new Classifier.ClassifyVerticesAgainstPlane()
                {
                    vertices = verticesNativeArray,
                    classificationResult = classificationArrays[planeIndex],
                    planeDistance = plane.distance,
                    planeNormal = plane.normal
                };

                var handle = jobData.Schedule(vertices.Length, 128);
                handle.Complete(); // Block until all jobs are done
            }

            for (var planeIndex = 0; planeIndex < 2; planeIndex++)
            {
                Plane plane = activeAxisData.CalculatePlane(planeIndex);

                var classificationArray = classificationArrays[planeIndex];
                for (int i = 0; i < triangles.Length / 3; i++)
                {
                    int index1 = triangles[i * 3 + 0];
                    int index2 = triangles[i * 3 + 1];
                    int index3 = triangles[i * 3 + 2];

                    Vector3 point1 = vertices[index1];
                    Vector3 point2 = vertices[index2];
                    Vector3 point3 = vertices[index3];
                    TriangleClassification triangleClassification =
                        Classifier.ClassifyTriangle(index1, index2, index3, classificationArray);

                    if (triangleClassification == TriangleClassification.Straddle)
                    {
                        int classificationSum = (classificationArray[index1] + classificationArray[index2] +
                                                 classificationArray[index3]);

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
                            float interpolantA = plane.GetPlaneIntersectionInterpolant(isolatedPoint, pointA);
                            float interpolantB = plane.GetPlaneIntersectionInterpolant(isolatedPoint, pointB);

                            Vector3 newPointA = Vector3.Lerp(isolatedPoint, pointA, interpolantA);
                            Vector3 newPointB = Vector3.Lerp(isolatedPoint, pointB, interpolantB);

                            Vector3 newUVA = Vector2.Lerp(uv[triangles[i * 3 + isolatedIndex]],
                                uv[triangles[i * 3 + indexA]], interpolantA);
                            Vector3 newUVB = Vector2.Lerp(uv[triangles[i * 3 + isolatedIndex]],
                                uv[triangles[i * 3 + indexB]], interpolantB);

                            Vector3 newNormalA = Vector3.Lerp(normals[triangles[i * 3 + isolatedIndex]],
                                normals[triangles[i * 3 + indexA]], interpolantA);
                            Vector3 newNormalB = Vector3.Lerp(normals[triangles[i * 3 + isolatedIndex]],
                                normals[triangles[i * 3 + indexB]], interpolantB);

                            Vector3 newTangentA = Vector3.Lerp(tangents[triangles[i * 3 + isolatedIndex]],
                                tangents[triangles[i * 3 + indexA]], interpolantA);
                            Vector3 newTangentB = Vector3.Lerp(tangents[triangles[i * 3 + isolatedIndex]],
                                tangents[triangles[i * 3 + indexB]], interpolantB);

                            // ORIGINAL TRIANGLE
                            if (showDebug && gizmosPass)
                            {
                                Gizmos.color = Color.green;
                                GizmoHelper.DrawTriangle(point1, point2, point3);
                            }

                            Vector3 transformedOffset = activeAxisData.GetTransformedOffset(planeIndex);

                            // NEW CLIPPED TRIANGLE
                            if (classificationSum == 1 + 1 - 1 // Two in front
                                || classificationSum == 1 - 1 - 1) // Two behind
                            {
                                additionalTrianglesWith1NewVertices.Add(new NewTriangleWith1NewVertex()
                                {
                                    ExistingIndex1 = triangles[i * 3 + indexA],
                                    ExistingIndex2 = triangles[i * 3 + indexB],

                                    NewVertexPosition1 = newPointA + transformedOffset,
                                    NewVertexUV1 = newUVA,
                                    NewVertexNormal1 = newNormalA,
                                    NewVertexTangent1 = newTangentA,

                                    Flipped = false,
                                });

                                // Add other triangle
                                additionalTrianglesWith2NewVertices.Add(new NewTriangleWith2NewVertex()
                                {
                                    ExistingIndex1 = triangles[i * 3 + indexB],

                                    NewVertexPosition1 = newPointA + transformedOffset,
                                    NewVertexUV1 = newUVA,
                                    NewVertexNormal1 = newNormalA,
                                    NewVertexTangent1 = newTangentA,

                                    NewVertexPosition2 = newPointB + transformedOffset,
                                    NewVertexUV2 = newUVB,
                                    NewVertexNormal2 = newNormalB,
                                    NewVertexTangent2 = newTangentB,

                                    Flipped = false,
                                });

                                if (showDebug && gizmosPass)
                                {
                                    Gizmos.color = Color.blue;
                                    GizmoHelper.DrawTriangle(pointB, pointA, newPointA);
                                    GizmoHelper.DrawTriangle(pointB, newPointA, newPointB);
                                }
                            }

                            if (classificationSum == 1 + 1 - 1 // Two in front
                                || classificationSum == 1 - 1 - 1) // Two behind
                            {
                                additionalTrianglesWith2NewVertices.Add(new NewTriangleWith2NewVertex()
                                {
                                    ExistingIndex1 = triangles[i * 3 + isolatedIndex],

                                    NewVertexPosition1 = newPointA + transformedOffset,
                                    NewVertexUV1 = newUVA,
                                    NewVertexNormal1 = newNormalA,
                                    NewVertexTangent1 = newTangentA,

                                    NewVertexPosition2 = newPointB + transformedOffset,
                                    NewVertexUV2 = newUVB,
                                    NewVertexNormal2 = newNormalB,
                                    NewVertexTangent2 = newTangentB,

                                    Flipped = true,
                                });
                            }

                            // DRAW SPLIT LINE
                            if (gizmosPass)
                            {
                                // Transformed
                                Gizmos.color = Color.blue;
                                Gizmos.DrawLine(newPointA + transformedOffset, newPointB + transformedOffset);
                            }
                        }
                        else
                        {
                            throw new NotSupportedException("Unexpected and unhandled classification sum");
                        }
                    }

                    if (triangleClassification == TriangleClassification.Straddle) 
                    {
                        // Zero out the indices for the straddling triangles as they are being replaced with new triangles
                        // Note: This is wasteful and in the future we should reuse these indices for one of the new triangles
                        newTriangles[i * 3 + 0] = 0;
                        newTriangles[i * 3 + 1] = 0;
                        newTriangles[i * 3 + 2] = 0;
                    }
                }
            }

            float sourceInset1 = sourceMesh.bounds.size[activeAxisData.AxisIndex] * (activeAxisData.Inset1);
            float sourceInset2 = sourceMesh.bounds.size[activeAxisData.AxisIndex] * (activeAxisData.Inset2);
            float scale = (size[activeAxisData.AxisIndex] - sourceInset1 - sourceInset2) /
                          (sourceMesh.bounds.size[activeAxisData.AxisIndex] - sourceInset1 - sourceInset2);

            for (int v = 0; v < vertices.Length; v++)
            {
                bool allBehind = true;
                for (var planeIndex = 0; planeIndex < 2; planeIndex++)
                {
                    if (classificationArrays[planeIndex][v] == -1)
                    {
                        vertices[v] += activeAxisData.GetTransformedOffset(planeIndex);
                        allBehind = false;
                    }
                }

                if (allBehind)
                {
                    var vertex = vertices[v];

                    vertex[activeAxisData.AxisIndex] *= scale;
                    vertices[v] = vertex;
                }
            }

            int baseTriangleCount = triangles.Length; // Before additional triangles start getting taken into account
            int baseVertexCount = vertices.Length; // Before additional triangles start getting taken into account

            int additionalTriangleCount =
                additionalTrianglesWith1NewVertices.Count + additionalTrianglesWith2NewVertices.Count;

            // Resize the triangle indices to accomodate the additional triangles we're adding
            Array.Resize(ref newTriangles, baseTriangleCount + (additionalTriangleCount * 3));

            // Resize the vertex attribute buffers to accomodate the additional triangles we're adding
            Array.Resize(ref vertices,
                baseVertexCount + additionalTrianglesWith1NewVertices.Count +
                additionalTrianglesWith2NewVertices.Count * 2);
            Array.Resize(ref uv,
                baseVertexCount + additionalTrianglesWith1NewVertices.Count +
                additionalTrianglesWith2NewVertices.Count * 2);
            Array.Resize(ref normals,
                baseVertexCount + additionalTrianglesWith1NewVertices.Count +
                additionalTrianglesWith2NewVertices.Count * 2);
            Array.Resize(ref tangents,
                baseVertexCount + additionalTrianglesWith1NewVertices.Count +
                additionalTrianglesWith2NewVertices.Count * 2);

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
                    newTriangles[baseTriangleCount + index * 3 + 2] = newTriangleWith1NewVertex.ExistingIndex1;
                    newTriangles[baseTriangleCount + index * 3 + 1] = newTriangleWith1NewVertex.ExistingIndex2;
                    newTriangles[baseTriangleCount + index * 3 + 0] = baseVertexCount + index;
                }
                else
                {
                    newTriangles[baseTriangleCount + index * 3 + 0] = newTriangleWith1NewVertex.ExistingIndex1;
                    newTriangles[baseTriangleCount + index * 3 + 1] = newTriangleWith1NewVertex.ExistingIndex2;
                    newTriangles[baseTriangleCount + index * 3 + 2] = baseVertexCount + index;
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
                    newTriangles[baseTriangleCount + offset * 3 + index * 3 + 0] =
                        newTriangleWith2NewVertex.ExistingIndex1;
                    newTriangles[baseTriangleCount + offset * 3 + index * 3 + 1] =
                        baseVertexCount + offset + index * 2 + 0;
                    newTriangles[baseTriangleCount + offset * 3 + index * 3 + 2] =
                        baseVertexCount + offset + index * 2 + 1;
                }
                else
                {
                    newTriangles[baseTriangleCount + offset * 3 + index * 3 + 2] =
                        newTriangleWith2NewVertex.ExistingIndex1;
                    newTriangles[baseTriangleCount + offset * 3 + index * 3 + 1] =
                        baseVertexCount + offset + index * 2 + 0;
                    newTriangles[baseTriangleCount + offset * 3 + index * 3 + 0] =
                        baseVertexCount + offset + index * 2 + 1;
                }
            }

            activeMesh.vertices = vertices;
            activeMesh.uv = uv;
            activeMesh.normals = normals;
            activeMesh.tangents = tangents;
            activeMesh.triangles = newTriangles;
            GetComponent<MeshFilter>().sharedMesh = activeMesh;

            // Cleanup the native arrays
            verticesNativeArray.Dispose();

            for (var planeIndex = 0; planeIndex < 2; planeIndex++)
            {
                classificationArrays[planeIndex].Dispose();
            }
        }
    }
}