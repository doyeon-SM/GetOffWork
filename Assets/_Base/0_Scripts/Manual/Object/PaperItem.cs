using UnityEngine;

/// <summary>
/// 프린터에서 출력된 서류 DeskObjectItem.
/// ObjectManagerBox가 Spawn 시 SetData()로 참조를 주입한다.
///
/// [printedRecordId]
///   인쇄 시점에 어떤 RecordId로 발급됐는지 기록한다.
///   null 또는 빈 문자열 = 조회 없이 인쇄한 빈 종이.
///   TryFinishAndReturn()에서 이 값을 읽어 올바른 발급 여부를 판단한다.
/// </summary>
public class PaperItem : DeskObjectItem
{
    private ComplaintContext   complaint;
    private ServiceDeskManager serviceDeskManager;
    private UIPaperView        paperView;
    private UserRecordDatabase database;
    private ObjectManagerBox   managerBoxRef;

    /// <summary>
    /// 인쇄 시점에 기록된 RecordId.
    /// null/빈 문자열이면 조회 없이 인쇄한 빈 종이.
    /// </summary>
    public string PrintedRecordId { get; private set; }

    /// <summary>
    /// ObjectManagerBox가 Spawn 직후 호출.
    /// </summary>
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
        PrintedRecordId    = printedRecordId;

        Debug.Log($"[PaperItem] SetData — printedRecordId={printedRecordId ?? "(null=빈종이)"}  ");
    }

    protected override void OnItemClicked()
    {
        if (paperView == null)
        {
            Debug.LogWarning("[PaperItem] paperView가 null입니다. SetData가 호출됐는지 확인하세요.");
            return;
        }

        paperView.Show(complaint, database);
        Debug.Log("[PaperItem] 서류 상세 표시");
    }

    protected override void OnItemDropped()
    {
        Debug.Log($"[PaperItem] TakeZone={IsInTakeZone} → 반납 대기");
        serviceDeskManager?.ExecuteCommand(ManualCommandIds.ReturnPrintedDoc);
    }
}
