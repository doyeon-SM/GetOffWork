using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WorkDayManager : MonoBehaviour
{
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
    [SerializeField] private TMP_Text clockText;

    [Header("점심 UI 부모")]
    [SerializeField] private Transform lunchUIPanelRoot;

    [Header("점심 UI 프리팹")]
    [SerializeField] private UILunchChoice lunchChoiceUIPrefab;
    [SerializeField] private UILunchResult lunchResultUIPrefab;

    [Header("점심 옵션 목록")]
    [SerializeField] private List<LunchOptionData> lunchOptionList = new List<LunchOptionData>();

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

    private UILunchChoice   currentLunchChoiceUI;
    private UILunchResult   currentLunchResultUI;
    private LunchOptionData selectedLunchOption;

    // 하루 시작 스냅샷 (정산용)
    private DayResultData _dayResultData;

    public PlayerBase CurrentPlayerBase => playerBase;
    public DayPhase   CurrentPhase      => currentPhase;
    public float      CurrentPhaseTimer => phaseTimer;

    // ── Unity 생명주기 ─────────────────────────────────────────────────────
    private void Awake()
    {
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
        currentPhase         = DayPhase.MorningWork;
        phaseTimer           = morningDuration;
        isPausedByUI         = false;
        lunchChoiceCompleted = false;
        selectedLunchOption  = null;

        // 하루 시작 스냅샷 저장
        SnapshotDayStart();

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
            case DayPhase.MorningWork:   StartLunchBreak(); break;
            case DayPhase.AfternoonWork: StartFinish();     break;
            case DayPhase.LunchBreak:
                Debug.LogWarning("[WorkDayManager] 점심시간은 수동으로 닫혀야 해서 AdvancePhase가 작동하지 않습니다.");
                break;
        }
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
            int idx = Random.Range(0, validOptions.Count);
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
    public void EnqueueStatChangeEvent(StatChangeEvent evt)
    {
        if (_dayResultData == null) return;
        _dayResultData.statChangeProgress.Enqueue(evt);
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
            // 정산 UI 표시 후 확인 시 씬 이동
            OpenDayResultUI(() =>
            {
                Debug.Log("[WorkDayManager] 성과 달성 -> HomeScene으로 이동");
                GameFlowManager.Instance?.FinishDayAndGoNext();
            });
        }
        else
        {
            Debug.Log("[WorkDayManager] 성과 미달성 -> Title로 이동");
            GameFlowManager.Instance?.ReturnToTitle();
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
        if (clockText == null) return;
        switch (currentPhase)
        {
            case DayPhase.MorningWork:
            case DayPhase.AfternoonWork: clockText.text = FormatTime(phaseTimer); break;
            case DayPhase.LunchBreak:   clockText.text = "점심시간";              break;
            case DayPhase.Finish:       clockText.text = "00:00";               break;
        }
    }

    private string FormatTime(float time)
    {
        int total = Mathf.CeilToInt(time);
        if (total < 0) total = 0;
        return $"{total / 60:00}:{total % 60:00}";
    }
}
