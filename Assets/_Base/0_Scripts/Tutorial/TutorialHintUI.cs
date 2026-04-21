using TMPro;
using UnityEngine;

/// <summary>
/// 튜토리얼 힌트 텍스트를 화면에 표시하는 UI 싱글턴.
/// Canvas 위에 배치된 패널과 TMP 텍스트를 연결해서 사용한다.
/// </summary>
public class TutorialHintUI : MonoBehaviour
{
    public static TutorialHintUI Instance { get; private set; }

    [Header("UI 연결")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text   hintText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (hintPanel != null) hintPanel.SetActive(false);
    }

    public void Show(string text)
    {
        if (string.IsNullOrEmpty(text)) { HidePanel(); return; }
        if (hintText  != null) hintText.text = text;
        if (hintPanel != null) hintPanel.SetActive(true);
    }

    public void HidePanel()
    {
        if (hintPanel != null) hintPanel.SetActive(false);
    }

    // 외부에서 Hide()로 호출 가능하도록 별칭 제공
    public void Hide() => HidePanel();
}
