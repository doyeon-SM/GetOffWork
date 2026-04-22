using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 시작 전 "튜토리얼을 진행하시겠습니까?" 팝업.
/// TutorialManager.RequestTutorialPrompt()에 의해 활성화된다.
/// 예 → TutorialManager.OnPromptAccepted()
/// 아니요 → TutorialManager.OnPromptDeclined()
/// </summary>
public class UITutorialPrompt : MonoBehaviour
{
    public static UITutorialPrompt Instance { get; private set; }

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button     btnYes;
    [SerializeField] private Button     btnNo;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (panelRoot != null) panelRoot.SetActive(false);

        if (btnYes != null) btnYes.onClick.AddListener(OnClickYes);
        if (btnNo  != null) btnNo.onClick.AddListener(OnClickNo);
    }

    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnClickYes()
    {
        Hide();
        TutorialManager.Instance?.OnPromptAccepted();
    }

    private void OnClickNo()
    {
        Hide();
        TutorialManager.Instance?.OnPromptDeclined();
    }
}
