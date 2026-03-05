using UnityEngine;

public class PlayerBase : MonoBehaviour
{
    [Header("플레이어 기본 스탯")]
    public int preformance = 0;
    public float kindness = 0.0f;
    public float stress = 0.0f;
    public float reliability = 0.0f;
    public int pay = 0;

    enum playerStat
    {
        Kindness,
        Stress,
        Reliability
    }

    void statPlus(playerStat stat, int amount)
    {
        switch (stat)
        {
            case playerStat.Kindness:
                kindness += 0.05f * (float)amount;
                break;
            case playerStat.Stress:
                stress += 0.05f * (float)amount;
                break;
            case playerStat.Reliability:
                reliability += 0.05f * (float)amount;
                break;
            default:
                break;
        }
    }

    void statMinus(playerStat stat, int amount)
    {
        switch (stat)
        {
            case playerStat.Kindness:
                kindness -= 0.05f * (float)amount;
                break;
            case playerStat.Stress:
                stress -= 0.05f * (float)amount;
                break;
            case playerStat.Reliability:
                reliability -= 0.05f * (float)amount;
                break;
            default:
                break;
        }
    }

}
