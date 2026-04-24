using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WorkDayManager : MonoBehaviour
{
    public static WorkDayManager Instance;
    public enum DayPhase
    {
        MorningWork,
        LunchBreak,
        AfternoonWork,
        Finish
    }

    [Header("플레이어 참조")]
    [SerializeField] private PlayerBase playerBase;

    [Header("데스크 시스템")]
    [SerializeField] private ServiceDeskManager serviceDeskManager;

    [Header("시간 설정 (초)")]
    [SerializeField] private float morningDuration   = 300f;
    [SerializeField] private float afternoonDuration = 300f;

    [Header("시간 UI")]
    [SerializeField] private UIClockTimer clockTimer;

    [Header("점심 UI 부모")]
    [SerializeField] private Transform lunchUIPanelRoot;

    [Header("점심 UI 프리팹")]
    [SerializeField] private UILunchChoice lunchChoiceUIPrefab;
    [SerializeField] private UILunchResult lunchResultUIPrefab;

    [Header("점심 옵션 목록")]
    [SerializeField] private List<LunchOptionData> lunchOptionList = new List<LunchOptionData>();

    [Header("영업 종료 시 대기 민원인 패널티")]
    [Tooltip("대기 민원인 1명 취소 시 효과음")]
    [SerializeField] private AudioClip customerCancelSfx;
    [Tooltip("한 명당 패널티 (성과, 스트레스, 친절도 단위: 정수%)")]
    [SerializeField] private int cancelPenaltyPerformance = -3;
    [SerializeField] private int cancelPenaltyStress      =  3;
    [SerializeField] private int cancelPenaltyKindness    = -3;
    [Tooltip("대기 민원인 취소 간격 (초)")]
    [SerializeField] private float customerCancelInterval = 0.5f;

        [Header("하루 정산 UI (프리팹 or 씬에 배치)")]
    [Tooltip("null이면 씬에서 UIDayResultView를 FindFirstObjectByType으로 탐색")]
    [SerializeField] private UIDayResultView dayResultViewPrefab;
    [SerializeField] private Transform       dayResultPanelRoot;

    // ── 내부 상태 ──────────────────────────────────────────────────────────
    private DayPhase        currentPhase;
    private float           phaseTimer;
    private bool            isPausedByUI;
    private bool            isFinished;
    private bool            lunchChoiceCompleted;
    private Coroutine       _dismissCoroutine;   // 대기 민원인 순차 처리 코루틴
    private System.Action   _pendingPhaseAction; // 대기 처리 완료 후 실행할 페이즈 전환

    private UILunchChoice   currentLunchChoiceUI;
    private UILunchResult   currentLunchResultUI;
    private LunchOptionData selectedLunchOption;

    // 하루 시작 스냅샷 (정산용)
    private DayResultData _dayResultData;

    public PlayerBase CurrentPlayerBase => playerBase;
    public DayPhase   CurrentPhase         => currentPhase;
    public float      CurrentPhaseTimer    => phaseTimer;
    /// <summary>현재 페이즈의 최대 시간(초). UIClockTimer가 비율 계산에 사용.</summary>
    public float      CurrentPhaseDuration => currentPhase == DayPhase.MorningWork ? morningDuration : afternoonDuration;
    // event
    public event Action<int, int, int, int> OnUIPlayerStatUpdate; // (performanceDelta, stressDelta%, kindnessDelta%, reliabilityDelta%)
    
    // ── Unity 생명주기 ─────────────────────────────────────────────────────
    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ResolvePlayerBase();
    }

private void Start()
    {
        ResolvePlayerBase();

        if (serviceDeskManager != null)
        {
            if (playerBase != null)
                serviceDeskManager.SetPlayerBase(playerBase);
            serviceDeskManager.SetWorkDayManager(this);
        }

        HideLunchUIObjects();
        isPausedByUI = true;   // 신문/대화가 끝날 때까지 시계 정지
        StartMorningWork();
        UpdateClockUI();
    }

    private void Update()
    {
        if (isFinished) return;

        if (isPausedByUI)
        {
            UpdateClockUI();
            return;
        }

        if (currentPhase == DayPhase.MorningWork || currentPhase == DayPhase.AfternoonWork)
        {
            phaseTimer -= Time.deltaTime;
            if (phaseTimer < 0f) phaseTimer = 0f;
            UpdateClockUI();
            if (phaseTimer <= 0f) AdvancePhase();
        }
        else
        {
            UpdateClockUI();
        }
    }

    // ── 플레이어 연결 ──────────────────────────────────────────────────────
    public void SetPlayerBase(PlayerBase player)
    {
        playerBase = player;
        if (serviceDeskManager != null)
            serviceDeskManager.SetPlayerBase(playerBase);
    }

    /// <summary>LevelDesignManager에서 날짜별 오전/오후 시간을 주입한다. (단위: 초)</summary>
    public void SetDurations(float morning, float afternoon)
    {
        morningDuration   = morning   >= 0f ? morning   : morningDuration;
        afternoonDuration = afternoon >= 0f ? afternoon : afternoonDuration;
    }

    /// <summary>신문/대화 등 외부 UI가 시계를 일시정지할 때 호출.</summary>
    public void PauseTimer()
    {
        isPausedByUI = true;
    }

    /// <summary>신문/대화 등 외부 UI가 닫힌 뒤 시계를 재개할 때 호출.</summary>
    public void ResumeTimer()
    {
        isPausedByUI = false;
    }

    /// <summary>
    /// 튜토리얼 종료 시 호출. phaseTimer를 morningDuration으로 초기화하고
    /// 타이머를 재개해 1일차 오전 업무를 본격 시작한다.
    /// </summary>
    public void ResetAndResumeDay()
    {
        phaseTimer   = morningDuration;
        isPausedByUI = false;
        UpdateClockUI();
        Debug.Log("[WorkDayManager] 튜토리얼 종료 → 1일차 오전 업무 본격 시작 / phaseTimer=" + phaseTimer);
    }

    private void ResolvePlayerBase()
    {
        if (playerBase != null) return;
        playerBase = PlayerBase.Instance;
        if (playerBase == null)
            Debug.LogError("[WorkDayManager] PlayerBase Instance가 없습니다!");
    }

    // ── 페이즈 진행 ────────────────────────────────────────────────────────
    private void StartMorningWork()
    {
        // 레벨 디자인 설정 주입 (morningDuration 등을 덮어쓰므로 phaseTimer 설정 이전에 호출)
        LevelDesignManager.Instance?.ApplyLevelForCurrentDay();

        currentPhase         = DayPhase.MorningWork;
        phaseTimer           = morningDuration;
        // 튜토리얼 중이면 isPausedByUI를 건드리지 않음 (PauseTimer 호출됨)
        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsActive)
            isPausedByUI = false;
        lunchChoiceCompleted = false;
        selectedLunchOption  = null;

        // 하루 시작 스냅샷 저장
        SnapshotDayStart();

        // 주소 큐 초기화 (매 하루 시작마다 리셋)
        ComplaintFactory.InitializeAddressQueue();

        if (serviceDeskManager != null)
            serviceDeskManager.BeginWorkPhase();

        Debug.Log("[WorkDayManager] 오전 업무 시작");
        UpdateClockUI();
    }

    private void StartLunchBreak()
    {
        currentPhase = DayPhase.LunchBreak;
        phaseTimer   = 0f;
        isPausedByUI = true;

        if (serviceDeskManager != null)
            serviceDeskManager.StopWorkPhase();

        OpenLunchChoiceUI();
        Debug.Log("[WorkDayManager] 점심시간 시작");
        UpdateClockUI();
    }

    private void StartAfternoonWork()
    {
        currentPhase = DayPhase.AfternoonWork;
        phaseTimer   = afternoonDuration;
        isPausedByUI = false;

        if (serviceDeskManager != null)
            serviceDeskManager.BeginWorkPhase();

        Debug.Log("[WorkDayManager] 오후 업무 시작");
        UpdateClockUI();
    }

    private void StartFinish()
    {
        currentPhase = DayPhase.Finish;
        phaseTimer   = 0f;
        isPausedByUI = true;

        if (serviceDeskManager != null)
            serviceDeskManager.StopWorkPhase();

        UpdateClockUI();
        FinishDay();
    }

    private void AdvancePhase()
    {
        switch (currentPhase)
        {
            case DayPhase.MorningWork:
                BeginDismissWaitingThen(StartLunchBreak);
                break;
            case DayPhase.AfternoonWork:
                BeginDismissWaitingThen(StartFinish);
                break;
            case DayPhase.LunchBreak:
                Debug.LogWarning("[WorkDayManager] 점심시간은 수동으로 닫혀야 해서 AdvancePhase가 작동하지 않습니다.");
                break;
        }
    }

    // ── 영업 종료 대기 민원인 처리 ────────────────────────────────────────

    /// <summary>
    /// 대기 민원인이 있으면 순차적으로 취소 처리 후 <paramref name="onComplete"/>를 실행한다.
    /// 없으면 즉시 <paramref name="onComplete"/>를 실행한다.
    /// </summary>
    private void BeginDismissWaitingThen(System.Action onComplete)
    {
        // 타이머 정지 (대기 처리 중 다시 AdvancePhase가 호출되지 않도록)
        isPausedByUI = true;

        // ServiceDeskManager의 새 손님 스케줄 중단 + 현재 응대 중 손님 마무리
        if (serviceDeskManager != null)
            serviceDeskManager.StopNewArrivalsOnly();

        int waitingCount = serviceDeskManager != null ? serviceDeskManager.WaitingCount : 0;

        if (waitingCount <= 0)
        {
            // 대기열 없음 → 즉시 다음 페이즈
            Debug.Log("[WorkDayManager] 대기열 없음 → 즉시 페이즈 전환");
            onComplete?.Invoke();
            return;
        }

        Debug.Log($"[WorkDayManager] 대기 민원인 {waitingCount}명 취소 처리 시작");
        _pendingPhaseAction = onComplete;
        if (_dismissCoroutine != null) StopCoroutine(_dismissCoroutine);
        _dismissCoroutine = StartCoroutine(DismissWaitingCustomers());
    }

    /// <summary>
    /// 대기열의 민원인을 0.5초 텀으로 한 명씩 취소 처리한다.
    /// 각 취소마다 패널티(성과-3, 스트레스+3, 친절도-3)를 적용하고
    /// 효과음과 UI 이벤트를 발행한다.
    /// </summary>
    private IEnumerator DismissWaitingCustomers()
    {
        while (serviceDeskManager != null && serviceDeskManager.WaitingCount > 0)
        {
            // 1) 효과음 재생
            if (customerCancelSfx != null)
                SoundSettingsManager.Instance?.PlaySfxOneShot(customerCancelSfx);

            // 2) 대기열에서 1명 제거
            serviceDeskManager.DismissOneWaiting();

            // 3) 패널티 적용 (스탯에 직접 반영)
            ResolvePlayerBase();
            if (playerBase != null)
            {
                if (cancelPenaltyPerformance != 0) playerBase.AddPerformance(cancelPenaltyPerformance);
                if (cancelPenaltyStress      != 0) playerBase.AddStat(Stat.Stress,   cancelPenaltyStress);
                if (cancelPenaltyKindness    != 0) playerBase.AddStat(Stat.Kindness, cancelPenaltyKindness);
            }

            // 4) StatChangeEvent 기록 (정산 UI에 표시)
            var evt = new StatChangeEvent
            {
                source           = StatChangeSource.CustomerCancelledAtClose,
                performanceDelta = cancelPenaltyPerformance,
                stressDelta      = cancelPenaltyStress,   // int % 단위
                kindnessDelta    = cancelPenaltyKindness, // int % 단위
                reliabilityDelta = 0,
            };
            EnqueueStatChangeEvent(evt);

            Debug.Log($"[WorkDayManager] 대기 민원인 취소 패널티 적용 / 남은 대기: {serviceDeskManager.WaitingCount}");

            // 5) 0.5초 대기 후 다음 명
            yield return new WaitForSeconds(customerCancelInterval);
        }

        Debug.Log("[WorkDayManager] 모든 대기 민원인 처리 완료 → 페이즈 전환");
        _dismissCoroutine = null;

        // 6) 대기열이 모두 비워진 후 다음 페이즈로 전환
        _pendingPhaseAction?.Invoke();
        _pendingPhaseAction = null;
    }

    // ── 점심 UI ────────────────────────────────────────────────────────────
    private void OpenLunchChoiceUI()
    {
        HideLunchUIObjects();
        if (lunchChoiceUIPrefab == null)
        {
            Debug.LogWarning("[WorkDayManager] lunchChoiceUIPrefab이 없습니다.");
            return;
        }
        Transform parent = lunchUIPanelRoot != null ? lunchUIPanelRoot : transform;
        currentLunchChoiceUI = Instantiate(lunchChoiceUIPrefab, parent);
        List<LunchOptionData> randomOptions = GetRandomLunchOptions(3);
        currentLunchChoiceUI.Initialize(this, randomOptions);
        Debug.Log("[WorkDayManager] 점심 선택 UI 생성");
    }

    private void OpenLunchResultUI(LunchOptionData selectedOption)
    {
        if (currentLunchChoiceUI != null)
        {
            Destroy(currentLunchChoiceUI.gameObject);
            currentLunchChoiceUI = null;
        }
        if (lunchResultUIPrefab == null)
        {
            Debug.LogWarning("[WorkDayManager] lunchResultUIPrefab이 없습니다.");
            return;
        }
        Transform parent = lunchUIPanelRoot != null ? lunchUIPanelRoot : transform;
        currentLunchResultUI = Instantiate(lunchResultUIPrefab, parent);
        currentLunchResultUI.Initialize(this, selectedOption);
        Debug.Log("[WorkDayManager] 점심 결과 UI 생성");
    }

    private void HideLunchUIObjects()
    {
        if (currentLunchChoiceUI != null) { Destroy(currentLunchChoiceUI.gameObject); currentLunchChoiceUI = null; }
        if (currentLunchResultUI != null) { Destroy(currentLunchResultUI.gameObject); currentLunchResultUI = null; }
    }

    private List<LunchOptionData> GetRandomLunchOptions(int count)
    {
        var validOptions = new List<LunchOptionData>();
        var result       = new List<LunchOptionData>();
        for (int i = 0; i < lunchOptionList.Count; i++)
            if (lunchOptionList[i] != null) validOptions.Add(lunchOptionList[i]);

        int pickCount = Mathf.Min(count, validOptions.Count);
        for (int i = 0; i < pickCount; i++)
        {
            int idx = UnityEngine.Random.Range(0, validOptions.Count);
            result.Add(validOptions[idx]);
            validOptions.RemoveAt(idx);
        }
        return result;
    }

    public void OnLunchOptionSelected(LunchOptionData optionData)
    {
        if (currentPhase != DayPhase.LunchBreak) { Debug.LogWarning("[WorkDayManager] 점심시간이 아닙니다.");      return; }
        if (lunchChoiceCompleted)                 { Debug.LogWarning("[WorkDayManager] 이미 점심을 선택했습니다."); return; }
        if (optionData == null)                   { Debug.LogWarning("[WorkDayManager] optionData가 null입니다."); return; }

        ResolvePlayerBase();
        selectedLunchOption  = optionData;
        lunchChoiceCompleted = true;
        ApplyLunchStatChanges(optionData);
        OpenLunchResultUI(optionData);
    }

    private void ApplyLunchStatChanges(LunchOptionData optionData)
    {
        if (playerBase == null || optionData == null || optionData.statChanges == null) return;

        var evt = new StatChangeEvent { source = StatChangeSource.Lunch };

        for (int i = 0; i < optionData.statChanges.Count; i++)
        {
            var change = optionData.statChanges[i];
            if (change == null) continue;
            playerBase.AddStat(change.stat, change.amount);
            Debug.Log($"[Lunch] {change.stat}: {change.amount}");

            switch (change.stat)
            {
                case Stat.Stress:      evt.stressDelta      += change.amount; break;
                case Stat.Kindness:    evt.kindnessDelta    += change.amount; break;
                case Stat.Reliability: evt.reliabilityDelta += change.amount; break;
            }
        }

        _dayResultData?.statChangeProgress.Enqueue(evt);
        Debug.Log($"[WorkDayManager] Lunch StatChangeEvent enqueued");
    }

    public void CloseLunchResultUIAndStartAfternoon()
    {
        if (currentPhase != DayPhase.LunchBreak)
        {
            Debug.LogWarning("[WorkDayManager] 현재 점심시간이 아닙니다.");
            return;
        }
        if (currentLunchResultUI != null) { Destroy(currentLunchResultUI.gameObject); currentLunchResultUI = null; }
        Debug.Log("[WorkDayManager] 점심 결과 UI 닫기 -> 오후 시작");
        StartAfternoonWork();
    }

    // ── 하루 정산 ──────────────────────────────────────────────────────────

    // ── 하루 과정 큐 API ────────────────────────────────────────────────

    /// <summary>
    /// 외부(ServiceDeskManager 등)에서 스탯 변화 이벤트를 큐에 추가한다.
    /// </summary>
    /// <summary>
    /// 스탯 변화량을 UI 이벤트로만 발행한다. StatChangeEvent 큐에는 추가하지 않는다.
    /// perMessagePenalty 등 응대 중 즉시 발생하는 스탯 변화를 UI에 반영할 때 사용한다.
    /// </summary>
    public void NotifyStatChangedUI(int performanceDelta, int stressDelta, int kindnessDelta, int reliabilityDelta)
    {
        OnUIPlayerStatUpdate?.Invoke(performanceDelta, stressDelta, kindnessDelta, reliabilityDelta);
    }

    public void EnqueueStatChangeEvent(StatChangeEvent evt)
    {
        if (_dayResultData == null) return;
        _dayResultData.statChangeProgress.Enqueue(evt);
        //ui event invoke
        // StatChangeEvent의 스탯 delta는 0~1 정규화 단위 → UIPlayerStat은 % 단위를 기대하므로 *100 변환
        OnUIPlayerStatUpdate?.Invoke(evt.performanceDelta, evt.stressDelta, evt.kindnessDelta, evt.reliabilityDelta);
        Debug.Log($"[WorkDayManager] StatChangeEvent enqueued — source:{evt.source} "
                + $"P:{evt.performanceDelta} S:{evt.stressDelta:F2} K:{evt.kindnessDelta:F2} "
                + $"R:{evt.reliabilityDelta:F2} Pay:{evt.payDelta}");
    }

    // ── 하루 정산 ──────────────────────────────────────────────────────────

        private void SnapshotDayStart()
    {
        ResolvePlayerBase();
        _dayResultData = new DayResultData();

        if (playerBase != null)
        {
            _dayResultData.startPerformance = playerBase.Performance;
            _dayResultData.startStress      = playerBase.Stress;
            _dayResultData.startKindness    = playerBase.Kindness;
            _dayResultData.startReliability = playerBase.Reliability;
            _dayResultData.startPay         = playerBase.Pay;
            _dayResultData.maxPerformance   = playerBase.GetMaxPerformance();
            _dayResultData.goalPerformance  = playerBase.GoalPerformance;
        }

        Debug.Log($"[WorkDayManager] 하루 시작 스냅샷 - P:{_dayResultData.startPerformance} K:{_dayResultData.startKindness:F2} S:{_dayResultData.startStress:F2} R:{_dayResultData.startReliability:F2}");
    }

    /// <summary>FinishDay 직전에 하루 종료 스탯을 기록한다.</summary>
    private void SnapshotDayEnd()
    {
        if (_dayResultData == null || playerBase == null) return;
        _dayResultData.endPerformance = playerBase.Performance;
        _dayResultData.endStress      = playerBase.Stress;
        _dayResultData.endKindness    = playerBase.Kindness;
        _dayResultData.endReliability = playerBase.Reliability;
        _dayResultData.endPay         = playerBase.Pay;
    }

    private void FinishDay()
    {
        isFinished = true;
        ResolvePlayerBase();

        // 종료 스냅샷
        SnapshotDayEnd();

        // 성과 목표 확인
        bool success = playerBase == null || playerBase.CheckPerformanceGoal();

        if (success)
        {
            // 성과 달성 → 승진 여부 확인 후 EndingScene 또는 다음날로 분기
            bool promoted = playerBase != null && playerBase.CheckPromotion();

            if (promoted)
            {
                // 승진 → 정산 UI → 확인 → EndingScene(해피엔딩)
                // Continue 이후 다음날 진행을 위해 현재 상태를 저장한다.
                GameFlowManager.Instance?.SavePlayerState(playerBase);
                OpenDayResultUI(() =>
                {
                    Debug.Log("[WorkDayManager] 승진 달성 → EndingScene(NormalEnding)으로 이동");
                    GameFlowManager.Instance?.TriggerGameOver(PlayerBase.PlayerEnding.NormalEnding);
                });
            }
            else
            {
                // 승진 아님 → 정산 UI → 확인 → 다음날(HomeScene)
                OpenDayResultUI(() =>
                {
                    Debug.Log("[WorkDayManager] 성과 달성 → HomeScene으로 이동");
                    GameFlowManager.Instance?.FinishDayAndGoNext();
                });
            }
        }
        else
        {
            // 성과 미달 → 정산 UI → 확인 → 게임오버(TitleScene or EndingScene)
            OpenDayResultUI(() =>
            {
                Debug.Log("[WorkDayManager] 성과 미달 → 게임오버");
                GameFlowManager.Instance?.TriggerGameOver(PlayerBase.PlayerEnding.PerformanceLess);
            });
        }
    }

    private void OpenDayResultUI(System.Action onConfirm)
    {
        if (dayResultViewPrefab == null)
        {
            // 프리팹이 없으면 씬에서 UIDayResultView를 탐색
            var existingView = FindFirstObjectByType<UIDayResultView>();
            if (existingView != null)
            {
                existingView.Open(_dayResultData, onConfirm);
                return;
            }
            Debug.LogWarning("[WorkDayManager] dayResultViewPrefab 미설정 & 씬에서 UIDayResultView를 찾을 수 없습니다. 정산 UI 없이 진행합니다.");
            onConfirm?.Invoke();
            return;
        }

        Transform parent = dayResultPanelRoot != null ? dayResultPanelRoot : transform;
        var view = Instantiate(dayResultViewPrefab, parent);
        view.Open(_dayResultData, () =>
        {
            Destroy(view.gameObject);
            onConfirm?.Invoke();
        });

        Debug.Log("[WorkDayManager] 하루 정산 UI 열기");
    }

    // ── 시간 UI ────────────────────────────────────────────────────────────
private void UpdateClockUI()
    {
        if (clockTimer == null) return;
        switch (currentPhase)
        {
            case DayPhase.MorningWork:
            case DayPhase.AfternoonWork:
                clockTimer.Tick(phaseTimer, currentPhase == DayPhase.MorningWork ? morningDuration : afternoonDuration);
                break;
            case DayPhase.LunchBreak:
                clockTimer.SetLunchBreak();
                break;
            case DayPhase.Finish:
                clockTimer.SetFinished();
                break;
        }
    }


}
