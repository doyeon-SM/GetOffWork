using UnityEngine;

/// <summary>
/// 신분증 DeskObjectItem.
/// ObjectManagerBox가 Spawn 시 SetComplaint()로 참조를 주입한다.
///
/// displayRecordId를 명시적으로 지정해 어떤 레코드를 표시할지 결정한다.
///   - IDCard(방문객)     : applicantRecordId  (창구에 온 사람, 대리인 절차의 대리인 본인)
///   - ProxyIDCard(대상자): targetRecordId     (대리 요청 대상자, 실제 발급 받을 사람)
/// </summary>
public class IDCardItem : DeskObjectItem
{
    private ComplaintContext   complaint;
    private ServiceDeskManager serviceDeskManager;
    private UIIDCardView       cardView;
    private bool               detailOpened;

    /// <summary>
    /// 이 신분증 오브젝트가 표시할 레코드 ID.
    /// ObjectManagerBox가 Spawn 직후 명시적으로 지정한다.
    /// </summary>
    private string displayRecordId;

    /// <summary>
    /// ObjectManagerBox가 Spawn 직후 호출.
    /// cardView는 ObjectManagerBox가 런타임 생성한 인스턴스를 그대로 전달한다.
    /// displayId : 이 신분증에 표시할 레코드 ID
    ///   - 방문객 신분증(IDCard)     → complaint.applicantRecordId
    ///   - 대리인 신분증(ProxyIDCard) → complaint.targetRecordId
    ///   - null 또는 빈 문자열 시 EffectiveTargetRecordId 사용 (기존 동작 유지)
    /// </summary>
    public void SetComplaint(
        ComplaintContext   ctx,
        ServiceDeskManager manager,
        UIIDCardView       view,
        string             displayId = null)
    {
        complaint          = ctx;
        serviceDeskManager = manager;
        cardView           = view;
        displayRecordId    = string.IsNullOrEmpty(displayId)
            ? ctx?.EffectiveTargetRecordId
            : displayId;
    }

protected override void OnItemClicked()
    {
        if (serviceDeskManager == null || complaint == null) return;

        // 첫 클릭에만 OpenIdCardDetail 절차 기록 (새 ID카드는 직접 코맨드 불필요)
        if (!detailOpened && ObjectType != DeskObjectType.NewIDCard)
        {
            serviceDeskManager.ExecuteCommand(ManualCommandIds.OpenIdCardDetail);
            detailOpened = true;
        }

        if (cardView == null)
        {
            Debug.LogWarning("[IDCardItem] cardView가 null입니다.");
            return;
        }

        string recordId = !string.IsNullOrEmpty(displayRecordId)
            ? displayRecordId
            : complaint.EffectiveTargetRecordId;

        if (!serviceDeskManager.TryGetResidentRecord(recordId, out UserRecordData record)) return;

        if (ObjectType == DeskObjectType.NewIDCard)
        {
            // 새 ID카드: 런타임에 수정된 주소(record.address)를 표시
            // SetIdCard가 호출되지 않은 상태이므로 IdCard필드를 직접 동기화
            record.IdCardAddress = record.address;
            record.IdCardId      = record.recordId;
            record.IdCardPortrait = record.portrait;
            cardView.Show(record);
            Debug.Log($"[IDCardItem] 새 ID카드 상세 표시 — 변경 주소={record.address}");
        }
        else
        {
            // 기존 ID카드: IdCardAddress(변경 전 또는 fake 주소) 표시
            cardView.Show(record);
            Debug.Log($"[IDCardItem] 기존 ID카드 상세 표시 — recordId={recordId}");
        }
    }

    protected override void OnItemDropped()
    {
        Debug.Log($"[IDCardItem] 드롭 — TakeZone={IsInTakeZone}");
    }
}
