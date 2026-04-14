using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 메뉴얼의 기반 클래스.
/// </summary>
public abstract class Manual
{
    protected List<QuestionData>       commandList         = new();
    protected List<ManualStepEntry>    requiredSteps       = new();
    private   Queue<PlayerActionRecord> actionQueue        = new();
    protected List<DeskObjectType>     requiredReturnItems = new();

    protected ComplaintContext context;
    protected bool             isCompleted;
    private   float            sessionStartTime;

    public IReadOnlyList<QuestionData>          CommandList         => commandList;
    public IReadOnlyList<ManualStepEntry>        RequiredSteps       => requiredSteps;
    public IReadOnlyCollection<PlayerActionRecord> ActionQueue       => actionQueue;
    public IReadOnlyList<DeskObjectType>         RequiredReturnItems => requiredReturnItems;
    public bool                                  IsCompleted         => isCompleted;

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

    protected virtual void BuildCommandList()
    {
        if (ServiceDataManager.Instance != null &&
            ServiceDataManager.Instance.QuestionList?.QuestionList != null)
        {
            foreach (var q in ServiceDataManager.Instance.QuestionList.QuestionList)
                commandList.Add(q);
        }
        else
        {
            Debug.LogWarning("[메뉴얼] 질문 리스트 Null");
        }
    }

    protected abstract void BuildSteps();
    protected abstract void BuildReturnItems();

    protected virtual ManualDataSO GetManualDataSO() => null;

    // ── 반납 관리 ─────────────────────────────────────────────────────────
    public void AddRequiredReturnItem(DeskObjectType objectType)
    {
        if (!requiredReturnItems.Contains(objectType))
            requiredReturnItems.Add(objectType);
    }

    public void ClearRequiredReturnItems() => requiredReturnItems.Clear();

    // ── 실행 ─────────────────────────────────────────────────────────────
    public abstract ResponseResult Execute(string commandId, string payload = null);

    public void RecordReturnAction(string commandId) => RecordAction(commandId);

    protected void RecordAction(string commandId)
    {
        float elapsed = Time.time - sessionStartTime;
        actionQueue.Enqueue(new PlayerActionRecord(commandId, elapsed));
    }

    // ── 대사 조회 헬퍼 ────────────────────────────────────────────────────
    protected string GetCorrectLine(string commandId, string fallback = "")
    {
        var so = GetManualDataSO();
        if (so == null) return fallback;
        var nt = context?.nuisanceType ?? ComplaintContext.NuisanceType.None;
        return so.GetCorrectLine(commandId, nt) ?? fallback;
    }

    protected string GetWrongOrderLine(string commandId, string fallback = "")
    {
        var so = GetManualDataSO();
        if (so == null) return fallback;
        var nt = context?.nuisanceType ?? ComplaintContext.NuisanceType.None;
        return so.GetWrongOrderLine(commandId, nt) ?? fallback;
    }

    // ── 응답 생성 헬퍼 ────────────────────────────────────────────────────
    protected ResponseResult WrongOrder(string customerMessage = "")
    {
        return ResponseResult.Create(isValid: false, isCompleted: false, customerMessage: customerMessage);
    }

    protected ResponseResult WrongOrderFromSO(string commandId, string fallback = "")
    {
        return WrongOrder(GetWrongOrderLine(commandId, fallback));
    }

    protected ResponseResult CorrectResponse(
        string customerMessage          = "",
        bool   completeNow              = false,
        bool   shouldSpawnIdCard        = false,
        bool   shouldSpawnProxyIdCard   = false,
        bool   shouldOpenIdCardDetail   = false,
        bool   shouldOpenMonitor        = false,
        bool   shouldRefreshMonitorData = false)
    {
        if (completeNow) isCompleted = true;

        return ResponseResult.Create(
            isValid:                 true,
            isCompleted:             completeNow,
            customerMessage:         customerMessage,
            shouldSpawnIdCard:       shouldSpawnIdCard,
            shouldSpawnProxyIdCard:  shouldSpawnProxyIdCard,
            shouldOpenIdCardDetail:  shouldOpenIdCardDetail,
            shouldOpenMonitor:       shouldOpenMonitor,
            shouldRefreshMonitorData: shouldRefreshMonitorData
        );
    }

    protected ResponseResult CorrectResponseFromSO(
        string commandId,
        string fallback                 = "",
        bool   completeNow              = false,
        bool   shouldSpawnIdCard        = false,
        bool   shouldSpawnProxyIdCard   = false,
        bool   shouldOpenIdCardDetail   = false,
        bool   shouldOpenMonitor        = false,
        bool   shouldRefreshMonitorData = false)
    {
        return CorrectResponse(
            customerMessage:         GetCorrectLine(commandId, fallback),
            completeNow:             completeNow,
            shouldSpawnIdCard:       shouldSpawnIdCard,
            shouldSpawnProxyIdCard:  shouldSpawnProxyIdCard,
            shouldOpenIdCardDetail:  shouldOpenIdCardDetail,
            shouldOpenMonitor:       shouldOpenMonitor,
            shouldRefreshMonitorData: shouldRefreshMonitorData
        );
    }

    public virtual string GetManualTitle() => "기본 메뉴얼";

    // ── 플레이스홀더 치환 ─────────────────────────────────────────────────
    protected string ResolvePlaceholders(string line, Dictionary<string, string> values)
    {
        if (string.IsNullOrEmpty(line) || values == null) return line;
        foreach (var kv in values)
            line = line.Replace("{" + kv.Key + "}", kv.Value ?? string.Empty);
        return line;
    }
}
