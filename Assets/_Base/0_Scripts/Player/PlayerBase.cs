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
    [SerializeField] private int playerLevel = 1;
    [SerializeField] private PlayerStat baseStats;

    [Header("승진 조건")]
    [SerializeField] private int[] promotions = { 30 };
    [SerializeField] private int promotionIndex = 0;

    [Header("일일 목표 성과")]
    [SerializeField] private int goalPerformance = 0;

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
        goalPerformance = Mathf.Max(0, startGoalPerformance);
        baseStats = initialStats;
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
        if (!baseStats.CanAddPerformance(amount))
        {
            Debug.Log("성과 미달");
            CheckEnding(PlayerEnding.PerformanceLess);
            return false;
        }

        baseStats = baseStats.WithAddedPerformance(amount);
        CheckPromotion();
        return true;
    }

    public bool AddPay(int amount)
    {
        if (!baseStats.CanAddPay(amount))
        {
            Debug.Log("소지금 부족");
            return false;
        }

        baseStats = baseStats.WithAddedPay(amount);
        return true;
    }

    public void SetGoalPerformance(int value)
    {
        goalPerformance = Mathf.Max(0, value);
    }

    public void CheckPromotion()
    {
        if (promotions == null || promotions.Length == 0)
            return;

        while (promotionIndex < promotions.Length &&
               baseStats.Performance >= promotions[promotionIndex])
        {
            promotionIndex++;
            playerLevel++;
            Debug.Log($"승진 완료! 현재 레벨 : {playerLevel}");
        }
    }

    public bool CheckPerformanceGoal()
    {
        if (baseStats.Performance < goalPerformance)
        {
            CheckEnding(PlayerEnding.PerformanceLess);
            return false;
        }

        Debug.Log("일일 목표 성과 달성");
        return true;
    }

    private void ValidateImmediateEndingByStat(Stat stat)
    {
        switch (stat)
        {
            case Stat.Kindness:
                if (baseStats.Kindness <= 0.0f)
                    CheckEnding(PlayerEnding.Unkindness);
                break;

            case Stat.Stress:
                if (baseStats.Stress >= 1.0f)
                    CheckEnding(PlayerEnding.Stressfull);
                break;
        }
    }

    public void CheckEnding(PlayerEnding endingType)
    {
        switch (endingType)
        {
            case PlayerEnding.NormalEnding:
                Debug.Log("기본 엔딩");
                break;
            case PlayerEnding.PerformanceLess:
                Debug.Log("성과 부족 엔딩");
                break;
            case PlayerEnding.Stressfull:
                Debug.Log("스트레스 과다 엔딩");
                break;
            case PlayerEnding.Unkindness:
                Debug.Log("친절도 부족 엔딩");
                break;
        }
    }
}