using System;

/// <summary>
/// 메뉴얼에 정의된 절차 한 단계.
/// Manual의 RequiredSteps List에 순서대로 등록된다.
/// </summary>
[Serializable]
public class ManualStepEntry
{
    /// <summary>이 단계에서 수행해야 할 commandId</summary>
    public string CommandId;

    /// <summary>
    /// 이 단계가 순서를 강제하는지 여부.
    /// true  : 이전 단계가 완료되지 않으면 실행 불가 (순서 강제)
    /// false : 언제든 수행 가능한 자유 순서 단계
    /// </summary>
    public bool IsOrdered;

    /// <summary>
    /// 이 단계를 아예 건너뛰었을 때 적용할 패널티.
    /// 민원 종료 시 평가에서 사용된다.
    /// </summary>
    public StepPenalty OmissionPenalty;

    /// <summary>
    /// 이 단계를 잘못된 순서로 수행했을 때 적용할 패널티.
    /// IsOrdered == true인 단계에만 의미 있음.
    /// </summary>
    public StepPenalty OrderPenalty;

    /// <summary>
    /// 이 단계를 정상 수행했을 때 적용할 보상.
    /// 민원 종료 시 평가에서 사용된다.
    /// </summary>
    public StepReward CompletionReward;

    public ManualStepEntry(
        string commandId,
        bool isOrdered = true,
        StepPenalty omissionPenalty = default,
        StepPenalty orderPenalty = default,
        StepReward completionReward = default)
    {
        CommandId        = commandId;
        IsOrdered        = isOrdered;
        OmissionPenalty  = omissionPenalty;
        OrderPenalty     = orderPenalty;
        CompletionReward = completionReward;
    }
}

/// <summary>
/// 단계 누락 또는 순서 위반 시 적용되는 패널티 묶음.
/// 모든 필드 기본값 0 = 패널티 없음.
/// </summary>
[Serializable]
public struct StepPenalty
{
    public int Performance;
    public int Kindness;
    public int Stress;
    public int Reliability;
    public int Pay;

    public StepPenalty(
        int performance  = 0,
        int kindness     = 0,
        int stress       = 0,
        int reliability  = 0,
        int pay          = 0)
    {
        Performance = performance;
        Kindness    = kindness;
        Stress      = stress;
        Reliability = reliability;
        Pay         = pay;
    }

    public bool IsEmpty =>
        Performance == 0 && Kindness == 0 &&
        Stress      == 0 && Reliability == 0 && Pay == 0;
}

/// <summary>
/// 단계 정상 완료 시 적용되는 보상 묶음.
/// 모든 필드 기본값 0 = 보상 없음.
/// </summary>
[Serializable]
public struct StepReward
{
    public int Performance;
    public int Kindness;
    public int Reliability;
    public int Pay;

    public StepReward(
        int performance = 0,
        int kindness    = 0,
        int reliability = 0,
        int pay         = 0)
    {
        Performance = performance;
        Kindness    = kindness;
        Reliability = reliability;
        Pay         = pay;
    }

    public bool IsEmpty =>
        Performance == 0 && Kindness == 0 &&
        Reliability == 0 && Pay     == 0;
}
