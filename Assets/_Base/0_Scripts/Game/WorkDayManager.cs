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

    [Header("시간 설정")]
    [SerializeField] private float morningDuration = 90f;
    [SerializeField] private float lunchDuration = 15f;
    [SerializeField] private float afternoonDuration = 90f;

    private DayPhase currentPhase;
    private float phaseTimer;
    private bool isPausedByUI;
    private bool isFinished;

    public PlayerBase CurrentPlayerBase => playerBase;

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

        StartMorningWork();
    }

    private void Update()
    {
        if (isFinished || isPausedByUI)
            return;

        phaseTimer -= Time.deltaTime;

        if (phaseTimer <= 0f)
        {
            AdvancePhase();
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
            Debug.LogError("PlayerBase Instance가 없습니다!");
        }
    }

    private void StartMorningWork()
    {
        currentPhase = DayPhase.MorningWork;
        phaseTimer = morningDuration;

        if (serviceDeskManager != null)
            serviceDeskManager.BeginWorkPhase();

        Debug.Log("오전 업무 시작");
    }

    private void StartLunchBreak()
    {
        currentPhase = DayPhase.LunchBreak;
        phaseTimer = lunchDuration;

        if (serviceDeskManager != null)
            serviceDeskManager.StopWorkPhase();

        isPausedByUI = true;
        OpenLunchChoiceUI();

        Debug.Log("점심시간 시작");
    }

    private void StartAfternoonWork()
    {
        currentPhase = DayPhase.AfternoonWork;
        phaseTimer = afternoonDuration;

        if (serviceDeskManager != null)
            serviceDeskManager.BeginWorkPhase();

        Debug.Log("오후 업무 시작");
    }

    private void StartFinish()
    {
        currentPhase = DayPhase.Finish;
        phaseTimer = 0f;

        if (serviceDeskManager != null)
            serviceDeskManager.StopWorkPhase();

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
                StartAfternoonWork();
                break;
            case DayPhase.AfternoonWork:
                StartFinish();
                break;
        }
    }

    private void OpenLunchChoiceUI()
    {
        Debug.Log("점심 선택 UI 오픈");
    }

    public void CloseLunchChoiceUI()
    {
        isPausedByUI = false;
        Debug.Log("점심 선택 UI 종료");
    }

    public void ApplyLunchChoice_ReduceStress(int amount)
    {
        ResolvePlayerBase();

        if (playerBase == null)
            return;

        playerBase.SubtractStat(PlayerBase.Stat.Stress, amount);
    }

    public void ApplyLunchChoice_IncreaseKindness(int amount)
    {
        ResolvePlayerBase();

        if (playerBase == null)
            return;

        playerBase.AddStat(PlayerBase.Stat.Kindness, amount);
    }

    public void ApplyLunchChoice_IncreaseReliability(int amount)
    {
        ResolvePlayerBase();

        if (playerBase == null)
            return;

        playerBase.AddStat(PlayerBase.Stat.Reliability, amount);
    }

    private void FinishDay()
    {
        isFinished = true;

        bool success = true;

        ResolvePlayerBase();

        if (playerBase != null)
        {
            success = playerBase.CheckPerformanceGoal();
        }

        if (success)
        {
            Debug.Log("업무 성공, Home으로 이동");
            GameFlowManager.Instance?.FinishDayAndGoNext();
        }
        else
        {
            Debug.Log("업무 실패, Title로 이동");
            GameFlowManager.Instance?.ReturnToTitle();
        }
    }
}