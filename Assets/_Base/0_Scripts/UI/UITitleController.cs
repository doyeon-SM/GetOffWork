using UnityEngine;

public class UITitleController : MonoBehaviour
{
    [Header("Option UI")]
    [SerializeField] private UIOption optionUI_prefab;
    [SerializeField] private Transform uiRoot;

    private UIOption currentOptionUI;

    public void OnClickStartButton()
    {
        if (GameFlowManager.Instance == null)
        {
            Debug.LogError("GameFlowManager Instance가 없습니다.");
            return;
        }

        GameFlowManager.Instance.StartNewGame();
    }

    public void OnClickQuitButton()
    {
        Application.Quit();
    }

    public void OpenOptionUIButton()
    {
        if(currentOptionUI == null)
        {
            currentOptionUI = Instantiate(optionUI_prefab, uiRoot);
            currentOptionUI.gameObject.SetActive(true);
        }
        else
        {
            currentOptionUI.Open();
        }
    }
}