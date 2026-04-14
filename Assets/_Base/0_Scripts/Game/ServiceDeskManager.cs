using System;
using System.Collections.Generic;
using UnityEngine;

public class ServiceDeskManager : MonoBehaviour
{
    private const string TAG       = "[Desk]";
    private const string TAG_EVAL  = "[정산]";
    private const string TAG_QUEUE = "[대기열]";

    private const float DEFAULT_PATIENCE_MIN = 20f;
    private const float DEFAULT_PATIENCE_MAX = 40f;

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
    public UserRecordDatabase UserDatabase       => ServiceDataManager.Instance.UserDatabase;

    public int  MaxCustomerPerDay    => playerBase != null ? playerBase.PlayerLevel * 3 : 0;
    public bool HasReachedDailyLimit => spawnedCustomerCountToday >= MaxCustomerPerDay;

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
        switch (complaint.complaintType)
        {
            case ComplaintContext.ComplaintType.FullID:
                if (complaint.applicantType == ComplaintContext.ApplicantType.Self)
                {
                    var m = new M_FullID_Self(ud);
                    if (complaint.requestedDeliveryType == ComplaintContext.DeliveryType.Print)
                        m.manualData = ServiceDataManager.Instance.FullSelf_Print;
                    else if (complaint.requestedDeliveryType == ComplaintContext.DeliveryType.Mobile)
                        m.manualData = ServiceDataManager.Instance.FullSelf_Mobile;
                    return m;
                }
                else
                {
                    var m = new M_FullID_Proxy(ud);
                    if (complaint.requestedDeliveryType == ComplaintContext.DeliveryType.Print)
                        m.manualData = ServiceDataManager.Instance.FullProxy_Print;
                    else if (complaint.requestedDeliveryType == ComplaintContext.DeliveryType.Mobile)
                        m.manualData = ServiceDataManager.Instance.Fullproxy_Mobile;
                    return m;
                }
            default: return null;
        }
    }

    // ── 민원 컨텍스트 생성 ────────────────────────────────────────────────
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

        var nuisanceSO = ServiceDataManager.Instance?.NuisanceSettings;
        c.nuisanceType = nuisanceSO != null
            ? nuisanceSO.RollNuisanceType()
            : ComplaintContext.NuisanceType.None;

        var ub = ServiceDataManager.Instance.UserDatabase;
        if (ub != null && ub.Records != null && ub.Records.Count > 0)
        {
            int ai = UnityEngine.Random.Range(0, ub.Records.Count);
            c.applicantRecordId = ub.Records[ai].recordId;

            if (c.applicantType == ComplaintContext.ApplicantType.Self)
            {
                c.targetRecordId = c.applicantRecordId;
            }
            else
            {
                if (ub.Records.Count >= 2)
                {
                    int ti = UnityEngine.Random.Range(0, ub.Records.Count - 1);
                    if (ti >= ai) ti++;
                    c.targetRecordId = ub.Records[ti].recordId;
                }
                else
                {
                    c.applicantType  = ComplaintContext.ApplicantType.Self;
                    c.targetRecordId = c.applicantRecordId;
                    Debug.LogWarning(TAG_QUEUE + " 레코드 1개만 존재 — Proxy 불가, Self로 대체");
                }
            }

            // 종류별 불일치 판정 — 각각 독립적으로 확률 체크
            var mismatchSO     = ServiceDataManager.Instance?.MismatchSetting;
            float addrChance    = mismatchSO != null ? mismatchSO.AddressspawnChance  : 0f;
            float idChance      = mismatchSO != null ? mismatchSO.IDspawnChance       : 0f;
            float portraitChance= mismatchSO != null ? mismatchSO.PortraitspawnChance : 0f;

            ub.TryGetRecord(c.applicantRecordId, out UserRecordData aRec);
            ub.TryGetRecord(c.targetRecordId,    out UserRecordData tRec);

            // 주소 불일치: fakeAddress가 있는 레코드 + 확률 성공
            c.isAddressMismatch = UnityEngine.Random.value < addrChance
                && ((aRec != null && aRec.HasAddressMismatch)
                    || (tRec != null && tRec.HasAddressMismatch));

            // ID 불일치: fakeID가 있는 레코드 + 확률 성공
            c.isIdMismatch = UnityEngine.Random.value < idChance
                && ((aRec != null && aRec.HasIdMismatch)
                    || (tRec != null && tRec.HasIdMismatch));

            // 사진 불일치: fakePortrait가 있는 레코드 + 확률 성공
            c.isPortraitMismatch = UnityEngine.Random.value < portraitChance
                && ((aRec != null && aRec.HasPortraitMismatch)
                    || (tRec != null && tRec.HasPortraitMismatch));

            aRec.SetIdCard(c.isAddressMismatch, c.isIdMismatch, c.isPortraitMismatch);

            Log(TAG_QUEUE + $" 불일치: addr={c.isAddressMismatch} id={c.isIdMismatch} portrait={c.isPortraitMismatch}");
        }

        ManualDataSO patienceSO = GetManualDataSOByComplaint(c);
        float pMin = DEFAULT_PATIENCE_MIN;
        float pMax = DEFAULT_PATIENCE_MAX;
        if (patienceSO != null && patienceSO.HasPatienceOverride)
        {
            if (patienceSO.patienceMin > 0f) pMin = patienceSO.patienceMin;
            if (patienceSO.patienceMax > 0f) pMax = patienceSO.patienceMax;
            if (pMax < pMin) pMax = pMin;
        }

        float basePatience = UnityEngine.Random.Range(pMin, pMax);
        if (nuisanceSO != null && c.nuisanceType != ComplaintContext.NuisanceType.None)
        {
            var entry = nuisanceSO.GetEntry(c.nuisanceType);
            basePatience *= entry.patienceMultiplier;
            Log(TAG_QUEUE + " [" + c.nuisanceType + "] 진상 생성 / 인내심 배율: " + entry.patienceMultiplier);
        }
        c.maxPatience     = basePatience;
        c.currentPatience = basePatience;
        return c;
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

        ApplyNuisancePenalty(entry.perMessagePenalty);
        Log(TAG + " [NuisancePenalty/msg] type:" + currentComplaint.nuisanceType);
    }

    private void ApplyNuisancePenalty(NuisancePenalty penalty)
    {
        if (penalty.stress      != 0) playerBase.AddStat(Stat.Stress,      penalty.stress);
        if (penalty.kindness    != 0) playerBase.AddStat(Stat.Kindness,     penalty.kindness);
        if (penalty.reliability != 0) playerBase.AddStat(Stat.Reliability,  penalty.reliability);
        if (penalty.performance != 0) playerBase.AddPerformance(-penalty.performance);
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

        bool hasAnyMismatch     = currentComplaint.HasAnyMismatch;
        bool isCompleted        = currentManual.IsCompleted;
        bool isValidRejection   = isRejection && hasAnyMismatch;
        bool isInvalidRejection = isRejection && !hasAnyMismatch;
        bool isMissedRejection  = !isRejection && hasAnyMismatch && isCompleted;

        // ── 이 응대에서 발생할 스탯 변화를 누적할 임시 이벤트 ──────────────
        // source는 마지막에 결정되므로 일단 Fail로 초기화
        var pendingEvt = new StatChangeEvent { source = StatChangeSource.ServiceFail };
        bool isSuccess = false; // 응대 성공 여부 — 마지막에 확정

        // 인내심 소진 패널티
        if (patienceExpired)
        {
            playerBase.AddStat(Stat.Stress, 2);
            pendingEvt.stressDelta += 2;
            Log(TAG_EVAL + " 인내심 소진 → Stress+2");
        }

        // 진상 종료 패널티
        var nuisanceSO = ServiceDataManager.Instance?.NuisanceSettings;
        if (nuisanceSO != null && currentComplaint.nuisanceType != ComplaintContext.NuisanceType.None)
        {
            var nEntry = nuisanceSO.GetEntry(currentComplaint.nuisanceType);
            if (!nEntry.onFinishPenalty.IsEmpty)
            {
                ApplyNuisancePenalty(nEntry.onFinishPenalty);
                pendingEvt.stressDelta       += nEntry.onFinishPenalty.stress;
                pendingEvt.kindnessDelta     += nEntry.onFinishPenalty.kindness;
                pendingEvt.reliabilityDelta  += nEntry.onFinishPenalty.reliability;
                pendingEvt.performanceDelta  -= nEntry.onFinishPenalty.performance;
                Log(TAG_EVAL + " [NuisancePenalty/finish] type:" + currentComplaint.nuisanceType);
            }
        }

        // ── Case 1: 불일치 + 정상 반려 ───────────────────────────────────────────
        if (isValidRejection)
        {
            string mismatchLog = $"addr={currentComplaint.isAddressMismatch} "
                               + $"id={currentComplaint.isIdMismatch} "
                               + $"portrait={currentComplaint.isPortraitMismatch}";
            Log(TAG_EVAL + " 정상 반려(불일치) [" + mismatchLog + "] — 절차 평가 무시");

            var soData = GetCurrentManualData();
            if (soData != null && !soData.completionReward.IsEmpty)
            {
                ApplyReward(soData.completionReward);
                pendingEvt.performanceDelta += soData.completionReward.Performance;
                pendingEvt.kindnessDelta    += soData.completionReward.Kindness;
                pendingEvt.reliabilityDelta += soData.completionReward.Reliability;
                pendingEvt.payDelta         += soData.completionReward.Pay;
                Log(TAG_EVAL + " completionReward 적용");
            }
            else
            {
                int perfReward = currentComplaint.applicantType == ComplaintContext.ApplicantType.Self ? 3 : 6;
                playerBase.AddPerformance(perfReward);
                pendingEvt.performanceDelta += perfReward;
                Log(TAG_EVAL + " Performance+" + perfReward + " [폴백]");
            }
            playerBase.AddStat(Stat.Stress, 1);
            pendingEvt.stressDelta += 1;
            isSuccess = true;
            goto Cleanup;
        }

        // ── Case 2: 불일치인데 인쇄물 전달 또는 전자전송 완료 (isMissedRejection) ──
        if (isMissedRejection)
        {
            string mismatchLog = $"addr={currentComplaint.isAddressMismatch} "
                               + $"id={currentComplaint.isIdMismatch} "
                               + $"portrait={currentComplaint.isPortraitMismatch}";
            Log(TAG_EVAL + " 반려사항 놓침(불일치인데 정상응대 완료) [" + mismatchLog + "]");

            var soData = GetCurrentManualData();
            if (soData != null && !soData.abnormalRejectionPenalty.IsEmpty)
            {
                ApplyPenaltyFromSO(soData.abnormalRejectionPenalty);
                pendingEvt.performanceDelta -= soData.abnormalRejectionPenalty.Performance;
                pendingEvt.kindnessDelta    -= soData.abnormalRejectionPenalty.Kindness;
                pendingEvt.stressDelta      += soData.abnormalRejectionPenalty.Stress;
                pendingEvt.reliabilityDelta -= soData.abnormalRejectionPenalty.Reliability;
                pendingEvt.payDelta         -= soData.abnormalRejectionPenalty.Pay;
                Log(TAG_EVAL + " [반려사항 놓침] → abnormalRejectionPenalty 적용");
            }
            else
            {
                playerBase.AddPerformance(-2);
                playerBase.AddStat(Stat.Reliability, -1);
                pendingEvt.performanceDelta -= 2;
                pendingEvt.reliabilityDelta -= 1;
                Log(TAG_EVAL + " [반려사항 놓침] → Performance-2, Reliability-1 [폴백]");
            }
            goto Cleanup;
        }

        // ── Case 3: 정상 케이스 평가 (불일치 없음) ─────────────────────────────
        var eval = ManualEvaluator.Evaluate(
            currentManual.RequiredSteps,
            currentManual.ActionQueue,
            isAddressMismatch: false);
        Log(TAG_EVAL + " 평가 — " + eval);

        bool isAbnormal = isInvalidRejection
                       || (isCompleted && !eval.IsClean)
                       || (!isCompleted && !isRejection);

        if (isAbnormal)
        {
            var soData = GetCurrentManualData();
            string reason = isInvalidRejection ? "비정상 반려" : "정상 응대 실패";
            if (soData != null && !soData.abnormalRejectionPenalty.IsEmpty)
            {
                ApplyPenaltyFromSO(soData.abnormalRejectionPenalty);
                pendingEvt.performanceDelta -= soData.abnormalRejectionPenalty.Performance;
                pendingEvt.kindnessDelta    -= soData.abnormalRejectionPenalty.Kindness;
                pendingEvt.stressDelta      += soData.abnormalRejectionPenalty.Stress;
                pendingEvt.reliabilityDelta -= soData.abnormalRejectionPenalty.Reliability;
                pendingEvt.payDelta         -= soData.abnormalRejectionPenalty.Pay;
                Log(TAG_EVAL + " [" + reason + "] → abnormalRejectionPenalty 적용");
            }
            else
            {
                playerBase.AddPerformance(-2);
                playerBase.AddStat(Stat.Reliability, -1);
                pendingEvt.performanceDelta -= 2;
                pendingEvt.reliabilityDelta -= 1;
                Log(TAG_EVAL + " [" + reason + "] → Performance-2, Reliability-1 [폴백]");
            }
        }

        if (eval.PerformanceDelta != 0) { playerBase.AddPerformance(eval.PerformanceDelta); pendingEvt.performanceDelta += eval.PerformanceDelta; }
        if (eval.KindnessDelta    != 0) { playerBase.AddStat(Stat.Kindness,    eval.KindnessDelta);    pendingEvt.kindnessDelta    += eval.KindnessDelta; }
        if (eval.StressDelta      != 0) { playerBase.AddStat(Stat.Stress,      eval.StressDelta);      pendingEvt.stressDelta      += eval.StressDelta; }
        if (eval.ReliabilityDelta != 0) { playerBase.AddStat(Stat.Reliability, eval.ReliabilityDelta); pendingEvt.reliabilityDelta += eval.ReliabilityDelta; }
        if (eval.PayDelta         != 0) { playerBase.AddPay(eval.PayDelta);                            pendingEvt.payDelta         += eval.PayDelta; }

        if (isCompleted && eval.IsClean)
        {
            var soData = GetCurrentManualData();
            if (soData != null && !soData.completionReward.IsEmpty)
            {
                ApplyReward(soData.completionReward);
                pendingEvt.performanceDelta += soData.completionReward.Performance;
                pendingEvt.kindnessDelta    += soData.completionReward.Kindness;
                pendingEvt.reliabilityDelta += soData.completionReward.Reliability;
                pendingEvt.payDelta         += soData.completionReward.Pay;
                Log(TAG_EVAL + " 정상 응대 보상 → completionReward 적용");
            }
            isSuccess = true;
        }

        Cleanup:
        // ── 이벤트 출처 확정 및 큐 저장 ─────────────────────────────────────
        pendingEvt.source = isSuccess ? StatChangeSource.ServiceSuccess : StatChangeSource.ServiceFail;
        var wdm = FindFirstObjectByType<WorkDayManager>();
        wdm?.EnqueueStatChangeEvent(pendingEvt);

        FireClosingLine(currentComplaint);
        currentManual.ClearRequiredReturnItems();
        Log(TAG + " 민원 종료 — " + currentComplaint.complaintType + " / rejected=" + currentComplaint.rejected);
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

    // ── ManualDataSO 헬퍼 ────────────────────────────────────────────────

    /// <summary>현재 진행 중인 메뉴얼의 ManualDataSO를 반환한다.</summary>
    private ManualDataSO GetCurrentManualData()
    {
        if (currentManual is M_FullID_Self self)   return self.manualData;
        if (currentManual is M_FullID_Proxy proxy) return proxy.manualData;
        return null;
    }

    /// <summary>
    /// 아직 Manual이 생성되기 전(CreateRandomComplaint 단계)에
    /// ComplaintContext 정보만으로 해당 ManualDataSO를 조회한다.
    /// </summary>
    private ManualDataSO GetManualDataSOByComplaint(ComplaintContext c)
    {
        var sd = ServiceDataManager.Instance;
        if (sd == null) return null;

        bool isSelf   = c.applicantType == ComplaintContext.ApplicantType.Self;
        bool isPrint  = c.requestedDeliveryType == ComplaintContext.DeliveryType.Print;
        bool isMobile = c.requestedDeliveryType == ComplaintContext.DeliveryType.Mobile;

        if (isSelf   && isPrint)  return sd.FullSelf_Print;
        if (isSelf   && isMobile) return sd.FullSelf_Mobile;
        if (!isSelf  && isPrint)  return sd.FullProxy_Print;
        if (!isSelf  && isMobile) return sd.Fullproxy_Mobile;
        return null;
    }

    private void ApplyReward(StepReward reward)
    {
        if (reward.Performance != 0) playerBase.AddPerformance(reward.Performance);
        if (reward.Kindness    != 0) playerBase.AddStat(Stat.Kindness,    reward.Kindness);
        if (reward.Reliability != 0) playerBase.AddStat(Stat.Reliability, reward.Reliability);
        if (reward.Pay         != 0) playerBase.AddPay(reward.Pay);
    }

    private void ApplyPenaltyFromSO(StepPenalty penalty)
    {
        if (penalty.Performance != 0) playerBase.AddPerformance(-penalty.Performance);
        if (penalty.Kindness    != 0) playerBase.AddStat(Stat.Kindness,    -penalty.Kindness);
        if (penalty.Stress      != 0) playerBase.AddStat(Stat.Stress,       penalty.Stress);
        if (penalty.Reliability != 0) playerBase.AddStat(Stat.Reliability, -penalty.Reliability);
        if (penalty.Pay         != 0) playerBase.AddPay(-penalty.Pay);
    }

    private void RaiseWaitingQueueChanged() => OnWaitingQueueChanged?.Invoke(waitingQueue.Count);
    private void Log(string message) { if (showDebugLog) Debug.Log(message); }
}
