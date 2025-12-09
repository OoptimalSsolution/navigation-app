using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PointCloudVisualizer : MonoBehaviour
{
    public ARPointCloudManager pointCloudManager;
    private Mesh mesh;

    void Start()
    {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // for large point sets
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void Update()
    {
        if (pointCloudManager == null) return;

        List<Vector3> points = new List<Vector3>();

        foreach (ARPointCloud cloud in pointCloudManager.trackables)
        {
            if (cloud.positions.HasValue)
            {
                foreach (Vector3 pos in cloud.positions.Value)
                {
                    points.Add(pos);
                }
            }
        }

        if (points.Count == 0)
        {
            mesh.Clear();
            return;
        }

        int[] indices = new int[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            indices[i] = i;
        }

        mesh.Clear();
        mesh.SetVertices(points);
        mesh.SetIndices(indices, MeshTopology.Points, 0);
    }
}
