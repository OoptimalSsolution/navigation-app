using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry; // PoseStamped 메시지 타입

public class PoseSubscriber : MonoBehaviour
{
    [Header("AR Camera or Target Object")]
    public Transform targetTransform; // Pose를 반영할 Transform (AR Camera 또는 3D 오브젝트)

    private ROSConnection ros;

    void Start()
    {
        // ROS 연결
        ros = ROSConnection.GetOrCreateInstance();
        // 토픽 구독: /pose_est
        ros.Subscribe<PoseStampedMsg>("/pose_est", PoseCallback);
    }

    void PoseCallback(PoseStampedMsg msg)
    {
        // 위치
        Vector3 pos = new Vector3(
            (float)msg.pose.position.x,
            (float)msg.pose.position.y,
            (float)msg.pose.position.z
        );

        // 방향 (Quaternion)
        Quaternion rot = new Quaternion(
            (float)msg.pose.orientation.x,
            (float)msg.pose.orientation.y,
            (float)msg.pose.orientation.z,
            (float)msg.pose.orientation.w
        );

        // Unity 좌표계 보정 (ROS 좌표계 → Unity 좌표계 변환)
        pos = new Vector3(pos.y, pos.z, -pos.x);
        rot = new Quaternion(rot.y, rot.z, -rot.x, rot.w);

        Debug.Log($"[ROS Pose] Pos: {pos.x:F3}, {pos.y:F3}, {pos.z:F3} | Rot: {rot.eulerAngles}");

        // Transform 업데이트 (메인 스레드에서)
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            targetTransform.SetPositionAndRotation(pos, rot);
        });
    }
}
