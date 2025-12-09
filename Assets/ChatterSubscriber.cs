using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class ChatterSubscriber : MonoBehaviour
{
    ROSConnection ros;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<StringMsg>("/chatter", ChatterCallback);
    }

    void ChatterCallback(StringMsg msg)
    {
        Debug.Log($"ROS2 says: {msg.data}");
    }
}
