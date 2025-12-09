using UnityEngine;
using UnityFigmaBridge.Runtime.UI;

public class GoToUnityPanel : MonoBehaviour
{
    [SerializeField] private PrototypeFlowController flowController;
    //[SerializeField] private GameObject mainPanel;
    //[SerializeField] private GameObject figmaMain;

    public void OnAdminClick()
    {
        // 1 버튼 클릭이 인식됐는지 확인
        Debug.Log("[GoToUnityPanel] 관리자 버튼 클릭됨!");

        // 2flowController가 연결되었는지 확인
        if (flowController == null)
        {
            Debug.LogError("[GoToUnityPanel] flowController가 연결되지 않았습니다!");
            return;
        }

        // 3 이동 실행 로그
        Debug.Log("[GoToUnityPanel] TransitionToScreenByName(MainPanel) 실행 중...");
        flowController.TransitionToScreenByName("MainPanel");
        //figmaMain.SetActive(false);
        //mainPanel.SetActive(true);
    }
}
