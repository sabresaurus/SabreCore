using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshFilterTest : MonoBehaviour
{
    [SerializeField] private Vector3 pointOnPlane;
    [SerializeField] private Quaternion planeOrientation = Quaternion.identity;

    [SerializeField] private bool showDebug = true;

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

    Plane CalculatePlane()
    {
        return new Plane(planeOrientation * Vector3.forward, pointOnPlane);
    }


    [SerializeField] private Mesh sourceMesh;

    private void Reset()
    {
        sourceMesh = GetComponent<MeshFilter>().sharedMesh;
    }

    private void OnDrawGizmos()
    {
        SliceMesh(sourceMesh);
    }

    void SliceMesh(Mesh sourceMesh)
    {
        //sourceMesh.isReadable
        var plane = CalculatePlane();

        Mesh newMesh = Instantiate(sourceMesh);
        Gizmos.matrix = transform.localToWorldMatrix;

        var vertices = sourceMesh.vertices;
//        foreach (Vector3 vertex in vertices)
//        {
//            Gizmos.color = (plane.GetSide(vertex)) ? Color.blue : Color.green;
//            Gizmos.DrawSphere(vertex, 0.01f);
//        }

        int[] triangles = sourceMesh.triangles;
        int[] newTriangles = newMesh.triangles;

        for (int i = 0; i < triangles.Length / 3; i++)
        {
            var point1 = vertices[triangles[i * 3 + 0]];
            var point2 = vertices[triangles[i * 3 + 1]];
            var point3 = vertices[triangles[i * 3 + 2]];
            var classification = Classify(point1, point2, point3, plane);
            if (classification == Classification.Front)
                Gizmos.color = Color.red;
            else if (classification == Classification.Back)
                Gizmos.color = Color.blue;
            else
                Gizmos.color = Color.green;

            Gizmos.DrawLine(point1, point2);
            Gizmos.DrawLine(point2, point3);
            Gizmos.DrawLine(point3, point1);

            if (classification == Classification.Back)
            {
                newTriangles[i * 3 + 0] = 0;
                newTriangles[i * 3 + 1] = 0;
                newTriangles[i * 3 + 2] = 0;
            }
        }

        newMesh.triangles = newTriangles;
        GetComponent<MeshFilter>().sharedMesh = newMesh;
    }

    static Classification Classify(Vector3 point1, Vector3 point2, Vector3 point3, Plane plane)
    {
        int numberInFront = 0;
        int numberBehind = 0;

        if (plane.GetSide(point1) == true)
            numberInFront++;
        else
            numberBehind++;

        if (plane.GetSide(point2) == true)
            numberInFront++;
        else
            numberBehind++;

        if (plane.GetSide(point3) == true)
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