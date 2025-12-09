using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.BuiltinInterfaces;
using System;
using System.Collections;

/// <summary>
/// 카메라 프레임을 JPEG로 압축하여 ROS2로 전송하는 스크립트
/// - 전송 주기: 10Hz
/// - 해상도: 640x480
/// - 품질: 70 (JPEG)
/// - 메시지 타입: sensor_msgs/CompressedImage
/// - 옵션: useRosTimestamp = true일 경우 ROS 호환 시간(sec/nanosec) 전송
/// </summary>
public class CameraCompressedPublisher : MonoBehaviour
{
    private ROSConnection ros;
    public Camera arCamera;
    public string cameraTopic = "/camera/compressed";

    [Header("Transmission Settings")]
    public int imageWidth = 640;
    public int imageHeight = 480;
    public int jpegQuality = 70;
    public float publishRate = 10f; // Hz
    public bool useRosTimestamp = false; // false: 단순 모드, true: ROS 호환 시간

    private Texture2D texture;
    private WaitForSeconds wait;

    void Start()
    {
        // ROS 연결 및 퍼블리셔 등록
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<CompressedImageMsg>(cameraTopic);

        // 전송 주기 설정 (10Hz)
        wait = new WaitForSeconds(1f / publishRate);

        // 렌더용 텍스처 생성
        texture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);

        // 코루틴 시작
        StartCoroutine(PublishCameraFrames());
    }

    IEnumerator PublishCameraFrames()
    {
        while (true)
        {
            yield return wait;
            SendCompressedFrame();
        }
    }

    void SendCompressedFrame()
    {
        // RenderTexture로 카메라 프레임 캡처
        RenderTexture renderTexture = new RenderTexture(imageWidth, imageHeight, 24);
        arCamera.targetTexture = renderTexture;
        arCamera.Render();

        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        texture.Apply();

        arCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);

        // JPEG 압축
        byte[] jpgBytes = texture.EncodeToJPG(jpegQuality);

        // 타임스탬프 생성
        TimeMsg timeMsg = new TimeMsg();
        if (useRosTimestamp)
        {
            double now = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            timeMsg.sec = (int)Math.Floor(now);
            timeMsg.nanosec = (uint)((now - Math.Floor(now)) * 1e9);
        }

        // CompressedImage 메시지 구성
        CompressedImageMsg msg = new CompressedImageMsg
        {
            header = new RosMessageTypes.Std.HeaderMsg
            {
                stamp = timeMsg,
                frame_id = "camera"
            },
            format = "jpeg",
            data = jpgBytes
        };

        // ROS 퍼블리시
        ros.Publish(cameraTopic, msg);
    }
}
