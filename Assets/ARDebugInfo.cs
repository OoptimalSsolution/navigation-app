using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using System.Text;
using System.Collections.Generic;

public class ARDebugInfo : MonoBehaviour
{
    public ARCameraManager cameraManager;
    public ARPointCloudManager pointCloudManager;
    public ARRaycastManager raycastManager;
    public Camera arCamera;
    public TMP_Text debugText;

    void Update()
    {
        StringBuilder sb = new StringBuilder();

        // 1. Camera Intrinsics
        if (cameraManager != null && cameraManager.TryGetIntrinsics(out XRCameraIntrinsics intrinsics))
        {
            sb.AppendLine("Camera Intrinsics:");
            sb.AppendLine($"  Focal Length: fx={intrinsics.focalLength.x:F2}, fy={intrinsics.focalLength.y:F2}");
            sb.AppendLine($"  Principal Point: cx={intrinsics.principalPoint.x:F2}, cy={intrinsics.principalPoint.y:F2}");
        }
        else
        {
            sb.AppendLine("Intrinsics: not available");
        }

        // 2. PointCloud Á¤º¸
        if (pointCloudManager != null)
        {
            int totalPoints = 0;
            float totalDistance = 0f;

            foreach (ARPointCloud cloud in pointCloudManager.trackables)
            {
                var points = cloud.positions;
                if (points.HasValue)
                {
                    var slice = points.Value;
                    totalPoints += slice.Length;

                    foreach (var pt in slice)
                    {
                        float dist = Vector3.Distance(arCamera.transform.position, pt);
                        totalDistance += dist;
                    }
                }
            }


            if (totalPoints > 0)
            {
                float avgDistance = totalDistance / totalPoints;
                sb.AppendLine($"PointCloud: Detected {totalPoints} pts");
                sb.AppendLine($"Avg Distance: {avgDistance:F2} m");
            }
            else
            {
                sb.AppendLine("PointCloud: no points detected");
            }
        }
        else
        {
            sb.AppendLine("PointCloud Manager not assigned");
        }

        // 3. AR Raycast from screen center
        if (raycastManager != null)
        {
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            var hits = new List<ARRaycastHit>();
            if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint))
            {
                var hitPose = hits[0].pose;
                sb.AppendLine("Raycast hit:");
                sb.AppendLine($"  Pos: ({hitPose.position.x:F2}, {hitPose.position.y:F2}, {hitPose.position.z:F2})");
            }
            else
            {
                sb.AppendLine("Raycast: no hit");
            }
        }

        // 4. Update UI
        if (debugText != null)
            debugText.text = sb.ToString();
    }
}
