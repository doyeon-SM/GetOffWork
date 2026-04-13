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

    [Header("�÷��̾� �⺻ ����")]
    [SerializeField] private int playerLevel = 1;
    [SerializeField] private PlayerStat baseStats;

    [Header("���� ����")]
    [SerializeField] private int[] promotions = { 30 };
    [SerializeField] private int promotionIndex = 0;

    [Header("���� ��ǥ ����")]
    [SerializeField] private int goalPerformance = 10;

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

    public int GetMaxPerformance()
    {
        if (promotions == null || promotions.Length == 0)
        {
            Debug.LogError("[PlayerBase] promotions�� ��� �ֽ��ϴ�.");
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
        CheckPromotion();
        return true;
    }

    public bool AddPay(int amount)
    {
        if (!baseStats.CanAddPay(amount))
        {
            Debug.Log("������ ����");
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
            Debug.Log($"���� �Ϸ�! ���� ���� : {playerLevel}");
        }
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

    public void DebugLogStat()
    {
        Debug.Log($"Player Level : {playerLevel}");
        Debug.Log($"Player Stat : P{Performance} / K{Kindness} / S{Stress} / R{Reliability} / P{Pay}");
        Debug.Log($"Player Goal : M{promotions[promotionIndex]} | D{goalPerformance}");
    }

    public void CheckEnding(PlayerEnding endingType)
    {
        switch (endingType)
        {
            case PlayerEnding.NormalEnding:
                Debug.Log("�⺻ ����");
                break;
            case PlayerEnding.PerformanceLess:
                Debug.Log("���� ���� ����");
                break;
            case PlayerEnding.Stressfull:
                Debug.Log("��Ʈ���� ���� ����");
                break;
            case PlayerEnding.Unkindness:
                Debug.Log("ģ���� ���� ����");
                break;
        }
    }
}