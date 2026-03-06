using UnityEngine;

public class PlayerBase : MonoBehaviour
{
    [Header("플레이어 기본 정보")]
    [SerializeField] private int playerLevel = 1;
    [SerializeField] private PlayerStats baseStats;

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

    public enum PlayerStat
    {
        Kindness,
        Stress,
        Reliability
    }

    public enum PlayerEnding
    {
        NormalEnding,
        Unkindness,
        Stressfull,
        PerformanceLess
    }

    public void AddStat(PlayerStat stat, int amount)
    {
        baseStats.AddStat(stat, amount);

        switch (stat)
        {
            case PlayerStat.Stress:
                if (baseStats.Stress >= 1.0f)
                {
                    CheckEnding(PlayerEnding.Stressfull);
                }
                break;
        }
    }

    public void SubtractStat(PlayerStat stat, int amount)
    {
        baseStats.SubtractStat(stat, amount);

        switch (stat)
        {
            case PlayerStat.Kindness:
                if (baseStats.Kindness <= 0.0f)
                {
                    CheckEnding(PlayerEnding.Unkindness);
                }
                break;
        }
    }

    public void AddPerformance(int amount)
    {
        if (!baseStats.TryAddPerformance(amount))
        {
            Debug.Log("성과 미달");
            return;
        }

        CheckPromotion();
    }

    public void AddPay(int amount)
    {
        if (!baseStats.TryAddPay(amount))
        {
            Debug.Log("소지금 부족");
            return;
        }
    }

    public void CheckPromotion()
    {
        if (promotions == null || promotions.Length == 0)
            return;

        if (promotionIndex < promotions.Length && baseStats.Performance >= promotions[promotionIndex])
        {
            promotionIndex++;
            playerLevel++;

            Debug.Log($"승진 완료! 현재 레벨 : {playerLevel}");
        }
    }

    public void CheckPerformanceGoal()
    {
        if (baseStats.Performance < goalPerformance)
        {
            CheckEnding(PlayerEnding.PerformanceLess);
        }
        else
        {
            Debug.Log("일일 목표 성과 달성");
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