using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 민원이 종료될 때 플레이어 행동 Queue와 메뉴얼 RequiredSteps List를 비교해
/// 성과/스탯 변화량을 계산하는 평가기.
///
/// 패널티 기준:
///   필수 누락    : omissionPenalty 적용 (kindness -1 등)
///   순서 위반    : orderPenalty 적용   (reliability -1 등)
///   불필요 절차  : kindness -0.1/건 누적
///
/// isAddressMismatch == true 일 때 제외되는 패널티:
///   AskPrintOrMobile, PrintDocument, SendMobile 의 omission/order 패널티
///   (주소불일치 특수성: 인쇄/전송 안 하는 것이 올바른 행동이므로)
///
/// 시스템 전용 ID (CallDisplay 등)는 불필요 절차 집계에서 항상 제외한다.
/// </summary>
public static class ManualEvaluator
{
    private const float UnnecessaryKindnessPenaltyPerAction = 0.1f;

    /// <summary>주소불일치 시 패널티 평가에서 제외할 commandId 집합</summary>
    private static readonly HashSet<string> AddressMismatchExcludedIds = new HashSet<string>
    {
        ManualCommandIds.AskPrintOrMobile,
        ManualCommandIds.PrintDocument,
        ManualCommandIds.SendMobile,
    };

    /// <summary>
    /// 메뉴얼 절차를 평가하고 적용할 총 스탯 변화를 반환한다.
    /// isAddressMismatch == true 이면 인쇄/전송/수령질문 관련 패널티를 제외한다.
    /// </summary>
    public static EvaluationResult Evaluate(
        IReadOnlyList<ManualStepEntry> requiredSteps,
        IReadOnlyCollection<PlayerActionRecord> actionQueue,
        bool isAddressMismatch = false)
    {
        var result = new EvaluationResult();
        var actionList = new List<PlayerActionRecord>(actionQueue);

        // ── 1단계: 각 RequiredStep이 Queue에서 첫 번째로 등장하는 위치 탐색 ──
        var stepQueueIndex = new int[requiredSteps.Count];
        for (int i = 0; i < stepQueueIndex.Length; i++)
            stepQueueIndex[i] = -1;

        for (int si = 0; si < requiredSteps.Count; si++)
        {
            string targetId = requiredSteps[si].CommandId;
            for (int ai = 0; ai < actionList.Count; ai++)
            {
                if (actionList[ai].CommandId == targetId)
                {
                    stepQueueIndex[si] = ai;
                    break;
                }
            }
        }

        // ── 2단계: 각 RequiredStep 평가 ──
        for (int si = 0; si < requiredSteps.Count; si++)
        {
            var step    = requiredSteps[si];
            int foundAt = stepQueueIndex[si];

            // 주소불일치 시 제외 대상 commandId는 평가 건너뜀
            bool skipForMismatch = isAddressMismatch &&
                                   AddressMismatchExcludedIds.Contains(step.CommandId);
            if (skipForMismatch)
            {
                Debug.Log($"[Evaluator] 주소불일치로 제외: {step.CommandId}");
                continue;
            }

            // 누락
            if (foundAt == -1)
            {
                ApplyPenalty(ref result, step.OmissionPenalty);
                Debug.Log($"[Evaluator] 누락: {step.CommandId} → OmissionPenalty 적용");
                continue;
            }

            // 순서 위반 검사 (IsOrdered인 단계만)
            bool orderViolated = false;
            if (step.IsOrdered && si > 0)
            {
                for (int prev = si - 1; prev >= 0; prev--)
                {
                    if (!requiredSteps[prev].IsOrdered) continue;
                    int prevFound = stepQueueIndex[prev];
                    if (prevFound == -1) continue;

                    if (foundAt < prevFound)
                    {
                        orderViolated = true;
                        ApplyPenalty(ref result, step.OrderPenalty);
                        Debug.Log($"[Evaluator] 순서위반: {step.CommandId} (qi={foundAt}) < {requiredSteps[prev].CommandId} (qi={prevFound})");
                    }
                    break;
                }
            }

            // 정상 완료
            if (!orderViolated)
                ApplyReward(ref result, step.CompletionReward);
        }

        // ── 3단계: 불필요한 절차 집계 ──
        var requiredIds = new HashSet<string>();
        foreach (var step in requiredSteps)
            requiredIds.Add(step.CommandId);

        // 시스템 전용 ID (CallDisplay 등)는 집계에서 항상 제외
        var systemExcludedIds = new HashSet<string> { ManualCommandIds.CallDisplay };

        var actionCount = new Dictionary<string, int>();
        foreach (var action in actionList)
        {
            if (!actionCount.ContainsKey(action.CommandId))
                actionCount[action.CommandId] = 0;
            actionCount[action.CommandId]++;
        }

        int unnecessaryCount = 0;
        foreach (var kv in actionCount)
        {
            string cmdId = kv.Key;
            int count    = kv.Value;

            if (systemExcludedIds.Contains(cmdId)) continue;

            if (!requiredIds.Contains(cmdId))
            {
                unnecessaryCount += count;
                Debug.Log($"[Evaluator] 불필요(미정의): {cmdId} x{count}");
            }
            else if (count > 1)
            {
                unnecessaryCount += count - 1;
                Debug.Log($"[Evaluator] 불필요(중복): {cmdId} x{count - 1}");
            }
        }

        if (unnecessaryCount > 0)
        {
            float rawKindness = unnecessaryCount * UnnecessaryKindnessPenaltyPerAction;
            result.KindnessDelta -= Mathf.RoundToInt(rawKindness);
            result.UnnecessaryActionCount = unnecessaryCount;
            Debug.Log($"[Evaluator] 불필요 행동 {unnecessaryCount}건 → kindness {-rawKindness:F1}");
        }

        return result;
    }

    private static void ApplyPenalty(ref EvaluationResult result, StepPenalty penalty)
    {
        result.PerformanceDelta -= penalty.Performance;
        result.KindnessDelta    -= penalty.Kindness;
        result.StressDelta      += penalty.Stress;
        result.ReliabilityDelta -= penalty.Reliability;
        result.PayDelta         -= penalty.Pay;
    }

    private static void ApplyReward(ref EvaluationResult result, StepReward reward)
    {
        result.PerformanceDelta += reward.Performance;
        result.KindnessDelta    += reward.Kindness;
        result.ReliabilityDelta += reward.Reliability;
        result.PayDelta         += reward.Pay;
    }
}

/// <summary>ManualEvaluator.Evaluate()가 반환하는 스탯 변화 묶음.</summary>
public struct EvaluationResult
{
    public int PerformanceDelta;
    public int KindnessDelta;
    public int StressDelta;
    public int ReliabilityDelta;
    public int PayDelta;
    public int UnnecessaryActionCount;

    public bool IsEmpty =>
        PerformanceDelta == 0 && KindnessDelta    == 0 &&
        StressDelta      == 0 && ReliabilityDelta == 0 &&
        PayDelta         == 0;

    public override string ToString() =>
        $"Perf:{PerformanceDelta:+0;-0} Kind:{KindnessDelta:+0;-0} " +
        $"Stress:{StressDelta:+0;-0} Rel:{ReliabilityDelta:+0;-0} Unnecessary:{UnnecessaryActionCount}";
}
