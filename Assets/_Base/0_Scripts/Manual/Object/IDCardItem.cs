using UnityEngine;

/// <summary>
/// 신분증 DeskObjectItem.
/// ObjectManagerBox가 Spawn 시 SetComplaint()로 참조를 주입한다.
///
/// [표시 규칙]
/// SetComplaint() 시점에 ID카드에 표시할 값(주소/ID/초상화)을 모두 스냅샷으로 캐싱한다.
/// 이후 대기열의 다른 민원인이 동일한 UserRecordData SO에 SetIdCard()를 호출해도
/// 이미 캐싱된 값을 사용하므로 오염되지 않는다.
///
/// - IDCard (기존 신분증) : SetComplaint 시점의 IdCard* 스냅샷을 표시
/// - NewIDCard (새 신분증) : 런타임에 수정된 record.address 등을 직접 표시
/// </summary>
public class IDCardItem : DeskObjectItem
{
    private ComplaintContext   complaint;
    private ServiceDeskManager serviceDeskManager;
    private UIIDCardView       cardView;
    private bool               detailOpened;

    /// <summary>이 신분증 오브젝트가 표시할 레코드 ID.</summary>
    private string displayRecordId;

    // ── SetComplaint 시점 스냅샷 (SO 오염 방지) ──────────────────────────
    private string cachedIdCardAddress;
    private string cachedIdCardId;
    private Sprite cachedIdCardPortrait;
    private string cachedFullName;

    /// <summary>
    /// NewID 등록 전 런타임 UserRecordData 직접 참조.
    /// DB에 방문객이 없을 때 TryGetResidentRecord 폴백용.
    /// </summary>
    private UserRecordData _runtimeRecord;

    // ── 초기화 ───────────────────────────────────────────────────────────

    /// <summary>
    /// ObjectManagerBox가 Spawn 직후 호출.
    /// 이 시점에 IdCard* 값을 스냅샷으로 캐싱해 이후 SO 오염을 방지한다.
    /// displayId : 이 신분증에 표시할 레코드 ID
    ///   - 방문객 신분증(IDCard)      → complaint.applicantRecordId
    ///   - 대리인 신분증(ProxyIDCard) → complaint.targetRecordId
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
        _runtimeRecord = ctx?.runtimeUserData as UserRecordData;

        string rid = !string.IsNullOrEmpty(displayRecordId)
            ? displayRecordId
            : ctx?.EffectiveTargetRecordId;

        if (!string.IsNullOrEmpty(rid) && manager != null
            && manager.TryGetResidentRecord(rid, out UserRecordData rec))
        {
            // ★ SetComplaint 시점에 IdCard* 값을 스냅샷으로 캐싱
            // 이 이후 다른 민원인의 RollMismatches → SetIdCard가 SO를 덮어써도 안전
            cachedIdCardAddress  = rec.IdCardAddress;
            cachedIdCardId       = rec.IdCardId;
            cachedIdCardPortrait = rec.IdCardPortrait;
            cachedFullName       = rec.fullName;
            Debug.Log($"[IDCardItem] 스냅샷 캐싱 — id={cachedIdCardId} addr={cachedIdCardAddress}");
        }
        else if (_runtimeRecord != null)
        {
            cachedIdCardAddress  = _runtimeRecord.IdCardAddress;
            cachedIdCardId       = _runtimeRecord.IdCardId;
            cachedIdCardPortrait = _runtimeRecord.IdCardPortrait;
            cachedFullName       = _runtimeRecord.fullName;
        }
    }

    // ── 클릭 / 드롭 ──────────────────────────────────────────────────────

    protected override void OnItemClicked()
    {
        if (serviceDeskManager == null || complaint == null) return;
        if (!detailOpened && ObjectType != DeskObjectType.NewIDCard)
        {
            serviceDeskManager.ExecuteCommand(ManualCommandIds.OpenIdCardDetail);
            detailOpened = true;
        }
        if (cardView == null) { Debug.LogWarning("[IDCardItem] cardView null"); return; }

        if (ObjectType == DeskObjectType.NewIDCard)
        {
            // NewIDCard: DB에서 직접 읽어 현재(변경 후) 정보 표시
            string recordId = !string.IsNullOrEmpty(displayRecordId)
                ? displayRecordId : complaint.EffectiveTargetRecordId;
            UserRecordData record;
            if (!serviceDeskManager.TryGetResidentRecord(recordId, out record))
            {
                if (_runtimeRecord != null)
                    record = _runtimeRecord;
                else { Debug.LogWarning($"[IDCardItem] 레코드 미발견: {recordId}"); return; }
            }
            record.IdCardAddress  = record.address;
            record.IdCardId       = record.recordId;
            record.IdCardPortrait = record.portrait;
            cardView.Show(record);
        }
        else
        {
            // 기존 IDCard / ProxyIDCard: 스냅샷 캐싱값으로 임시 UserRecordData 구성해 표시
            // SO를 직접 수정하지 않으므로 다른 민원인 영향 없음
            var snapshot = ScriptableObject.CreateInstance<UserRecordData>();
            snapshot.IdCardAddress  = cachedIdCardAddress;
            snapshot.IdCardId       = cachedIdCardId;
            snapshot.IdCardPortrait = cachedIdCardPortrait;
            snapshot.fullName       = cachedFullName;
            cardView.Show(snapshot);
            Destroy(snapshot); // 즉시 정리
        }
    }

    protected override void OnItemDropped()
    {
        if (ObjectType == DeskObjectType.NewIDCard && IsInTakeZone)
        {
            serviceDeskManager?.ExecuteCommand(ManualCommandIds.ReturnNewIdCard);
            Debug.Log("[IDCardItem] 새 ID카드 TakeZone 반납 → ReturnNewIdCard 실행");
        }
    }
}
