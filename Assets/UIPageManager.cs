using UnityEngine;

public class UIPageManager : MonoBehaviour
{
    [Header("페이지 목록")]
    public GameObject mainPage;
    public GameObject qrPopupPage;
    public GameObject adminPage;

    private GameObject currentPage;

    void Start()
    {
        // 초기 페이지 설정
        ShowPage(mainPage);
    }

    public void ShowPage(GameObject targetPage)
    {
        // 현재 페이지 비활성화
        if (currentPage != null)
            currentPage.SetActive(false);

        // 새 페이지 활성화
        targetPage.SetActive(true);
        currentPage = targetPage;
    }
}
