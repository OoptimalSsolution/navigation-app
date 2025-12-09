using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.ARFoundation;
using ZXing;
using UnityEngine.UI;
using System.Linq;

public class NewIndoorNavBackup : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private GameObject trakedImagePrefab;
    [SerializeField] private LineRenderer line;
    [SerializeField] private Camera arCamera;

    private List<NavigationTarget> navigationTargets = new List<NavigationTarget>();
    private NavMeshSurface navMeshSurface;
    private NavMeshPath navMeshPath;
    private GameObject navigationBase;
    private IBarcodeReader barcodeReader;
    private Texture2D cameraTexture;

    private int frameCounter = 0;

    private void Start()
    {
        navMeshPath = new NavMeshPath();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        barcodeReader = new BarcodeReader { AutoRotate = true };

        cameraTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
    }

    private void Update()
    {
        if (navMeshPath != null && navigationTargets.Count > 0 && navMeshSurface != null)
        {
            NavMesh.CalculatePath(player.position, navigationTargets[0].transform.position, NavMesh.AllAreas, navMeshPath);

            if (navMeshPath.status == NavMeshPathStatus.PathComplete)
            {
                line.positionCount = navMeshPath.corners.Length;
                line.SetPositions(navMeshPath.corners);
            }
            else
            {
                line.positionCount = 0;
            }
        }

        if (frameCounter % 15 == 0)
        {
            ScanQRCode();
        }

        frameCounter++;
    }

    private void ScanQRCode()
    {
        if (arCamera == null)
            return;

        RenderTexture activeRenderTexture = RenderTexture.active;
        RenderTexture.active = arCamera.targetTexture;

        cameraTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        cameraTexture.Apply();

        RenderTexture.active = activeRenderTexture;

        try
        {
            Color32[] pixels = cameraTexture.GetPixels32();
            int width = cameraTexture.width;
            int height = cameraTexture.height;

            Result result = barcodeReader.Decode(pixels, width, height);

            if (result != null)
            {
                Debug.Log($"QR 코드 인식: {result.Text}");
                UpdateNavigationBasePosition(result.Text);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"QR 코드 스캔 오류: {ex.Message}");
        }
    }

    private void UpdateNavigationBasePosition(string data)
    {
        string[] positionData = data.Split(',');

        if (positionData.Length == 3 &&
            float.TryParse(positionData[0], out float x) &&
            float.TryParse(positionData[1], out float y) &&
            float.TryParse(positionData[2], out float z))
        {
            Vector3 qrPosition = new Vector3(x, y, z);
            Vector3 init = Vector3.zero;

            if (navigationBase != null)
            {
                Destroy(navigationBase);
            }
            navigationBase = Instantiate(trakedImagePrefab);
            navigationBase.transform.SetPositionAndRotation(init, Quaternion.identity);

            navigationTargets.Clear();
            navigationTargets = navigationBase.transform.GetComponentsInChildren<NavigationTarget>().ToList();
            navMeshSurface = navigationBase.transform.GetComponentInChildren<NavMeshSurface>();

            // **InitialPose 컴포넌트를 가진 오브젝트 찾기**
            InitialPose initialPose = navigationBase.GetComponentInChildren<InitialPose>();
            if (initialPose != null)
            {
                player.transform.SetPositionAndRotation(initialPose.transform.position, Quaternion.identity);
                Debug.Log($"InitialPose 기준으로 Player 위치 설정: {player.position}");
            }
            else
            {
                player.transform.SetPositionAndRotation(qrPosition, Quaternion.identity);
                Debug.Log($"InitialPose 없음. QR 코드 위치 기준으로 Player 위치 설정: {qrPosition}");
            }

            Debug.Log($"QR 코드 기반으로 NavigationBase 위치 재설정: {qrPosition}");

            if (navigationTargets.Count > 0)
            {
                NavMesh.CalculatePath(player.position, navigationTargets[0].transform.position, NavMesh.AllAreas, navMeshPath);

                if (navMeshPath.status == NavMeshPathStatus.PathComplete)
                {
                    Debug.Log("네비게이션 경로 업데이트 완료");
                }
                else
                {
                    Debug.LogError("네비게이션 경로 설정 실패");
                }
            }
            else
            {
                Debug.LogError("Navigation Target이 설정되지 않음.");
            }
        }
        else
        {
            Debug.LogError("QR 코드 데이터가 올바른 좌표 형식이 아닙니다. 예: 10,0,-6");
        }
    }
}