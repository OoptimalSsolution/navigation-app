using UnityEngine;
using UnityEngine.UI;

public class PageButton : MonoBehaviour
{
    [SerializeField] private UIPageManager pageManager;
    [SerializeField] private GameObject targetPage;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            pageManager.ShowPage(targetPage);
        });
    }
}
