using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 메뉴얼의 기반 클래스.
///
/// 구조:
///   RequiredSteps       : 절차 정의 (List) — 평가에 사용
///   ActionQueue         : 플레이어 행동 기록 (Queue)
///   RequiredReturnItems : 반납 필수 오브젝트 목록 (DeskObjectType 열거)
///                         하위 클래스가 BuildReturnItems()에서 채운다.
///                         ObjectManagerBox가 이 목록을 읽어 반납 검사를 수행한다.
///
/// 종료 흐름:
///   발급/전송/반려 완료 → IsDelivered = true (민원 처리 완료, 아직 종료 아님)
///   호출 버튼 클릭 → ObjectManagerBox.TryFinishAndReturn() 검사
///   → 반납 완료 → ServiceDeskManager.FinishCurrentCustomer() → 정산 → OnCustomerCleared
/// </summary>
public abstract class Manual
{
    // ── UI 버튼 목록 ─────────────────────────────────────────────────────
    protected List<QuestionData> commandList = new();
    public IReadOnlyList<QuestionData> CommandList => commandList;

    // ── 절차 정의 (List) ──────────────────────────────────────────────────
    protected List<ManualStepEntry> requiredSteps = new();
    public IReadOnlyList<ManualStepEntry> RequiredSteps => requiredSteps;

    // ── 행동 기록 (Queue) ─────────────────────────────────────────────────
    private Queue<PlayerActionRecord> actionQueue = new();
    public IReadOnlyCollection<PlayerActionRecord> ActionQueue => actionQueue;

    // ── 반납 필수 오브젝트 목록 ───────────────────────────────────────────
    // ObjectManagerBox가 이 목록을 읽어 "반납해야 할 오브젝트가 모두 있는가" 를 판정한다.
    protected List<DeskObjectType> requiredReturnItems = new();
    public IReadOnlyList<DeskObjectType> RequiredReturnItems => requiredReturnItems;

    // ── 상태 ─────────────────────────────────────────────────────────────
    protected ComplaintContext context;
    protected bool isCompleted;   // 발급/전송/반려 완료 여부
    private float sessionStartTime;

    /// <summary>발급/전송/반려가 완료됐는가 (반납 완료 여부와 무관)</summary>
    public bool IsCompleted => isCompleted;

    // ── 초기화 ───────────────────────────────────────────────────────────
    public virtual void Initialize(ComplaintContext newContext)
    {
        context           = newContext;
        isCompleted       = false;
        sessionStartTime  = Time.time;

        commandList.Clear();
        requiredSteps.Clear();
        actionQueue.Clear();
        requiredReturnItems.Clear();

        BuildCommandList();
        BuildSteps();
        BuildReturnItems();
    }

    protected abstract void BuildCommandList();
    protected abstract void BuildSteps();

    /// <summary>
    /// 이 메뉴얼에서 반납해야 할 오브젝트 종류를 등록한다.
    /// 예: requiredReturnItems.Add(DeskObjectType.IDCard);
    /// </summary>
    protected abstract void BuildReturnItems();

    /// <summary>
    /// 민원인이 실제로 물품을 제출할 때 호출.
    /// 동적으로 필수 반납 목록에 추가한다.
    /// </summary>
    public void AddRequiredReturnItem(DeskObjectType objectType)
    {
        if (!requiredReturnItems.Contains(objectType))
            requiredReturnItems.Add(objectType);
    }

    /// <summary>
    /// 응대 종료 시 필수 반납 목록 초기화.
    /// ServiceDeskManager.FinishCurrentCustomer()에서 호출.
    /// </summary>
    public void ClearRequiredReturnItems()
    {
        requiredReturnItems.Clear();
    }


    // ── 실행 ─────────────────────────────────────────────────────────────
    public abstract ResponseResult Execute(string commandId, string payload = null);

    protected void RecordAction(string commandId)
    {
        float elapsed = Time.time - sessionStartTime;
        actionQueue.Enqueue(new PlayerActionRecord(commandId, elapsed));
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────
    protected ResponseResult WrongOrder(string customerMessage = "")
    {
        return ResponseResult.Create(
            isValid: false,
            isCompleted: false,
            customerMessage: customerMessage
        );
    }

    protected ResponseResult CorrectResponse(
        string customerMessage          = "",
        bool   completeNow              = false,
        bool   shouldSpawnIdCard        = false,
        bool   shouldOpenIdCardDetail   = false,
        bool   shouldOpenMonitor        = false,
        bool   shouldRefreshMonitorData = false)
    {
        if (completeNow)
            isCompleted = true;

        return ResponseResult.Create(
            isValid: true,
            isCompleted: completeNow,
            customerMessage: customerMessage,
            shouldSpawnIdCard: shouldSpawnIdCard,
            shouldOpenIdCardDetail: shouldOpenIdCardDetail,
            shouldOpenMonitor: shouldOpenMonitor,
            shouldRefreshMonitorData: shouldRefreshMonitorData
        );
    }

    public virtual string GetManualTitle() => "기본 메뉴얼";
}
