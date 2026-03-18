using UnityEngine;

[System.Serializable]
public struct PlayerStats
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

    public PlayerStats(int performance, float kindness, float stress, float reliability, int pay)
    {
        this.performance = performance;
        this.kindness = Mathf.Clamp01(kindness);
        this.stress = Mathf.Clamp01(stress);
        this.reliability = Mathf.Clamp01(reliability);
        this.pay = pay;
    }

    public PlayerStats WithAddedStat(PlayerBase.PlayerStat stat, int amount)
    {
        float value = 0.05f * amount;

        switch (stat)
        {
            case PlayerBase.PlayerStat.Kindness:
                return new PlayerStats(performance, kindness + value, stress, reliability, pay);

            case PlayerBase.PlayerStat.Stress:
                return new PlayerStats(performance, kindness, stress + value, reliability, pay);

            case PlayerBase.PlayerStat.Reliability:
                return new PlayerStats(performance, kindness, stress, reliability + value, pay);

            default:
                return this;
        }
    }

    public PlayerStats WithSubtractedStat(PlayerBase.PlayerStat stat, int amount)
    {
        float value = 0.05f * amount;

        switch (stat)
        {
            case PlayerBase.PlayerStat.Kindness:
                return new PlayerStats(performance, kindness - value, stress, reliability, pay);

            case PlayerBase.PlayerStat.Stress:
                return new PlayerStats(performance, kindness, stress - value, reliability, pay);

            case PlayerBase.PlayerStat.Reliability:
                return new PlayerStats(performance, kindness, stress, reliability - value, pay);

            default:
                return this;
        }
    }

    public bool CanAddPerformance(int amount)
    {
        return performance + amount >= 0;
    }

    public PlayerStats WithAddedPerformance(int amount)
    {
        return new PlayerStats(performance + amount, kindness, stress, reliability, pay);
    }

    public bool CanAddPay(int amount)
    {
        return pay + amount >= 0;
    }

    public PlayerStats WithAddedPay(int amount)
    {
        return new PlayerStats(performance, kindness, stress, reliability, pay + amount);
    }

    public PlayerStats WithPerformance(int value)
    {
        return new PlayerStats(value, kindness, stress, reliability, pay);
    }

    public PlayerStats WithKindness(float value)
    {
        return new PlayerStats(performance, value, stress, reliability, pay);
    }

    public PlayerStats WithStress(float value)
    {
        return new PlayerStats(performance, kindness, value, reliability, pay);
    }

    public PlayerStats WithReliability(float value)
    {
        return new PlayerStats(performance, kindness, stress, value, pay);
    }

    public PlayerStats WithPay(int value)
    {
        return new PlayerStats(performance, kindness, stress, reliability, value);
    }
}