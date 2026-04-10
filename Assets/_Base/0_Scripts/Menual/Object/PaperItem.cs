using UnityEngine;

/// <summary>
/// 프린터에서 출력된 서류 DeskObjectItem.
/// ObjectManagerBox가 Spawn 시 SetData()로 참조를 주입한다.
///
/// 동작:
///   - 클릭 → 민원 유형에 맞는 UIPaperView(paperView)를 열어 내용 표시
///   - TakeZone에 드롭 → ObjectManagerBox에서 자신을 해제한 뒤 Destroy
///     ※ 반납 필수 절차 검사는 추후 수정 예정
/// </summary>
public class PaperItem : DeskObjectItem
{
    private ComplaintContext   complaint;
    private ServiceDeskManager serviceDeskManager;
    private UIPaperView        paperView;
    private UserRecordDatabase database;
    private ObjectManagerBox   managerBoxRef;  // UnregisterItem 호출용

    /// <summary>
    /// ObjectManagerBox가 Spawn 직후 호출.
    /// </summary>
    public void SetData(
        ComplaintContext   ctx,
        ServiceDeskManager manager,
        UIPaperView        view,
        UserRecordDatabase db,
        ObjectManagerBox   box)
    {
        complaint       = ctx;
        serviceDeskManager = manager;
        paperView       = view;
        database        = db;
        managerBoxRef   = box;
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
        //if (!IsInTakeZone) return;

        Debug.Log($"[PaperItem] TakeZone={IsInTakeZone} → 반납 대기");
        //paperView?.Hide();

        // 평가에서 제외되는 시스템 커맨드지만, 반납 확인을 위해 실행
        serviceDeskManager?.ExecuteCommand(ManualCommandIds.ReturnPrintedDoc);

        //managerBoxRef?.UnregisterItem(this);
        //Destroy(gameObject);
    }
}
