using TMPro;
using UnityEngine;

/// <summary>
/// 주민등록등본/초본 발급 서류 UI.
/// printedRecordId 기준으로 레코드를 조회해 표시한다.
/// 여러 장 인쇄 시 각 paper마다 자신의 recordId에 맞는 정보를 표시하므로
/// 플레이어가 어떤 정보가 인쇄됐는지 직접 확인할 수 있다.
/// </summary>
public class UIFullIDPaperView : UIPaperView
{
    [Header("UI 요소")]
    [SerializeField] private TMP_Text idText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text addressText;
    [SerializeField] private TMP_Text birthDateText;
    [SerializeField] private TMP_Text purposeText;

    public override void Show(ComplaintContext complaint, UserRecordDatabase database,
        string printedRecordId = null)
    {
        if (complaint == null) return;

        // printedRecordId 기준으로 레코드 조회
        // null이면 complaint.EffectiveTargetRecordId fallback (하위 호환)
        string lookupId = !string.IsNullOrEmpty(printedRecordId)
            ? printedRecordId
            : complaint.EffectiveTargetRecordId;

        UserRecordData record = null;
        database?.TryGetRecord(lookupId, out record);

        if (idText        != null) idText.text        = record != null ? record.recordId  : lookupId;
        if (nameText      != null) nameText.text       = record != null ? record.fullName  : "-";
        if (addressText   != null) addressText.text    = record != null ? record.address   : "-";
        if (birthDateText != null) birthDateText.text  = record != null ? record.birthDate : "-";

        if (purposeText != null)
            purposeText.text = complaint.applicantType == ComplaintContext.ApplicantType.Self
                ? "본인 발급"
                : "대리 발급";

        canvasGroup.alpha          = 1f;
        canvasGroup.interactable   = true;
        canvasGroup.blocksRaycasts = true;
    }
}
