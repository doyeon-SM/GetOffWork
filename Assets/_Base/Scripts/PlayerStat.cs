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
        this.kindness = kindness;
        this.stress = stress;
        this.reliability = reliability;
        this.pay = pay;
    }

    public void AddStat(PlayerBase.PlayerStat stat, int amount)
    {
        float value = 0.05f * amount;

        switch (stat)
        {
            case PlayerBase.PlayerStat.Kindness:
                kindness += value;
                kindness = Mathf.Clamp01(kindness);
                break;

            case PlayerBase.PlayerStat.Stress:
                stress += value;
                stress = Mathf.Clamp01(stress);
                break;

            case PlayerBase.PlayerStat.Reliability:
                reliability += value;
                reliability = Mathf.Clamp01(reliability);
                break;
        }
    }

    public void SubtractStat(PlayerBase.PlayerStat stat, int amount)
    {
        float value = 0.05f * amount;

        switch (stat)
        {
            case PlayerBase.PlayerStat.Kindness:
                kindness -= value;
                kindness = Mathf.Clamp01(kindness);
                break;

            case PlayerBase.PlayerStat.Stress:
                stress -= value;
                stress = Mathf.Clamp01(stress);
                break;

            case PlayerBase.PlayerStat.Reliability:
                reliability -= value;
                reliability = Mathf.Clamp01(reliability);
                break;
        }
    }

    public bool TryAddPerformance(int amount)
    {
        if (performance + amount <= 0)
            return false;

        performance += amount;
        return true;
    }

    public bool TryAddPay(int amount)
    {
        if (pay + amount < 0)
            return false;

        pay += amount;
        return true;
    }

    public void SetPerformanceGoal(int value)
    {
        performance = value;
    }
}