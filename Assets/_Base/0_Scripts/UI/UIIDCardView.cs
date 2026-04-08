using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 신분증 상세 UI.
/// ObjectManagerBox가 런타임에 Canvas 위에 Instantiate한다.
/// Hide()는 SetActive(false) 대신 CanvasGroup.alpha로 처리해
/// 비활성화로 인한 FindFirstObjectByType 탐색 실패 문제를 방지한다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIIDCardView : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private Image    portraitImage;
    [SerializeField] private TMP_Text idText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text addressText;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    public void Show(UserRecordData record)
    {
        if (record == null) return;

        if (portraitImage != null) portraitImage.sprite = record.portrait;
        if (idText        != null) idText.text          = record.recordId;
        if (nameText      != null) nameText.text        = record.fullName;
        if (addressText   != null) addressText.text     = record.address;

        canvasGroup.alpha          = 1f;
        canvasGroup.interactable   = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void Hide()
    {
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnClickClose() => Hide();
}
