using System.Collections;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshFilter)), CanEditMultipleObjects]
public class MeshFilterInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            int triangles = 0;
            int vertices = 0;
            int uvs = 0;
            int normals = 0;
            int tangents = 0;
            int colors = 0;

            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

            for (int i = 0; i < targets.Length; i++)
            {
                MeshFilter meshFilter = (targets[i] as MeshFilter);
                Mesh sharedMesh = meshFilter.sharedMesh;

                if (sharedMesh != null)
                {
                    triangles += sharedMesh.triangles.Length / 3;

                    vertices += sharedMesh.vertexCount;
                    uvs += sharedMesh.uv.Length;
                    normals += sharedMesh.normals.Length;
                    tangents += sharedMesh.tangents.Length;
                    colors += sharedMesh.colors.Length;
                    if (i == 0)
                    {
                        bounds = sharedMesh.bounds;
                    }
                    else
                    {
                        bounds.Encapsulate(sharedMesh.bounds);
                    }
                }
            }
            GUILayout.Label("Triangles: " + triangles);
            GUILayout.Label("Vertices: " + vertices);
            GUILayout.Label("UV: " + uvs);
            GUILayout.Label("Normals: " + normals);
            GUILayout.Label("Tangents: " + tangents);
            GUILayout.Label("Colors: " + colors);
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.wordWrap = true;
            GUILayout.Label("Bounds: " + bounds, style);
        }
    }
}