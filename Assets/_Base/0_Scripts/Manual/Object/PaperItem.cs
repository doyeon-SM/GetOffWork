using UnityEngine;

/// <summary>
/// 프린터에서 출력된 서류 DeskObjectItem.
/// ObjectManagerBox가 Spawn 시 SetData()로 참조를 주입한다.
///
/// [printedRecordId]
///   인쇄 시점 UIMonitorController.CurrentRecord.recordId 값.
///   null/빈 문자열 = 조회 없이 인쇄 (발생하지 않아야 함, Manual에서 방어).
///   Inspector의 _printedRecordId 필드로 확인 가능.
/// </summary>
public class PaperItem : DeskObjectItem
{
    private ComplaintContext   complaint;
    private ServiceDeskManager serviceDeskManager;
    private UIPaperView        paperView;
    private UserRecordDatabase database;
    private ObjectManagerBox   managerBoxRef;

    [Header("인쇄 정보 (읽기 전용)")]
    [SerializeField] private string _printedRecordId;

    /// <summary>인쇄된 RecordId — Inspector에서도 확인 가능.</summary>
    public string PrintedRecordId => _printedRecordId;

    /// <summary>ObjectManagerBox가 Spawn 직후 호출.</summary>
    public void SetData(
        ComplaintContext   ctx,
        ServiceDeskManager manager,
        UIPaperView        view,
        UserRecordDatabase db,
        ObjectManagerBox   box,
        string             printedRecordId = null)
    {
        complaint          = ctx;
        serviceDeskManager = manager;
        paperView          = view;
        database           = db;
        managerBoxRef      = box;
        _printedRecordId   = printedRecordId;

        Debug.Log($"[PaperItem] SetData — printedRecordId={_printedRecordId ?? "(null)"}");
    }

    protected override void OnItemClicked()
    {
        if (paperView == null)
        {
            Debug.LogWarning("[PaperItem] paperView가 null입니다.");
            return;
        }
        // UIFullIDPaperView.Show()에서 _printedRecordId 기반으로 레코드 조회
        paperView.Show(complaint, database, _printedRecordId);
        Debug.Log("[PaperItem] 서류 상세 표시");
    }

    protected override void OnItemDropped()
    {
        Debug.Log($"[PaperItem] TakeZone={IsInTakeZone} → 반납 대기");
        serviceDeskManager?.ExecuteCommand(ManualCommandIds.ReturnPrintedDoc);
    }
}
