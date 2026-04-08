using UnityEngine;

/// <summary>
/// 신분증 DeskObjectItem.
/// ObjectManagerBox가 Spawn 시 SetComplaint()로 참조를 주입한다.
/// FindFirstObjectByType에 의존하지 않으므로 UI가 비활성이어도 안전하다.
/// </summary>
public class IDCardItem : DeskObjectItem
{
    private ComplaintContext   complaint;
    private ServiceDeskManager serviceDeskManager;
    private UIIDCardView       cardView;
    private bool               detailOpened;

    /// <summary>
    /// ObjectManagerBox가 Spawn 직후 호출.
    /// cardView는 ObjectManagerBox가 런타임 생성한 인스턴스를 그대로 전달한다.
    /// </summary>
    public void SetComplaint(
        ComplaintContext   ctx,
        ServiceDeskManager manager,
        UIIDCardView       view)
    {
        complaint          = ctx;
        serviceDeskManager = manager;
        cardView           = view;
    }

    protected override void OnItemClicked()
    {
        if (serviceDeskManager == null || complaint == null) return;

        // 첫 클릭에만 OpenIdCardDetail 절차 기록
        if (!detailOpened)
        {
            serviceDeskManager.ExecuteCommand(ManualCommandIds.OpenIdCardDetail);
            detailOpened = true;
        }

        if (cardView == null)
        {
            Debug.LogWarning("[IDCardItem] cardView가 null입니다. SetComplaint가 호출됐는지 확인하세요.");
            return;
        }

        string recordId = complaint.EffectiveTargetRecordId;
        if (serviceDeskManager.TryGetResidentRecord(recordId, out UserRecordData record))
            cardView.Show(record);

        Debug.Log("[IDCardItem] 신분증 상세 표시");
    }

    protected override void OnItemDropped()
    {
        Debug.Log($"[IDCardItem] 드롭 — TakeZone={IsInTakeZone}");
    }
}
