using UnityEngine;
public enum Stat
{
    Kindness,
    Stress,
    Reliability
}

public class PlayerBase : MonoBehaviour
{
    public static PlayerBase Instance { get; private set; }

    [Header("플레이어 기본 정보")]
    [SerializeField] private string playerName = "Player";
    [SerializeField] private int playerLevel = 1;
    [SerializeField] private PlayerStat baseStats;

    [Header("게임 목표")]
    [SerializeField] private int[] promotions = { 0 };
    [SerializeField] private int promotionIndex = 0;
    /// <summary>현재 승진 사이클이 시작된 날(currentDay 기준, 1-indexed)</summary>
    [SerializeField] private int cycleStartDay = 1;

    [Header("일일 목표 성과")]
    [SerializeField] private int goalPerformance = 0;

    public string PlayerName => playerName;
    public int PlayerLevel => playerLevel;
    public int Performance => baseStats.Performance;
    public float Kindness => baseStats.Kindness;
    public float Stress => baseStats.Stress;
    public float Reliability => baseStats.Reliability;
    public int Pay => baseStats.Pay;
    public int GoalPerformance => goalPerformance;
    public PlayerStat CurrentStats => baseStats;


    public enum PlayerEnding
    {
        NormalEnding,
        Unkindness,
        Stressfull,
        PerformanceLess
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void InitializeForNewGame(PlayerStat initialStats, int startLevel = 1, int startGoalPerformance = 0)
    {
        playerLevel = startLevel;
        promotionIndex = 0;
        cycleStartDay = 1;
        goalPerformance = Mathf.Max(0, startGoalPerformance);
        baseStats = initialStats;
    }

    /// <summary>타이틀 씬 입력창에서 호출 — 플레이어 이름을 저장합니다.</summary>
    public void SetPlayerName(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
            playerName = name.Trim();
    }

    public int GetMaxPerformance()
    {
        if (promotions == null || promotions.Length == 0)
        {
            Debug.LogError("[PlayerBase] promotions null");
            return 0;
        }

        int index = Mathf.Clamp(promotionIndex, 0, promotions.Length - 1);
        return promotions[index];
    }

    public void ApplyFullStats(PlayerStat stats)
    {
        baseStats = stats;
    }

    public void AddStat(Stat stat, int amount)
    {
        baseStats = baseStats.WithAddedStat(stat, amount);
        ValidateImmediateEndingByStat(stat);
    }


public bool AddPerformance(int amount)
    {
        int next = baseStats.Performance + amount;

        if (next < 0)
        {
            // 성과는 0 미만으로 내려가지 않는다.
            // 응대 실패로 인한 성과 해소는 0으로 케램프하고
            // 엔딩 체크는 하루 정산(CheckPerformanceGoal)에서만 수행한다.
            baseStats = baseStats.WithPerformance(0);
            Debug.Log("[PlayerBase] 성과가  0 미만으로 내려가려 함 — 0으로 클램프되었습니다.");
            return false;
        }

        baseStats = baseStats.WithAddedPerformance(amount);
        // 승진 체크는 하루 정산(WorkDayManager.FinishDay) 시점에만 수행한다.
        return true;
    }

    public bool AddPay(int amount)
    {
        if (!baseStats.CanAddPay(amount))
        {
            Debug.Log("[PlayerBase] Null CanAddPay");
            return false;
        }

        baseStats = baseStats.WithAddedPay(amount);
        return true;
    }

    /// <summary>
    /// 다음 날의 일일 목표 성과를 설정한다.
    /// value = currentDay (FinishDayAndGoNext에서 day++ 후 호출)
    /// goal = promotions[promotionIndex] / 5 * dayInCycle  (dayInCycle: 1~5 클램프)
    /// </summary>
    public void SetGoalPerformance(int value)
    {
        if (promotions == null || promotions.Length == 0) return;
        int index      = Mathf.Clamp(promotionIndex, 0, promotions.Length - 1);
        int dayInCycle = Mathf.Clamp(value - cycleStartDay + 1, 1, 5);
        goalPerformance = Mathf.RoundToInt(promotions[index] / 5f * dayInCycle);
        Debug.Log($"[PlayerBase] SetGoal day={value} cycleStart={cycleStartDay} dayInCycle={dayInCycle} goal={goalPerformance}");
    }

    /// <summary>승진 조건 충족 시 스탯/레벨을 갱신하고 true를 반환한다. 엔딩 트리거는 호출측에서 처리한다.</summary>
    public bool CheckPromotion()
    {
        if (promotions == null || promotions.Length == 0)
            return false;

        bool promoted = false;

        while (promotionIndex < promotions.Length &&
               baseStats.Performance >= promotions[promotionIndex])
        {
            promoted = true;

            // 승진 보너스: 5일 사이클에서 남은 일수 × 1000원
            int currentDay  = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentDay : cycleStartDay;
            int dayInCycle  = Mathf.Clamp(currentDay - cycleStartDay + 1, 1, 5);
            int remainDays  = 5 - dayInCycle;
            int bonusPay    = remainDays * 1000;
            if (bonusPay > 0)
            {
                AddPay(bonusPay);
                Debug.Log($"[PlayerBase] 승진 보너스! dayInCycle={dayInCycle} 남은일수={remainDays} +{bonusPay}원");
            }

            // 다음 사이클 시작일 = 다음 날
            cycleStartDay = currentDay + 1;

            promotionIndex++;
            playerLevel++;

            // 승진 시 스탯 초기화 (스트레스 제외, Pay는 보너스 포함 유지)
            baseStats = new PlayerStat(
                performance: 0,
                kindness:    0.2f,
                stress:      baseStats.Stress,
                reliability: 0.5f,
                pay:         baseStats.Pay
            );
            Debug.Log($"[PlayerBase] 승진! 레벨={playerLevel} 다음사이클시작일={cycleStartDay} / 스탯초기화(스트레스 유지)");
        }

        Debug.Log($"[PlayerBase] CheckPromotion 결과: {promoted}");
        return promoted;
    }

/// <summary>
    /// 하루 정산 시에만 호출한다.
    /// 성과가 일일 목표치에 미달하면 false를 반환한다.
    /// 엔딩 트리거는 호출측(WorkDayManager.FinishDay)에서 담당한다.
    /// </summary>
    public bool CheckPerformanceGoal()
    {
        if (baseStats.Performance < goalPerformance)
        {
            Debug.Log($"[PlayerBase] 성과 미달성 — 현재:{baseStats.Performance} / 목표:{goalPerformance}");
            return false;
        }
        Debug.Log("[PlayerBase] 성과 목표 달성");
        return true;
    }

    private void ValidateImmediateEndingByStat(Stat stat)
    {
        // 부동소수점 오차 대응: Clamp01 후 엡실론 여유를 둔 경계 판정
        const float EPSILON = 0.0001f;
        switch (stat)
        {
            case Stat.Kindness:
                if (baseStats.Kindness <= EPSILON)
                    CheckEnding(PlayerEnding.Unkindness);
                break;

            case Stat.Stress:
                if (baseStats.Stress >= 1.0f - EPSILON)
                    CheckEnding(PlayerEnding.Stressfull);
                break;

            case Stat.Reliability:
                if (baseStats.Reliability <= EPSILON)
                    CheckEnding(PlayerEnding.PerformanceLess);
                break;
        }
    }

    public void DebugLogStat()
    {
        Debug.Log($"Player Level : {playerLevel}");
        Debug.Log($"Player Stat : P{Performance} / K{Kindness} / S{Stress} / R{Reliability} / P{Pay}");
        Debug.Log($"Player Goal : M{promotions[promotionIndex]} | D{goalPerformance}");
    }

    /// <summary>
    /// 즉시 게임오버 엔딩 처리.
    /// NormalEnding은 WorkDayManager.FinishDay()가 담당하므로 여기서는 처리하지 않는다.
    /// </summary>
    public void CheckEnding(PlayerEnding endingType)
    {
        Debug.Log($"[PlayerBase] 엔딩 발생: {endingType}");
        switch (endingType)
        {
            case PlayerEnding.NormalEnding:
                // 승진 → GameFlowManager 경유로 EndingScene(해피엔딩)
                GameFlowManager.Instance?.TriggerGameOver(endingType);
                break;
            case PlayerEnding.PerformanceLess:
            case PlayerEnding.Stressfull:
            case PlayerEnding.Unkindness:
                // 즉시 게임오버 → GameFlowManager 경유로 EndingScene
                GameFlowManager.Instance?.TriggerGameOver(endingType);
                break;
        }
    }
}