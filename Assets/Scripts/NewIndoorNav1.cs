using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.ARFoundation;
using System.Linq;

public class NewIndoorNavbackup: MonoBehaviour
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
    }

    private void Update()
    {
        if(navMeshPath != null&&navigationTargets.Count >0 && navMeshSurface !=null)
        {
            //navMeshSurface.BuildNavMesh();
            NavMesh.CalculatePath(player.position, navigationTargets[0].transform.position, NavMesh.AllAreas, navMeshPath);

            if(navMeshPath.status == NavMeshPathStatus.PathComplete)
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

    /*
        private void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
        {
            foreach(var newImage in eventArgs.added)
            {
                navigationBase = GameObject.Instantiate(trakedImagePrefab);
                navigationTargets.Clear();
                navigationTargets = navigationBase.transform.GetComponentsInChildren<NavigationTarget>().ToList();
                navMeshSurface = navigationBase.transform.GetComponentInChildren<NavMeshSurface>();
            } 
            foreach(var updatedImage in eventArgs.updated)
            {
                navigationBase.transform.SetPositionAndRotation(updatedImage.pose.position, Quaternion.Euler(0, updatedImage.pose.rotation.eulerAngles.y, 0));
            }

            foreach(var removedImage in eventArgs.removed)
            {

            }
        }

        */

    /*
    private void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (var newImage in eventArgs.added)
        {
            navigationBase = GameObject.Instantiate(trakedImagePrefab);
            navigationTargets.Clear();
            navigationTargets = navigationBase.transform.GetComponentsInChildren<NavigationTarget>().ToList();
            navMeshSurface = navigationBase.transform.GetComponentInChildren<NavMeshSurface>();
        }

        foreach (var updatedImage in eventArgs.updated)
        {
            // 특정 Pose로 강제 설정 (항상 같은 위치와 방향)
            Vector3 fixedPosition = new Vector3(10, 0, -5);
            Quaternion fixedRotation = Quaternion.Euler(0, updatedImage.pose.rotation.eulerAngles.y, 0); // (0, 0, 0)

            updatedImage.transform.SetPositionAndRotation(fixedPosition, fixedRotation);
            navigationBase.transform.SetPositionAndRotation(fixedPosition, fixedRotation);
        }

        foreach (var removedImage in eventArgs.removed)
        {
            // 마커가 제거되었을 때 필요한 로직 추가 가능
        }
    }
   */ 
    private void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (var newImage in eventArgs.added)
        {
            navigationBase = GameObject.Instantiate(trakedImagePrefab);
            navigationTargets.Clear();
            navigationTargets = navigationBase.transform.GetComponentsInChildren<NavigationTarget>().ToList();
            navMeshSurface = navigationBase.transform.GetComponentInChildren<NavMeshSurface>();
        }

        foreach (var updatedImage in eventArgs.updated)
        {
            // 특정 위치 강제 적용
            Vector3 fixedPosition = new Vector3(10, 0, -5);

            // 카메라의 회전을 반영하면서도 90도에서 크게 벗어나지 않도록 제한
            float cameraYRotation = updatedImage.pose.rotation.eulerAngles.y;
            float clampedRotationY = Mathf.Clamp(cameraYRotation, 75f, 105f); // 90도 기준으로 ±15도 허용

            Quaternion fixedRotation = Quaternion.Euler(0, clampedRotationY, 0); // 조정된 회전 적용

            updatedImage.transform.SetPositionAndRotation(fixedPosition, fixedRotation);
            navigationBase.transform.SetPositionAndRotation(fixedPosition, fixedRotation);
        }

        foreach (var removedImage in eventArgs.removed)
        {
            // 마커가 제거되었을 때 필요한 로직 추가 가능
        }
    }
    
}
