using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

[RequireComponent(typeof(ARCameraManager))]
public class PoseLogger : MonoBehaviour
{
    [Header("Logging Settings")]
    public float logInterval = 0.01f; // 100Hz
    public bool logIMU = true;
    public bool logPose = true;

    [Header("UI Buttons")]
    public Button startButton;
    public Button stopButton;

    [Header("Popup UI")]
    public GameObject popupPanel;      // 팝업 패널 (Canvas 안)
    public TMP_Text popupText;             // 팝업 안의 텍스트

    private ARCameraManager arCameraManager;
    private StreamWriter writer;
    private float timer = 0f;
    private string logPath;
    private bool isLogging = false;

    void Start()
    {
        arCameraManager = GetComponent<ARCameraManager>();
        Input.gyro.enabled = true;
        Input.compass.enabled = true;

        //버튼 이벤트 등록
        if (startButton != null)
            startButton.onClick.AddListener(StartLogging);
        if (stopButton != null)
            stopButton.onClick.AddListener(StopLogging);

        //팝업은 기본적으로 꺼두기
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    void Update()
    {
        if (!isLogging) return;

        timer += Time.deltaTime;
        if (timer >= logInterval)
        {
            timer = 0f;
            LogData();
        }
    }

    public void StartLogging()
    {
        if (isLogging) return;

        string basePath = Application.persistentDataPath;
        logPath = Path.Combine(basePath, "pose_log.csv");
        writer = new StreamWriter(logPath);
        writer.WriteLine("time,x,y,z,qx,qy,qz,qw,gyro_x,gyro_y,gyro_z,accel_x,accel_y,accel_z,mag_x,mag_y,mag_z");

        isLogging = true;
        Debug.Log($"[PoseLogger] Started logging to: {logPath}");
    }

    public void StopLogging()
    {
        if (!isLogging) return;

        isLogging = false;
        writer?.Flush();
        writer?.Close();
        Debug.Log("[PoseLogger] Stopped logging.");

        // 팝업 표시
        if (popupPanel != null && popupText != null)
        {
            popupText.text = $"데이터가 아래 경로에 저장되었습니다.\n\n{logPath}";
            popupPanel.SetActive(true);
            StartCoroutine(HidePopupAfterDelay(4f)); // 4초 뒤 자동 닫기
        }
    }

    IEnumerator HidePopupAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    void LogData()
    {
        double timestamp = Time.realtimeSinceStartupAsDouble;

        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;
        Vector3 gyro = Input.gyro.rotationRateUnbiased;
        Vector3 accel = Input.acceleration;
        Vector3 mag = Input.compass.rawVector;

        string line = $"{timestamp:F4}," +
                      $"{pos.x:F4},{pos.y:F4},{pos.z:F4}," +
                      $"{rot.x:F4},{rot.y:F4},{rot.z:F4},{rot.w:F4}," +
                      $"{gyro.x:F4},{gyro.y:F4},{gyro.z:F4}," +
                      $"{accel.x:F4},{accel.y:F4},{accel.z:F4}," +
                      $"{mag.x:F4},{mag.y:F4},{mag.z:F4}";
        writer.WriteLine(line);
    }

    void OnApplicationQuit()
    {
        StopLogging();
    }
}
