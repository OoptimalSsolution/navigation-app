using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.ARFoundation;
using System.Linq;

public class NewIndoorNav2 : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private ARTrackedImageManager m_TrakedImageManager;
    [SerializeField] private GameObject trakedImagePrefab;
    [SerializeField] private LineRenderer line;

    private List<NavigationTarget> navigationTargets = new List<NavigationTarget>();
    private NavMeshSurface navMeshSurface;
    private NavMeshPath navMeshPath;

    private GameObject navigationBase;

    private void Start()
    {
        navMeshPath = new NavMeshPath();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // 초기 위치 강제 설정 (AR 트래킹과 관계없이)
        SetFixedNavigationBase();
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
    }

    private void OnEnable() => m_TrakedImageManager.trackablesChanged.AddListener(OnChanged);

    private void OnDisable() => m_TrakedImageManager.trackablesChanged.RemoveListener(OnChanged);

    private void SetFixedNavigationBase()
    {
        if (navigationBase == null)
        {
            navigationBase = Instantiate(trakedImagePrefab);
        }

        navigationTargets.Clear();
        navigationTargets = navigationBase.transform.GetComponentsInChildren<NavigationTarget>().ToList();
        navMeshSurface = navigationBase.transform.GetComponentInChildren<NavMeshSurface>();

        // **초기 위치를 강제로 (10, 0, -5)로 설정**
        Vector3 fixedPosition = new Vector3(10, 0, -5);
        Quaternion fixedRotation = Quaternion.Euler(0, 90, 0); // 90도 회전 설정 (원하는 방향으로 조정 가능)

        navigationBase.transform.SetPositionAndRotation(fixedPosition, fixedRotation);
    }

    private void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (var newImage in eventArgs.added)
        {
            SetFixedNavigationBase();
        }

        foreach (var updatedImage in eventArgs.updated)
        {
            SetFixedNavigationBase();
        }

        foreach (var removedImage in eventArgs.removed)
        {
            // 마커가 제거되었을 때 필요한 로직 추가 가능
        }
    }
}