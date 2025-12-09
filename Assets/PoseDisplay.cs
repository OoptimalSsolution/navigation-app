using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;  //추가해야 함

public class PoseDisplay : MonoBehaviour
{
    [SerializeField] private XROrigin xrOrigin;  // 변경됨
    [SerializeField] private TMPro.TextMeshProUGUI poseText;

    void Update()
    {
        var camTransform = xrOrigin.Camera.transform;  // 변경됨

        Vector3 pos = camTransform.position;
        Quaternion rot = camTransform.rotation;

        poseText.text = $"<b>Pose</b>\n" +
                        $"Position: ({pos.x:F3}, {pos.y:F3}, {pos.z:F3})\n" +
                        $"Rotation: ({rot.eulerAngles.x:F1}, {rot.eulerAngles.y:F1}, {rot.eulerAngles.z:F1})";
    }
}
