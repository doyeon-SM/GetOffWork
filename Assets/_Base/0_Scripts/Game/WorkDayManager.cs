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

    [Header("업무 시스템")]
    [SerializeField] private ServiceDeskManager serviceDeskManager;

    [Header("시간 설정 (초)")]
    [SerializeField] private float morningDuration = 300f;
    [SerializeField] private float afternoonDuration = 300f;

    [Header("시계 UI")]
    [SerializeField] private TMP_Text clockText;

    [Header("점심 UI 부모")]
    [SerializeField] private Transform lunchUIPanelRoot;

    [Header("점심 UI 프리팹")]
    [SerializeField] private UILunchChoice lunchChoiceUIPrefab;
    [SerializeField] private UILunchResult lunchResultUIPrefab;

    [Header("점심 선택지 목록")]
    [SerializeField] private List<LunchOptionData> lunchOptionList = new List<LunchOptionData>();

    private DayPhase currentPhase;
    private float phaseTimer;
    private bool isPausedByUI;
    private bool isFinished;
    private bool lunchChoiceCompleted;

    private UILunchChoice currentLunchChoiceUI;
    private UILunchResult currentLunchResultUI;
    private LunchOptionData selectedLunchOption;

    public PlayerBase CurrentPlayerBase => playerBase;
    public DayPhase CurrentPhase => currentPhase;
    public float CurrentPhaseTimer => phaseTimer;

    private void Awake()
    {
        ResolvePlayerBase();
    }

    private void Start()
    {
        ResolvePlayerBase();

        if (serviceDeskManager != null && playerBase != null)
        {
            serviceDeskManager.SetPlayerBase(playerBase);
        }

        HideLunchUIObjects();
        StartMorningWork();
        UpdateClockUI();
    }

    private void Update()
    {
        if (isFinished)
            return;

        if (isPausedByUI)
        {
            UpdateClockUI();
            return;
        }

        if (currentPhase == DayPhase.MorningWork || currentPhase == DayPhase.AfternoonWork)
        {
            phaseTimer -= Time.deltaTime;

            if (phaseTimer < 0f)
                phaseTimer = 0f;

            UpdateClockUI();

            if (phaseTimer <= 0f)
            {
                AdvancePhase();
            }
        }
        else
        {
            UpdateClockUI();
        }
    }

    public void SetPlayerBase(PlayerBase player)
    {
        playerBase = player;

        if (serviceDeskManager != null)
        {
            serviceDeskManager.SetPlayerBase(playerBase);
        }
    }

    private void ResolvePlayerBase()
    {
        if (playerBase != null)
            return;

        playerBase = PlayerBase.Instance;

        if (playerBase == null)
        {
            Debug.LogError("[WorkDayManager] PlayerBase Instance가 없습니다!");
        }
    }

    private void StartMorningWork()
    {
        currentPhase = DayPhase.MorningWork;
        phaseTimer = morningDuration;
        isPausedByUI = false;
        lunchChoiceCompleted = false;
        selectedLunchOption = null;

        if (serviceDeskManager != null)
            serviceDeskManager.BeginWorkPhase();

        Debug.Log("[WorkDayManager] 오전 업무 시작");
        UpdateClockUI();
    }

    private void StartLunchBreak()
    {
        currentPhase = DayPhase.LunchBreak;
        phaseTimer = 0f;
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
        phaseTimer = afternoonDuration;
        isPausedByUI = false;

        if (serviceDeskManager != null)
            serviceDeskManager.BeginWorkPhase();

        Debug.Log("[WorkDayManager] 오후 업무 시작");
        UpdateClockUI();
    }

    private void StartFinish()
    {
        currentPhase = DayPhase.Finish;
        phaseTimer = 0f;
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
                StartLunchBreak();
                break;

            case DayPhase.LunchBreak:
                Debug.LogWarning("[WorkDayManager] 점심시간은 선택 후 결과창을 닫아야 오후가 시작됩니다.");
                break;

            case DayPhase.AfternoonWork:
                StartFinish();
                break;
        }
    }

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

        Debug.Log("[UI 위치] Panel 아래 점심 선택 UI 출력");
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

        Debug.Log("[UI 위치] Panel 아래 점심 결과 UI 출력");
    }

    private void HideLunchUIObjects()
    {
        if (currentLunchChoiceUI != null)
        {
            Destroy(currentLunchChoiceUI.gameObject);
            currentLunchChoiceUI = null;
        }

        if (currentLunchResultUI != null)
        {
            Destroy(currentLunchResultUI.gameObject);
            currentLunchResultUI = null;
        }
    }

    private List<LunchOptionData> GetRandomLunchOptions(int count)
    {
        List<LunchOptionData> validOptions = new List<LunchOptionData>();
        List<LunchOptionData> result = new List<LunchOptionData>();

        for (int i = 0; i < lunchOptionList.Count; i++)
        {
            if (lunchOptionList[i] != null)
            {
                validOptions.Add(lunchOptionList[i]);
            }
        }

        int pickCount = Mathf.Min(count, validOptions.Count);

        for (int i = 0; i < pickCount; i++)
        {
            int randomIndex = Random.Range(0, validOptions.Count);
            result.Add(validOptions[randomIndex]);
            validOptions.RemoveAt(randomIndex);
        }

        return result;
    }

    public void OnLunchOptionSelected(LunchOptionData optionData)
    {
        if (currentPhase != DayPhase.LunchBreak)
        {
            Debug.LogWarning("[WorkDayManager] 점심시간이 아닙니다.");
            return;
        }

        if (lunchChoiceCompleted)
        {
            Debug.LogWarning("[WorkDayManager] 이미 점심 선택이 완료되었습니다.");
            return;
        }

        if (optionData == null)
        {
            Debug.LogWarning("[WorkDayManager] 선택된 점심 옵션 데이터가 없습니다.");
            return;
        }

        ResolvePlayerBase();
        selectedLunchOption = optionData;
        lunchChoiceCompleted = true;

        ApplyLunchStatChanges(optionData);
        OpenLunchResultUI(optionData);
    }

    private void ApplyLunchStatChanges(LunchOptionData optionData)
    {
        if (playerBase == null || optionData == null || optionData.statChanges == null)
            return;

        for (int i = 0; i < optionData.statChanges.Count; i++)
        {
            LunchStatChange change = optionData.statChanges[i];
            if (change == null)
                continue;

            playerBase.AddStat(change.stat, change.amount);
            Debug.Log($"[Lunch] {change.stat}: {change.amount}");
        }
    }

    public void CloseLunchResultUIAndStartAfternoon()
    {
        if (currentPhase != DayPhase.LunchBreak)
        {
            Debug.LogWarning("[WorkDayManager] 현재 점심시간이 아닙니다.");
            return;
        }

        if (currentLunchResultUI != null)
        {
            Destroy(currentLunchResultUI.gameObject);
            currentLunchResultUI = null;
        }

        Debug.Log("[WorkDayManager] 점심 결과 UI 종료 -> 오후 시작");
        StartAfternoonWork();
    }

    private void FinishDay()
    {
        isFinished = true;

        Debug.Log("[UI 위치] 중앙 팝업: 결과창 UI 출력");
        Debug.Log("[WorkDayManager] 하루 종료");

        bool success = true;

        ResolvePlayerBase();

        if (playerBase != null)
        {
            success = playerBase.CheckPerformanceGoal();
        }

        if (success)
        {
            Debug.Log("[WorkDayManager] 업무 성공 -> HomeScene으로 이동");
            GameFlowManager.Instance?.FinishDayAndGoNext();
        }
        else
        {
            Debug.Log("[WorkDayManager] 업무 실패 -> Title로 이동");
            GameFlowManager.Instance?.ReturnToTitle();
        }
    }

    private void UpdateClockUI()
    {
        if (clockText == null)
            return;

        switch (currentPhase)
        {
            case DayPhase.MorningWork:
            case DayPhase.AfternoonWork:
                clockText.text = FormatTime(phaseTimer);
                break;

            case DayPhase.LunchBreak:
                clockText.text = "점심시간";
                break;

            case DayPhase.Finish:
                clockText.text = "00:00";
                break;
        }
    }

    private string FormatTime(float time)
    {
        int totalSeconds = Mathf.CeilToInt(time);
        if (totalSeconds < 0)
            totalSeconds = 0;

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        return $"{minutes:00}:{seconds:00}";
    }
}