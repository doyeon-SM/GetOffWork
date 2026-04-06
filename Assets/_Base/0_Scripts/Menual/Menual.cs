using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 메뉴얼의 기반 클래스.
///
/// 구조:
///   RequiredSteps (List)  : 이 메뉴얼이 요구하는 절차를 순서대로 정의.
///                           하위 클래스가 BuildSteps()에서 채운다.
///   ActionQueue   (Queue) : 민원인 입장 후 플레이어가 실제로 수행한 행동을 기록.
///                           Execute()가 유효한 결과를 반환할 때만 쌓인다.
///   commandList           : UI 버튼에 표시할 선택지 (변경 없음).
///
/// 평가는 Manual 자체에서 하지 않는다. ServiceDeskManager가 민원 종료 시
/// ManualEvaluator.Evaluate(RequiredSteps, ActionQueue)를 호출한다.
/// </summary>
public abstract class Manual
{
    // ── UI 버튼 목록 (기존 유지) ──────────────────────────────────────────
    protected List<QuestionData> commandList = new();
    public IReadOnlyList<QuestionData> CommandList => commandList;

    // ── 메뉴얼 절차 정의 (List) ───────────────────────────────────────────
    protected List<ManualStepEntry> requiredSteps = new();
    public IReadOnlyList<ManualStepEntry> RequiredSteps => requiredSteps;

    // ── 플레이어 행동 기록 (Queue) ────────────────────────────────────────
    private Queue<PlayerActionRecord> actionQueue = new();
    public IReadOnlyCollection<PlayerActionRecord> ActionQueue => actionQueue;

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

        BuildCommandList();
        BuildSteps();
    }

    /// <summary>UI 버튼 목록을 채운다 (기존과 동일한 역할)</summary>
    protected abstract void BuildCommandList();

    /// <summary>
    /// RequiredSteps를 순서대로 채운다.
    /// 각 항목에 OmissionPenalty / OrderPenalty / CompletionReward를 정의한다.
    /// </summary>
    protected abstract void BuildSteps();

    // ── 실행 ─────────────────────────────────────────────────────────────
    public abstract ResponseResult Execute(string commandId, string payload = null);

    /// <summary>
    /// 행동 결과가 유효(IsValid == true)할 때 Queue에 기록한다.
    /// Execute 구현 내부에서 호출한다.
    /// </summary>
    protected void RecordAction(string commandId)
    {
        float elapsed = Time.time - sessionStartTime;
        actionQueue.Enqueue(new PlayerActionRecord(commandId, elapsed));
    }

    // ── 헬퍼: 순서/조건 위반 ─────────────────────────────────────────────
    /// <summary>
    /// 선행 조건이 충족되지 않아 행동이 불가할 때 반환.
    /// Queue에 기록하지 않는다.
    /// </summary>
    protected ResponseResult WrongOrder(string playerMessage, string customerMessage = "")
    {
        return ResponseResult.Create(
            isValid: false,
            isCompleted: false,
            playerMessage: playerMessage,
            customerMessage: customerMessage
        );
    }

    // ── 헬퍼: 정상 응답 ──────────────────────────────────────────────────
    /// <summary>
    /// 행동이 정상 처리됐을 때 반환.
    /// Queue 기록은 각 Handle 메서드에서 RecordAction으로 직접 한다.
    /// </summary>
    protected ResponseResult CorrectResponse(
        string playerMessage,
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
            playerMessage: playerMessage,
            customerMessage: customerMessage,
            shouldSpawnIdCard: shouldSpawnIdCard,
            shouldOpenIdCardDetail: shouldOpenIdCardDetail,
            shouldOpenMonitor: shouldOpenMonitor,
            shouldRefreshMonitorData: shouldRefreshMonitorData
        );
    }

    public virtual string GetManualTitle() => "기본 메뉴얼";
}
