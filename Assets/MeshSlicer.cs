using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Sabresaurus.SabreSlice
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    public class MeshSlicer : MeshModifier
    {
        [Serializable]
        public class PlaneData
        {
            [SerializeField] private Vector3 pointOnPlane;
            [SerializeField] private Vector3 planeEuler;
            [SerializeField, Range(0, 1)] private float inset = 0;
            [SerializeField] private float offset = 0;

            public Vector3 PointOnPlane
            {
                get => pointOnPlane;
                set => pointOnPlane = value;
            }

            public Quaternion PlaneOrientation
            {
                get => Quaternion.Euler(planeEuler);
                set => planeEuler = value.eulerAngles;
            }

            public float Inset
            {
                get => inset;
                set => inset = value;
            }

            public float Offset
            {
                get => offset;
                set => offset = value;
            }
            public Vector3 TransformedOffset => PlaneOrientation * Vector3.forward * offset;

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
        }

        [SerializeField] private PlaneData[] planeDatas = new PlaneData[2];

        [SerializeField] private bool showDebug = true;

        [SerializeField, Min(0)] private float width;


        public PlaneData[] PlaneDatas
        {
            get => planeDatas;
            set => planeDatas = value;
        }

        private void OnDrawGizmosSelected()
        {
            SliceMesh(sourceMesh);
        }

        protected override void Reset()
        {
            base.Reset();

            width = sourceMesh.bounds.size.x;
        }

        private void Update()
        {
            planeDatas[0].PlaneOrientation = Quaternion.Euler(0, 270, 0);
            planeDatas[0].PointOnPlane = Vector3.left * sourceMesh.bounds.size.x * 0.5f * (1f - planeDatas[0].Inset);
            planeDatas[0].Offset = (width - sourceMesh.bounds.size.x) / 2f;
            planeDatas[1].PlaneOrientation = Quaternion.Euler(0, 90, 0);
            planeDatas[1].PointOnPlane = Vector3.right * sourceMesh.bounds.size.x * 0.5f * (1f - planeDatas[1].Inset);
            planeDatas[1].Offset = (width - sourceMesh.bounds.size.x) / 2f;
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
            int[] triangles = sourceMesh.triangles;
            // Second copy so that we can modify triangles buffer while iterating through it
            int[] newTriangles = sourceMesh.triangles;


            List<NewTriangleWith1NewVertex> additionalTrianglesWith1NewVertices = new List<NewTriangleWith1NewVertex>();
            List<NewTriangleWith2NewVertex> additionalTrianglesWith2NewVertices = new List<NewTriangleWith2NewVertex>();

            NativeArray<float3> verticesNativeArray = new NativeArray<float3>(vertices.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < vertices.Length; i++)
            {
                verticesNativeArray[i] = vertices[i];
            }

            NativeArray<float>[] classificationArrays = new NativeArray<float>[planeDatas.Length];

            // Loop through all the planes and classify the source vertices relative to each plane
            for (var planeIndex = 0; planeIndex < planeDatas.Length; planeIndex++)
            {
                PlaneData planeData = planeDatas[planeIndex];
                Plane plane = planeData.CalculatePlane();
                classificationArrays[planeIndex] = new NativeArray<float>(vertices.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                var jobData = new ClassifyJobs.ClassifyVertices()
                {
                    vertices = verticesNativeArray,
                    classificationResult = classificationArrays[planeIndex],
                    planeDistance = plane.distance,
                    planeNormal = plane.normal
                };

                var handle = jobData.Schedule(vertices.Length, 128);
                handle.Complete(); // Block until all jobs are done
            }

            for (var planeIndex = 0; planeIndex < planeDatas.Length; planeIndex++)
            {
                PlaneData planeData = planeDatas[planeIndex];
                Plane plane = planeData.CalculatePlane();

                var classificationArray = classificationArrays[planeIndex];
                for (int i = 0; i < triangles.Length / 3; i++)
                {
                    int index1 = triangles[i * 3 + 0];
                    int index2 = triangles[i * 3 + 1];
                    int index3 = triangles[i * 3 + 2];

                    Vector3 point1 = vertices[index1];
                    Vector3 point2 = vertices[index2];
                    Vector3 point3 = vertices[index3];
                    Classification classification = Classifier.Classify(index1, index2, index3, classificationArray);

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
                            float interpolantA = plane.GetPlaneIntersectionInterpolant(isolatedPoint, pointA);
                            float interpolantB = plane.GetPlaneIntersectionInterpolant(isolatedPoint, pointB);

                            Vector3 newPointA = Vector3.Lerp(isolatedPoint, pointA, interpolantA);
                            Vector3 newPointB = Vector3.Lerp(isolatedPoint, pointB, interpolantB);

                            Vector3 newUVA = Vector2.Lerp(uv[triangles[i * 3 + isolatedIndex]], uv[triangles[i * 3 + indexA]], interpolantA);
                            Vector3 newUVB = Vector2.Lerp(uv[triangles[i * 3 + isolatedIndex]], uv[triangles[i * 3 + indexB]], interpolantB);

                            Vector3 newNormalA = Vector3.Lerp(normals[triangles[i * 3 + isolatedIndex]], normals[triangles[i * 3 + indexA]], interpolantA);
                            Vector3 newNormalB = Vector3.Lerp(normals[triangles[i * 3 + isolatedIndex]], normals[triangles[i * 3 + indexB]], interpolantB);

                            Vector3 newTangentA = Vector3.Lerp(tangents[triangles[i * 3 + isolatedIndex]], tangents[triangles[i * 3 + indexA]], interpolantA);
                            Vector3 newTangentB = Vector3.Lerp(tangents[triangles[i * 3 + isolatedIndex]], tangents[triangles[i * 3 + indexB]], interpolantB);

                            // ORIGINAL TRIANGLE
                            if (showDebug)
                            {
                                Gizmos.color = Color.green;
                                GizmoHelper.DrawTriangle(point1, point2, point3);
                            }

                            // NEW CLIPPED TRIANGLE
                            if (classificationSum == 1 + 1 - 1 // Two in front
                                || classificationSum == 1 - 1 - 1) // Two behind
                            {
                                additionalTrianglesWith1NewVertices.Add(new NewTriangleWith1NewVertex()
                                {
                                    ExistingIndex1 = triangles[i * 3 + indexA],
                                    ExistingIndex2 = triangles[i * 3 + indexB],

                                    NewVertexPosition1 = newPointA + planeData.TransformedOffset,
                                    NewVertexUV1 = newUVA,
                                    NewVertexNormal1 = newNormalA,
                                    NewVertexTangent1 = newTangentA,

                                    Flipped = false,
                                });

                                // Add other triangle
                                additionalTrianglesWith2NewVertices.Add(new NewTriangleWith2NewVertex()
                                {
                                    ExistingIndex1 = triangles[i * 3 + indexB],

                                    NewVertexPosition1 = newPointA + planeData.TransformedOffset,
                                    NewVertexUV1 = newUVA,
                                    NewVertexNormal1 = newNormalA,
                                    NewVertexTangent1 = newTangentA,

                                    NewVertexPosition2 = newPointB + planeData.TransformedOffset,
                                    NewVertexUV2 = newUVB,
                                    NewVertexNormal2 = newNormalB,
                                    NewVertexTangent2 = newTangentB,

                                    Flipped = false,
                                });

                                if (showDebug)
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

                                    NewVertexPosition1 = newPointA + planeData.TransformedOffset,
                                    NewVertexUV1 = newUVA,
                                    NewVertexNormal1 = newNormalA,
                                    NewVertexTangent1 = newTangentA,

                                    NewVertexPosition2 = newPointB + planeData.TransformedOffset,
                                    NewVertexUV2 = newUVB,
                                    NewVertexNormal2 = newNormalB,
                                    NewVertexTangent2 = newTangentB,

                                    Flipped = true,
                                });
                            }

                            // DRAW SPLIT LINE
//                        if (showDebug)
                            {
                                Gizmos.color = Color.red;
                                // Untransformed
                                Gizmos.DrawLine(newPointA, newPointB);
                                // Transformed
                                Gizmos.DrawLine(newPointA + planeData.TransformedOffset, newPointB + planeData.TransformedOffset);
                            }
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }

                    if (classification == Classification.Straddle)
                    {
                        newTriangles[i * 3 + 0] = 0;
                        newTriangles[i * 3 + 1] = 0;
                        newTriangles[i * 3 + 2] = 0;
                    }
                }
            }

            float a = sourceMesh.bounds.size.x * 0.5f * (planeDatas[0].Inset);
            float b = sourceMesh.bounds.size.x * 0.5f * (planeDatas[1].Inset);
            float scale = (width - a - b) / (sourceMesh.bounds.size.x - a - b);

            for (int v = 0; v < vertices.Length; v++)
            {
                bool allBehind = true;
                for (var planeIndex = 0; planeIndex < planeDatas.Length; planeIndex++)
                {
                    var planeData = planeDatas[planeIndex];
                    if (classificationArrays[planeIndex][v] == -1)
                    {
                        vertices[v] += planeData.TransformedOffset;
                        allBehind = false;
                    }
                }

                if (allBehind)
                {
                    var vertex = vertices[v];
                    
                    vertex.x *= scale;
                    vertices[v] = vertex;
                }
            }

            int baseTriangleCount = triangles.Length; // Before additional triangles start getting taken into account
            int baseVertexCount = vertices.Length; // Before additional triangles start getting taken into account

            int additionalTriangleCount = additionalTrianglesWith1NewVertices.Count + additionalTrianglesWith2NewVertices.Count;

            // Resize the triangle indices to accomodate the additional triangles we're adding
            Array.Resize(ref newTriangles, baseTriangleCount + (additionalTriangleCount * 3));

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
                    newTriangles[baseTriangleCount + offset * 3 + index * 3 + 0] = newTriangleWith2NewVertex.ExistingIndex1;
                    newTriangles[baseTriangleCount + offset * 3 + index * 3 + 1] = baseVertexCount + offset + index * 2 + 0;
                    newTriangles[baseTriangleCount + offset * 3 + index * 3 + 2] = baseVertexCount + offset + index * 2 + 1;
                }
                else
                {
                    newTriangles[baseTriangleCount + offset * 3 + index * 3 + 2] = newTriangleWith2NewVertex.ExistingIndex1;
                    newTriangles[baseTriangleCount + offset * 3 + index * 3 + 1] = baseVertexCount + offset + index * 2 + 0;
                    newTriangles[baseTriangleCount + offset * 3 + index * 3 + 0] = baseVertexCount + offset + index * 2 + 1;
                }
            }

            // TODO: Add support for NewTriangleWith3NewVertex

            newMesh.vertices = vertices;
            newMesh.uv = uv;
            newMesh.normals = normals;
            newMesh.tangents = tangents;
            newMesh.triangles = newTriangles;
            GetComponent<MeshFilter>().sharedMesh = newMesh;

            verticesNativeArray.Dispose();


            for (var planeIndex = 0; planeIndex < planeDatas.Length; planeIndex++)
            {
                classificationArrays[planeIndex].Dispose();
            }
        }
    }
}