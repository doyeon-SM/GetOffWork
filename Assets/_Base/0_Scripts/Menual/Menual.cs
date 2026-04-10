using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 메뉴얼의 기반 클래스.
///
/// 구조:
///   RequiredSteps       : 절차 정의 (List) — 평가에 사용
///   ActionQueue         : 플레이어 행동 기록 (Queue)
///   RequiredReturnItems : 반납 필수 오브젝트 목록 (DeskObjectType 열거)
///
/// 대사 조회 흐름:
///   WrongOrder(commandId)   → ManualDataSO.GetWrongOrderLine(commandId, nuisanceType)
///   CorrectResponse(commandId) → ManualDataSO.GetCorrectLine(commandId, nuisanceType)
///   SO에 대사가 없으면 fallback 문자열 사용
///
/// 종료 흐름:
///   발급/전송/반려 완료 → isCompleted = true
///   호출 버튼 클릭 → ObjectManagerBox.TryFinishAndReturn()
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
    protected List<DeskObjectType> requiredReturnItems = new();
    public IReadOnlyList<DeskObjectType> RequiredReturnItems => requiredReturnItems;

    // ── 상태 ─────────────────────────────────────────────────────────────
    protected ComplaintContext context;
    protected bool isCompleted;
    private float sessionStartTime;

    public bool IsCompleted => isCompleted;

    // ── 초기화 ───────────────────────────────────────────────────────────
    public virtual void Initialize(ComplaintContext newContext)
    {
        context          = newContext;
        isCompleted      = false;
        sessionStartTime = Time.time;

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
    protected abstract void BuildReturnItems();

    // ── ManualDataSO 참조 (하위 클래스에서 제공) ──────────────────────────
    /// <summary>
    /// 하위 클래스가 자신의 manualData를 반환하도록 override한다.
    /// 대사 조회에 사용된다.
    /// </summary>
    protected virtual ManualDataSO GetManualDataSO() => null;

    // ── 반납 관리 ─────────────────────────────────────────────────────────
    public void AddRequiredReturnItem(DeskObjectType objectType)
    {
        if (!requiredReturnItems.Contains(objectType))
            requiredReturnItems.Add(objectType);
    }

    public void ClearRequiredReturnItems()
    {
        requiredReturnItems.Clear();
    }

    // ── 실행 ─────────────────────────────────────────────────────────────
    public abstract ResponseResult Execute(string commandId, string payload = null);

    public void RecordReturnAction(string commandId) => RecordAction(commandId);

    protected void RecordAction(string commandId)
    {
        float elapsed = Time.time - sessionStartTime;
        actionQueue.Enqueue(new PlayerActionRecord(commandId, elapsed));
    }

    // ── 대사 조회 헬퍼 ────────────────────────────────────────────────────

    /// <summary>
    /// commandId에 대한 정상 응답 대사를 SO에서 조회한다.
    /// SO에 대사가 없으면 fallback을 반환한다.
    /// </summary>
    protected string GetCorrectLine(string commandId, string fallback = "")
    {
        var so = GetManualDataSO();
        if (so == null) return fallback;
        var nuisanceType = context?.nuisanceType ?? ComplaintContext.NuisanceType.None;
        return so.GetCorrectLine(commandId, nuisanceType) ?? fallback;
    }

    /// <summary>
    /// commandId에 대한 WrongOrder 대사를 SO에서 조회한다.
    /// SO에 대사가 없으면 fallback을 반환한다.
    /// </summary>
    protected string GetWrongOrderLine(string commandId, string fallback = "")
    {
        var so = GetManualDataSO();
        if (so == null) return fallback;
        var nuisanceType = context?.nuisanceType ?? ComplaintContext.NuisanceType.None;
        return so.GetWrongOrderLine(commandId, nuisanceType) ?? fallback;
    }

    // ── 응답 생성 헬퍼 ────────────────────────────────────────────────────

    /// <summary>잘못된 순서/상태. customerMessage를 직접 지정할 때.</summary>
    protected ResponseResult WrongOrder(string customerMessage = "")
    {
        return ResponseResult.Create(
            isValid: false,
            isCompleted: false,
            customerMessage: customerMessage
        );
    }

    /// <summary>
    /// 잘못된 순서/상태. CommandDialogueSO에서 WrongOrder 대사를 조회할 때.
    /// SO에 대사가 없으면 fallback 사용.
    /// </summary>
    protected ResponseResult WrongOrderFromSO(string commandId, string fallback = "")
    {
        return WrongOrder(GetWrongOrderLine(commandId, fallback));
    }

    /// <summary>정상 응답. customerMessage를 직접 지정할 때.</summary>
    protected ResponseResult CorrectResponse(
        string customerMessage          = "",
        bool   completeNow              = false,
        bool   shouldSpawnIdCard        = false,
        bool   shouldOpenIdCardDetail   = false,
        bool   shouldOpenMonitor        = false,
        bool   shouldRefreshMonitorData = false)
    {
        if (completeNow) isCompleted = true;

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

    /// <summary>
    /// 정상 응답. CommandDialogueSO에서 Correct 대사를 조회할 때.
    /// SO에 대사가 없으면 fallback 사용.
    /// </summary>
    protected ResponseResult CorrectResponseFromSO(
        string commandId,
        string fallback                 = "",
        bool   completeNow              = false,
        bool   shouldSpawnIdCard        = false,
        bool   shouldOpenIdCardDetail   = false,
        bool   shouldOpenMonitor        = false,
        bool   shouldRefreshMonitorData = false)
    {
        return CorrectResponse(
            customerMessage: GetCorrectLine(commandId, fallback),
            completeNow: completeNow,
            shouldSpawnIdCard: shouldSpawnIdCard,
            shouldOpenIdCardDetail: shouldOpenIdCardDetail,
            shouldOpenMonitor: shouldOpenMonitor,
            shouldRefreshMonitorData: shouldRefreshMonitorData
        );
    }

    public virtual string GetManualTitle() => "기본 메뉴얼";
}
