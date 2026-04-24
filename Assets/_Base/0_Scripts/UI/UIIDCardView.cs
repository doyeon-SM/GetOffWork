using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 신분증 상세 UI.
///
/// [표시 규칙]
/// 모니터(DB) → record.address / record.recordId / record.portrait  (실제/올바른 정보)
/// ID카드     → Spawn 시점에 계산된 displayId / displayAddress / displayPortrait
///              (fake 여부는 ObjectManagerBox가 ComplaintContext 플래그 기반으로 결정)
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

    /// <summary>신분증 카드 표시값을 직접 받아 출력한다. SO를 참조하지 않는다.</summary>
    public void Show(string displayId, string displayAddress, string fullName, Sprite displayPortrait)
    {
        if (portraitImage != null) portraitImage.sprite = displayPortrait;
        if (idText        != null) idText.text          = displayId;
        if (nameText      != null) nameText.text        = fullName;
        if (addressText   != null) addressText.text     = displayAddress;

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
