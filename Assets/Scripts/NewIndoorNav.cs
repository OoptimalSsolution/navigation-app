using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using Unity.XR.CoreUtils;


public class NewIndoorNav : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private GameObject trackedImagePrefab;
    [SerializeField] private LineRenderer line;
    [SerializeField] private Camera arCamera;
    
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown targetDropdown;

    [Header("Line Hover Settings")]
    [SerializeField] private float hoverHeight = 0.0f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float raycastUp = 1.0f;
    [SerializeField] private int segmentSubdiv = 0;

    [Header("AR Foundation")]
    [SerializeField] private ARSession arSession;                  // ARSession 오브젝트
    [SerializeField] private ARCameraBackground arCameraBackground; // AR Camera에 붙은 컴포넌트

    [Header("UI Panels")]
    [SerializeField] private GameObject arPanel;     // Canvas/ARPanel
    [SerializeField] private Button endNavButton;    // (선택) ARPanel의 종료 버튼
    [SerializeField] private GameObject pathPanel;     // 네비게이션 경로 Panel (필요 시)
    [SerializeField] private GameObject popupPanel;  // Canvas/PopupPanel
    [SerializeField] private GameObject qrScanPanel;      // Canvas/QRScanPanel
    [SerializeField] private GameObject qrPopupPanel;     // Canvas/QRPopupPanel

    [Header("Path Panel")]
    [SerializeField] private Button startNav;    // (선택) ARPanel의 종료 버튼



    //[SerializeField] private Button popupConfirmButton; // PopupPanel/ConfirmButton

    //[Header("Debug Info UI")]
    //[SerializeField] private TMP_Text qrText;
    //[SerializeField] private TMP_Text vioText;
    //[SerializeField] private TMP_Text prefabText;

    [Header("QR Scan Panels")]
    [SerializeField] private TMP_Text qrPopupMessage;     // QRPopupPanel/MessageText
    [SerializeField] private Button qrPopupConfirmButton; // QRPopupPanel/ConfirmButton
    [SerializeField] private Button qrScanStartButton;    // MainPanel/QRScanButton

    [Header("Home Panel")]
    [SerializeField] private GameObject homePanel;   // Canvas/HomePanel
    [SerializeField] private Button homeScanButton;  // HomePanel/ScanButton(Image)



    private List<NavigationTarget> navigationTargets = new();
    private NavMeshSurface navMeshSurface;
    private NavMeshPath navMeshPath;
    private GameObject navigationBase;
    private IBarcodeReader barcodeReader;
    private Texture2D cameraTexture;
    private bool pathActive = false;
    private bool vioActive = true;

    private int frameCounter = 0;
    private bool qrRecognized = false; //qr 성공 여부
    private Vector3 lastQRPosition = Vector3.zero;
    private Vector3 qrStartPosition = Vector3.zero;
    private bool hasQRStart = false;
    private bool isScanningQR = false;  // 현재 스캔 중인지


    private readonly Dictionary<string, Vector3> roomPositions = new()
    {
        { "entrance", new Vector3(95.06f, 0f, -10f) },
        { "S06-601", new Vector3(80.5f, 0f, -5.3f) },
        { "S06-602", new Vector3(78.2f, 0f, -2.1f) },
        { "S06-603", new Vector3(43.77f, 0f, -10f) },
        { "S06-604", new Vector3(71.7f, 0f, 4.4f) },
        { "S06-605", new Vector3(-2.52f, 0f, -13.79f) },
        { "S06-606", new Vector3(80.5f, 0f, -5.3f) },
        { "S06-607", new Vector3(80.5f, 0f, -5.3f) },
        { "S06-608", new Vector3(78.2f, 0f, -2.1f) },
        { "S06-609", new Vector3(80.5f, 0f, -5.3f) },
        { "S06-610", new Vector3(78.2f, 0f, -2.1f) },
        { "S06-611", new Vector3(74.9f, 0f, 1.0f) },
        { "S06-612", new Vector3(71.7f, 0f, 4.4f) },
        { "S06-613", new Vector3(68.2f, 0f, 7.5f) },
        { "S06-614", new Vector3(80.5f, 0f, -5.3f) },
        { "S06-616", new Vector3(9.936f, 0f, 11.45f) },
        { "S06-617", new Vector3(80.5f, 0f, -5.3f) },
        { "S06-618", new Vector3(74.9f, 0f, 1.0f) },
        { "S06-619", new Vector3(71.7f, 0f, 4.4f) },
        { "S06-620", new Vector3(68.2f, 0f, 7.5f) },
        { "S06-621", new Vector3(9.936f, 0f, 11.45f) },
        { "S06-622", new Vector3(9.936f, 0f, 11.45f) },
        { "S06-623", new Vector3(80.5f, 0f, -5.3f) },
        { "S06-624", new Vector3(74.9f, 0f, 1.0f) },
        { "S06-625", new Vector3(71.7f, 0f, 4.4f) },
        { "S06-626", new Vector3(68.2f, 0f, 7.5f) },
        { "S06-627", new Vector3(9.936f, 0f, 11.45f) }
    };

    private void Start()
    {
        if (homePanel) homePanel.SetActive(true);
        if (qrScanPanel) qrScanPanel.SetActive(false);
        if (popupPanel) popupPanel.SetActive(false);
        if (pathPanel) pathPanel.SetActive(false);
        if (arPanel) arPanel.SetActive(false); //
        if (qrPopupPanel) qrPopupPanel.SetActive(false);


        navMeshPath = new NavMeshPath();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        barcodeReader = new BarcodeReader { AutoRotate = true };

        cameraTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);

        // NEW: LineRenderer 기본 설정 권장
        if (line != null)
        {
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;   // 또는 필요 없으면 이 줄 삭제
            line.numCornerVertices = 2;
            line.numCapVertices = 2;

            // ---- AR LineRenderer 보이도록 강제 렌더 설정 ----
            line.sortingOrder = 999;
            line.sortingLayerName = "Default";   // 또는 다른 최상위 레이어

            line.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            line.startWidth = 0.1f;
            line.endWidth = 0.1f;
            line.startColor = new Color(0.435f, 0.576f, 1f);
            line.endColor = new Color(0.435f, 0.576f, 1f);

        }





        if (targetDropdown != null)
        {
            targetDropdown.ClearOptions();
            targetDropdown.AddOptions(roomPositions.Keys.ToList());
        }

      

        if (endNavButton != null) 
            endNavButton.onClick.AddListener(OnEndNavigation);

        if (popupPanel != null)
            popupPanel.SetActive(false);  // 시작 시 팝업 숨김

        //패널 초기 상태
        ShowPathPanel();

        //QR 이벤트 연결
        if (qrScanStartButton != null)
            qrScanStartButton.onClick.AddListener(OnClickQRScanButton);

        if (homeScanButton != null)
            homeScanButton.onClick.AddListener(OnClickHomeScanButton);

        if (startNav != null)
        {
            startNav.onClick.AddListener(OnClickStartNavButton);
        }



    }

    private void OnClickStartNavButton()
    {
        ShowHomePanel();
        Debug.Log("StartNav button clicked -> HomePanel shown");
    }


    private void OnClickQRScanButton()
    {
        Debug.Log("QR 스캔 시작 버튼 클릭됨");

        qrRecognized = false;
        isScanningQR = true;            // 스캔 시작

        // 1 패널 전환\
        if (qrScanPanel) qrScanPanel.SetActive(true);
        if (qrPopupPanel) qrPopupPanel.SetActive(false);

        // 2 스캔 코루틴 실행
        StartCoroutine(ScanQRCodeCoroutine());
    }
    private void StopAllScanCoroutines()
    {
        StopCoroutine(ScanQRCodeCoroutine());
        StopCoroutine(QRScanTimeoutCoroutine());

        // 혹시 남아 있을 수 있는 모든 코루틴 정리
        StopAllCoroutines();

        Debug.Log("모든 QR 스캔 코루틴 종료됨");
    }

    private IEnumerator AutoMoveToPathPanel()
    {
        yield return new WaitForSeconds(2f); // 팝업 보여주는 시간

        ShowPathPanel();
    }

    /*
    private void ShowQRPopup(string message)
    {
        if (qrScanPanel) qrScanPanel.SetActive(true);
        if (qrPopupPanel) qrPopupPanel.SetActive(true);

        if (qrPopupMessage != null)
            qrPopupMessage.text = message;
    }
    */


    private void ShowARPanel()
    {
        if (homePanel) homePanel.SetActive(false);
        if (qrScanPanel) qrScanPanel.SetActive(false);
        if (popupPanel) popupPanel.SetActive(false);
        if (pathPanel) pathPanel.SetActive(false);
        if (arPanel) arPanel.SetActive(true); //
        if (qrPopupPanel) qrPopupPanel.SetActive(false);

        // 카메라 배경/세션 활성화
        if (arSession) arSession.enabled = true;
        if (arCameraBackground) arCameraBackground.enabled = true;

    }


    private void Update()
    {
        
        if (navMeshPath != null && navigationTargets.Count > 0 && navMeshSurface != null)
        {
            // (선택) 시작/도착이 NavMesh 위에 확실히 스냅되도록
            Vector3 start = xrOrigin.transform.position;
            Vector3 end = navigationTargets[0].transform.position;

            if (NavMesh.SamplePosition(start, out var hitStart, 1.0f, NavMesh.AllAreas))
                start = hitStart.position;
            if (NavMesh.SamplePosition(end, out var hitEnd, 1.0f, NavMesh.AllAreas))
                end = hitEnd.position;

            // XR Origin에서 첫 번째 NavigationTarget까지의 경로 계산
            NavMesh.CalculatePath(start, end, NavMesh.AllAreas, navMeshPath);

            if (navMeshPath.status == NavMeshPathStatus.PathComplete)
            {
                // NEW: 경로 코너를 바닥에 투영 + 살짝 띄우기(필요 시 세그먼트 보간)
                var elevated = BuildElevatedLineFromPath(navMeshPath, hoverHeight, groundLayer, raycastUp, segmentSubdiv);

                line.positionCount = elevated.Count;
                line.SetPositions(elevated.ToArray());
            }
            else
            {
                line.positionCount = 0;
            }
        }
        

        
        // 일정 주기로 QR 코드 스캔 실행
        if (frameCounter % 50 == 0)
        {
            StartCoroutine(ScanQRCodeCoroutine());
        }

        frameCounter++;

        

        if (pathActive && navMeshPath != null && navMeshPath.corners != null && navMeshPath.corners.Length > 1)   
        {
            Vector3 playerPos = xrOrigin.Camera.transform.position;
            Vector3 dest = navMeshPath.corners.Last();

            if (Vector3.Distance(playerPos, dest) < 1.0f)
            {
                pathActive = false;
                ShowPopupPanel();
            }
        }
    }

    // 라인 생성
    private List<Vector3> BuildElevatedLineFromPath(NavMeshPath path, float hover, LayerMask mask, float upOffset, int subdiv)
    {
        var corners = path.corners;
        var samples = new List<Vector3>();
        if (corners == null || corners.Length == 0) return samples;

        if (subdiv <= 0)
        {
            foreach (var c in corners)
                samples.Add(ElevatePoint(c, hover, mask, upOffset));
        }
        else
        {
            for (int i = 0; i < corners.Length - 1; i++)
            {
                for (int s = 0; s <= subdiv; s++)
                {
                    float t = s / (float)subdiv;
                    Vector3 p = Vector3.Lerp(corners[i], corners[i + 1], t);
                    samples.Add(ElevatePoint(p, hover, mask, upOffset));
                }
            }
        }
        return samples;
    }

    private Vector3 ElevatePoint(Vector3 p, float hover, LayerMask mask, float upOffset)
    {
        Vector3 origin = p + Vector3.up * upOffset;
        if (Physics.Raycast(origin, Vector3.down, out var hit, upOffset * 2f, mask))
            return hit.point + hit.normal * hover;
        return p + Vector3.up * hover;
    }


    // NEW: 위에서 아래로 레이캐스트하여 표면 점/법선 얻고, 법선 방향으로 hover 만큼 올림

    private IEnumerator ScanQRCodeCoroutine()
    {
        // 렌더링이 끝날 때까지 대기
        yield return new WaitForEndOfFrame();

        if (arCamera == null)
        {
            Debug.Log($"AR 카메라가 없습니다.");
            yield break;
        }

        //RenderTexture -> Texture2D 복사 (현재 RenderTexture를 백업 : 나중에 복구하려구)
        RenderTexture activeRenderTexture = RenderTexture.active;
        RenderTexture.active = arCamera.targetTexture;

        cameraTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        cameraTexture.Apply();

        //GPU가 읽을 화면을 "AR 카메라가 찍은 실제 화면"으로 설정
        //GPU가 읽는 화면 = 카메라가 렌더링한 최종 이미지가 담겨 있는 버퍼(RenderTexture)
        RenderTexture.active = activeRenderTexture;

        try
        {
            Color32[] pixels = cameraTexture.GetPixels32();
            int width = cameraTexture.width;
            int height = cameraTexture.height;

            Result result = barcodeReader.Decode(pixels, width, height);

            if (result != null)
            {
                qrRecognized = true;
                isScanningQR = false;

                //StopAllScanCoroutines();

                Debug.Log($"QR 코드 인식: {result.Text}");
                //ShowQRPopupPanel();
                StartCoroutine(UpdateNavigationBasePosition(result.Text));

            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"QR 코드 스캔 오류: {ex.Message}");
        }

        // 인식 주기 조정 (예: 0.2초)
        yield return new WaitForSeconds(0.2f);
    }
    private IEnumerator QRProcessFlow(string qr)
    {

        //QR → NavMesh → Path → Line 생성 완료될 때까지 대기
        yield return StartCoroutine(UpdateNavigationBasePosition(qr));

        //팝업 표시 (여기서도 라인은 이미 존재)
        ShowQRPopupPanel();

        //1초 뒤 ARPanel 이동
        yield return new WaitForSeconds(1f);
        ShowARPanel();


    }

    /*
    private IEnumerator UpdateNavigationBasePosition(string data)
    {
        // QR 코드 데이터 형식: x, y, z, x축 회전, y축 회전, z축 회전
        string[] dataValues = data.Split(',');
        if (dataValues.Length == 6 &&
            float.TryParse(dataValues[0], out float x) &&
            float.TryParse(dataValues[1], out float y) &&
            float.TryParse(dataValues[2], out float z) &&
            float.TryParse(dataValues[3], out float rotX) &&
            float.TryParse(dataValues[4], out float rotY) &&
            float.TryParse(dataValues[5], out float rotZ))
        {
            Vector3 qrPosition = new Vector3(x, y, z);
            Quaternion qrRotation = Quaternion.Euler(rotX, rotY, rotZ);
            Vector3 init = Vector3.zero;

            if (navigationBase != null)
            {
                Destroy(navigationBase);
            }
            navigationBase = Instantiate(trackedImagePrefab);
            navigationBase.transform.SetPositionAndRotation(init, Quaternion.identity);

            yield return null; // navigationBase가 완전히 로드될 때까지 한 프레임 대기

            navigationTargets.Clear();
            navigationTargets = navigationBase.transform.GetComponentsInChildren<NavigationTarget>().ToList();
            navMeshSurface = navigationBase.transform.GetComponentInChildren<NavMeshSurface>();

            //NavMesh 생성 (이 한 줄이 없으면 CalculatePath 실패)
            if (navMeshSurface != null)
            {
                navMeshSurface.BuildNavMesh();
                Debug.Log("NavMesh rebuilt");
            }
            else
            {
                Debug.LogError("NavMeshSurface not found in navigationBase!");
            }

            // targetDropDown 기반으로 목적지를 결정
            string targetKey = targetDropdown.options[targetDropdown.value].text;

            if (!roomPositions.ContainsKey(targetKey))
            {
                Debug.LogWarning("타겟 좌표가 roomPositions에 없습니다.");
                yield break;
            }

            Vector3 end = roomPositions[targetKey];
            if (NavMesh.SamplePosition(end, out var hitEnd, 1f, NavMesh.AllAreas))
                end = hitEnd.position;
            Vector3 start = xrOrigin.transform.position;
            if (NavMesh.SamplePosition(start, out var hitStart, 1f, NavMesh.AllAreas))
                start = hitStart.position;

            float maxDistance = 2f;

            NavMeshHit debugHitStart, debugHitEnd;
            bool startOnNav = NavMesh.SamplePosition(start, out debugHitStart, maxDistance, NavMesh.AllAreas);
            bool endOnNav = NavMesh.SamplePosition(end, out debugHitEnd, maxDistance, NavMesh.AllAreas);

            Debug.Log($"[DEBUG] Start raw: {start} | Sampled: {(startOnNav ? debugHitStart.position : "FAILED")}");
            Debug.Log($"[DEBUG] End raw: {end} | Sampled: {(endOnNav ? debugHitEnd.position : "FAILED")}");

            if (!startOnNav) Debug.LogError("[ERROR] Start position is NOT on NavMesh");
            if (!endOnNav) Debug.LogError("[ERROR] End position is NOT on NavMesh");
 
            // ---- NavMesh Debug End ----

            Debug.Log($"QR 코드 기반으로 NavigationBase 위치 재설정: {qrPosition}");

            if (navigationTargets.Count > 0)
            {
                //NavMesh.CalculatePath(xrOrigin.transform.position, navigationTargets[0].transform.position, NavMesh.AllAreas, navMeshPath);
                NavMesh.CalculatePath(start, end, NavMesh.AllAreas, navMeshPath);
                if (navMeshPath.status == NavMeshPathStatus.PathComplete)
                {
                    Debug.Log("네비게이션 경로 업데이트 완료");
                    // InitialPose 컴포넌트를 가진 오브젝트 찾기
                    InitialPose initialPose = navigationBase.GetComponentInChildren<InitialPose>();


                    if (initialPose != null)
                    {
                        // InitialPose의 위치를 사용하되, 회전은 QR 코드의 회전 값을 적용합니다.
                        MoveXROrigin(initialPose.transform.position, qrRotation);
                        Debug.Log($"InitialPose 기준으로 XROrigin 이동: {initialPose.transform.position}, 회전: {qrRotation.eulerAngles}");
                    }
                    else
                    {
                        MoveXROrigin(qrPosition, qrRotation);
                        Debug.Log($"InitialPose 없음. QR 코드 기준으로 XROrigin 이동: {qrPosition}, 회전: {qrRotation.eulerAngles}");
                    }
                    var elevated = BuildElevatedLineFromPath(
                            navMeshPath,
                            hoverHeight,
                            groundLayer,
                            raycastUp,
                            segmentSubdiv);

                    line.positionCount = elevated.Count;
                    line.SetPositions(elevated.ToArray());
                    pathActive = true;


                    Debug.Log($" Path generated - Total segments: {elevated.Count}");
                    Debug.Log($" NavMesh corner count: {navMeshPath.corners.Length}");

                    float pathLength = 0f;
                    for (int i = 0; i < elevated.Count - 1; i++)
                        pathLength += Vector3.Distance(elevated[i], elevated[i + 1]);
                    Debug.Log($" Path length: {pathLength:F2} m");

                    // Print each point coordinate
                    for (int i = 0; i < elevated.Count; i++)
                        Debug.Log($" Point[{i}]: {elevated[i]}");

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
            Debug.LogError("QR 코드 데이터가 올바른 형식이 아닙니다. 예: 10,0,-6,0,180,0");
        }

        yield return null;

    }
    */
    private IEnumerator UpdateNavigationBasePosition(string data)
    {
        // QR 코드 데이터 형식: x, y, z, x축 회전, y축 회전, z축 회전
        string[] dataValues = data.Split(',');
        if (dataValues.Length == 6 &&
            float.TryParse(dataValues[0], out float x) &&
            float.TryParse(dataValues[1], out float y) &&
            float.TryParse(dataValues[2], out float z) &&
            float.TryParse(dataValues[3], out float rotX) &&
            float.TryParse(dataValues[4], out float rotY) &&
            float.TryParse(dataValues[5], out float rotZ))
        {
            Vector3 qrPosition = new Vector3(x, y, z);
            Quaternion qrRotation = Quaternion.Euler(rotX, rotY, rotZ);
            Vector3 init = Vector3.zero;

            if (navigationBase != null)
            {
                Destroy(navigationBase);
            }
            navigationBase = Instantiate(trackedImagePrefab);
            navigationBase.transform.SetPositionAndRotation(init, Quaternion.identity);

            yield return null; // navigationBase가 완전히 로드될 때까지 한 프레임 대기

            navigationTargets.Clear();
            navigationTargets = navigationBase.transform.GetComponentsInChildren<NavigationTarget>().ToList();
            navMeshSurface = navigationBase.transform.GetComponentInChildren<NavMeshSurface>();

            if (navMeshSurface != null)
            {
                navMeshSurface.BuildNavMesh();
                Debug.Log("[DEBUG] NavMesh built successfully");
            }
            else
            {
                Debug.LogError("[ERROR] NavMeshSurface missing in NavigationBase");
            }

            // InitialPose 컴포넌트를 가진 오브젝트 찾기
            InitialPose initialPose = navigationBase.GetComponentInChildren<InitialPose>();
            if (initialPose != null)
            {
                // InitialPose의 위치를 사용하되, 회전은 QR 코드의 회전 값을 적용합니다.
                MoveXROrigin(initialPose.transform.position, qrRotation);
                Debug.Log($"InitialPose 기준으로 XROrigin 이동: {initialPose.transform.position}, 회전: {qrRotation.eulerAngles}");
            }
            else
            {
                MoveXROrigin(qrPosition, qrRotation);
                Debug.Log($"InitialPose 없음. QR 코드 기준으로 XROrigin 이동: {qrPosition}, 회전: {qrRotation.eulerAngles}");
            }

            Debug.Log($"QR 코드 기반으로 NavigationBase 위치 재설정: {qrPosition}");

            if (navigationTargets.Count > 0)
            {
                // ---- NavMesh 상태 디버깅 ----
                float maxDistance = 2f;

                NavMeshHit hitStart, hitEnd;
                bool startOnNav = NavMesh.SamplePosition(xrOrigin.transform.position, out hitStart, maxDistance, NavMesh.AllAreas);
                bool endOnNav = NavMesh.SamplePosition(navigationTargets[0].transform.position, out hitEnd, maxDistance, NavMesh.AllAreas);

                Debug.Log($"[DEBUG] Start Raw: {xrOrigin.transform.position} | Sampled: {(startOnNav ? hitStart.position.ToString() : "FAILED")}");
                Debug.Log($"[DEBUG] End Raw: {navigationTargets[0].transform.position} | Sampled: {(endOnNav ? hitEnd.position.ToString() : "FAILED")}");

                if (!startOnNav) Debug.LogError("[ERROR] Start position is NOT on NavMesh");
                if (!endOnNav) Debug.LogError("[ERROR] End position is NOT on NavMesh");

                NavMesh.CalculatePath(xrOrigin.transform.position, navigationTargets[0].transform.position, NavMesh.AllAreas, navMeshPath);

                if (navMeshPath.status == NavMeshPathStatus.PathComplete)
                {
                    Debug.Log("네비게이션 경로 업데이트 완료");

                    var elevated = BuildElevatedLineFromPath(
                        navMeshPath,
                        hoverHeight,
                        groundLayer,
                        raycastUp,
                        segmentSubdiv);

                    line.positionCount = elevated.Count;
                    line.SetPositions(elevated.ToArray());
                    pathActive = true;

                    Debug.Log($"[LINE] point count = {line.positionCount}");

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
            Debug.LogError("QR 코드 데이터가 올바른 형식이 아닙니다. 예: 10,0,-6,0,180,0");
        }
        yield return null;
    }



    private void MoveXROrigin(Vector3 targetPosition, Quaternion targetRotation)
    {
        if (xrOrigin != null)
        {
            // 카메라의 현재 위치와 비교하여 오프셋을 계산하고, XR Origin의 위치와 회전을 업데이트
            Vector3 offset = targetPosition - xrOrigin.Camera.transform.position;
            xrOrigin.transform.position += offset;
            xrOrigin.transform.rotation = targetRotation;
            Debug.Log($"XROrigin 이동 완료: {xrOrigin.transform.position}, 회전: {xrOrigin.transform.rotation.eulerAngles}");
        }
        else
        {
            Debug.LogError("XROrigin이 설정되지 않음.");
        }
    }

    private void ShowPopup(string message)
    {
        if (popupPanel == null) return;

        // 텍스트 자동 변경
        var text = popupPanel.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.text = message;

        popupPanel.SetActive(true);
    }

    private void OnEndNavigation()
    {
        ShowPopup("목적지에 도착하였습니다");
    }

    private void OnClickHomeScanButton()
    {
        Debug.Log("Scan Button 클릭됨");
        qrRecognized = false;
        isScanningQR = true;
        // 패널 전환
        if (homePanel) homePanel.SetActive(false);
        if (qrScanPanel) qrScanPanel.SetActive(true);

        // 스캔 프로세스 바로 시작
        ShowQRScanPanel();       // 패널 전환 + AR 활성화
        StartCoroutine(ScanQRCodeCoroutine());
        StartCoroutine(QRScanTimeoutCoroutine());
        //if (arSession) arSession.enabled = true;
        //if (arCameraBackground) arCameraBackground.enabled = true;

    }
    private IEnumerator QRScanTimeoutCoroutine()
    {
        qrRecognized = false;
        isScanningQR = true;

        float timeout = 10f;
        float elapsed = 0f;

        //10초 동안 QR 스캔 성공을 기다림
        while (elapsed < timeout)
        {
            if (qrRecognized) yield break; // QR 성공 → 타임아웃 종료
            elapsed += Time.deltaTime;
            yield return null;
        }

        //여기까지 왔으면 QR 실패
        Debug.Log("QR scan timeout -> fallback to entrance");

        string fallbackKey = "entrance";
        Vector3 fallbackPos = roomPositions[fallbackKey];
        Debug.Log($"Fallback start location: {fallbackKey} ({fallbackPos})");

        // fallback 위치로 네비게이션 초기화
        StartCoroutine(UpdateNavigationBasePosition($"{fallbackPos.x},{fallbackPos.y},{fallbackPos.z},0,0,0"));

        // PopupPanel 표시 → 1초 후 ARPanel로 이동
        ShowQRPopupPanel();
        StartCoroutine(AutoToARPanel());
    }

    private IEnumerator AutoToARPanel()
    {

        yield return new WaitForSeconds(1f);  // QRPopupPanel 표시 시간
        ShowARPanel();                        // ARPanel 이동
        Debug.Log("Move to ARPanel (navigation start)");
    }

    private void ShowOnlyPanel(GameObject panelToShow, bool enableAR = false)
    {
        if (homePanel) homePanel.SetActive(false);
        if (qrScanPanel) qrScanPanel.SetActive(false);
        if (popupPanel) popupPanel.SetActive(false);
        if (pathPanel) pathPanel.SetActive(false);
        if (arPanel) arPanel.SetActive(false);
        if (qrPopupPanel) qrPopupPanel.SetActive(false);

        if (panelToShow) panelToShow.SetActive(true);

        // AR 카메라 활성화 여부
        //if (arCameraBackground) arCameraBackground.enabled = enableAR;
        //if (arSession) arSession.enabled = enableAR;
        if (enableAR)
             {
                if (arCameraBackground) arCameraBackground.enabled = true;
                if (arSession) arSession.enabled = true;
            }
        // enableAR이 false라도 ARSession을 끄지 않음

        // 라인 숨김
        //if (line) line.positionCount = 0;
    }

    private void ShowHomePanel()
    {
        ShowOnlyPanel(homePanel, false);
    }

    private void ShowQRScanPanel()
    {
        ShowOnlyPanel(qrScanPanel, true);
    }
    private void ShowQRPopupPanel()
    {
        ShowOnlyPanel(qrPopupPanel, true);
    }

    private void ShowPathPanel()
    {
        ShowOnlyPanel(pathPanel, false);
    }

    private void ShowPopupPanel()
    {
        ShowOnlyPanel(popupPanel, true);
    }


}
