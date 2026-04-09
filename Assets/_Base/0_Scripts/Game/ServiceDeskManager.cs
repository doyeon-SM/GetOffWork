using System;
using System.Collections.Generic;
using UnityEngine;

public class ServiceDeskManager : MonoBehaviour
{
    // ── 로그 태그 상수 ────────────────────────────────────────────────────
    // Unity 콘솔에서 이 태그로 필터링하면 민원 흐름만 볼 수 있다.
    // 예) 콘솔 검색창에 "[Desk]" 또는 "[정산]" 또는 "[Evaluator]" 입력
    private const string TAG      = "[Desk]";
    private const string TAG_EVAL = "[정산]";
    private const string TAG_QUEUE = "[대기열]";

    private enum DeskState { Idle, ServingCustomer }

    [Header("플레이어")]
    [SerializeField] private PlayerBase playerBase;

    [Header("주민 데이터")]
    [SerializeField] private UserRecordDatabase userDatabase;

        [Header("오브젝트 관리 박스")]
    [SerializeField] private ObjectManagerBox objectManagerBox;

    
[Header("입장 대사 데이터")]
    [SerializeField] private ComplaintOpeningLineTable openingLineTable;

    [Header("다음 손님 대기 시간")]
    [SerializeField] private float minCustomerDelay = 2f;
    [SerializeField] private float maxCustomerDelay = 6f;

    [Header("현재 민원")]
    [SerializeField] private ComplaintContext currentComplaint;

    [Header("디버그")]
    [SerializeField] private bool showDebugLog = true;

    private readonly Queue<ComplaintContext> waitingQueue = new Queue<ComplaintContext>();

    private Manual    currentManual;
    private bool      isWorking;
    private float     nextCustomerTimer;
    private DeskState deskState = DeskState.Idle;
    private int       spawnedCustomerCountToday;

    // ── 공개 프로퍼티 ─────────────────────────────────────────────────────
    public ComplaintContext   CurrentComplaint   => currentComplaint;
    public Manual             CurrentManual      => currentManual;
    public bool               IsWorking          => isWorking;
    public bool               HasActiveCustomer  => currentComplaint != null && currentManual != null;
    public int                WaitingCount       => waitingQueue.Count;
    public UserRecordDatabase UserDatabase       => userDatabase;

    public int  MaxCustomerPerDay    => playerBase != null ? playerBase.PlayerLevel * 3 : 0;
    public bool HasReachedDailyLimit => spawnedCustomerCountToday >= MaxCustomerPerDay;

    // ── 이벤트 ───────────────────────────────────────────────────────────
    public event Action<int>             OnWaitingQueueChanged;
    public event Action<ComplaintContext> OnCustomerCalled;
    public event Action                  OnCustomerCleared;
    public event Action<bool>            OnWorkStateChanged;

    public event Action<string> OnPlayerText;
    public event Action<string> OnCustomerText;

    /// <summary>민원인이 입장하며 첫 마디를 할 때 발생. string = 입장 대사</summary>
    public event Action<string> OnCustomerOpening;

    public event Action<ComplaintContext> OnSpawnIdCardRequested;
    public event Action<ComplaintContext> OnOpenIdCardDetailRequested;
    public event Action<ComplaintContext> OnOpenMonitorRequested;
    public event Action<ComplaintContext> OnMonitorRefreshRequested;

    // ── 생명주기 ─────────────────────────────────────────────────────────
    private void Awake()
    {
        ResolvePlayerBase();
        if (userDatabase != null) userDatabase.BuildCache();
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
            Debug.LogError($"{TAG} PlayerBase Instance가 없습니다!");
    }

    // ── 주민 레코드 ───────────────────────────────────────────────────────
    public bool TryGetResidentRecord(string recordId, out UserRecordData record)
    {
        record = null;
        if (userDatabase == null || string.IsNullOrWhiteSpace(recordId)) return false;
        return userDatabase.TryGetRecord(recordId, out record);
    }

/// <summary>
    /// 외부(ObjectManagerBox 등)에서 민원인 대사를 직접 방송할 때 사용한다.
    /// </summary>
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
        ScheduleNextCustomerArrival();
        RaiseWaitingQueueChanged();
        OnWorkStateChanged?.Invoke(true);
        Log($"{TAG} 근무 시작 / 일일 최대 인원: {MaxCustomerPerDay}");
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
        Log($"{TAG} 근무 종료");
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
            else Log($"{TAG_QUEUE} 일일 최대 인원 도달 — 손님 생성 중단");
        }
    }

    private void ScheduleNextCustomerArrival()
    {
        if (HasReachedDailyLimit) { nextCustomerTimer = 0f; return; }
        nextCustomerTimer = UnityEngine.Random.Range(minCustomerDelay, maxCustomerDelay);
        Log($"{TAG_QUEUE} 다음 손님 대기: {nextCustomerTimer:F1}초");
    }

    private void EnqueueNextCustomer()
    {
        if (HasReachedDailyLimit) return;
        var complaint = CreateRandomComplaint();
        complaint.ResetPatience();
        waitingQueue.Enqueue(complaint);
        spawnedCustomerCountToday++;
        RaiseWaitingQueueChanged();
        Log($"{TAG_QUEUE} 추가 / 대기: {waitingQueue.Count} / 오늘: {spawnedCustomerCountToday}/{MaxCustomerPerDay}");
    }

    // ── 손님 호출 ─────────────────────────────────────────────────────────
public void OnClickCallNextCustomer()
    {
        if (!isWorking) return;

        // ── 필수 반납 검사 + 반려 여부 판정 ───────────────────────────────
        if (objectManagerBox != null)
        {
            bool ok = objectManagerBox.TryFinishAndReturn(out bool isRejection);
            if (!ok) return; // 반납 미완료 → 호출 방어

            if (HasActiveCustomer)
            {
                FinishCurrentCustomer(isRejection: isRejection);
            }
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
        if (!isWorking)          return false;
        if (HasActiveCustomer)   { Log($"{TAG} 이미 민원 처리 중"); return false; }
        if (waitingQueue.Count <= 0) { Log($"{TAG} 대기열 비어있음"); return false; }

        var nextComplaint = waitingQueue.Dequeue();
        RaiseWaitingQueueChanged();

        var manual = CreateManualByComplaint(nextComplaint);
        if (manual == null) { Debug.LogWarning($"{TAG} 매뉴얼 생성 실패"); return false; }

        currentComplaint = nextComplaint;
        currentManual    = manual;
        currentManual.Initialize(currentComplaint);
        deskState        = DeskState.ServingCustomer;

        Log($"{TAG} 호출: {currentManual.GetManualTitle()} / {currentComplaint.applicantType}");

        // 입장 대사 발화 (OnCustomerCalled 이전에 발화해서 UI가 먼저 텍스트를 표시)
        FireOpeningLine(currentComplaint);

        OnCustomerCalled?.Invoke(currentComplaint);
        return true;
    }

    /// <summary>
    /// ComplaintOpeningLineTable에서 민원 유형과 신청인 유형에 맞는 대사를 골라
    /// OnCustomerText와 OnCustomerOpening 이벤트로 방송한다.
    /// </summary>
    private void FireOpeningLine(ComplaintContext complaint)
    {
        string line = openingLineTable != null
            ? openingLineTable.GetLine(complaint.complaintType, complaint.applicantType)
            : GetFallbackOpeningLine(complaint);

        if (string.IsNullOrWhiteSpace(line)) return;

        Log($"{TAG} 입장 대사: {line}");
        OnCustomerText?.Invoke(line);
        OnCustomerOpening?.Invoke(line);
    }

    /// <summary>테이블이 없을 때 사용하는 하드코딩 기본 대사</summary>
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
        switch (complaint.complaintType)
        {
            case ComplaintContext.ComplaintType.FullID: return new M_FullID(userDatabase);
            default: return null;
        }
    }

    private ComplaintContext CreateRandomComplaint()
    {
        var c = new ComplaintContext();
        c.complaintType         = ComplaintContext.ComplaintType.FullID;
        c.applicantType         = UnityEngine.Random.value > 0.5f
                                  ? ComplaintContext.ApplicantType.Self
                                  : ComplaintContext.ApplicantType.Proxy;
        c.requestedDeliveryType = UnityEngine.Random.value > 0.5f
                                  ? ComplaintContext.DeliveryType.Print
                                  : ComplaintContext.DeliveryType.Mobile;

        if (userDatabase != null && userDatabase.Records?.Count > 0)
        {
            int ai = UnityEngine.Random.Range(0, userDatabase.Records.Count);
            c.applicantRecordId = userDatabase.Records[ai].recordId;
            c.targetRecordId    = c.applicantType == ComplaintContext.ApplicantType.Self
                                  ? c.applicantRecordId
                                  : userDatabase.Records[UnityEngine.Random.Range(0, userDatabase.Records.Count)].recordId;
        }

        c.maxPatience     = UnityEngine.Random.Range(20f, 40f);
        c.currentPatience = c.maxPatience;
        return c;
    }

    // ── 명령 실행 ─────────────────────────────────────────────────────────
public void ExecuteCommand(string commandId, string payload = null)
    {
        if (!isWorking || deskState != DeskState.ServingCustomer) return;
        if (currentManual == null || currentComplaint == null)    return;

        var result = currentManual.Execute(commandId, payload);

        if (commandId == ManualCommandIds.AskSubmitId && result.IsValid)
        {
            currentManual.Execute(ManualCommandIds.SpawnIdCard);
            currentComplaint.idCardSpawned = true;
        }

        DispatchUIResult(result);

        if (showDebugLog)
        {
            if (!string.IsNullOrWhiteSpace(result.PlayerMessage))
                Log($"{TAG} Player: {result.PlayerMessage}");
            if (!string.IsNullOrWhiteSpace(result.CustomerMessage))
                Log($"{TAG} Customer: {result.CustomerMessage}");
        }

        // result.IsCompleted(completeNow=true)인 경우만 즉시 종료
        // M_FullID는 completeNow=false를 반환하므로 호출 버튼에서 종료
        if (result.IsCompleted)
            FinishCurrentCustomer();
    }

    private void DispatchUIResult(ResponseResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.PlayerMessage))    OnPlayerText?.Invoke(result.PlayerMessage);
        if (!string.IsNullOrWhiteSpace(result.CustomerMessage))  OnCustomerText?.Invoke(result.CustomerMessage);
        if (result.ShouldSpawnIdCard)        OnSpawnIdCardRequested?.Invoke(currentComplaint);
        if (result.ShouldOpenIdCardDetail)   OnOpenIdCardDetailRequested?.Invoke(currentComplaint);
        if (result.ShouldOpenMonitor)        OnOpenMonitorRequested?.Invoke(currentComplaint);
        if (result.ShouldRefreshMonitorData) OnMonitorRefreshRequested?.Invoke(currentComplaint);
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
        Log($"{TAG} 인내심 소진");
        FinishCurrentCustomer(patienceExpired: true);
    }

    // ── 민원 종료 & 정산 ──────────────────────────────────────────────────
private void FinishCurrentCustomer(bool patienceExpired = false, bool isRejection = false)
    {
        if (currentManual == null || currentComplaint == null) return;
        if (playerBase == null) return;

        bool isAddressMismatch = currentComplaint.isAddressMismatch;
        bool isCompleted       = currentManual.IsCompleted;

        // ── 반려 결과 유형 판정 ────────────────────────────────────────────────
        //
        // 정상 반려   : isRejection==true  && isAddressMismatch==true
        // 비정상 반려 : isRejection==true  && isAddressMismatch==false
        //                (주소일치인데 반려 / 인쇄 완료 후 반려 시도)
        // 반려사항 놓침 : isRejection==false && isAddressMismatch==true  && isCompleted==true
        //                (주소불일치인데 그냥 인쇄/발송 완료)

        bool isValidRejection   = isRejection && isAddressMismatch;    // 정상 반려
        bool isInvalidRejection = isRejection && !isAddressMismatch;   // 비정상 반려
        bool isMissedRejection  = !isRejection && isAddressMismatch && isCompleted; // 반려사항 놓침

        // ── 인내심 소진 패널티 ────────────────────────────────────────────────
        if (patienceExpired)
        {
            playerBase.AddStat(Stat.Stress, 2);
            Log($"{TAG_EVAL} 인내심 소진 → Stress+2");
        }

        // ── 정상 반려 ──────────────────────────────────────────────────────
        if (isValidRejection)
        {
            int perfReward = currentComplaint.applicantType == ComplaintContext.ApplicantType.Self ? 3 : 6;
            playerBase.AddPerformance(perfReward);
            playerBase.AddStat(Stat.Stress, 1);
            Log($"{TAG_EVAL} 정상 반려(주소불일치) → Performance+{perfReward}, Stress+1");
        }

        // ── 비정상 반려 ────────────────────────────────────────────────────
        if (isInvalidRejection)
        {
            playerBase.AddPerformance(-2);
            playerBase.AddStat(Stat.Reliability, -1);
            Log($"{TAG_EVAL} 비정상 반려(주소 일치) → Performance-2, Reliability-1");
        }

        // ── 반려사항 놓침 ───────────────────────────────────────────────────
        if (isMissedRejection)
        {
            playerBase.AddPerformance(-2);
            playerBase.AddStat(Stat.Reliability, -1);
            Log($"{TAG_EVAL} 반려사항 놓침(주소불일치인데 인쇄/발송 완료) → Performance-2, Reliability-1");
        }

        // ── 메뉴얼 절차 평가 (ManualEvaluator) ─────────────────────────────
        // isAddressMismatch==true: AskPrintOrMobile/PrintDocument/SendMobile 패널티 자동 제외
        var eval = ManualEvaluator.Evaluate(
            currentManual.RequiredSteps,
            currentManual.ActionQueue,
            isAddressMismatch: isAddressMismatch
        );

        Log($"{TAG_EVAL} 평가 — Perf:{eval.PerformanceDelta} Kind:{eval.KindnessDelta} Stress:{eval.StressDelta} Rel:{eval.ReliabilityDelta}");

        if (eval.PerformanceDelta != 0) playerBase.AddPerformance(eval.PerformanceDelta);
        if (eval.KindnessDelta    != 0) playerBase.AddStat(Stat.Kindness,     eval.KindnessDelta);
        if (eval.StressDelta      != 0) playerBase.AddStat(Stat.Stress,       eval.StressDelta);
        if (eval.ReliabilityDelta != 0) playerBase.AddStat(Stat.Reliability,  eval.ReliabilityDelta);
        if (eval.PayDelta         != 0) playerBase.AddPay(eval.PayDelta);

        // ── 필수 반납 목록 초기화 + 클린업 ──────────────────────────────────
        currentManual.ClearRequiredReturnItems();

        Log($"{TAG} 민원 종료 — {currentComplaint.complaintType} / rejected={currentComplaint.rejected}");
        currentManual    = null;
        currentComplaint = null;
        deskState        = DeskState.Idle;
        OnCustomerCleared?.Invoke();
    }

    private void ClearCurrentCustomerInternal()
    {
        currentComplaint = null;
        currentManual    = null;
    }

    private void RaiseWaitingQueueChanged() =>
        OnWaitingQueueChanged?.Invoke(waitingQueue.Count);

    // ── 로그 헬퍼 ─────────────────────────────────────────────────────────
    private void Log(string message)
    {
        if (showDebugLog) Debug.Log(message);
    }
}
