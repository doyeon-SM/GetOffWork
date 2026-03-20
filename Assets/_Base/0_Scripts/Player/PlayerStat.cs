using UnityEngine;

[System.Serializable]
public struct PlayerStat
{
    [Header("플레이어 기본 스탯")]
    [SerializeField] private int performance;
    [SerializeField] private float kindness;
    [SerializeField] private float stress;
    [SerializeField] private float reliability;
    [SerializeField] private int pay;

    public int Performance => performance;
    public float Kindness => kindness;
    public float Stress => stress;
    public float Reliability => reliability;
    public int Pay => pay;

    public PlayerStat(int performance, float kindness, float stress, float reliability, int pay)
    {
        this.performance = performance;
        this.kindness = Mathf.Clamp01(kindness);
        this.stress = Mathf.Clamp01(stress);
        this.reliability = Mathf.Clamp01(reliability);
        this.pay = pay;
    }

    public PlayerStat WithAddedStat(PlayerBase.Stat stat, int amount)
    {
        float value = 0.05f * amount;

        switch (stat)
        {
            case PlayerBase.Stat.Kindness:
                return new PlayerStat(performance, kindness + value, stress, reliability, pay);

            case PlayerBase.Stat.Stress:
                return new PlayerStat(performance, kindness, stress + value, reliability, pay);

            case PlayerBase.Stat.Reliability:
                return new PlayerStat(performance, kindness, stress, reliability + value, pay);

            default:
                return this;
        }
    }

    public PlayerStat WithSubtractedStat(PlayerBase.Stat stat, int amount)
    {
        float value = 0.05f * amount;

        switch (stat)
        {
            case PlayerBase.Stat.Kindness:
                return new PlayerStat(performance, kindness - value, stress, reliability, pay);

            case PlayerBase.Stat.Stress:
                return new PlayerStat(performance, kindness, stress - value, reliability, pay);

            case PlayerBase.Stat.Reliability:
                return new PlayerStat(performance, kindness, stress, reliability - value, pay);

            default:
                return this;
        }
    }

    public bool CanAddPerformance(int amount)
    {
        return performance + amount >= 0;
    }

    public PlayerStat WithAddedPerformance(int amount)
    {
        return new PlayerStat(performance + amount, kindness, stress, reliability, pay);
    }

    public bool CanAddPay(int amount)
    {
        return pay + amount >= 0;
    }

    public PlayerStat WithAddedPay(int amount)
    {
        return new PlayerStat(performance, kindness, stress, reliability, pay + amount);
    }

    public PlayerStat WithPerformance(int value)
    {
        return new PlayerStat(value, kindness, stress, reliability, pay);
    }

    public PlayerStat WithKindness(float value)
    {
        return new PlayerStat(performance, value, stress, reliability, pay);
    }

    public PlayerStat WithStress(float value)
    {
        return new PlayerStat(performance, kindness, value, reliability, pay);
    }

    public PlayerStat WithReliability(float value)
    {
        return new PlayerStat(performance, kindness, stress, value, pay);
    }

    public PlayerStat WithPay(int value)
    {
        return new PlayerStat(performance, kindness, stress, reliability, value);
    }
}