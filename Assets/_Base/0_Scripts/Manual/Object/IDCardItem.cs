using UnityEngine;

/// <summary>
/// 신분증 DeskObjectItem.
/// ObjectManagerBox가 Spawn 시 SetComplaint()로 참조를 주입한다.
///
/// [주소 표시 규칙]
/// - IDCard (기존 신분증) : SetComplaint 시점에 캐싱한 변경 전 주소를 표시
///                         (ApplyRuntimeAddressChange가 record.address를 덮어써도 안전)
/// - NewIDCard (새 신분증) : 런타임에 수정된 주소(record.address)를 표시
/// </summary>
public class IDCardItem : DeskObjectItem
{
    private ComplaintContext   complaint;
    private ServiceDeskManager serviceDeskManager;
    private UIIDCardView       cardView;
    private bool               detailOpened;

    /// <summary>이 신분증 오브젝트가 표시할 레코드 ID.</summary>
    private string displayRecordId;

    /// <summary>
    /// SetComplaint 시점에 캐싱한 변경 전 주소.
    /// SubmitNewAddress가 record.address를 런타임 수정하기 전에 기록해두므로
    /// 기존 ID카드 클릭 시 변경 전 주소를 안정적으로 표시할 수 있다.
    /// </summary>
    private string cachedAddressBeforeChange;

    // ── 초기화 ───────────────────────────────────────────────────────────

    /// <summary>
    /// ObjectManagerBox가 Spawn 직후 호출.
    /// displayId : 이 신분증에 표시할 레코드 ID
    ///   - 방문객 신분증(IDCard)      → complaint.applicantRecordId
    ///   - 대리인 신분증(ProxyIDCard) → complaint.targetRecordId
    ///   - null/빈 문자열 시 EffectiveTargetRecordId 사용
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

        // 주소 변경 전 주소를 미리 캐싱 (SubmitNewAddress 실행 전 시점)
        string rid = string.IsNullOrEmpty(displayRecordId)
            ? ctx?.EffectiveTargetRecordId
            : displayRecordId;
        if (!string.IsNullOrEmpty(rid) && manager != null
            && manager.TryGetResidentRecord(rid, out UserRecordData rec))
        {
            cachedAddressBeforeChange = rec.address;
        }
    }

    // ── 클릭 / 드롭 ──────────────────────────────────────────────────────

    protected override void OnItemClicked()
    {
        if (serviceDeskManager == null || complaint == null) return;

        // 첫 클릭에만 OpenIdCardDetail 절차 기록 (새 ID카드는 불필요)
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
            // 새 ID카드: 런타임 수정된 주소(record.address) 표시
            record.IdCardAddress  = record.address;
            record.IdCardId       = record.recordId;
            record.IdCardPortrait = record.portrait;
            cardView.Show(record);
            Debug.Log($"[IDCardItem] 새 ID카드 상세 표시 — 변경 주소={record.address}");
        }
        else
        {
            // 기존 ID카드: ApplyRuntimeAddressChange가 IdCardAddress를 덮어쓸 수 있으므로
            // SetComplaint 시점에 캐싱한 변경 전 주소를 강제 적용
            if (!string.IsNullOrEmpty(cachedAddressBeforeChange))
                record.IdCardAddress = cachedAddressBeforeChange;
            cardView.Show(record);
            Debug.Log($"[IDCardItem] 기존 ID카드 상세 표시 — 변경 전 주소={record.IdCardAddress}");
        }
    }

    protected override void OnItemDropped()
    {
        // NewIDCard가 TakeZone에 드롭될 때 ReturnNewIdCard 커맨드 실행
        if (ObjectType == DeskObjectType.NewIDCard && IsInTakeZone)
        {
            serviceDeskManager?.ExecuteCommand(ManualCommandIds.ReturnNewIdCard);
            Debug.Log("[IDCardItem] 새 ID카드 TakeZone 반납 → ReturnNewIdCard 실행");
        }
    }
}
