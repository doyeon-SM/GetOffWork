using UnityEngine;

public class PlayerBase : MonoBehaviour
{
    [Header("플레이어 기본 스탯")]
    public int preformance = 0;
    public float kindness = 0.0f;
    public float stress = 0.0f;
    public float reliability = 0.0f;
    public int pay = 0;

    
    [Header("승진 조건")]
    public int[] promotion = { 30 };
    [SerializeField]
    private int promotion_index = 0;

    [SerializeField]
    private int goalpreformance = 0;

    //플레이어 스탯
    public enum playerStat
    {
        Kindness,
        Stress,
        Reliability
    }
    //플레이 엔딩 조건
    public enum playerEnding
    {
        Ending,
        Unkindness,
        Stressfull,
        preformanceless
    }

    //스탯 플러스
    public void statPlus(playerStat stat, int amount)
    {
        switch (stat)
        {
            case playerStat.Kindness:
                kindness += 0.05f * (float)amount;
                break;
            case playerStat.Stress:
                stress += 0.05f * (float)amount;
                if (stress >= 1.0f) Endingcheck(playerEnding.Stressfull);
                break;
            case playerStat.Reliability:
                reliability += 0.05f * (float)amount;
                break;
            default:
                break;
        }
    }

    //스탯 마이너스
    public void statMinus(playerStat stat, int amount)
    {
        switch (stat)
        {
            case playerStat.Kindness:
                kindness -= 0.05f * (float)amount;
                if (kindness <= 0.0f) Endingcheck(playerEnding.Unkindness);
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

    //성과 갱신
    public void setPreformance(int amount)
    {
        if(preformance + amount <= 0)
        {
            Debug.Log("성과 미달");
            return;
        }

        preformance += amount;

        //승진 확인
        PromotionCheck();
    }

    //급여 갱신
    public void setpay(int amount)
    {
        if(pay + amount < 0)
        {
            Debug.Log("소지금 부족");
            return;
        }

        pay += amount;
    }
    //승진 확인
    public void PromotionCheck()
    {
        //if : Promotion[Promotion_index] <= preformance ? Promotion_index++ : Promotion_index
    }
    //일일 목표 성과 달성 확인
    public void PreformanceCheck()
    {
        //if : goalpreformance > preformance ? Endingcheck(playerEnding.preformanceless) : 목표 갱신
    }
    //엔딩 확인
    public void Endingcheck(playerEnding endingN)
    {
        switch(endingN)
        {
            case playerEnding.Ending:
                break;
            case playerEnding.preformanceless:
                break;
            case playerEnding.Stressfull:
                break;
            case playerEnding.Unkindness:
                break;
            default:
                break;
        }
    }
}
