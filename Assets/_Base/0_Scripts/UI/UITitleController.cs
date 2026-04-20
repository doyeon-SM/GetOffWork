using UnityEngine;
using TMPro;

public class UITitleController : MonoBehaviour
{
    [Header("Option UI")]
    [SerializeField] private UIOption optionUI_prefab;
    [SerializeField] private Transform uiRoot;

    [Header("이름 입력 UI")]
    [Tooltip("플레이어 이름을 입력받는 TMP_InputField")]
    [SerializeField] private TMP_InputField nameInputField;

    private UIOption currentOptionUI;

    // ── 게임 시작 ──────────────────────────────────────────

    public void OnClickStartButton()
    {
        if (GameFlowManager.Instance == null)
        {
            Debug.LogError("[UITitleController] GameFlowManager Instance가 없습니다.");
            return;
        }

        // 이름 입력 필드가 연결돼 있으면 PlayerBase에 이름 저장
        if (nameInputField != null && PlayerBase.Instance != null)
        {
            string inputName = nameInputField.text.Trim();
            if (!string.IsNullOrEmpty(inputName))
                PlayerBase.Instance.SetPlayerName(inputName);
        }

        GameFlowManager.Instance.StartNewGame();
    }

    // ── 기타 버튼 ─────────────────────────────────────────

    public void OnClickQuitButton()
    {
        Application.Quit();
    }

    public void OpenOptionUIButton()
    {
        if (currentOptionUI == null)
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