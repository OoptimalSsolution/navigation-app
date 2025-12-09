using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.BuiltinInterfaces;
using System;

public class ImuPublisher : MonoBehaviour
{
    private ROSConnection ros;
    public string imuTopic = "/imu_data";

    private ImuMsg imuMsg;

    void Start()
    {
        Input.gyro.enabled = true;
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ImuMsg>(imuTopic);

        imuMsg = new ImuMsg();
        imuMsg.header.frame_id = "imu_link";
    }

    void Update()
    {
        // 프레임 단위로 카메라 시점에 맞춰 호출
        PublishFrameSyncedImu();
    }

    void PublishFrameSyncedImu()
    {
        double now = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        imuMsg.header.stamp = new TimeMsg
        {
            sec = (int)Math.Floor(now),
            nanosec = (uint)((now - Math.Floor(now)) * 1e9)
        };

        Vector3 gyro = Input.gyro.rotationRateUnbiased;
        Vector3 accel = Input.gyro.userAcceleration;
        Quaternion q = Input.gyro.attitude;

        imuMsg.angular_velocity.x = gyro.x;
        imuMsg.angular_velocity.y = gyro.y;
        imuMsg.angular_velocity.z = gyro.z;

        imuMsg.linear_acceleration.x = accel.x;
        imuMsg.linear_acceleration.y = accel.y;
        imuMsg.linear_acceleration.z = accel.z;

        imuMsg.orientation.x = q.x;
        imuMsg.orientation.y = q.y;
        imuMsg.orientation.z = q.z;
        imuMsg.orientation.w = q.w;

        if (ros != null)
            ros.Publish(imuTopic, imuMsg);
    }
}
