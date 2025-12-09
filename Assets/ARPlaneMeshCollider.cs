using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARPlane), typeof(MeshFilter), typeof(MeshCollider))]
public class ARPlaneMeshCollider : MonoBehaviour
{
    private ARPlane plane;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    void Awake()
    {
        plane = GetComponent<ARPlane>();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        plane.boundaryChanged += OnBoundaryChanged;
        UpdateCollider();
    }

    void OnDestroy() => plane.boundaryChanged -= OnBoundaryChanged;

    void OnBoundaryChanged(ARPlaneBoundaryChangedEventArgs _) => UpdateCollider();

    void UpdateCollider()
    {
        if (!meshFilter || !meshCollider) return;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = meshFilter.mesh;
    }
}
