using System;
using System.Collections.Generic;
using UnityEngine;

public class ServiceDeskManager : MonoBehaviour
{
    private const string TAG       = "[Desk]";
    private const string TAG_QUEUE = "[대기열]";

    private enum DeskState { Idle, ServingCustomer }

    [Header("플레이어")]
    [SerializeField] private PlayerBase playerBase;

    [Header("오브젝트 관리 박스")]
    [SerializeField] private ObjectManagerBox objectManagerBox;

    [Header("다음 손님 대기 시간")]
    [SerializeField] private float minCustomerDelay = 2f;
    [SerializeField] private float maxCustomerDelay = 6f;

    [Header("현재 민원")]
    [SerializeField] private ComplaintContext currentComplaint;

    [Header("디버그")]
    [SerializeField] private bool showDebugLog = true;

    private readonly Queue<ComplaintContext> waitingQueue = new Queue<ComplaintContext>();

    private WorkDayManager _workDayManager;
    private Manual    currentManual;
    private bool      isWorking;
    private float     nextCustomerTimer;
    private DeskState deskState = DeskState.Idle;
    private int       spawnedCustomerCountToday;

    // ── perMessagePenalty 누적 (응대 1건당 evt에 합산용) ─────────────────────
    private int   _msgPenaltyPerformance;
    private int   _msgPenaltyStress;
    private int   _msgPenaltyKindness;
    private int   _msgPenaltyReliability;

    private int       _overrideMaxCustomer = -1; // -1이면 PlayerLevel * 3 기본값 사용
    // ── 공개 프로퍼티 ─────────────────────────────────────────────────────
    public ComplaintContext   CurrentComplaint   => currentComplaint;
    public Manual             CurrentManual      => currentManual;
    public bool               IsWorking          => isWorking;
    public bool               HasActiveCustomer  => currentComplaint != null && currentManual != null;
    public int                WaitingCount       => waitingQueue.Count;
    public UserRecordDatabase UserDatabase       => ServiceDataManager.Instance.UserDatabase;

    public int  MaxCustomerPerDay    => _overrideMaxCustomer > 0 ? _overrideMaxCustomer : (playerBase != null ? playerBase.PlayerLevel * 3 : 0);

    public bool HasReachedDailyLimit => spawnedCustomerCountToday >= MaxCustomerPerDay;
    /// <summary>LevelDesignManager에서 날짜별 최대 손님 수를 주입한다. 0 이하이면 기본값(PlayerLevel * 3)으로 되돌린다.</summary>
    public void SetMaxCustomerPerDay(int value) => _overrideMaxCustomer = value > 0 ? value : -1;

    // ── 이벤트 ───────────────────────────────────────────────────────────
    public event Action<int>              OnWaitingQueueChanged;
    public event Action<ComplaintContext> OnCustomerCalled;
    public event Action                   OnCustomerCleared;
    public event Action<bool>             OnWorkStateChanged;

    public event Action<string> OnPlayerText;
    public event Action<string> OnCustomerText;
    public event Action<string> OnCustomerOpening;

    public event Action<ComplaintContext> OnSpawnIdCardRequested;
    public event Action<ComplaintContext> OnSpawnProxyIdCardRequested;
    public event Action<ComplaintContext> OnPrintDocumentRequested;
    public event Action<ComplaintContext> OnOpenIdCardDetailRequested;
    public event Action<ComplaintContext> OnOpenMonitorRequested;
    public event Action<ComplaintContext> OnMonitorRefreshRequested;
    public event Action<ComplaintContext> OnPrintNewIdCardRequested;  // AddressChange: 새 ID카드 출력

    // ── 생명주기 ─────────────────────────────────────────────────────────
    private void Awake()
    {
        ResolvePlayerBase();
        if (ServiceDataManager.Instance != null)
            ServiceDataManager.Instance.UserDatabase.BuildCache();
    }

    private void Start()
    {
        ResolvePlayerBase();
        RaiseWaitingQueueChanged();
    }

    private void Update()
    {
        if (!isWorking) return;
        UpdateWaitingArrival();
        if (deskState == DeskState.ServingCustomer)
            UpdateCurrentCustomerPatience();
    }

    // ── 플레이어 참조 ─────────────────────────────────────────────────────
    public void SetPlayerBase(PlayerBase player) => playerBase = player;

    private void ResolvePlayerBase()
    {
        if (playerBase != null) return;
        playerBase = PlayerBase.Instance;
        if (playerBase == null)
            Debug.LogError(TAG + " PlayerBase Instance가 없습니다!");
    }

    // ── 주민 레코드 ───────────────────────────────────────────────────────
    public bool TryGetResidentRecord(string recordId, out UserRecordData record)
    {
        record = null;
        if (ServiceDataManager.Instance.UserDatabase == null || string.IsNullOrWhiteSpace(recordId)) return false;
        return ServiceDataManager.Instance.UserDatabase.TryGetRecord(recordId, out record);
    }

    public void BroadcastCustomerText(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            OnCustomerText?.Invoke(message);
    }

    // ── 근무 흐름 ─────────────────────────────────────────────────────────
    public void BeginWorkPhase()
    {
        ResolvePlayerBase();
        isWorking                 = true;
        spawnedCustomerCountToday = 0;
        waitingQueue.Clear();
        ClearCurrentCustomerInternal();
        deskState = DeskState.Idle;
        ResetMessagePenaltyAccumulators();
        ScheduleNextCustomerArrival();
        RaiseWaitingQueueChanged();
        OnWorkStateChanged?.Invoke(true);
        Log(TAG + " 근무 시작 / 일일 최대 인원: " + MaxCustomerPerDay);
    }

public void StopWorkPhase()
    {
        isWorking = false;
        waitingQueue.Clear();
        ClearCurrentCustomerInternal();
        deskState         = DeskState.Idle;
        nextCustomerTimer = 0f;
        RaiseWaitingQueueChanged();
        OnCustomerCleared?.Invoke();
        OnWorkStateChanged?.Invoke(false);
        ServiceDataManager.Instance?.UserDatabase?.ClearRuntimeRecords();
        Log(TAG + " 근무 종료");
    }

    // ── 손님 도착 스케줄 ──────────────────────────────────────────────────
    private void UpdateWaitingArrival()
    {
        if (HasReachedDailyLimit) return;
        nextCustomerTimer -= Time.deltaTime;
        if (nextCustomerTimer <= 0f)
        {
            EnqueueNextCustomer();
            if (!HasReachedDailyLimit) ScheduleNextCustomerArrival();
            else Log(TAG_QUEUE + " 일일 최대 인원 도달 — 손님 생성 중단");
        }
    }

    private void ScheduleNextCustomerArrival()
    {
        if (HasReachedDailyLimit) { nextCustomerTimer = 0f; return; }
        nextCustomerTimer = UnityEngine.Random.Range(minCustomerDelay, maxCustomerDelay);
        Log(TAG_QUEUE + " 다음 손님 대기: " + nextCustomerTimer.ToString("F1") + "초");
    }

    private void EnqueueNextCustomer()
    {
        if (HasReachedDailyLimit) return;
        var complaint = CreateRandomComplaint();
        complaint.ResetPatience();
        waitingQueue.Enqueue(complaint);
        spawnedCustomerCountToday++;
        RaiseWaitingQueueChanged();
        Log(TAG_QUEUE + " 추가 / 대기: " + waitingQueue.Count + " / 오늘: " + spawnedCustomerCountToday + "/" + MaxCustomerPerDay);
    }

    // ── 손님 호출 ─────────────────────────────────────────────────────────
    public void OnClickCallNextCustomer()
    {
        if (!isWorking) return;

        if (objectManagerBox != null)
        {
            bool ok = objectManagerBox.TryFinishAndReturn(out bool isRejection);
            if (!ok) return;
            if (HasActiveCustomer)
                FinishCurrentCustomer(isRejection: isRejection);
        }
        else
        {
            if (HasActiveCustomer)
                FinishCurrentCustomer();
        }

        CallNextCustomer();
    }

    public bool CallNextCustomer()
    {
        if (!isWorking)              return false;
        if (HasActiveCustomer)       { Log(TAG + " 이미 민원 처리 중"); return false; }
        if (waitingQueue.Count <= 0) { Log(TAG + " 대기열 비어있음");   return false; }

        var nextComplaint = waitingQueue.Dequeue();
        RaiseWaitingQueueChanged();

        var manual = CreateManualByComplaint(nextComplaint);
        if (manual == null) { Debug.LogWarning(TAG + " 매뉴얼 생성 실패"); return false; }

        currentComplaint = nextComplaint;
        currentManual    = manual;
        currentManual.Initialize(currentComplaint);
        deskState        = DeskState.ServingCustomer;

        Log(TAG + " 호출: " + currentManual.GetManualTitle() + " / " + currentComplaint.applicantType
            + " / NuisanceType:" + currentComplaint.nuisanceType);

        FireOpeningLine(currentComplaint);
        OnCustomerCalled?.Invoke(currentComplaint);
        return true;
    }

    private void FireOpeningLine(ComplaintContext complaint)
    {
        string line = ServiceDataManager.Instance.OpeningLineTable != null
            ? ServiceDataManager.Instance.OpeningLineTable.GetLine(
                complaint.complaintType,
                complaint.applicantType,
                complaint.nuisanceType)
            : GetFallbackOpeningLine(complaint);

        if (string.IsNullOrWhiteSpace(line)) return;
        Log(TAG + " 입장 대사: " + line);
        OnCustomerText?.Invoke(line);
        OnCustomerOpening?.Invoke(line);
    }

    private string GetFallbackOpeningLine(ComplaintContext complaint)
    {
        bool isSelf = complaint.applicantType == ComplaintContext.ApplicantType.Self;
        switch (complaint.complaintType)
        {
            case ComplaintContext.ComplaintType.FullID:
                return isSelf
                    ? "안녕하세요, 주민등록등본 발급하러 왔습니다."
                    : "안녕하세요, 가족 대리로 주민등록등본 받으러 왔어요.";
            default:
                return "안녕하세요, 민원 처리 부탁드립니다.";
        }
    }

    private Manual CreateManualByComplaint(ComplaintContext complaint)
    {
        var ud = ServiceDataManager.Instance.UserDatabase;
        // 대기열 추가 시 확률로 결정된 메뉴얼을 그대로 사용한다
        var manualData = complaint.assignedManualData;
        switch (complaint.complaintType)
        {
            case ComplaintContext.ComplaintType.FullID:
                if (complaint.applicantType == ComplaintContext.ApplicantType.Self)
                {
                    var m = new M_FullID_Self(ud);
                    m.manualData = manualData;
                    return m;
                }
                else
                {
                    var m = new M_FullID_Proxy(ud);
                    m.manualData = manualData;
                    return m;
                }
            case ComplaintContext.ComplaintType.AddressChange:
            {
                var m = new M_AddressChange(ud);
                m.manualData = manualData;
                return m;
            }
            case ComplaintContext.ComplaintType.NewID:
            {
                var sd = ServiceDataManager.Instance;
                var m  = new M_NewID(ud, sd.PortraitList, sd.AddressListSO, sd.FakeAddressListSO);
                m.manualData = manualData;
                return m;
            }
            default: return null;
        }
    }

    // ── 민원 컨텍스트 생성 ────────────────────────────────────────────────
    private ComplaintContext CreateRandomComplaint()
    {
        return ComplaintFactory.Create();
    }

    // ── 명령 실행 ─────────────────────────────────────────────────────────
    public void ExecuteCommand(string commandId, string payload = null)
    {
        if (!isWorking || deskState != DeskState.ServingCustomer) return;
        if (currentManual == null || currentComplaint == null)    return;

        if (commandId == ManualCommandIds.ReturnPrintedDoc)
        {
            currentManual.RecordReturnAction(commandId);
            Log(TAG + " Paper 반납 확인 커맨드 발행");
            return;
        }

        var result = currentManual.Execute(commandId, payload);

        if (commandId == ManualCommandIds.AskSubmitId && result.IsValid)
        {
            currentManual.Execute(ManualCommandIds.SpawnIdCard);
            currentComplaint.idCardSpawned = true;
        }

        if (commandId == ManualCommandIds.AskSubmitProxyId && result.IsValid)
        {
            currentManual.Execute(ManualCommandIds.SpawnProxyIdCard);
            currentComplaint.proxyIdCardSpawned = true;
        }

        if (commandId == ManualCommandIds.PrintDocument && result.IsValid)
        {
            OnPrintDocumentRequested?.Invoke(currentComplaint);
            Log(TAG + " OnPrintDocumentRequested 발행");
        }

        if (commandId == ManualCommandIds.PrintNewIdCard && result.IsValid)
        {
            currentManual.Execute(ManualCommandIds.SpawnNewIdCard);
            OnPrintNewIdCardRequested?.Invoke(currentComplaint);
            Log(TAG + " OnPrintNewIdCardRequested 발행");
        }

        DispatchUIResult(result);

        if (showDebugLog)
        {
            if (!string.IsNullOrWhiteSpace(result.PlayerMessage))
                Log(TAG + " Player: " + result.PlayerMessage);
            if (!string.IsNullOrWhiteSpace(result.CustomerMessage))
                Log(TAG + " Customer: " + result.CustomerMessage);
        }

        // 응대 종료는 CallDisplay(OnClickCallNextCustomer) 시에만 수행된다.
    }

    private void DispatchUIResult(ResponseResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.PlayerMessage))
            OnPlayerText?.Invoke(result.PlayerMessage);

        if (!string.IsNullOrWhiteSpace(result.CustomerMessage))
        {
            OnCustomerText?.Invoke(result.CustomerMessage);
            ApplyNuisancePerMessagePenalty();
        }

        if (result.ShouldSpawnIdCard)        OnSpawnIdCardRequested?.Invoke(currentComplaint);
        if (result.ShouldSpawnProxyIdCard)   OnSpawnProxyIdCardRequested?.Invoke(currentComplaint);
        if (result.ShouldOpenIdCardDetail)   OnOpenIdCardDetailRequested?.Invoke(currentComplaint);
        if (result.ShouldOpenMonitor)        OnOpenMonitorRequested?.Invoke(currentComplaint);
        if (result.ShouldRefreshMonitorData) OnMonitorRefreshRequested?.Invoke(currentComplaint);
    }

    private void ApplyNuisancePerMessagePenalty()
    {
        if (currentComplaint == null) return;
        if (currentComplaint.nuisanceType == ComplaintContext.NuisanceType.None) return;
        if (playerBase == null) return;

        var nuisanceSO = ServiceDataManager.Instance?.NuisanceSettings;
        if (nuisanceSO == null) return;

        var entry = nuisanceSO.GetEntry(currentComplaint.nuisanceType);
        if (entry.perMessagePenalty.IsEmpty) return;

        // 메시지당 진상 패널티는 응대 종료 시 이벤트에 누적되지 않으르로 직접 적용
        var p = entry.perMessagePenalty;
        if (p.stress      != 0) playerBase.AddStat(Stat.Stress,      p.stress);
        if (p.kindness    != 0) playerBase.AddStat(Stat.Kindness,    p.kindness);
        if (p.reliability != 0) playerBase.AddStat(Stat.Reliability, p.reliability);
        if (p.performance != 0) playerBase.AddPerformance(-p.performance);
        Log(TAG + " [NuisancePenalty/msg] type:" + currentComplaint.nuisanceType);
    }


    // ── 인내심 ────────────────────────────────────────────────────────────
    private void UpdateCurrentCustomerPatience()
    {
        if (currentComplaint == null) return;
        currentComplaint.currentPatience -= Time.deltaTime;
        if (currentComplaint.currentPatience <= 0f)
            HandlePatienceExpired();
    }

    private void HandlePatienceExpired()
    {
        Log(TAG + " 인내심 소진");
        FinishCurrentCustomer(patienceExpired: true);
    }

    private void FireClosingLine(ComplaintContext complaint)
    {
        var table = ServiceDataManager.Instance?.ClosingLineTable;
        if (table == null || complaint == null) return;
        string line = table.GetLine(complaint.complaintType, complaint.applicantType, complaint.nuisanceType);
        if (string.IsNullOrWhiteSpace(line)) return;
        Log(TAG + " [퇴장 대사] " + line);
        OnCustomerText?.Invoke(line);
    }

    // ── 민원 종료 & 정산 ──────────────────────────────────────────────────
    private void FinishCurrentCustomer(bool patienceExpired = false, bool isRejection = false)
    {
        if (currentManual == null || currentComplaint == null) return;
        if (playerBase == null) return;

        var evt = ServiceEvaluator.Evaluate(
            playerBase,
            currentComplaint,
            currentManual,
            patienceExpired,
            isRejection,
            out bool isSuccess);

        CommitAndClose(evt, isSuccess);
    }
    private void CommitAndClose(StatChangeEvent evt, bool isSuccess)
    {
        evt.source = isSuccess ? StatChangeSource.ServiceSuccess : StatChangeSource.ServiceFail;
        _workDayManager?.EnqueueStatChangeEvent(evt);

        FireClosingLine(currentComplaint);
        currentManual.ClearRequiredReturnItems();
        Log(TAG + " 민원 종료 — " + currentComplaint.complaintType + " / rejected=" + currentComplaint.rejected);
        currentManual = null;
        currentComplaint = null;
        deskState = DeskState.Idle;
        OnCustomerCleared?.Invoke();
    }

    private void ClearCurrentCustomerInternal()
    {
        currentComplaint = null;
        currentManual    = null;
    }


    private void ResetMessagePenaltyAccumulators()
    {
        _msgPenaltyPerformance  = 0;
        _msgPenaltyStress       = 0;
        _msgPenaltyKindness     = 0;
        _msgPenaltyReliability  = 0;
    }
    /// <summary>WorkDayManager 참조를 주입한다. WorkDayManager.Start()에서 호출된다.</summary>
    // ── 진상 퇴치 메뉴얼 ──────────────────────────────────────────────────
    /// <summary>
    /// 진상 퇴치 메뉴얼을 발동한다. UI(SOSManualGroup 버튼)에서 호출된다.
    /// SOS처럼 강제 종료가 필요한 메뉴얼은 먼저 현재 응대를 평가 없이 클리어한 뒤
    /// AntiNuisanceManual.Activate()를 호출한다.
    /// </summary>
    public void ExecuteAntiNuisanceManual(AntiNuisanceManual manual)
    {
        if (manual == null || playerBase == null) return;

        bool hadActiveCustomer = HasActiveCustomer;

        // 강제 종료가 필요한 메뉴얼(SOS 등): 평가 없이 즉시 응대 클리어
        if (hadActiveCustomer)
        {
            currentManual?.ClearRequiredReturnItems();
            Log(TAG + $" [진상퇴치:{manual.GetTitle()}] 현재 응대 강제 종료");
            currentManual    = null;
            currentComplaint = null;
            deskState        = DeskState.Idle;
            ResetMessagePenaltyAccumulators();
            OnCustomerCleared?.Invoke();
        }

        // 스탯 변화 적용 + StatChangeEvent 기록
        manual.Activate(playerBase, _workDayManager, hadActiveCustomer);
        Log(TAG + $" [진상퇴치:{manual.GetTitle()}] 발동 완료");
    }

    public void SetWorkDayManager(WorkDayManager wdm) => _workDayManager = wdm;

    private void RaiseWaitingQueueChanged() => OnWaitingQueueChanged?.Invoke(waitingQueue.Count);
    private void Log(string message) { if (showDebugLog) Debug.Log(message); }
}
