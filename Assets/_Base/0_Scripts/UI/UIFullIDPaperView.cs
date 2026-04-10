using TMPro;
using UnityEngine;

/// <summary>
/// 주민등록등본/초본 발급 서류 UI.
/// UIPaperView를 상속받아 FullID 민원에 맞는 필드를 채운다.
///
/// 표시 항목:
///   - ID (recordId)
///   - 이름 (fullName)
///   - 주소 (address)
///   - 생년월일 (birthDate)
///   - 발급 목적 (본인 / 대리)
/// </summary>
public class UIFullIDPaperView : UIPaperView
{
    [Header("UI 요소")]
    [SerializeField] private TMP_Text idText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text addressText;
    [SerializeField] private TMP_Text birthDateText;
    [SerializeField] private TMP_Text purposeText;

    public override void Show(ComplaintContext complaint, UserRecordDatabase database)
    {
        if (complaint == null) return;

        // 발급 대상 레코드 조회
        string recordId = complaint.EffectiveTargetRecordId;
        UserRecordData record = null;
        database?.TryGetRecord(recordId, out record);

        if (idText        != null) idText.text        = record != null ? record.recordId  : recordId;
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
