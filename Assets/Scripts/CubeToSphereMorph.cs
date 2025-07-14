using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class CubeToSphereMorph : MonoBehaviour
{
    public MeshFilter targetMeshFilter; // assegna la sfera qui
    private Mesh morphMesh;
    private Vector3[] startVertices;
    private Vector3[] endVertices;
    private Vector3[] morphedVertices;

    [Range(0f, 1f)]
    public float morphAmount = 0f;

    void Start()
    {
        morphMesh = GetComponent<MeshFilter>().mesh;
        startVertices = morphMesh.vertices;
        endVertices = targetMeshFilter.sharedMesh.vertices;
        morphedVertices = new Vector3[startVertices.Length];

        if (startVertices.Length != endVertices.Length)
        {
            Debug.LogError("Le mesh non hanno lo stesso numero di vertici!");
        }
    }

    void Update()
    {
        for (int i = 0; i < startVertices.Length; i++)
        {
            morphedVertices[i] = Vector3.Lerp(startVertices[i], endVertices[i], morphAmount);
        }

        morphMesh.vertices = morphedVertices;
        morphMesh.RecalculateNormals();
    }
}
