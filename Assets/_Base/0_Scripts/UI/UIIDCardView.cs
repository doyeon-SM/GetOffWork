using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 신분증 상세 UI.
///
/// [표시 규칙]
/// 모니터(DB) → record.address / record.recordId / record.portrait  (실제/올바른 정보)
/// ID카드     → record.IdCardAddress / record.IdCardId / record.IdCardPortrait
///              (fake 필드가 있으면 틀린 정보, 없으면 실제 정보)
/// 플레이어가 둘을 비교해 불일치를 직접 판단한다.
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

        // ID카드 표시: fake 필드가 있으면 가짜 정보, 없으면 실제 정보
        if (portraitImage != null) portraitImage.sprite = record.IdCardPortrait;
        if (idText        != null) idText.text          = record.IdCardId;
        if (nameText      != null) nameText.text        = record.fullName;
        if (addressText   != null) addressText.text     = record.IdCardAddress;

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
