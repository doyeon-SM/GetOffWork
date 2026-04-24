using UnityEngine;

/// <summary>
/// 신분증 DeskObjectItem.
/// ObjectManagerBox가 Spawn 시 SetDisplayValues()로 표시값을 주입한다.
///
/// [표시 규칙]
/// - IDCard / ProxyIDCard : Spawn 시점에 전달된 displayId/Address/Portrait 를 그대로 표시.
///   SO를 일절 참조하지 않으므로 대기열 내 동일 SO 공유로 인한 오염이 없다.
/// - NewIDCard : DB에서 직접 읽어 현재(변경 후) 정보를 표시한다.
/// </summary>
public class IDCardItem : DeskObjectItem
{
    private ComplaintContext   complaint;
    private ServiceDeskManager serviceDeskManager;
    private UIIDCardView       cardView;
    private bool               detailOpened;

    // ── Spawn 시점에 주입된 표시값 (SO 비참조) ───────────────────────────
    private string _displayId;
    private string _displayAddress;
    private Sprite _displayPortrait;
    private string _displayFullName;

    /// <summary>NewID 등록 전 런타임 UserRecordData 직접 참조 (NewIDCard 전용).</summary>
    private UserRecordData _runtimeRecord;
    /// <summary>이 신분증이 표시할 레코드 ID (NewIDCard DB 조회용).</summary>
    private string _displayRecordId;

    // ── 초기화 ───────────────────────────────────────────────────────────

    /// <summary>
    /// ObjectManagerBox가 Spawn 직후 호출.
    /// Spawn 시점에 이미 계산된 표시값을 직접 전달한다.
    /// SO에 아무것도 쓰지 않으므로 대기열 오염이 구조적으로 불가능하다.
    /// </summary>
    public void SetComplaint(
        ComplaintContext   ctx,
        ServiceDeskManager manager,
        UIIDCardView       view,
        string             displayId,
        string             displayAddress,
        Sprite             displayPortrait,
        string             fullName,
        string             recordIdForNewCard = null)
    {
        complaint          = ctx;
        serviceDeskManager = manager;
        cardView           = view;
        _runtimeRecord     = ctx?.runtimeUserData as UserRecordData;
        _displayRecordId   = recordIdForNewCard;

        _displayId       = displayId;
        _displayAddress  = displayAddress;
        _displayPortrait = displayPortrait;
        _displayFullName = fullName;

        Debug.Log($"[IDCardItem] 표시값 설정 — id={_displayId} addr={_displayAddress}");
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
            string rid = !string.IsNullOrEmpty(_displayRecordId)
                ? _displayRecordId : complaint.EffectiveTargetRecordId;
            UserRecordData record;
            if (!serviceDeskManager.TryGetResidentRecord(rid, out record))
            {
                if (_runtimeRecord != null) record = _runtimeRecord;
                else { Debug.LogWarning($"[IDCardItem] 레코드 미발견: {rid}"); return; }
            }
            // NewIDCard는 현재 DB 값(변경 후)을 정상 표시
            cardView.Show(record.recordId, record.address, record.fullName, record.portrait);
        }
        else
        {
            // IDCard / ProxyIDCard: Spawn 시 주입된 표시값을 그대로 사용
            cardView.Show(_displayId, _displayAddress, _displayFullName, _displayPortrait);
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
